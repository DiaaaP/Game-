using DeliveryImpossible.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static readonly Color QuayInk = new Color(0.045f, 0.074f, 0.083f);
        private static readonly Color QuayPaper = new Color(0.94f, 0.96f, 0.94f);
        private static readonly Color QuayMuted = new Color(0.66f, 0.76f, 0.75f);
        private static readonly Color QuayMint = new Color(0.50f, 0.83f, 0.73f);
        private static readonly Color QuayDarkButton = new Color(0.13f, 0.21f, 0.23f);
        private static readonly Color MenuAmber = new Color(0.97f, 0.76f, 0.51f);

        private static void SetUpQuayMenus()
        {
            EditPrefab("NetworkBootstrap", root =>
            {
                var props = new SerializedObject(root.GetComponent<NetworkLobbyUi>());
                var canvas = (Canvas)props.FindProperty("lobbyCanvas").objectReferenceValue;
                var originalInput = props.FindProperty("addressInput").objectReferenceValue as TMP_InputField;
                var address = Object.Instantiate(originalInput, root.transform, false);
                for (int index = canvas.transform.childCount - 1; index >= 0; index--)
                    Object.DestroyImmediate(canvas.transform.GetChild(index).gameObject);
                var scaler = canvas.GetComponent<CanvasScaler>();
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.75f;
                var camera = (Camera)props.FindProperty("menuCamera").objectReferenceValue;
                camera.transform.SetPositionAndRotation(new Vector3(0.1f, 5.6f, 36.4f), Quaternion.Euler(4f, 21f, 0f));
                camera.fieldOfView = 62f;
                camera.cullingMask = ~0;
                camera.clearFlags = CameraClearFlags.Skybox;
                ConfigureQuayCamera(camera);

                var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/DeliveryRubik.asset");
                var headingFont = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/DeliveryOswald.asset");
                RectTransform main = CanvasLayer(canvas.transform, "Dock Shift main", Color.clear);
                var gradient = new GameObject("Dockside shade", typeof(RectTransform), typeof(RawImage)).GetComponent<RawImage>();
                gradient.transform.SetParent(main, false);
                gradient.texture = QuayMenuShade();
                gradient.raycastTarget = false;
                Stretch(gradient.rectTransform);
                gradient.rectTransform.anchorMax = new Vector2(0.58f, 1f);

                RectTransform left = ChildRect(main, "Main actions");
                left.anchorMin = left.anchorMax = left.pivot = Vector2.zero;
                left.anchoredPosition = new Vector2(71f, 56f);
                left.sizeDelta = new Vector2(396f, 400f);
                TMP_Text title = UiText(left, headingFont, ProductName, 68f, QuayPaper, 0, 65, 395, 108);
                title.fontStyle = FontStyles.UpperCase;
                title.characterSpacing = 2.2f;
                title.textWrappingMode = TextWrappingModes.NoWrap;
                Button singlePlayer = MenuListItem(left, font, "Одиночная игра", 181f, first: true);
                Wire(props, "soloButton", singlePlayer);
                Wire(props, "hostButton", MenuListItem(left, font, "Создать комнату", 222f));
                Wire(props, "openJoinButton", MenuListItem(left, font, "Войти по IP", 263f));
                Wire(props, "settingsButton", MenuListItem(left, font, "Настройки", 304f));
                Wire(props, "quitButton", MenuListItem(left, font, "Выход", 345f));
                var input = root.GetComponentInChildren<UnityEngine.EventSystems.EventSystem>(true);
                if (input != null) input.firstSelectedGameObject = singlePlayer.gameObject;
                Wire(props, "mainMenuPanel", main.gameObject);
                Wire(props, "playMenuPanel", null);
                Wire(props, "playButton", null);
                Wire(props, "playBackButton", null);

                RectTransform join = MenuDialog(canvas.transform, "Join ship crew", new Vector2(512f, 302f));
                RectTransform joinCard = (RectTransform)join.GetChild(0);
                UiText(joinCard, font, "Подключение", 31f, QuayPaper, 28, 25, 447, 46).fontStyle = FontStyles.Bold;
                UiText(joinCard, font, "IP компьютера хоста", 16f, QuayMuted, 29, 85, 446, 24);
                address.transform.SetParent(joinCard, false);
                address.gameObject.SetActive(true);
                TopLeft((RectTransform)address.transform, 28, 115, 455, 49);
                if (address.targetGraphic != null) address.targetGraphic.color = QuayDarkButton;
                address.textComponent.font = font;
                address.textComponent.fontSize = 20f;
                address.textComponent.color = QuayPaper;
                if (address.placeholder is TMP_Text placeholder)
                {
                    placeholder.font = font;
                    placeholder.fontSize = 19f;
                    placeholder.color = QuayMuted;
                }
                Wire(props, "addressInput", address);
                Wire(props, "connectionStatusText", UiText(joinCard, font, "", 14f, QuayMuted, 28, 174, 455, 28));
                Wire(props, "joinButton", QuayButton(joinCard, font, "Подключиться", 28, 227, 240, 47, true));
                Wire(props, "joinBackButton", QuayButton(joinCard, font, "Назад", 282, 227, 199, 47));
                Wire(props, "joinMenuPanel", join.gameObject);
                join.gameObject.SetActive(false);

                RectTransform settings = MenuDialog(canvas.transform, "Game settings", new Vector2(498f, 320f));
                RectTransform settingsCard = (RectTransform)settings.GetChild(0);
                UiText(settingsCard, headingFont, "Настройки", 32f, QuayPaper, 28, 20, 440, 48);
                Wire(props, "volumeSlider", QuaySlider(settingsCard, font, "Общая громкость", 86f, 0f, 1f, out TMP_Text volume));
                Wire(props, "sensitivitySlider", QuaySlider(settingsCard, font, "Чувствительность мыши", 159f, 0.5f, 2f, out TMP_Text sensitivity));
                Wire(props, "volumeValue", volume);
                Wire(props, "sensitivityValue", sensitivity);
                Wire(props, "settingsBackButton", QuayButton(settingsCard, font, "Назад", 28, 247, 161, 44));
                Wire(props, "settingsPanel", settings.gameObject);
                settings.gameObject.SetActive(false);

                RectTransform lobby = MenuDialog(canvas.transform, "Port crew room", new Vector2(554f, 355f));
                RectTransform crew = (RectTransform)lobby.GetChild(0);
                Wire(props, "lobbyTitleText", UiText(crew, font, "Экипаж", 31f, QuayPaper, 28, 24, 499, 48));
                Wire(props, "playerListText", UiText(crew, font, "", 21f, QuayPaper, 28, 86, 499, 78));
                Wire(props, "lobbyStatusText", UiText(crew, font, "", 16f, QuayMuted, 28, 171, 498, 52));
                Button ready = QuayButton(crew, font, "Готов", 28, 239, 211, 49);
                Button begin = QuayButton(crew, font, "Начать рейс", 252, 239, 274, 49, true);
                Wire(props, "readyButton", ready);
                Wire(props, "startButton", begin);
                Wire(props, "readyButtonText", ready.GetComponentInChildren<TMP_Text>());
                Wire(props, "startButtonText", begin.GetComponentInChildren<TMP_Text>());
                Wire(props, "disconnectButton", QuayButton(crew, font, "Выйти из комнаты", 28, 301, 219, 34));
                Wire(props, "lobbyPanel", lobby.gameObject);
                lobby.gameObject.SetActive(false);

                RectTransform pause = MenuDialog(canvas.transform, "Dock Shift pause", new Vector2(457f, 335f));
                RectTransform halt = (RectTransform)pause.GetChild(0);
                UiText(halt, font, "Пауза", 34f, QuayPaper, 27, 24, 403, 47).fontStyle = FontStyles.Bold;
                Wire(props, "resumeButton", QuayButton(halt, font, "Продолжить", 27, 92, 401, 49, true));
                Wire(props, "retryShiftButton", QuayButton(halt, font, "Повторить доставку", 27, 153, 401, 46));
                Wire(props, "leaveShiftButton", QuayButton(halt, font, "В главное меню", 27, 211, 401, 46));
                UiText(halt, font, "E · взять / отпустить     Q · повернуть     R · повторить",
                    12f, QuayMuted, 27, 286, 401, 23);
                Wire(props, "pausePanel", pause.gameObject);
                pause.gameObject.SetActive(false);

                RectTransform finish = MenuDialog(canvas.transform, "Dock Shift result", new Vector2(520f, 360f));
                RectTransform report = (RectTransform)finish.GetChild(0);
                Wire(props, "campaignResultTitle", UiText(report, font, "", 27f, QuayPaper, 29, 28, 462, 56));
                Wire(props, "campaignResultDetails", UiText(report, font, "", 25f, QuayPaper, 29, 93, 462, 96));
                Wire(props, "campaignResultHint", UiText(report, font, "", 14f, QuayMuted, 29, 184, 462, 35));
                Wire(props, "restartCampaignButton", QuayButton(report, font, "Начать новую смену", 29, 223, 462, 49, true));
                Wire(props, "leaveCampaignButton", QuayButton(report, font, "В главное меню", 29, 286, 462, 45));
                Wire(props, "campaignResultsPanel", finish.gameObject);
                finish.gameObject.SetActive(false);

                props.ApplyModifiedPropertiesWithoutUndo();
            });
        }

        private static Button MenuListItem(RectTransform parent, TMP_FontAsset font, string label, float y, bool first = false)
        {
            RectTransform row = Panel(parent, label, Color.clear);
            TopLeft(row, 0f, y, 260f, 38f);
            TMP_Text caption = UiText(row, font, label, first ? 23f : 20f, Color.white, 0f, 0f, 258f, 38f);
            if (first) caption.fontStyle = FontStyles.Bold;
            Button button = row.gameObject.AddComponent<Button>();
            button.targetGraphic = caption;
            ColorBlock tint = button.colors;
            tint.normalColor = first ? MenuAmber : QuayPaper;
            tint.highlightedColor = MenuAmber;
            tint.selectedColor = MenuAmber;
            tint.pressedColor = new Color(0.85f, 0.56f, 0.30f);
            tint.disabledColor = QuayMuted;
            button.colors = tint;
            return button;
        }

        private static Slider QuaySlider(RectTransform parent, TMP_FontAsset font, string label, float y,
            float min, float max, out TMP_Text value)
        {
            UiText(parent, font, label, 17f, QuayPaper, 28, y, 345, 26);
            value = UiText(parent, font, "100%", 17f, MenuAmber, 379, y, 91, 26);
            value.alignment = TextAlignmentOptions.MidlineRight;
            RectTransform control = Panel(parent, label, Color.clear);
            TopLeft(control, 28, y + 31, 440, 27);
            RectTransform track = Panel(control, "Track", QuayDarkButton);
            TopLeft(track, 0, 10, 440, 7);
            RectTransform fill = Panel(track, "Fill", MenuAmber);
            Stretch(fill);
            RectTransform handleArea = ChildRect(control, "Handle area");
            Stretch(handleArea);
            handleArea.offsetMin = new Vector2(8f, 4.5f);
            handleArea.offsetMax = new Vector2(-8f, -4.5f);
            RectTransform handle = Panel(handleArea, "Handle", QuayPaper);
            handle.sizeDelta = new Vector2(14f, 0f);
            Slider slider = control.gameObject.AddComponent<Slider>();
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handle.GetComponent<Image>();
            slider.minValue = min;
            slider.maxValue = max;
            return slider;
        }

        private static Button QuayButton(RectTransform parent, TMP_FontAsset font, string label,
            float x, float y, float width, float height, bool main = false)
        {
            RectTransform plate = Panel(parent, label, main ? MenuAmber : QuayDarkButton);
            TopLeft(plate, x, y, width, height);
            Button button = plate.gameObject.AddComponent<Button>();
            button.targetGraphic = plate.GetComponent<Image>();
            var tint = button.colors;
            tint.normalColor = Color.white;
            tint.highlightedColor = new Color(0.85f, 0.94f, 0.92f);
            tint.pressedColor = new Color(0.71f, 0.86f, 0.84f);
            tint.disabledColor = new Color(0.61f, 0.67f, 0.68f);
            button.colors = tint;
            TMP_Text caption = UiText(plate, font, label, main ? 19f : 16f, main ? QuayInk : QuayPaper, 15f, 0f, width - 25f, height);
            if (main) caption.fontStyle = FontStyles.Bold;
            return button;
        }

        private static RectTransform MenuDialog(Transform canvas, string name, Vector2 size)
        {
            RectTransform layer = CanvasLayer(canvas, name, new Color(0.02f, 0.04f, 0.05f, 0.69f));
            RectTransform card = Panel(layer, "Dialog panel", QuayInk);
            card.sizeDelta = size;
            return layer;
        }

        private static RectTransform CanvasLayer(Transform parent, string label, Color tint)
        {
            RectTransform layer = Panel(parent, label, tint);
            Stretch(layer);
            return layer;
        }

        private static RectTransform ChildRect(Transform parent, string name)
        {
            var child = new GameObject(name, typeof(RectTransform)).GetComponent<RectTransform>();
            child.SetParent(parent, false);
            return child;
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = rect.offsetMax = Vector2.zero;
        }

        private static Texture2D QuayMenuShade()
        {
            string path = Root + "/UI/PortMenuVignette.asset";
            var asset = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
            if (asset != null) return asset;
            var texture = new Texture2D(128, 64, TextureFormat.RGBA32, false) { name = "Port menu vignette", wrapMode = TextureWrapMode.Clamp };
            for (int y = 0; y < texture.height; y++)
            for (int x = 0; x < texture.width; x++)
            {
                float horizontal = 1f - Mathf.SmoothStep(0f, 1f, (float)x / (texture.width - 1));
                float vertical = 0.98f - 0.52f * Mathf.SmoothStep(0f, 1f, (float)y / (texture.height - 1));
                texture.SetPixel(x, y, new Color(0.022f, 0.048f, 0.060f, horizontal * vertical));
            }
            texture.Apply();
            AssetDatabase.CreateAsset(texture, path);
            return texture;
        }

        private static void Wire(SerializedObject props, string name, Object target)
        {
            props.FindProperty(name).objectReferenceValue = target;
        }
    }
}
