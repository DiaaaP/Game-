using System;
using DeliveryImpossible.Interaction;
using DeliveryImpossible.Networking;
using DeliveryImpossible.Player;
using Mirror;
using UnityEngine;
using LobbyPlayer = DeliveryImpossible.Networking.NetworkLobbyPlayer;

namespace DeliveryImpossible.Orders
{
    public enum DeliveryOrderState : byte
    {
        AwaitingPickup,
        InTransit,
        ReadyForDelivery,
        Completed,
        ShiftCompleted,
        Failed
    }

    public enum DeliveryGrade : byte
    {
        None,
        S,
        A,
        B,
        C
    }

    public enum DeliveryFailure : byte
    {
        None,
        BrokenCargo,
        TimeExpired,
        Overheated
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkDeliveryOrder : NetworkBehaviour
    {
        [Serializable]
        private sealed class OrderDefinition
        {
            public string displayName = "ГРУЗ";
            public string levelName;
            public string routeHint;
            public int levelNumber;
            public bool coOpOnly;
            public DeliveryCourse course;
            public NetworkGrabbable targetCargo;
            public DeliveryCheckpoint doorwayCheckpoint;
            public DeliveryCheckpoint[] routeCheckpoints;
            public DeliveryZone deliveryZone;
            public Transform courierSpawn;
            [Min(0f)] public float timeLimit;

            public int CheckpointCount => routeCheckpoints is { Length: > 0 } ? routeCheckpoints.Length : 1;

            public DeliveryCheckpoint GetCheckpoint(int index) =>
                routeCheckpoints is { Length: > 0 } ? routeCheckpoints[index] : doorwayCheckpoint;
        }

        [Header("Orders")]
        [SerializeField] private OrderDefinition[] orders;

        [Header("Completion")]
        [SerializeField, Min(0.1f)] private float placementDuration = 1.5f;
        [SerializeField, Min(0f)] private float maximumLinearSpeed = 0.35f;
        [SerializeField, Min(0f)] private float maximumAngularSpeed = 0.75f;
        [SerializeField, Min(1f)] private float nextOrderDelay = 6f;

        [SyncVar] private DeliveryOrderState state;
        [SyncVar] private int currentOrderIndex;
        [SyncVar] private float placementProgress;
        [SyncVar] private double startedAtNetworkTime;
        [SyncVar] private float completionTimeSeconds;
        [SyncVar] private float completionIntegrity = 1f;
        [SyncVar] private int dropCount;
        [SyncVar] private DeliveryGrade lastGrade;
        [SyncVar] private int completedOrders;
        [SyncVar] private int totalScore;
        [SyncVar] private double transitionAtNetworkTime;
        [SyncVar] private int checkpointsPassed;
        [SyncVar] private DeliveryFailure failure;
        [SyncVar] private bool includesBonusCargo;
        [SyncVar] private int currentOrderNumber;
        [SyncVar] private bool bridgePowered;
        [SyncVar] private float heat;
        [SyncVar] private double bridgeStartedAt;
        [SyncVar] private double bypassStartedAt;

        private Rigidbody cargoBody;
        private Vector3[] initialCargoPositions;
        private Quaternion[] initialCargoRotations;
        private bool wasHeld;
        private int soloOrderCount;
        private int levelCount;
        private double nextBeamDamageAt;
        private double nextFallCheckAt;

        public static NetworkDeliveryOrder Active { get; private set; }

        public DeliveryOrderState State => state;
        public bool CurrentOrderRequiresTeam => CurrentCargo?.RequiresTeam == true;
        public int CurrentOrderNumber => currentOrderNumber;
        public int TotalOrders => includesBonusCargo ? orders?.Length ?? 0 : soloOrderCount;
        public int CurrentLevelNumber => CurrentOrder?.levelNumber > 0 ? CurrentOrder.levelNumber : currentOrderNumber;
        public int TotalLevels => levelCount;
        public int CargoNumberInLevel => CurrentOrder?.coOpOnly == true ? 2 : 1;
        public bool IncludesBonusCargo => includesBonusCargo;
        public bool BridgePowered => bridgePowered;
        public float BridgeProgress => !bridgePowered ? 0f :
            Mathf.Clamp01((float)(NetworkTime.time - bridgeStartedAt) / DeliveryCourse.BridgeTravelSeconds);
        public bool BridgeReady => BridgeProgress >= 0.999f;
        public float BypassGateProgress => bypassStartedAt <= 0d ? 0f :
            Mathf.Clamp01((float)(NetworkTime.time - bypassStartedAt) / 0.85f);
        public const float HeatingPerSecond = 0.067f;
        public const float CoolingPerSecond = 0.47f;
        public float Heat => heat;
        public bool IsCooling => CurrentMechanic == DeliveryMechanic.ColdChain && CurrentCargo != null &&
                                 CurrentOrder.course.IsCooling(CurrentCargo.WorldCenter);
        public DeliveryMechanic CurrentMechanic => CurrentOrder?.course?.Mechanic ?? DeliveryMechanic.Standard;
        public bool CurrentBeamActive => CurrentMechanic == DeliveryMechanic.Security && checkpointsPassed < CheckpointCount &&
                                         DeliveryCourse.IsScannerActive(checkpointsPassed);
        public int ScannerSecondsRemaining => Mathf.Max(1, Mathf.CeilToInt(DeliveryCourse.ScannerTimeRemaining(checkpointsPassed)));
        public bool IsDockAligned => CurrentMechanic != DeliveryMechanic.Docking ||
                                     DeliveryCourse.DockAligned(CurrentCargo.transform, CurrentOrder.deliveryZone.transform);
        public int CompletedOrders => completedOrders;
        public int TotalScore => totalScore;
        public DeliveryGrade LastGrade => lastGrade;
        public string CurrentOrderName => CurrentOrder?.displayName ?? "ГРУЗ";
        public string CurrentLevelName => CurrentOrder?.levelName ?? "";
        public string CurrentRouteHint => CurrentOrder?.routeHint ?? "";
        public int CheckpointsPassed => checkpointsPassed;
        public int CheckpointCount => CurrentOrder?.CheckpointCount ?? 0;
        public DeliveryFailure Failure => failure;
        public bool HasTimeLimit => CurrentOrder?.timeLimit > 0f;
        public bool CampaignWon => state == DeliveryOrderState.ShiftCompleted && completedOrders == TotalOrders;
        public int TimeRemaining => Mathf.Max(0, Mathf.CeilToInt((CurrentOrder?.timeLimit ?? 0f) - ElapsedSeconds));
        public bool CurrentOrderIsFragile => CurrentFragileCargo != null;
        public float CurrentIntegrity => state == DeliveryOrderState.Completed
            ? completionIntegrity
            : CurrentFragileCargo?.IntegrityNormalized ?? 1f;
        public float PlacementNormalized => placementDuration > 0f
            ? Mathf.Clamp01(placementProgress / placementDuration)
            : 0f;
        public float ElapsedSeconds => state is DeliveryOrderState.Completed or DeliveryOrderState.Failed
            ? completionTimeSeconds
            : startedAtNetworkTime > 0d
                ? Mathf.Max(0f, (float)(NetworkTime.time - startedAtNetworkTime))
                : 0f;
        public int DropCount => dropCount;
        public int TransitionSecondsRemaining =>
            state is DeliveryOrderState.Completed or DeliveryOrderState.Failed
                ? Mathf.Max(0, Mathf.CeilToInt((float)(transitionAtNetworkTime - NetworkTime.time)))
                : 0;

        private OrderDefinition CurrentOrder =>
            orders != null && currentOrderIndex >= 0 && currentOrderIndex < orders.Length
                ? orders[currentOrderIndex]
                : null;
        private NetworkGrabbable CurrentCargo => CurrentOrder?.targetCargo;
        public NetworkGrabbable ActiveCargo => CurrentCargo;
        private NetworkFragileCargo CurrentFragileCargo =>
            CurrentCargo != null ? CurrentCargo.GetComponent<NetworkFragileCargo>() : null;

        public bool UsesCourse(DeliveryCourse course) => ReferenceEquals(CurrentOrder?.course, course);

        public bool TryGetNearbyDockRotation(NetworkGrabbable cargo, Vector3 player, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            if (cargo != CurrentCargo || CurrentMechanic != DeliveryMechanic.Docking || state != DeliveryOrderState.ReadyForDelivery)
                return false;
            Transform dock = CurrentOrder.deliveryZone.transform;
            if (FlatDistance(player, dock.position) > 5f) return false;
            rotation = dock.rotation;
            return true;
        }

        public bool TryGetWaypoint(Vector3 viewer, out Vector3 marker, out string caption)
        {
            marker = Vector3.zero;
            caption = "";
            if (CurrentCargo == null || state is DeliveryOrderState.Failed or DeliveryOrderState.Completed or DeliveryOrderState.ShiftCompleted)
                return false;

            var course = CurrentOrder.course;
            bool pickedUp = CurrentCargo.IsHeld;
            if (CurrentMechanic == DeliveryMechanic.Drawbridge && !bridgePowered && pickedUp)
            {
                marker = course.NearestPlate(viewer).transform.position;
                caption = "УПРАВЛЕНИЕ МОСТОМ";
            }
            else if (state == DeliveryOrderState.InTransit && CurrentMechanic == DeliveryMechanic.Conveyor &&
                     checkpointsPassed == 0 && (pickedUp || course.IsOnBelt(CurrentCargo.WorldCenter)))
            {
                marker = pickedUp && course.BeltLoadPoint != null
                    ? course.BeltLoadPoint.position : CurrentOrder.GetCheckpoint(0).transform.position;
                caption = pickedUp ? "ПОГРУЗИТЕ НА ЛЕНТУ" : "ПРИЁМНИК ЛЕНТЫ";
            }
            else if (!pickedUp)
            {
                marker = CurrentCargo.WorldCenter;
                caption = CurrentOrderName.ToUpperInvariant();
            }
            else if (state == DeliveryOrderState.ReadyForDelivery)
            {
                marker = CurrentOrder.deliveryZone.transform.position;
                caption = CurrentMechanic == DeliveryMechanic.Docking ? "ГНЕЗДО МОДУЛЯ" : "ПРИЁМКА";
            }
            else if (state == DeliveryOrderState.InTransit)
            {
                marker = CurrentOrder.GetCheckpoint(checkpointsPassed).transform.position;
                caption = CurrentMechanic switch
                {
                    DeliveryMechanic.Drawbridge => checkpointsPassed == 0 ? "КОНЕЦ МОСТА" : "ПРОВЕРКА НА ПРИЧАЛЕ",
                    DeliveryMechanic.Conveyor => "ПОСТ КОНТРОЛЯ",
                    DeliveryMechanic.Security => $"СКАНЕР {checkpointsPassed + 1}",
                    DeliveryMechanic.ColdChain => $"ОХЛАЖДЕНИЕ {checkpointsPassed + 1}/{CheckpointCount}",
                    DeliveryMechanic.Docking => checkpointsPassed switch
                    {
                        0 => "ВЫХОД С МОСТИКА",
                        1 => "НАСОСНЫЙ УЗЕЛ",
                        _ => "ЭНЕРГОСЕКЦИЯ"
                    },
                    _ => "МАРШРУТ"
                };
            }
            else
            {
                marker = CurrentCargo.WorldCenter;
                caption = CurrentOrderName.ToUpperInvariant();
            }

            marker.y += 0.65f;
            return true;
        }

        private static float FlatDistance(Vector3 first, Vector3 second)
        {
            Vector3 difference = first - second;
            difference.y = 0f;
            return difference.magnitude;
        }

        private void Awake()
        {
            if (orders == null) return;
            foreach (OrderDefinition order in orders)
            {
                if (order == null || order.coOpOnly) continue;
                soloOrderCount++;
                levelCount = Mathf.Max(levelCount, order.levelNumber);
            }

            if (levelCount == 0) levelCount = soloOrderCount;
        }

        public bool AllowsPickup(NetworkGrabbable cargo) =>
            (NetworkSessionState.Active == null || NetworkSessionState.Active.SessionStarted) &&
            cargo == CurrentCargo &&
            state is DeliveryOrderState.AwaitingPickup or DeliveryOrderState.InTransit or DeliveryOrderState.ReadyForDelivery;

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (!ValidateOrders())
            {
                enabled = false;
                return;
            }

            initialCargoPositions = new Vector3[orders.Length];
            initialCargoRotations = new Quaternion[orders.Length];
            for (int index = 0; index < orders.Length; index++)
            {
                NetworkGrabbable cargo = orders[index].targetCargo;
                initialCargoPositions[index] = cargo.transform.position;
                initialCargoRotations[index] = cargo.transform.rotation;
            }

            Active = this;
            RestartShift();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Active = this;
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            if (NetworkSessionState.Active != null && !NetworkSessionState.Active.SessionStarted)
            {
                return;
            }

            CheckCourierFalls();

            if (state is DeliveryOrderState.Completed or DeliveryOrderState.Failed)
            {
                if (NetworkTime.time >= transitionAtNetworkTime)
                {
                    if (state == DeliveryOrderState.Completed) AdvanceOrder();
                    else BeginCurrentOrder();
                }

                return;
            }

            if (state == DeliveryOrderState.ShiftCompleted) return;

            OrderDefinition order = CurrentOrder;
            NetworkGrabbable cargo = CurrentCargo;
            if (order == null || cargo == null || cargoBody == null)
            {
                return;
            }

            if (CurrentFragileCargo?.IsBroken == true)
            {
                FailCurrentOrder(DeliveryFailure.BrokenCargo);
                return;
            }

            if (HasTimeLimit && startedAtNetworkTime > 0d && ElapsedSeconds >= order.timeLimit)
            {
                FailCurrentOrder(DeliveryFailure.TimeExpired);
                return;
            }

            if (cargoBody.position.y < -4f)
            {
                ServerRecoverCargo();
                return;
            }

            bool canCarry = cargo.HolderCount >= cargo.RequiredHolders;
            if (state == DeliveryOrderState.AwaitingPickup && canCarry)
            {
                state = DeliveryOrderState.InTransit;
                if (startedAtNetworkTime <= 0d)
                {
                    startedAtNetworkTime = NetworkTime.time;
                }
            }

            if (!ApplyCourseRules(order.course, cargo)) return;

            if (state == DeliveryOrderState.InTransit && CanPassCheckpoint(canCarry, cargo) &&
                order.GetCheckpoint(checkpointsPassed).ContainsPoint(cargo.WorldCenter))
            {
                checkpointsPassed++;
                if (CurrentMechanic == DeliveryMechanic.Conveyor && checkpointsPassed == 1)
                    bypassStartedAt = NetworkTime.time;
                if (checkpointsPassed == order.CheckpointCount)
                {
                    state = DeliveryOrderState.ReadyForDelivery;
                }
            }

            bool insideDeliveryZone = order.deliveryZone.ContainsEntireObject(cargo.gameObject);
            if (wasHeld && !cargo.IsHeld && !insideDeliveryZone && order.course?.IsOnBelt(cargo.WorldCenter) != true)
            {
                dropCount++;
            }

            wasHeld = cargo.IsHeld;

            bool placedCorrectly =
                state == DeliveryOrderState.ReadyForDelivery &&
                !cargo.IsHeld &&
                insideDeliveryZone &&
                IsDockAligned &&
                cargoBody.linearVelocity.sqrMagnitude <= maximumLinearSpeed * maximumLinearSpeed &&
                cargoBody.angularVelocity.sqrMagnitude <= maximumAngularSpeed * maximumAngularSpeed;

            placementProgress = placedCorrectly
                ? Mathf.Min(placementDuration, placementProgress + Time.fixedDeltaTime)
                : 0f;

            if (placementProgress >= placementDuration)
            {
                CompleteCurrentOrder();
            }
        }

        [Server]
        private bool ApplyCourseRules(DeliveryCourse course, NetworkGrabbable cargo)
        {
            if (course == null) return true;
            Vector3 position = cargo.WorldCenter;
            switch (course.Mechanic)
            {
                case DeliveryMechanic.Drawbridge:
                    if (!bridgePowered && course.PlatesPressed(LobbyPlayer.ServerPlayers, includesBonusCargo))
                    {
                        bridgePowered = true;
                        bridgeStartedAt = NetworkTime.time;
                    }
                    break;
                case DeliveryMechanic.Conveyor:
                    if (!cargo.IsHeld && course.IsOnBelt(position)) course.DriveBelt(cargoBody);
                    break;
                case DeliveryMechanic.Security:
                    if (state == DeliveryOrderState.InTransit && CurrentBeamActive &&
                        NetworkTime.time >= nextBeamDamageAt &&
                        CurrentOrder.GetCheckpoint(checkpointsPassed).ContainsPoint(position))
                    {
                        nextBeamDamageAt = NetworkTime.time + 1.1d;
                        CurrentFragileCargo?.ServerApplyDamage(0.20f);
                    }

                    break;
                case DeliveryMechanic.ColdChain:
                    if (startedAtNetworkTime <= 0d) break;
                    heat = Mathf.Clamp01(heat + Time.fixedDeltaTime * (course.IsCooling(position) ? -CoolingPerSecond : HeatingPerSecond));
                    if (heat < 1f) break;
                    FailCurrentOrder(DeliveryFailure.Overheated);
                    return false;
            }

            return true;
        }

        private bool CanPassCheckpoint(bool canCarry, NetworkGrabbable cargo) => CurrentMechanic switch
        {
            DeliveryMechanic.Drawbridge => canCarry && BridgeReady,
            DeliveryMechanic.Conveyor => checkpointsPassed == 0 ? !cargo.IsHeld : canCarry,
            DeliveryMechanic.Security => canCarry && !CurrentBeamActive,
            DeliveryMechanic.ColdChain => canCarry && heat <= 0.23f,
            _ => canCarry
        };

        [Server]
        private void CheckCourierFalls()
        {
            if (NetworkTime.time < nextFallCheckAt) return;
            nextFallCheckAt = NetworkTime.time + 0.2d;
            foreach (LobbyPlayer courier in LobbyPlayer.ServerPlayers)
            {
                if (courier.transform.position.y >= -5f) continue;
                ServerRecoverCargo();
                break;
            }
        }

        [Server]
        private void FailCurrentOrder(DeliveryFailure reason)
        {
            failure = reason;
            completionTimeSeconds = ElapsedSeconds;
            state = DeliveryOrderState.Failed;
            placementProgress = 0f;
            transitionAtNetworkTime = NetworkTime.time + nextOrderDelay;
        }

        [Server]
        public void ServerRecoverCargo()
        {
            if (CurrentCargo == null || state is DeliveryOrderState.Completed or DeliveryOrderState.ShiftCompleted)
            {
                return;
            }

            BeginCurrentOrder();
        }

        [Server]
        private void CompleteCurrentOrder()
        {
            completionTimeSeconds = Mathf.Max(
                0f,
                (float)(NetworkTime.time - startedAtNetworkTime));
            completionIntegrity = CurrentFragileCargo?.IntegrityNormalized ?? 1f;
            lastGrade = CalculateGrade(
                CurrentOrderIsFragile,
                completionIntegrity,
                completionTimeSeconds,
                dropCount);
            totalScore += ScoreFor(lastGrade);
            completedOrders++;
            state = DeliveryOrderState.Completed;
            transitionAtNetworkTime = NetworkTime.time + nextOrderDelay;
            cargoBody.linearVelocity = Vector3.zero;
            cargoBody.angularVelocity = Vector3.zero;
        }

        [Server]
        private void AdvanceOrder()
        {
            ResetCargo(currentOrderIndex);
            int nextOrder = currentOrderIndex + 1;
            while (nextOrder < orders.Length && !includesBonusCargo && orders[nextOrder].coOpOnly)
                nextOrder++;

            if (nextOrder >= orders.Length)
            {
                state = DeliveryOrderState.ShiftCompleted;
                placementProgress = 0f;
                transitionAtNetworkTime = 0d;
                return;
            }

            if (!ReferenceEquals(CurrentOrder.course, orders[nextOrder].course))
                bridgePowered = false;
            currentOrderIndex = nextOrder;
            currentOrderNumber++;
            BeginCurrentOrder();
        }

        [Server]
        private void RestartShift()
        {
            includesBonusCargo = NetworkSessionState.Active?.SessionStarted == true && !NetworkSessionState.Active.IsSolo;
            bridgePowered = false;
            for (int index = 0; index < orders.Length; index++)
            {
                ResetCargo(index);
            }

            currentOrderIndex = 0;
            currentOrderNumber = 1;
            completedOrders = 0;
            totalScore = 0;
            BeginCurrentOrder();
        }

        [Server]
        public void ServerRestartShift()
        {
            if (initialCargoPositions != null)
            {
                RestartShift();
            }
        }

        [Server]
        private void BeginCurrentOrder()
        {
            NetworkGrabbable cargo = CurrentCargo;
            cargoBody = cargo.GetComponent<Rigidbody>();
            cargo.ServerResetPose(
                initialCargoPositions[currentOrderIndex],
                initialCargoRotations[currentOrderIndex]);
            CurrentFragileCargo?.ServerResetDamage();
            state = DeliveryOrderState.AwaitingPickup;
            checkpointsPassed = 0;
            failure = DeliveryFailure.None;
            heat = CurrentMechanic == DeliveryMechanic.ColdChain ? 0.29f : 0f;
            bypassStartedAt = 0d;
            nextBeamDamageAt = 0d;
            placementProgress = 0f;
            startedAtNetworkTime = 0d;
            completionTimeSeconds = 0f;
            completionIntegrity = 1f;
            dropCount = 0;
            lastGrade = DeliveryGrade.None;
            transitionAtNetworkTime = 0d;
            wasHeld = false;

            Transform spawn = CurrentOrder.courierSpawn;
            if (spawn != null && NetworkSessionState.Active?.SessionStarted == true)
            {
                foreach (LobbyPlayer courier in LobbyPlayer.ServerPlayers)
                {
                    ServerPlaceCourier(courier);
                }
            }
        }

        [Server]
        public void ServerPlaceCourier(LobbyPlayer courier)
        {
            Transform spawn = CurrentOrder?.courierSpawn;
            if (spawn == null || NetworkSessionState.Active?.SessionStarted != true)
            {
                return;
            }

            float side = courier.PlayerNumber == 1 ? -1f : 1f;
            if (courier.TryGetComponent(out NetworkFirstPersonController controller))
            {
                controller.ServerEnterLevel(spawn.position + spawn.right * (1.15f * side), spawn.rotation);
            }
        }

        [Server]
        private void ResetCargo(int index)
        {
            if (orders == null || index < 0 || index >= orders.Length)
            {
                return;
            }

            NetworkGrabbable cargo = orders[index].targetCargo;
            cargo.ServerResetPose(initialCargoPositions[index], initialCargoRotations[index]);
            cargo.GetComponent<NetworkFragileCargo>()?.ServerResetDamage();
        }

        private bool ValidateOrders()
        {
            if (orders == null || orders.Length == 0)
            {
                Debug.LogError($"{nameof(NetworkDeliveryOrder)} on '{name}' has no orders.", this);
                return false;
            }

            foreach (OrderDefinition order in orders)
            {
                if (order == null || order.targetCargo == null || order.deliveryZone == null)
                {
                    Debug.LogError(
                        $"{nameof(NetworkDeliveryOrder)} on '{name}' has an incomplete order definition.",
                        this);
                    return false;
                }

                for (int index = 0; index < order.CheckpointCount; index++)
                {
                    if (order.GetCheckpoint(index) == null)
                    {
                        Debug.LogError($"Order '{order.displayName}' has an unassigned checkpoint.", this);
                        return false;
                    }
                }

                if (order.targetCargo.GetComponent<Rigidbody>() == null)
                {
                    Debug.LogError(
                        $"Order cargo '{order.targetCargo.name}' needs a Rigidbody.",
                        order.targetCargo);
                    return false;
                }
            }

            return true;
        }

        private static DeliveryGrade CalculateGrade(
            bool fragile,
            float integrity,
            float seconds,
            int drops)
        {
            if (fragile)
            {
                if (integrity >= 0.95f && drops == 0)
                {
                    return DeliveryGrade.S;
                }

                if (integrity >= 0.8f)
                {
                    return DeliveryGrade.A;
                }

                return integrity >= 0.5f ? DeliveryGrade.B : DeliveryGrade.C;
            }

            if (drops == 0 && seconds <= 60f)
            {
                return DeliveryGrade.S;
            }

            if (drops <= 1 && seconds <= 90f)
            {
                return DeliveryGrade.A;
            }

            return drops <= 2 ? DeliveryGrade.B : DeliveryGrade.C;
        }

        private static int ScoreFor(DeliveryGrade grade)
        {
            return grade switch
            {
                DeliveryGrade.S => 1000,
                DeliveryGrade.A => 750,
                DeliveryGrade.B => 500,
                DeliveryGrade.C => 250,
                _ => 0
            };
        }

        public static string FormatTime(float seconds)
        {
            int totalSeconds = Mathf.Max(0, Mathf.FloorToInt(seconds));
            return $"{totalSeconds / 60:00}:{totalSeconds % 60:00}";
        }

        public override void OnStopClient()
        {
            if (Active == this)
            {
                Active = null;
            }

            base.OnStopClient();
        }

        public override void OnStopServer()
        {
            if (Active == this)
            {
                Active = null;
            }

            base.OnStopServer();
        }
    }
}
