using DeliveryImpossible.Interaction;
using Mirror;
using UnityEngine;

namespace DeliveryImpossible.Orders
{
    public enum DeliveryOrderState : byte
    {
        AwaitingPickup,
        InTransit,
        Completed
    }

    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkDeliveryOrder : NetworkBehaviour
    {
        [Header("Order")]
        [SerializeField] private NetworkGrabbable targetCargo;
        [SerializeField] private DeliveryZone deliveryZone;

        [Header("Completion")]
        [SerializeField, Min(0.1f)] private float placementDuration = 1.5f;
        [SerializeField, Min(0f)] private float maximumLinearSpeed = 0.35f;
        [SerializeField, Min(0f)] private float maximumAngularSpeed = 0.75f;
        [SerializeField, Min(1f)] private float restartDelay = 6f;

        [SyncVar] private DeliveryOrderState state;
        [SyncVar] private float placementProgress;
        [SyncVar] private double restartAtNetworkTime;

        private Rigidbody cargoBody;
        private Vector3 initialCargoPosition;
        private Quaternion initialCargoRotation;

        public static NetworkDeliveryOrder Active { get; private set; }

        public DeliveryOrderState State => state;
        public float PlacementNormalized => placementDuration > 0f
            ? Mathf.Clamp01(placementProgress / placementDuration)
            : 0f;
        public int RestartSecondsRemaining => state == DeliveryOrderState.Completed
            ? Mathf.Max(0, Mathf.CeilToInt((float)(restartAtNetworkTime - NetworkTime.time)))
            : 0;

        private void Awake()
        {
            if (targetCargo != null)
            {
                cargoBody = targetCargo.GetComponent<Rigidbody>();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();

            if (targetCargo == null || deliveryZone == null)
            {
                Debug.LogError(
                    $"{nameof(NetworkDeliveryOrder)} on '{name}' is missing its cargo or delivery zone reference.",
                    this);
                enabled = false;
                return;
            }

            cargoBody ??= targetCargo.GetComponent<Rigidbody>();
            if (cargoBody == null)
            {
                Debug.LogError(
                    $"Order cargo '{targetCargo.name}' needs a Rigidbody.",
                    targetCargo);
                enabled = false;
                return;
            }

            initialCargoPosition = targetCargo.transform.position;
            initialCargoRotation = targetCargo.transform.rotation;
            state = DeliveryOrderState.AwaitingPickup;
            placementProgress = 0f;
            restartAtNetworkTime = 0d;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Active = this;
        }

        [ServerCallback]
        private void FixedUpdate()
        {
            if (targetCargo == null || deliveryZone == null || cargoBody == null)
            {
                return;
            }

            if (state == DeliveryOrderState.Completed)
            {
                if (NetworkTime.time >= restartAtNetworkTime)
                {
                    ResetOrder();
                }

                return;
            }

            if (state == DeliveryOrderState.AwaitingPickup && targetCargo.IsHeld)
            {
                state = DeliveryOrderState.InTransit;
            }

            bool placedCorrectly =
                state == DeliveryOrderState.InTransit &&
                !targetCargo.IsHeld &&
                deliveryZone.ContainsEntireObject(targetCargo.gameObject) &&
                cargoBody.linearVelocity.sqrMagnitude <= maximumLinearSpeed * maximumLinearSpeed &&
                cargoBody.angularVelocity.sqrMagnitude <= maximumAngularSpeed * maximumAngularSpeed;

            placementProgress = placedCorrectly
                ? Mathf.Min(placementDuration, placementProgress + Time.fixedDeltaTime)
                : 0f;

            if (placementProgress < placementDuration)
            {
                return;
            }

            state = DeliveryOrderState.Completed;
            restartAtNetworkTime = NetworkTime.time + restartDelay;
            cargoBody.linearVelocity = Vector3.zero;
            cargoBody.angularVelocity = Vector3.zero;
        }

        [Server]
        private void ResetOrder()
        {
            targetCargo.ServerResetPose(initialCargoPosition, initialCargoRotation);
            state = DeliveryOrderState.AwaitingPickup;
            placementProgress = 0f;
            restartAtNetworkTime = 0d;
        }

        public string GetHudText()
        {
            if (state == DeliveryOrderState.Completed)
            {
                return $"ЗАКАЗ ВЫПОЛНЕН!\nНовый заказ через {RestartSecondsRemaining}";
            }

            if (PlacementNormalized > 0f)
            {
                int percent = Mathf.RoundToInt(PlacementNormalized * 100f);
                return $"Не трогайте груз: {percent}%";
            }

            return state == DeliveryOrderState.AwaitingPickup
                ? "ЗАКАЗ: заберите тяжёлый груз"
                : "ЗАКАЗ: пронесите груз через дверь\nи поставьте в зелёную зону";
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
