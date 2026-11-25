using System.Linq;
using DeliveryImpossible.Networking;
using DeliveryImpossible.Orders;
using Mirror;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem;
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
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private Button resumeButton;
        [SerializeField] private Button leaveShiftButton;
        [SerializeField] private Button retryShiftButton;

        [Header("Campaign results")]
        [SerializeField] private GameObject campaignResultsPanel;
        [SerializeField] private TMP_Text campaignResultTitle;
        [SerializeField] private TMP_Text campaignResultDetails;
        [SerializeField] private TMP_Text campaignResultHint;
        [SerializeField] private Button restartCampaignButton;
        [SerializeField] private Button leaveCampaignButton;

        [Header("Main menu")]
        [SerializeField] private Button soloButton;
        [SerializeField] private Button playButton;
        [SerializeField] private Button playBackButton;
        [SerializeField] private Button openJoinButton;
        [SerializeField] private Button joinBackButton;
        [SerializeField] private TMP_InputField addressInput;
        [SerializeField] private Button hostButton;
        [SerializeField] private Button joinButton;
        [SerializeField] private Button quitButton;
        [SerializeField] private TMP_Text connectionStatusText;
        [SerializeField] private GameObject settingsPanel;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button settingsBackButton;
        [SerializeField] private Slider volumeSlider;
        [SerializeField] private Slider sensitivitySlider;
        [SerializeField] private TMP_Text volumeValue;
        [SerializeField] private TMP_Text sensitivityValue;

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
        private bool pauseOpen;
        private bool startSoloOnConnect;
        private MenuPage menuPage = MenuPage.Main;
        private const string VolumePref = "NorthQuay.Volume";
        private const string SensitivityPref = "NorthQuay.MouseSensitivity";

        private enum MenuPage
        {
            Main,
            Play,
            Join,
            Settings
        }

        public static bool BlocksGameplay { get; private set; } = true;
        public static bool ManagesPause { get; private set; }
        public static bool ShowsCampaignResults { get; private set; }
        public static float MouseSensitivityMultiplier { get; private set; } = 1f;

        private void Awake()
        {
            manager = GetComponent<NetworkManager>();
            soloButton?.onClick.AddListener(StartSolo);
            playButton?.onClick.AddListener(ShowPlayMenu);
            playBackButton?.onClick.AddListener(ShowMainMenu);
            openJoinButton?.onClick.AddListener(ShowJoinMenu);
            joinBackButton?.onClick.AddListener(ReturnFromJoin);
            hostButton?.onClick.AddListener(StartHost);
            joinButton?.onClick.AddListener(StartClient);
            quitButton?.onClick.AddListener(QuitGame);
            settingsButton?.onClick.AddListener(() => menuPage = MenuPage.Settings);
            settingsBackButton?.onClick.AddListener(ShowMainMenu);
            SetVolume(Mathf.Clamp01(PlayerPrefs.GetFloat(VolumePref, 1f)));
            SetSensitivity(Mathf.Clamp(PlayerPrefs.GetFloat(SensitivityPref, 1f), 0.5f, 2f));
            if (volumeSlider != null)
            {
                volumeSlider.SetValueWithoutNotify(AudioListener.volume);
                volumeSlider.onValueChanged.AddListener(SetVolume);
            }
            if (sensitivitySlider != null)
            {
                sensitivitySlider.SetValueWithoutNotify(MouseSensitivityMultiplier);
                sensitivitySlider.onValueChanged.AddListener(SetSensitivity);
            }
            readyButton?.onClick.AddListener(ToggleReady);
            startButton?.onClick.AddListener(StartSession);
            disconnectButton?.onClick.AddListener(Disconnect);
            resumeButton?.onClick.AddListener(ResumeShift);
            leaveShiftButton?.onClick.AddListener(Disconnect);
            retryShiftButton?.onClick.AddListener(RetryShift);
            restartCampaignButton?.onClick.AddListener(StartSession);
            leaveCampaignButton?.onClick.AddListener(Disconnect);
            ManagesPause = pausePanel != null;

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
            if (startSoloOnConnect && connected && NetworkServer.active && LobbyPlayer.LocalPlayer != null)
            {
                startSoloOnConnect = false;
                LobbyPlayer.LocalPlayer.RequestStartSession();
                sessionStarted = NetworkSessionState.Active?.SessionStarted == true;
            }
            bool showMainMenu = !connected;
            bool showLobby = connected && !sessionStarted;
            bool showMenu = showMainMenu || showLobby;
            NetworkDeliveryOrder order = NetworkDeliveryOrder.Active;
            ShowsCampaignResults = campaignResultsPanel != null && sessionStarted &&
                                   order != null && order.State == DeliveryOrderState.ShiftCompleted;
            if (!sessionStarted || ShowsCampaignResults)
            {
                pauseOpen = false;
            }
            else if (ManagesPause && Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                pauseOpen = !pauseOpen;
            }

            if (sessionStarted && !pauseOpen && !ShowsCampaignResults &&
                Keyboard.current?.rKey.wasPressedThisFrame == true)
            {
                LobbyPlayer.LocalPlayer?.RequestCargoRecovery();
            }

            if (!connected && wasConnected)
            {
                menuPage = MenuPage.Main;
            }
            if (showMainMenu && !connecting && menuPage != MenuPage.Main &&
                Keyboard.current?.escapeKey.wasPressedThisFrame == true)
            {
                ShowMainMenu();
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
            settingsPanel?.SetActive(showMainMenu && menuPage == MenuPage.Settings);

            if (lobbyPanel != null)
            {
                lobbyPanel.SetActive(showLobby);
            }

            pausePanel?.SetActive(pauseOpen);
            if (retryShiftButton != null) retryShiftButton.interactable = !ShowsCampaignResults;
            campaignResultsPanel?.SetActive(ShowsCampaignResults);
            if (ShowsCampaignResults)
            {
                UpdateCampaignResults(order);
            }

            if (lobbyCanvas != null)
            {
                lobbyCanvas.enabled = showMenu || pauseOpen || ShowsCampaignResults;
            }

            UpdateMainMenu(connecting);
            UpdateLobby(showLobby);
            SetInputBlocked(showMenu || pauseOpen || ShowsCampaignResults);
        }

        private void UpdateCampaignResults(NetworkDeliveryOrder order)
        {
            campaignResultTitle.text = order.CampaignWon ? "КАМПАНИЯ ПРОЙДЕНА" : "КАМПАНИЯ НЕ ПРОЙДЕНА";
            campaignResultTitle.color = order.CampaignWon
                ? new Color32(141, 225, 173, 255)
                : new Color32(255, 164, 119, 255);
            campaignResultDetails.text = $"Доставлено  {order.CompletedOrders}/{order.TotalOrders}\nОчки  {order.TotalScore}";
            bool host = NetworkServer.active;
            restartCampaignButton.gameObject.SetActive(host);
            campaignResultHint.text = host ? "" : "Новую кампанию запускает хост.";
        }

        private void UpdateMainMenu(bool connecting)
        {
            bool idle = !NetworkClient.active && !NetworkServer.active;
            if (soloButton != null) soloButton.interactable = idle;
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
                        ? "НАЧАТЬ ОДНОМУ"
                        : "НАЧАТЬ СМЕНУ";
            }

            if (lobbyStatusText != null)
            {
                lobbyStatusText.text = soloTestAvailable
                    ? "ПРОЙДИТЕ СМЕНУ БЕЗ НАПАРНИКА"
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

        private void StartSolo()
        {
            if (NetworkClient.active || NetworkServer.active) return;
            startSoloOnConnect = true;
            StartHost();
        }

        private void ReturnFromJoin()
        {
            if (playMenuPanel != null) ShowPlayMenu();
            else ShowMainMenu();
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

        private void SetVolume(float value)
        {
            AudioListener.volume = value;
            if (volumeValue != null) volumeValue.text = $"{Mathf.RoundToInt(value * 100f)}%";
            PlayerPrefs.SetFloat(VolumePref, value);
        }

        private void SetSensitivity(float value)
        {
            MouseSensitivityMultiplier = value;
            if (sensitivityValue != null) sensitivityValue.text = $"{Mathf.RoundToInt(value * 100f)}%";
            PlayerPrefs.SetFloat(SensitivityPref, value);
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

        private void ResumeShift()
        {
            pauseOpen = false;
        }

        private void RetryShift()
        {
            LobbyPlayer.LocalPlayer?.RequestCargoRecovery();
            pauseOpen = false;
        }

        private void Disconnect()
        {
            startSoloOnConnect = false;
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
            ManagesPause = false;
            ShowsCampaignResults = false;
        }
    }
}
