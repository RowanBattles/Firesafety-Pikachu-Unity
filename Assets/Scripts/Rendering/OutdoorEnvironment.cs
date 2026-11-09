using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireSafety.Rendering
{
    // Decorative scenery has no colliders and never changes the shared house/navigation layout.
    public sealed class OutdoorEnvironment : MonoBehaviour
    {
        private readonly List<Mesh> meshes = new List<Mesh>();
        private readonly List<Material> materials = new List<Material>();
        private readonly System.Random random = new System.Random(84199);
        private float Between(float min, float max) => min + (float)random.NextDouble() * (max - min);

        public void Initialize(Transform house)
        {
            var root = new GameObject("Garden and surrounding meadow").transform;
            root.SetParent(house, false);
            var ground = new Shape();
            ground.Quad(new Vector3(-256, -0.22f, -256), new Vector3(-256, -0.22f, 256),
                new Vector3(256, -0.22f, 256), new Vector3(256, -0.22f, -256));
            Draw(root, "Grass to the horizon", ground, new Color(0.25f, 0.42f, 0.19f), false);

            var patches = new Shape(); var grass = new Shape();
            for (int x = -12; x <= 12; x++)
                for (int z = -12; z <= 12; z++)
                {
                    Vector3 position = new Vector3(x * 6 + Between(-2, 2), -0.214f, z * 6 + Between(-2, 2));
                    if (NearHouseOrPath(position, 0.5f)) continue;
                    float radius = Between(1.3f, 3.1f);
                    for (int side = 0; side < 7; side++)
                    {
                        float a = side * Mathf.PI * 2 / 7, b = (side + 1) * Mathf.PI * 2 / 7;
                        patches.Triangle(position, position + new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * radius,
                            position + new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * radius);
                    }
                    for (int tuft = 0; tuft < 4; tuft++)
                    {
                        Vector3 p = position + new Vector3(Between(-2, 2), 0, Between(-2, 2));
                        if (NearHouseOrPath(p, 0.6f)) continue;
                        float height = Between(0.16f, 0.33f);
                        grass.Triangle(p + Vector3.left * 0.11f, p + Vector3.up * height + Vector3.forward * 0.14f, p + Vector3.right * 0.11f);
                        grass.Triangle(p + Vector3.back * 0.11f, p + Vector3.up * height + Vector3.left * 0.14f, p + Vector3.forward * 0.11f);
                    }
                }
            Draw(root, "Meadow patches", patches, new Color(0.29f, 0.46f, 0.21f), false);
            Draw(root, "Grass tufts", grass, new Color(0.18f, 0.34f, 0.14f), false);

            var trunks = new Shape(); var leaves = new Shape(); var highlights = new Shape();
            for (int x = -7; x <= 7; x++)
                for (int z = -7; z <= 7; z++)
                {
                    Vector3 p = new Vector3(x * 9 + Between(-2.2f, 2.2f), -0.21f, z * 9 + Between(-2.2f, 2.2f));
                    if (NearHouseOrPath(p, 4.5f)) continue;
                    float size = Between(0.8f, 1.3f);
                    for (int side = 0; side < 6; side++)
                    {
                        float a = side * Mathf.PI / 3, b = (side + 1) * Mathf.PI / 3;
                        Vector3 u = new Vector3(Mathf.Cos(a), 0, Mathf.Sin(a)) * 0.24f * size;
                        Vector3 v = new Vector3(Mathf.Cos(b), 0, Mathf.Sin(b)) * 0.24f * size;
                        trunks.Quad(p + v, p + u, p + u + Vector3.up * 2.8f * size, p + v + Vector3.up * 2.8f * size);
                    }
                    leaves.Crown(p + new Vector3(-0.55f, 2.35f, 0) * size, new Vector3(1.55f, 1.4f, 1.6f) * size);
                    leaves.Crown(p + new Vector3(0.6f, 2.55f, 0.35f) * size, new Vector3(1.45f, 1.5f, 1.5f) * size);
                    highlights.Crown(p + new Vector3(0, 3.25f, -0.15f) * size, Vector3.one * 1.4f * size);
                }
            Draw(root, "Tree trunks", trunks, new Color(0.37f, 0.25f, 0.16f), true);
            Draw(root, "Tree crowns", leaves, new Color(0.15f, 0.34f, 0.2f), true);
            Draw(root, "Sunlit leaves", highlights, new Color(0.28f, 0.46f, 0.23f), true);

            var stones = new Shape();
            for (float z = -13.1f; z > -70; z -= 1.35f)
            {
                float x = -5 + Between(-0.08f, 0.08f);
                stones.Quad(new Vector3(x - 0.9f, -0.205f, z - 0.45f), new Vector3(x - 0.9f, -0.205f, z + 0.45f),
                    new Vector3(x + 0.9f, -0.205f, z + 0.45f), new Vector3(x + 0.9f, -0.205f, z - 0.45f));
            }
            Draw(root, "Garden stepping stones", stones, new Color(0.7f, 0.69f, 0.57f), false);
        }

        private static bool NearHouseOrPath(Vector3 p, float margin)
            => Mathf.Abs(p.x) < 10 + margin && Mathf.Abs(p.z) < 8 + margin
                || p.z < -7 && p.x > -7.2f - margin && p.x < -2.8f + margin;

        private void Draw(Transform parent, string name, Shape shape, Color color, bool shadows)
        {
            var mesh = new Mesh { name = name };
            mesh.SetVertices(shape.Vertices); mesh.SetTriangles(shape.Triangles, 0);
            mesh.RecalculateNormals(); mesh.RecalculateBounds(); meshes.Add(mesh);
            var material = RuntimeMaterials.House(color); materials.Add(material);
            var item = new GameObject(name); item.transform.SetParent(parent, false);
            item.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = item.AddComponent<MeshRenderer>(); renderer.sharedMaterial = material;
            renderer.shadowCastingMode = shadows ? ShadowCastingMode.On : ShadowCastingMode.Off;
        }

        private sealed class Shape
        {
            public readonly List<Vector3> Vertices = new List<Vector3>();
            public readonly List<int> Triangles = new List<int>();
            public void Triangle(Vector3 a, Vector3 b, Vector3 c)
            {
                int first = Vertices.Count; Vertices.Add(a); Vertices.Add(b); Vertices.Add(c);
                Triangles.Add(first); Triangles.Add(first + 1); Triangles.Add(first + 2);
            }
            public void Quad(Vector3 a, Vector3 b, Vector3 c, Vector3 d) { Triangle(a, b, c); Triangle(a, c, d); }
            public void Crown(Vector3 centre, Vector3 scale)
            {
                const float t = 1.618034f;
                Vector3[] vertices = { new Vector3(-1,t,0),new Vector3(1,t,0),new Vector3(-1,-t,0),new Vector3(1,-t,0),
                    new Vector3(0,-1,t),new Vector3(0,1,t),new Vector3(0,-1,-t),new Vector3(0,1,-t),
                    new Vector3(t,0,-1),new Vector3(t,0,1),new Vector3(-t,0,-1),new Vector3(-t,0,1) };
                int[] faces = {0,11,5,0,5,1,0,1,7,0,7,10,0,10,11,1,5,9,5,11,4,11,10,2,10,7,6,7,1,8,
                    3,9,4,3,4,2,3,2,6,3,6,8,3,8,9,4,9,5,2,4,11,6,2,10,8,6,7,9,8,1};
                for (int i = 0; i < faces.Length; i += 3)
                    Triangle(centre + Vector3.Scale(vertices[faces[i]].normalized, scale),
                        centre + Vector3.Scale(vertices[faces[i + 1]].normalized, scale),
                        centre + Vector3.Scale(vertices[faces[i + 2]].normalized, scale));
            }
        }

        private void OnDestroy()
        {
            foreach (Mesh mesh in meshes) Destroy(mesh);
            foreach (Material material in materials) Destroy(material);
        }
    }
}
