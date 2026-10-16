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
            public Vector3 TargetPosition;
            public Quaternion TargetRotation;
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
        [SerializeField] private Color highlightColor = new Color(1f, 0.72f, 0.12f, 1f);
        [SerializeField] private Renderer[] highlightedRenderers;

        [Header("Network Smoothing")]
        [SerializeField, Min(0f)] private float clientPositionLerp = 22f;
        [SerializeField, Min(0f)] private float clientRotationLerp = 18f;

        [SyncVar] private byte holderCount;
        [SyncVar] private uint firstHolderNetId;
        [SyncVar] private uint secondHolderNetId;

        [SyncVar(hook = nameof(OnNetworkPositionChanged))]
        private Vector3 networkPosition;

        [SyncVar(hook = nameof(OnNetworkRotationChanged))]
        private Quaternion networkRotation = Quaternion.identity;

        private Rigidbody body;
        private readonly HolderState[] serverHolders = new HolderState[2];
        private Vector3 clientTargetPosition;
        private Quaternion clientTargetRotation;
        private MaterialPropertyBlock highlightBlock;
        private bool originalIsKinematic;
        private bool highlighted;

        public bool IsHeld => holderCount > 0;
        public bool IsFullyHeld => holderCount >= maxHolders;
        public bool RequiresTeam => requiredHolders > 1;
        public bool NeedsMoreHolders => holderCount > 0 && holderCount < requiredHolders;
        public int HolderCount => holderCount;
        public int RequiredHolders => requiredHolders;
        public uint FirstHolderNetId => firstHolderNetId;
        public uint SecondHolderNetId => secondHolderNetId;
        public Vector3 WorldCenter => body != null ? body.worldCenterOfMass : transform.position;

        private void Awake()
        {
            body = GetComponent<Rigidbody>();
            originalIsKinematic = body.isKinematic;

            // Scene objects exist before Host/Client is started. Keep cargo
            // frozen until the server becomes authoritative so each peer
            // cannot simulate a different initial position.
            body.isKinematic = true;

            if (highlightedRenderers == null || highlightedRenderers.Length == 0)
            {
                highlightedRenderers = GetComponentsInChildren<Renderer>(true);
            }
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            body.isKinematic = originalIsKinematic;
            networkPosition = body.position;
            networkRotation = body.rotation;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            clientTargetPosition = networkPosition;
            clientTargetRotation = networkRotation;

            if (!isServer)
            {
                body.isKinematic = true;
                body.position = networkPosition;
                body.rotation = networkRotation;
            }
        }

        private void FixedUpdate()
        {
            if (body == null)
            {
                return;
            }

            if (isServer)
            {
                UpdateServerPhysics();
                networkPosition = body.position;
                networkRotation = body.rotation;
                return;
            }

            if (isClient)
            {
                float positionT = 1f - Mathf.Exp(-clientPositionLerp * Time.fixedDeltaTime);
                float rotationT = 1f - Mathf.Exp(-clientRotationLerp * Time.fixedDeltaTime);
                body.MovePosition(Vector3.Lerp(body.position, clientTargetPosition, positionT));
                body.MoveRotation(Quaternion.Slerp(body.rotation, clientTargetRotation, rotationT));
            }
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
            float controlStrength = holderCount >= requiredHolders
                ? 1f
                : insufficientHolderStrength;
            foreach (HolderState state in serverHolders)
            {
                if (state == null)
                {
                    continue;
                }

                Vector3 anchorPosition = transform.TransformPoint(state.LocalAnchor);
                Vector3 positionError = state.TargetPosition - anchorPosition;
                Vector3 pointVelocity = body.GetPointVelocity(anchorPosition);
                Vector3 acceleration = positionError * positionSpring - pointVelocity * positionDamping;
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

        private void OnNetworkPositionChanged(Vector3 _, Vector3 current)
        {
            clientTargetPosition = current;
        }

        private void OnNetworkRotationChanged(Quaternion _, Quaternion current)
        {
            clientTargetRotation = current;
        }

        [Server]
        public bool ServerBeginGrab(
            NetworkGrabber holder,
            Vector3 worldGrabPoint,
            Vector3 desiredPosition,
            Quaternion desiredRotation)
        {
            if (holder == null ||
                body == null ||
                body.isKinematic ||
                FindHolderIndex(holder) >= 0)
            {
                return false;
            }

            int freeIndex = FindFreeHolderIndex();
            if (freeIndex < 0 || freeIndex >= maxHolders)
            {
                return false;
            }

            serverHolders[freeIndex] = new HolderState
            {
                Holder = holder,
                LocalAnchor = transform.InverseTransformPoint(worldGrabPoint),
                TargetPosition = desiredPosition,
                TargetRotation = desiredRotation
            };
            RefreshHolderSyncState();
            body.WakeUp();
            return true;
        }

        [Server]
        public void ServerSetTargetPose(
            NetworkGrabber holder,
            Vector3 desiredPosition,
            Quaternion desiredRotation)
        {
            int index = FindHolderIndex(holder);
            if (index < 0)
            {
                return;
            }

            serverHolders[index].TargetPosition = desiredPosition;
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
        public void ServerResetPose(Vector3 position, Quaternion rotation)
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
            body.position = position;
            body.rotation = rotation;
            body.linearVelocity = Vector3.zero;
            body.angularVelocity = Vector3.zero;
            networkPosition = position;
            networkRotation = rotation;
            body.WakeUp();
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
            if (!isServer && body != null)
            {
                body.isKinematic = true;
            }

            base.OnStopClient();
        }
    }
}
