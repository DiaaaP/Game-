using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryImpossible.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkGrabber : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Camera playerCamera;
        [SerializeField] private Canvas interactionCanvas;
        [SerializeField] private TMP_Text interactionPrompt;

        [Header("Interaction")]
        [SerializeField, Min(0.5f)] private float interactionDistance = 4f;
        [SerializeField, Min(0.5f)] private float holdDistance = 2.2f;
        [SerializeField, Min(0f)] private float sphereCastRadius = 0.08f;
        [SerializeField] private LayerMask interactionMask = ~0;

        [Header("Networking")]
        [SerializeField, Range(5f, 30f)] private float poseSendRate = 20f;
        [SerializeField, Min(1f)] private float maxTargetDistanceFromPlayer = 4.5f;

        [SyncVar(hook = nameof(OnHeldIdentityChanged))]
        private NetworkIdentity heldIdentity;

        private NetworkGrabbable hovered;
        private Vector3 hoveredGrabPoint;
        private NetworkGrabbable serverHeld;
        private Quaternion heldRotationOffset = Quaternion.identity;
        private uint pendingGrabNetId;
        private double nextPoseSendTime;

        public NetworkGrabbable HeldObject =>
            heldIdentity != null ? heldIdentity.GetComponent<NetworkGrabbable>() : null;

        private void Update()
        {
            if (!isLocalPlayer || playerCamera == null)
            {
                return;
            }

            UpdateInteractionPrompt();

            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            NetworkGrabbable held = HeldObject;
            if (held != null)
            {
                SetHovered(null);

                if (keyboard.eKey.wasPressedThisFrame)
                {
                    CmdRelease();
                    return;
                }

                if (Time.unscaledTimeAsDouble >= nextPoseSendTime)
                {
                    nextPoseSendTime = Time.unscaledTimeAsDouble + 1d / poseSendRate;
                    GetDesiredPose(out Vector3 targetPosition, out Quaternion targetRotation);
                    CmdUpdateHeldPose(targetPosition, targetRotation);
                }

                return;
            }

            UpdateHoveredObject();
            if (hovered == null || !keyboard.eKey.wasPressedThisFrame)
            {
                return;
            }

            heldRotationOffset = Quaternion.Inverse(playerCamera.transform.rotation) * hovered.transform.rotation;
            pendingGrabNetId = hovered.netId;
            GetDesiredPose(out Vector3 desiredPosition, out Quaternion desiredRotation);
            CmdTryGrab(hovered.netId, hoveredGrabPoint, desiredPosition, desiredRotation);
        }

        private void UpdateHoveredObject()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            NetworkGrabbable candidate = null;

            if (Physics.SphereCast(
                    ray,
                    sphereCastRadius,
                    out RaycastHit hit,
                    interactionDistance,
                    interactionMask,
                    QueryTriggerInteraction.Ignore))
            {
                candidate = hit.collider.GetComponentInParent<NetworkGrabbable>();
                if (candidate != null && candidate.IsFullyHeld)
                {
                    candidate = null;
                }

                if (candidate != null)
                {
                    hoveredGrabPoint = hit.point;
                }
            }

            SetHovered(candidate);
        }

        private void SetHovered(NetworkGrabbable candidate)
        {
            if (hovered == candidate)
            {
                return;
            }

            hovered?.SetHighlighted(false);
            hovered = candidate;
            hovered?.SetHighlighted(true);
        }

        private void UpdateInteractionPrompt()
        {
            if (interactionCanvas == null || interactionPrompt == null)
            {
                return;
            }

            string message = string.Empty;
            NetworkGrabbable held = HeldObject;
            if (held != null)
            {
                if (held.NeedsMoreHolders)
                {
                    message = "Нужен второй игрок\nE — отпустить";
                }
                else if (held.RequiresTeam)
                {
                    message = "Несём вдвоём\nE — отпустить";
                }
                else
                {
                    message = "E — отпустить";
                }
            }
            else if (hovered != null)
            {
                if (hovered.RequiresTeam && hovered.HolderCount > 0)
                {
                    message = "E — помочь нести";
                }
                else if (hovered.RequiresTeam)
                {
                    message = "E — схватить\nТяжёлый груз: нужен второй игрок";
                }
                else
                {
                    message = "E — взять";
                }
            }

            interactionPrompt.text = message;
            interactionCanvas.enabled = !string.IsNullOrEmpty(message);
        }

        private void GetDesiredPose(out Vector3 position, out Quaternion rotation)
        {
            Transform cameraTransform = playerCamera.transform;
            position = cameraTransform.position + cameraTransform.forward * holdDistance;
            rotation = cameraTransform.rotation * heldRotationOffset;
        }

        [Command]
        private void CmdTryGrab(
            uint targetNetId,
            Vector3 worldGrabPoint,
            Vector3 desiredPosition,
            Quaternion desiredRotation)
        {
            if (serverHeld != null ||
                !IsFinite(worldGrabPoint) ||
                !IsFinite(desiredPosition, desiredRotation) ||
                !NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            {
                return;
            }

            NetworkGrabbable target = targetIdentity.GetComponent<NetworkGrabbable>();
            Vector3 interactionOrigin = transform.position + Vector3.up * 1.5f;
            if (target == null ||
                target.DistanceFrom(interactionOrigin) > interactionDistance + 0.5f ||
                target.DistanceFrom(worldGrabPoint) > 0.5f ||
                Vector3.Distance(transform.position, desiredPosition) > maxTargetDistanceFromPlayer)
            {
                return;
            }

            if (!target.ServerBeginGrab(this, worldGrabPoint, desiredPosition, desiredRotation))
            {
                return;
            }

            serverHeld = target;
            heldIdentity = targetIdentity;
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdUpdateHeldPose(Vector3 desiredPosition, Quaternion desiredRotation)
        {
            if (serverHeld == null ||
                !IsFinite(desiredPosition, desiredRotation) ||
                Vector3.Distance(transform.position, desiredPosition) > maxTargetDistanceFromPlayer)
            {
                return;
            }

            serverHeld.ServerSetTargetPose(this, desiredPosition, desiredRotation);
        }

        [Command]
        private void CmdRelease()
        {
            ServerReleaseTarget(serverHeld);
        }

        [Server]
        public void ServerReleaseTarget(NetworkGrabbable expectedTarget)
        {
            if (serverHeld == null || (expectedTarget != null && serverHeld != expectedTarget))
            {
                return;
            }

            NetworkGrabbable released = serverHeld;
            serverHeld = null;
            heldIdentity = null;
            released.ServerEndGrab(this);
        }

        private void OnHeldIdentityChanged(NetworkIdentity _, NetworkIdentity current)
        {
            if (!isLocalPlayer || current == null)
            {
                pendingGrabNetId = 0;
                return;
            }

            if (current.netId != pendingGrabNetId)
            {
                heldRotationOffset = Quaternion.Inverse(playerCamera.transform.rotation) * current.transform.rotation;
            }

            pendingGrabNetId = 0;
            nextPoseSendTime = 0d;
        }

        private static bool IsFinite(Vector3 position, Quaternion rotation)
        {
            return float.IsFinite(position.x) &&
                   float.IsFinite(position.y) &&
                   float.IsFinite(position.z) &&
                   float.IsFinite(rotation.x) &&
                   float.IsFinite(rotation.y) &&
                   float.IsFinite(rotation.z) &&
                   float.IsFinite(rotation.w);
        }

        private static bool IsFinite(Vector3 position)
        {
            return float.IsFinite(position.x) &&
                   float.IsFinite(position.y) &&
                   float.IsFinite(position.z);
        }

        public override void OnStopServer()
        {
            if (serverHeld != null)
            {
                ServerReleaseTarget(serverHeld);
            }

            base.OnStopServer();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();

            if (interactionCanvas != null)
            {
                interactionCanvas.enabled = false;
            }
        }

        public override void OnStopLocalPlayer()
        {
            SetHovered(null);

            if (interactionCanvas != null)
            {
                interactionCanvas.enabled = false;
            }

            base.OnStopLocalPlayer();
        }

        private void OnDisable()
        {
            SetHovered(null);
        }
    }
}
