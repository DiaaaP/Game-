using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

namespace DeliveryImpossible.Editor
{
    public static partial class WarehouseSetup
    {
        private static Renderer RoundedBlock(Transform parent, string label, Vector3 position, Vector3 size, Material material)
        {
            Transform form = Child(parent, label);
            form.localPosition = position;
            form.localScale = size;
            form.gameObject.AddComponent<MeshFilter>().sharedMesh = AuthoredMesh("RoundedHousing", BuildRoundedHousing);
            var surface = form.gameObject.AddComponent<MeshRenderer>();
            surface.sharedMaterial = material;
            return surface;
        }

        private static Renderer LathedProp(Transform parent, string label, Vector3 position, Material material,
            string meshName, Vector2[] profile)
        {
            Transform form = Child(parent, label);
            form.localPosition = position;
            form.gameObject.AddComponent<MeshFilter>().sharedMesh = AuthoredMesh(meshName, () => BuildLathe(profile));
            var surface = form.gameObject.AddComponent<MeshRenderer>();
            surface.sharedMaterial = material;
            return surface;
        }

        private static Mesh AuthoredMesh(string id, System.Func<Mesh> create)
        {
            const string folder = Root + "/Models/Generated";
            string path = folder + "/" + id + ".asset";
            Mesh previous = AssetDatabase.LoadAssetAtPath<Mesh>(path);
            if (previous != null) return previous;
            if (!AssetDatabase.IsValidFolder(folder)) AssetDatabase.CreateFolder(Root + "/Models", "Generated");
            Mesh mesh = create();
            mesh.name = id;
            AssetDatabase.CreateAsset(mesh, path);
            return mesh;
        }

        private static Mesh BuildRoundedHousing()
        {
            float[] axis = { -0.5f, -0.43f, -0.36f, 0.36f, 0.43f, 0.5f };
            var vertices = new List<Vector3>();
            var normals = new List<Vector3>();
            var indices = new List<int>();
            foreach (Vector3 face in new[] { Vector3.left, Vector3.right, Vector3.up, Vector3.down, Vector3.forward, Vector3.back })
            {
                Vector3 right = Vector3.Cross(Mathf.Abs(face.y) > 0.9f ? Vector3.forward : Vector3.up, face);
                Vector3 up = Vector3.Cross(face, right);
                int start = vertices.Count;
                foreach (float v in axis)
                foreach (float u in axis)
                {
                    Vector3 cubePoint = face * 0.5f + right * u + up * v;
                    Vector3 core = new Vector3(Mathf.Clamp(cubePoint.x, -0.36f, 0.36f),
                        Mathf.Clamp(cubePoint.y, -0.36f, 0.36f), Mathf.Clamp(cubePoint.z, -0.36f, 0.36f));
                    Vector3 normal = (cubePoint - core).normalized;
                    vertices.Add(core + normal * 0.14f);
                    normals.Add(normal);
                }

                for (int y = 0; y < axis.Length - 1; y++)
                for (int x = 0; x < axis.Length - 1; x++)
                {
                    int cell = start + y * axis.Length + x;
                    indices.Add(cell);
                    indices.Add(cell + 1);
                    indices.Add(cell + axis.Length + 1);
                    indices.Add(cell);
                    indices.Add(cell + axis.Length + 1);
                    indices.Add(cell + axis.Length);
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetNormals(normals);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateBounds();
            return mesh;
        }

        private static Mesh BuildLathe(Vector2[] profile)
        {
            const int segments = 40;
            var vertices = new List<Vector3>(segments * profile.Length);
            var indices = new List<int>((profile.Length - 1) * segments * 6);
            for (int ring = 0; ring < profile.Length; ring++)
            {
                for (int segment = 0; segment < segments; segment++)
                {
                    float angle = segment * (Mathf.PI * 2f / segments);
                    vertices.Add(new Vector3(profile[ring].x * Mathf.Cos(angle), profile[ring].y,
                        profile[ring].x * Mathf.Sin(angle)));
                    if (ring >= profile.Length - 1) continue;
                    int first = ring * segments + segment;
                    int next = ring * segments + (segment + 1) % segments;
                    indices.Add(first);
                    indices.Add(first + segments);
                    indices.Add(next + segments);
                    indices.Add(first);
                    indices.Add(next + segments);
                    indices.Add(next);
                }
            }

            var mesh = new Mesh();
            mesh.SetVertices(vertices);
            mesh.SetTriangles(indices, 0);
            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }
    }
}
