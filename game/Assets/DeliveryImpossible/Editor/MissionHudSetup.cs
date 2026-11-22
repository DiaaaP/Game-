using DeliveryImpossible.Interaction;
using DeliveryImpossible.UI;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static void SetUpReadableHud(GameObject player)
        {
            SerializedObject data = new SerializedObject(player.GetComponent<NetworkOrderHud>());
            Canvas canvas = (Canvas)data.FindProperty("hudCanvas").objectReferenceValue;
            var oldPanel = data.FindProperty("orderPanel").objectReferenceValue as GameObject;
            if (oldPanel != null) oldPanel.SetActive(false);
            RemoveChild(canvas.transform, "Courier Mission");
            RemoveChild(canvas.transform, "Route Marker");

            CanvasScaler scaler = canvas.GetComponent<CanvasScaler>();
            if (scaler != null)
            {
                scaler.referenceResolution = new Vector2(1280f, 720f);
                scaler.matchWidthOrHeight = 0.75f;
            }

            Color ink = new Color(0.043f, 0.065f, 0.072f, 0.90f);
            Color paper = QuayPaper;
            Color accent = QuayMint;
            Color muted = QuayMuted;
            var font = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/DeliveryRubik.asset");
            RectTransform panel = Panel(canvas.transform, "Courier Mission", ink);
            panel.anchorMin = panel.anchorMax = panel.pivot = new Vector2(0, 1);
            panel.anchoredPosition = new Vector2(28f, -26f);
            panel.sizeDelta = new Vector2(396f, 111f);
            panel.GetComponent<Image>().raycastTarget = false;
            RectTransform line = Panel(panel, "Amber rule", accent);
            TopLeft(line, 0f, 0f, 396f, 2f);
            line.GetComponent<Image>().raycastTarget = false;

            TMP_Text level = UiText(panel, font, "", 12f, accent, 13, 8, 239, 21);
            TMP_Text status = UiText(panel, font, "", 11f, accent, 253, 8, 129, 21);
            status.alignment = TextAlignmentOptions.Right;
            TMP_Text goal = UiText(panel, font, "", 21f, paper, 13, 30, 373, 34);
            goal.fontStyle = FontStyles.Bold;
            TMP_Text hint = UiText(panel, font, "", 14f, muted, 13, 67, 369, 37);

            RectTransform placement = Panel(panel, "Receipt progress", new Color(0.15f, 0.23f, 0.31f));
            TopLeft(placement, 0, 113, 396, 23);
            placement.GetComponent<Image>().raycastTarget = false;
            RectTransform fill = Panel(placement, "Fill", new Color(0.22f, 0.58f, 0.41f));
            fill.anchorMin = new Vector2(0f, 0f);
            fill.anchorMax = new Vector2(0f, 1f);
            fill.offsetMin = fill.offsetMax = Vector2.zero;
            fill.GetComponent<Image>().raycastTarget = false;
            TMP_Text receipt = UiText(placement, font, "", 12f, paper, 9, 1, 373, 20);
            placement.gameObject.SetActive(false);

            RectTransform waypoint = Panel(canvas.transform, "Route Marker", ink);
            waypoint.sizeDelta = new Vector2(236f, 25f);
            waypoint.GetComponent<Image>().raycastTarget = false;
            TMP_Text label = UiText(waypoint, font, "", 12f, paper, 5, 0, 226, 25);
            label.alignment = TextAlignmentOptions.Center;
            label.textWrappingMode = TextWrappingModes.NoWrap;
            waypoint.gameObject.SetActive(false);

            RemoveChild(canvas.transform, "Cargo Condition");
            RectTransform meter = Panel(canvas.transform, "Cargo Condition", ink);
            meter.anchorMin = meter.anchorMax = meter.pivot = Vector2.one;
            meter.anchoredPosition = new Vector2(-28f, -26f);
            meter.sizeDelta = new Vector2(278f, 100f);
            meter.GetComponent<Image>().raycastTarget = false;
            TMP_Text meterHeading = UiText(meter, font, "", 16f, paper, 13f, 8f, 168f, 31f);
            TMP_Text meterValue = UiText(meter, font, "", 25f, accent, 178f, 7f, 84f, 33f);
            meterValue.alignment = TextAlignmentOptions.Right;
            meterValue.fontStyle = FontStyles.Bold;
            RectTransform track = Panel(meter, "Meter track", new Color(0.17f, 0.24f, 0.27f));
            TopLeft(track, 13, 48, 252, 9);
            track.GetComponent<Image>().raycastTarget = false;
            RectTransform meterFill = Panel(track, "Meter level", accent);
            Stretch(meterFill);
            meterFill.anchorMax = Vector2.up;
            Image meterImage = meterFill.GetComponent<Image>();
            meterImage.raycastTarget = false;
            TMP_Text meterNote = UiText(meter, font, "", 13f, muted, 13f, 65f, 252f, 26f);
            meter.gameObject.SetActive(false);

            data.FindProperty("orderPanel").objectReferenceValue = panel.gameObject;
            data.FindProperty("orderTitleText").objectReferenceValue = level;
            data.FindProperty("orderStampText").objectReferenceValue = status;
            data.FindProperty("objectiveText").objectReferenceValue = goal;
            data.FindProperty("objectiveHintText").objectReferenceValue = hint;
            data.FindProperty("placementPanel").objectReferenceValue = placement.gameObject;
            data.FindProperty("placementText").objectReferenceValue = receipt;
            data.FindProperty("placementFill").objectReferenceValue = fill;
            data.FindProperty("waypoint").objectReferenceValue = waypoint;
            data.FindProperty("waypointText").objectReferenceValue = label;
            data.FindProperty("waypointCamera").objectReferenceValue = player.GetComponentInChildren<Camera>(true);
            data.FindProperty("meterPanel").objectReferenceValue = meter.gameObject;
            data.FindProperty("meterHeading").objectReferenceValue = meterHeading;
            data.FindProperty("meterValue").objectReferenceValue = meterValue;
            data.FindProperty("meterNote").objectReferenceValue = meterNote;
            data.FindProperty("meterFill").objectReferenceValue = meterImage;
            data.ApplyModifiedPropertiesWithoutUndo();
            SetUpQuayInteraction(player, font, ink, paper, accent);
        }

        private static void SetUpQuayInteraction(GameObject player, TMP_FontAsset font, Color ink, Color paper, Color accent)
        {
            var data = new SerializedObject(player.GetComponent<NetworkGrabber>());
            var canvas = (Canvas)data.FindProperty("interactionCanvas").objectReferenceValue;
            foreach (string name in new[] { "interactionPanel", "cooperationWarningPanel" })
            {
                var old = data.FindProperty(name).objectReferenceValue as GameObject;
                if (old != null) old.SetActive(false);
            }
            RemoveChild(canvas.transform, "Quay interaction");
            RemoveChild(canvas.transform, "Team lift reminder");
            var scaler = canvas.GetComponent<CanvasScaler>();
            scaler.referenceResolution = new Vector2(1280f, 720f);
            scaler.matchWidthOrHeight = 0.75f;
            RectTransform prompt = BottomChip(canvas.transform, "Quay interaction", 203f, 34f, 45f, ink);
            TopLeft(Panel(prompt, "Key cap", accent), 5f, 4f, 29f, 26f);
            TMP_Text key = UiText(prompt, font, "E", 15f, QuayInk, 6, 3, 27, 27);
            key.alignment = TextAlignmentOptions.Center;
            TMP_Text action = UiText(prompt, font, "", 16f, paper, 44, 0, 155, 32);
            RectTransform team = BottomChip(canvas.transform, "Team lift reminder", 260f, 27f, 83f, ink);
            TMP_Text reminder = UiText(team, font, "", 13f, accent, 7, 0, 248, 27);
            reminder.alignment = TextAlignmentOptions.Center;
            data.FindProperty("interactionPanel").objectReferenceValue = prompt.gameObject;
            data.FindProperty("interactionPrompt").objectReferenceValue = action;
            data.FindProperty("interactionHint").objectReferenceValue = null;
            data.FindProperty("cooperationWarningPanel").objectReferenceValue = team.gameObject;
            data.FindProperty("cooperationWarningText").objectReferenceValue = reminder;
            data.ApplyModifiedPropertiesWithoutUndo();
            prompt.gameObject.SetActive(false);
            team.gameObject.SetActive(false);
        }

        private static RectTransform BottomChip(Transform parent, string name, float width, float height, float bottom, Color fill)
        {
            RectTransform box = Panel(parent, name, fill);
            box.anchorMin = box.anchorMax = box.pivot = new Vector2(0.5f, 0f);
            box.anchoredPosition = new Vector2(0f, bottom);
            box.sizeDelta = new Vector2(width, height);
            box.GetComponent<Image>().raycastTarget = false;
            return box;
        }

        private static TMP_Text UiText(RectTransform parent, TMP_FontAsset font, string value, float size,
            Color color, float x, float y, float width, float height)
        {
            var text = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(parent, false);
            TopLeft(text.rectTransform, x, y, width, height);
            text.font = font;
            text.text = value;
            text.fontSize = size;
            text.color = color;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.raycastTarget = false;
            return text;
        }

        private static void TopLeft(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
        }
    }
}
