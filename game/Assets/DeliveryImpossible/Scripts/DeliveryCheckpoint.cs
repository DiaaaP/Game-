using UnityEngine;

namespace DeliveryImpossible.Orders
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DeliveryCheckpoint : MonoBehaviour
    {
        [SerializeField] private BoxCollider checkpointCollider;

        private void Awake()
        {
            if (checkpointCollider == null)
            {
                checkpointCollider = GetComponent<BoxCollider>();
            }
        }

        public bool ContainsPoint(Vector3 worldPoint)
        {
            if (checkpointCollider == null)
            {
                return false;
            }

            Vector3 localPoint = checkpointCollider.transform.InverseTransformPoint(worldPoint);
            Vector3 minimum = checkpointCollider.center - checkpointCollider.size * 0.5f;
            Vector3 maximum = checkpointCollider.center + checkpointCollider.size * 0.5f;

            return localPoint.x >= minimum.x && localPoint.x <= maximum.x &&
                   localPoint.y >= minimum.y && localPoint.y <= maximum.y &&
                   localPoint.z >= minimum.z && localPoint.z <= maximum.z;
        }
    }
}
