using DeliveryImpossible.Interaction;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static GameObject MakeSpecialCargo(string name)
        {
            (Vector3 size, float mass, bool fragile, bool team) = name switch
            {
                "HarborBuoy" => (new Vector3(0.83f, 1.19f, 0.83f), 7f, false, false),
                "FoundryGearbox" => (new Vector3(0.99f, 0.68f, 1.22f), 36f, false, true),
                "RollerCassette" => (new Vector3(1.03f, 0.49f, 1.24f), 14f, false, false),
                "MuseumVase" => (new Vector3(0.77f, 1.08f, 0.77f), 6f, true, false),
                "FramedPainting" => (new Vector3(1.13f, 1.24f, 0.42f), 5f, true, false),
                "IceSpecimen" => (new Vector3(0.86f, 1.10f, 0.86f), 8f, false, false),
                "OrbitalFuelCell" => (new Vector3(1.02f, 1.12f, 0.82f), 11f, false, false),
                _ => throw new System.ArgumentException("Unknown campaign cargo.", nameof(name))
            };

            GameObject item = new GameObject(name);
            try
            {
                Rigidbody body = item.AddComponent<Rigidbody>();
                body.mass = mass;
                body.linearDamping = 0.22f;
                body.angularDamping = 0.70f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                item.AddComponent<BoxCollider>().size = size;
                var grabbable = item.AddComponent<NetworkGrabbable>();
                SerializedObject hold = new SerializedObject(grabbable);
                hold.FindProperty("maxHolders").intValue = team ? 2 : 1;
                hold.FindProperty("requiredHolders").intValue = team ? 2 : 1;
                hold.ApplyModifiedPropertiesWithoutUndo();

                Transform art = Child(item.transform, "Cargo Model");
                Renderer damageSurface = SculptCargo(art, name);
                SetRenderers(grabbable, "highlightedRenderers", art.GetComponentsInChildren<Renderer>());
                if (fragile)
                {
                    var condition = item.AddComponent<NetworkFragileCargo>();
                    SetRenderers(condition, "damageRenderers", new[] { damageSurface });
                    SerializedObject durability = new SerializedObject(condition);
                    durability.FindProperty("breakingImpactSpeed").floatValue = 9.7f;
                    durability.FindProperty("safeImpactSpeed").floatValue = 4.1f;
                    durability.FindProperty("intactColor").colorValue = damageSurface.sharedMaterial.color;
                    durability.ApplyModifiedPropertiesWithoutUndo();
                }

                return PrefabUtility.SaveAsPrefabAsset(item, Root + "/Prefabs/" + name + ".prefab");
            }
            finally
            {
                Object.DestroyImmediate(item);
            }
        }

        private static Renderer SculptCargo(Transform art, string name)
        {
            Material iron = Material("Campaign Cargo Frame", "#293746");
            Material steel = Material("Campaign Cargo Steel", "#9BAAB3");
            Material orange = Material("Cargo Vermilion", "#E4833D");
            Material pale = Material("Insulation White", "#D4E4EC");
            Material teal = Material("Specimen Blue", "#54ABC8");
            Material gold = Material("Museum Gilt", "#A47D51");
            switch (name)
            {
                case "HarborBuoy":
                    LathedProp(art, "Molded buoy shell", Vector3.zero, orange, "MarineBuoy", new[]
                    {
                        new Vector2(0.14f,-0.58f), new Vector2(0.25f,-0.55f), new Vector2(0.35f,-0.43f),
                        new Vector2(0.401f,-0.25f), new Vector2(0.408f,-0.052f), new Vector2(0.377f,0.13f),
                        new Vector2(0.32f,0.255f), new Vector2(0.22f,0.33f), new Vector2(0.10f,0.34f)
                    });
                    Cylinder(art, "Signal rim", new Vector3(0f, -0.055f, 0f), 0.83f, 0.095f, pale, Quaternion.identity);
                    Cylinder(art, "Mooring collar", new Vector3(0f, -0.49f, 0f), 0.58f, 0.11f, iron, Quaternion.identity);
                    Cylinder(art, "Beacon mast", new Vector3(0f, 0.35f, 0f), 0.13f, 0.29f, iron, Quaternion.identity);
                    Blob(art, "Beacon", new Vector3(0f, 0.50f, 0f), new Vector3(0.23f, 0.19f, 0.23f), orange);
                    break;
                case "FoundryGearbox":
                    Block(art, "Skid plate", new Vector3(0f, -0.27f, 0f), new Vector3(0.96f, 0.15f, 1.18f), iron);
                    RoundedBlock(art, "Cast gear housing", new Vector3(0f, -0.020f, 0f), new Vector3(0.87f, 0.53f, 0.79f), orange);
                    Cylinder(art, "Shaft casing", new Vector3(0f, 0.018f, -0.42f), 0.51f, 0.21f, steel, Quaternion.Euler(90f, 0f, 0f));
                    Cylinder(art, "Drive axle", new Vector3(0f, 0.018f, -0.53f), 0.22f, 0.12f, iron, Quaternion.Euler(90f, 0f, 0f));
                    for (int i = 0; i < 8; i++)
                    {
                        float angle = i * Mathf.PI / 4f;
                        Cylinder(art, "Flange bolt", new Vector3(Mathf.Cos(angle) * 0.203f, 0.018f + Mathf.Sin(angle) * 0.203f, -0.53f),
                            0.047f, 0.045f, iron, Quaternion.Euler(90f, 0f, 0f));
                    }
                    for (int i = -2; i <= 2; i++)
                        Block(art, "Cooling fin", new Vector3(i * 0.14f, 0.273f, 0.04f), new Vector3(0.056f, 0.09f, 0.62f), steel);
                    break;
                case "RollerCassette":
                    RoundedBlock(art, "Cassette frame", new Vector3(0f, -0.15f, 0f), new Vector3(1.0f, 0.17f, 1.21f), iron);
                    for (int i = -1; i <= 1; i++)
                        Cylinder(art, "Spare roller", new Vector3(0f, 0.047f, i * 0.34f), 0.30f, 0.84f, steel, Quaternion.Euler(0f, 0f, 90f));
                    foreach (float x in new[] { -0.46f, 0.46f })
                        Block(art, "Cassette end plate", new Vector3(x, 0.03f, 0f), new Vector3(0.093f, 0.42f, 1.13f), orange);
                    break;
                case "MuseumVase":
                    Material glaze = Material("Porcelain Teal", "#4D879B");
                    Cylinder(art, "Vase foot", new Vector3(0f, -0.49f, 0f), 0.59f, 0.094f, gold, Quaternion.identity);
                    Renderer vase = LathedProp(art, "Glazed porcelain", Vector3.zero, glaze, "PorcelainVase", new[]
                    {
                        new Vector2(0.221f,-0.48f), new Vector2(0.257f,-0.44f), new Vector2(0.304f,-0.32f),
                        new Vector2(0.362f,-0.19f), new Vector2(0.370f,-0.045f), new Vector2(0.340f,0.09f),
                        new Vector2(0.267f,0.214f), new Vector2(0.181f,0.307f), new Vector2(0.147f,0.391f),
                        new Vector2(0.181f,0.49f), new Vector2(0.225f,0.519f), new Vector2(0.171f,0.526f),
                        new Vector2(0.147f,0.467f)
                    });
                    Cylinder(art, "Gilt lip", new Vector3(0f, 0.49f, 0f), 0.401f, 0.035f, gold, Quaternion.identity);
                    for (int detail = 0; detail < 12; detail++)
                    {
                        float angle = detail * Mathf.PI / 6f;
                        Renderer enamel = Blob(art, "Gilt enamel", new Vector3(Mathf.Cos(angle) * 0.36f, -0.11f,
                            Mathf.Sin(angle) * 0.36f), new Vector3(0.022f, 0.105f, 0.063f), gold);
                        enamel.transform.localRotation = Quaternion.Euler(0f, -angle * Mathf.Rad2Deg, 0f);
                    }
                    return vase;
                case "FramedPainting":
                    Material linen = Material("Gallery Canvas", "#527881");
                    Block(art, "Standing support", new Vector3(0f, -0.57f, 0f), new Vector3(1.1f, 0.10f, 0.41f), iron);
                    RoundedBlock(art, "Molded picture frame", new Vector3(0f, 0.04f, 0f), new Vector3(1.10f, 1.12f, 0.18f), gold);
                    Renderer canvas = Block(art, "Painted canvas", new Vector3(0f, 0.04f, -0.101f), new Vector3(0.93f, 0.92f, 0.06f), linen);
                    Blob(art, "Painted sun", new Vector3(0.23f, 0.27f, -0.142f), new Vector3(0.24f, 0.24f, 0.026f), orange);
                    Renderer summit = Block(art, "Painted summit", new Vector3(-0.12f, -0.035f, -0.142f), new Vector3(0.46f, 0.46f, 0.022f), pale);
                    summit.transform.localRotation = Quaternion.Euler(0f, 0f, 45f);
                    Block(art, "Painted lake", new Vector3(0f, -0.263f, -0.153f), new Vector3(0.93f, 0.23f, 0.030f), teal);
                    Block(art, "Reflection", new Vector3(-0.03f, -0.257f, -0.171f), new Vector3(0.62f, 0.028f, 0.009f), pale);
                    return canvas;
                case "IceSpecimen":
                    LathedProp(art, "Frosted specimen vessel", Vector3.zero, teal, "IceSpecimenFlask", new[]
                    {
                        new Vector2(0.31f,-0.51f), new Vector2(0.377f,-0.39f), new Vector2(0.387f,-0.20f),
                        new Vector2(0.387f,0.26f), new Vector2(0.35f,0.42f), new Vector2(0.265f,0.50f)
                    });
                    for (int i = -1; i <= 1; i++)
                        Cylinder(art, "Insulated ring", new Vector3(0f, i * 0.45f, 0f), 0.84f, 0.10f, pale, Quaternion.identity);
                    Block(art, "Instrument case handle", new Vector3(0f, 0.53f, 0f), new Vector3(0.42f, 0.05f, 0.12f), iron);
                    break;
                case "OrbitalFuelCell":
                    Material violet = Material("Orbital Violet", "#B284FD", emissive: true);
                    RoundedBlock(art, "Fuel cell chassis", Vector3.zero, new Vector3(0.97f, 1.0f, 0.77f), iron);
                    foreach (float side in new[] { -1f, 1f })
                    {
                        Cylinder(art, "Fuel canister", new Vector3(side * 0.24f, 0.025f, -0.21f), 0.39f, 0.92f, steel, Quaternion.identity);
                        Block(art, "Fuel readout", new Vector3(side * 0.24f, 0.21f, -0.402f), new Vector3(0.17f, 0.34f, 0.035f), violet);
                    }
                    Block(art, "Fuel cap", new Vector3(0f, 0.511f, 0.02f), new Vector3(0.90f, 0.07f, 0.72f), steel);
                    Arrow(art, new Vector3(0f, 0.551f, 0.034f), violet, 0.70f);
                    break;
            }

            return null;
        }
    }
}
