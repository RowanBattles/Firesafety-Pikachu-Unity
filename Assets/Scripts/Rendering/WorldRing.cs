using UnityEngine;
using UnityEngine.Rendering;

namespace FireSafety.Rendering
{
    // A collider-free marker: highlighting an object never changes its approach route.
    public sealed class WorldRing : MonoBehaviour
    {
        private Mesh mesh;
        private Material material;

        public static Transform Create(Transform parent, string name, Color color)
        {
            var root = new GameObject(name);
            root.transform.SetParent(parent, false);
            var ring = root.AddComponent<WorldRing>();
            ring.material = RuntimeMaterials.Flame(color);
            ring.mesh = CreateMesh();
            root.AddComponent<MeshFilter>().sharedMesh = ring.mesh;
            var renderer = root.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = ring.material;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            root.SetActive(false);
            return root.transform;
        }

        private static Mesh CreateMesh()
        {
            const int segments = 48;
            var vertices = new Vector3[segments * 2];
            var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0, Mathf.Sin(angle));
                vertices[i * 2] = direction * 0.34f;
                vertices[i * 2 + 1] = direction * 0.49f;
                int a = i * 2, b = a + 1, c = ((i + 1) % segments) * 2, d = c + 1, t = i * 6;
                triangles[t] = a; triangles[t + 1] = c; triangles[t + 2] = b;
                triangles[t + 3] = b; triangles[t + 4] = c; triangles[t + 5] = d;
            }
            var mesh = new Mesh { name = "Selection ring" };
            mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (material != null) Destroy(material);
        }
    }
}
