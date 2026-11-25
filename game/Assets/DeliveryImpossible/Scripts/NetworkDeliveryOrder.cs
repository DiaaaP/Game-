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

            public Rigidbody Body { get; set; }
            public NetworkFragileCargo Fragile { get; set; }

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

        public struct CargoProgress
        {
            public DeliveryOrderState State;
            public DeliveryFailure Failure;
            public DeliveryGrade Grade;
            public int Checkpoints;
            public int Drops;
            public uint LastCourier;
            public float Heat;
            public float Placement;
            public float FinalTime;
            public float FinalIntegrity;
            public double StartedAt;
            public double RetryAt;
        }

        public readonly struct CargoView
        {
            private readonly NetworkDeliveryOrder campaign;
            private readonly int index;
            private readonly CargoProgress progress;

            internal CargoView(NetworkDeliveryOrder campaign, int index, CargoProgress progress)
            {
                this.campaign = campaign;
                this.index = index;
                this.progress = progress;
            }

            public bool IsValid => campaign != null;
            private OrderDefinition Definition => campaign.orders[index];
            public DeliveryOrderState State => campaign.shiftCompleted ? DeliveryOrderState.ShiftCompleted : progress.State;
            public DeliveryMechanic CurrentMechanic => Definition.course?.Mechanic ?? DeliveryMechanic.Standard;
            public string CurrentOrderName => Definition.displayName;
            public string CurrentLevelName => Definition.levelName;
            public string CurrentRouteHint => Definition.routeHint;
            public NetworkGrabbable ActiveCargo => Definition.targetCargo;
            public bool CurrentOrderRequiresTeam => ActiveCargo.RequiresTeam;
            public bool CurrentOrderIsFragile => Definition.Fragile != null;
            public float CurrentIntegrity => progress.State == DeliveryOrderState.Completed
                ? progress.FinalIntegrity : Definition.Fragile?.IntegrityNormalized ?? 1f;
            public int CurrentLevelNumber => campaign.CurrentLevelNumber;
            public int TotalLevels => campaign.TotalLevels;
            public int TotalOrders => campaign.TotalOrders;
            public int CompletedOrders => campaign.CompletedOrders;
            public int TotalScore => campaign.TotalScore;
            public bool CampaignWon => campaign.CampaignWon;
            public bool IncludesBonusCargo => campaign.IncludesBonusCargo;
            public int LevelCargoCount => campaign.LevelCargoCount;
            public int AcceptedThisLevel => campaign.AcceptedThisLevel;
            public bool BridgePowered => campaign.BridgePowered;
            public bool BridgeReady => campaign.BridgeReady;
            public float BridgeProgress => campaign.BridgeProgress;
            public int CheckpointsPassed => progress.Checkpoints;
            public int CheckpointCount => Definition.CheckpointCount;
            public float Heat => progress.Heat;
            public bool IsCooling => CurrentMechanic == DeliveryMechanic.ColdChain &&
                                     Definition.course.IsCooling(ActiveCargo.WorldCenter);
            public bool CurrentBeamActive => CurrentMechanic == DeliveryMechanic.Security &&
                                             CheckpointsPassed < CheckpointCount &&
                                             DeliveryCourse.IsScannerActive(CheckpointsPassed);
            public int ScannerSecondsRemaining => SecondsUntil(DeliveryCourse.ScannerTimeRemaining(CheckpointsPassed));
            public bool IsDockAligned => CurrentMechanic != DeliveryMechanic.Docking ||
                DeliveryCourse.DockAligned(ActiveCargo.transform, Definition.deliveryZone.transform);
            public float PlacementNormalized => Mathf.Clamp01(progress.Placement / campaign.placementDuration);
            public DeliveryGrade LastGrade => progress.Grade;
            public DeliveryFailure Failure => progress.Failure;
            public int DropCount => progress.Drops;
            public float ElapsedSeconds => progress.State is DeliveryOrderState.Completed or DeliveryOrderState.Failed
                ? progress.FinalTime : ElapsedSince(progress.StartedAt);
            public int TransitionSecondsRemaining => SecondsUntil(
                (progress.State == DeliveryOrderState.Failed ? progress.RetryAt : campaign.nextLevelAt) - NetworkTime.time);

            public bool TryGetWaypoint(Vector3 viewer, out Vector3 marker, out string caption)
            {
                marker = Vector3.zero;
                caption = "";
                if (ActiveCargo == null || State is DeliveryOrderState.Failed or DeliveryOrderState.Completed or DeliveryOrderState.ShiftCompleted)
                    return false;

                DeliveryCourse course = Definition.course;
                bool pickedUp = ActiveCargo.IsHeld;
                if (CurrentMechanic == DeliveryMechanic.Drawbridge && !BridgePowered && pickedUp)
                {
                    marker = course.NearestPlate(viewer).transform.position;
                    caption = "УПРАВЛЕНИЕ МОСТОМ";
                }
                else if (State == DeliveryOrderState.InTransit && CurrentMechanic == DeliveryMechanic.Conveyor &&
                         CheckpointsPassed == 0 && (pickedUp || course.IsOnBelt(ActiveCargo.WorldCenter)))
                {
                    marker = pickedUp && course.BeltLoadPoint != null
                        ? course.BeltLoadPoint.position : Definition.GetCheckpoint(0).transform.position;
                    caption = pickedUp ? "ПОГРУЗИТЕ НА ЛЕНТУ" : "ПРИЁМНИК ЛЕНТЫ";
                }
                else if (pickedUp && State == DeliveryOrderState.ReadyForDelivery)
                {
                    marker = Definition.deliveryZone.transform.position;
                    caption = CurrentMechanic == DeliveryMechanic.Docking ? "ГНЕЗДО МОДУЛЯ" : "ПРИЁМКА";
                }
                else if (pickedUp && State == DeliveryOrderState.InTransit)
                {
                    marker = Definition.GetCheckpoint(CheckpointsPassed).transform.position;
                    caption = CurrentMechanic switch
                    {
                        DeliveryMechanic.Drawbridge => CheckpointsPassed == 0 ? "КОНЕЦ МОСТА" : "ПРОВЕРКА НА ПРИЧАЛЕ",
                        DeliveryMechanic.Conveyor => "ПОСТ КОНТРОЛЯ",
                        DeliveryMechanic.Security => $"СКАНЕР {CheckpointsPassed + 1}",
                        DeliveryMechanic.ColdChain => $"ОХЛАЖДЕНИЕ {CheckpointsPassed + 1}/{CheckpointCount}",
                        DeliveryMechanic.Docking => CheckpointsPassed switch
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
                    marker = ActiveCargo.WorldCenter;
                    caption = CurrentOrderName.ToUpperInvariant();
                }

                marker.y += 0.65f;
                return true;
            }
        }

        [SyncVar] private int currentOrderIndex;
        [SyncVar] private int currentOrderNumber;
        [SyncVar] private int completedOrders;
        [SyncVar] private int totalScore;
        [SyncVar] private bool includesBonusCargo;
        [SyncVar] private bool shiftCompleted;
        [SyncVar] private bool bridgePowered;
        [SyncVar] private double bridgeStartedAt;
        [SyncVar] private double bypassStartedAt;
        [SyncVar] private double nextLevelAt;
        [SyncVar] private CargoProgress primaryProgress;
        [SyncVar] private CargoProgress bonusProgress;

        private readonly bool[] wasHeld = new bool[2];
        private readonly double[] nextBeamDamageAt = new double[2];
        private Vector3[] initialCargoPositions;
        private Quaternion[] initialCargoRotations;
        private int soloOrderCount;
        private int levelCount;
        private double nextFallCheckAt;

        public const float HeatingPerSecond = 0.067f;
        private const float CoolingPerSecond = 0.47f;
        public static NetworkDeliveryOrder Active { get; private set; }

        public DeliveryOrderState State => shiftCompleted ? DeliveryOrderState.ShiftCompleted : primaryProgress.State;
        public int TotalOrders => includesBonusCargo ? orders.Length : soloOrderCount;
        public int CurrentLevelNumber => PrimaryOrder?.levelNumber > 0 ? PrimaryOrder.levelNumber : currentOrderNumber;
        public int TotalLevels => levelCount;
        public int LevelCargoCount => HasBonusAt(currentOrderIndex) ? 2 : 1;
        public int AcceptedThisLevel => (primaryProgress.State == DeliveryOrderState.Completed ? 1 : 0) +
            (LevelCargoCount == 2 && bonusProgress.State == DeliveryOrderState.Completed ? 1 : 0);
        public bool IncludesBonusCargo => includesBonusCargo;
        public bool BridgePowered => bridgePowered;
        public float BridgeProgress => !bridgePowered ? 0f :
            Mathf.Clamp01((float)(NetworkTime.time - bridgeStartedAt) / DeliveryCourse.BridgeTravelSeconds);
        public bool BridgeReady => BridgeProgress >= 0.999f;
        public float BypassGateProgress => bypassStartedAt <= 0d ? 0f :
            Mathf.Clamp01((float)(NetworkTime.time - bypassStartedAt) / 0.85f);
        public int CompletedOrders => completedOrders;
        public int TotalScore => totalScore;
        public bool CampaignWon => shiftCompleted && completedOrders == TotalOrders;

        private OrderDefinition PrimaryOrder => orders != null && orders.Length > currentOrderIndex
            ? orders[currentOrderIndex] : null;

        private bool HasBonusAt(int index) => includesBonusCargo && orders != null && index + 1 < orders.Length &&
            orders[index + 1].coOpOnly && orders[index + 1].levelNumber == orders[index].levelNumber &&
            ReferenceEquals(orders[index + 1].course, orders[index].course);

        private static int SecondsUntil(double seconds) => Mathf.Max(0, Mathf.CeilToInt((float)seconds));

        private static float ElapsedSince(double startedAt) =>
            startedAt > 0d ? Mathf.Max(0f, (float)(NetworkTime.time - startedAt)) : 0f;

        public bool UsesCourse(DeliveryCourse course) => ReferenceEquals(PrimaryOrder?.course, course);

        public NetworkGrabbable CargoAt(int slot) => PrimaryOrder != null && slot >= 0 && slot < LevelCargoCount
            ? orders[currentOrderIndex + slot].targetCargo : null;

        private CargoProgress GetProgress(int slot) => slot == 0 ? primaryProgress : bonusProgress;

        private void SetProgress(int slot, CargoProgress progress)
        {
            if (slot == 0) primaryProgress = progress;
            else bonusProgress = progress;
        }

        private int SlotOf(NetworkGrabbable cargo)
        {
            if (cargo == null) return -1;
            if (cargo == CargoAt(0)) return 0;
            return cargo == CargoAt(1) ? 1 : -1;
        }

        public bool CanCarrySeparately(NetworkGrabbable cargo) => LevelCargoCount == 2 && SlotOf(cargo) >= 0;

        public bool AllowsPickup(NetworkGrabbable cargo)
        {
            if (shiftCompleted || NetworkSessionState.Active?.SessionStarted == false) return false;
            int slot = SlotOf(cargo);
            return slot >= 0 && GetProgress(slot).State is
                DeliveryOrderState.AwaitingPickup or DeliveryOrderState.InTransit or DeliveryOrderState.ReadyForDelivery;
        }

        private int SlotForCourier(LobbyPlayer courier, NetworkGrabbable held)
        {
            int slot = SlotOf(held);
            if (slot >= 0) return slot;
            if (LevelCargoCount == 1 || courier == null) return 0;

            uint id = courier.netId;
            bool usedPrimary = primaryProgress.LastCourier == id;
            bool usedBonus = bonusProgress.LastCourier == id;
            int preferred = usedPrimary != usedBonus ? (usedPrimary ? 0 : 1) : (courier.PlayerNumber == 2 ? 1 : 0);
            int other = 1 - preferred;
            NetworkGrabbable cargo = CargoAt(preferred);
            if (cargo.IsHeld && cargo.FirstHolderNetId != id &&
                GetProgress(other).State != DeliveryOrderState.Completed && !CargoAt(other).IsHeld)
            {
                return other;
            }
            return preferred;
        }

        public CargoView ViewForCourier(LobbyPlayer courier, NetworkGrabbable held)
        {
            if (PrimaryOrder == null) return default;
            int slot = SlotForCourier(courier, held);
            return new CargoView(this, currentOrderIndex + slot, GetProgress(slot));
        }

        public bool TryGetNearbyDockRotation(NetworkGrabbable cargo, Vector3 player, out Quaternion rotation)
        {
            rotation = Quaternion.identity;
            int slot = SlotOf(cargo);
            if (slot < 0 || GetProgress(slot).State != DeliveryOrderState.ReadyForDelivery) return false;
            OrderDefinition definition = orders[currentOrderIndex + slot];
            if (definition.course?.Mechanic != DeliveryMechanic.Docking) return false;
            Transform dock = definition.deliveryZone.transform;
            Vector3 offset = player - dock.position;
            offset.y = 0f;
            if (offset.sqrMagnitude > 25f) return false;
            rotation = dock.rotation;
            return true;
        }

        private void Awake()
        {
            if (orders == null) return;
            foreach (OrderDefinition order in orders)
            {
                if (order?.targetCargo == null) continue;
                order.Body = order.targetCargo.GetComponent<Rigidbody>();
                order.Fragile = order.targetCargo.GetComponent<NetworkFragileCargo>();
                if (order.coOpOnly) continue;
                soloOrderCount++;
                levelCount = Mathf.Max(levelCount, order.levelNumber);
            }
            if (levelCount == 0) levelCount = soloOrderCount;
        }

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
                Transform cargo = orders[index].targetCargo.transform;
                initialCargoPositions[index] = cargo.position;
                initialCargoRotations[index] = cargo.rotation;
            }
            Active = this;
            ServerRestartShift();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Active = this;
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            if (initialCargoPositions == null || shiftCompleted || NetworkSessionState.Active?.SessionStarted == false)
                return;

            CheckCourierFalls();
            if (nextLevelAt > 0d)
            {
                if (NetworkTime.time >= nextLevelAt) AdvanceLevel();
                return;
            }

            DeliveryCourse course = PrimaryOrder.course;
            if (!bridgePowered && course?.Mechanic == DeliveryMechanic.Drawbridge &&
                course.PlatesPressed(LobbyPlayer.ServerPlayers, includesBonusCargo))
            {
                bridgePowered = true;
                bridgeStartedAt = NetworkTime.time;
            }

            for (int slot = 0; slot < LevelCargoCount; slot++)
            {
                TickCargo(slot);
            }
            if (AcceptedThisLevel == LevelCargoCount) nextLevelAt = NetworkTime.time + nextOrderDelay;
        }

        [Server]
        private void TickCargo(int slot)
        {
            CargoProgress progress = GetProgress(slot);
            if (progress.State == DeliveryOrderState.Completed) return;
            double now = NetworkTime.time;
            if (progress.State == DeliveryOrderState.Failed)
            {
                if (now >= progress.RetryAt) BeginCargo(slot, respawn: true);
                return;
            }

            OrderDefinition definition = orders[currentOrderIndex + slot];
            NetworkGrabbable cargo = definition.targetCargo;
            Rigidbody body = definition.Body;
            if (body.position.y < -4f)
            {
                BeginCargo(slot, respawn: true);
                return;
            }
            if (definition.Fragile?.IsBroken == true)
            {
                FailCargo(slot, progress, DeliveryFailure.BrokenCargo);
                return;
            }
            float elapsed = ElapsedSince(progress.StartedAt);
            if (definition.timeLimit > 0f && progress.StartedAt > 0d && elapsed >= definition.timeLimit)
            {
                FailCargo(slot, progress, DeliveryFailure.TimeExpired);
                return;
            }

            bool canCarry = cargo.HolderCount >= cargo.RequiredHolders;
            if (cargo.IsHeld) progress.LastCourier = cargo.FirstHolderNetId;
            if (progress.State == DeliveryOrderState.AwaitingPickup && canCarry)
            {
                progress.State = DeliveryOrderState.InTransit;
                progress.StartedAt = now;
            }

            DeliveryCourse course = definition.course;
            DeliveryMechanic mechanic = course?.Mechanic ?? DeliveryMechanic.Standard;
            bool beamsActive = mechanic == DeliveryMechanic.Security && progress.Checkpoints < definition.CheckpointCount &&
                               DeliveryCourse.IsScannerActive(progress.Checkpoints);
            Vector3 position = cargo.WorldCenter;
            bool onBelt = mechanic == DeliveryMechanic.Conveyor && course.IsOnBelt(position);

            if (onBelt && !cargo.IsHeld) course.DriveBelt(body);
            if (mechanic == DeliveryMechanic.ColdChain && progress.StartedAt > 0d)
            {
                float rate = course.IsCooling(position) ? -CoolingPerSecond : HeatingPerSecond;
                progress.Heat = Mathf.Clamp01(progress.Heat + Time.fixedDeltaTime * rate);
                if (progress.Heat >= 1f)
                {
                    FailCargo(slot, progress, DeliveryFailure.Overheated);
                    return;
                }
            }
            if (progress.State == DeliveryOrderState.InTransit)
            {
                DeliveryCheckpoint checkpoint = definition.GetCheckpoint(progress.Checkpoints);
                if (checkpoint.ContainsPoint(position))
                {
                    if (beamsActive && now >= nextBeamDamageAt[slot])
                    {
                        nextBeamDamageAt[slot] = now + 1.1d;
                        definition.Fragile?.ServerApplyDamage(0.20f);
                    }

                    bool canCross = mechanic switch
                    {
                        DeliveryMechanic.Drawbridge => canCarry && BridgeReady,
                        DeliveryMechanic.Conveyor => progress.Checkpoints == 0 ? !cargo.IsHeld : canCarry,
                        DeliveryMechanic.Security => canCarry && !beamsActive,
                        DeliveryMechanic.ColdChain => canCarry && progress.Heat <= 0.23f,
                        _ => canCarry
                    };
                    if (canCross)
                    {
                        progress.Checkpoints++;
                        if (mechanic == DeliveryMechanic.Conveyor && progress.Checkpoints == 1 && bypassStartedAt <= 0d)
                            bypassStartedAt = now;
                        if (progress.Checkpoints == definition.CheckpointCount)
                            progress.State = DeliveryOrderState.ReadyForDelivery;
                    }
                }
            }

            bool justDropped = wasHeld[slot] && !cargo.IsHeld;
            wasHeld[slot] = cargo.IsHeld;
            bool inside = !cargo.IsHeld && (justDropped || progress.State == DeliveryOrderState.ReadyForDelivery) &&
                          definition.deliveryZone.ContainsEntireObject(cargo.gameObject);
            if (justDropped && !inside && !onBelt) progress.Drops++;

            bool placed = progress.State == DeliveryOrderState.ReadyForDelivery && inside &&
                          (mechanic != DeliveryMechanic.Docking ||
                           DeliveryCourse.DockAligned(cargo.transform, definition.deliveryZone.transform)) &&
                          body.linearVelocity.sqrMagnitude <= maximumLinearSpeed * maximumLinearSpeed &&
                          body.angularVelocity.sqrMagnitude <= maximumAngularSpeed * maximumAngularSpeed;
            progress.Placement = placed ? Mathf.Min(placementDuration, progress.Placement + Time.fixedDeltaTime) : 0f;
            if (progress.Placement >= placementDuration)
            {
                progress.FinalTime = ElapsedSince(progress.StartedAt);
                progress.FinalIntegrity = definition.Fragile?.IntegrityNormalized ?? 1f;
                progress.Grade = CalculateGrade(definition.Fragile != null, progress.FinalIntegrity, progress.FinalTime, progress.Drops);
                totalScore += ScoreFor(progress.Grade);
                completedOrders++;
                progress.State = DeliveryOrderState.Completed;
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
            SetProgress(slot, progress);
        }

        [Server]
        private void FailCargo(int slot, CargoProgress progress, DeliveryFailure reason)
        {
            progress.State = DeliveryOrderState.Failed;
            progress.Failure = reason;
            progress.FinalTime = ElapsedSince(progress.StartedAt);
            progress.Placement = 0f;
            progress.RetryAt = NetworkTime.time + nextOrderDelay;
            SetProgress(slot, progress);
        }

        [Server]
        private void CheckCourierFalls()
        {
            if (NetworkTime.time < nextFallCheckAt) return;
            nextFallCheckAt = NetworkTime.time + 0.2d;
            foreach (LobbyPlayer courier in LobbyPlayer.ServerPlayers)
            {
                if (courier.transform.position.y >= -5f) continue;
                int slot = SlotOf(courier.GetComponent<NetworkGrabber>().HeldObject);
                if (slot >= 0) BeginCargo(slot, respawn: false);
                ServerPlaceCourier(courier);
            }
        }

        [Server]
        public void ServerRecoverCargo(LobbyPlayer courier)
        {
            if (shiftCompleted || NetworkSessionState.Active?.SessionStarted == false) return;
            int slot = SlotForCourier(courier, courier.GetComponent<NetworkGrabber>().HeldObject);
            NetworkGrabbable cargo = CargoAt(slot);
            if (GetProgress(slot).State == DeliveryOrderState.Completed ||
                (cargo.IsHeld && cargo.FirstHolderNetId != courier.netId && cargo.SecondHolderNetId != courier.netId))
                return;
            BeginCargo(slot, respawn: false);
            ServerPlaceCourier(courier);
        }

        [Server]
        private void AdvanceLevel()
        {
            int count = LevelCargoCount;
            for (int slot = 0; slot < count; slot++) ResetCargo(currentOrderIndex + slot);
            int next = currentOrderIndex + count;
            while (!includesBonusCargo && next < orders.Length && orders[next].coOpOnly) next++;
            if (next >= orders.Length)
            {
                shiftCompleted = true;
                nextLevelAt = 0d;
                return;
            }
            currentOrderIndex = next;
            currentOrderNumber += count;
            BeginLevel();
        }

        [Server]
        public void ServerRestartShift()
        {
            if (initialCargoPositions == null) return;

            includesBonusCargo = NetworkSessionState.Active?.SessionStarted == true && !NetworkSessionState.Active.IsSolo;
            for (int index = 0; index < orders.Length; index++) ResetCargo(index);
            currentOrderIndex = 0;
            currentOrderNumber = 1;
            completedOrders = 0;
            totalScore = 0;
            shiftCompleted = false;
            BeginLevel();
        }

        [Server]
        private void BeginLevel()
        {
            bridgePowered = false;
            bridgeStartedAt = 0d;
            bypassStartedAt = 0d;
            nextLevelAt = 0d;
            primaryProgress = default;
            bonusProgress = default;
            for (int slot = 0; slot < LevelCargoCount; slot++) BeginCargo(slot, respawn: false);
            foreach (LobbyPlayer courier in LobbyPlayer.ServerPlayers) ServerPlaceCourier(courier);
        }

        [Server]
        private void BeginCargo(int slot, bool respawn)
        {
            int index = currentOrderIndex + slot;
            uint lastCourier = GetProgress(slot).LastCourier;
            ResetCargo(index);
            SetProgress(slot, new CargoProgress
            {
                State = DeliveryOrderState.AwaitingPickup,
                LastCourier = lastCourier,
                Heat = orders[index].course?.Mechanic == DeliveryMechanic.ColdChain ? 0.29f : 0f
            });
            wasHeld[slot] = false;
            nextBeamDamageAt[slot] = 0d;
            if (LevelCargoCount == 1 && orders[index].course?.Mechanic == DeliveryMechanic.Conveyor)
                bypassStartedAt = 0d;
            if (!respawn) return;

            foreach (LobbyPlayer courier in LobbyPlayer.ServerPlayers)
            {
                bool responsible = lastCourier != 0u ? courier.netId == lastCourier : courier.PlayerNumber == slot + 1;
                if (responsible && courier.GetComponent<NetworkGrabber>().HeldObject == null)
                    ServerPlaceCourier(courier);
            }
        }

        [Server]
        public void ServerPlaceCourier(LobbyPlayer courier)
        {
            Transform spawn = PrimaryOrder.courierSpawn;
            if (spawn == null || NetworkSessionState.Active?.SessionStarted != true) return;
            float side = courier.PlayerNumber == 1 ? -1f : 1f;
            if (courier.TryGetComponent(out NetworkFirstPersonController controller))
                controller.ServerEnterLevel(spawn.position + spawn.right * (1.15f * side), spawn.rotation);
        }

        [Server]
        private void ResetCargo(int index)
        {
            OrderDefinition order = orders[index];
            order.targetCargo.ServerResetPose(initialCargoPositions[index], initialCargoRotations[index]);
            order.Fragile?.ServerResetDamage();
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

                if (order.Body == null)
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
