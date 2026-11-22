using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static void BuildCoastalVista(Transform location, bool powerStation)
        {
            Transform vista = Child(location, "Coastal horizon");
            Material cliffs = Material("Shore Granite", "#536A72");
            Material woodland = Material("Coastal Woodland", "#364F50");
            Material farCliffs = Material("Outer Headland", "#758996");
            Material heather = Material("Heather Coast", "#526B64");
            CoastRidge(vista, "Outer peninsula", new Vector3(7f, -2.8f, 202f),
                new Vector3(226f, 46f, 51f), -13f, farCliffs, heather, false);
            CoastRidge(vista, "West headland", new Vector3(-95f, -2.5f, 101f),
                new Vector3(172f, 25f, 47f), -21f, cliffs, woodland, true);
            CoastRidge(vista, "East headland", new Vector3(101f, -2.6f, 132f),
                new Vector3(163f, 31f, 44f), 34f, cliffs, woodland, true);
            CoastRidge(vista, "Bay entrance", new Vector3(-88f, -2.3f, -28f),
                new Vector3(124f, 19f, 44f), -58f, cliffs, woodland, true);

            Material basalt = Material("Breakwater Granite", "#536468");
            Material concrete = QuaySurface("Wharf Works Concrete", "IndustrialFloor", "#A2A7A1", 3.2f);
            Material siding = QuaySurface("Harbor Siding", "SteelPanels", "#728780", 3.1f);
            Material redRoof = Material("Harbor Roof Oxide", "#70473B");
            Material structure = Material("Gantry Steel", "#405764");
            Material window = Material("Harbor Windows", "#FFD096", emissive: true);
            if (powerStation)
            {
                Transform dam = Child(vista, "Tidal barrage");
                dam.SetLocalPositionAndRotation(new Vector3(-39f, -0.4f, 50f), Quaternion.Euler(0f, -17f, 0f));
                for (int bay = 0; bay < 7; bay++)
                {
                    float z = -37f + bay * 8.2f;
                    Block(dam, "Lock pier", new Vector3(0f, 0.85f, z), new Vector3(6.2f, 5.8f, 1.33f), concrete);
                    Block(dam, "Sluice panel", new Vector3(-0.5f, -0.1f, z + 3.7f), new Vector3(1.05f, 3.8f, 7.5f), siding);
                    Block(dam, "Sluice actuator", new Vector3(0.62f, 4.2f, z), new Vector3(1.7f, 1.25f, 1.74f), redRoof);
                    Block(dam, "Causeway deck", new Vector3(0.25f, 3.68f, z + 3.85f), new Vector3(5.15f, 0.4f, 8.25f), concrete);
                }
                Block(dam, "Control tower", new Vector3(0.35f, 7.35f, -10.5f), new Vector3(4.8f, 7.10f, 5.5f), concrete);
                Block(dam, "Control tower beacon", new Vector3(0.75f, 10.5f, -10.5f), new Vector3(4.2f, 0.52f, 5.65f), window);
                CoastWarehouse(vista, new Vector3(37f, 0f, 103f), new Vector3(14f, 8f, 12f), siding, structure, window);
                return;
            }

            Block(vista, "Facing wharf", new Vector3(40f, -1.44f, 86f), new Vector3(118f, 3.2f, 17f), concrete);
            Block(vista, "East freight apron", new Vector3(58f, -0.86f, 45f), new Vector3(22f, 2.24f, 78f), basalt);
            for (int shed = 0; shed < 5; shed++)
            {
                float x = -12f + shed * 21.2f;
                CoastWarehouse(vista, new Vector3(x, 0.18f, 88.5f + shed % 2 * 4.8f),
                    new Vector3(15f + shed % 3 * 1.6f, 5.2f + shed % 3 * 1.65f, 10.2f),
                    shed % 2 == 0 ? siding : concrete, redRoof, window);
            }
            FreightGantry(vista, new Vector3(52f, 0.2f, 24f), 17f, structure, redRoof, window);
            FreightGantry(vista, new Vector3(56f, 0.2f, 70f), 19.5f, structure, redRoof, window);
            FreightGantry(vista, new Vector3(-32f, -0.2f, 94f), 16.5f, structure, redRoof, window);
            for (int pile = 0; pile < 7; pile++)
            {
                float x = 49f + pile % 2 * 5.1f;
                float z = 17f + pile * 8.8f;
                ImportedProp("Container", true, vista, new Vector3(2.7f, 2.6f, 5.2f), new Vector3(x, 1.5f, z));
                if (pile % 3 != 1)
                    ImportedProp("Container", true, vista, new Vector3(2.7f, 2.6f, 5.2f), new Vector3(x, 4f, z), 180f);
            }
            ImportedProp("FishingBoat", true, vista, new Vector3(5f, 4.5f, 10.6f), new Vector3(-42f, -0.20f, 37f), -37f);
        }

        private static void FreightGantry(Transform parent, Vector3 point, float height, Material steel, Material paint, Material lamp)
        {
            Transform gantry = Child(parent, "Freight gantry crane");
            gantry.localPosition = point;
            foreach (float x in new[] { -5f, 5f })
            foreach (float z in new[] { -3.25f, 3.25f })
            {
                Block(gantry, "Main upright", new Vector3(x, height * 0.5f, z), new Vector3(0.56f, height, 0.54f), steel);
                Block(gantry, "Crane footing", new Vector3(x, 0.30f, z), new Vector3(1.32f, 0.6f, 1.0f), paint);
            }
            foreach (float z in new[] { -3.25f, 3.25f })
            {
                Block(gantry, "Cantilever girder", new Vector3(-5.3f, height, z), new Vector3(23.25f, 0.69f, 0.59f), paint);
                DiagonalBrace(gantry, new Vector3(-5f, height * 0.69f, z), new Vector3(-13f, height, z), 0.32f, steel);
                DiagonalBrace(gantry, new Vector3(-5f, height * 0.47f, z), new Vector3(5f, height * 0.93f, z), 0.25f, steel);
            }
            Block(gantry, "Winch trolley", new Vector3(-11.5f, height - 0.21f, 0), new Vector3(1.65f, 1.05f, 7.4f), steel);
            Block(gantry, "Operator cab", new Vector3(4.5f, height - 1.58f, -3.78f), new Vector3(2.28f, 1.72f, 2.17f), paint);
            Block(gantry, "Cab windows", new Vector3(4.45f, height - 1.36f, -4.88f), new Vector3(1.86f, 0.61f, 0.05f), lamp);
            foreach (float z in new[] { -1.58f, 1.58f })
                Block(gantry, "Crane cable", new Vector3(-11.5f, height - 4.5f, z), new Vector3(0.064f, 8f, 0.064f), steel);
            Block(gantry, "Container spreader", new Vector3(-11.5f, height - 8.5f, 0), new Vector3(1.4f, 0.35f, 4.2f), paint);
        }

        private static void DiagonalBrace(Transform parent, Vector3 start, Vector3 end, float thickness, Material material)
        {
            Vector3 line = end - start;
            Renderer beam = Block(parent, "Diagonal brace", (start + end) * 0.5f,
                new Vector3(thickness, line.magnitude, thickness), material);
            beam.transform.localRotation = Quaternion.FromToRotation(Vector3.up, line.normalized);
        }

        private static void CoastWarehouse(Transform parent, Vector3 point, Vector3 size, Material wall, Material roof, Material glass)
        {
            Transform shed = Child(parent, "Waterfront warehouse");
            shed.localPosition = point;
            Block(shed, "Warehouse masonry", Vector3.up * size.y * 0.5f, size, wall);
            foreach (float side in new[] { -1f, 1f })
            {
                Renderer pitch = Block(shed, "Pitched warehouse roof",
                    new Vector3(0f, size.y + 0.82f, side * size.z * 0.245f),
                    new Vector3(size.x + 0.82f, 0.28f, size.z * 0.55f), roof);
                pitch.transform.localRotation = Quaternion.Euler(side * 17f, 0f, 0f);
            }
            for (float x = -size.x * 0.37f; x < size.x * 0.45f; x += 2.8f)
                Block(shed, "Lit warehouse glazing", new Vector3(x, size.y * 0.73f, -size.z * 0.5f - 0.025f),
                    new Vector3(1.47f, 0.52f, 0.07f), glass);
        }

        private static void CoastRidge(Transform parent, string name, Vector3 point, Vector3 size, float yaw,
            Material granite, Material trees, bool forest)
        {
            Transform hill = Child(parent, name);
            hill.SetLocalPositionAndRotation(point, Quaternion.Euler(0f, yaw, 0f));
            MeshFilter filter = hill.gameObject.AddComponent<MeshFilter>();
            filter.sharedMesh = AuthoredMesh("RockyCoastalRidge", BuildCoastalRidgeMesh);
            var renderer = hill.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterials = new[] { granite, trees };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            hill.localScale = size;
            if (!forest) return;

            Transform forestRoot = Child(parent, name + " treeline");
            forestRoot.SetLocalPositionAndRotation(point, Quaternion.Euler(0f, yaw, 0f));
            var scatter = new System.Random(name.Length * 247 + Mathf.RoundToInt(size.y));
            for (int index = 0; index < 39; index++)
            {
                float x = (float)scatter.NextDouble() * 0.82f - 0.41f;
                float z = (float)scatter.NextDouble() * 0.68f - 0.35f;
                float elevation = RidgeHeight(x, z) * size.y;
                if (elevation < 4.2f) continue;
                float height = 2.3f + (float)scatter.NextDouble() * 3.8f;
                var tree = LathedProp(forestRoot, "Coastal spruce", new Vector3(x * size.x, elevation - 1.1f, z * size.z),
                    trees, "CoastalSpruce", new[]
                    {
                        new Vector2(0.09f, 0f), new Vector2(0.09f, 0.15f), new Vector2(0.36f, 0.19f),
                        new Vector2(0.22f, 0.51f), new Vector2(0.28f, 0.44f), new Vector2(0.14f, 0.78f),
                        new Vector2(0.18f, 0.69f), new Vector2(0.01f, 1f),
                    });
                tree.transform.localScale = new Vector3(height * 0.78f, height, height * 0.78f);
                tree.shadowCastingMode = ShadowCastingMode.Off;
            }
        }

        private static float RidgeHeight(float x, float z)
        {
            float outline = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(x * Mathf.PI)), 0.74f);
            float crossSection = Mathf.Pow(Mathf.Max(0f, Mathf.Cos(z * Mathf.PI)), 1.48f);
            float rock = Mathf.PerlinNoise(x * 4.4f + 4.1f, z * 3.7f + 9.3f);
            float crags = Mathf.PerlinNoise(x * 14f + 18.6f, z * 17f + 3.8f);
            return outline * crossSection * (0.30f + rock * 0.70f + crags * 0.13f);
        }

        private static Mesh BuildCoastalRidgeMesh()
        {
            const int columns = 35;
            const int rows = 13;
            var vertices = new List<Vector3>(columns * rows);
            var stone = new List<int>();
            var cover = new List<int>();
            for (int row = 0; row < rows; row++)
            for (int column = 0; column < columns; column++)
            {
                float x = column / (columns - 1f) - 0.5f;
                float z = row / (rows - 1f) - 0.5f;
                vertices.Add(new Vector3(x, RidgeHeight(x, z), z));
                if (row == 0 || column == 0) continue;
                int top = row * columns + column;
                float band = Mathf.PerlinNoise(x * 9f + 8f, z * 12f + 3f);
                List<int> triangles = Mathf.Abs(z) < 0.33f && band > 0.32f ? cover : stone;
                triangles.Add(top - columns - 1);
                triangles.Add(top - 1);
                triangles.Add(top);
                triangles.Add(top - columns - 1);
                triangles.Add(top);
                triangles.Add(top - columns);
            }
            var mesh = new Mesh { subMeshCount = 2 };
            mesh.SetVertices(vertices);
            mesh.SetTriangles(stone, 0);
            mesh.SetTriangles(cover, 1);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
