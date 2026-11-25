using DeliveryImpossible.Interaction;
using UnityEditor;
using UnityEngine;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static GameObject MakeCampaignCargo(string name, Vector3 size, float mass, bool fragile = false,
            int maxHolders = 1)
        {
            GameObject cargo = new GameObject(name);
            try
            {
                Rigidbody body = cargo.AddComponent<Rigidbody>();
                body.mass = mass;
                body.linearDamping = 0.28f;
                body.angularDamping = 0.65f;
                body.interpolation = RigidbodyInterpolation.Interpolate;
                body.collisionDetectionMode = CollisionDetectionMode.ContinuousSpeculative;
                cargo.AddComponent<BoxCollider>().size = size;
                NetworkGrabbable grabbable = cargo.AddComponent<NetworkGrabbable>();
                SerializedObject hold = new SerializedObject(grabbable);
                hold.FindProperty("maxHolders").intValue = maxHolders;
                hold.ApplyModifiedPropertiesWithoutUndo();

                Transform art = Child(cargo.transform, "Cargo Model");
                Material frame = Material("Campaign Cargo Frame", "#293746");
                Material silver = Material("Campaign Cargo Steel", "#9BAAB3");
                Material shell = fragile ? Material("Module Ochre", "#D6903F") : Material("Thermal Case Blue", "#327994");
                RoundedBlock(art, "Molded shell", Vector3.zero, size - new Vector3(0.04f, 0.06f, 0.05f), shell);
                RoundedBlock(art, "Base", new Vector3(0f, -size.y * 0.46f, 0f), new Vector3(size.x, 0.065f, size.z), frame);
                foreach (float x in new[] { -1f, 1f })
                {
                    Block(art, "Lid latch", new Vector3(x * size.x * 0.30f, size.y * 0.34f, -size.z * 0.48f),
                        new Vector3(0.12f, 0.14f, 0.052f), silver);
                    Block(art, "Lifting grip", new Vector3(x * size.x * 0.48f, 0.16f, 0f),
                        new Vector3(0.075f, 0.07f, size.z * 0.51f), frame);
                }

                Material signal = Material("Thermal Indicator", "#8DEEF4", emissive: true);
                float displayX = fragile ? 0.28f : 0f;
                Renderer display = Block(art, "Status display", new Vector3(displayX, -0.002f, -size.z * 0.49f),
                    new Vector3(size.x * 0.29f, 0.19f, 0.035f), signal);
                if (fragile)
                {
                    RoundedBlock(art, "Lid", new Vector3(0f, size.y * 0.46f, 0f), new Vector3(size.x, 0.065f, size.z), frame);
                    for (int vent = -2; vent <= 2; vent++)
                    {
                        Block(art, "Module heat sink", new Vector3(0f, 0.13f * vent, size.z * 0.48f),
                            new Vector3(size.x * 0.73f, 0.052f, 0.045f), silver);
                    }

                    Vector3 fan = new Vector3(-0.22f, -0.02f, -0.53f);
                    Cylinder(art, "Fan housing", fan, 0.48f, 0.045f, frame, Quaternion.Euler(90f, 0f, 0f));
                    Cylinder(art, "Fan hub", fan + Vector3.back * 0.026f, 0.16f, 0.03f, silver, Quaternion.Euler(90f, 0f, 0f));
                    for (int blade = 0; blade < 4; blade++)
                    {
                        Renderer grille = Block(art, "Cooling fan grille", fan + Vector3.back * 0.027f,
                            new Vector3(0.39f, 0.024f, 0.016f), silver);
                        grille.transform.localRotation = Quaternion.Euler(0f, 0f, 45f * blade);
                    }

                    Cylinder(art, "Power coupling", new Vector3(0.26f, 0.44f, 0.13f), 0.26f, 0.075f, frame, Quaternion.identity);
                    Cylinder(art, "Power coupling cap", new Vector3(0.26f, 0.485f, 0.13f), 0.17f, 0.04f, silver, Quaternion.identity);
                    Arrow(art, new Vector3(-0.19f, 0.443f, 0.03f),
                        Material("Orbital Cyan", "#76E9E8", emissive: true), 0.68f);
                    NetworkFragileCargo protection = cargo.AddComponent<NetworkFragileCargo>();
                    SetRenderers(protection, "damageRenderers", new[] { display });
                    SerializedObject impact = new SerializedObject(protection);
                    impact.FindProperty("safeImpactSpeed").floatValue = 4.5f;
                    impact.FindProperty("breakingImpactSpeed").floatValue = 10.5f;
                    impact.ApplyModifiedPropertiesWithoutUndo();
                }
                else
                {
                    RoundedBlock(art, "Insulated lid", new Vector3(0f, 0.366f, 0f), new Vector3(size.x, 0.052f, size.z), silver);
                    Block(art, "Recessed carry handle", new Vector3(0f, 0.398f, 0f), new Vector3(0.36f, 0.036f, 0.115f), frame);
                    foreach (float x in new[] { -0.31f, 0.31f })
                    {
                        RoundedBlock(art, "Cold case corner guard", new Vector3(x, 0f, -size.z * 0.49f),
                            new Vector3(0.072f, 0.53f, 0.035f), silver);
                    }
                }

                SetRenderers(grabbable, "highlightedRenderers", art.GetComponentsInChildren<Renderer>());
                return PrefabUtility.SaveAsPrefabAsset(cargo, Root + "/Prefabs/" + name + ".prefab");
            }
            finally
            {
                Object.DestroyImmediate(cargo);
            }
        }

        private static void Cylinder(Transform parent, string name, Vector3 position, float diameter, float depth,
            Material material, Quaternion rotation, bool solid = false)
        {
            GameObject disk = GameObject.CreatePrimitive(PrimitiveType.Cylinder);
            disk.name = name;
            disk.transform.SetParent(parent, false);
            disk.transform.SetLocalPositionAndRotation(position, rotation);
            disk.transform.localScale = new Vector3(diameter, depth / 2f, diameter);
            Object.DestroyImmediate(disk.GetComponent<Collider>());
            if (solid) disk.AddComponent<MeshCollider>().sharedMesh = disk.GetComponent<MeshFilter>().sharedMesh;
            disk.GetComponent<Renderer>().sharedMaterial = material;
        }
    }
}
