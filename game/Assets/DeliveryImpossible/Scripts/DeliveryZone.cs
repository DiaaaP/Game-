using UnityEngine;

namespace DeliveryImpossible.Orders
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(BoxCollider))]
    public sealed class DeliveryZone : MonoBehaviour
    {
        [SerializeField] private BoxCollider zoneCollider;

        private void Awake()
        {
            if (zoneCollider == null)
            {
                zoneCollider = GetComponent<BoxCollider>();
            }
        }

        public bool ContainsEntireObject(GameObject target)
        {
            if (target == null || zoneCollider == null)
            {
                return false;
            }

            Collider[] targetColliders = target.GetComponentsInChildren<Collider>();
            bool foundCollider = false;
            foreach (Collider targetCollider in targetColliders)
            {
                if (targetCollider == null || !targetCollider.enabled || targetCollider.isTrigger)
                {
                    continue;
                }

                foundCollider = true;
                if (!ContainsCollider(targetCollider))
                {
                    return false;
                }
            }

            return foundCollider;
        }

        private bool ContainsCollider(Collider targetCollider)
        {
            if (targetCollider is BoxCollider boxCollider)
            {
                Vector3 halfSize = boxCollider.size * 0.5f;
                for (int x = -1; x <= 1; x += 2)
                {
                    for (int y = -1; y <= 1; y += 2)
                    {
                        for (int z = -1; z <= 1; z += 2)
                        {
                            Vector3 localCorner = boxCollider.center + Vector3.Scale(
                                halfSize,
                                new Vector3(x, y, z));
                            Vector3 worldCorner = boxCollider.transform.TransformPoint(localCorner);
                            if (!ContainsPoint(worldCorner))
                            {
                                return false;
                            }
                        }
                    }
                }

                return true;
            }

            Bounds bounds = targetCollider.bounds;
            Vector3 center = bounds.center;
            Vector3 extents = bounds.extents;
            for (int x = -1; x <= 1; x += 2)
            {
                for (int y = -1; y <= 1; y += 2)
                {
                    for (int z = -1; z <= 1; z += 2)
                    {
                        Vector3 corner = center + Vector3.Scale(extents, new Vector3(x, y, z));
                        if (!ContainsPoint(corner))
                        {
                            return false;
                        }
                    }
                }
            }

            return true;
        }

        private bool ContainsPoint(Vector3 worldPoint)
        {
            Vector3 localPoint = zoneCollider.transform.InverseTransformPoint(worldPoint);
            Vector3 minimum = zoneCollider.center - zoneCollider.size * 0.5f;
            Vector3 maximum = zoneCollider.center + zoneCollider.size * 0.5f;
            const float tolerance = 0.001f;

            return localPoint.x >= minimum.x - tolerance && localPoint.x <= maximum.x + tolerance &&
                   localPoint.y >= minimum.y - tolerance && localPoint.y <= maximum.y + tolerance &&
                   localPoint.z >= minimum.z - tolerance && localPoint.z <= maximum.z + tolerance;
        }
    }
}
