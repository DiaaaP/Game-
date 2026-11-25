using System;
using System.IO;
using System.Linq;
using DeliveryImpossible.Interaction;
using DeliveryImpossible.Orders;
using DeliveryImpossible.Player;
using DeliveryImpossible.UI;
using TMPro;
using UnityEditor;
using UnityEditor.Animations;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private const string Root = "Assets/DeliveryImpossible";
        private const string ScenePath = Root + "/Scenes/MovementTest.unity";
        private const string Models = Root + "/Models/";
        private const string Surface = Root + "/Materials/";
        private const string ProductName = "Dock Shift";

        [MenuItem("Tools/" + ProductName + "/Set Up Campaign")]
        public static void Apply()
        {
            if (EditorApplication.isPlayingOrWillChangePlaymode)
                throw new InvalidOperationException("Exit Play Mode before applying the campaign.");

            ConfigureCourier();
            SetUpCargo();
            SetUpQuayMenus();
            SetUpCampaignScene();
            PlayerSettings.productName = ProductName;
            EditorBuildSettings.scenes = new[] { new EditorBuildSettingsScene(ScenePath, true) };
            AssetDatabase.SaveAssets();
        }

        public static void BuildWindows()
        {
            const string path = "Builds/Windows/" + ProductName + ".exe";
            Directory.CreateDirectory(Path.GetDirectoryName(path));
            BuildReport report = BuildPipeline.BuildPlayer(new[] { ScenePath }, path,
                BuildTarget.StandaloneWindows64, BuildOptions.None);
            if (report.summary.result != BuildResult.Succeeded)
            {
                throw new InvalidOperationException($"Windows build failed: {report.summary.result}.");
            }
        }

        private static void ConfigureCourier()
        {
            string modelPath = Models + "Courier.fbx";
            ModelImporter importer = (ModelImporter)AssetImporter.GetAtPath(modelPath);
            if (importer.animationType != ModelImporterAnimationType.Generic)
            {
                importer.animationType = ModelImporterAnimationType.Generic;
                importer.avatarSetup = ModelImporterAvatarSetup.CreateFromThisModel;
                importer.SaveAndReimport();
            }

            ModelImporterClipAnimation[] clips = importer.defaultClipAnimations
                .Where(clip => clip.name is "idle" or "walk").ToArray();
            if (clips.Length != 2)
            {
                throw new InvalidOperationException("The courier model needs the idle and walk animations.");
            }
            if (importer.clipAnimations.Length != clips.Length || importer.clipAnimations.Any(clip => !clip.loopTime))
            {
                foreach (ModelImporterClipAnimation clip in clips)
                {
                    clip.loopTime = true;
                }
                importer.clipAnimations = clips;
                importer.SaveAndReimport();
            }

            Texture2D orangeTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Models + "CourierOrange.png");
            Texture2D blueTexture = AssetDatabase.LoadAssetAtPath<Texture2D>(Models + "CourierBlue.png");
            Material orangeUniform = Material("Courier Orange", "#FFFFFF", orangeTexture);
            Material blueUniform = Material("Courier Blue", "#FFFFFF", blueTexture);
            AnimatorController controller = GetCourierController(modelPath);

            EditPrefab("NetworkPlayer", player =>
            {
                RemoveChild(player.transform, "Body");
                Transform body = Child(player.transform, "Body");
                float courierHeight = player.GetComponent<CharacterController>().height;
                GameObject model = FitModel("Courier", body, Vector3.one * courierHeight,
                    Vector3.up * (courierHeight * 0.5f), preserveProportions: true);
                Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
                foreach (Renderer renderer in renderers)
                {
                    renderer.sharedMaterial = orangeUniform;
                }

                Animator animator = model.GetComponent<Animator>();
                if (animator == null)
                {
                    animator = model.AddComponent<Animator>();
                }
                animator.runtimeAnimatorController = controller;
                animator.applyRootMotion = false;
                animator.cullingMode = AnimatorCullingMode.CullUpdateTransforms;
                CourierVisual visual = body.gameObject.AddComponent<CourierVisual>();
                SerializedObject presentation = new SerializedObject(visual);
                presentation.FindProperty("animator").objectReferenceValue = animator;
                presentation.FindProperty("secondCourierUniform").objectReferenceValue = blueUniform;
                presentation.ApplyModifiedPropertiesWithoutUndo();
                SetRenderers(visual, "uniformRenderers", renderers);
                SetRenderers(player.GetComponent<NetworkFirstPersonController>(), "ownerHiddenRenderers", renderers);
                ConfigureQuayCamera(player.GetComponentInChildren<Camera>(true));
                SetUpShiftAudio(player);
                SetUpReadableHud(player);
            });
        }

        private static void SetUpShiftAudio(GameObject player)
        {
            AudioSource source = player.GetComponent<AudioSource>();
            if (source == null)
            {
                source = player.AddComponent<AudioSource>();
            }
            source.playOnAwake = false;
            source.loop = true;
            source.clip = AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/Warehouse.wav");
            SerializedObject hud = new SerializedObject(player.GetComponent<NetworkOrderHud>());
            hud.FindProperty("warehouseAmbience").objectReferenceValue = source;
            hud.FindProperty("deliveryAccepted").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/DeliveryAccepted.wav");
            hud.FindProperty("cargoBroken").objectReferenceValue =
                AssetDatabase.LoadAssetAtPath<AudioClip>(Root + "/Audio/CargoBroken.wav");
            hud.ApplyModifiedPropertiesWithoutUndo();
        }

        private static AnimatorController GetCourierController(string modelPath)
        {
            const string path = Models + "Courier.controller";
            AnimatorController existing = AssetDatabase.LoadAssetAtPath<AnimatorController>(path);
            if (existing != null)
            {
                return existing;
            }

            AnimatorController controller = AnimatorController.CreateAnimatorControllerAtPath(path);
            controller.AddParameter("Speed", AnimatorControllerParameterType.Float);
            AnimationClip[] animations = AssetDatabase.LoadAllAssetsAtPath(modelPath).OfType<AnimationClip>().ToArray();
            AnimatorStateMachine machine = controller.layers[0].stateMachine;
            AnimatorState idle = machine.AddState("Idle");
            idle.motion = animations.Single(clip => clip.name == "idle");
            AnimatorState walk = machine.AddState("Walk");
            walk.motion = animations.Single(clip => clip.name == "walk");
            walk.speed = 1.55f;
            machine.defaultState = idle;
            AnimatorStateTransition start = idle.AddTransition(walk);
            start.hasExitTime = false;
            start.duration = 0.055f;
            start.AddCondition(AnimatorConditionMode.Greater, 0.25f, "Speed");
            AnimatorStateTransition stop = walk.AddTransition(idle);
            stop.hasExitTime = false;
            stop.duration = 0.055f;
            stop.AddCondition(AnimatorConditionMode.Less, 0.16f, "Speed");
            return controller;
        }

        private static void SetUpCargo()
        {
            Material cardboard = Material("Kraft Cardboard", "#B68A53");
            Material darkTape = Material("Packing Tape", "#806039");
            Material lightLabel = Material("Shipping Label", "#EADCC2");
            Material blue = Material("Freight Blue", "#355C72");
            Material steel = Material("Powder Coated Steel", "#1F2A32");
            Material amber = Material("Safety Amber", "#F6AA39");
            Material screen = Material("Television Glass", "#164054");
            Material damage = Material("Cracked Glass", "#F1B9A0");

            EditPrefab("NetworkCargo", root =>
            {
                Transform art = ReplaceCargoVisual(root);
                GameObject box = FitModel("Parcel", art, new Vector3(0.96f, 0.98f, 0.96f), Vector3.zero);
                AssignMaterial(box, cardboard);
                Block(art, "Packing tape", new Vector3(0f, 0.491f, 0f), new Vector3(0.20f, 0.012f, 0.97f), darkTape);
                Block(art, "Shipping label", new Vector3(-0.22f, 0.10f, -0.482f), new Vector3(0.26f, 0.21f, 0.012f), lightLabel);
                for (int index = 0; index < 4; index++)
                {
                    Block(art, "Label print", new Vector3(-0.22f, 0.038f + index * 0.039f, -0.49f),
                        new Vector3(index % 2 == 0 ? 0.18f : 0.12f, 0.013f, 0.006f), darkTape);
                }
                SetRenderers(root.GetComponent<NetworkGrabbable>(), "highlightedRenderers", box.GetComponentsInChildren<Renderer>());
            });

            EditPrefab("HeavyNetworkCargo", root =>
            {
                Transform art = ReplaceCargoVisual(root);
                Renderer shell = Block(art, "Flight case shell", Vector3.zero,
                    new Vector3(0.93f, 0.91f, 0.9f), blue);
                foreach (float x in new[] { -0.44f, -0.20f, 0.20f, 0.44f })
                {
                    Block(art, "Steel case band", new Vector3(x, 0f, 0f),
                        new Vector3(0.026f, 0.98f, 0.98f), steel);
                }
                foreach (float z in new[] { -0.46f, 0.46f })
                {
                    Block(art, "Handle mount", new Vector3(0f, 0.06f, z),
                        new Vector3(0.19f, 0.22f, 0.035f), steel);
                    Block(art, "Recessed handle", new Vector3(0f, 0.05f, z * 1.07f),
                        new Vector3(0.125f, 0.042f, 0.025f), amber);
                    Block(art, "Case badge", new Vector3(0.31f, 0.12f, z),
                        new Vector3(0.12f, 0.18f, 0.025f), amber);
                }
                SetRenderers(root.GetComponent<NetworkGrabbable>(), "highlightedRenderers", new[] { shell });
            });

            EditPrefab("FragileTelevision", root =>
            {
                Transform art = ReplaceCargoVisual(root);
                RemoveChild(root.transform, "Screen");
                RemoveChild(root.transform, "Damage Visual");
                GameObject television = FitModel("Television", art,
                    new Vector3(0.97f, 0.97f, 0.91f), Vector3.zero, 180f);
                AssignMaterial(television, steel);
                Renderer glass = Block(art, "Screen", new Vector3(0f, 0.07f, -0.471f),
                    new Vector3(0.88f, 0.79f, 0.024f), screen);
                Transform cracks = Child(art, "Cracked screen");
                foreach (float angle in new[] { -28f, 17f, 55f })
                {
                    Renderer crack = Block(cracks, "Crack", new Vector3(0.07f, 0.11f, -0.487f),
                        new Vector3(0.66f, 0.017f, 0.012f), damage);
                    crack.transform.localRotation = Quaternion.Euler(0f, 0f, angle);
                }
                cracks.gameObject.SetActive(false);

                NetworkFragileCargo fragile = root.GetComponent<NetworkFragileCargo>();
                SetRenderers(fragile, "damageRenderers", new[] { glass });
                SerializedObject serialized = new SerializedObject(fragile);
                serialized.FindProperty("damageVisual").objectReferenceValue = cracks.gameObject;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                SetRenderers(root.GetComponent<NetworkGrabbable>(), "highlightedRenderers",
                    television.GetComponentsInChildren<Renderer>());
            });
        }

        private static RectTransform Panel(Transform parent, string name, Color color)
        {
            GameObject panel = new GameObject(name, typeof(RectTransform), typeof(Image));
            panel.transform.SetParent(parent, false);
            panel.GetComponent<Image>().color = color;
            return (RectTransform)panel.transform;
        }

        private static void MarkPickup(Transform parent, Vector3 position, float width, float depth, Material paint)
        {
            foreach (float x in new[] { -1f, 1f })
            {
                foreach (float z in new[] { -1f, 1f })
                {
                    const float length = 0.25f;
                    Vector3 corner = position + new Vector3(x * width * 0.5f, 0f, z * depth * 0.5f);
                    Block(parent, "Pickup bay", corner - new Vector3(x * length * 0.5f, 0f, 0f),
                        new Vector3(length, 0.01f, 0.045f), paint);
                    Block(parent, "Pickup bay", corner - new Vector3(0f, -0.002f, z * length * 0.5f),
                        new Vector3(0.045f, 0.01f, length), paint);
                }
            }
        }

        private static void Sign(Transform parent, string label, Vector3 position, float width,
            Material foreground, Material background, TMP_FontAsset font)
        {
            Block(parent, label, position, new Vector3(width, 0.34f, 0.06f), background);
            TextMeshPro text = Child(parent, label + " text").gameObject.AddComponent<TextMeshPro>();
            text.transform.localPosition = position + Vector3.back * 0.033f;
            text.rectTransform.sizeDelta = new Vector2(width, 0.34f);
            text.font = font;
            text.fontSize = 2.15f;
            text.alignment = TextAlignmentOptions.Center;
            text.color = foreground.color;
            text.text = label;
        }

        private static Transform ReplaceCargoVisual(GameObject root)
        {
            RemoveChild(root.transform, "Cargo Model");
            root.GetComponent<MeshRenderer>().enabled = false;
            return Child(root.transform, "Cargo Model");
        }

        private static void EditPrefab(string name, Action<GameObject> update)
        {
            string path = Root + "/Prefabs/" + name + ".prefab";
            GameObject root = PrefabUtility.LoadPrefabContents(path);
            try
            {
                update(root);
                PrefabUtility.SaveAsPrefabAsset(root, path);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void RemoveChild(Transform parent, string name)
        {
            Transform old = parent.Find(name);
            if (old != null) Object.DestroyImmediate(old.gameObject);
        }

        private static Transform Child(Transform parent, string name)
        {
            Transform child = new GameObject(name).transform;
            child.SetParent(parent, false);
            return child;
        }

        private static Renderer Block(Transform parent, string name, Vector3 position, Vector3 size,
            Material material, bool solid = false)
        {
            GameObject block = GameObject.CreatePrimitive(PrimitiveType.Cube);
            block.name = name;
            block.transform.SetParent(parent, false);
            block.transform.localPosition = position;
            block.transform.localScale = size;
            if (!solid) Object.DestroyImmediate(block.GetComponent<Collider>());
            Renderer renderer = block.GetComponent<Renderer>();
            renderer.sharedMaterial = material;
            return renderer;
        }

        private static Material Material(string name, string color, Texture2D texture = null, bool emissive = false)
        {
            string path = Surface + name.Replace(" ", "") + ".mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (material != null) return material;
            material = new Material(Shader.Find("Universal Render Pipeline/Lit")) { name = name };
            if (ColorUtility.TryParseHtmlString(color, out Color tint)) material.color = tint;
            material.SetFloat("_Smoothness", 0.22f);
            material.mainTexture = texture;
            material.enableInstancing = true;
            if (emissive)
            {
                material.EnableKeyword("_EMISSION");
                material.SetColor("_EmissionColor", material.color * 1.6f);
            }
            AssetDatabase.CreateAsset(material, path);
            return material;
        }

        private static GameObject FitModel(string name, Transform parent, Vector3 size,
            Vector3 center, float yaw = 0f, bool preserveProportions = false)
        {
            GameObject source = AssetDatabase.LoadAssetAtPath<GameObject>(Models + name + ".fbx");
            Transform holder = Child(parent, name + " model");
            GameObject model = (GameObject)PrefabUtility.InstantiatePrefab(source, holder);
            model.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
            Renderer[] renderers = model.GetComponentsInChildren<Renderer>();
            Bounds bounds = renderers[0].bounds;
            foreach (Renderer renderer in renderers.Skip(1)) bounds.Encapsulate(renderer.bounds);
            Vector3 min = parent.InverseTransformPoint(bounds.min);
            Vector3 max = parent.InverseTransformPoint(bounds.max);
            Vector3 sourceSize = max - min;
            Vector3 scale = new Vector3(size.x / sourceSize.x, size.y / sourceSize.y, size.z / sourceSize.z);
            if (preserveProportions) scale = Vector3.one * Mathf.Min(scale.x, scale.y, scale.z);
            holder.localScale = scale;
            holder.localPosition = new Vector3(center.x - (min.x + max.x) * 0.5f * scale.x,
                center.y - size.y * 0.5f - min.y * scale.y, center.z - (min.z + max.z) * 0.5f * scale.z);
            return model;
        }

        private static void AssignMaterial(GameObject model, Material material)
        {
            foreach (Renderer renderer in model.GetComponentsInChildren<Renderer>())
            {
                renderer.sharedMaterials = Enumerable.Repeat(material, renderer.sharedMaterials.Length).ToArray();
            }
        }

        private static void SetRenderers(Component component, string propertyName, Renderer[] renderers)
        {
            SerializedObject serialized = new SerializedObject(component);
            SerializedProperty property = serialized.FindProperty(propertyName);
            property.arraySize = renderers.Length;
            for (int index = 0; index < renderers.Length; index++)
            {
                property.GetArrayElementAtIndex(index).objectReferenceValue = renderers[index];
            }
            serialized.ApplyModifiedPropertiesWithoutUndo();
        }
    }
}
