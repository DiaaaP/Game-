using System.Linq;
using DeliveryImpossible.Orders;
using Mirror;
using UnityEngine;

namespace DeliveryImpossible.Networking
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkSessionState : NetworkBehaviour
    {
        [SerializeField, Min(1)] private int requiredPlayers = 2;
        [SerializeField] private bool allowSoloTesting = true;

        [SyncVar] private bool sessionStarted;
        private bool startedSolo;

        public static NetworkSessionState Active { get; private set; }
        public bool SessionStarted => sessionStarted;
        public int RequiredPlayers => requiredPlayers;
        public bool AllowSoloTesting => allowSoloTesting;

        private void Awake()
        {
            Active = this;
        }

        public override void OnStartServer()
        {
            base.OnStartServer();
            Active = this;
            sessionStarted = false;
            startedSolo = false;
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            Active = this;
        }

        [Server]
        public void ServerTryStart(NetworkConnectionToClient requester)
        {
            if (sessionStarted || requester == null || requester.connectionId != 0)
            {
                return;
            }

            int playerCount = NetworkLobbyPlayer.ServerPlayers.Count;
            bool soloTest = allowSoloTesting && playerCount == 1;
            if (!soloTest &&
                (playerCount < requiredPlayers ||
                 NetworkLobbyPlayer.ServerPlayers.Any(player => !player.IsReady)))
            {
                return;
            }

            startedSolo = soloTest;
            sessionStarted = true;
            FindFirstObjectByType<NetworkDeliveryOrder>()?.ServerRestartShift();
        }

        [Server]
        public void ServerHandleRosterChanged()
        {
            int minimumPlayers = startedSolo ? 1 : requiredPlayers;
            if (!sessionStarted || NetworkLobbyPlayer.ServerPlayers.Count >= minimumPlayers)
            {
                return;
            }

            sessionStarted = false;
            startedSolo = false;
            foreach (NetworkLobbyPlayer player in NetworkLobbyPlayer.ServerPlayers)
            {
                player.ServerSetReady(false);
            }

            FindFirstObjectByType<NetworkDeliveryOrder>()?.ServerRestartShift();
        }

        private void OnDestroy()
        {
            if (Active == this)
            {
                Active = null;
            }
        }
    }
}
