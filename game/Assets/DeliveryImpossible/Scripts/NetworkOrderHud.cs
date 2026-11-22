using DeliveryImpossible.Networking;
using DeliveryImpossible.Orders;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryImpossible.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkOrderHud : NetworkBehaviour
    {
        private static readonly Color32 Slate = new Color32(31, 35, 40, 255);
        private static readonly Color32 Paper = new Color32(222, 216, 206, 255);
        private static readonly Color32 PaperMuted = new Color32(190, 185, 176, 255);
        private static readonly Color32 Orange = new Color32(255, 85, 0, 255);
        private static readonly Color32 Green = new Color32(34, 197, 94, 255);

        [Header("Root")]
        [SerializeField] private Canvas hudCanvas;
        [SerializeField] private GameObject orderPanel;

        [Header("Order manifest")]
        [SerializeField] private TMP_Text orderTitleText;
        [SerializeField] private TMP_Text objectiveText;
        [SerializeField] private TMP_Text objectiveHintText;
        [SerializeField] private TMP_Text orderStampText;
        [SerializeField] private Image stageOneBackground;
        [SerializeField] private Image stageTwoBackground;
        [SerializeField] private Image stageThreeBackground;
        [SerializeField] private TMP_Text stageOneStatus;
        [SerializeField] private TMP_Text stageTwoStatus;
        [SerializeField] private TMP_Text stageThreeStatus;

        [Header("Placement")]
        [SerializeField] private GameObject placementPanel;
        [SerializeField] private TMP_Text placementText;
        [SerializeField] private RectTransform placementFill;
        [SerializeField] private Camera waypointCamera;
        [SerializeField] private RectTransform waypoint;
        [SerializeField] private TMP_Text waypointText;
        [SerializeField] private GameObject meterPanel;
        [SerializeField] private TMP_Text meterHeading;
        [SerializeField] private TMP_Text meterValue;
        [SerializeField] private TMP_Text meterNote;
        [SerializeField] private Image meterFill;

        [Header("Shift audio")]
        [SerializeField] private AudioSource warehouseAmbience;
        [SerializeField] private AudioClip deliveryAccepted;
        [SerializeField] private AudioClip cargoBroken;
        private DeliveryOrderState previousState;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            SetVisible(false);
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            bool sessionStarted = NetworkSessionState.Active != null &&
                                  NetworkSessionState.Active.SessionStarted;
            NetworkDeliveryOrder order = sessionStarted ? NetworkDeliveryOrder.Active : null;
            SetVisible(sessionStarted && !NetworkLobbyUi.BlocksGameplay);
            UpdateAudio(sessionStarted, order);
            if (sessionStarted)
            {
                UpdateOrderPanel(order);
                UpdateWaypoint(order);
                UpdateMeter(order);
            }
        }

        private void UpdateWaypoint(NetworkDeliveryOrder order)
        {
            if (waypoint == null || waypointCamera == null) return;
            if (order == null || NetworkLobbyUi.BlocksGameplay ||
                !order.TryGetWaypoint(transform.position, out Vector3 marker, out string caption))
            {
                waypoint.gameObject.SetActive(false);
                return;
            }

            Vector3 view = waypointCamera.WorldToViewportPoint(marker);
            Vector2 offset = new Vector2(view.x - 0.5f, view.y - 0.5f);
            if (view.z <= 0f)
            {
                offset = -offset;
                offset.x = offset.x < 0f ? -0.42f : 0.42f;
                offset.y = -0.10f;
            }

            RectTransform canvasRect = (RectTransform)hudCanvas.transform;
            float margin = Mathf.Clamp((waypoint.rect.width * 0.5f + 18f) / canvasRect.rect.width, 0.10f, 0.43f);
            float limit = 0.5f - margin;
            Vector2 anchor = new Vector2(0.5f + Mathf.Clamp(offset.x, -limit, limit),
                0.5f + Mathf.Clamp(offset.y, -0.34f, 0.24f));
            waypoint.anchorMin = waypoint.anchorMax = anchor;
            waypoint.gameObject.SetActive(true);
            string arrow = offset.x < -limit ? "‹ " : offset.x > limit ? "› " : "";
            SetText(waypointText, $"{arrow}{caption}  ·  {Mathf.CeilToInt(Vector3.Distance(transform.position, marker))} м");
        }

        private void UpdateMeter(NetworkDeliveryOrder order)
        {
            if (meterPanel == null) return;
            if (order == null || NetworkLobbyUi.BlocksGameplay)
            {
                meterPanel.SetActive(false);
                return;
            }

            bool running = order.State is DeliveryOrderState.InTransit or DeliveryOrderState.ReadyForDelivery;
            if (order.CurrentMechanic == DeliveryMechanic.ColdChain &&
                order.State is not (DeliveryOrderState.Completed or DeliveryOrderState.ShiftCompleted))
            {
                bool cooling = running && order.IsCooling;
                int percent = Mathf.RoundToInt(order.Heat * 100f);
                bool danger = order.Heat >= 0.8f;
                Color color = danger ? new Color32(255, 112, 100, 255) :
                    cooling ? new Color32(116, 216, 210, 255) : new Color32(239, 181, 118, 255);
                string heading = running ? cooling ? "Охлаждается" : "Нагревается" : "Температура груза";
                string note = running
                    ? cooling ? "Охладите до 23% на этой станции." :
                        $"До перегрева: {Mathf.CeilToInt((1f - order.Heat) / NetworkDeliveryOrder.HeatingPerSecond)} с"
                    : "Нагрев начнётся, когда возьмёте груз.";
                if (order.State == DeliveryOrderState.Failed)
                {
                    heading = "Перегрев";
                    note = "Доставка будет начата заново.";
                }
                ShowMeter(heading, $"{percent}%", note, order.Heat, color);
            }
            else if (order.State == DeliveryOrderState.InTransit && order.CurrentMechanic == DeliveryMechanic.Security)
            {
                bool on = order.CurrentBeamActive;
                int seconds = order.ScannerSecondsRemaining;
                ShowMeter(on ? "Сканер активен" : "Проход свободен", $"{seconds} с",
                    on ? "До безопасного прохода" : "До включения сканера",
                    Mathf.Clamp01(seconds / (on ? 4f : 5f)),
                    on ? new Color32(255, 121, 103, 255) : new Color32(117, 219, 191, 255));
            }
            else if (order.CurrentMechanic == DeliveryMechanic.Drawbridge && order.BridgePowered && !order.BridgeReady)
            {
                ShowMeter("Мост опускается", $"{Mathf.RoundToInt(order.BridgeProgress * 100f)}%",
                    "Дождитесь, пока мост опустится.", order.BridgeProgress, new Color32(236, 183, 120, 255));
            }
            else
            {
                meterPanel.SetActive(false);
            }
        }

        private void ShowMeter(string heading, string value, string note, float fill, Color color)
        {
            meterPanel.SetActive(true);
            SetText(meterHeading, heading);
            SetText(meterValue, value);
            SetText(meterNote, note);
            meterValue.color = color;
            meterFill.rectTransform.anchorMax = new Vector2(Mathf.Clamp01(fill), 1f);
            meterFill.color = color;
        }

        private void UpdateAudio(bool sessionStarted, NetworkDeliveryOrder order)
        {
            if (warehouseAmbience == null) return;
            if (!sessionStarted)
            {
                if (warehouseAmbience.isPlaying) warehouseAmbience.Stop();
                previousState = DeliveryOrderState.AwaitingPickup;
                return;
            }

            if (!warehouseAmbience.isPlaying) warehouseAmbience.Play();
            if (order == null || order.State == previousState) return;
            previousState = order.State;
            if (previousState == DeliveryOrderState.Completed && deliveryAccepted != null)
                warehouseAmbience.PlayOneShot(deliveryAccepted);
            if (previousState == DeliveryOrderState.Failed && cargoBroken != null)
                warehouseAmbience.PlayOneShot(cargoBroken);
        }

        private void UpdateOrderPanel(NetworkDeliveryOrder order)
        {
            if (objectiveText == null)
            {
                return;
            }

            if (order == null)
            {
                objectiveText.text = "ОЖИДАНИЕ ЗАКАЗА";
                SetText(objectiveHintText, "Подключение к диспетчерской...");
                SetText(orderStampText, "ОЖИДАНИЕ");
                SetStage(stageOneBackground, stageOneStatus, StageVisual.Pending);
                SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Pending);
                SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Pending);
                SetPlacementProgress(0f, false);
                return;
            }

            float progress = order.PlacementNormalized;
            string cargoType = order.CurrentMechanic switch
            {
                DeliveryMechanic.Security => $"ЦЕЛОСТНОСТЬ {Mathf.RoundToInt(order.CurrentIntegrity * 100f)}%",
                DeliveryMechanic.Docking => order.State == DeliveryOrderState.ReadyForDelivery ?
                    order.IsDockAligned ? "СОВМЕЩЕНО" : "ПОВЕРНИТЕ" : "",
                DeliveryMechanic.Drawbridge => order.BridgeReady ? "МОСТ ОТКРЫТ" : "",
                DeliveryMechanic.ColdChain => "",
                _ => order.CurrentOrderRequiresTeam ? "ТЯЖЁЛОЕ" : "ЗАКАЗ"
            };
            string levelName = string.IsNullOrEmpty(order.CurrentLevelName) ? order.CurrentOrderName : order.CurrentLevelName;
            string cargoCounter = order.IncludesBonusCargo ? $"  ·  {order.CargoNumberInLevel}/2" : "";
            SetText(
                orderTitleText,
                $"{order.CurrentLevelNumber}/{order.TotalLevels}  ·  {levelName.ToUpperInvariant()}{cargoCounter}");
            switch (order.State)
            {
                case DeliveryOrderState.AwaitingPickup:
                    objectiveText.text = order.CurrentOrderName;
                    SetText(objectiveHintText, "Подойдите к отмеченному грузу и нажмите E.");
                    SetText(orderStampText, cargoType);
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Active);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Pending);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Pending);
                    SetPlacementProgress(0f, false);
                    break;

                case DeliveryOrderState.ReadyForDelivery when progress > 0f:
                    objectiveText.text = "ОСТАВЬТЕ ГРУЗ В ЗОНЕ";
                    SetText(objectiveHintText, "Не двигайте груз до подтверждения.");
                    SetText(orderStampText, "ПРИЁМКА");
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Done);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Done);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Active);
                    SetPlacementProgress(progress, true);
                    break;

                case DeliveryOrderState.ReadyForDelivery:
                    bool dock = order.CurrentMechanic == DeliveryMechanic.Docking;
                    objectiveText.text = dock ? "СОВМЕСТИТЕ СТРЕЛКИ" : "ДОСТАВЬТЕ ГРУЗ В ЗОНУ";
                    SetText(objectiveHintText, dock
                        ? "У гнезда нажмите Q для совмещения. Опустите взгляд и отпустите груз с E."
                        : "Занесите груз на площадку, опустите взгляд и отпустите его с E.");
                    SetText(orderStampText, dock || order.CurrentMechanic == DeliveryMechanic.ColdChain ? cargoType : "ПРИЁМКА");
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Done);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Done);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Active);
                    SetPlacementProgress(0f, false);
                    break;

                case DeliveryOrderState.InTransit:
                    objectiveText.text = order.CurrentMechanic switch
                    {
                        DeliveryMechanic.Drawbridge => !order.BridgePowered ? "Подойдите к педали моста" :
                            !order.BridgeReady ? "Дождитесь моста" :
                            order.CheckpointsPassed == 0 ? "Перейдите по мосту" : "Пройдите досмотр",
                        DeliveryMechanic.Conveyor => order.CheckpointsPassed > 0 ?
                            order.ActiveCargo.IsHeld ? "Несите груз к отгрузке" : "Заберите груз за шлюзом" :
                            order.ActiveCargo.IsHeld ? "Отправьте груз по ленте" : "Идёт сканирование груза",
                        DeliveryMechanic.Security => order.CurrentBeamActive
                            ? $"Дождитесь окна у сканера {order.CheckpointsPassed + 1}"
                            : "Пройдите через арку с грузом",
                        DeliveryMechanic.ColdChain => order.IsCooling ? "Охладите груз" : "Найдите охлаждение",
                        DeliveryMechanic.Docking => "Доставьте модуль на подстанцию",
                        _ => $"ПРОЙДИТЕ ВОРОТА {order.CheckpointsPassed + 1}/{order.CheckpointCount}"
                    };
                    string routeHint = string.IsNullOrEmpty(order.CurrentRouteHint) ? "Следуйте по разметке." : order.CurrentRouteHint;
                    if (order.CurrentMechanic == DeliveryMechanic.Drawbridge && !order.BridgePowered)
                    {
                        routeHint = order.IncludesBonusCargo
                            ? "Встаньте вдвоём на две оранжевые плиты."
                            : "Встаньте на любую оранжевую плиту.";
                    }
                    else if (order.CurrentMechanic == DeliveryMechanic.Conveyor && order.CheckpointsPassed == 0)
                    {
                        routeHint = order.ActiveCargo.IsHeld
                            ? "Отпустите груз с E над отмеченным началом ленты."
                            : "Шлюз справа откроется, когда груз пройдёт датчик.";
                    }
                    else if (order.CurrentMechanic == DeliveryMechanic.Conveyor && !order.ActiveCargo.IsHeld)
                    {
                        routeHint = "Боковой шлюз открыт. Подберите груз с другой стороны ленты.";
                    }
                    else if (order.CurrentMechanic == DeliveryMechanic.Security)
                    {
                        routeHint = order.CurrentBeamActive
                            ? "Красные лучи повредят груз. До выключения остаётся время на панели."
                            : "Перенесите груз за арку, пока лучи погашены.";
                    }
                    else if (order.CurrentOrderRequiresTeam)
                    {
                        routeHint += "Несите груз вместе.";
                    }

                    SetText(objectiveHintText, routeHint);
                    SetText(orderStampText, cargoType);
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Done);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Active);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Pending);
                    SetPlacementProgress(0f, false);
                    break;

                case DeliveryOrderState.Completed:
                    objectiveText.text = "ЗАКАЗ ВЫПОЛНЕН";
                    SetText(
                        objectiveHintText,
                        order.CurrentOrderIsFragile
                            ? $"ОЦЕНКА: {order.LastGrade} · ЦЕЛОСТНОСТЬ: {Mathf.RoundToInt(order.CurrentIntegrity * 100f)}% · ДАЛЬШЕ: {order.TransitionSecondsRemaining} СЕК."
                            : $"ОЦЕНКА: {order.LastGrade} · {NetworkDeliveryOrder.FormatTime(order.ElapsedSeconds)} · ПАДЕНИЯ: {order.DropCount} · ДАЛЬШЕ: {order.TransitionSecondsRemaining} СЕК.");
                    SetText(orderStampText, order.LastGrade.ToString());
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Done);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Done);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Done);
                    SetPlacementProgress(1f, false);
                    break;
                case DeliveryOrderState.Failed:
                    objectiveText.text = order.Failure switch
                    {
                        DeliveryFailure.TimeExpired => "ВРЕМЯ ВЫШЛО",
                        DeliveryFailure.Overheated => "ГРУЗ ПЕРЕГРЕЛСЯ",
                        _ => "ГРУЗ РАЗБИТ"
                    };
                    SetText(objectiveHintText, $"R — повторить сейчас. Автоповтор через {order.TransitionSecondsRemaining} сек.");
                    SetText(orderStampText, "ПОВТОР");
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Done);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Pending);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Pending);
                    SetPlacementProgress(0f, false);
                    break;

                case DeliveryOrderState.ShiftCompleted:
                    SetText(orderTitleText, order.CampaignWon ? "КАМПАНИЯ ПРОЙДЕНА" : "КАМПАНИЯ НЕ ПРОЙДЕНА");
                    objectiveText.text = $"ВЫПОЛНЕНО {order.CompletedOrders}/{order.TotalOrders}";
                    SetText(
                        objectiveHintText,
                        $"ИТОГ: {order.TotalScore} ОЧКОВ");
                    SetText(orderStampText, "ИТОГ");
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Done);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Done);
                    SetStage(stageThreeBackground, stageThreeStatus,
                        order.CompletedOrders == order.TotalOrders ? StageVisual.Done : StageVisual.Pending);
                    SetPlacementProgress(0f, false);
                    break;
            }
        }

        private void SetPlacementProgress(float progress, bool visible)
        {
            if (placementPanel != null)
            {
                placementPanel.SetActive(visible);
            }

            int percent = Mathf.RoundToInt(Mathf.Clamp01(progress) * 100f);
            SetText(placementText, $"НЕ ТРОГАЙТЕ ГРУЗ — {percent}%");
            if (placementFill != null)
            {
                Vector2 anchorMax = placementFill.anchorMax;
                anchorMax.x = Mathf.Clamp01(progress);
                placementFill.anchorMax = anchorMax;
            }
        }

        private static void SetStage(Image background, TMP_Text status, StageVisual visual)
        {
            if (background != null)
            {
                background.color = visual switch
                {
                    StageVisual.Done => PaperMuted,
                    StageVisual.Active => Orange,
                    _ => Paper
                };
            }

            if (status == null)
            {
                return;
            }

            status.text = visual switch
            {
                StageVisual.Done => "СДАНО",
                StageVisual.Active => "В РАБОТЕ",
                _ => "ОЖИДАЕТ"
            };
            status.color = visual == StageVisual.Done ? Green : Slate;
        }

        private static void SetText(TMP_Text target, string value)
        {
            if (target != null)
            {
                target.text = value;
            }
        }

        public override void OnStopLocalPlayer()
        {
            SetVisible(false);
            warehouseAmbience?.Stop();
            base.OnStopLocalPlayer();
        }

        private void SetVisible(bool visible)
        {
            if (hudCanvas != null)
            {
                hudCanvas.enabled = visible;
            }

            if (orderPanel != null)
            {
                orderPanel.SetActive(visible);
            }
        }

        private enum StageVisual
        {
            Pending,
            Active,
            Done
        }
    }
}
