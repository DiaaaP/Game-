using System;
using DeliveryImpossible.Interaction;
using Mirror;
using UnityEngine;

namespace DeliveryImpossible.Orders
{
    public enum DeliveryOrderState : byte
    {
        AwaitingPickup,
        InTransit,
        ReadyForDelivery,
        Completed,
        ShiftCompleted
    }

    public enum DeliveryGrade : byte
    {
        None,
        S,
        A,
        B,
        C
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkDeliveryOrder : NetworkBehaviour
    {
        [Serializable]
        private sealed class OrderDefinition
        {
            public string displayName = "ГРУЗ";
            public NetworkGrabbable targetCargo;
            public DeliveryCheckpoint doorwayCheckpoint;
            public DeliveryZone deliveryZone;
        }

        [Header("Orders")]
        [SerializeField] private OrderDefinition[] orders;

        [Header("Completion")]
        [SerializeField, Min(0.1f)] private float placementDuration = 1.5f;
        [SerializeField, Min(0f)] private float maximumLinearSpeed = 0.35f;
        [SerializeField, Min(0f)] private float maximumAngularSpeed = 0.75f;
        [SerializeField, Min(1f)] private float nextOrderDelay = 6f;
        [SerializeField, Min(1f)] private float shiftSummaryDuration = 10f;

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

        private Rigidbody cargoBody;
        private Vector3[] initialCargoPositions;
        private Quaternion[] initialCargoRotations;
        private bool wasHeld;

        public static NetworkDeliveryOrder Active { get; private set; }

        public DeliveryOrderState State => state;
        public int CurrentOrderNumber => orders == null || orders.Length == 0
            ? 0
            : Mathf.Clamp(currentOrderIndex + 1, 1, orders.Length);
        public int TotalOrders => orders?.Length ?? 0;
        public int CompletedOrders => completedOrders;
        public int TotalScore => totalScore;
        public DeliveryGrade LastGrade => lastGrade;
        public string CurrentOrderName => CurrentOrder?.displayName ?? "ГРУЗ";
        public bool CurrentOrderIsFragile => CurrentFragileCargo != null;
        public float CurrentIntegrity => state == DeliveryOrderState.Completed
            ? completionIntegrity
            : CurrentFragileCargo?.IntegrityNormalized ?? 1f;
        public float PlacementNormalized => placementDuration > 0f
            ? Mathf.Clamp01(placementProgress / placementDuration)
            : 0f;
        public float ElapsedSeconds => state == DeliveryOrderState.Completed
            ? completionTimeSeconds
            : startedAtNetworkTime > 0d
                ? Mathf.Max(0f, (float)(NetworkTime.time - startedAtNetworkTime))
                : 0f;
        public int DropCount => dropCount;
        public int TransitionSecondsRemaining =>
            state is DeliveryOrderState.Completed or DeliveryOrderState.ShiftCompleted
                ? Mathf.Max(0, Mathf.CeilToInt((float)(transitionAtNetworkTime - NetworkTime.time)))
                : 0;

        private OrderDefinition CurrentOrder =>
            orders != null && currentOrderIndex >= 0 && currentOrderIndex < orders.Length
                ? orders[currentOrderIndex]
                : null;
        private NetworkGrabbable CurrentCargo => CurrentOrder?.targetCargo;
        private NetworkFragileCargo CurrentFragileCargo =>
            CurrentCargo != null ? CurrentCargo.GetComponent<NetworkFragileCargo>() : null;

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
            if (state == DeliveryOrderState.Completed)
            {
                if (NetworkTime.time >= transitionAtNetworkTime)
                {
                    AdvanceOrder();
                }

                return;
            }

            if (state == DeliveryOrderState.ShiftCompleted)
            {
                if (NetworkTime.time >= transitionAtNetworkTime)
                {
                    RestartShift();
                }

                return;
            }

            OrderDefinition order = CurrentOrder;
            NetworkGrabbable cargo = CurrentCargo;
            if (order == null || cargo == null || cargoBody == null)
            {
                return;
            }

            if (state == DeliveryOrderState.AwaitingPickup && cargo.IsHeld)
            {
                state = DeliveryOrderState.InTransit;
                startedAtNetworkTime = NetworkTime.time;
            }

            if (state == DeliveryOrderState.InTransit &&
                order.doorwayCheckpoint.ContainsPoint(cargo.WorldCenter))
            {
                state = DeliveryOrderState.ReadyForDelivery;
            }

            bool insideDeliveryZone = order.deliveryZone.ContainsEntireObject(cargo.gameObject);
            if (wasHeld && !cargo.IsHeld && !insideDeliveryZone)
            {
                dropCount++;
            }

            wasHeld = cargo.IsHeld;

            bool placedCorrectly =
                state == DeliveryOrderState.ReadyForDelivery &&
                !cargo.IsHeld &&
                insideDeliveryZone &&
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
            completedOrders = Mathf.Max(completedOrders, currentOrderIndex + 1);
            state = DeliveryOrderState.Completed;
            transitionAtNetworkTime = NetworkTime.time + nextOrderDelay;
            cargoBody.linearVelocity = Vector3.zero;
            cargoBody.angularVelocity = Vector3.zero;
        }

        [Server]
        private void AdvanceOrder()
        {
            ResetCargo(currentOrderIndex);
            if (currentOrderIndex + 1 >= orders.Length)
            {
                state = DeliveryOrderState.ShiftCompleted;
                placementProgress = 0f;
                transitionAtNetworkTime = NetworkTime.time + shiftSummaryDuration;
                return;
            }

            currentOrderIndex++;
            BeginCurrentOrder();
        }

        [Server]
        private void RestartShift()
        {
            for (int index = 0; index < orders.Length; index++)
            {
                ResetCargo(index);
            }

            currentOrderIndex = 0;
            completedOrders = 0;
            totalScore = 0;
            BeginCurrentOrder();
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
            placementProgress = 0f;
            startedAtNetworkTime = 0d;
            completionTimeSeconds = 0f;
            completionIntegrity = 1f;
            dropCount = 0;
            lastGrade = DeliveryGrade.None;
            transitionAtNetworkTime = 0d;
            wasHeld = false;
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
                if (order == null || order.targetCargo == null ||
                    order.doorwayCheckpoint == null || order.deliveryZone == null)
                {
                    Debug.LogError(
                        $"{nameof(NetworkDeliveryOrder)} on '{name}' has an incomplete order definition.",
                        this);
                    return false;
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
    }
}
