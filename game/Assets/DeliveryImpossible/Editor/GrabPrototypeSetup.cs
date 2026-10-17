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
using DeliveryLobbyPlayer = DeliveryImpossible.Networking.NetworkLobbyPlayer;

namespace DeliveryImpossible.Editor
{
    public static class GrabPrototypeSetup
    {
        private const string RootFolder = "Assets/DeliveryImpossible";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string FontsFolder = RootFolder + "/Fonts";
        private const string UiFolder = RootFolder + "/UI";
        private const string MenuBackgroundPath = UiFolder + "/MainMenuBackground.png";
        private const string PlayerPrefabPath = PrefabsFolder + "/NetworkPlayer.prefab";
        private const string BootstrapPrefabPath = PrefabsFolder + "/NetworkBootstrap.prefab";
        private const string CargoPrefabPath = PrefabsFolder + "/NetworkCargo.prefab";
        private const string HeavyCargoPrefabPath = PrefabsFolder + "/HeavyNetworkCargo.prefab";
        private const string FragileCargoPrefabPath = PrefabsFolder + "/FragileTelevision.prefab";
        private const string CargoMaterialPath = MaterialsFolder + "/CargoPrototype.mat";
        private const string HeavyCargoMaterialPath = MaterialsFolder + "/HeavyCargoPrototype.mat";
        private const string FragileBodyMaterialPath = MaterialsFolder + "/FragileBodyPrototype.mat";
        private const string FragileScreenMaterialPath = MaterialsFolder + "/FragileScreenPrototype.mat";
        private const string FragileDamageMaterialPath = MaterialsFolder + "/FragileDamagePrototype.mat";
        private const string ObstacleMaterialPath = MaterialsFolder + "/ObstaclePrototype.mat";
        private const string DeliveryZoneMaterialPath = MaterialsFolder + "/DeliveryZonePrototype.mat";
        private const string OswaldSourceFontPath = FontsFolder + "/Source/Oswald-Variable.ttf";
        private const string RubikSourceFontPath = FontsFolder + "/Source/Rubik-Variable.ttf";
        private const string OswaldFontPath = FontsFolder + "/DeliveryOswald.asset";
        private const string RubikFontPath = FontsFolder + "/DeliveryRubik.asset";
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
            Material fragileBodyMaterial = GetOrCreateMaterial(
                FragileBodyMaterialPath,
                "Fragile Television Body",
                new Color(0.055f, 0.06f, 0.07f, 1f));
            Material fragileScreenMaterial = GetOrCreateMaterial(
                FragileScreenMaterialPath,
                "Fragile Television Screen",
                new Color(0.05f, 0.25f, 0.35f, 1f));
            Material fragileDamageMaterial = GetOrCreateMaterial(
                FragileDamageMaterialPath,
                "Fragile Television Damage",
                new Color(0.9f, 0.08f, 0.025f, 1f));
            Material obstacleMaterial = GetOrCreateMaterial(
                ObstacleMaterialPath,
                "Obstacle Prototype",
                new Color(0.25f, 0.27f, 0.31f, 1f));
            Material deliveryZoneMaterial = GetOrCreateMaterial(
                DeliveryZoneMaterialPath,
                "Delivery Zone Prototype",
                new Color(0.12f, 0.85f, 0.28f, 1f));
            TMP_FontAsset headingFont = GetOrCreateFontAsset(
                OswaldSourceFontPath,
                OswaldFontPath,
                "Delivery Oswald");
            TMP_FontAsset bodyFont = GetOrCreateFontAsset(
                RubikSourceFontPath,
                RubikFontPath,
                "Delivery Rubik");
            EnsureHudCharacters(headingFont);
            EnsureHudCharacters(bodyFont);
            Sprite menuBackground = GetMenuBackgroundSprite();
            GameObject cargoPrefab = GetOrCreateCargoPrefab(cargoMaterial);
            GameObject heavyCargoPrefab = GetOrCreateHeavyCargoPrefab(heavyCargoMaterial);
            GameObject fragileCargoPrefab = GetOrCreateFragileCargoPrefab(
                fragileBodyMaterial,
                fragileScreenMaterial,
                fragileDamageMaterial);
            AddGrabberToPlayerPrefab(headingFont, bodyFont);
            AddLobbyUiToBootstrapPrefab(headingFont, bodyFont, menuBackground);
            RegisterCargoPrefabs(cargoPrefab, heavyCargoPrefab, fragileCargoPrefab);
            AddCargoToTestScene(
                cargoPrefab,
                heavyCargoPrefab,
                fragileCargoPrefab,
                obstacleMaterial,
                deliveryZoneMaterial);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Delivery Impossible] Cooperative cargo prototype is ready. Aim at cargo and press E.");
        }

        private static void EnsureHudCharacters(TMP_FontAsset fontAsset)
        {
            const string promptCharacters =
                "E — взять отпустить Нужен второй игрок Тяжёлый груз схватить помочь нести Несём вдвоём " +
                "ЗАКАЗ заберите пронесите через дверь поставьте в зелёную зону выполнен Новый заказ " +
                "Не трогайте Ожидание заказа Подключение диспетчерской Напарник В сети " +
                "держит хват стабильность отличная помощь принято приёмка тяжёлое новый сек " +
                "дело транспортная накладная категория груза текущая цель маршрутный лист " +
                "сдано работе поставить забрать доставить балансируйте вместе подойдите нажмите " +
                "перенести предмет груз будет брошен свою сторону ожидаем подключения " +
                "доставьте внутрь разметки падения повтор целостность разбит 0123456789%:!№—.«»";

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

        private static TMP_FontAsset GetOrCreateFontAsset(
            string sourceFontPath,
            string destinationPath,
            string assetName)
        {
            TMP_FontAsset existing = AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(destinationPath);
            if (existing != null)
            {
                return existing;
            }

            if (!AssetDatabase.IsValidFolder(FontsFolder))
            {
                AssetDatabase.CreateFolder(RootFolder, "Fonts");
            }

            Font sourceFont = AssetDatabase.LoadAssetAtPath<Font>(sourceFontPath);
            if (sourceFont == null)
            {
                Debug.LogError($"[Delivery Impossible] Source font is missing: {sourceFontPath}");
                return TMP_Settings.defaultFontAsset;
            }

            TMP_FontAsset fontAsset = TMP_FontAsset.CreateFontAsset(sourceFont);
            fontAsset.name = assetName;
            fontAsset.atlasTexture.name = assetName + " Atlas";
            fontAsset.material.name = assetName + " Material";
            AssetDatabase.CreateAsset(fontAsset, destinationPath);
            AssetDatabase.AddObjectToAsset(fontAsset.atlasTexture, fontAsset);
            AssetDatabase.AddObjectToAsset(fontAsset.material, fontAsset);
            AssetDatabase.SaveAssetIfDirty(fontAsset);
            return fontAsset;
        }

        private static Sprite GetMenuBackgroundSprite()
        {
            TextureImporter importer = AssetImporter.GetAtPath(MenuBackgroundPath) as TextureImporter;
            if (importer == null)
            {
                AssetDatabase.ImportAsset(MenuBackgroundPath, ImportAssetOptions.ForceUpdate);
                importer = AssetImporter.GetAtPath(MenuBackgroundPath) as TextureImporter;
            }

            if (importer == null)
            {
                Debug.LogWarning($"[Delivery Impossible] Menu background is missing: {MenuBackgroundPath}");
                return null;
            }

            bool changed = importer.textureType != TextureImporterType.Sprite ||
                           importer.mipmapEnabled ||
                           importer.maxTextureSize != 2048;
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = false;
            importer.maxTextureSize = 2048;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            if (changed)
            {
                importer.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(MenuBackgroundPath);
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

        private static GameObject GetOrCreateFragileCargoPrefab(
            Material bodyMaterial,
            Material screenMaterial,
            Material damageMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(FragileCargoPrefabPath);
            if (existing != null)
            {
                UpgradeFragileCargoPrefab(bodyMaterial, screenMaterial, damageMaterial);
                return AssetDatabase.LoadAssetAtPath<GameObject>(FragileCargoPrefabPath);
            }

            GameObject root = GameObject.CreatePrimitive(PrimitiveType.Cube);
            root.name = "FragileTelevision";

            try
            {
                ConfigureFragileCargo(root, bodyMaterial, screenMaterial, damageMaterial);
                return PrefabUtility.SaveAsPrefabAsset(root, FragileCargoPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void UpgradeFragileCargoPrefab(
            Material bodyMaterial,
            Material screenMaterial,
            Material damageMaterial)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(FragileCargoPrefabPath);
            try
            {
                ConfigureFragileCargo(root, bodyMaterial, screenMaterial, damageMaterial);
                PrefabUtility.SaveAsPrefabAsset(root, FragileCargoPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void ConfigureFragileCargo(
            GameObject root,
            Material bodyMaterial,
            Material screenMaterial,
            Material damageMaterial)
        {
            root.transform.localScale = new Vector3(1.8f, 1.15f, 0.32f);
            root.GetComponent<Renderer>().sharedMaterial = bodyMaterial;

            Rigidbody body = root.GetComponent<Rigidbody>();
            if (body == null)
            {
                body = root.AddComponent<Rigidbody>();
            }

            body.mass = 12f;
            body.interpolation = RigidbodyInterpolation.Interpolate;
            body.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            body.maxAngularVelocity = 16f;

            if (root.GetComponent<NetworkIdentity>() == null)
            {
                root.AddComponent<NetworkIdentity>();
            }

            NetworkGrabbable grabbable = root.GetComponent<NetworkGrabbable>();
            if (grabbable == null)
            {
                grabbable = root.AddComponent<NetworkGrabbable>();
            }

            grabbable.syncDirection = SyncDirection.ServerToClient;
            grabbable.syncInterval = 1f / 30f;
            ConfigureGrabbable(grabbable, 2, 1, 1f);

            GameObject screen = GetOrCreateVisualCube(
                root.transform,
                "Screen",
                new Vector3(0f, 0.03f, -0.54f),
                new Vector3(0.88f, 0.77f, 0.08f),
                Quaternion.identity,
                screenMaterial);

            Transform existingDamage = root.transform.Find("Damage Visual");
            GameObject damageVisual = existingDamage != null
                ? existingDamage.gameObject
                : new GameObject("Damage Visual");
            damageVisual.transform.SetParent(root.transform, false);
            damageVisual.transform.localPosition = Vector3.zero;
            damageVisual.transform.localRotation = Quaternion.identity;
            damageVisual.transform.localScale = Vector3.one;

            GetOrCreateVisualCube(
                damageVisual.transform,
                "Crack A",
                new Vector3(0f, 0.03f, -0.6f),
                new Vector3(0.72f, 0.035f, 0.025f),
                Quaternion.Euler(0f, 0f, 32f),
                damageMaterial);
            GetOrCreateVisualCube(
                damageVisual.transform,
                "Crack B",
                new Vector3(0.08f, 0.02f, -0.61f),
                new Vector3(0.58f, 0.03f, 0.025f),
                Quaternion.Euler(0f, 0f, -38f),
                damageMaterial);
            damageVisual.SetActive(false);

            NetworkFragileCargo fragileCargo = root.GetComponent<NetworkFragileCargo>();
            if (fragileCargo == null)
            {
                fragileCargo = root.AddComponent<NetworkFragileCargo>();
            }

            SerializedObject serializedFragile = new SerializedObject(fragileCargo);
            SerializedProperty renderers = serializedFragile.FindProperty("damageRenderers");
            renderers.arraySize = 1;
            renderers.GetArrayElementAtIndex(0).objectReferenceValue = screen.GetComponent<Renderer>();
            serializedFragile.FindProperty("damageVisual").objectReferenceValue = damageVisual;
            serializedFragile.ApplyModifiedPropertiesWithoutUndo();
        }

        private static GameObject GetOrCreateVisualCube(
            Transform parent,
            string name,
            Vector3 localPosition,
            Vector3 localScale,
            Quaternion localRotation,
            Material material)
        {
            Transform existing = parent.Find(name);
            GameObject visual = existing != null
                ? existing.gameObject
                : GameObject.CreatePrimitive(PrimitiveType.Cube);
            visual.name = name;
            visual.transform.SetParent(parent, false);
            visual.transform.localPosition = localPosition;
            visual.transform.localRotation = localRotation;
            visual.transform.localScale = localScale;
            visual.GetComponent<Renderer>().sharedMaterial = material;

            Collider visualCollider = visual.GetComponent<Collider>();
            if (visualCollider != null)
            {
                Object.DestroyImmediate(visualCollider);
            }

            return visual;
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

        private static void AddGrabberToPlayerPrefab(
            TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont)
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

                if (root.GetComponent<DeliveryLobbyPlayer>() == null)
                {
                    root.AddComponent<DeliveryLobbyPlayer>();
                }

                SerializedObject serializedGrabber = new SerializedObject(grabber);
                serializedGrabber.FindProperty("playerCamera").objectReferenceValue = camera;
                CreateOrUpdateHud(root, serializedGrabber, headingFont, bodyFont);
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
            TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont)
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

            for (int index = canvasObject.transform.childCount - 1; index >= 0; index--)
            {
                Object.DestroyImmediate(canvasObject.transform.GetChild(index).gameObject);
            }

            Canvas canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 20;
            canvas.enabled = false;

            UnityEngine.UI.CanvasScaler scaler =
                canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
            scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920f, 1080f);
            scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            CreateReticle(canvasObject.transform);
            CreatePromptPanel(
                canvasObject.transform,
                headingFont,
                bodyFont,
                out GameObject promptPanel,
                out TMP_Text promptText,
                out TMP_Text promptHint);
            CreateCooperationWarning(
                canvasObject.transform,
                headingFont,
                bodyFont,
                out GameObject warningPanel,
                out TMP_Text warningText);
            CreatePlacementPanel(
                canvasObject.transform,
                headingFont,
                bodyFont,
                out GameObject placementPanel,
                out TMP_Text placementText,
                out RectTransform placementFill);
            CreateOrderManifest(
                canvasObject.transform,
                headingFont,
                bodyFont,
                out GameObject orderPanel,
                out TMP_Text orderTitle,
                out TMP_Text objectiveText,
                out TMP_Text objectiveHint,
                out TMP_Text orderStamp,
                out UnityEngine.UI.Image[] stageBackgrounds,
                out TMP_Text[] stageStatuses);

            serializedGrabber.FindProperty("interactionCanvas").objectReferenceValue = canvas;
            serializedGrabber.FindProperty("interactionPanel").objectReferenceValue = promptPanel;
            serializedGrabber.FindProperty("interactionPrompt").objectReferenceValue = promptText;
            serializedGrabber.FindProperty("interactionHint").objectReferenceValue = promptHint;
            serializedGrabber.FindProperty("cooperationWarningPanel").objectReferenceValue = warningPanel;
            serializedGrabber.FindProperty("cooperationWarningText").objectReferenceValue = warningText;

            NetworkOrderHud orderHud = playerRoot.GetComponent<NetworkOrderHud>();
            if (orderHud == null)
            {
                orderHud = playerRoot.AddComponent<NetworkOrderHud>();
            }

            SerializedObject serializedOrderHud = new SerializedObject(orderHud);
            serializedOrderHud.FindProperty("hudCanvas").objectReferenceValue = canvas;
            serializedOrderHud.FindProperty("orderPanel").objectReferenceValue = orderPanel;
            serializedOrderHud.FindProperty("orderTitleText").objectReferenceValue = orderTitle;
            serializedOrderHud.FindProperty("objectiveText").objectReferenceValue = objectiveText;
            serializedOrderHud.FindProperty("objectiveHintText").objectReferenceValue = objectiveHint;
            serializedOrderHud.FindProperty("orderStampText").objectReferenceValue = orderStamp;
            serializedOrderHud.FindProperty("stageOneBackground").objectReferenceValue = stageBackgrounds[0];
            serializedOrderHud.FindProperty("stageTwoBackground").objectReferenceValue = stageBackgrounds[1];
            serializedOrderHud.FindProperty("stageThreeBackground").objectReferenceValue = stageBackgrounds[2];
            serializedOrderHud.FindProperty("stageOneStatus").objectReferenceValue = stageStatuses[0];
            serializedOrderHud.FindProperty("stageTwoStatus").objectReferenceValue = stageStatuses[1];
            serializedOrderHud.FindProperty("stageThreeStatus").objectReferenceValue = stageStatuses[2];
            serializedOrderHud.FindProperty("placementPanel").objectReferenceValue = placementPanel;
            serializedOrderHud.FindProperty("placementText").objectReferenceValue = placementText;
            serializedOrderHud.FindProperty("placementFill").objectReferenceValue = placementFill;
            serializedOrderHud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void CreateReticle(Transform parent)
        {
            GameObject root = CreateEmptyUi(
                "Reticle", parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, new Vector2(42f, 42f));
            Color tickColor = new Color(1f, 1f, 1f, 0.92f);
            CreatePanel("TopLeftHorizontal", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(12f, 3f), tickColor, false);
            CreatePanel("TopLeftVertical", root.transform, new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero, new Vector2(3f, 12f), tickColor, false);
            CreatePanel("TopRightHorizontal", root.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(12f, 3f), tickColor, false);
            CreatePanel("TopRightVertical", root.transform, new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f), Vector2.zero, new Vector2(3f, 12f), tickColor, false);
            CreatePanel("BottomLeftHorizontal", root.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(12f, 3f), tickColor, false);
            CreatePanel("BottomLeftVertical", root.transform, Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero, new Vector2(3f, 12f), tickColor, false);
            CreatePanel("BottomRightHorizontal", root.transform, Vector2.right, Vector2.right, Vector2.right, Vector2.zero, new Vector2(12f, 3f), tickColor, false);
            CreatePanel("BottomRightVertical", root.transform, Vector2.right, Vector2.right, Vector2.right, Vector2.zero, new Vector2(3f, 12f), tickColor, false);
            CreatePanel("CenterDot", root.transform, new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(6f, 6f), Color.white, false);
        }

        private static void CreatePromptPanel(
            Transform parent,
            TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont,
            out GameObject panel,
            out TMP_Text actionText,
            out TMP_Text hintText)
        {
            panel = CreatePanel(
                "PromptPanel", parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 62f), new Vector2(350f, 72f),
                new Color32(11, 15, 17, 250), true);
            AddOutline(panel, new Color32(91, 96, 103, 255), new Vector2(2f, -2f));
            GameObject key = CreatePanel(
                "Keycap", panel.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(16f, 0f), new Vector2(48f, 48f),
                new Color32(235, 235, 232, 255), false);
            AddShadow(key, new Color32(120, 123, 126, 255), new Vector2(0f, -6f));
            CreateText(
                "Key", key.transform, headingFont, "E", 28f,
                new Color32(20, 24, 28, 255), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, FontStyles.Bold);
            actionText = CreateText(
                "Action", panel.transform, headingFont, "ВЗЯТЬ ГРУЗ", 23f,
                Color.white, TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(82f, 10f), new Vector2(-16f, 31f), FontStyles.Bold);
            hintText = CreateText(
                "Hint", panel.transform, bodyFont, "ПЕРЕНЕСТИ ПРЕДМЕТ", 10f,
                new Color32(155, 160, 167, 255), TextAlignmentOptions.Left,
                new Vector2(0f, 0.5f), new Vector2(1f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(82f, -17f), new Vector2(-16f, 21f), FontStyles.Normal);
            panel.SetActive(false);
        }

        private static void CreateCooperationWarning(
            Transform parent,
            TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont,
            out GameObject panel,
            out TMP_Text warningText)
        {
            panel = CreatePanel(
                "CooperationWarning", parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 148f), new Vector2(470f, 36f),
                new Color32(18, 20, 23, 250), true);
            AddOutline(panel, new Color32(245, 158, 11, 255), new Vector2(2f, -2f));
            CreatePanel(
                "WarningIcon", panel.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(8f, 0f), new Vector2(22f, 22f),
                new Color32(245, 158, 11, 255), false);
            CreateText(
                "Exclamation", panel.transform, headingFont, "!", 20f,
                new Color32(20, 20, 20, 255), TextAlignmentOptions.Center,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(8f, 0f), new Vector2(22f, 22f), FontStyles.Bold);
            warningText = CreateText(
                "WarningText", panel.transform, headingFont,
                "НУЖЕН ВТОРОЙ ИГРОК  |  ТЯЖЁЛЫЙ ГРУЗ", 15f,
                new Color32(245, 158, 11, 255), TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(1f, 1f), new Vector2(0f, 0.5f),
                new Vector2(42f, 0f), new Vector2(-12f, 0f), FontStyles.Bold);
            panel.SetActive(false);
        }

        private static void CreatePlacementPanel(
            Transform parent,
            TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont,
            out GameObject panel,
            out TMP_Text placementText,
            out RectTransform placementFill)
        {
            panel = CreatePanel(
                "PlacementPanel", parent,
                new Vector2(0.5f, 0f), new Vector2(0.5f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 54f), new Vector2(520f, 70f),
                new Color32(27, 31, 36, 250), true);
            AddOutline(panel, new Color32(70, 76, 83, 255), new Vector2(2f, -2f));
            placementText = CreateText(
                "PlacementText", panel.transform, headingFont,
                "НЕ ТРОГАЙТЕ ГРУЗ — 68%", 16f,
                new Color32(230, 232, 235, 255), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0.7f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -8f), new Vector2(-4f, 24f), FontStyles.Bold);
            CreateText(
                "BalancingText", panel.transform, bodyFont,
                "БАЛАНСИРОВКА...", 11f,
                new Color32(255, 85, 0, 255), TextAlignmentOptions.Right,
                new Vector2(0.7f, 1f), Vector2.one, new Vector2(1f, 1f),
                new Vector2(-14f, -8f), new Vector2(-4f, 24f), FontStyles.Bold);
            GameObject track = CreatePanel(
                "Track", panel.transform,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0.5f, 0f),
                new Vector2(0f, 12f), new Vector2(-28f, 18f),
                new Color32(10, 12, 14, 255), false);
            AddOutline(track, new Color32(92, 96, 101, 255), new Vector2(1f, -1f));
            GameObject fill = CreatePanel(
                "Fill", track.transform,
                Vector2.zero, new Vector2(0.68f, 1f), new Vector2(0f, 0.5f),
                new Vector2(3f, 0f), new Vector2(-6f, -6f),
                new Color32(255, 85, 0, 255), false);
            fill.AddComponent<UnityEngine.UI.RectMask2D>();
            placementFill = fill.GetComponent<RectTransform>();
            for (int index = 0; index < 18; index++)
            {
                GameObject stripe = CreatePanel(
                    $"Stripe {index + 1}", fill.transform,
                    new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                    new Vector2(index * 42f - 10f, 0f), new Vector2(15f, 45f),
                    new Color32(20, 23, 26, 255), false);
                stripe.transform.localRotation = Quaternion.Euler(0f, 0f, -35f);
            }
            panel.SetActive(false);
        }

        private static void CreateOrderManifest(
            Transform parent,
            TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont,
            out GameObject panel,
            out TMP_Text orderTitle,
            out TMP_Text objective,
            out TMP_Text objectiveHint,
            out TMP_Text stamp,
            out UnityEngine.UI.Image[] stageBackgrounds,
            out TMP_Text[] stageStatuses)
        {
            Color paper = new Color32(228, 222, 212, 255);
            Color ink = new Color32(20, 23, 26, 255);
            panel = CreatePanel(
                "OrderPanel", parent,
                Vector2.one, Vector2.one, Vector2.one,
                new Vector2(-48f, -48f), new Vector2(480f, 244f),
                paper, true);
            AddOutline(panel, new Color32(42, 45, 48, 255), new Vector2(2f, -2f));
            GameObject header = CreatePanel(
                "ManifestHeader", panel.transform,
                new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 42f),
                new Color32(36, 38, 40, 255), false);
            CreatePanel(
                "BoxIcon", header.transform,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(16f, 0f), new Vector2(13f, 13f),
                new Color32(255, 85, 0, 255), false);
            CreateText(
                "ManifestTitle", header.transform, headingFont,
                "МАРШРУТ ДОСТАВКИ", 17f,
                Color.white, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
                new Vector2(40f, 0f), new Vector2(-120f, 0f), FontStyles.Bold);
            CreateText(
                "ManifestId", header.transform, bodyFont,
                "ДЕЛО 408-B", 10f,
                new Color32(165, 168, 172, 255), TextAlignmentOptions.Right,
                Vector2.zero, Vector2.one, new Vector2(1f, 0.5f),
                new Vector2(-14f, 0f), new Vector2(-4f, 0f), FontStyles.Normal);
            orderTitle = CreateText(
                "OrderTitle", panel.transform, headingFont,
                "ТЯЖЁЛЫЙ ГРУЗ", 20f,
                ink, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0.72f, 1f), new Vector2(0f, 1f),
                new Vector2(16f, -50f), new Vector2(-4f, 28f), FontStyles.Bold);
            GameObject stampPanel = CreatePanel(
                "Stamp", panel.transform,
                new Vector2(1f, 1f), new Vector2(1f, 1f), new Vector2(1f, 1f),
                new Vector2(-14f, -49f), new Vector2(96f, 25f),
                new Color(0f, 0f, 0f, 0f), false);
            AddOutline(stampPanel, new Color32(197, 40, 24, 255), new Vector2(2f, -2f));
            stamp = CreateText(
                "StampText", stampPanel.transform, headingFont,
                "ТЯЖЁЛОЕ", 13f,
                new Color32(197, 40, 24, 255), TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, FontStyles.Bold);
            GameObject objectivePanel = CreatePanel(
                "ObjectivePanel", panel.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -82f), new Vector2(-28f, 82f),
                new Color32(20, 20, 20, 255), false);
            CreatePanel(
                "ObjectiveRib", objectivePanel.transform,
                Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(5f, 0f),
                new Color32(255, 85, 0, 255), false);
            CreateText(
                "ObjectiveLabel", objectivePanel.transform, bodyFont,
                "ТЕКУЩАЯ ЦЕЛЬ:", 10f,
                new Color32(160, 163, 167, 255), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                new Vector2(16f, -7f), new Vector2(-10f, 16f), FontStyles.Bold);
            objective = CreateText(
                "ObjectiveText", objectivePanel.transform, headingFont,
                "ЗАБЕРИТЕ ТЯЖЁЛЫЙ ГРУЗ", 19f,
                Color.white, TextAlignmentOptions.Left,
                new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                new Vector2(16f, -25f), new Vector2(-10f, 28f), FontStyles.Bold);
            objectiveHint = CreateText(
                "ObjectiveHint", objectivePanel.transform, bodyFont,
                "Подойдите к грузу и нажмите E.", 12f,
                new Color32(202, 205, 208, 255), TextAlignmentOptions.Left,
                new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                new Vector2(16f, 8f), new Vector2(-10f, 20f), FontStyles.Normal);
            stageBackgrounds = new UnityEngine.UI.Image[3];
            stageStatuses = new TMP_Text[3];
            string[] stageNames = { "ЗАБРАТЬ", "ДОНЕСТИ", "ПОСТАВИТЬ" };
            for (int index = 0; index < 3; index++)
            {
                float x = 14f + index * 151f;
                GameObject stage = CreatePanel(
                    $"Stage {index + 1}", panel.transform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(x, -178f), new Vector2(140f, 48f),
                    index == 0 ? new Color32(255, 85, 0, 255) : new Color32(222, 216, 206, 255),
                    index == 0);
                AddOutline(stage, new Color32(115, 116, 116, 255), new Vector2(1f, -1f));
                stageBackgrounds[index] = stage.GetComponent<UnityEngine.UI.Image>();
                CreateText(
                    "Number", stage.transform, bodyFont, $"0{index + 1}", 9f,
                    ink, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), new Vector2(0.35f, 1f), new Vector2(0f, 1f),
                    new Vector2(8f, -4f), new Vector2(-2f, 14f), FontStyles.Bold);
                stageStatuses[index] = CreateText(
                    "Status", stage.transform, bodyFont,
                    index == 0 ? "В РАБОТЕ" : "ОЖИДАЕТ", 8f,
                    ink, TextAlignmentOptions.Right,
                    new Vector2(0.35f, 1f), Vector2.one, new Vector2(1f, 1f),
                    new Vector2(-7f, -4f), new Vector2(-2f, 14f), FontStyles.Bold);
                CreateText(
                    "Name", stage.transform, headingFont, stageNames[index], 14f,
                    ink, TextAlignmentOptions.Left,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                    new Vector2(8f, 5f), new Vector2(-6f, 22f), FontStyles.Bold);
            }
        }

        private static GameObject CreatePanel(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            Color color,
            bool hardShadow)
        {
            GameObject result = new GameObject(
                name,
                typeof(RectTransform),
                typeof(UnityEngine.UI.Image));
            result.transform.SetParent(parent, false);
            SetRect(result.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            UnityEngine.UI.Image image = result.GetComponent<UnityEngine.UI.Image>();
            image.color = color;
            image.raycastTarget = false;
            if (hardShadow)
            {
                AddShadow(result, new Color32(5, 7, 9, 230), new Vector2(5f, -5f));
            }

            return result;
        }

        private static GameObject CreateEmptyUi(
            string name,
            Transform parent,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            GameObject result = new GameObject(name, typeof(RectTransform));
            result.transform.SetParent(parent, false);
            SetRect(result.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            return result;
        }

        private static TextMeshProUGUI CreateText(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string content,
            float size,
            Color color,
            TextAlignmentOptions alignment,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta,
            FontStyles style)
        {
            GameObject textObject = new GameObject(
                name,
                typeof(RectTransform),
                typeof(TextMeshProUGUI));
            textObject.transform.SetParent(parent, false);
            SetRect(textObject.GetComponent<RectTransform>(), anchorMin, anchorMax, pivot, anchoredPosition, sizeDelta);
            TextMeshProUGUI text = textObject.GetComponent<TextMeshProUGUI>();
            text.font = font;
            text.text = content;
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.raycastTarget = false;
            text.overflowMode = TextOverflowModes.Overflow;
            return text;
        }

        private static void SetRect(
            RectTransform rect,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition,
            Vector2 sizeDelta)
        {
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.pivot = pivot;
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }

        private static void AddShadow(GameObject target, Color color, Vector2 distance)
        {
            UnityEngine.UI.Shadow shadow = target.AddComponent<UnityEngine.UI.Shadow>();
            shadow.effectColor = color;
            shadow.effectDistance = distance;
            shadow.useGraphicAlpha = true;
        }

        private static void AddOutline(GameObject target, Color color, Vector2 distance)
        {
            UnityEngine.UI.Outline outline = target.AddComponent<UnityEngine.UI.Outline>();
            outline.effectColor = color;
            outline.effectDistance = distance;
            outline.useGraphicAlpha = false;
        }

        private static void AddLobbyUiToBootstrapPrefab(
            TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont,
            Sprite menuBackground)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BootstrapPrefabPath);
            try
            {
                foreach (string childName in new[]
                         {
                             "NetworkLobbyCanvas", "LobbyEventSystem", "LobbyBackgroundCamera"
                         })
                {
                    Transform child = root.transform.Find(childName);
                    if (child != null)
                    {
                        Object.DestroyImmediate(child.gameObject);
                    }
                }

                NetworkLobbyUi lobbyUi = root.GetComponent<NetworkLobbyUi>() ??
                                         root.AddComponent<NetworkLobbyUi>();

                GameObject canvasObject = new GameObject(
                    "NetworkLobbyCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(UnityEngine.UI.CanvasScaler),
                    typeof(UnityEngine.UI.GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform, false);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                UnityEngine.UI.CanvasScaler scaler =
                    canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GameObject eventSystemObject = new GameObject(
                    "LobbyEventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                eventSystemObject.transform.SetParent(root.transform, false);
                eventSystemObject
                    .GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>()
                    .AssignDefaultActions();

                GameObject menuCameraObject = new GameObject(
                    "LobbyBackgroundCamera",
                    typeof(Camera));
                menuCameraObject.transform.SetParent(root.transform, false);
                Camera menuCamera = menuCameraObject.GetComponent<Camera>();
                menuCamera.clearFlags = CameraClearFlags.SolidColor;
                menuCamera.backgroundColor = new Color32(7, 11, 17, 255);
                menuCamera.cullingMask = 0;
                menuCamera.depth = -100f;
                menuCamera.allowHDR = false;
                menuCamera.allowMSAA = false;
                menuCamera.useOcclusionCulling = false;

                GameObject background = CreatePanel(
                    "MenuBackground", canvasObject.transform,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero, Color.white, false);
                UnityEngine.UI.Image backgroundImage = background.GetComponent<UnityEngine.UI.Image>();
                backgroundImage.sprite = menuBackground;
                backgroundImage.type = UnityEngine.UI.Image.Type.Simple;
                backgroundImage.preserveAspect = false;
                backgroundImage.raycastTarget = false;
                CreatePanel(
                    "CinematicTint", canvasObject.transform,
                    Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                    Vector2.zero, Vector2.zero,
                    new Color32(4, 8, 13, 54), false);
                CreatePanel(
                    "LeftShade", canvasObject.transform,
                    Vector2.zero, new Vector2(0.46f, 1f), new Vector2(0f, 0.5f),
                    Vector2.zero, Vector2.zero,
                    new Color32(5, 10, 17, 176), false);
                CreatePanel(
                    "TopRule", canvasObject.transform,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                    Vector2.zero, new Vector2(0f, 5f),
                    new Color32(255, 85, 0, 255), false);

                GameObject mainMenu = CreateMenuScreen("MainMenu", canvasObject.transform);
                CreateText(
                    "Eyebrow", mainMenu.transform, bodyFont,
                    "КООПЕРАТИВНАЯ СЛУЖБА ДОСТАВКИ", 16f,
                    new Color32(255, 111, 38, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(120f, -118f), new Vector2(-120f, 30f), FontStyles.Bold);
                CreateText(
                    "Title", mainMenu.transform, headingFont,
                    "ДОСТАВКА\nНЕВОЗМОЖНА", 88f,
                    Color.white, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(115f, -160f), new Vector2(-100f, 220f), FontStyles.Bold);
                CreateText(
                    "Tagline", mainMenu.transform, bodyFont,
                    "ДВА КУРЬЕРА. ОДИН ГРУЗ. СЛИШКОМ УЗКАЯ ДВЕРЬ.", 17f,
                    new Color32(201, 207, 215, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(120f, -392f), new Vector2(-120f, 34f), FontStyles.Normal);
                CreatePanel(
                    "TitleRule", mainMenu.transform,
                    new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                    new Vector2(120f, -448f), new Vector2(92f, 5f),
                    new Color32(255, 85, 0, 255), false);
                UnityEngine.UI.Button playButton = CreateLobbyButton(
                    "PlayButton", mainMenu.transform, headingFont,
                    "ИГРАТЬ", new Vector2(120f, -570f),
                    new Vector2(390f, 68f), new Color32(255, 85, 0, 255), out _);
                UnityEngine.UI.Button quitButton = CreateLobbyButton(
                    "QuitButton", mainMenu.transform, headingFont,
                    "ВЫЙТИ", new Vector2(120f, -654f),
                    new Vector2(260f, 54f), new Color32(29, 35, 43, 242), out _);
                CreateText(
                    "Version", mainMenu.transform, bodyFont,
                    "ПРОТОТИП 0.1  •  СЕТЕВАЯ СМЕНА", 12f,
                    new Color32(135, 144, 154, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 0f), new Vector2(1f, 0f), new Vector2(0f, 0f),
                    new Vector2(120f, 52f), new Vector2(-120f, 24f), FontStyles.Normal);

                GameObject playMenu = CreateMenuScreen("PlayMenu", canvasObject.transform);
                GameObject playCard = CreateMenuSideCard(playMenu.transform, new Vector2(600f, 580f));
                CreateText(
                    "Eyebrow", playCard.transform, bodyFont,
                    "ВЫБОР РЕЖИМА", 14f,
                    new Color32(255, 111, 38, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(44f, -40f), new Vector2(-44f, 24f), FontStyles.Bold);
                CreateText(
                    "Title", playCard.transform, headingFont,
                    "СЕТЕВАЯ СМЕНА", 48f,
                    Color.white, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -76f), new Vector2(-42f, 70f), FontStyles.Bold);
                CreateText(
                    "Hint", playCard.transform, bodyFont,
                    "Создайте комнату или присоединитесь к напарнику.", 16f,
                    new Color32(184, 191, 200, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(44f, -154f), new Vector2(-44f, 52f), FontStyles.Normal);
                UnityEngine.UI.Button hostButton = CreateLobbyButton(
                    "HostButton", playCard.transform, headingFont,
                    "СОЗДАТЬ КОМНАТУ", new Vector2(42f, -244f),
                    new Vector2(516f, 66f), new Color32(255, 85, 0, 255), out _);
                UnityEngine.UI.Button openJoinButton = CreateLobbyButton(
                    "OpenJoinButton", playCard.transform, headingFont,
                    "ПОДКЛЮЧИТЬСЯ", new Vector2(42f, -326f),
                    new Vector2(516f, 66f), new Color32(38, 45, 54, 250), out _);
                UnityEngine.UI.Button playBackButton = CreateLobbyButton(
                    "BackButton", playCard.transform, bodyFont,
                    "←  НАЗАД", new Vector2(42f, -438f),
                    new Vector2(210f, 48f), new Color32(24, 30, 37, 235), out _);

                GameObject joinMenu = CreateMenuScreen("JoinMenu", canvasObject.transform);
                GameObject joinCard = CreateMenuSideCard(joinMenu.transform, new Vector2(620f, 590f));
                CreateText(
                    "Eyebrow", joinCard.transform, bodyFont,
                    "ПОДКЛЮЧЕНИЕ К ЭКИПАЖУ", 14f,
                    new Color32(255, 111, 38, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(44f, -40f), new Vector2(-44f, 24f), FontStyles.Bold);
                CreateText(
                    "Title", joinCard.transform, headingFont,
                    "НАЙТИ НАПАРНИКА", 44f,
                    Color.white, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -76f), new Vector2(-42f, 66f), FontStyles.Bold);
                TMP_Text connectionStatus = CreateText(
                    "ConnectionStatus", joinCard.transform, bodyFont,
                    "Введите IP компьютера, на котором создана комната.", 15f,
                    new Color32(184, 191, 200, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(44f, -148f), new Vector2(-44f, 42f), FontStyles.Normal);
                CreateText(
                    "AddressLabel", joinCard.transform, bodyFont,
                    "АДРЕС ХОСТА", 11f,
                    new Color32(144, 153, 163, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(44f, -224f), new Vector2(-44f, 20f), FontStyles.Bold);
                TMP_InputField addressInput = CreateLobbyInput(
                    joinCard.transform, bodyFont,
                    new Vector2(44f, -254f), new Vector2(532f, 58f));
                UnityEngine.UI.Button joinButton = CreateLobbyButton(
                    "JoinButton", joinCard.transform, headingFont,
                    "ПОДКЛЮЧИТЬСЯ", new Vector2(44f, -338f),
                    new Vector2(532f, 66f), new Color32(255, 85, 0, 255), out _);
                UnityEngine.UI.Button joinBackButton = CreateLobbyButton(
                    "BackButton", joinCard.transform, bodyFont,
                    "←  НАЗАД", new Vector2(44f, -454f),
                    new Vector2(210f, 48f), new Color32(24, 30, 37, 235), out _);

                GameObject lobbyPanel = CreateMenuScreen("Lobby", canvasObject.transform);
                GameObject lobbyCard = CreateMenuSideCard(lobbyPanel.transform, new Vector2(700f, 670f));
                CreateText(
                    "Eyebrow", lobbyCard.transform, bodyFont,
                    "ДИСПЕТЧЕРСКАЯ  /  ПОДГОТОВКА К ВЫЕЗДУ", 14f,
                    new Color32(255, 111, 38, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -42f), new Vector2(-42f, 24f), FontStyles.Bold);
                TMP_Text lobbyTitle = CreateText(
                    "LobbyTitle", lobbyCard.transform, headingFont,
                    "ЭКИПАЖ  1/2", 46f,
                    Color.white, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -78f), new Vector2(-42f, 68f), FontStyles.Bold);
                GameObject roster = CreatePanel(
                    "Roster", lobbyCard.transform,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                    new Vector2(0f, -176f), new Vector2(-84f, 156f),
                    new Color32(12, 17, 23, 238), false);
                AddOutline(roster, new Color32(69, 78, 88, 255), new Vector2(2f, -2f));
                TMP_Text playerList = CreateText(
                    "PlayerList", roster.transform, headingFont,
                    "ПОДКЛЮЧЕНИЕ ИГРОКА...", 22f,
                    Color.white, TextAlignmentOptions.TopLeft,
                    Vector2.zero, Vector2.one, new Vector2(0f, 1f),
                    new Vector2(24f, -20f), new Vector2(-24f, -20f), FontStyles.Bold);
                TMP_Text lobbyStatus = CreateText(
                    "LobbyStatus", lobbyCard.transform, bodyFont,
                    "ОЖИДАЕМ ВТОРОГО КУРЬЕРА", 13f,
                    new Color32(190, 196, 204, 255), TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                    new Vector2(0f, -354f), new Vector2(-84f, 28f), FontStyles.Bold);
                UnityEngine.UI.Button readyButton = CreateLobbyButton(
                    "ReadyButton", lobbyCard.transform, headingFont,
                    "ГОТОВ", new Vector2(42f, -414f),
                    new Vector2(616f, 62f), new Color32(255, 85, 0, 255),
                    out TMP_Text readyButtonText);
                UnityEngine.UI.Button startButton = CreateLobbyButton(
                    "StartButton", lobbyCard.transform, headingFont,
                    "НАЧАТЬ СМЕНУ", new Vector2(42f, -492f),
                    new Vector2(616f, 62f), new Color32(38, 45, 54, 250),
                    out TMP_Text startButtonText);
                UnityEngine.UI.Button disconnectButton = CreateLobbyButton(
                    "DisconnectButton", lobbyCard.transform, bodyFont,
                    "ОТКЛЮЧИТЬСЯ", new Vector2(42f, -584f),
                    new Vector2(220f, 46f), new Color32(24, 30, 37, 235), out _);

                playMenu.SetActive(false);
                joinMenu.SetActive(false);
                lobbyPanel.SetActive(false);

                SerializedObject serializedLobby = new SerializedObject(lobbyUi);
                serializedLobby.FindProperty("lobbyCanvas").objectReferenceValue = canvas;
                serializedLobby.FindProperty("menuCamera").objectReferenceValue = menuCamera;
                serializedLobby.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
                serializedLobby.FindProperty("playMenuPanel").objectReferenceValue = playMenu;
                serializedLobby.FindProperty("joinMenuPanel").objectReferenceValue = joinMenu;
                serializedLobby.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanel;
                serializedLobby.FindProperty("playButton").objectReferenceValue = playButton;
                serializedLobby.FindProperty("playBackButton").objectReferenceValue = playBackButton;
                serializedLobby.FindProperty("openJoinButton").objectReferenceValue = openJoinButton;
                serializedLobby.FindProperty("joinBackButton").objectReferenceValue = joinBackButton;
                serializedLobby.FindProperty("addressInput").objectReferenceValue = addressInput;
                serializedLobby.FindProperty("hostButton").objectReferenceValue = hostButton;
                serializedLobby.FindProperty("joinButton").objectReferenceValue = joinButton;
                serializedLobby.FindProperty("quitButton").objectReferenceValue = quitButton;
                serializedLobby.FindProperty("connectionStatusText").objectReferenceValue = connectionStatus;
                serializedLobby.FindProperty("lobbyTitleText").objectReferenceValue = lobbyTitle;
                serializedLobby.FindProperty("playerListText").objectReferenceValue = playerList;
                serializedLobby.FindProperty("lobbyStatusText").objectReferenceValue = lobbyStatus;
                serializedLobby.FindProperty("readyButton").objectReferenceValue = readyButton;
                serializedLobby.FindProperty("readyButtonText").objectReferenceValue = readyButtonText;
                serializedLobby.FindProperty("startButton").objectReferenceValue = startButton;
                serializedLobby.FindProperty("startButtonText").objectReferenceValue = startButtonText;
                serializedLobby.FindProperty("disconnectButton").objectReferenceValue = disconnectButton;
                serializedLobby.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, BootstrapPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject CreateMenuScreen(string name, Transform parent)
        {
            return CreateEmptyUi(
                name, parent,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero);
        }

        private static GameObject CreateMenuSideCard(Transform parent, Vector2 size)
        {
            GameObject card = CreatePanel(
                "TerminalCard", parent,
                new Vector2(0f, 0.5f), new Vector2(0f, 0.5f), new Vector2(0f, 0.5f),
                new Vector2(110f, 0f), size,
                new Color32(18, 24, 31, 244), true);
            AddOutline(card, new Color32(76, 86, 97, 255), new Vector2(2f, -2f));
            CreatePanel(
                "OrangeRib", card.transform,
                Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(7f, 0f),
                new Color32(255, 85, 0, 255), false);
            return card;
        }

        private static void AddLobbyUiToBootstrapPrefab(
            TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont)
        {
            GameObject root = PrefabUtility.LoadPrefabContents(BootstrapPrefabPath);
            try
            {
                Transform oldCanvas = root.transform.Find("NetworkLobbyCanvas");
                if (oldCanvas != null)
                {
                    Object.DestroyImmediate(oldCanvas.gameObject);
                }

                Transform oldEventSystem = root.transform.Find("LobbyEventSystem");
                if (oldEventSystem != null)
                {
                    Object.DestroyImmediate(oldEventSystem.gameObject);
                }

                Transform oldMenuCamera = root.transform.Find("LobbyBackgroundCamera");
                if (oldMenuCamera != null)
                {
                    Object.DestroyImmediate(oldMenuCamera.gameObject);
                }

                NetworkLobbyUi lobbyUi = root.GetComponent<NetworkLobbyUi>();
                if (lobbyUi == null)
                {
                    lobbyUi = root.AddComponent<NetworkLobbyUi>();
                }

                GameObject canvasObject = new GameObject(
                    "NetworkLobbyCanvas",
                    typeof(RectTransform),
                    typeof(Canvas),
                    typeof(UnityEngine.UI.CanvasScaler),
                    typeof(UnityEngine.UI.GraphicRaycaster));
                canvasObject.transform.SetParent(root.transform, false);
                Canvas canvas = canvasObject.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = 100;
                UnityEngine.UI.CanvasScaler scaler =
                    canvasObject.GetComponent<UnityEngine.UI.CanvasScaler>();
                scaler.uiScaleMode = UnityEngine.UI.CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920f, 1080f);
                scaler.screenMatchMode = UnityEngine.UI.CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
                scaler.matchWidthOrHeight = 0.5f;

                GameObject eventSystemObject = new GameObject(
                    "LobbyEventSystem",
                    typeof(UnityEngine.EventSystems.EventSystem),
                    typeof(UnityEngine.InputSystem.UI.InputSystemUIInputModule));
                eventSystemObject.transform.SetParent(root.transform, false);
                eventSystemObject
                    .GetComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>()
                    .AssignDefaultActions();

                GameObject menuCameraObject = new GameObject(
                    "LobbyBackgroundCamera",
                    typeof(Camera));
                menuCameraObject.transform.SetParent(root.transform, false);
                Camera menuCamera = menuCameraObject.GetComponent<Camera>();
                menuCamera.clearFlags = CameraClearFlags.SolidColor;
                menuCamera.backgroundColor = new Color32(9, 12, 15, 255);
                menuCamera.cullingMask = 0;
                menuCamera.depth = -100f;
                menuCamera.allowHDR = false;
                menuCamera.allowMSAA = false;
                menuCamera.useOcclusionCulling = false;

                GameObject mainMenu = CreateLobbyBackdrop("MainMenu", canvasObject.transform);
                GameObject mainCard = CreateLobbyCard(mainMenu.transform, new Vector2(620f, 650f));
                CreateText(
                    "Eyebrow", mainCard.transform, bodyFont,
                    "СЛУЖБА ДОСТАВКИ  /  СЕТЕВОЙ ТЕРМИНАЛ", 14f,
                    new Color32(255, 100, 25, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -42f), new Vector2(-42f, 24f), FontStyles.Bold);
                CreateText(
                    "Title", mainCard.transform, headingFont,
                    "ДОСТАВКА\nНЕВОЗМОЖНА", 58f,
                    Color.white, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -78f), new Vector2(-42f, 142f), FontStyles.Bold);
                TMP_Text connectionStatus = CreateText(
                    "ConnectionStatus", mainCard.transform, bodyFont,
                    "СЕТЕВОЙ КООПЕРАТИВ НА ДВОИХ", 15f,
                    new Color32(166, 171, 177, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -230f), new Vector2(-42f, 28f), FontStyles.Normal);
                CreateText(
                    "AddressLabel", mainCard.transform, bodyFont,
                    "АДРЕС СЕРВЕРА", 11f,
                    new Color32(150, 155, 161, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -290f), new Vector2(-42f, 20f), FontStyles.Bold);

                TMP_InputField addressInput = CreateLobbyInput(
                    mainCard.transform,
                    bodyFont,
                    new Vector2(42f, -320f),
                    new Vector2(-84f, 54f));
                UnityEngine.UI.Button hostButton = CreateLobbyButton(
                    "HostButton", mainCard.transform, headingFont,
                    "СОЗДАТЬ ИГРУ", new Vector2(42f, -398f),
                    new Vector2(536f, 62f), new Color32(255, 85, 0, 255),
                    out _);
                UnityEngine.UI.Button joinButton = CreateLobbyButton(
                    "JoinButton", mainCard.transform, headingFont,
                    "ПОДКЛЮЧИТЬСЯ", new Vector2(42f, -476f),
                    new Vector2(536f, 62f), new Color32(45, 50, 56, 255),
                    out _);
                UnityEngine.UI.Button quitButton = CreateLobbyButton(
                    "QuitButton", mainCard.transform, bodyFont,
                    "ВЫЙТИ", new Vector2(42f, -566f),
                    new Vector2(180f, 42f), new Color32(31, 35, 40, 255),
                    out _);

                GameObject lobbyPanel = CreateLobbyBackdrop("Lobby", canvasObject.transform);
                GameObject lobbyCard = CreateLobbyCard(lobbyPanel.transform, new Vector2(700f, 650f));
                CreateText(
                    "Eyebrow", lobbyCard.transform, bodyFont,
                    "ДИСПЕТЧЕРСКАЯ  /  ПОДГОТОВКА К ВЫЕЗДУ", 14f,
                    new Color32(255, 100, 25, 255), TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -42f), new Vector2(-42f, 24f), FontStyles.Bold);
                TMP_Text lobbyTitle = CreateText(
                    "LobbyTitle", lobbyCard.transform, headingFont,
                    "ЭКИПАЖ  1/2", 44f,
                    Color.white, TextAlignmentOptions.Left,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0f, 1f),
                    new Vector2(42f, -78f), new Vector2(-42f, 64f), FontStyles.Bold);
                GameObject roster = CreatePanel(
                    "Roster", lobbyCard.transform,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                    new Vector2(0f, -170f), new Vector2(-84f, 150f),
                    new Color32(15, 18, 21, 255), false);
                AddOutline(roster, new Color32(65, 70, 76, 255), new Vector2(2f, -2f));
                TMP_Text playerList = CreateText(
                    "PlayerList", roster.transform, headingFont,
                    "ПОДКЛЮЧЕНИЕ ИГРОКА...", 22f,
                    Color.white, TextAlignmentOptions.TopLeft,
                    Vector2.zero, Vector2.one, new Vector2(0f, 1f),
                    new Vector2(24f, -20f), new Vector2(-24f, -20f), FontStyles.Bold);
                TMP_Text lobbyStatus = CreateText(
                    "LobbyStatus", lobbyCard.transform, bodyFont,
                    "ОЖИДАЕМ ВТОРОГО КУРЬЕРА", 13f,
                    new Color32(180, 184, 189, 255), TextAlignmentOptions.Center,
                    new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                    new Vector2(0f, -344f), new Vector2(-84f, 28f), FontStyles.Bold);
                UnityEngine.UI.Button readyButton = CreateLobbyButton(
                    "ReadyButton", lobbyCard.transform, headingFont,
                    "ГОТОВ", new Vector2(42f, -404f),
                    new Vector2(616f, 62f), new Color32(255, 85, 0, 255),
                    out TMP_Text readyButtonText);
                UnityEngine.UI.Button startButton = CreateLobbyButton(
                    "StartButton", lobbyCard.transform, headingFont,
                    "НАЧАТЬ СМЕНУ", new Vector2(42f, -482f),
                    new Vector2(616f, 62f), new Color32(45, 50, 56, 255),
                    out TMP_Text startButtonText);
                UnityEngine.UI.Button disconnectButton = CreateLobbyButton(
                    "DisconnectButton", lobbyCard.transform, bodyFont,
                    "ОТКЛЮЧИТЬСЯ", new Vector2(42f, -570f),
                    new Vector2(210f, 42f), new Color32(31, 35, 40, 255),
                    out _);

                lobbyPanel.SetActive(false);

                SerializedObject serializedLobby = new SerializedObject(lobbyUi);
                serializedLobby.FindProperty("lobbyCanvas").objectReferenceValue = canvas;
                serializedLobby.FindProperty("menuCamera").objectReferenceValue = menuCamera;
                serializedLobby.FindProperty("mainMenuPanel").objectReferenceValue = mainMenu;
                serializedLobby.FindProperty("lobbyPanel").objectReferenceValue = lobbyPanel;
                serializedLobby.FindProperty("addressInput").objectReferenceValue = addressInput;
                serializedLobby.FindProperty("hostButton").objectReferenceValue = hostButton;
                serializedLobby.FindProperty("joinButton").objectReferenceValue = joinButton;
                serializedLobby.FindProperty("quitButton").objectReferenceValue = quitButton;
                serializedLobby.FindProperty("connectionStatusText").objectReferenceValue = connectionStatus;
                serializedLobby.FindProperty("lobbyTitleText").objectReferenceValue = lobbyTitle;
                serializedLobby.FindProperty("playerListText").objectReferenceValue = playerList;
                serializedLobby.FindProperty("lobbyStatusText").objectReferenceValue = lobbyStatus;
                serializedLobby.FindProperty("readyButton").objectReferenceValue = readyButton;
                serializedLobby.FindProperty("readyButtonText").objectReferenceValue = readyButtonText;
                serializedLobby.FindProperty("startButton").objectReferenceValue = startButton;
                serializedLobby.FindProperty("startButtonText").objectReferenceValue = startButtonText;
                serializedLobby.FindProperty("disconnectButton").objectReferenceValue = disconnectButton;
                serializedLobby.ApplyModifiedPropertiesWithoutUndo();

                PrefabUtility.SaveAsPrefabAsset(root, BootstrapPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static GameObject CreateLobbyBackdrop(string name, Transform parent)
        {
            GameObject backdrop = CreatePanel(
                name, parent,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero,
                new Color32(9, 12, 15, 248), false);
            CreatePanel(
                "TopRule", backdrop.transform,
                new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 6f),
                new Color32(255, 85, 0, 255), false);
            return backdrop;
        }

        private static GameObject CreateLobbyCard(Transform parent, Vector2 size)
        {
            GameObject card = CreatePanel(
                "TerminalCard", parent,
                new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f), new Vector2(0.5f, 0.5f),
                Vector2.zero, size,
                new Color32(25, 29, 33, 255), true);
            AddOutline(card, new Color32(75, 81, 88, 255), new Vector2(2f, -2f));
            CreatePanel(
                "OrangeRib", card.transform,
                Vector2.zero, new Vector2(0f, 1f), new Vector2(0f, 0.5f),
                Vector2.zero, new Vector2(8f, 0f),
                new Color32(255, 85, 0, 255), false);
            return card;
        }

        private static TMP_InputField CreateLobbyInput(
            Transform parent,
            TMP_FontAsset font,
            Vector2 position,
            Vector2 size)
        {
            GameObject root = CreatePanel(
                "AddressInput", parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                position, size,
                new Color32(10, 13, 16, 255), false);
            AddOutline(root, new Color32(85, 91, 98, 255), new Vector2(2f, -2f));
            TMP_Text text = CreateText(
                "Text", root.transform, font, "127.0.0.1", 18f,
                Color.white, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
                new Vector2(18f, 0f), new Vector2(-18f, 0f), FontStyles.Normal);
            TMP_Text placeholder = CreateText(
                "Placeholder", root.transform, font, "127.0.0.1", 18f,
                new Color32(90, 95, 102, 255), TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
                new Vector2(18f, 0f), new Vector2(-18f, 0f), FontStyles.Italic);
            TMP_InputField input = root.AddComponent<TMP_InputField>();
            root.GetComponent<UnityEngine.UI.Image>().raycastTarget = true;
            input.textComponent = (TextMeshProUGUI)text;
            input.placeholder = (TextMeshProUGUI)placeholder;
            input.lineType = TMP_InputField.LineType.SingleLine;
            input.text = "127.0.0.1";
            return input;
        }

        private static UnityEngine.UI.Button CreateLobbyButton(
            string name,
            Transform parent,
            TMP_FontAsset font,
            string label,
            Vector2 position,
            Vector2 size,
            Color color,
            out TMP_Text labelText)
        {
            GameObject root = CreatePanel(
                name, parent,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f),
                position, size, color, true);
            AddOutline(root, new Color32(95, 101, 108, 255), new Vector2(2f, -2f));
            UnityEngine.UI.Button button = root.AddComponent<UnityEngine.UI.Button>();
            UnityEngine.UI.Image buttonImage = root.GetComponent<UnityEngine.UI.Image>();
            buttonImage.raycastTarget = true;
            button.targetGraphic = buttonImage;
            UnityEngine.UI.ColorBlock colors = button.colors;
            colors.normalColor = Color.white;
            colors.highlightedColor = new Color(1f, 1f, 1f, 0.82f);
            colors.pressedColor = new Color(0.72f, 0.72f, 0.72f, 1f);
            colors.disabledColor = new Color(0.35f, 0.35f, 0.35f, 0.7f);
            button.colors = colors;
            labelText = CreateText(
                "Label", root.transform, font, label, 20f,
                Color.white, TextAlignmentOptions.Center,
                Vector2.zero, Vector2.one, new Vector2(0.5f, 0.5f),
                Vector2.zero, Vector2.zero, FontStyles.Bold);
            return button;
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
            GameObject fragileCargoPrefab,
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

                GameObject fragileCargo = testScene.GetRootGameObjects()
                    .FirstOrDefault(item => item.name == "Test Fragile Television");
                if (fragileCargo == null)
                {
                    fragileCargo = (GameObject)PrefabUtility.InstantiatePrefab(
                        fragileCargoPrefab,
                        testScene);
                    fragileCargo.name = "Test Fragile Television";
                }

                fragileCargo.transform.SetPositionAndRotation(
                    new Vector3(2.4f, 0.58f, 4.5f),
                    Quaternion.Euler(0f, -15f, 0f));

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

                DeliveryCheckpoint doorwayCheckpoint = CreateOrUpdateDeliveryCheckpoint(testScene);
                DeliveryZone deliveryZone = CreateOrUpdateDeliveryZone(
                    testScene,
                    deliveryZoneMaterial);
                CreateOrUpdateFirstOrder(
                    testScene,
                    heavyCargo,
                    fragileCargo,
                    doorwayCheckpoint,
                    deliveryZone);
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

        private static DeliveryCheckpoint CreateOrUpdateDeliveryCheckpoint(Scene scene)
        {
            GameObject checkpointObject = scene.GetRootGameObjects()
                .FirstOrDefault(item => item.name == "Doorway Checkpoint");
            if (checkpointObject == null)
            {
                checkpointObject = new GameObject("Doorway Checkpoint");
                SceneManager.MoveGameObjectToScene(checkpointObject, scene);
            }

            checkpointObject.transform.SetPositionAndRotation(
                new Vector3(0f, 1.25f, 10.85f),
                Quaternion.identity);

            BoxCollider checkpointCollider = checkpointObject.GetComponent<BoxCollider>();
            if (checkpointCollider == null)
            {
                checkpointCollider = checkpointObject.AddComponent<BoxCollider>();
            }

            checkpointCollider.isTrigger = true;
            checkpointCollider.center = Vector3.zero;
            checkpointCollider.size = new Vector3(2f, 2.5f, 1.2f);

            DeliveryCheckpoint checkpoint = checkpointObject.GetComponent<DeliveryCheckpoint>();
            if (checkpoint == null)
            {
                checkpoint = checkpointObject.AddComponent<DeliveryCheckpoint>();
            }

            SerializedObject serializedCheckpoint = new SerializedObject(checkpoint);
            serializedCheckpoint.FindProperty("checkpointCollider").objectReferenceValue =
                checkpointCollider;
            serializedCheckpoint.ApplyModifiedPropertiesWithoutUndo();
            return checkpoint;
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
            GameObject fragileCargo,
            DeliveryCheckpoint doorwayCheckpoint,
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

            if (orderObject.GetComponent<NetworkSessionState>() == null)
            {
                orderObject.AddComponent<NetworkSessionState>();
            }

            SerializedObject serializedOrder = new SerializedObject(order);
            SerializedProperty definitions = serializedOrder.FindProperty("orders");
            definitions.arraySize = 2;
            ConfigureOrderDefinition(
                definitions.GetArrayElementAtIndex(0),
                "ТЯЖЁЛЫЙ ГРУЗ",
                heavyCargo.GetComponent<NetworkGrabbable>(),
                doorwayCheckpoint,
                deliveryZone);
            ConfigureOrderDefinition(
                definitions.GetArrayElementAtIndex(1),
                "ТЕЛЕВИЗОР",
                fragileCargo.GetComponent<NetworkGrabbable>(),
                doorwayCheckpoint,
                deliveryZone);
            serializedOrder.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void ConfigureOrderDefinition(
            SerializedProperty definition,
            string displayName,
            NetworkGrabbable cargo,
            DeliveryCheckpoint doorwayCheckpoint,
            DeliveryZone deliveryZone)
        {
            definition.FindPropertyRelative("displayName").stringValue = displayName;
            definition.FindPropertyRelative("targetCargo").objectReferenceValue = cargo;
            definition.FindPropertyRelative("doorwayCheckpoint").objectReferenceValue =
                doorwayCheckpoint;
            definition.FindPropertyRelative("deliveryZone").objectReferenceValue = deliveryZone;
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
