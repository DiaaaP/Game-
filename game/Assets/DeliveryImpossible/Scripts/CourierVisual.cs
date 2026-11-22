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
        private Vector3 lastPosition;
        private float standingHeight;
        private bool uniformAssigned;

        private void Awake()
        {
            body = GetComponentInParent<CharacterController>();
            courier = GetComponentInParent<NetworkLobbyPlayer>();
            standingHeight = body.height;
            lastPosition = body.transform.position;
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

            Vector3 position = body.transform.position;
            Vector3 displacement = position - lastPosition;
            lastPosition = position;
            displacement.y = 0f;
            float speed = displacement.magnitude / Mathf.Max(Time.deltaTime, 0.0001f);
            animator.SetFloat(Speed, Mathf.Clamp(speed, 0f, 5f), 0.12f, Time.deltaTime);
            transform.localScale = new Vector3(1f, body.height / standingHeight, 1f);
        }
    }
}
