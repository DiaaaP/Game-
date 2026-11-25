using DeliveryImpossible.Interaction;
using DeliveryImpossible.Networking;
using UnityEngine;

namespace DeliveryImpossible.Player
{
    [DisallowMultipleComponent]
    public sealed class CourierVisual : MonoBehaviour
    {
        [SerializeField] private Animator animator;
        [SerializeField] private Renderer[] uniformRenderers;
        [SerializeField] private Material secondCourierUniform;

        private static readonly int Speed = Animator.StringToHash("Speed");
        private CharacterController body;
        private NetworkLobbyPlayer courier;
        private NetworkFirstPersonController movement;
        private NetworkGrabber grabber;
        private Transform leftArm;
        private Transform rightArm;
        private Renderer leftArmRenderer;
        private Renderer rightArmRenderer;
        private Transform animatedRoot;
        private float neutralRootHeight;
        private float carryWeight;
        private float displayedLowering;
        private float standingHeight;
        private bool uniformAssigned;

        private void Awake()
        {
            body = GetComponentInParent<CharacterController>();
            courier = GetComponentInParent<NetworkLobbyPlayer>();
            movement = body.GetComponent<NetworkFirstPersonController>();
            grabber = body.GetComponent<NetworkGrabber>();
            standingHeight = body.height;
            animatedRoot = animator.transform.Find("root");
            if (animatedRoot != null) neutralRootHeight = animatedRoot.localPosition.y;
            foreach (Renderer renderer in uniformRenderers)
            {
                if (renderer.name == "arm-left")
                {
                    leftArm = renderer.transform;
                    leftArmRenderer = renderer;
                }
                else if (renderer.name == "arm-right")
                {
                    rightArm = renderer.transform;
                    rightArmRenderer = renderer;
                }
            }
        }

        private void Update()
        {
            animator.SetFloat(Speed, movement.IsWalking ? 1f : 0f);
        }

        private void LateUpdate()
        {
            if (!uniformAssigned && courier.PlayerNumber > 0)
            {
                if (courier.PlayerNumber == 2)
                {
                    foreach (Renderer renderer in uniformRenderers)
                    {
                        renderer.sharedMaterial = secondCourierUniform;
                    }
                }

                uniformAssigned = true;
            }

            transform.localScale = new Vector3(1f, body.height / standingHeight, 1f);
            NetworkGrabbable cargo = grabber.HeldObject;
            carryWeight = Mathf.MoveTowards(carryWeight, cargo != null ? 1f : 0f, 12f * Time.deltaTime);
            if (courier.isLocalPlayer && leftArmRenderer != null && rightArmRenderer != null)
            {
                leftArmRenderer.enabled = carryWeight > 0f;
                rightArmRenderer.enabled = carryWeight > 0f;
            }
            if (carryWeight <= 0f) return;

            if (animatedRoot != null)
            {
                Vector3 position = animatedRoot.localPosition;
                position.y = Mathf.Lerp(position.y, neutralRootHeight, carryWeight);
                animatedRoot.localPosition = position;
            }
            if (cargo != null)
            {
                float blend = 1f - Mathf.Exp(-17f * Time.deltaTime);
                displayedLowering = Mathf.Lerp(displayedLowering, grabber.VisualLowering, blend);
            }
            Vector3 grip = grabber.CarryCenter(displayedLowering) - transform.forward * 0.5f;
            AimArm(leftArm, grip - transform.right * 0.35f);
            AimArm(rightArm, grip + transform.right * 0.35f);
        }

        private void AimArm(Transform arm, Vector3 target)
        {
            if (arm == null) return;
            Vector3 direction = arm.parent.InverseTransformVector(target - arm.position);
            direction.z = Mathf.Max(direction.z, 0.35f);
            Quaternion pose = Quaternion.FromToRotation(Vector3.down, direction.normalized);
            arm.localRotation = Quaternion.Slerp(arm.localRotation, pose, carryWeight);
        }
    }
}
