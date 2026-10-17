using System.Linq;
using DeliveryImpossible.Interaction;
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
        private static readonly Color32 Muted = new Color32(83, 88, 95, 255);

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

        [Header("Teammate")]
        [SerializeField] private TMP_Text teammateTitleText;
        [SerializeField] private TMP_Text teammateConnectionText;
        [SerializeField] private TMP_Text teammateStateText;
        [SerializeField] private TMP_Text teammateGripText;
        [SerializeField] private TMP_Text teammateStabilityText;
        [SerializeField] private Image[] teammateGripSegments;

        private NetworkGrabber localGrabber;
        private NetworkGrabber teammateGrabber;
        private float nextTeammateSearchTime;

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            localGrabber = GetComponent<NetworkGrabber>();
            SetVisible(true);
            UpdateTeammatePanel(true);
        }

        private void Update()
        {
            if (!isLocalPlayer)
            {
                return;
            }

            UpdateOrderPanel(NetworkDeliveryOrder.Active);
            UpdateTeammatePanel(false);
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
            SetText(
                orderTitleText,
                $"{order.CurrentOrderName.ToUpperInvariant()}  ·  {order.CurrentOrderNumber}/{order.TotalOrders}");
            switch (order.State)
            {
                case DeliveryOrderState.AwaitingPickup:
                    objectiveText.text = $"ЗАБЕРИТЕ {order.CurrentOrderName.ToUpperInvariant()}";
                    SetText(objectiveHintText, "Подойдите к грузу и нажмите E.");
                    SetText(orderStampText, order.CurrentOrderIsFragile ? "ХРУПКОЕ" : "ТЯЖЁЛОЕ");
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Active);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Pending);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Pending);
                    SetPlacementProgress(0f, false);
                    break;

                case DeliveryOrderState.ReadyForDelivery when progress > 0f:
                    objectiveText.text = $"ОСТАВЬТЕ {order.CurrentOrderName.ToUpperInvariant()} В ЗОНЕ";
                    SetText(objectiveHintText, "Не двигайте груз до подтверждения.");
                    SetText(orderStampText, "ПРИЁМКА");
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Done);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Done);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Active);
                    SetPlacementProgress(progress, true);
                    break;

                case DeliveryOrderState.ReadyForDelivery:
                    objectiveText.text = $"ДОСТАВЬТЕ {order.CurrentOrderName.ToUpperInvariant()} В ЗОНУ";
                    SetText(objectiveHintText, "Поставьте груз внутрь зелёной разметки.");
                    SetText(orderStampText, "ПРИЁМКА");
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Done);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Done);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Active);
                    SetPlacementProgress(0f, false);
                    break;

                case DeliveryOrderState.InTransit:
                    objectiveText.text = $"ПРОНЕСИТЕ {order.CurrentOrderName.ToUpperInvariant()} ЧЕРЕЗ ДВЕРЬ";
                    SetText(
                        objectiveHintText,
                        order.CurrentOrderIsFragile
                            ? $"Не ударяйте груз. Целостность: {Mathf.RoundToInt(order.CurrentIntegrity * 100f)}%."
                            : "Балансируйте груз вместе с напарником.");
                    SetText(orderStampText, order.CurrentOrderIsFragile ? "ХРУПКОЕ" : "ТЯЖЁЛОЕ");
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

                case DeliveryOrderState.ShiftCompleted:
                    SetText(orderTitleText, "СМЕНА ЗАВЕРШЕНА");
                    objectiveText.text = $"ВЫПОЛНЕНО {order.CompletedOrders}/{order.TotalOrders}";
                    SetText(
                        objectiveHintText,
                        $"ИТОГ: {order.TotalScore} ОЧКОВ · НОВАЯ СМЕНА: {order.TransitionSecondsRemaining} СЕК.");
                    SetText(orderStampText, "ИТОГ");
                    SetStage(stageOneBackground, stageOneStatus, StageVisual.Done);
                    SetStage(stageTwoBackground, stageTwoStatus, StageVisual.Done);
                    SetStage(stageThreeBackground, stageThreeStatus, StageVisual.Done);
                    SetPlacementProgress(0f, false);
                    break;
            }
        }

        private void UpdateTeammatePanel(bool forceSearch)
        {
            if (forceSearch || teammateGrabber == null || !teammateGrabber.isActiveAndEnabled)
            {
                if (forceSearch || Time.unscaledTime >= nextTeammateSearchTime)
                {
                    nextTeammateSearchTime = Time.unscaledTime + 0.5f;
                    teammateGrabber = FindObjectsByType<NetworkGrabber>(FindObjectsSortMode.None)
                        .FirstOrDefault(grabber => grabber != localGrabber && grabber.isClient);
                }
            }

            if (teammateGrabber == null)
            {
                SetText(teammateTitleText, "НАПАРНИК: ОЖИДАНИЕ");
                SetText(teammateConnectionText, "НЕ В СЕТИ");
                SetText(teammateStateText, "ОЖИДАЕМ ПОДКЛЮЧЕНИЯ");
                SetText(teammateGripText, "ХВАТ: 0%");
                SetText(teammateStabilityText, "—");
                SetGripSegments(0f, Muted);
                return;
            }

            NetworkGrabbable heldCargo = teammateGrabber.HeldObject;
            SetText(teammateTitleText, "НАПАРНИК");
            SetText(teammateConnectionText, "В СЕТИ");

            if (heldCargo == null)
            {
                SetText(teammateStateText, "ГРУЗ НЕ ДЕРЖИТ");
                SetText(teammateGripText, "ХВАТ: 0%");
                SetText(teammateStabilityText, "ОЖИДАЕТ");
                SetGripSegments(0f, Muted);
                return;
            }

            bool fullyHeld = heldCargo.IsFullyHeld;
            float grip = fullyHeld ? 1f : 0.5f;
            Color32 gripColor = fullyHeld ? Green : Orange;
            SetText(teammateStateText, "ДЕРЖИТ ГРУЗ");
            SetText(teammateGripText, $"ХВАТ: {Mathf.RoundToInt(grip * 100f)}%");
            SetText(teammateStabilityText, fullyHeld ? "ОТЛИЧНАЯ" : "НУЖНА ПОМОЩЬ");
            SetGripSegments(grip, gripColor);
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

        private void SetGripSegments(float normalized, Color32 activeColor)
        {
            if (teammateGripSegments == null)
            {
                return;
            }

            int activeCount = Mathf.RoundToInt(normalized * teammateGripSegments.Length);
            for (int index = 0; index < teammateGripSegments.Length; index++)
            {
                if (teammateGripSegments[index] != null)
                {
                    teammateGripSegments[index].color = index < activeCount
                        ? activeColor
                        : Muted;
                }
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
