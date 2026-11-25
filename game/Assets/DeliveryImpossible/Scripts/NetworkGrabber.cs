using DeliveryImpossible.Orders;
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
        [SerializeField] private GameObject interactionPanel;
        [SerializeField] private TMP_Text interactionPrompt;
        [SerializeField] private TMP_Text interactionHint;
        [SerializeField] private GameObject cooperationWarningPanel;
        [SerializeField] private TMP_Text cooperationWarningText;

        [Header("Interaction")]
        [SerializeField, Min(0.5f)] private float interactionDistance = 4f;
        [SerializeField, Min(0.5f)] private float holdDistance = 2.2f;
        [SerializeField, Min(0f)] private float sphereCastRadius = 0.08f;
        [SerializeField] private LayerMask interactionMask = ~0;

        [Header("Networking")]
        [SerializeField, Range(5f, 30f)] private float poseSendRate = 20f;

        [SyncVar(hook = nameof(OnHeldIdentityChanged))]
        private NetworkIdentity heldIdentity;
        [SyncVar] private float networkLowering;

        private NetworkGrabbable hovered;
        private Vector3 hoveredGrabPoint;
        private NetworkGrabbable serverHeld;
        private NetworkGrabbable ignoredCargo;
        private CharacterController carrierCollider;
        private Quaternion carryRotation = Quaternion.identity;
        private uint pendingGrabNetId;
        private double nextPoseSendTime;

        public NetworkGrabbable HeldObject =>
            heldIdentity != null ? heldIdentity.GetComponent<NetworkGrabbable>() : null;

        private void Awake()
        {
            carrierCollider = GetComponent<CharacterController>();
        }

        private void Update()
        {
            if (!isLocalPlayer || playerCamera == null)
            {
                return;
            }

            if (DeliveryImpossible.UI.NetworkLobbyUi.BlocksGameplay)
            {
                SetHovered(null);
                interactionPanel?.SetActive(false);
                cooperationWarningPanel?.SetActive(false);
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

                if (keyboard.qKey.wasPressedThisFrame && !held.RequiresTeam)
                {
                    NetworkDeliveryOrder order = NetworkDeliveryOrder.Active;
                    carryRotation = order != null && order.TryGetNearbyDockRotation(held, transform.position, out Quaternion dockRotation)
                        ? dockRotation
                        : Quaternion.Euler(0f, Mathf.Round(carryRotation.eulerAngles.y / 90f) * 90f + 90f, 0f);
                }

                if (Time.unscaledTimeAsDouble >= nextPoseSendTime)
                {
                    nextPoseSendTime = Time.unscaledTimeAsDouble + 1d / poseSendRate;
                    CmdUpdateHeldPose(ReadLowering(), carryRotation);
                }

                return;
            }

            UpdateHoveredObject();
            if (hovered == null || !keyboard.eKey.wasPressedThisFrame)
            {
                return;
            }

            carryRotation = Quaternion.Euler(0f, hovered.transform.eulerAngles.y, 0f);
            pendingGrabNetId = hovered.netId;
            CmdTryGrab(hovered.netId, hoveredGrabPoint, ReadLowering());
        }

        private void UpdateHoveredObject()
        {
            Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
            NetworkGrabbable candidate = null;

            if (Physics.SphereCast(
                    ray,
                    Mathf.Max(0.18f, sphereCastRadius),
                    out RaycastHit hit,
                    interactionDistance,
                    interactionMask,
                    QueryTriggerInteraction.Ignore))
            {
                candidate = hit.collider.GetComponentInParent<NetworkGrabbable>();
                if (candidate != null && !candidate.CanPickUp)
                {
                    candidate = null;
                }

                if (candidate != null)
                {
                    hoveredGrabPoint = hit.point;
                }
            }

            if (candidate == null)
            {
                NetworkDeliveryOrder order = NetworkDeliveryOrder.Active;
                float bestAim = 0.65f;
                for (int slot = 0; order != null && slot < order.LevelCargoCount; slot++)
                {
                    NetworkGrabbable nearby = order.CargoAt(slot);
                    if (nearby == null || !nearby.CanPickUp || nearby.DistanceFrom(ray.origin) > 3.3f)
                        continue;
                    Vector3 direction = nearby.WorldCenter - ray.origin;
                    float aim = Vector3.Dot(ray.direction, direction.normalized);
                    if (aim >= bestAim &&
                        Physics.Raycast(ray.origin, direction.normalized, out RaycastHit direct, direction.magnitude + 0.16f,
                            interactionMask, QueryTriggerInteraction.Ignore) &&
                        direct.collider.GetComponentInParent<NetworkGrabbable>() == nearby)
                    {
                        candidate = nearby;
                        hoveredGrabPoint = direct.point;
                        bestAim = aim;
                    }
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

            string action = string.Empty;
            string hint = string.Empty;
            string warning = string.Empty;
            NetworkGrabbable held = HeldObject;
            if (held != null)
            {
                if (held.NeedsMoreHolders)
                {
                    action = "Отпустить";
                    hint = "Чтобы опустить груз, наклоните взгляд.";
                    warning = "НУЖЕН ВТОРОЙ ИГРОК  |  ТЯЖЁЛЫЙ ГРУЗ";
                }
                else if (held.RequiresTeam)
                {
                    action = "Отпустить";
                    hint = GetCargoConditionHint(held, "НАПАРНИК ДЕРЖИТ ВТОРУЮ СТОРОНУ");
                }
                else if (held.HolderCount == 2)
                {
                    action = "Отпустить";
                    hint = "Вы держите модуль вдвоём.";
                }
                else
                {
                    action = "Отпустить";
                    hint = "Чтобы опустить груз, наклоните взгляд.";
                }
            }
            else if (hovered != null)
            {
                if (hovered.HolderCount > 0)
                {
                    action = "Помочь нести";
                    hint = "";
                }
                else if (hovered.RequiresTeam)
                {
                    action = "Взять груз";
                    hint = "";
                    warning = "НУЖЕН ВТОРОЙ ИГРОК  |  ТЯЖЁЛЫЙ ГРУЗ";
                }
                else
                {
                    action = "Взять груз";
                    hint = GetCargoConditionHint(hovered, "");
                }
            }

            interactionPrompt.text = action;
            if (interactionHint != null)
            {
                interactionHint.text = hint;
            }

            if (cooperationWarningText != null)
            {
                cooperationWarningText.text = warning;
            }

            if (interactionPanel != null)
            {
                interactionPanel.SetActive(!string.IsNullOrEmpty(action));
            }

            if (cooperationWarningPanel != null)
            {
                cooperationWarningPanel.SetActive(!string.IsNullOrEmpty(warning));
            }
        }

        private static string GetCargoConditionHint(NetworkGrabbable cargo, string fallback)
        {
            NetworkFragileCargo fragileCargo = cargo.GetComponent<NetworkFragileCargo>();
            if (fragileCargo == null)
            {
                return fallback;
            }

            return fragileCargo.IsBroken
                ? "ГРУЗ РАЗБИТ"
                : $"ЦЕЛОСТНОСТЬ: {fragileCargo.IntegrityPercent}%";
        }

        private float ReadLowering() => playerCamera != null
            ? Mathf.Clamp01(-playerCamera.transform.forward.y * 1.75f) : 0f;

        internal float VisualLowering => isLocalPlayer ? ReadLowering() : networkLowering;

        internal Vector3 CarryCenter(float lowering)
        {
            float height = Mathf.Lerp(1.10f, 0.66f, lowering);
            return transform.position + transform.forward * Mathf.Clamp(holdDistance, 1.45f, 1.85f) + Vector3.up * height;
        }

        [Command]
        private void CmdTryGrab(uint targetNetId, Vector3 worldGrabPoint, float lowering)
        {
            if (serverHeld != null ||
                !IsFinite(worldGrabPoint) ||
                !float.IsFinite(lowering) ||
                !NetworkServer.spawned.TryGetValue(targetNetId, out NetworkIdentity targetIdentity))
            {
                return;
            }

            NetworkGrabbable target = targetIdentity.GetComponent<NetworkGrabbable>();
            Vector3 interactionOrigin = transform.position + Vector3.up * 1.5f;
            if (target == null ||
                target.DistanceFrom(interactionOrigin) > interactionDistance + 0.5f ||
                target.DistanceFrom(worldGrabPoint) > 0.5f)
            {
                return;
            }

            lowering = Mathf.Clamp01(lowering);
            if (!target.ServerBeginGrab(this, worldGrabPoint, lowering))
            {
                return;
            }

            serverHeld = target;
            networkLowering = lowering;
            heldIdentity = targetIdentity;
            SetIgnoredCargo(target);
        }

        [Command(channel = Channels.Unreliable)]
        private void CmdUpdateHeldPose(float lowering, Quaternion desiredRotation)
        {
            if (serverHeld == null || !float.IsFinite(lowering) ||
                !float.IsFinite(desiredRotation.y) || !float.IsFinite(desiredRotation.w))
            {
                return;
            }

            networkLowering = Mathf.Clamp01(lowering);
            // Only the vertical placement and yaw come from the carrier.
            // The server already has the carrier's movement and builds the target from that.
            var yaw = new Quaternion(0f, desiredRotation.y, 0f, desiredRotation.w).normalized;
            serverHeld.ServerSetTargetPose(this, networkLowering, yaw);
        }

        [Command]
        private void CmdRelease()
        {
            NetworkGrabbable cargo = serverHeld;
            if (cargo == null) return;
            ServerReleaseTarget(cargo);
            if (!cargo.IsHeld) cargo.ServerDampenRelease();
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
            SetIgnoredCargo(null);
        }

        private void OnHeldIdentityChanged(NetworkIdentity _, NetworkIdentity current)
        {
            SetIgnoredCargo(current != null ? current.GetComponent<NetworkGrabbable>() : null);
            if (!isLocalPlayer || current == null)
            {
                pendingGrabNetId = 0;
                return;
            }

            if (current.netId != pendingGrabNetId)
            {
                carryRotation = Quaternion.Euler(0f, current.transform.eulerAngles.y, 0f);
            }

            pendingGrabNetId = 0;
            nextPoseSendTime = 0d;
        }

        private void SetIgnoredCargo(NetworkGrabbable cargo)
        {
            if (ignoredCargo == cargo) return;
            SetCargoCollision(ignoredCargo, false);
            ignoredCargo = cargo;
            SetCargoCollision(ignoredCargo, true);
        }

        private void SetCargoCollision(NetworkGrabbable cargo, bool ignore)
        {
            if (carrierCollider == null || cargo == null) return;
            foreach (Collider obstacle in cargo.CargoColliders)
            {
                if (obstacle != null && !obstacle.isTrigger)
                    Physics.IgnoreCollision(carrierCollider, obstacle, ignore);
            }
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
                interactionCanvas.enabled = true;
            }

            if (interactionPanel != null)
            {
                interactionPanel.SetActive(false);
            }

            if (cooperationWarningPanel != null)
            {
                cooperationWarningPanel.SetActive(false);
            }
        }

        public override void OnStopLocalPlayer()
        {
            SetHovered(null);

            if (interactionCanvas != null)
            {
                interactionCanvas.enabled = false;
            }

            if (interactionPanel != null)
            {
                interactionPanel.SetActive(false);
            }

            if (cooperationWarningPanel != null)
            {
                cooperationWarningPanel.SetActive(false);
            }

            base.OnStopLocalPlayer();
        }

        private void OnDisable()
        {
            SetHovered(null);
            SetIgnoredCargo(null);
        }
    }
}
