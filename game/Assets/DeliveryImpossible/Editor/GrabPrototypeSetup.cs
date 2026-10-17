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
            GameObject cargoPrefab = GetOrCreateCargoPrefab(cargoMaterial);
            GameObject heavyCargoPrefab = GetOrCreateHeavyCargoPrefab(heavyCargoMaterial);
            GameObject fragileCargoPrefab = GetOrCreateFragileCargoPrefab(
                fragileBodyMaterial,
                fragileScreenMaterial,
                fragileDamageMaterial);
            AddGrabberToPlayerPrefab(headingFont, bodyFont);
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
            CreateTeammatePanel(
                canvasObject.transform,
                headingFont,
                bodyFont,
                out TMP_Text teammateTitle,
                out TMP_Text teammateConnection,
                out TMP_Text teammateState,
                out TMP_Text teammateGrip,
                out TMP_Text teammateStability,
                out UnityEngine.UI.Image[] teammateSegments);
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
            serializedOrderHud.FindProperty("teammateTitleText").objectReferenceValue = teammateTitle;
            serializedOrderHud.FindProperty("teammateConnectionText").objectReferenceValue = teammateConnection;
            serializedOrderHud.FindProperty("teammateStateText").objectReferenceValue = teammateState;
            serializedOrderHud.FindProperty("teammateGripText").objectReferenceValue = teammateGrip;
            serializedOrderHud.FindProperty("teammateStabilityText").objectReferenceValue = teammateStability;
            SerializedProperty segmentProperty = serializedOrderHud.FindProperty("teammateGripSegments");
            segmentProperty.arraySize = teammateSegments.Length;
            for (int index = 0; index < teammateSegments.Length; index++)
            {
                segmentProperty.GetArrayElementAtIndex(index).objectReferenceValue = teammateSegments[index];
            }

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

        private static void CreateTeammatePanel(
            Transform parent,
            TMP_FontAsset headingFont,
            TMP_FontAsset bodyFont,
            out TMP_Text title,
            out TMP_Text connection,
            out TMP_Text state,
            out TMP_Text grip,
            out TMP_Text stability,
            out UnityEngine.UI.Image[] segments)
        {
            GameObject panel = CreatePanel(
                "TeammatePanel", parent,
                Vector2.zero, Vector2.zero, Vector2.zero,
                new Vector2(48f, 48f), new Vector2(390f, 116f),
                new Color32(20, 23, 26, 248), true);
            AddOutline(panel, new Color32(62, 68, 75, 255), new Vector2(2f, -2f));
            GameObject header = CreatePanel(
                "Header", panel.transform,
                new Vector2(0f, 1f), Vector2.one, new Vector2(0.5f, 1f),
                Vector2.zero, new Vector2(0f, 42f),
                new Color32(38, 42, 46, 255), false);
            title = CreateText(
                "Title", header.transform, headingFont, "НАПАРНИК: ОЖИДАНИЕ", 17f,
                Color.white, TextAlignmentOptions.Left,
                Vector2.zero, Vector2.one, new Vector2(0f, 0.5f),
                new Vector2(14f, 0f), new Vector2(-138f, 0f), FontStyles.Bold);
            connection = CreateText(
                "Connection", header.transform, bodyFont, "НЕ В СЕТИ", 10f,
                new Color32(74, 225, 118, 255), TextAlignmentOptions.Center,
                new Vector2(1f, 0.5f), new Vector2(1f, 0.5f), new Vector2(1f, 0.5f),
                new Vector2(-12f, 0f), new Vector2(112f, 24f), FontStyles.Bold);
            state = CreateText(
                "State", panel.transform, headingFont, "ОЖИДАЕМ ПОДКЛЮЧЕНИЯ", 14f,
                new Color32(210, 213, 216, 255), TextAlignmentOptions.Left,
                new Vector2(0f, 1f), new Vector2(0.68f, 1f), new Vector2(0f, 1f),
                new Vector2(14f, -52f), new Vector2(-4f, 22f), FontStyles.Bold);
            grip = CreateText(
                "Grip", panel.transform, bodyFont, "ХВАТ: 0%", 11f,
                Color.white, TextAlignmentOptions.Right,
                new Vector2(0.68f, 1f), Vector2.one, new Vector2(1f, 1f),
                new Vector2(-14f, -52f), new Vector2(-4f, 22f), FontStyles.Bold);
            GameObject track = CreatePanel(
                "GripTrack", panel.transform,
                new Vector2(0f, 1f), new Vector2(1f, 1f), new Vector2(0.5f, 1f),
                new Vector2(0f, -84f), new Vector2(-28f, 16f),
                new Color32(10, 12, 14, 255), false);
            AddOutline(track, new Color32(75, 80, 87, 255), new Vector2(1f, -1f));
            segments = new UnityEngine.UI.Image[5];
            for (int index = 0; index < segments.Length; index++)
            {
                GameObject segment = CreatePanel(
                    $"Segment {index + 1}", track.transform,
                    new Vector2(index / 5f, 0f), new Vector2((index + 1f) / 5f, 1f),
                    new Vector2(0.5f, 0.5f), Vector2.zero, new Vector2(-4f, -6f),
                    new Color32(83, 88, 95, 255), false);
                segments[index] = segment.GetComponent<UnityEngine.UI.Image>();
            }
            stability = null;
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
