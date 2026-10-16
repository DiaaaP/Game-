using Mirror;
using UnityEngine;

namespace DeliveryImpossible.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(Rigidbody))]
    public sealed class NetworkGrabbable : NetworkBehaviour
    {
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

        [Header("Local Feedback")]
        [SerializeField] private Color highlightColor = new Color(1f, 0.72f, 0.12f, 1f);
        [SerializeField] private Renderer[] highlightedRenderers;

        [Header("Network Smoothing")]
        [SerializeField, Min(0f)] private float clientPositionLerp = 22f;
        [SerializeField, Min(0f)] private float clientRotationLerp = 18f;

        [SyncVar] private uint holderNetId;

        [SyncVar(hook = nameof(OnNetworkPositionChanged))]
        private Vector3 networkPosition;

        [SyncVar(hook = nameof(OnNetworkRotationChanged))]
        private Quaternion networkRotation = Quaternion.identity;

        private Rigidbody body;
        private NetworkGrabber serverHolder;
        private Vector3 targetPosition;
        private Quaternion targetRotation;
        private Vector3 clientTargetPosition;
        private Quaternion clientTargetRotation;
        private MaterialPropertyBlock highlightBlock;
        private bool originalIsKinematic;
        private bool highlighted;

        public bool IsHeld => holderNetId != 0;
        public uint HolderNetId => holderNetId;
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
            if (serverHolder == null || body.isKinematic)
            {
                return;
            }

            if (!serverHolder.isActiveAndEnabled ||
                Vector3.Distance(serverHolder.transform.position, WorldCenter) > breakDistance)
            {
                serverHolder.ServerReleaseTarget(this);
                return;
            }

            Vector3 positionError = targetPosition - WorldCenter;
            Vector3 acceleration = positionError * positionSpring - body.linearVelocity * positionDamping;
            body.AddForce(Vector3.ClampMagnitude(acceleration, maxAcceleration), ForceMode.Acceleration);

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
                    Vector3.ClampMagnitude(angularAcceleration, maxAngularAcceleration),
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
            Vector3 desiredPosition,
            Quaternion desiredRotation)
        {
            if (holder == null || serverHolder != null || body == null || body.isKinematic)
            {
                return false;
            }

            serverHolder = holder;
            holderNetId = holder.netId;
            targetPosition = desiredPosition;
            targetRotation = desiredRotation;
            body.WakeUp();
            return true;
        }

        [Server]
        public void ServerSetTargetPose(Vector3 desiredPosition, Quaternion desiredRotation)
        {
            if (serverHolder == null)
            {
                return;
            }

            targetPosition = desiredPosition;
            targetRotation = desiredRotation;
        }

        [Server]
        public void ServerEndGrab(NetworkGrabber holder)
        {
            if (holder != serverHolder)
            {
                return;
            }

            serverHolder = null;
            holderNetId = 0;
            body?.WakeUp();
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
            if (serverHolder != null)
            {
                serverHolder.ServerReleaseTarget(this);
            }

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
