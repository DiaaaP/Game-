using System.Linq;
using DeliveryImpossible.Networking;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using LobbyPlayer = DeliveryImpossible.Networking.NetworkLobbyPlayer;

namespace DeliveryImpossible.UI
{
    [DisallowMultipleComponent]
    [RequireComponent(typeof(NetworkManager))]
    public sealed class NetworkLobbyUi : MonoBehaviour
    {
        [Header("Panels")]
        [SerializeField] private Canvas lobbyCanvas;
        [SerializeField] private Camera menuCamera;
        [SerializeField] private GameObject mainMenuPanel;
        [SerializeField] private GameObject playMenuPanel;
        [SerializeField] private GameObject joinMenuPanel;
        [SerializeField] private GameObject lobbyPanel;

        [Header("Main menu")]
        [SerializeField] private Button playButton;
        [SerializeField] private Button playBackButton;
        [SerializeField] private Button openJoinButton;
        [SerializeField] private Button joinBackButton;
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text connectionStatusText;

        [Header("Lobby")]
        [SerializeField] private TMP_Text lobbyTitleText;
        [SerializeField] private TMP_Text playerListText;
        [SerializeField] private TMP_Text lobbyStatusText;
        [SerializeField] private Button readyButton;
        [SerializeField] private TMP_Text readyButtonText;
        [SerializeField] private Button startButton;
        [SerializeField] private TMP_Text startButtonText;
        [SerializeField] private Button disconnectButton;

        private NetworkManager manager;
        private bool wasBlocking;
        private bool wasConnected;
        private MenuPage menuPage = MenuPage.Main;

        private enum MenuPage
        {
            Main,
            Play,
            Join
        }

        public static bool BlocksGameplay { get; private set; } = true;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            playButton?.onClick.AddListener(ShowPlayMenu);
            playBackButton?.onClick.AddListener(ShowMainMenu);
            openJoinButton?.onClick.AddListener(ShowJoinMenu);
            joinBackButton?.onClick.AddListener(ShowPlayMenu);
            hostButton?.onClick.AddListener(StartHost);
            joinButton?.onClick.AddListener(StartClient);
            quitButton?.onClick.AddListener(QuitGame);
            readyButton?.onClick.AddListener(ToggleReady);
            startButton?.onClick.AddListener(StartSession);
            disconnectButton?.onClick.AddListener(Disconnect);

            if (addressInput != null && string.IsNullOrWhiteSpace(addressInput.text))
            {
                addressInput.text = "127.0.0.1";
            }
        }

        private void Update()
        {
            bool connected = NetworkClient.isConnected;
            bool connecting = NetworkClient.active && !connected;
            bool sessionStarted = connected &&
                                  NetworkSessionState.Active != null &&
                                  NetworkSessionState.Active.SessionStarted;
            bool showMainMenu = !connected;
            bool showLobby = connected && !sessionStarted;
            bool showMenu = showMainMenu || showLobby;

            if (!connected && wasConnected)
            {
                menuPage = MenuPage.Main;
            }

            wasConnected = connected;

            if (menuCamera != null)
            {
                menuCamera.enabled = showMenu;
            }

            if (mainMenuPanel != null)
            {
                mainMenuPanel.SetActive(showMainMenu && menuPage == MenuPage.Main);
            }

            if (playMenuPanel != null)
            {
                playMenuPanel.SetActive(showMainMenu && menuPage == MenuPage.Play);
            }

            if (joinMenuPanel != null)
            {
                joinMenuPanel.SetActive(showMainMenu && menuPage == MenuPage.Join);
            }

            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(showLobby);
            }

            if (lobbyCanvas != null)
            {
                lobbyCanvas.enabled = showMenu;
            }

            UpdateMainMenu(connecting);
            UpdateLobby(showLobby);
            SetInputBlocked(showMenu);
        }

        private void UpdateMainMenu(bool connecting)
        {
            bool idle = !NetworkClient.active && !NetworkServer.active;
            if (hostButton != null) hostButton.interactable = idle;
            if (joinButton != null) joinButton.interactable = idle;
            if (addressInput != null) addressInput.interactable = idle;

            if (connectionStatusText != null)
            {
                connectionStatusText.text = connecting
                    ? "ПОДКЛЮЧЕНИЕ К ДИСПЕТЧЕРСКОЙ..."
                    : "Введите IP компьютера, на котором создана комната.";
            }
        }

        private void UpdateLobby(bool visible)
        {
            if (!visible)
            {
                return;
            }

            LobbyPlayer[] players = LobbyPlayer.Players
                .Where(player => player != null)
                .OrderBy(player => player.PlayerNumber)
                .ToArray();
            int requiredPlayers = NetworkSessionState.Active?.RequiredPlayers ?? 2;
            bool allReady = players.Length >= requiredPlayers && players.All(player => player.IsReady);
            bool isHost = NetworkServer.active && NetworkClient.active;
            bool soloTestAvailable = isHost &&
                                     players.Length == 1 &&
                                     NetworkSessionState.Active != null &&
                                     NetworkSessionState.Active.AllowSoloTesting;
            LobbyPlayer localPlayer = LobbyPlayer.LocalPlayer;

            if (lobbyTitleText != null)
            {
                lobbyTitleText.text = $"ЭКИПАЖ  {players.Length}/{requiredPlayers}";
            }

            if (playerListText != null)
            {
                playerListText.text = players.Length == 0
                    ? "ПОДКЛЮЧЕНИЕ ИГРОКА..."
                    : string.Join(
                        "\n",
                        players.Select(player =>
                            $"КУРЬЕР {player.PlayerNumber}   " +
                            (player.IsReady
                                ? "<color=#22C55E>ГОТОВ</color>"
                                : "<color=#9CA3AF>НЕ ГОТОВ</color>")));
            }

            if (readyButton != null)
            {
                readyButton.interactable = localPlayer != null;
            }

            if (readyButtonText != null)
            {
                readyButtonText.text = localPlayer != null && localPlayer.IsReady
                    ? "ОТМЕНИТЬ ГОТОВНОСТЬ"
                    : "ГОТОВ";
            }

            if (startButton != null)
            {
                startButton.interactable = isHost && (allReady || soloTestAvailable);
            }

            if (startButtonText != null)
            {
                startButtonText.text = !isHost
                    ? "ЖДЁМ ХОСТА"
                    : soloTestAvailable
                        ? "НАЧАТЬ ОДНОМУ  /  ТЕСТ"
                        : "НАЧАТЬ СМЕНУ";
            }

            if (lobbyStatusText != null)
            {
                lobbyStatusText.text = soloTestAvailable
                    ? "МОЖНО ЗАПУСТИТЬ ТЕСТ БЕЗ НАПАРНИКА"
                    : players.Length < requiredPlayers
                    ? "ОЖИДАЕМ ВТОРОГО КУРЬЕРА"
                    : allReady
                        ? isHost ? "ЭКИПАЖ ГОТОВ К ВЫЕЗДУ" : "ХОСТ ЗАПУСКАЕТ СМЕНУ"
                        : "ВСЕ ИГРОКИ ДОЛЖНЫ БЫТЬ ГОТОВЫ";
            }
        }

        private void StartHost()
        {
            if (!NetworkClient.active && !NetworkServer.active)
            {
                manager.StartHost();
            }
        }

        private void ShowMainMenu()
        {
            menuPage = MenuPage.Main;
        }

        private void ShowPlayMenu()
        {
            menuPage = MenuPage.Play;
        }

        private void ShowJoinMenu()
        {
            menuPage = MenuPage.Join;
        }

        private void StartClient()
        {
            if (NetworkClient.active || NetworkServer.active)
            {
                return;
            }

            manager.networkAddress = string.IsNullOrWhiteSpace(addressInput?.text)
                ? "127.0.0.1"
                : addressInput.text.Trim();
            manager.StartClient();
        }

        private static void ToggleReady()
        {
            LobbyPlayer player = LobbyPlayer.LocalPlayer;
            if (player != null)
            {
                player.SetReady(!player.IsReady);
            }
        }

        private static void StartSession()
        {
            LobbyPlayer.LocalPlayer?.RequestStartSession();
        }

        private void Disconnect()
        {
            if (NetworkServer.active && NetworkClient.isConnected)
            {
                manager.StopHost();
            }
            else if (NetworkClient.active)
            {
                manager.StopClient();
            }
            else if (NetworkServer.active)
            {
                manager.StopServer();
            }
        }

        private static void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void SetInputBlocked(bool blocked)
        {
            BlocksGameplay = blocked;
            if (blocked)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            }
            else if (wasBlocking)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }

            wasBlocking = blocked;
        }

        private void OnDestroy()
        {
            BlocksGameplay = false;
        }
    }
}
