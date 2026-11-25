using System.Collections.Generic;
using DeliveryImpossible.Networking;
using DeliveryImpossible.Orders;
using Mirror;
using UnityEngine;

namespace DeliveryImpossible.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NetworkGrabbable : NetworkBehaviour
    {
        private sealed class HolderState
        {
            public NetworkGrabber Holder;
            public Vector3 LocalAnchor;
            public float Lowering;
            public float SmoothedLowering;
            public Quaternion TargetRotation;
            public Vector3 LastCarrierPosition;
            public Vector3 CarrierVelocity;
        }

        public struct CargoPose
        {
            public Vector3 Position;
            public Quaternion Rotation;
            public double Time;
            public uint Reset;
        }

        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Holding Physics")]
        [SerializeField, Min(0f)] private float positionSpring = 42f;
        [SerializeField, Min(0f)] private float positionDamping = 11f;
        [SerializeField, Min(0f)] private float maxAcceleration = 75f;
        [SerializeField, Min(0f)] private float rotationSpring = 28f;
        [SerializeField, Min(0f)] private float rotationDamping = 8f;
        [SerializeField, Min(0f)] private float maxAngularAcceleration = 45f;
        [SerializeField, Min(0.5f)] private float breakDistance = 5.5f;
        [SerializeField, Range(1, 2)] private int maxHolders = 2;
        [SerializeField, Range(1, 2)] private int requiredHolders = 1;
        [SerializeField, Range(0.01f, 1f)] private float insufficientHolderStrength = 0.1f;

        [Header("Local Feedback")]
        [SerializeField] private bool campaignBonusCargo;
        [SerializeField] private Color highlightColor = new Color(1f, 0.72f, 0.12f, 1f);
        [SerializeField] private Renderer[] highlightedRenderers;

        [SyncVar] private byte holderCount;
        [SyncVar] private uint firstHolderNetId;
        [SyncVar] private uint secondHolderNetId;

        [SyncVar(hook = nameof(OnNetworkPoseChanged))]
        private CargoPose networkPose;

        private Rigidbody body;
        private readonly HolderState[] serverHolders = new HolderState[2];
        private readonly SortedList<double, TransformSnapshot> clientSnapshots = new();
        private RigidbodyInterpolation originalInterpolation;
        private MaterialPropertyBlock highlightBlock;
        private bool originalIsKinematic;
        private bool highlighted;
        private Quaternion teamCarryRotation;
        private Collider[] cargoColliders;
        private bool? bonusVisible;

        public bool IsHeld => holderCount > 0;
        private bool SingleCarrier => NetworkSessionState.Active?.IsSolo == true ||
                                      NetworkDeliveryOrder.Active?.CanCarrySeparately(this) == true;
        private int HolderCapacity => SingleCarrier ? 1 : maxHolders;
        public bool IsFullyHeld => holderCount >= HolderCapacity;
        public bool RequiresTeam => RequiredHolders > 1;
        public bool NeedsMoreHolders => IsHeld && holderCount < RequiredHolders;
        public bool CanPickUp => !IsFullyHeld && (NetworkDeliveryOrder.Active?.AllowsPickup(this) ?? true);
        public int HolderCount => holderCount;
        public int RequiredHolders => SingleCarrier ? 1 : requiredHolders;
        public uint FirstHolderNetId => firstHolderNetId;
        public uint SecondHolderNetId => secondHolderNetId;
        public Vector3 WorldCenter => body == null ? transform.position :
            isServer ? body.worldCenterOfMass : transform.TransformPoint(body.centerOfMass);
        internal Collider[] CargoColliders => cargoColliders;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            cargoColliders = GetComponentsInChildren<Collider>(true);
            originalIsKinematic = body.isKinematic;
            originalInterpolation = body.interpolation;

            // Scene objects exist before Host/Client is started. Keep cargo
            // frozen until the server becomes authoritative so each peer
            // cannot simulate a different initial position.
            body.isKinematic = true;

            if (highlightedRenderers == null || highlightedRenderers.Length == 0)
            {
                highlightedRenderers = GetComponentsInChildren<Renderer>(true);
            }

            if (campaignBonusCargo)
            {
                UpdateBonusVisibility();
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            body.isKinematic = originalIsKinematic;
            body.interpolation = originalInterpolation;
            if (campaignBonusCargo)
            {
                bonusVisible = null;
                UpdateBonusVisibility();
            }
            teamCarryRotation = body.rotation;
            PublishPose(reset: true);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!isServer)
            {
                body.isKinematic = true;
                body.interpolation = RigidbodyInterpolation.None;
                ApplyClientReset(networkPose);
            }
        }

        private void Update()
        {
            if (!isClient || isServer || clientSnapshots.Count == 0) return;
            SnapshotInterpolation.StepInterpolation(clientSnapshots, NetworkTime.unscaledTime,
                out TransformSnapshot from, out TransformSnapshot to, out double blend);
            TransformSnapshot pose = TransformSnapshot.Interpolate(from, to, blend);
            transform.SetPositionAndRotation(pose.position, pose.rotation);
        }

        private void LateUpdate()
        {
            if (campaignBonusCargo) UpdateBonusVisibility();
        }

        private void UpdateBonusVisibility()
        {
            bool visible = NetworkDeliveryOrder.Active?.IncludesBonusCargo == true;
            if (bonusVisible == visible) return;
            bonusVisible = visible;
            foreach (Renderer item in highlightedRenderers) item.enabled = visible;
            foreach (Collider item in cargoColliders) item.enabled = visible;
            body.isKinematic = !isServer || !visible || originalIsKinematic;
        }

        private void FixedUpdate()
        {
            if (!isServer || body == null) return;
            UpdateServerPhysics();
            PublishPose(reset: false);
        }

        [Server]
        private void UpdateServerPhysics()
        {
            if (holderCount == 0 || body.isKinematic)
            {
                return;
            }

            for (int index = serverHolders.Length - 1; index >= 0; index--)
            {
                HolderState state = serverHolders[index];
                if (state != null &&
                    (state.Holder == null ||
                     !state.Holder.isActiveAndEnabled ||
                     Vector3.Distance(state.Holder.transform.position, WorldCenter) > breakDistance))
                {
                    if (state.Holder != null)
                    {
                        state.Holder.ServerReleaseTarget(this);
                    }
                    else
                    {
                        serverHolders[index] = null;
                        RefreshHolderSyncState();
                    }
                }
            }

            Quaternion targetRotation = Quaternion.identity;
            int activeHolders = 0;
            float controlStrength = holderCount >= RequiredHolders
                ? 1f
                : insufficientHolderStrength;
            foreach (HolderState state in serverHolders)
            {
                if (state == null)
                {
                    continue;
                }

                Vector3 anchorPosition = body.position + body.rotation * Vector3.Scale(state.LocalAnchor, transform.lossyScale);
                state.SmoothedLowering = Mathf.MoveTowards(state.SmoothedLowering, state.Lowering, 8f * Time.fixedDeltaTime);
                Vector3 positionError = state.Holder.CarryCenter(state.SmoothedLowering) - anchorPosition;
                Vector3 pointVelocity = body.GetPointVelocity(anchorPosition);
                Vector3 carrierPosition = state.Holder.transform.position;
                Vector3 carrierVelocity = Vector3.ClampMagnitude(
                    (carrierPosition - state.LastCarrierPosition) / Time.fixedDeltaTime, 7f);
                state.LastCarrierPosition = carrierPosition;
                state.CarrierVelocity = Vector3.Lerp(state.CarrierVelocity, carrierVelocity, 0.45f);
                Vector3 acceleration = positionError * positionSpring + (state.CarrierVelocity - pointVelocity) * positionDamping;
                body.AddForceAtPosition(
                    Vector3.ClampMagnitude(acceleration, maxAcceleration * controlStrength),
                    anchorPosition,
                    ForceMode.Acceleration);

                targetRotation = activeHolders == 0
                    ? state.TargetRotation
                    : Quaternion.Slerp(targetRotation, state.TargetRotation, 1f / (activeHolders + 1f));
                activeHolders++;
            }

            if (activeHolders == 0)
            {
                return;
            }

            if (RequiresTeam)
            {
                targetRotation = teamCarryRotation;
            }

            Quaternion rotationError = targetRotation * Quaternion.Inverse(body.rotation);
            rotationError.ToAngleAxis(out float angleDegrees, out Vector3 axis);
            if (angleDegrees > 180f)
            {
                angleDegrees -= 360f;
            }

            if (axis.sqrMagnitude > 0.0001f && float.IsFinite(angleDegrees))
            {
                Vector3 angularAcceleration =
                    axis.normalized * (angleDegrees * Mathf.Deg2Rad * rotationSpring) -
                    body.angularVelocity * rotationDamping;
                body.AddTorque(
                    Vector3.ClampMagnitude(
                        angularAcceleration,
                        maxAngularAcceleration * controlStrength),
                    ForceMode.Acceleration);
            }
        }

        private void PublishPose(bool reset)
        {
            Vector3 position = body.position;
            Quaternion rotation = body.rotation;
            if (!reset && (position - networkPose.Position).sqrMagnitude < 0.000001f &&
                Mathf.Abs(Quaternion.Dot(rotation, networkPose.Rotation)) > 0.9999999f)
                return;
            networkPose = new CargoPose
            {
                Position = position,
                Rotation = rotation,
                Time = NetworkTime.localTime,
                Reset = networkPose.Reset + (reset ? 1u : 0u)
            };
        }

        private void OnNetworkPoseChanged(CargoPose previous, CargoPose current)
        {
            if (isServer || !isClient) return;
            if (previous.Reset != current.Reset || clientSnapshots.Count == 0)
            {
                ApplyClientReset(current);
                return;
            }

            double interval = NetworkServer.sendInterval;
            if (current.Time - previous.Time > 3d * interval)
            {
                // Seed the end of a stationary period so movement doesn't interpolate across the whole pause.
                AddSnapshot(previous, current.Time - interval);
            }
            AddSnapshot(current, current.Time);
        }

        private void ApplyClientReset(CargoPose pose)
        {
            clientSnapshots.Clear();
            body.position = pose.Position;
            body.rotation = pose.Rotation;
            AddSnapshot(pose, pose.Time);
        }

        private void AddSnapshot(CargoPose pose, double time)
        {
            SnapshotInterpolation.InsertIfNotExists(clientSnapshots, 32,
                new TransformSnapshot(time, NetworkTime.localTime, pose.Position, pose.Rotation, Vector3.one));
        }

        [Server]
        public bool ServerBeginGrab(
            NetworkGrabber holder,
            Vector3 worldGrabPoint,
            float lowering)
        {
            if (holder == null ||
                body == null ||
                body.isKinematic || !CanPickUp ||
                FindHolderIndex(holder) >= 0)
            {
                return false;
            }

            int freeIndex = FindFreeHolderIndex();
            if (freeIndex < 0 || freeIndex >= HolderCapacity)
            {
                return false;
            }

            serverHolders[freeIndex] = new HolderState
            {
                Holder = holder,
                LocalAnchor = RequiresTeam ? transform.InverseTransformPoint(worldGrabPoint) : body.centerOfMass,
                Lowering = lowering,
                SmoothedLowering = lowering,
                TargetRotation = Quaternion.Euler(0f, body.rotation.eulerAngles.y, 0f),
                LastCarrierPosition = holder.transform.position
            };
            RefreshHolderSyncState();
            body.WakeUp();
            return true;
        }

        [Server]
        public void ServerSetTargetPose(
            NetworkGrabber holder,
            float lowering,
            Quaternion desiredRotation)
        {
            int index = FindHolderIndex(holder);
            if (index < 0)
            {
                return;
            }

            serverHolders[index].Lowering = lowering;
            serverHolders[index].TargetRotation = desiredRotation;
        }

        [Server]
        public void ServerEndGrab(NetworkGrabber holder)
        {
            int index = FindHolderIndex(holder);
            if (index < 0)
            {
                return;
            }

            serverHolders[index] = null;
            RefreshHolderSyncState();
            body?.WakeUp();
        }

        [Server]
        public void ServerDampenRelease()
        {
            if (body.isKinematic || IsHeld) return;
            Vector3 velocity = body.linearVelocity;
            body.linearVelocity = new Vector3(velocity.x * 0.24f, Mathf.Min(velocity.y, 0.25f), velocity.z * 0.24f);
            body.angularVelocity *= 0.24f;
        }

        [Server]
        public void ServerResetPose(Vector3 position, Quaternion rotation)
        {
            for (int index = serverHolders.Length - 1; index >= 0; index--)
            {
                NetworkGrabber holder = serverHolders[index]?.Holder;
                if (holder != null)
                {
                    holder.ServerReleaseTarget(this);
                }
                serverHolders[index] = null;
            }

            RefreshHolderSyncState();
            body.position = position;
            body.rotation = rotation;
            teamCarryRotation = rotation;
            if (!body.isKinematic)
            {
                body.linearVelocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
                body.WakeUp();
            }
            PublishPose(reset: true);
        }

        [Server]
        private void RefreshHolderSyncState()
        {
            firstHolderNetId = GetHolderNetId(serverHolders[0]);
            secondHolderNetId = GetHolderNetId(serverHolders[1]);
            holderCount = (byte)((firstHolderNetId != 0 ? 1 : 0) +
                                 (secondHolderNetId != 0 ? 1 : 0));
        }

        private static uint GetHolderNetId(HolderState state)
        {
            return state?.Holder != null ? state.Holder.netId : 0;
        }

        [Server]
        private int FindHolderIndex(NetworkGrabber holder)
        {
            for (int index = 0; index < serverHolders.Length; index++)
            {
                if (serverHolders[index]?.Holder == holder)
                {
                    return index;
                }
            }

            return -1;
        }

        [Server]
        private int FindFreeHolderIndex()
        {
            for (int index = 0; index < serverHolders.Length; index++)
            {
                if (serverHolders[index] == null)
                {
                    return index;
                }
            }

            return -1;
        }

        public float DistanceFrom(Vector3 point)
        {
            Collider[] colliders = GetComponentsInChildren<Collider>();
            float closestDistance = float.PositiveInfinity;

            foreach (Collider candidate in colliders)
            {
                if (candidate == null || !candidate.enabled || candidate.isTrigger)
                {
                    continue;
                }

                float distance = Vector3.Distance(point, candidate.ClosestPoint(point));
                closestDistance = Mathf.Min(closestDistance, distance);
            }

            return float.IsPositiveInfinity(closestDistance)
                ? Vector3.Distance(point, transform.position)
                : closestDistance;
        }

        public void SetHighlighted(bool value)
        {
            if (highlighted == value)
            {
                return;
            }

            highlighted = value;
            highlightBlock ??= new MaterialPropertyBlock();

            foreach (Renderer candidate in highlightedRenderers)
            {
                if (candidate == null)
                {
                    continue;
                }

                if (value)
                {
                    candidate.GetPropertyBlock(highlightBlock);
                    highlightBlock.SetColor(BaseColorId, highlightColor);
                    highlightBlock.SetColor(ColorId, highlightColor);
                    candidate.SetPropertyBlock(highlightBlock);
                }
                else
                {
                    candidate.SetPropertyBlock(null);
                }
            }
        }

        private void OnDisable()
        {
            if (highlighted)
            {
                SetHighlighted(false);
            }
        }

        public override void OnStopServer()
        {
            for (int index = serverHolders.Length - 1; index >= 0; index--)
            {
                NetworkGrabber holder = serverHolders[index]?.Holder;
                if (holder != null)
                {
                    holder.ServerReleaseTarget(this);
                }
                else
                {
                    serverHolders[index] = null;
                }
            }

            RefreshHolderSyncState();

            if (body != null)
            {
                body.isKinematic = true;
            }

            base.OnStopServer();
        }

        public override void OnStopClient()
        {
            clientSnapshots.Clear();
            if (!isServer && body != null)
            {
                body.isKinematic = true;
            }

            base.OnStopClient();
        }
    }
}
