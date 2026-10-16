using System.Linq;
using DeliveryImpossible.Interaction;
using DeliveryImpossible.Networking;
using Mirror;
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
        private const string PlayerPrefabPath = PrefabsFolder + "/NetworkPlayer.prefab";
        private const string BootstrapPrefabPath = PrefabsFolder + "/NetworkBootstrap.prefab";
        private const string CargoPrefabPath = PrefabsFolder + "/NetworkCargo.prefab";
        private const string CargoMaterialPath = MaterialsFolder + "/CargoPrototype.mat";
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

            Material cargoMaterial = GetOrCreateCargoMaterial();
            GameObject cargoPrefab = GetOrCreateCargoPrefab(cargoMaterial);
            AddGrabberToPlayerPrefab();
            RegisterCargoPrefab(cargoPrefab);
            AddCargoToTestScene(cargoPrefab);

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[Delivery Impossible] Network grab prototype is ready. Aim at cargo and press E.");
        }

        private static Material GetOrCreateCargoMaterial()
        {
            Material material = AssetDatabase.LoadAssetAtPath<Material>(CargoMaterialPath);
            if (material != null)
            {
                return material;
            }

            Shader shader = Shader.Find("Universal Render Pipeline/Lit") ?? Shader.Find("Standard");
            material = new Material(shader)
            {
                name = "Cargo Prototype",
                color = new Color(0.9f, 0.32f, 0.08f, 1f)
            };
            AssetDatabase.CreateAsset(material, CargoMaterialPath);
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
                PrefabUtility.SaveAsPrefabAsset(root, CargoPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddGrabberToPlayerPrefab()
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
                serializedGrabber.ApplyModifiedPropertiesWithoutUndo();
                PrefabUtility.SaveAsPrefabAsset(root, PlayerPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RegisterCargoPrefab(GameObject cargoPrefab)
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
                if (!manager.spawnPrefabs.Contains(cargoPrefab))
                {
                    manager.spawnPrefabs.Add(cargoPrefab);
                }

                PrefabUtility.SaveAsPrefabAsset(root, BootstrapPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void AddCargoToTestScene(GameObject cargoPrefab)
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
    }
}
