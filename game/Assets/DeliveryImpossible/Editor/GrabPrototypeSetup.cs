using System.Linq;
using DeliveryImpossible.Interaction;
using DeliveryImpossible.Networking;
using DeliveryImpossible.Orders;
using DeliveryImpossible.UI;
using Mirror;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeliveryImpossible.Editor
{
    public static class GrabPrototypeSetup
    {
        private const string RootFolder = "Assets/DeliveryImpossible";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string FontsFolder = RootFolder + "/Fonts";
        private const string PlayerPrefabPath = PrefabsFolder + "/NetworkPlayer.prefab";
        private const string BootstrapPrefabPath = PrefabsFolder + "/NetworkBootstrap.prefab";
        private const string CargoPrefabPath = PrefabsFolder + "/NetworkCargo.prefab";
        private const string HeavyCargoPrefabPath = PrefabsFolder + "/HeavyNetworkCargo.prefab";
        private const string CargoMaterialPath = MaterialsFolder + "/CargoPrototype.mat";
        private const string HeavyCargoMaterialPath = MaterialsFolder + "/HeavyCargoPrototype.mat";
        private const string ObstacleMaterialPath = MaterialsFolder + "/ObstaclePrototype.mat";
        private const string DeliveryZoneMaterialPath = MaterialsFolder + "/DeliveryZonePrototype.mat";
        private const string SourceFontPath = "Assets/TextMesh Pro/Fonts/LiberationSans.ttf";
        private const string DeliveryFontPath = FontsFolder + "/DeliveryFont.asset";
        private const string TestScenePath = RootFolder + "/Scenes/MovementTest.unity";

        [MenuItem("Tools/Delivery Impossible/Setup Grab Prototype")]
        public static void Setup()
        {
            GameObject playerPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            GameObject bootstrapPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath);
            if (playerPrefab == null || bootstrapPrefab == null)
            {
                Debug.LogError("[Delivery Impossible] Run Setup Movement Prototype first.");
                return;
            }

            Material cargoMaterial = GetOrCreateMaterial(
                CargoMaterialPath,
                "Cargo Prototype",
                new Color(0.9f, 0.32f, 0.08f, 1f));
            Material heavyCargoMaterial = GetOrCreateMaterial(
                HeavyCargoMaterialPath,
                "Heavy Cargo Prototype",
                new Color(0.18f, 0.42f, 0.9f, 1f));
            Material obstacleMaterial = GetOrCreateMaterial(
                ObstacleMaterialPath,
                "Obstacle Prototype",
                new Color(0.25f, 0.27f, 0.31f, 1f));
            Material deliveryZoneMaterial = GetOrCreateMaterial(
                DeliveryZoneMaterialPath,
                "Delivery Zone Prototype",
                new Color(0.12f, 0.85f, 0.28f, 1f));
            TMP_FontAsset deliveryFont = GetOrCreateDeliveryFont();
            EnsurePromptCharacters(deliveryFont);
            GameObject cargoPrefab = GetOrCreateCargoPrefab(cargoMaterial);
            GameObject heavyCargoPrefab = GetOrCreateHeavyCargoPrefab(heavyCargoMaterial);
            AddGrabberToPlayerPrefab(deliveryFont);
            RegisterCargoPrefabs(cargoPrefab, heavyCargoPrefab);
            AddCargoToTestScene(
                cargoPrefab,
                heavyCargoPrefab,
                obstacleMaterial,
                deliveryZoneMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Delivery Impossible] Cooperative cargo prototype is ready. Aim at cargo and press E.");
        }

        private static void EnsurePromptCharacters(TMP_FontAsset fontAsset)
        {
            const string promptCharacters =
                "E — взять отпустить Нужен второй игрок Тяжёлый груз схватить помочь нести Несём вдвоём " +
                "ЗАКАЗ заберите пронесите через дверь поставьте в зелёную зону выполнен Новый заказ " +
                "Не трогайте Ожидание заказа 0123456789%:!";

            if (fontAsset == null)
            {
                return;
            }

            if (!fontAsset.TryAddCharacters(promptCharacters, out string missingCharacters))
            {
                Debug.LogWarning(
                    $"[Delivery Impossible] Prompt font is missing characters: {missingCharacters}");
            }

            EditorUtility.SetDirty(fontAsset);
        }

        private static TMP_FontAsset GetOrCreateDeliveryFont()
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(DeliveryFontPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder(FontsFolder))
            {
                AssetDatabase.CreateFolder(RootFolder, "Fonts");
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(SourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError("[Delivery Impossible] TMP LiberationSans source font is missing.");
                return TMP_Settings.defaultFontAsset;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            fontAsset.name = "Delivery Font";
            fontAsset.atlasTexture.name = "Delivery Font Atlas";
            fontAsset.material.name = "Delivery Font Material";
            AssetDatabase.CreateAsset(fontAsset, DeliveryFontPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.SaveAssetIfDirty(fontAsset);
            return fontAsset;
        }

        private static Material GetOrCreateMaterial(string path, string name, Color color)
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                name = name,
                color = color
            };
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject GetOrCreateCargoPrefab(Material cargoMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(CargoPrefabPath);
            if (existing != null)
            {
                UpgradeCargoPrefab();
                return AssetDatabase.LoadAssetAtPath<GameObject>(CargoPrefabPath);
            }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "NetworkCargo";
            root.transform.localScale = new Vector3(1f, 1.2f, 1f);

            try
            {
                root.GetComponent<Renderer>().sharedMaterial = cargoMaterial;

                Rigidbody body = root.AddComponent<Rigidbody>();
                body.mass = 8f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
                body.maxAngularVelocity = 20f;

                root.AddComponent<NetworkIdentity>();
                NetworkGrabbable grabbable = root.AddComponent<NetworkGrabbable>();
                grabbable.syncDirection = SyncDirection.ServerToClient;
                grabbable.syncInterval = 1f / 30f;
                ConfigureGrabbable(grabbable, 2, 1, 1f);

                return PrefabUtility.SaveAsPrefabAsset(root, CargoPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void UpgradeCargoPrefab()
        {
            GameObject root = PrefabUtility.LoadPrefabContents(CargoPrefabPath);
            try
            {
                foreach (NetworkRigidbodyUnreliable networkBody in
                         root.GetComponents<NetworkRigidbodyUnreliable>())
                {
                    Object.DestroyImmediate(networkBody);
                }

                NetworkGrabbable grabbable = root.GetComponent<NetworkGrabbable>();
                grabbable.syncDirection = SyncDirection.ServerToClient;
                grabbable.syncInterval = 1f / 30f;
                ConfigureGrabbable(grabbable, 2, 1, 1f);
                PrefabUtility.SaveAsPrefabAsset(root, CargoPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject GetOrCreateHeavyCargoPrefab(Material material)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(HeavyCargoPrefabPath);
            if (existing != null)
            {
                UpgradeHeavyCargoPrefab(material);
                return AssetDatabase.LoadAssetAtPath<GameObject>(HeavyCargoPrefabPath);
            }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "HeavyNetworkCargo";
            root.transform.localScale = new Vector3(3.2f, 1.1f, 1f);

            try
            {
                root.GetComponent<Renderer>().sharedMaterial = material;

                Rigidbody body = root.AddComponent<Rigidbody>();
                ConfigureHeavyBody(body);

                root.AddComponent<NetworkIdentity>();
                NetworkGrabbable grabbable = root.AddComponent<NetworkGrabbable>();
                grabbable.syncDirection = SyncDirection.ServerToClient;
                grabbable.syncInterval = 1f / 30f;
                ConfigureGrabbable(grabbable, 2, 2, 0.08f);

                return PrefabUtility.SaveAsPrefabAsset(root, HeavyCargoPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void UpgradeHeavyCargoPrefab(Material material)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(HeavyCargoPrefabPath);
            try
            {
                root.transform.localScale = new Vector3(3.2f, 1.1f, 1f);
                root.GetComponent<Renderer>().sharedMaterial = material;
                ConfigureHeavyBody(root.GetComponent<Rigidbody>());

                NetworkGrabbable grabbable = root.GetComponent<NetworkGrabbable>();
                grabbable.syncDirection = SyncDirection.ServerToClient;
                grabbable.syncInterval = 1f / 30f;
                ConfigureGrabbable(grabbable, 2, 2, 0.08f);
                PrefabUtility.SaveAsPrefabAsset(root, HeavyCargoPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureHeavyBody(Rigidbody body)
        {
            body.mass = 45f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 12f;
        }

        private static void ConfigureGrabbable(
            NetworkGrabbable grabbable,
            int maximumHolders,
            int requiredHolders,
            float insufficientStrength)
        {
            SerializedObject serializedGrabbable = new SerializedObject(grabbable);
            serializedGrabbable.FindProperty("maxHolders").intValue = maximumHolders;
            serializedGrabbable.FindProperty("requiredHolders").intValue = requiredHolders;
            serializedGrabbable.FindProperty("insufficientHolderStrength").floatValue = insufficientStrength;
            serializedGrabbable.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void AddGrabberToPlayerPrefab(TMP_FontAsset deliveryFont)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(PlayerPrefabPath);
            try
            {
                Camera camera = root.GetComponentInChildren<Camera>(true);
                NetworkGrabber grabber = root.GetComponent<NetworkGrabber>();
                if (grabber == null)
                {
                    grabber = root.AddComponent<NetworkGrabber>();
                }

                SerializedObject serializedGrabber = new SerializedObject(grabber);
                serializedGrabber.FindProperty("playerCamera").objectReferenceValue = camera;
                CreateOrUpdateHud(root, serializedGrabber, deliveryFont);
                serializedGrabber.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void CreateOrUpdateHud(
            GameObject playerRoot,
            SerializedObject serializedGrabber,
            TMP_FontAsset deliveryFont)
        {
            Transform existingCanvas = playerRoot.transform.Find("InteractionCanvas");
            GameObject canvasObject;
            if (existingCanvas == null)
            {
                canvasObject = new GameObject(
                    "InteractionCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(UnityEngine.UI.CanvasScaler),
                    typeof(UnityEngine.UI.GraphicRaycaster));
                canvasObject.transform.SetParent(playerRoot.transform, false);
            }
            else
            {
                canvasObject = existingCanvas.gameObject;
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvas.enabled = false;

            UnityEngine.UI.CanvasScaler scaler =
                canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.matchWidthOrHeight = 0.5f;

            Transform existingPanel = canvasObject.transform.Find("PromptPanel");
            GameObject panelObject;
            if (existingPanel == null)
            {
                panelObject = new GameObject(
                    "PromptPanel",
                    typeof(RectTransform),
                    typeof(UnityEngine.UI.Image));
                panelObject.transform.SetParent(canvasObject.transform, false);
            }
            else
            {
                panelObject = existingPanel.gameObject;
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(0.5f, 0.16f);
            panelRect.anchorMax = new Vector2(0.5f, 0.16f);
            panelRect.pivot = new Vector2(0.5f, 0.5f);
            panelRect.anchoredPosition = Vector2.zero;
            panelRect.sizeDelta = new Vector2(620f, 110f);

            UnityEngine.UI.Image background = panelObject.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.02f, 0.025f, 0.035f, 0.78f);
            background.raycastTarget = false;

            Transform existingText = panelObject.transform.Find("PromptText");
            GameObject textObject;
            if (existingText == null)
            {
                textObject = new GameObject(
                    "PromptText",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                textObject.transform.SetParent(panelObject.transform, false);
            }
            else
            {
                textObject = existingText.gameObject;
            }

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(18f, 10f);
            textRect.offsetMax = new Vector2(-18f, -10f);

            TextMeshProUGUI prompt = textObject.GetComponent<TextMeshProUGUI>();
            prompt.font = deliveryFont;
            prompt.text = string.Empty;
            prompt.fontSize = 30f;
            prompt.fontStyle = FontStyles.Bold;
            prompt.alignment = TextAlignmentOptions.Center;
            prompt.color = Color.white;
            prompt.raycastTarget = false;

            serializedGrabber.FindProperty("interactionCanvas").objectReferenceValue = canvas;
            serializedGrabber.FindProperty("interactionPanel").objectReferenceValue = panelObject;
            serializedGrabber.FindProperty("interactionPrompt").objectReferenceValue = prompt;

            NetworkOrderHud orderHud = playerRoot.GetComponent<NetworkOrderHud>();
            if (orderHud == null)
            {
                orderHud = playerRoot.AddComponent<NetworkOrderHud>();
            }

            CreateOrUpdateOrderPanel(canvasObject, deliveryFont, out GameObject orderPanel, out TMP_Text orderText);
            SerializedObject serializedOrderHud = new SerializedObject(orderHud);
            serializedOrderHud.FindProperty("hudCanvas").objectReferenceValue = canvas;
            serializedOrderHud.FindProperty("orderPanel").objectReferenceValue = orderPanel;
            serializedOrderHud.FindProperty("orderText").objectReferenceValue = orderText;
            serializedOrderHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateOrUpdateOrderPanel(
            GameObject canvasObject,
            TMP_FontAsset deliveryFont,
            out GameObject panelObject,
            out TMP_Text orderText)
        {
            Transform existingPanel = canvasObject.transform.Find("OrderPanel");
            if (existingPanel == null)
            {
                panelObject = new GameObject(
                    "OrderPanel",
                    typeof(RectTransform),
                    typeof(UnityEngine.UI.Image));
                panelObject.transform.SetParent(canvasObject.transform, false);
            }
            else
            {
                panelObject = existingPanel.gameObject;
            }

            RectTransform panelRect = panelObject.GetComponent<RectTransform>();
            panelRect.anchorMin = new Vector2(1f, 1f);
            panelRect.anchorMax = new Vector2(1f, 1f);
            panelRect.pivot = new Vector2(1f, 1f);
            panelRect.anchoredPosition = new Vector2(-32f, -32f);
            panelRect.sizeDelta = new Vector2(640f, 128f);

            UnityEngine.UI.Image background = panelObject.GetComponent<UnityEngine.UI.Image>();
            background.color = new Color(0.02f, 0.025f, 0.035f, 0.82f);
            background.raycastTarget = false;

            Transform existingText = panelObject.transform.Find("OrderText");
            GameObject textObject;
            if (existingText == null)
            {
                textObject = new GameObject(
                    "OrderText",
                    typeof(RectTransform),
                    typeof(TextMeshProUGUI));
                textObject.transform.SetParent(panelObject.transform, false);
            }
            else
            {
                textObject = existingText.gameObject;
            }

            RectTransform textRect = textObject.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(22f, 14f);
            textRect.offsetMax = new Vector2(-22f, -14f);

            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = deliveryFont;
            text.text = "ЗАКАЗ: заберите тяжёлый груз";
            text.fontSize = 28f;
            text.fontStyle = FontStyles.Bold;
            text.alignment = TextAlignmentOptions.MidlineLeft;
            text.color = Color.white;
            text.raycastTarget = false;
            panelObject.SetActive(true);
            orderText = text;
        }

        private static void RegisterCargoPrefabs(params GameObject[] cargoPrefabs)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BootstrapPrefabPath);
            try
            {
                NetworkManager manager = root.GetComponent<NetworkManager>();
                if (root.GetComponent<DevelopmentNetworkLauncher>() == null)
                {
                    root.AddComponent<DevelopmentNetworkLauncher>();
                }

                manager.spawnPrefabs.RemoveAll(prefab => prefab == null);
                foreach (GameObject cargoPrefab in cargoPrefabs)
                {
                    if (!manager.spawnPrefabs.Contains(cargoPrefab))
                    {
                        manager.spawnPrefabs.Add(cargoPrefab);
                    }
                }

                PrefabUtility.SaveAsPrefabAsset(root, BootstrapPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddCargoToTestScene(
            GameObject cargoPrefab,
            GameObject heavyCargoPrefab,
            Material obstacleMaterial,
            Material deliveryZoneMaterial)
        {
            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene testScene = EditorSceneManager.GetSceneByPath(TestScenePath);
            bool openedForSetup = !testScene.IsValid() || !testScene.isLoaded;
            if (openedForSetup)
            {
                testScene = EditorSceneManager.OpenScene(TestScenePath, OpenSceneMode.Additive);
            }

            try
            {
                SceneManager.SetActiveScene(testScene);
                GameObject cargo = testScene.GetRootGameObjects()
                    .FirstOrDefault(item => item.name == "Test Network Cargo");

                if (cargo == null)
                {
                    cargo = (GameObject)PrefabUtility.InstantiatePrefab(cargoPrefab, testScene);
                    cargo.name = "Test Network Cargo";
                }

                cargo.transform.SetPositionAndRotation(
                    new Vector3(-1.5f, 0.65f, 3f),
                    Quaternion.Euler(0f, 20f, 0f));

                GameObject heavyCargo = testScene.GetRootGameObjects()
                    .FirstOrDefault(item => item.name == "Test Heavy Network Cargo");
                if (heavyCargo == null)
                {
                    heavyCargo = (GameObject)PrefabUtility.InstantiatePrefab(
                        heavyCargoPrefab,
                        testScene);
                    heavyCargo.name = "Test Heavy Network Cargo";
                }

                heavyCargo.transform.SetPositionAndRotation(
                    new Vector3(0f, 0.6f, 6.5f),
                    Quaternion.identity);

                CreateOrUpdateObstacle(
                    testScene,
                    "Doorway Left",
                    new Vector3(-3.25f, 1.5f, 10f),
                    new Vector3(5f, 3f, 0.5f),
                    obstacleMaterial);
                CreateOrUpdateObstacle(
                    testScene,
                    "Doorway Right",
                    new Vector3(3.25f, 1.5f, 10f),
                    new Vector3(5f, 3f, 0.5f),
                    obstacleMaterial);
                CreateOrUpdateObstacle(
                    testScene,
                    "Doorway Header",
                    new Vector3(0f, 2.75f, 10f),
                    new Vector3(1.5f, 0.5f, 0.5f),
                    obstacleMaterial);

                DeliveryZone deliveryZone = CreateOrUpdateDeliveryZone(
                    testScene,
                    deliveryZoneMaterial);
                CreateOrUpdateFirstOrder(testScene, heavyCargo, deliveryZone);
                EditorSceneManager.MarkSceneDirty(testScene);
                EditorSceneManager.SaveScene(testScene);
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                if (openedForSetup && testScene.IsValid() && testScene.isLoaded)
                {
                    EditorSceneManager.CloseScene(testScene, true);
                }
            }
        }

        private static DeliveryZone CreateOrUpdateDeliveryZone(Scene scene, Material material)
        {
            GameObject zoneObject = scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "Delivery Zone");
            if (zoneObject == null)
            {
                zoneObject = new GameObject("Delivery Zone");
                SceneManager.MoveGameObjectToScene(zoneObject, scene);
            }

            zoneObject.transform.SetPositionAndRotation(
                new Vector3(0f, 0f, 14f),
                Quaternion.identity);

            BoxCollider zoneCollider = zoneObject.GetComponent<BoxCollider>();
            if (zoneCollider == null)
            {
                zoneCollider = zoneObject.AddComponent<BoxCollider>();
            }

            zoneCollider.isTrigger = true;
            zoneCollider.center = new Vector3(0f, 1.25f, 0f);
            zoneCollider.size = new Vector3(5f, 2.5f, 4f);

            DeliveryZone deliveryZone = zoneObject.GetComponent<DeliveryZone>();
            if (deliveryZone == null)
            {
                deliveryZone = zoneObject.AddComponent<DeliveryZone>();
            }

            SerializedObject serializedZone = new SerializedObject(deliveryZone);
            serializedZone.FindProperty("zoneCollider").objectReferenceValue = zoneCollider;
            serializedZone.ApplyModifiedPropertiesWithoutUndo();

            Transform visualTransform = zoneObject.transform.Find("Zone Visual");
            GameObject visual;
            if (visualTransform == null)
            {
                visual = GameObject.CreatePrimitive(PrimitiveType.Cube);
                visual.name = "Zone Visual";
                visual.transform.SetParent(zoneObject.transform, false);
                Object.DestroyImmediate(visual.GetComponent<Collider>());
            }
            else
            {
                visual = visualTransform.gameObject;
            }

            visual.transform.localPosition = new Vector3(0f, 0.025f, 0f);
            visual.transform.localRotation = Quaternion.identity;
            visual.transform.localScale = new Vector3(5f, 0.05f, 4f);
            visual.GetComponent<Renderer>().sharedMaterial = material;
            return deliveryZone;
        }

        private static void CreateOrUpdateFirstOrder(
            Scene scene,
            GameObject heavyCargo,
            DeliveryZone deliveryZone)
        {
            GameObject orderObject = scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "First Delivery Order");
            if (orderObject == null)
            {
                orderObject = new GameObject("First Delivery Order");
                SceneManager.MoveGameObjectToScene(orderObject, scene);
            }

            if (orderObject.GetComponent<NetworkIdentity>() == null)
            {
                orderObject.AddComponent<NetworkIdentity>();
            }

            NetworkDeliveryOrder order = orderObject.GetComponent<NetworkDeliveryOrder>();
            if (order == null)
            {
                order = orderObject.AddComponent<NetworkDeliveryOrder>();
            }

            SerializedObject serializedOrder = new SerializedObject(order);
            serializedOrder.FindProperty("targetCargo").objectReferenceValue =
                heavyCargo.GetComponent<NetworkGrabbable>();
            serializedOrder.FindProperty("deliveryZone").objectReferenceValue = deliveryZone;
            serializedOrder.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateOrUpdateObstacle(
            Scene scene,
            string name,
            Vector3 position,
            Vector3 scale,
            Material material)
        {
            GameObject obstacle = scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == name);
            if (obstacle == null)
            {
                obstacle = GameObject.CreatePrimitive(PrimitiveType.Cube);
                obstacle.name = name;
                SceneManager.MoveGameObjectToScene(obstacle, scene);
            }

            obstacle.transform.SetPositionAndRotation(position, Quaternion.identity);
            obstacle.transform.localScale = scale;
            obstacle.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
