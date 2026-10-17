using System.Collections.Generic;
using Mirror;
using UnityEngine;

namespace DeliveryImpossible.Networking
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkIdentity))]
    public sealed class NetworkLobbyPlayer : NetworkBehaviour
    {
        private static readonly List<NetworkLobbyPlayer> ClientPlayers = new();
        private static readonly List<NetworkLobbyPlayer> ServerPlayersInternal = new();

        [SyncVar] private int playerNumber;
        [SyncVar] private bool ready;

        public static IReadOnlyList<NetworkLobbyPlayer> Players => ClientPlayers;
        public static IReadOnlyList<NetworkLobbyPlayer> ServerPlayers => ServerPlayersInternal;
        public static NetworkLobbyPlayer LocalPlayer { get; private set; }

        public int PlayerNumber => playerNumber;
        public bool IsReady => ready;

        public override void OnStartServer()
        {
            base.OnStartServer();
            playerNumber = GetAvailablePlayerNumber();
            ready = false;
            if (!ServerPlayersInternal.Contains(this))
            {
                ServerPlayersInternal.Add(this);
            }
        }

        public override void OnStopServer()
        {
            ServerPlayersInternal.Remove(this);
            NetworkSessionState.Active?.ServerHandleRosterChanged();
            base.OnStopServer();
        }

        public override void OnStartClient()
        {
            base.OnStartClient();
            if (!ClientPlayers.Contains(this))
            {
                ClientPlayers.Add(this);
            }
        }

        public override void OnStopClient()
        {
            ClientPlayers.Remove(this);
            if (LocalPlayer == this)
            {
                LocalPlayer = null;
            }

            base.OnStopClient();
        }

        public override void OnStartLocalPlayer()
        {
            base.OnStartLocalPlayer();
            LocalPlayer = this;
        }

        public void SetReady(bool value)
        {
            if (isLocalPlayer)
            {
                CmdSetReady(value);
            }
        }

        public void RequestStartSession()
        {
            if (isLocalPlayer)
            {
                CmdRequestStartSession();
            }
        }

        [Command]
        private void CmdSetReady(bool value)
        {
            if (NetworkSessionState.Active != null && !NetworkSessionState.Active.SessionStarted)
            {
                ready = value;
            }
        }

        [Command]
        private void CmdRequestStartSession()
        {
            NetworkSessionState.Active?.ServerTryStart(connectionToClient);
        }

        [Server]
        internal void ServerSetReady(bool value)
        {
            ready = value;
        }

        [Server]
        private static int GetAvailablePlayerNumber()
        {
            int candidate = 1;
            while (ServerPlayersInternal.Exists(player => player != null && player.playerNumber == candidate))
            {
                candidate++;
            }

            return candidate;
        }
    }
}
