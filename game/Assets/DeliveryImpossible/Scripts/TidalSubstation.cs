using Mirror;
using TMPro;
using UnityEngine;

namespace DeliveryImpossible.Orders
{
    public sealed class TidalSubstation : MonoBehaviour
    {
        [System.Serializable]
        private sealed class SteamBank
        {
            public DeliveryCheckpoint passage;
            public ParticleSystem steam;
            public Renderer signal;
            public TMP_Text clock;
        }

        public const int Charger = 0;
        public const int FirstSteamBank = 1;
        public const int LiftCheck = 3;
        public const float ChargeSeconds = 1.4f;

        private const float SteamCycleSeconds = 9f;
        private const float SteamOnSeconds = 3.4f;
        private const float WarningSeconds = 1f;

        [SerializeField] private Transform bridgeShutter;
        [SerializeField] private Transform bridgeLanding;
        [SerializeField] private Renderer chargerSignal;
        [SerializeField] private SteamBank[] steamBanks;
        [SerializeField] private Transform[] recoveryPoints;
        [SerializeField] private Renderer[] circuitSignals;
        [SerializeField] private Transform[] generatorRotors;
        [SerializeField] private Light[] circuitLights;
        [SerializeField] private Material danger;
        [SerializeField] private Material caution;
        [SerializeField] private Material clear;

        private Vector3 shutterClosed;
        public Vector3 BridgeLanding => bridgeLanding.position;

        public bool BeforeBridgeLanding(Vector3 position) => bridgeLanding.InverseTransformPoint(position).z < 0f;

        private void Awake()
        {
            shutterClosed = bridgeShutter.localPosition;
        }

        public static bool IsSteamCheck(int checkpoint) => checkpoint >= FirstSteamBank && checkpoint < LiftCheck;

        private static float SteamPhase(int checkpoint) =>
            (float)((NetworkTime.time + (checkpoint - FirstSteamBank) * 4.25d) % SteamCycleSeconds);

        public static bool SteamActive(int checkpoint) => SteamPhase(checkpoint) < SteamOnSeconds;

        public static int SteamSecondsRemaining(int checkpoint)
        {
            float phase = SteamPhase(checkpoint);
            return Mathf.CeilToInt((phase < SteamOnSeconds ? SteamOnSeconds : SteamCycleSeconds) - phase);
        }

        public Transform RecoveryPoint(int checkpoints) => checkpoints > 0 && checkpoints <= recoveryPoints.Length
            ? recoveryPoints[checkpoints - 1] : null;

        public void ApplySteam(Rigidbody cargo, Vector3 center)
        {
            if (cargo.isKinematic) return;

            for (int index = 0; index < steamBanks.Length; index++)
            {
                SteamBank bank = steamBanks[index];
                if (SteamActive(FirstSteamBank + index) && bank.passage.ContainsPoint(center))
                    cargo.AddForce(bank.passage.transform.TransformDirection(new Vector3(0f, 1.6f, -15f)), ForceMode.Acceleration);
            }
        }

        public void UpdateShutter(float travel)
        {
            bridgeShutter.localPosition = shutterClosed + Vector3.up * (3.64f * Mathf.SmoothStep(0f, 1f, travel));
        }

        public void UpdateDisplays(NetworkDeliveryOrder order, bool active, bool visible)
        {
            UpdateShutter(active ? order.StationGateProgress : 0f);
            SetSignal(chargerSignal, active && order.StationGateProgress > 0f ? clear : caution);
            for (int index = 0; index < steamBanks.Length; index++)
            {
                SteamBank bank = steamBanks[index];
                int checkpoint = FirstSteamBank + index;
                float phase = SteamPhase(checkpoint);
                bool blasting = active && phase < SteamOnSeconds;
                bool warning = active && phase >= SteamCycleSeconds - WarningSeconds;
                SetSignal(bank.signal, blasting ? danger : warning ? caution : clear);
                if (blasting && !bank.steam.isPlaying) bank.steam.Play();
                if (!blasting && bank.steam.isPlaying)
                    bank.steam.Stop(true, ParticleSystemStopBehavior.StopEmitting);
                if (visible)
                {
                    string text = !active ? "ПАРОВЫЙ КАНАЛ" :
                        $"{(blasting ? "ПАР" : warning ? "ПАР СКОРО" : "ПРОХОД")}  {SteamSecondsRemaining(checkpoint)} с";
                    if (bank.clock.text != text) bank.clock.text = text;
                    bank.clock.color = bank.signal.sharedMaterial.color;
                }
            }

            for (int index = 0; index < circuitSignals.Length; index++)
            {
                bool online = active && order.StationCircuitOnline(index);
                SetSignal(circuitSignals[index], online ? clear : danger);
                if (circuitLights[index].enabled != online) circuitLights[index].enabled = online;
                if (online) generatorRotors[index].Rotate(Vector3.forward, Time.deltaTime * 155f, Space.Self);
            }
        }

        private static void SetSignal(Renderer signal, Material material)
        {
            if (signal.sharedMaterial != material) signal.sharedMaterial = material;
        }
    }
}
