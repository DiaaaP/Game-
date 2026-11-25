using System.Collections.Generic;
using Mirror;
using UnityEngine;
using LobbyPlayer = DeliveryImpossible.Networking.NetworkLobbyPlayer;

namespace DeliveryImpossible.Orders
{
    public enum DeliveryMechanic
    {
        Standard,
        Drawbridge,
        Conveyor,
        Security,
        ColdChain,
        Docking
    }

    [DisallowMultipleComponent]
    public sealed class DeliveryCourse : MonoBehaviour
    {
        [SerializeField] private DeliveryMechanic mechanic;
        [SerializeField] private GameObject bridgeDeck;
        [SerializeField] private DeliveryCheckpoint[] controls;
        [SerializeField] private DeliveryCheckpoint belt;
        [SerializeField] private Transform beltLoadPoint;
        [SerializeField] private Transform bypassGate;
        [SerializeField] private GameObject[] securityBeams;

        public DeliveryMechanic Mechanic => mechanic;
        public Transform BeltLoadPoint => beltLoadPoint;
        public const float BridgeTravelSeconds = 2.8f;
        private const float ScannerCycleSeconds = 7.4f;
        private const float ScannerActiveSeconds = 3.15f;
        private Renderer[] scenery;
        private Light[] lights;
        private bool? visibleOnClient;
        private Collider[] bridgeColliders;
        private Vector3 bypassClosedPosition;

        private void Awake()
        {
            scenery = GetComponentsInChildren<Renderer>(true);
            lights = GetComponentsInChildren<Light>(true);
            if (bridgeDeck != null) bridgeColliders = bridgeDeck.GetComponentsInChildren<Collider>();
            if (bypassGate != null) bypassClosedPosition = bypassGate.localPosition;
        }

        private void LateUpdate()
        {
            NetworkDeliveryOrder order = NetworkDeliveryOrder.Active;
            bool active = order != null && order.UsesCourse(this);
            bool visible = order == null || active;
            if (visibleOnClient != visible)
            {
                visibleOnClient = visible;
                foreach (Renderer item in scenery) item.forceRenderingOff = !visible;
                foreach (Light lamp in lights) lamp.enabled = visible;
            }

            if (mechanic == DeliveryMechanic.Drawbridge)
            {
                float travel = active ? order.BridgeProgress : 0f;
                bridgeDeck.transform.localRotation = Quaternion.Euler(Mathf.Lerp(-76f, 0f,
                    Mathf.SmoothStep(0f, 1f, travel)), 0f, 0f);
                foreach (Collider item in bridgeColliders) item.enabled = travel >= 0.999f;
            }
            else if (mechanic == DeliveryMechanic.Conveyor && bypassGate != null)
            {
                float lift = active ? order.BypassGateProgress : 0f;
                bypassGate.localPosition = bypassClosedPosition + Vector3.up * (4.0f * lift);
            }
            else if (mechanic == DeliveryMechanic.Security)
            {
                for (int index = 0; index < securityBeams.Length; index++)
                {
                    securityBeams[index].SetActive(!active || IsScannerActive(index));
                }
            }
        }

        private static float ScannerPhase(int index)
        {
            return (float)((NetworkTime.time + index * 2.22d) % ScannerCycleSeconds);
        }

        public static bool IsScannerActive(int index) => ScannerPhase(index) < ScannerActiveSeconds;

        public static float ScannerTimeRemaining(int index)
        {
            float phase = ScannerPhase(index);
            return (phase < ScannerActiveSeconds ? ScannerActiveSeconds : ScannerCycleSeconds) - phase;
        }

        public bool PlatesPressed(IReadOnlyList<LobbyPlayer> players, bool team)
        {
            LobbyPlayer left = null;
            LobbyPlayer right = null;
            foreach (LobbyPlayer player in players)
            {
                Vector3 feet = player.transform.position;
                if (controls[0].ContainsPoint(feet)) left = player;
                if (controls[1].ContainsPoint(feet)) right = player;
            }

            return team ? left != null && right != null && left != right : left != null || right != null;
        }

        public bool IsOnBelt(Vector3 position) => belt != null && belt.ContainsPoint(position);

        public DeliveryCheckpoint GetControl(int index) =>
            controls != null && index >= 0 && index < controls.Length ? controls[index] : null;

        public DeliveryCheckpoint NearestPlate(Vector3 position)
        {
            return (controls[0].transform.position - position).sqrMagnitude <=
                   (controls[1].transform.position - position).sqrMagnitude ? controls[0] : controls[1];
        }

        public void DriveBelt(Rigidbody body)
        {
            Vector3 local = belt.transform.InverseTransformPoint(body.worldCenterOfMass);
            Vector3 velocity = belt.transform.InverseTransformDirection(body.linearVelocity);
            float centering = Mathf.Clamp(-14f * local.x - 6f * velocity.x, -16f, 16f);
            float acceleration = Mathf.Clamp(14f * (3.3f - velocity.z), -8f, 30f);
            body.AddForce(belt.transform.TransformDirection(new Vector3(centering, 0f, acceleration)), ForceMode.Acceleration);
        }

        public bool IsCooling(Vector3 position)
        {
            foreach (DeliveryCheckpoint control in controls)
            {
                if (control.ContainsPoint(position)) return true;
            }

            return false;
        }

        public static bool DockAligned(Transform cargo, Transform dock)
        {
            Vector3 heading = Vector3.ProjectOnPlane(cargo.forward, Vector3.up).normalized;
            return Vector3.Dot(heading, dock.forward) >= 0.88f && Vector3.Dot(cargo.up, Vector3.up) >= 0.86f;
        }
    }
}
