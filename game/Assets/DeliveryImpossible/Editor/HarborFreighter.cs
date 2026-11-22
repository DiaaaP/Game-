using System.Collections.Generic;
using UnityEngine;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static void BuildFreighter(Transform harbor, Vector3 berth)
        {
            Transform ship = Child(harbor, "Coastal cargo freighter");
            ship.localPosition = berth;
            Material hull = Material("Freighter Hull", "#284A59");
            Material oxide = Material("Freighter Waterline", "#7D463B");
            Material stripe = Material("Freighter Draft Stripe", "#D2C5A8");
            Material deck = Material("Freighter Deck", "#526369");
            Material white = Material("Freighter Wheelhouse", "#C3C1AD");
            Material glass = Material("Freighter Windows", "#3E6068");
            Material lamp = Material("Freighter Bridge Light", "#FFD5A1", emissive: true);
            Material mast = Material("Freighter Hardware", "#38494F");
            Transform hullForm = Child(ship, "Tapered steel hull");
            hullForm.gameObject.AddComponent<MeshFilter>().sharedMesh = AuthoredMesh("FreighterHull", BuildFreighterHullMesh);
            hullForm.gameObject.AddComponent<MeshRenderer>().sharedMaterials = new[] { oxide, stripe, hull, deck };

            foreach (float side in new[] { -1f, 1f })
            {
                for (float z = -10.5f; z <= 14f; z += 2.45f)
                {
                    float x = side * (z >= 11 ? 5.23f : 5.71f);
                    Block(ship, "Deck railing stanchion", new Vector3(x, 2.84f, z), new Vector3(0.055f, 0.98f, 0.074f), white);
                }
                foreach (float y in new[] { 2.83f, 3.28f })
                {
                    Block(ship, "Deck guard cable", new Vector3(side * 5.71f, y, -0.55f), new Vector3(0.069f, 0.058f, 20.2f), white);
                    DiagonalBrace(ship, new Vector3(side * 5.71f, y, 9.55f), new Vector3(side * 5.23f, y, 14f), 0.056f, white);
                }
                for (int porthole = 0; porthole < 5; porthole++)
                    Cylinder(ship, "Wheelhouse porthole", new Vector3(side * 3.35f, 4.24f, 5.4f + porthole * 1.59f),
                        0.44f, 0.065f, porthole % 3 == 0 ? lamp : glass, Quaternion.Euler(0f, 0f, 90f));
                Block(ship, "Navigation light", new Vector3(4.48f * side, 5.67f, 5.6f), new Vector3(0.19f, 0.22f, 0.22f),
                    side < 0f ? Material("Freighter Port Signal", "#E27465", emissive: true) : Material("Freighter Starboard Signal", "#6AEAA8", emissive: true));
            }

            RoundedBlock(ship, "Aft quarter deck", new Vector3(0f, 3.48f, 8.7f), new Vector3(6.7f, 2.46f, 9.0f), white);
            RoundedBlock(ship, "Raised bridge", new Vector3(0f, 5.48f, 7.51f), new Vector3(8.9f, 1.62f, 3.44f), white);
            RoundedBlock(ship, "Bridge visor", new Vector3(0f, 6.31f, 7.30f), new Vector3(9.07f, 0.19f, 3.62f), mast);
            for (int pane = 0; pane < 7; pane++)
                Block(ship, "Bridge windshield panes", new Vector3(-3.66f + pane * 1.22f, 5.69f, 5.79f),
                    new Vector3(1.08f, 0.54f, 0.10f), pane == 2 || pane == 3 ? lamp : glass);
            foreach (float side in new[] { -1f, 1f })
                Block(ship, "Bridge side glazing", new Vector3(side * 4.43f, 5.69f, 7.47f),
                    new Vector3(0.09f, 0.54f, 2.08f), glass);

            Transform funnel = Child(ship, "Raked exhaust stack");
            funnel.SetLocalPositionAndRotation(new Vector3(0.65f, 5.91f, 11.08f), Quaternion.Euler(8f, 0f, 0f));
            RoundedBlock(funnel, "Stack casing", new Vector3(0f, 1.17f, 0), new Vector3(1.74f, 2.46f, 1.9f), oxide);
            RoundedBlock(funnel, "Stack top", new Vector3(0f, 2.33f, 0), new Vector3(1.85f, 0.43f, 1.98f), mast);
            Cylinder(ship, "Radio mast", new Vector3(0f, 7.52f, 6.97f), 0.12f, 2.43f, mast, Quaternion.identity);
            Block(ship, "Radar spreader", new Vector3(0f, 8.45f, 6.97f), new Vector3(1.40f, 0.12f, 0.25f), white);
            RoundedBlock(ship, "Launch lifeboat", new Vector3(-4.47f, 3.5f, 9.72f), new Vector3(1.11f, 0.57f, 3.1f), oxide);
            RoundedBlock(ship, "Lifeboat canopy", new Vector3(-4.47f, 3.79f, 9.72f), new Vector3(0.91f, 0.21f, 2.87f), stripe);

            Material redBox = QuaySurface("Freighter Oxide Container", "SteelPanels", "#985547", 3.4f);
            Material blueBox = QuaySurface("Freighter Blue Container", "SteelPanels", "#507987", 3.4f);
            Material sandBox = QuaySurface("Freighter Sand Container", "SteelPanels", "#9B8869", 3.4f);
            Material[] loads = { redBox, blueBox, sandBox };
            for (int bay = 0; bay < 3; bay++)
            for (int column = 0; column < 3; column++)
            {
                float x = -2.83f + column * 2.83f;
                float z = -9.0f + bay * 4.80f;
                Block(ship, "Deck freight container", new Vector3(x, 3.33f, z),
                    new Vector3(2.60f, 1.92f, 4.50f), loads[(bay + column) % loads.Length]);
                if (column == 1 && bay != 2)
                    Block(ship, "Stacked deck freight", new Vector3(x, 5.25f, z),
                        new Vector3(2.60f, 1.91f, 4.50f), loads[(bay + 2) % loads.Length]);
            }
            Cylinder(ship, "Forward windlass", new Vector3(0f, 3.01f, -13.9f), 0.88f, 1.96f, mast, Quaternion.Euler(0f, 0f, 90f));
            Cylinder(ship, "Foremast", new Vector3(0f, 4.01f, -15.3f), 0.11f, 2.9f, white, Quaternion.identity);
        }

        private static Mesh BuildFreighterHullMesh()
        {
            float[] length = { -18.8f, -16.4f, -12.3f, -7.2f, 0f, 8.7f, 13.9f, 17.1f };
            float[] width = { 0.13f, 2.65f, 5.4f, 6.15f, 6.33f, 6.35f, 5.72f, 3.94f };
            float[] height = { -1.66f, 0.24f, 0.60f, 2.37f };
            float[] flare = { 0.52f, 0.88f, 0.93f, 1f };
            var vertices = new List<Vector3>();
            var layers = new[] { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
            void Quad(List<int> triangles, Vector3 a, Vector3 b, Vector3 c, Vector3 d)
            {
                int first = vertices.Count;
                vertices.Add(a);
                vertices.Add(b);
                vertices.Add(c);
                vertices.Add(d);
                triangles.AddRange(new[] { first, first + 1, first + 2, first, first + 2, first + 3 });
            }
            Vector3 Point(int row, int tier, float side) => new Vector3(width[row] * flare[tier] * side,
                height[tier] + (row == 0 ? 0.38f : 0f), length[row]);
            for (int row = 0; row < length.Length - 1; row++)
            {
                for (int tier = 0; tier < height.Length - 1; tier++)
                {
                    Quad(layers[tier], Point(row, tier, -1f), Point(row + 1, tier, -1f), Point(row + 1, tier + 1, -1f), Point(row, tier + 1, -1f));
                    Quad(layers[tier], Point(row + 1, tier, 1f), Point(row, tier, 1f), Point(row, tier + 1, 1f), Point(row + 1, tier + 1, 1f));
                }
                Quad(layers[3], Point(row, 3, -1f), Point(row + 1, 3, -1f), Point(row + 1, 3, 1f), Point(row, 3, 1f));
            }
            int stern = length.Length - 1;
            for (int tier = 0; tier < height.Length - 1; tier++)
            {
                Quad(layers[tier], Point(0, tier, -1f), Point(0, tier + 1, -1f), Point(0, tier + 1, 1f), Point(0, tier, 1f));
                Quad(layers[tier], Point(stern, tier, 1f), Point(stern, tier + 1, 1f), Point(stern, tier + 1, -1f), Point(stern, tier, -1f));
            }
            var mesh = new Mesh { subMeshCount = layers.Length };
            mesh.SetVertices(vertices);
            for (int layer = 0; layer < layers.Length; layer++) mesh.SetTriangles(layers[layer], layer);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
