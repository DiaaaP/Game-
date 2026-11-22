using DeliveryImpossible.Orders;
using UnityEngine;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static void BuildPortOffice(Transform pier, Material teal, Material brass, Material wood, Material trim)
        {
            Material shell = Material("Lighthouse Plaster", "#E5D8B3");
            Material signal = Material("Lighthouse Copper", "#9E4737");
            Material lamp = Material("Beacon Light", "#FFB568", emissive: true);
            Vector3 tower = new Vector3(-6.0f, 0f, 39.2f);
            Cylinder(pier, "Lighthouse stone plinth", tower + Vector3.up * 0.35f, 3.35f, 0.70f, trim, Quaternion.identity);
            Cylinder(pier, "Lighthouse tower", tower + Vector3.up * 3.54f, 2.48f, 6.7f, shell, Quaternion.identity);
            foreach (float y in new[] { 2.1f, 4.6f, 6.90f })
                Cylinder(pier, "Lighthouse band", tower + Vector3.up * y, 2.57f, 0.65f, signal, Quaternion.identity);
            Cylinder(pier, "Lantern balcony", tower + Vector3.up * 7.08f, 3.58f, 0.23f, brass, Quaternion.identity);
            Cylinder(pier, "Lantern glass", tower + Vector3.up * 7.69f, 1.78f, 1.14f, lamp, Quaternion.identity);
            Cylinder(pier, "Copper roof", tower + Vector3.up * 8.36f, 2.40f, 0.29f, signal, Quaternion.identity);
            Blob(pier, "Copper cap", tower + Vector3.up * 8.65f, new Vector3(1.62f, 0.65f, 1.62f), signal);
            Lamp(pier, tower + Vector3.up * 7.7f, new Color(1f, 0.72f, 0.34f), 18f, 4.8f);

            Vector3 office = new Vector3(4.95f, 0f, 42.55f);
            RoundedBlock(pier, "Dispatch office siding", office + Vector3.up * 1.39f, new Vector3(7.6f, 2.78f, 1.7f), teal);
            Block(pier, "Dispatch office barrier", office + Vector3.up * 1.38f, new Vector3(7.5f, 2.76f, 1.68f), teal, true).enabled = false;
            RoundedBlock(pier, "Dispatch office roof", office + new Vector3(0f, 2.88f, -0.07f),
                new Vector3(7.92f, 0.36f, 2.1f), signal);
            CourseLabel(pier, "МАЯК · ПОЧТОВАЯ ДИСПЕТЧЕРСКАЯ", office + new Vector3(0f, 2.20f, -0.88f), 6.82f, brass, trim);
            foreach (float x in new[] { -2.25f, 2.25f })
                RoundedBlock(pier, "Glazed dispatch window", office + new Vector3(x, 1.35f, -0.86f),
                    new Vector3(1.35f, 0.78f, 0.10f), lamp);
            CoursePost(pier, "ПРИЧАЛ · ПУНКТ ПРОВЕРКИ", new Vector3(6.38f, 0f, 28f), brass, trim);

            foreach (float z in new[] { 24.7f, 28.2f, 32.9f })
                CargoStack(pier, new Vector3(-1.35f, 0f, z), wood, trim);
            Transform launch = Child(pier, "Harbor pilot boat");
            launch.localPosition = new Vector3(-11.6f, -0.72f, 20.4f);
            launch.localRotation = Quaternion.Euler(0f, 18f, 0f);
            RoundedBlock(launch, "Pilot hull", Vector3.zero, new Vector3(2.85f, 0.93f, 7.8f), signal);
            RoundedBlock(launch, "Pilot deck", new Vector3(0f, 0.44f, 0f), new Vector3(2.65f, 0.16f, 7.27f), wood);
            RoundedBlock(launch, "Wheelhouse", new Vector3(0f, 1.03f, 0.76f), new Vector3(1.88f, 1.30f, 2.2f), shell);
            Block(launch, "Windshield", new Vector3(0f, 1.21f, -0.35f), new Vector3(1.55f, 0.48f, 0.045f), teal);
            Cylinder(launch, "Antenna", new Vector3(0f, 2.08f, 0.84f), 0.07f, 0.83f, brass, Quaternion.identity);
        }

        private static void CargoStack(Transform parent, Vector3 point, Material casePaint, Material band)
        {
            Block(parent, "Pallet collision", point + Vector3.up * 0.46f, new Vector3(2.35f, 0.92f, 1.62f), band, true).enabled = false;
            foreach (float x in new[] { -0.76f, 0f, 0.76f })
            {
                RoundedBlock(parent, "Pallet runner", point + new Vector3(x, 0.085f, 0f),
                    new Vector3(0.21f, 0.17f, 1.66f), band);
            }

            foreach (float x in new[] { -0.54f, 0.54f })
            {
                RoundedBlock(parent, "Pallet crate", point + new Vector3(x, 0.59f, 0f),
                    new Vector3(1.0f, 0.82f, 1.40f), casePaint);
                Block(parent, "Secure pallet band", point + new Vector3(x, 1.0f, 0f), new Vector3(0.14f, 0.027f, 1.38f), band);
            }
        }

        private static void GalleryExhibit(Transform hall, Vector3 point, Material wood, Material stone, Material metal)
        {
            RoundedBlock(hall, "Exhibit base", point + Vector3.up * 0.36f, new Vector3(1.46f, 0.72f, 1.42f), wood);
            Cylinder(hall, "Ceramic exhibit plinth", point + Vector3.up * 0.73f, 0.83f, 0.11f, metal, Quaternion.identity);
            LathedProp(hall, "Exhibition urn", point + Vector3.up * 1.19f, stone, "GalleryUrn", new[]
            {
                new Vector2(0.25f,-0.40f), new Vector2(0.28f,-0.34f), new Vector2(0.40f,-0.21f),
                new Vector2(0.43f,0f), new Vector2(0.36f,0.18f), new Vector2(0.23f,0.28f),
                new Vector2(0.22f,0.40f), new Vector2(0.28f,0.46f), new Vector2(0.18f,0.46f)
            });
            Cylinder(hall, "Urn rim", point + Vector3.up * 1.60f, 0.52f, 0.05f, metal, Quaternion.identity);
        }

        private static void CoursePost(Transform parent, string label, Vector3 point, Material text, Material frame)
        {
            Block(parent, "Direction sign post", point + Vector3.up * 0.82f,
                new Vector3(0.12f, 1.66f, 0.12f), frame);
            CourseLabel(parent, label, point + new Vector3(0f, 1.70f, -0.078f), 2.6f, text, frame);
        }

        private static void RouteStuds(Transform parent, Material material, params Vector2[] path) =>
            RouteStuds(parent, material, 0f, path);

        private static void RouteStuds(Transform parent, Material material, float height, params Vector2[] path)
        {
            for (int index = 1; index < path.Length; index++)
            {
                Vector2 start = path[index - 1];
                Vector2 delta = path[index] - start;
                for (float distance = 0.4f; distance < delta.magnitude; distance += 0.88f)
                {
                    Vector2 point = start + delta.normalized * distance;
                    Block(parent, "Route trail", new Vector3(point.x, height + 0.019f, point.y),
                        new Vector3(0.13f, 0.015f, 0.22f), material);
                }
            }
        }
    }
}
