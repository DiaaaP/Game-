using System;
using System.Collections;
using System.Linq;
using DeliveryImpossible.Orders;
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
                NetworkLobbyPlayer courier = NetworkLobbyPlayer.LocalPlayer;
                NetworkDeliveryOrder order = NetworkDeliveryOrder.Active;
                if (courier != null && order != null && NetworkClient.isConnected)
                {
                    Debug.Log($"[NetworkDiagnostics] courier={courier.PlayerNumber} " +
                        $"position={courier.transform.position:F3} level={order.CurrentLevelNumber}/{order.TotalLevels} " +
                        $"order={order.CurrentOrderNumber}/{order.TotalOrders} " +
                        $"state={order.State} gates={order.CheckpointsPassed}/{order.CheckpointCount}");
                }

                yield return wait;
            }
        }
    }
}
