using Mirror;
using UnityEngine;

namespace DeliveryImpossible.Interaction
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    [RequireComponent(typeof(Rigidbody))]
    [RequireComponent(typeof(NetworkGrabbable))]
    public sealed class NetworkFragileCargo : NetworkBehaviour
    {
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        private static readonly int ColorId = Shader.PropertyToID("_Color");

        [Header("Impact damage")]
        [SerializeField, Min(0f)] private float safeImpactSpeed = 3.5f;
        [SerializeField, Min(0.1f)] private float breakingImpactSpeed = 9f;

        [Header("Feedback")]
        [SerializeField] private Renderer[] damageRenderers;
        [SerializeField] private GameObject damageVisual;
        [SerializeField] private Color intactColor = new Color(0.05f, 0.25f, 0.35f, 1f);
        [SerializeField] private Color damagedColor = new Color(0.75f, 0.04f, 0.015f, 1f);

        [SyncVar(hook = nameof(OnIntegrityChanged))]
        private float integrity = 1f;

        private MaterialPropertyBlock propertyBlock;

        public float IntegrityNormalized => integrity;
        public int IntegrityPercent => Mathf.RoundToInt(integrity * 100f);
        public bool IsBroken => integrity <= 0f;

        private void Awake()
        {
            propertyBlock = new MaterialPropertyBlock();
            ApplyDamageAppearance(integrity);
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            integrity = 1f;
            ApplyDamageAppearance(integrity);
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            ApplyDamageAppearance(integrity);
        }

        [ServerCallback]
        private void OnCollisionEnter(Collision collision)
        {
            if (integrity <= 0f)
            {
                return;
            }

            float impactSpeed = collision.relativeVelocity.magnitude;
            if (impactSpeed <= safeImpactSpeed)
            {
                return;
            }

            float damage = Mathf.InverseLerp(
                safeImpactSpeed,
                Mathf.Max(safeImpactSpeed + 0.1f, breakingImpactSpeed),
                impactSpeed);
            integrity = Mathf.Clamp01(integrity - damage);
            ApplyDamageAppearance(integrity);
        }

        [Server]
        public void ServerResetDamage()
        {
            integrity = 1f;
            ApplyDamageAppearance(integrity);
        }

        private void OnIntegrityChanged(float _, float current)
        {
            ApplyDamageAppearance(current);
        }

        private void ApplyDamageAppearance(float currentIntegrity)
        {
            propertyBlock ??= new MaterialPropertyBlock();
            Color color = Color.Lerp(damagedColor, intactColor, Mathf.Clamp01(currentIntegrity));

            if (damageRenderers != null)
            {
                foreach (Renderer targetRenderer in damageRenderers)
                {
                    if (targetRenderer == null)
                    {
                        continue;
                    }

                    targetRenderer.GetPropertyBlock(propertyBlock);
                    propertyBlock.SetColor(BaseColorId, color);
                    propertyBlock.SetColor(ColorId, color);
                    targetRenderer.SetPropertyBlock(propertyBlock);
                }
            }

            if (damageVisual != null)
            {
                damageVisual.SetActive(currentIntegrity < 0.7f);
            }
        }
    }
}
