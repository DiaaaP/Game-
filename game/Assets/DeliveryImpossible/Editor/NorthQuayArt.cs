using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private const string PortTextures = Root + "/PortSurfaces/";
        private static readonly Color HarborHaze = new Color(0.46f, 0.55f, 0.62f);
        private static readonly Quaternion HarborSun = Quaternion.Euler(19f, 210f, 0f);

        private static Material QuaySurface(string name, string texture, string tint, float tileMeters)
        {
            Material mat = Material(name, tint);
            mat.shader = Shader.Find("North Quay/World Surface");
            ColorUtility.TryParseHtmlString(tint, out Color color);
            mat.SetColor("_BaseColor", color);
            mat.SetTexture("_BaseMap", AssetDatabase.LoadAssetAtPath<Texture2D>(PortTextures + texture + ".jpg"));
            string normalPath = PortTextures + texture + "Normal.jpg";
            if (AssetImporter.GetAtPath(normalPath) is TextureImporter importer &&
                (importer.textureType != TextureImporterType.NormalMap || importer.anisoLevel != 8))
            {
                importer.textureType = TextureImporterType.NormalMap;
                importer.anisoLevel = 8;
                importer.SaveAndReimport();
            }
            mat.SetTexture("_BumpMap", AssetDatabase.LoadAssetAtPath<Texture2D>(normalPath));
            mat.SetFloat("_BumpScale", texture == "SteelPanels" ? 0.68f : 0.48f);
            mat.SetFloat("_Smoothness", texture == "QuayBrick" ? 0.15f : 0.49f);
            mat.SetFloat("_TileSize", tileMeters);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static Material OpenWater()
        {
            Material mat = Material("North Quay Water", "#427D83");
            mat.shader = Shader.Find("North Quay/Harbor Water");
            mat.SetColor("_Deep", new Color(0.025f, 0.19f, 0.25f));
            ApplyPanorama(mat);
            Vector3 facingSun = -(HarborSun * Vector3.forward);
            mat.SetVector("_SunDirection", facingSun);
            EditorUtility.SetDirty(mat);
            return mat;
        }

        private static void ApplyPanorama(Material material)
        {
            material.SetTexture("_Panorama", AssetDatabase.LoadAssetAtPath<Texture2D>(PortTextures + "HarborSky.hdr"));
            material.SetColor("_HorizonColor", HarborHaze);
            material.SetFloat("_SkyRotation", -105f);
            material.SetFloat("_SkyExposure", 0.38f);
        }

        private static void SetPortLighting(Transform scenery)
        {
            Material sky = Material("Harbor Panorama", "#FFFFFF");
            sky.shader = Shader.Find("North Quay/Harbor Sky");
            ApplyPanorama(sky);
            EditorUtility.SetDirty(sky);

            RenderSettings.skybox = sky;
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.66f, 0.74f, 0.87f);
            RenderSettings.ambientEquatorColor = new Color(0.47f, 0.58f, 0.69f);
            RenderSettings.ambientGroundColor = new Color(0.29f, 0.36f, 0.47f);
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = 0.0058f;
            RenderSettings.fogColor = HarborHaze;

            Light sun = GameObject.Find("Directional Light").GetComponent<Light>();
            sun.transform.rotation = HarborSun;
            sun.intensity = 1.25f;
            sun.color = new Color(1f, 0.80f, 0.64f);
            sun.shadows = LightShadows.Soft;
            sun.shadowStrength = 0.84f;

            UniversalRenderPipelineAsset pipeline = AssetDatabase.LoadAssetAtPath<UniversalRenderPipelineAsset>(
                "Assets/Settings/PC_RPAsset.asset");
            if (pipeline != null)
            {
                pipeline.msaaSampleCount = 4;
                pipeline.shadowDistance = 75f;
                EditorUtility.SetDirty(pipeline);
            }

            SetPortPostProcessing(scenery);
        }

        private static void SetPortPostProcessing(Transform scenery)
        {
            string path = PortTextures + "HarborGrade.asset";
            var profile = AssetDatabase.LoadAssetAtPath<VolumeProfile>(path);
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                AssetDatabase.CreateAsset(profile, path);
            }
            if (!profile.TryGet(out Bloom bloom))
            {
                bloom = profile.Add<Bloom>();
                AssetDatabase.AddObjectToAsset(bloom, profile);
            }
            bloom.threshold.Override(1.2f);
            bloom.intensity.Override(0.23f);
            bloom.scatter.Override(0.62f);
            EditorUtility.SetDirty(bloom);
            if (!profile.TryGet(out ColorAdjustments grade))
            {
                grade = profile.Add<ColorAdjustments>();
                AssetDatabase.AddObjectToAsset(grade, profile);
            }
            grade.contrast.Override(7f);
            grade.saturation.Override(1.5f);
            EditorUtility.SetDirty(grade);
            EditorUtility.SetDirty(profile);
            var volume = Child(scenery, "Harbor post-processing").gameObject.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = profile;

            var renderer = AssetDatabase.LoadAssetAtPath<UniversalRendererData>("Assets/Settings/PC_Renderer.asset");
            var ambientOcclusion = renderer.rendererFeatures.Find(feature => feature.name == "ScreenSpaceAmbientOcclusion");
            if (ambientOcclusion != null)
            {
                var settings = new SerializedObject(ambientOcclusion);
                settings.FindProperty("m_Settings.Source").enumValueIndex = 0;
                settings.FindProperty("m_Settings.AfterOpaque").boolValue = true;
                settings.FindProperty("m_Settings.Intensity").floatValue = 0.65f;
                settings.FindProperty("m_Settings.Radius").floatValue = 0.48f;
                settings.FindProperty("m_Settings.Downsample").boolValue = true;
                settings.ApplyModifiedPropertiesWithoutUndo();
                EditorUtility.SetDirty(renderer);
            }
        }

        private static void ConfigureQuayCamera(Camera camera)
        {
            camera.allowHDR = true;
            camera.farClipPlane = 340f;
            UniversalAdditionalCameraData data = camera.GetUniversalAdditionalCameraData();
            data.renderPostProcessing = true;
            data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
            data.antialiasingQuality = AntialiasingQuality.High;
        }

        private static GameObject ImportedProp(string model, bool maritime, Transform parent, Vector3 size,
            Vector3 center, float yaw = 0f, bool solid = false)
        {
            GameObject prop = FitModel("Port/" + model, parent, size, center, yaw, true);
            string palette = maritime ? "BoatPalette" : "FactoryPalette";
            Texture2D atlas = AssetDatabase.LoadAssetAtPath<Texture2D>(Models + "Port/" + palette + ".png");
            AssignMaterial(prop, Material(palette, "#DDE2DE", atlas));
            if (solid)
            {
                foreach (MeshFilter mesh in prop.GetComponentsInChildren<MeshFilter>())
                {
                    if (mesh.sharedMesh == null) continue;
                    Bounds bounds = mesh.sharedMesh.bounds;
                    if (bounds.size.x < 0.001f || bounds.size.y < 0.001f || bounds.size.z < 0.001f) continue;
                    BoxCollider collider = mesh.gameObject.AddComponent<BoxCollider>();
                    collider.center = bounds.center;
                    collider.size = bounds.size;
                }
            }
            return prop;
        }

        private static void HarborRail(Transform quay, float x, float from, float to, Material rail) =>
            Guardrail(quay, new Vector3(x, 0f, from), new Vector3(x, 0f, to), rail);

        private static void Guardrail(Transform parent, Vector3 start, Vector3 end, Material rail)
        {
            Vector3 span = end - start;
            float length = span.magnitude;
            Transform section = Child(parent, "Guardrail");
            section.SetLocalPositionAndRotation((start + end) * 0.5f, Quaternion.LookRotation(span));

            BoxCollider barrier = section.gameObject.AddComponent<BoxCollider>();
            barrier.center = Vector3.up * 0.49f;
            barrier.size = new Vector3(0.11f, 0.98f, length);
            Block(section, "Steel guard rail", Vector3.up * 0.92f, new Vector3(0.11f, 0.105f, length), rail);
            Block(section, "Lower rail", Vector3.up * 0.44f, new Vector3(0.075f, 0.075f, length), rail);
            int gaps = Mathf.CeilToInt(length / 2.55f);
            for (int index = 0; index < gaps; index++)
            {
                float z = Mathf.Lerp(-length * 0.5f, length * 0.5f, (index + 0.5f) / gaps);
                Block(section, "Rail upright", new Vector3(0f, 0.46f, z), new Vector3(0.105f, 0.92f, 0.10f), rail);
            }
        }

        private static void BerthScenery(Transform quay, Material steel)
        {
            BuildCoastalVista(quay, powerStation: false);
            BuildFreighter(quay, new Vector3(25.0f, -0.39f, 43.0f));
            ImportedProp("Tug", true, quay, new Vector3(5.8f, 5.6f, 10f), new Vector3(-14.5f, 1.1f, 17.2f), -17f);
            ImportedProp("FishingBoat", true, quay, new Vector3(4.0f, 3.25f, 7.8f), new Vector3(-13.0f, 0.18f, 51.0f), 14f);
            ImportedProp("Crane", false, quay, new Vector3(10f, 10f, 10f), new Vector3(-14.5f, 5.4f, 50f), 90f);
            for (int index = 0; index < 6; index++)
            {
                Vector3 point = new Vector3(-7.9f, 1.17f, 32f + index * 5f);
                ImportedProp("Container", true, quay, new Vector3(2.3f, 2.4f, 4.7f), point, solid: true);
                if (index % 2 == 0)
                    ImportedProp("Container", true, quay, new Vector3(2.3f, 2.4f, 4.7f), point + Vector3.up * 2.4f, solid: true);
            }

            Material rock = QuaySurface("Breakwater Stone", "IndustrialFloor", "#778687", 3.36f);
            Block(quay, "Outer breakwater", new Vector3(-62f, -0.58f, 78.5f), new Vector3(66f, 2.24f, 5.62f), rock);
            Block(quay, "Breakwater coping", new Vector3(-62f, 0.56f, 78.15f), new Vector3(66f, 0.22f, 2.55f), steel);
            ImportedProp("Crane", false, quay, new Vector3(17f, 16f, 17f), new Vector3(27f, 8.6f, 99f), 180f);
            ImportedProp("Tug", true, quay, new Vector3(4.8f, 4.8f, 8f), new Vector3(-64f, 0.2f, 102f), 35f);
        }
    }
}
