using UnityEngine;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static void DetailSortingHall(Transform hall, Material frame, Material equipment, Material safety)
        {
            Material hardware = QuaySurface("Yard Equipment Enamel", "SteelPanels", "#85939A", 1.85f);
            Material concrete = QuaySurface("Loading Yard Concrete", "IndustrialFloor", "#9FA29B", 2.6f);
            Material officeWindow = Material("Yard Office Window", "#F4C780", emissive: true);
            Block(hall, "Outside loading yard", new Vector3(-24f, -0.32f, 33f),
                new Vector3(28.7f, 0.44f, 69f), concrete);
            CoastWarehouse(hall, new Vector3(-31f, -0.1f, 22f), new Vector3(11f, 7.4f, 25f),
                hardware, frame, officeWindow);
            for (int span = 0; span < 3; span++)
            {
                float z = 14f + span * 21.5f;
                Block(hall, "Overhead gantry rail", new Vector3(0f, 5.75f, z), new Vector3(16.8f, 0.37f, 0.65f), safety);
                Block(hall, "Gantry suspended carriage", new Vector3(-4.55f + span * 3.7f, 5.19f, z),
                    new Vector3(1.64f, 0.73f, 0.89f), equipment);
                DiagonalBrace(hall, new Vector3(-9.3f, 4.15f, z), new Vector3(-6.6f, 5.75f, z), 0.20f, frame);
                DiagonalBrace(hall, new Vector3(9.3f, 4.15f, z), new Vector3(6.6f, 5.75f, z), 0.20f, frame);
            }
            for (int rack = 0; rack < 6; rack++)
            {
                float z = 4.3f + rack * 11.7f;
                float x = rack % 2 == 0 ? -9.20f : 9.20f;
                var position = new Vector3(x, 2.82f, z);
                Block(hall, "Wall mounted tool cabinet", position, new Vector3(0.58f, 1.46f, 2.42f), hardware, true);
                for (int row = 0; row < 5; row++)
                    Block(hall, "Cabinet drawer pull", position + new Vector3(-Mathf.Sign(x) * 0.315f, row * 0.22f - 0.42f, 0),
                        new Vector3(0.07f, 0.046f, 1.1f), frame);
                ImportedProp("Container", true, hall, new Vector3(2.7f, 2.5f, 5.1f), new Vector3(-18.25f, 1.18f, z), 90f);
            }
        }

        private static void DetailPortArchive(Transform hall, Material timber, Material frame, Material brass)
        {
            Material glass = Material("Archive Display Glass", "#6B9490");
            Material parchment = Material("Archive Parchment", "#CDAD82");
            Material spineBlue = Material("Archive Blue Spine", "#405B63");
            Material spineRed = Material("Archive Red Spine", "#744339");
            Material[] volumes = { parchment, spineBlue, timber, spineRed };
            foreach (float sign in new[] { -1f, 1f })
            {
                for (int bay = 0; bay < 8; bay++)
                {
                    float z = 3.15f + bay * 9.95f;
                    float x = sign * 9.53f;
                    Block(hall, "Timber archive panelling", new Vector3(sign * 9.77f, 0.78f, z),
                        new Vector3(0.18f, 1.53f, 9.30f), timber, true);
                    Block(hall, "Timber dado trim", new Vector3(sign * 9.66f, 1.57f, z),
                        new Vector3(0.16f, 0.14f, 9.32f), frame);
                    if ((bay + (sign > 0f ? 1 : 0)) % 2 == 0)
                    {
                        Block(hall, "Document card alcove", new Vector3(x, 2.60f, z),
                            new Vector3(0.24f, 1.58f, 3.65f), frame, true);
                        Block(hall, "Framed navigation chart", new Vector3(x - sign * 0.135f, 2.60f, z),
                            new Vector3(0.07f, 1.22f, 3.28f), parchment);
                        for (int line = 0; line < 6; line++)
                        {
                            var start = new Vector3(x - sign * 0.18f, 2.18f + line * 0.144f, z - 1.38f);
                            var end = start + new Vector3(0f, 0.022f * (line % 2), 1.17f + 0.28f * (line % 3));
                            DiagonalBrace(hall, start, end, 0.024f, spineBlue);
                        }
                        continue;
                    }
                    Block(hall, "Archive shelving case", new Vector3(x, 2.3f, z), new Vector3(0.48f, 2.15f, 3.7f), timber, true);
                    for (int shelf = 0; shelf < 3; shelf++)
                    {
                        float y = 1.48f + shelf * 0.61f;
                        Block(hall, "Shelf edge", new Vector3(x - sign * 0.27f, y, z), new Vector3(0.25f, 0.085f, 3.66f), frame, true);
                        for (int book = 0; book < 11; book++)
                        {
                            float height = 0.27f + 0.055f * ((book * 3 + shelf + bay) % 4);
                            Block(hall, "Catalogued folio", new Vector3(x - sign * 0.255f, y + 0.046f + height * 0.5f, z - 1.53f + book * 0.30f),
                                new Vector3(0.16f, height, 0.22f + 0.027f * (book % 3)), volumes[(book + shelf) % volumes.Length]);
                        }
                    }
                }
            }

            foreach (Vector3 door in new[] { new Vector3(-2.95f, 0f, 17.9f), new Vector3(3.25f, 0f, 38.8f), new Vector3(-2.1f, 0f, 59.35f) })
            {
                for (int block = 0; block <= 18; block++)
                {
                    float angle = block * Mathf.PI / 18f;
                    var center = door + new Vector3(Mathf.Cos(angle) * 2.18f, 2.04f + Mathf.Sin(angle) * 1.23f, -0.242f);
                    Renderer stone = Block(hall, "Archive portal masonry", center,
                        new Vector3(0.30f, 0.29f, 0.13f), block == 9 ? brass : parchment);
                    stone.transform.localRotation = Quaternion.Euler(0f, 0f, angle * Mathf.Rad2Deg - 90f);
                }
                foreach (float side in new[] { -1f, 1f })
                {
                    Block(hall, "Archive portal jamb", door + new Vector3(side * 2.20f, 1.01f, -0.252f),
                        new Vector3(0.24f, 2.02f, 0.125f), parchment, true);
                    Vector3 lantern = door + new Vector3(side * 3.0f, 2.63f, -0.47f);
                    Material mantle = Material("Archive Amber Sconce", "#FFC982", emissive: true);
                    RoundedBlock(hall, "Wall sconce housing", lantern, new Vector3(0.31f, 0.43f, 0.26f), brass, solid: true);
                    Block(hall, "Wall sconce window", lantern + Vector3.back * 0.14f, new Vector3(0.17f, 0.24f, 0.04f), mantle);
                    Lamp(hall, lantern + Vector3.back * 0.19f, new Color(1f, 0.66f, 0.39f), 5.6f, 2.25f);
                }
            }
            foreach (float x in new[] { -6.0f, 6.0f })
            {
                Vector3 plinth = new Vector3(x, 0.52f, 11.3f);
                RoundedBlock(hall, "Chart exhibit pedestal", plinth, new Vector3(1.65f, 1.04f, 1.22f), timber, solid: true);
                Block(hall, "Chart display frame", plinth + Vector3.up * 0.54f, new Vector3(1.53f, 0.12f, 1.1f), brass, true);
                Block(hall, "Chart display insert", plinth + Vector3.up * 0.612f, new Vector3(1.31f, 0.027f, 0.9f), glass);
            }
        }

        private static void DetailRefrigerationHall(Transform hall, Material steel, Material fixtures)
        {
            Material duct = Material("Refrigeration Duct", "#6F949F");
            duct.SetFloat("_Smoothness", 0.46f);
            Material clip = Material("Refrigeration Collar", "#324B58");
            Material ice = Material("Freezer Panel Light", "#AFE6EF", emissive: true);
            foreach (float side in new[] { -1f, 1f })
            {
                float x = side * 6.25f;
                Cylinder(hall, "Main refrigeration duct", new Vector3(x, 4.9f, 44f), 1.26f, 86.7f, duct,
                    Quaternion.Euler(90f, 0f, 0f));
                for (int station = 0; station < 7; station++)
                {
                    float z = 4.32f + station * 13f;
                    Cylinder(hall, "Insulated pipe collar", new Vector3(x, 4.9f, z), 1.40f, 0.18f, clip,
                        Quaternion.Euler(90f, 0f, 0f));
                    Block(hall, "Pipe support bracket", new Vector3(x, 5.79f, z), new Vector3(0.18f, 0.68f, 0.17f), clip);
                    Block(hall, "Wall refrigeration panel", new Vector3(side * 10.77f, 2.45f, z),
                        new Vector3(0.23f, 3.56f, 3.15f), steel, true);
                    Block(hall, "Refrigeration panel status", new Vector3(side * 10.63f, 3.8f, z),
                        new Vector3(0.08f, 0.08f, 2.47f), ice);
                    for (int vane = 0; vane < 7; vane++)
                        Block(hall, "Intake vent louvre", new Vector3(side * 10.63f, 2.13f + 0.18f * vane, z),
                            new Vector3(0.18f, 0.075f, 2.12f), fixtures);
                }
            }
        }
    }
}
