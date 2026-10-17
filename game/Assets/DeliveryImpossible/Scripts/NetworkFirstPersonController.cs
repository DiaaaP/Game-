using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;
using DeliveryImpossible.UI;

namespace DeliveryImpossible.Player
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkFirstPersonController : NetworkBehaviour
    {
        [Header("References")]
        [SerializeField] private Transform cameraRoot;
        [SerializeField] private Camera playerCamera;
        [SerializeField] private AudioListener audioListener;
        [SerializeField] private Renderer[] ownerHiddenRenderers;

        [Header("Movement")]
        [SerializeField, Min(0f)] private float walkSpeed = 5f;
        [SerializeField, Min(0f)] private float crouchSpeed = 2.5f;
        [SerializeField, Min(0f)] private float jumpHeight = 1.2f;
        [SerializeField] private float gravity = -24f;

        [Header("Look")]
        [SerializeField, Min(0f)] private float mouseSensitivity = 0.12f;
        [SerializeField, Range(1f, 89f)] private float maxLookAngle = 85f;

        [Header("Crouch")]
        [SerializeField, Min(0.2f)] private float standingHeight = 1.8f;
        [SerializeField, Min(0.2f)] private float crouchingHeight = 1.1f;
        [SerializeField, Min(0f)] private float crouchTransitionSpeed = 8f;
        [SerializeField] private float standingCameraHeight = 1.65f;
        [SerializeField] private float crouchingCameraHeight = 0.95f;

        private CharacterController characterController;
        private float verticalVelocity;
        private float cameraPitch;
        private bool targetCrouching;
        private bool cursorCaptured;

        [SyncVar(hook = nameof(OnNetworkCrouchingChanged))]
        private bool networkCrouching;

        private void Awake()
        {
            characterController = GetComponent<CharacterController>();
            SetLocalPresentation(false);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            targetCrouching = networkCrouching;
            SetLocalPresentation(false);
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            SetLocalPresentation(true);
            if (!NetworkLobbyUi.BlocksGameplay)
            {
                CaptureCursor();
            }
        }

        public override void OnStopLocalPlayer()
        {
            ReleaseCursor();
            SetLocalPresentation(false);
            base.OnStopLocalPlayer();
        }

        private void OnDisable()
        {
            if (isLocalPlayer)
            {
                ReleaseCursor();
            }
        }

        private void Update()
        {
            UpdateCrouchDimensions();

            if (!isLocalPlayer)
            {
                return;
            }

            if (NetworkLobbyUi.BlocksGameplay)
            {
                ReleaseCursor();
                return;
            }

            HandleCursor();
            HandleLook();
            HandleCrouchInput();
            HandleMovement();
        }

        private void HandleMovement()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            Vector2 input = Vector2.zero;
            if (keyboard.wKey.isPressed) input.y += 1f;
            if (keyboard.sKey.isPressed) input.y -= 1f;
            if (keyboard.dKey.isPressed) input.x += 1f;
            if (keyboard.aKey.isPressed) input.x -= 1f;
            input = Vector2.ClampMagnitude(input, 1f);

            Vector3 horizontalVelocity =
                (transform.forward * input.y + transform.right * input.x) *
                (targetCrouching ? crouchSpeed : walkSpeed);

            if (characterController.isGrounded && verticalVelocity < 0f)
            {
                verticalVelocity = -2f;
            }

            if (!targetCrouching && characterController.isGrounded && keyboard.spaceKey.wasPressedThisFrame)
            {
                verticalVelocity = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }

            verticalVelocity += gravity * Time.deltaTime;
            horizontalVelocity.y = verticalVelocity;
            characterController.Move(horizontalVelocity * Time.deltaTime);
        }

        private void HandleLook()
        {
            if (!cursorCaptured || Mouse.current == null || cameraRoot == null)
            {
                return;
            }

            Vector2 mouseDelta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            cameraPitch = Mathf.Clamp(cameraPitch - mouseDelta.y, -maxLookAngle, maxLookAngle);
            cameraRoot.localRotation = Quaternion.Euler(cameraPitch, 0f, 0f);
            transform.Rotate(Vector3.up, mouseDelta.x, Space.Self);
        }

        private void HandleCrouchInput()
        {
            Keyboard keyboard = Keyboard.current;
            if (keyboard == null)
            {
                return;
            }

            bool requestedCrouch = keyboard.leftCtrlKey.isPressed ||
                                   keyboard.rightCtrlKey.isPressed ||
                                   keyboard.cKey.isPressed;

            if (!requestedCrouch && targetCrouching && !HasStandingClearance())
            {
                requestedCrouch = true;
            }

            if (requestedCrouch == targetCrouching)
            {
                return;
            }

            targetCrouching = requestedCrouch;
            CmdSetCrouching(requestedCrouch);
        }

        [Command]
        private void CmdSetCrouching(bool crouching)
        {
            networkCrouching = crouching;
        }

        private void OnNetworkCrouchingChanged(bool _, bool crouching)
        {
            if (!isLocalPlayer)
            {
                targetCrouching = crouching;
            }
        }

        private void UpdateCrouchDimensions()
        {
            if (characterController == null)
            {
                return;
            }

            float targetHeight = targetCrouching ? crouchingHeight : standingHeight;
            float newHeight = Mathf.MoveTowards(
                characterController.height,
                targetHeight,
                crouchTransitionSpeed * Time.deltaTime);

            characterController.height = newHeight;
            Vector3 center = characterController.center;
            center.y = newHeight * 0.5f;
            characterController.center = center;

            if (cameraRoot != null)
            {
                float targetCameraHeight = targetCrouching ? crouchingCameraHeight : standingCameraHeight;
                Vector3 cameraPosition = cameraRoot.localPosition;
                cameraPosition.y = Mathf.MoveTowards(
                    cameraPosition.y,
                    targetCameraHeight,
                    crouchTransitionSpeed * Time.deltaTime);
                cameraRoot.localPosition = cameraPosition;
            }
        }

        private bool HasStandingClearance()
        {
            float radius = Mathf.Max(0.01f, characterController.radius - characterController.skinWidth);
            Vector3 center = new Vector3(
                characterController.center.x,
                standingHeight * 0.5f,
                characterController.center.z);
            Vector3 bottom = transform.TransformPoint(center + Vector3.down * (standingHeight * 0.5f - radius));
            Vector3 top = transform.TransformPoint(center + Vector3.up * (standingHeight * 0.5f - radius));

            Collider[] overlaps = Physics.OverlapCapsule(
                bottom,
                top,
                radius,
                ~0,
                QueryTriggerInteraction.Ignore);

            foreach (Collider overlap in overlaps)
            {
                if (!overlap.transform.IsChildOf(transform))
                {
                    return false;
                }
            }

            return true;
        }

        private void HandleCursor()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;

            if (keyboard != null && keyboard.escapeKey.wasPressedThisFrame)
            {
                ReleaseCursor();
            }
            else if (!cursorCaptured && mouse != null && mouse.leftButton.wasPressedThisFrame)
            {
                CaptureCursor();
            }
        }

        private void CaptureCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            cursorCaptured = true;
        }

        private void ReleaseCursor()
        {
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            cursorCaptured = false;
        }

        private void SetLocalPresentation(bool localPlayer)
        {
            if (playerCamera != null)
            {
                playerCamera.enabled = localPlayer;
            }

            if (audioListener != null)
            {
                audioListener.enabled = localPlayer;
            }

            if (ownerHiddenRenderers == null)
            {
                return;
            }

            foreach (Renderer ownerHiddenRenderer in ownerHiddenRenderers)
            {
                if (ownerHiddenRenderer != null)
                {
                    ownerHiddenRenderer.enabled = !localPlayer;
                }
            }
        }

        protected override void OnValidate()
        {
            base.OnValidate();
            standingHeight = Mathf.Max(standingHeight, 0.2f);
            crouchingHeight = Mathf.Clamp(crouchingHeight, 0.2f, standingHeight);
            gravity = Mathf.Min(gravity, -0.01f);
        }
    }
}
