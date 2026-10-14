using System.Linq;
using DeliveryImpossible.Player;
using kcp2k;
using Mirror;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using Object = UnityEngine.Object;

namespace DeliveryImpossible.Editor
{
    public static class MovementPrototypeSetup
    {
        private const string RootFolder = "Assets/DeliveryImpossible";
        private const string PrefabsFolder = RootFolder + "/Prefabs";
        private const string MaterialsFolder = RootFolder + "/Materials";
        private const string ScenesFolder = RootFolder + "/Scenes";
        private const string PlayerPrefabPath = PrefabsFolder + "/NetworkPlayer.prefab";
        private const string BootstrapPrefabPath = PrefabsFolder + "/NetworkBootstrap.prefab";
        private const string PlayerMaterialPath = MaterialsFolder + "/PlayerPrototype.mat";
        private const string TestScenePath = ScenesFolder + "/MovementTest.unity";

        [MenuItem("Tools/Delivery Impossible/Setup Movement Prototype")]
        public static void Setup()
        {
            EnsureFolders();
            Material playerMaterial = CreatePlayerMaterial();
            GameObject playerPrefab = CreatePlayerPrefab(playerMaterial);
            GameObject bootstrapPrefab = CreateBootstrapPrefab(playerPrefab);
            CreateTestScene(bootstrapPrefab);
            AddSceneToBuildSettings();

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log($"[Delivery Impossible] Movement prototype is ready: {TestScenePath}");
        }

        private static void EnsureFolders()
        {
            EnsureFolder("Assets", "DeliveryImpossible");
            EnsureFolder(RootFolder, "Prefabs");
            EnsureFolder(RootFolder, "Materials");
            EnsureFolder(RootFolder, "Scenes");
        }

        private static void EnsureFolder(string parent, string child)
        {
            string path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static Material CreatePlayerMaterial()
        {
            Material existing = AssetDatabase.LoadAssetAtPath<Material>(PlayerMaterialPath);
            if (existing != null)
            {
                return existing;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            Material material = new Material(shader)
            {
                name = "Player Prototype",
                color = new Color(0.16f, 0.55f, 0.95f, 1f)
            };
            AssetDatabase.CreateAsset(material, PlayerMaterialPath);
            return material;
        }

        private static GameObject CreatePlayerPrefab(Material playerMaterial)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(PlayerPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("NetworkPlayer");
            try
            {
                CharacterController characterController = root.AddComponent<CharacterController>();
                characterController.height = 1.8f;
                characterController.radius = 0.4f;
                characterController.center = new Vector3(0f, 0.9f, 0f);
                characterController.stepOffset = 0.3f;
                characterController.slopeLimit = 50f;

                root.AddComponent<NetworkIdentity>();
                NetworkTransformUnreliable networkTransform = root.AddComponent<NetworkTransformUnreliable>();
                networkTransform.syncDirection = SyncDirection.ClientToServer;
                networkTransform.syncInterval = 1f / 30f;

                GameObject body = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                body.name = "Body";
                Object.DestroyImmediate(body.GetComponent<Collider>());
                body.transform.SetParent(root.transform, false);
                body.transform.localPosition = new Vector3(0f, 0.9f, 0f);
                body.transform.localScale = new Vector3(0.8f, 0.9f, 0.8f);
                Renderer bodyRenderer = body.GetComponent<Renderer>();
                bodyRenderer.sharedMaterial = playerMaterial;

                GameObject cameraRootObject = new GameObject("CameraRoot");
                cameraRootObject.transform.SetParent(root.transform, false);
                cameraRootObject.transform.localPosition = new Vector3(0f, 1.65f, 0f);

                GameObject cameraObject = new GameObject("PlayerCamera");
                cameraObject.tag = "MainCamera";
                cameraObject.transform.SetParent(cameraRootObject.transform, false);
                Camera playerCamera = cameraObject.AddComponent<Camera>();
                playerCamera.nearClipPlane = 0.05f;
                AudioListener audioListener = cameraObject.AddComponent<AudioListener>();

                NetworkFirstPersonController controller = root.AddComponent<NetworkFirstPersonController>();
                SerializedObject serializedController = new SerializedObject(controller);
                serializedController.FindProperty("cameraRoot").objectReferenceValue = cameraRootObject.transform;
                serializedController.FindProperty("playerCamera").objectReferenceValue = playerCamera;
                serializedController.FindProperty("audioListener").objectReferenceValue = audioListener;
                SerializedProperty hiddenRenderers = serializedController.FindProperty("ownerHiddenRenderers");
                hiddenRenderers.arraySize = 1;
                hiddenRenderers.GetArrayElementAtIndex(0).objectReferenceValue = bodyRenderer;
                serializedController.ApplyModifiedPropertiesWithoutUndo();

                return PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static GameObject CreateBootstrapPrefab(GameObject playerPrefab)
        {
            GameObject existing = AssetDatabase.LoadAssetAtPath<GameObject>(BootstrapPrefabPath);
            if (existing != null)
            {
                return existing;
            }

            GameObject root = new GameObject("NetworkBootstrap");
            try
            {
                KcpTransport transport = root.AddComponent<KcpTransport>();
                NetworkManager manager = root.AddComponent<NetworkManager>();
                root.AddComponent<NetworkManagerHUD>();

                manager.transport = transport;
                manager.playerPrefab = playerPrefab;
                manager.maxConnections = 4;
                manager.sendRate = 30;
                manager.dontDestroyOnLoad = false;

                return PrefabUtility.SaveAsPrefabAsset(root, BootstrapPrefabPath);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static void CreateTestScene(GameObject bootstrapPrefab)
        {
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(TestScenePath) != null)
            {
                return;
            }

            Scene previousActiveScene = SceneManager.GetActiveScene();
            Scene testScene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Additive);
            SceneManager.SetActiveScene(testScene);

            try
            {
                GameObject lightObject = new GameObject("Directional Light");
                Light light = lightObject.AddComponent<Light>();
                light.type = LightType.Directional;
                light.intensity = 1.5f;
                lightObject.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

                GameObject ground = GameObject.CreatePrimitive(PrimitiveType.Cube);
                ground.name = "Ground";
                ground.transform.position = new Vector3(0f, -0.5f, 0f);
                ground.transform.localScale = new Vector3(50f, 1f, 50f);

                PrefabUtility.InstantiatePrefab(bootstrapPrefab, testScene);
                CreateSpawnPoint(testScene, "Spawn Point 1", new Vector3(-1.5f, 0.05f, 0f));
                CreateSpawnPoint(testScene, "Spawn Point 2", new Vector3(1.5f, 0.05f, 0f));

                EditorSceneManager.SaveScene(testScene, TestScenePath);
            }
            finally
            {
                if (previousActiveScene.IsValid() && previousActiveScene.isLoaded)
                {
                    SceneManager.SetActiveScene(previousActiveScene);
                }

                EditorSceneManager.CloseScene(testScene, true);
            }
        }

        private static void CreateSpawnPoint(Scene scene, string name, Vector3 position)
        {
            GameObject spawnPoint = new GameObject(name);
            spawnPoint.transform.position = position;
            spawnPoint.AddComponent<NetworkStartPosition>();
            SceneManager.MoveGameObjectToScene(spawnPoint, scene);
        }

        private static void AddSceneToBuildSettings()
        {
            EditorBuildSettingsScene[] currentScenes = EditorBuildSettings.scenes;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(TestScenePath, true) }
                .Concat(currentScenes.Where(scene => scene.path != TestScenePath))
                .ToArray();
        }
    }
}
