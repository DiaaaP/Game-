using System;
using System.Collections;
using System.Linq;
using Mirror;
using UnityEngine;
using UnityEngine.InputSystem;

namespace DeliveryImpossible.Networking
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class DevelopmentNetworkLauncher : MonoBehaviour
    {
        private NetworkManagerHUD networkHud;
        private bool showNetworkHudWhileConnected;

        private void Awake()
        {
            Application.runInBackground = true;
            networkHud = GetComponent<NetworkManagerHUD>();
        }

        private void Update()
        {
            if (Keyboard.current?.f1Key.wasPressedThisFrame == true)
            {
                showNetworkHudWhileConnected = !showNetworkHudWhileConnected;
            }

            if (networkHud == null)
            {
                return;
            }

            networkHud.enabled = showNetworkHudWhileConnected;
        }

        private IEnumerator Start()
        {
            string[] arguments = Environment.GetCommandLineArgs();
            bool autoClient = arguments.Contains("--auto-client");
            bool diagnostics = arguments.Contains("--network-diagnostics");

            if (autoClient)
            {
                yield return null;
                NetworkManager manager = GetComponent<NetworkManager>();
                manager.networkAddress = "127.0.0.1";
                manager.StartClient();
            }

            if (diagnostics)
            {
                StartCoroutine(LogNetworkState());
            }
        }

        private static IEnumerator LogNetworkState()
        {
            WaitForSeconds wait = new WaitForSeconds(0.5f);
            while (true)
            {
                Interaction.NetworkGrabbable cargo =
                    FindFirstObjectByType<Interaction.NetworkGrabbable>();

                if (cargo != null && NetworkClient.isConnected)
                {
                    Rigidbody body = cargo.GetComponent<Rigidbody>();
                    Debug.Log(
                        $"[NetworkDiagnostics] role=client netId={cargo.netId} " +
                        $"position={cargo.transform.position:F3} held={cargo.IsHeld} " +
                        $"kinematic={body.isKinematic}");
                }

                yield return wait;
            }
        }
    }
}
