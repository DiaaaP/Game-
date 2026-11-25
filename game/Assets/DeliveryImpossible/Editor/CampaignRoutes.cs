using DeliveryImpossible.Interaction;
using DeliveryImpossible.Orders;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private sealed class CampaignLevel
        {
            public string Name;
            public string Hint;
            public DeliveryCourse Course;
            public Transform Spawn;
            public DeliveryCheckpoint[] Checkpoints;
            public DeliveryZone MainZone;
            public DeliveryZone BonusZone;
            public NetworkGrabbable MainCargo;
            public NetworkGrabbable BonusCargo;
            public string MainLabel;
            public string BonusLabel;
        }

        private static void SetUpCampaignScene()
        {
            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            foreach (string oldName in new[]
            {
                "Warehouse", "Ground", "Campaign Locations", "Doorway Left", "Doorway Right", "Doorway Header",
                "Doorway Checkpoint", "Delivery Zone", "Test Heavy Network Cargo", "Test Fragile Television"
            })
            {
                GameObject old = GameObject.Find(oldName);
                if (old != null) Object.DestroyImmediate(old);
            }

            Transform hub = new GameObject("Campaign Locations").transform;
            GameObject thermal = MakeCampaignCargo("ThermalContainer", new Vector3(0.9f, 0.74f, 0.88f), 9f);
            GameObject module = MakeCampaignCargo("PowerModule", new Vector3(1.12f, 0.86f, 1.06f), 16f,
                fragile: true, maxHolders: 2);
            CampaignLevel[] levels =
            {
                BuildHarbor(hub), BuildFoundry(hub), BuildArchive(hub), BuildColdStore(hub, thermal), BuildPowerStation(hub, module)
            };

            SerializedObject order = new SerializedObject(Object.FindFirstObjectByType<NetworkDeliveryOrder>());
            SerializedProperty items = order.FindProperty("orders");
            items.arraySize = levels.Length * 2;
            for (int index = 0; index < levels.Length; index++)
            {
                WriteManifest(items.GetArrayElementAtIndex(2 * index), levels[index], index + 1, teamCargo: false);
                WriteManifest(items.GetArrayElementAtIndex(2 * index + 1), levels[index], index + 1, teamCargo: true);
            }

            order.FindProperty("nextOrderDelay").floatValue = 3.5f;
            order.ApplyModifiedPropertiesWithoutUndo();
            SetPortLighting(hub);
            EditorSceneManager.SaveScene(scene);
        }

        private static void WriteManifest(SerializedProperty item, CampaignLevel level, int index, bool teamCargo)
        {
            item.FindPropertyRelative("levelName").stringValue = level.Name;
            item.FindPropertyRelative("displayName").stringValue = teamCargo ? level.BonusLabel : level.MainLabel;
            item.FindPropertyRelative("routeHint").stringValue = level.Hint;
            item.FindPropertyRelative("levelNumber").intValue = index;
            item.FindPropertyRelative("coOpOnly").boolValue = teamCargo;
            item.FindPropertyRelative("timeLimit").floatValue = 0f;
            item.FindPropertyRelative("course").objectReferenceValue = level.Course;
            item.FindPropertyRelative("targetCargo").objectReferenceValue = teamCargo ? level.BonusCargo : level.MainCargo;
            item.FindPropertyRelative("courierSpawn").objectReferenceValue = level.Spawn;
            item.FindPropertyRelative("deliveryZone").objectReferenceValue = teamCargo && level.BonusZone != null
                ? level.BonusZone : level.MainZone;
            item.FindPropertyRelative("doorwayCheckpoint").objectReferenceValue = null;
            SetObjects(item.FindPropertyRelative("routeCheckpoints"), level.Checkpoints);
        }

        private static Transform Location(Transform hub, string name, float offset)
        {
            Transform location = Child(hub, name);
            location.localPosition = Vector3.right * offset;
            return location;
        }

        private static Transform SpawnAt(Transform parent, Vector3 position)
        {
            Transform spawn = Child(parent, "Courier arrival");
            spawn.localPosition = position + Vector3.up * 0.05f;
            return spawn;
        }

        private static void Floor(Transform root, Material material, float width, float from, float to, float elevation = 0f)
        {
            Block(root, "Walkable floor", new Vector3(0f, elevation - 0.15f, (from + to) * 0.5f),
                new Vector3(width, 0.30f, to - from), material, true);
        }

        private static void WallBox(Transform root, Material material, float width, float from, float to, float height)
        {
            foreach (float x in new[] { -width / 2f, width / 2f })
                Block(root, "Side wall", new Vector3(x, height / 2f, (from + to) / 2f),
                    new Vector3(0.30f, height, to - from), material, true);
            Partition(root, material, -width / 2f, width / 2f, 0f, height, from);
            Partition(root, material, -width / 2f, width / 2f, 0f, height, to);
        }

        private static void Partition(Transform root, Material material, float left, float right, float bottom, float top, float z)
        {
            Block(root, "Partition", new Vector3((left + right) / 2f, (top + bottom) / 2f, z),
                new Vector3(right - left, top - bottom, 0.40f), material, true);
        }

        private static DeliveryCheckpoint Volume(Transform parent, string name, Vector3 center, Vector3 size)
        {
            Transform area = Child(parent, name);
            area.localPosition = center;
            BoxCollider volume = area.gameObject.AddComponent<BoxCollider>();
            volume.isTrigger = true;
            volume.size = size;
            return area.gameObject.AddComponent<DeliveryCheckpoint>();
        }

        private static DeliveryZone ZoneAt(Transform parent, Vector3 position, float width = 4.2f, float yaw = 0f)
        {
            Transform area = Child(parent, "Receiving area");
            area.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, yaw, 0f));
            BoxCollider collider = area.gameObject.AddComponent<BoxCollider>();
            collider.isTrigger = true;
            collider.size = new Vector3(width, 2.8f, width);
            collider.center = new Vector3(0f, 1.365f, 0f);
            Material marking = Material("Dispatch Green", "#4DC38B");
            Material mat = Material("Receiving Mat", "#31574D");
            Block(area, "Receiving mat", new Vector3(0f, 0.012f, 0f), new Vector3(width - 0.18f, 0.011f, width - 0.18f), mat);
            foreach (float side in new[] { -1f, 1f })
            {
                float edge = side * (width / 2f - 0.05f);
                Block(area, "Receiving border", new Vector3(edge, 0.02f, 0f), new Vector3(0.09f, 0.015f, width), marking);
                Block(area, "Receiving border", new Vector3(0f, 0.02f, edge), new Vector3(width - 0.19f, 0.015f, 0.09f), marking);
            }

            return area.gameObject.AddComponent<DeliveryZone>();
        }

        private static DeliveryCourse Course(Transform parent, DeliveryMechanic mechanic, System.Action<SerializedObject> configure = null)
        {
            var course = parent.gameObject.AddComponent<DeliveryCourse>();
            SerializedObject data = new SerializedObject(course);
            data.FindProperty("mechanic").enumValueIndex = (int)mechanic;
            configure?.Invoke(data);
            data.ApplyModifiedPropertiesWithoutUndo();
            return course;
        }

        private static void SetObjects<T>(SerializedProperty property, T[] items) where T : Object
        {
            property.arraySize = items.Length;
            for (int index = 0; index < items.Length; index++)
                property.GetArrayElementAtIndex(index).objectReferenceValue = items[index];
        }

        private static NetworkGrabbable PutCargo(Transform root, GameObject prefab, Vector3 position, bool bonus, Material marking)
        {
            GameObject item = (GameObject)PrefabUtility.InstantiatePrefab(prefab, root);
            item.transform.localPosition = position;
            PrefabUtility.RecordPrefabInstancePropertyModifications(item.transform);
            NetworkGrabbable cargo = item.GetComponent<NetworkGrabbable>();
            if (bonus)
            {
                SerializedObject data = new SerializedObject(cargo);
                data.FindProperty("campaignBonusCargo").boolValue = true;
                data.ApplyModifiedPropertiesWithoutUndo();
            }

            Vector3 size = item.GetComponent<BoxCollider>().size;
            MarkPickup(root, new Vector3(position.x, 0.014f, position.z), size.x + 0.40f, size.z + 0.40f, marking);
            return cargo;
        }

        private static void CourseLabel(Transform root, string text, Vector3 position, float width, Material foreground, Material background)
        {
            Sign(root, text, position, width, foreground, background,
                AssetDatabase.LoadAssetAtPath<TMP_FontAsset>(Root + "/Fonts/DeliveryOswald.asset"));
        }

        private static void Lamp(Transform parent, Vector3 position, Color color, float range, float intensity)
        {
            Light light = Child(parent, "Area light").gameObject.AddComponent<Light>();
            light.transform.localPosition = position;
            light.type = LightType.Point;
            light.color = color;
            light.range = range;
            light.intensity = intensity;
        }

        private static Renderer Blob(Transform parent, string label, Vector3 position, Vector3 size, Material material, bool solid = false)
        {
            GameObject obj = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            obj.name = label;
            obj.transform.SetParent(parent, false);
            obj.transform.localPosition = position;
            obj.transform.localScale = size;
            if (!solid) Object.DestroyImmediate(obj.GetComponent<Collider>());
            Renderer renderer = obj.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static void Arrow(Transform parent, Vector3 point, Material material, float size)
        {
            Block(parent, "Directional arrow line", point + Vector3.back * size * 0.15f,
                new Vector3(size * 0.10f, 0.014f, size * 0.42f), material);
            foreach (float side in new[] { -1f, 1f })
            {
                Renderer tip = Block(parent, "Directional arrow head", point + new Vector3(side * size * 0.105f, 0f, size * 0.12f),
                    new Vector3(size * 0.095f, 0.014f, size * 0.30f), material);
                tip.transform.localRotation = Quaternion.Euler(0f, -side * 42f, 0f);
            }
        }
    }
}
