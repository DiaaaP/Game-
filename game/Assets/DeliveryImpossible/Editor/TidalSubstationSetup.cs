using DeliveryImpossible.Orders;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private const float SubstationDeckHeight = 2.4f;

        private static DeliveryCheckpoint[] BuildSubstationStages(Transform station, Material deck, Material wall,
            Material brick, Material steel, Material amber, Material green)
        {
            Material red = Material("Tidal Steam Warning", "#FF513B", emissive: true);
            Material copper = Material("Tidal Copper Pipes", "#A66B48");
            Material grate = Material("Tidal Iron Grate", "#71858C");
            var stages = new DeliveryCheckpoint[4];
            var controller = station.gameObject.AddComponent<TidalSubstation>();
            var data = new SerializedObject(controller);
            data.FindProperty("danger").objectReferenceValue = red;
            data.FindProperty("caution").objectReferenceValue = amber;
            data.FindProperty("clear").objectReferenceValue = green;

            Renderer signal = BuildChargeBay(station, steel, amber, out stages[TidalSubstation.Charger]);
            data.FindProperty("chargerSignal").objectReferenceValue = signal;
            data.FindProperty("bridgeShutter").objectReferenceValue = BuildSubstationShutter(station, wall, steel, amber);
            Transform bridgeLanding = Child(station, "Bridge route marker");
            bridgeLanding.localPosition = new Vector3(4.1f, 0.45f, 28.0f);
            data.FindProperty("bridgeLanding").objectReferenceValue = bridgeLanding;
            SerializedProperty banks = data.FindProperty("steamBanks");
            banks.arraySize = 2;
            Material steamMaterial = SubstationSteamMaterial();
            for (int index = 0; index < banks.arraySize; index++)
            {
                float x = index == 0 ? -2.95f : 2.95f;
                float z = index == 0 ? 40.3f : 54.2f;
                stages[TidalSubstation.FirstSteamBank + index] = BuildSteamBank(station, x, z, index + 1,
                    brick, steel, copper, grate, amber, steamMaterial, banks.GetArrayElementAtIndex(index));
            }

            BuildSubstationRamp(station, deck, steel, amber);
            MarkPickup(station, new Vector3(0f, SubstationDeckHeight + 0.027f, 81.35f), 10.35f, 3.7f, green);
            CoursePost(station, "ВЕРХНЯЯ ПЛОЩАДКА", new Vector3(5.85f, SubstationDeckHeight, 80.45f), green, steel);
            stages[TidalSubstation.LiftCheck] = Volume(station, "Upper lifting bay",
                new Vector3(0f, SubstationDeckHeight + 1.32f, 81.35f), new Vector3(10.35f, 2.75f, 3.7f));

            BuildGeneratorHall(station, wall, brick, steel, red, green, amber, data);
            Transform[] recovery =
            {
                SpawnAt(station, new Vector3(2.59f, 0f, 12.25f)),
                SpawnAt(station, new Vector3(-2.95f, 0f, 43.4f)),
                SpawnAt(station, new Vector3(2.20f, 0f, 57.05f)),
                SpawnAt(station, new Vector3(0f, SubstationDeckHeight, 83.85f))
            };
            SetObjects(data.FindProperty("recoveryPoints"), recovery);
            data.ApplyModifiedPropertiesWithoutUndo();
            return stages;
        }

        private static Renderer BuildChargeBay(Transform station, Material steel, Material amber,
            out DeliveryCheckpoint contact)
        {
            const float z = 9.55f;
            foreach (float x in new[] { -2.08f, 2.08f })
            {
                Block(station, "Contact pad plate", new Vector3(x, 0.013f, z),
                    new Vector3(3.85f, 0.012f, 3.1f), steel);
                MarkPickup(station, new Vector3(x, 0.027f, z), 3.83f, 3.08f, amber);
                for (int bar = -2; bar <= 2; bar++)
                    Block(station, "Power contact strip", new Vector3(x + bar * 0.51f, 0.025f, z),
                        new Vector3(0.14f, 0.010f, 1.75f), amber);
            }
            CourseLabel(station, "ПОСТАВЬТЕ ГРУЗ НА КОНТАКТЫ", new Vector3(0f, 2.95f, 10.85f), 6.2f, amber, steel);
            foreach (float x in new[] { -4.5f, 4.5f })
            {
                RoundedBlock(station, "Charger column", new Vector3(x, 1.42f, 10.85f),
                    new Vector3(0.38f, 2.84f, 0.52f), steel, solid: true);
                Block(station, "Charger cable duct", new Vector3(x, 0.052f, z),
                    new Vector3(0.22f, 0.10f, 3.12f), steel);
            }
            contact = Volume(station, "Cargo contact scanner", new Vector3(0f, 0.83f, z), new Vector3(8.08f, 1.7f, 3.1f));
            return Block(station, "Charger power light", new Vector3(0f, 3.23f, 10.77f), new Vector3(3.65f, 0.08f, 0.08f), amber);
        }

        private static Transform BuildSubstationShutter(Transform station, Material wall, Material steel, Material amber)
        {
            const float x = 4.1f;
            const float z = 15.85f;
            foreach (float side in new[] { -1f, 1f })
                Block(station, "Powered shutter guide", new Vector3(x + side * 2.34f, 2.72f, z),
                    new Vector3(0.29f, 5.44f, 0.6f), steel, true);
            Block(station, "Shutter motor box", new Vector3(x, 5.58f, z), new Vector3(5.1f, 0.57f, 0.83f), steel);
            CourseLabel(station, "ШЛЮЗ МОСТА", new Vector3(x, 5.57f, z - 0.45f), 3.15f, amber, steel);
            Transform shutter = Child(station, "Powered bridge shutter");
            shutter.localPosition = new Vector3(x, 1.72f, z);
            RoundedBlock(shutter, "Shutter panel", Vector3.zero, new Vector3(4.42f, 3.38f, 0.27f), wall, solid: true);
            for (int slat = -4; slat <= 4; slat++)
                Block(shutter, "Shutter slat", new Vector3(0f, slat * 0.34f, -0.14f),
                    new Vector3(4.31f, 0.058f, 0.048f), steel);
            Block(shutter, "Shutter safety edge", new Vector3(0f, -1.52f, -0.146f),
                new Vector3(4.3f, 0.14f, 0.048f), amber);
            return shutter;
        }

        private static DeliveryCheckpoint BuildSteamBank(Transform station, float x, float z, int number,
            Material brick, Material steel, Material copper, Material grate, Material amber, Material steamMaterial,
            SerializedProperty bank)
        {
            const float opening = 6.05f;
            float left = x - opening * 0.5f;
            float right = x + opening * 0.5f;
            Partition(station, brick, -10f, left, 0f, 3.85f, z);
            Partition(station, brick, right, 10f, 0f, 3.85f, z);
            CheckArch(station, new Vector3(x, 0f, z - 0.42f), opening, steel, amber, $"ПАРОВОЙ КАНАЛ {number}");

            Transform clockSign = Child(station, "Steam countdown");
            clockSign.localPosition = new Vector3(x, 2.73f, z - 0.60f);
            CourseLabel(clockSign, "ПАРОВЫЙ КАНАЛ", Vector3.zero, 3.15f, amber, steel);
            TMP_Text clock = clockSign.GetComponentInChildren<TMP_Text>();
            Renderer signal = Block(station, "Steam phase signal", new Vector3(x, 3.49f, z - 0.58f),
                new Vector3(4.86f, 0.085f, 0.095f), amber);
            for (int row = -4; row <= 4; row++)
            {
                Block(station, "Steam drain grate", new Vector3(x, 0.017f, z + row * 0.44f),
                    new Vector3(5.77f, 0.014f, 0.25f), grate);
            }
            MarkPickup(station, new Vector3(x, 0.032f, z - 3.37f), 3.78f, 1.55f, amber);
            CoursePost(station, "ЗОНА ОЖИДАНИЯ", new Vector3(x + 3.4f, 0f, z - 3.43f), amber, steel);
            Cylinder(station, "Pressure header", new Vector3(x, 4.45f, z + 0.10f), 0.72f, 9.4f,
                copper, Quaternion.Euler(0f, 0f, 90f));
            foreach (float side in new[] { -1f, 1f })
            {
                float pipe = x + side * (opening * 0.5f + 0.43f);
                Cylinder(station, "Steam riser", new Vector3(pipe, 2.25f, z + 0.58f), 0.58f, 4.5f,
                    copper, Quaternion.identity, solid: true);
                Cylinder(station, "Steam pressure gauge", new Vector3(pipe, 1.71f, z - 0.33f), 0.51f, 0.22f,
                    steel, Quaternion.Euler(90f, 0f, 0f));
                Lamp(station, new Vector3(x + side * 2.2f, 3.65f, z - 1.75f), new Color(1f, 0.67f, 0.43f), 12f, 3.1f);
            }
            DeliveryCheckpoint passage = Volume(station, $"Steam crossing {number}", new Vector3(x, 1.30f, z),
                new Vector3(opening - 0.18f, 2.7f, 4.35f));
            ParticleSystem steam = BuildSteamJet(station, new Vector3(x, 1.05f, z + 1.87f), steamMaterial);
            bank.FindPropertyRelative("passage").objectReferenceValue = passage;
            bank.FindPropertyRelative("steam").objectReferenceValue = steam;
            bank.FindPropertyRelative("signal").objectReferenceValue = signal;
            bank.FindPropertyRelative("clock").objectReferenceValue = clock;
            return passage;
        }

        private static ParticleSystem BuildSteamJet(Transform station, Vector3 position, Material material)
        {
            var emitter = Child(station, "Live steam").gameObject.AddComponent<ParticleSystem>();
            emitter.transform.SetLocalPositionAndRotation(position, Quaternion.Euler(0f, 180f, 0f));
            var main = emitter.main;
            main.playOnAwake = false;
            main.duration = 3.4f;
            main.loop = true;
            main.startLifetime = new ParticleSystem.MinMaxCurve(0.48f, 0.89f);
            main.startSpeed = new ParticleSystem.MinMaxCurve(4.4f, 6.1f);
            main.startSize = new ParticleSystem.MinMaxCurve(0.40f, 1.03f);
            main.startRotation = new ParticleSystem.MinMaxCurve(0f, Mathf.PI * 2f);
            main.startColor = new Color(0.88f, 0.95f, 0.99f, 0.58f);
            main.maxParticles = 220;
            var emission = emitter.emission;
            emission.rateOverTime = 135f;
            var shape = emitter.shape;
            shape.shapeType = ParticleSystemShapeType.Box;
            shape.scale = new Vector3(5.26f, 1.09f, 0.16f);
            shape.randomDirectionAmount = 0.13f;
            var color = emitter.colorOverLifetime;
            color.enabled = true;
            var fade = new Gradient();
            fade.SetKeys(new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new[] { new GradientAlphaKey(0.08f, 0f), new GradientAlphaKey(0.78f, 0.18f), new GradientAlphaKey(0f, 1f) });
            color.color = fade;
            var renderer = emitter.GetComponent<ParticleSystemRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return emitter;
        }

        private static Material SubstationSteamMaterial()
        {
            const string materialPath = Surface + "TidalSteam.mat";
            Material material = AssetDatabase.LoadAssetAtPath<Material>(materialPath);
            if (material != null) return material;
            const int size = 64;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false) { name = "Tidal vapor", wrapMode = TextureWrapMode.Clamp };
            var pixels = new Color[size * size];
            for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
            {
                var point = new Vector2((x + 0.5f) / size - 0.5f, (y + 0.5f) / size - 0.5f);
                float density = Mathf.SmoothStep(1f, 0f, Mathf.Clamp01(point.magnitude * 2.0f));
                pixels[y * size + x] = new Color(1f, 1f, 1f, density * density);
            }
            texture.SetPixels(pixels);
            texture.Apply();
            material = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit")) { name = "TidalSteam" };
            material.SetTexture("_BaseMap", texture);
            material.SetFloat("_Surface", 1f);
            material.SetFloat("_SrcBlend", (float)BlendMode.SrcAlpha);
            material.SetFloat("_DstBlend", (float)BlendMode.OneMinusSrcAlpha);
            material.SetFloat("_ZWrite", 0f);
            material.SetOverrideTag("RenderType", "Transparent");
            material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
            material.renderQueue = (int)RenderQueue.Transparent;
            AssetDatabase.CreateAsset(material, materialPath);
            AssetDatabase.AddObjectToAsset(texture, material);
            return material;
        }

        private static void BuildSubstationRamp(Transform station, Material deck, Material steel, Material amber)
        {
            const float rampHalfWidth = 3.95f;
            const float start = 60.4f;
            const float end = 76.11f;
            Vector3 toe = new Vector3(0f, -0.04f, start);
            Vector3 summit = new Vector3(0f, SubstationDeckHeight, end);
            Vector3 climb = summit - toe;
            Quaternion incline = Quaternion.LookRotation(climb);
            Transform ramp = Child(station, "Generator ramp");
            ramp.SetLocalPositionAndRotation((toe + summit) * 0.5f, incline);
            Block(ramp, "Walkable generator ramp", Vector3.down * 0.17f,
                new Vector3(rampHalfWidth * 2f, 0.34f, climb.magnitude), deck, true);
            for (float z = -7f; z <= 7f; z += 1.15f)
            {
                Block(ramp, "Ramp traction strip", new Vector3(0f, 0.008f, z),
                    new Vector3(7.58f, 0.012f, 0.095f), steel);
                Arrow(ramp, new Vector3(0f, 0.024f, z), amber, 0.75f);
            }
            foreach (float side in new[] { -1f, 1f })
            {
                Vector3 edge = Vector3.right * (side * (rampHalfWidth - 0.12f));
                Guardrail(station, toe + edge, summit + edge, steel);
                Guardrail(station, summit + edge,
                    new Vector3(side * 9.7f, SubstationDeckHeight, 76.19f), steel);
                for (float z = 65f; z <= 75f; z += 4.8f)
                {
                    float height = Mathf.Lerp(0f, SubstationDeckHeight, (z - start) / (end - start));
                    Block(station, "Ramp support", new Vector3(side * 3.75f, height * 0.45f, z),
                        new Vector3(0.37f, height * 0.9f, 0.50f), steel, true);
                }
            }
            CoursePost(station, "ПОДЪЁМ К ГЕНЕРАТОРАМ", new Vector3(4.47f, 0f, 60.7f), amber, steel);
        }

        private static void BuildGeneratorHall(Transform station, Material wall, Material brick, Material steel,
            Material red, Material green, Material amber, SerializedObject data)
        {
            const float front = 76.03f;
            const float back = 102.0f;
            float height = SubstationDeckHeight;
            Floor(station, brick, 20f, front, back, height);
            Block(station, "Generator deck fascia", new Vector3(0f, (height - 0.3f) * 0.5f, front + 0.10f),
                new Vector3(19.94f, height - 0.3f, 0.20f), steel, true);
            foreach (float side in new[] { -1f, 1f })
            {
                Guardrail(station, new Vector3(side * 9.72f, height, 76.15f),
                    new Vector3(side * 9.72f, height, 101.7f), steel);
                Block(station, "Switchyard siding", new Vector3(side * 9.88f, height + 2f, 94f),
                    new Vector3(0.28f, 4f, 15.65f), wall, true);
                foreach (float z in new[] { 86.0f, 92.6f, 99.6f })
                    Block(station, "Switchyard structural rib", new Vector3(side * 9.42f, height + 2.6f, z),
                        new Vector3(0.39f, 5.2f, 0.47f), steel, true);
            }
            Partition(station, brick, -10f, 10f, height - 0.25f, height + 5.37f, 102f);
            Block(station, "Switchyard roof", new Vector3(0f, height + 5.32f, 94.15f),
                new Vector3(20.45f, 0.38f, 16.1f), wall);
            CheckArch(station, new Vector3(0f, height, 86.10f), 12.5f, steel, green, "ЩИТОВАЯ");
            CourseLabel(station, "ПРИЛИВНАЯ ПОДСТАНЦИЯ", new Vector3(0f, height + 4.15f, 100.26f), 8.1f, green, steel);
            RoundedBlock(station, "Substation control cabinet", new Vector3(0f, height + 1.12f, 98.2f),
                new Vector3(2.5f, 2.24f, 1.3f), wall, solid: true);
            Lamp(station, new Vector3(0f, height + 4.6f, 89.9f), new Color(1f, 0.77f, 0.53f), 14f, 4.6f);
            Lamp(station, new Vector3(0f, height + 4.6f, 98.2f), new Color(0.63f, 0.80f, 1f), 13f, 4.0f);

            var signals = new Renderer[2];
            var rotors = new Transform[2];
            var lights = new Light[2];
            for (int circuit = 0; circuit < signals.Length; circuit++)
            {
                float x = circuit == 0 ? -4.2f : 4.2f;
                Material paint = circuit == 0 ? green : amber;
                BuildGenerator(station, new Vector3(x, height, 97.5f), wall, steel, red, paint,
                    circuit == 0 ? "МОДУЛЬ ПИТАНИЯ" : "РЕЗЕРВНЫЙ АККУМУЛЯТОР",
                    out signals[circuit], out rotors[circuit], out lights[circuit]);
                MarkPickup(station, new Vector3(x, height + 0.037f, 92.4f), 4.55f, 4.55f, paint);
            }
            SetObjects(data.FindProperty("circuitSignals"), signals);
            SetObjects(data.FindProperty("generatorRotors"), rotors);
            SetObjects(data.FindProperty("circuitLights"), lights);
        }

        private static void BuildGenerator(Transform station, Vector3 point, Material shell, Material steel,
            Material standby, Material trim, string label, out Renderer signal, out Transform rotor, out Light lamp)
        {
            RoundedBlock(station, "Station generator plinth", point + Vector3.up * 0.26f,
                new Vector3(4.36f, 0.52f, 2.7f), steel, solid: true);
            RoundedBlock(station, "Station generator housing", point + new Vector3(0f, 2.03f, 0.25f),
                new Vector3(4.1f, 3.05f, 1.9f), shell, solid: true);
            Vector3 center = point + new Vector3(0f, 2.05f, -0.82f);
            Quaternion face = Quaternion.Euler(90f, 0f, 0f);
            Cylinder(station, "Rotor recess", center, 2.38f, 0.30f, steel, face, solid: true);
            rotor = Child(station, "Generator rotor");
            rotor.localPosition = center + Vector3.back * 0.20f;
            for (int blade = 0; blade < 8; blade++)
            {
                Quaternion angle = Quaternion.Euler(0f, 0f, 45f * blade);
                Renderer fin = RoundedBlock(rotor, "Rotor blade", angle * new Vector3(0f, 0.62f, 0f),
                    new Vector3(0.28f, 0.76f, 0.12f), shell);
                fin.transform.localRotation = angle * Quaternion.Euler(0f, 0f, 14f);
            }
            Cylinder(rotor, "Rotor pin", Vector3.back * 0.08f, 0.54f, 0.24f, trim, face);
            CourseLabel(station, label, point + new Vector3(0f, 0.82f, -0.74f), 3.28f, trim, steel);
            signal = Block(station, "Circuit status light", point + new Vector3(0f, 3.26f, -0.74f),
                new Vector3(2.44f, 0.11f, 0.11f), standby);
            lamp = Child(station, "Online circuit light").gameObject.AddComponent<Light>();
            lamp.transform.localPosition = point + new Vector3(0f, 2.8f, -1.35f);
            lamp.color = trim.color;
            lamp.range = 6.1f;
            lamp.intensity = 5f;
            lamp.enabled = false;
        }
    }
}
