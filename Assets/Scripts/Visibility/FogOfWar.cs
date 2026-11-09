using System.Collections.Generic;
using FireSafety.Movement;
using UnityEngine;

namespace FireSafety.Visibility
{
    public sealed class FogOfWar : MonoBehaviour
    {
        private const int Resolution = 256;
        private HousePrototype house;
        private Transform player;
        private Camera view;
        private VisibilityMap map;
        private readonly List<SightObstacle> obstacles = new List<SightObstacle>();
        private Texture2D texture;
        private Color32[] pixels;
        private Material material;
        private Mesh mesh;
        private int cachedVersion = -1, renderedVersion = -1;
        private Vector3 lastPosition;
        private float lastExtent;
        private float ViewExtent => VisibilityMap.ViewExtent(view.orthographicSize, view.aspect);

        public void Initialize(HousePrototype layout, Transform character, Camera camera)
        {
            house = layout; player = character; view = camera;
            map = new VisibilityMap(Resolution, Resolution, 0f, 0f, 1f);
            texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, true)
                { name = "Current line of sight", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            pixels = new Color32[Resolution * Resolution];
            material = new Material(Resources.Load<Shader>("Effects/VisibilityOverlay"));
            material.SetTexture("_VisibilityTex", texture);
            mesh = new Mesh { name = "Fog of war overlay" };
            mesh.vertices = new[] { new Vector3(-0.5f, 0f, -0.5f), new Vector3(0.5f, 0f, -0.5f),
                new Vector3(0.5f, 0f, 0.5f), new Vector3(-0.5f, 0f, 0.5f) };
            mesh.uv = new[] { Vector2.zero, Vector2.right, Vector2.one, Vector2.up };
            mesh.triangles = new[] { 0, 2, 1, 0, 3, 2 };
            mesh.RecalculateBounds();
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = material;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            camera.backgroundColor = new Color(30f / 255f, 32f / 255f, 36f / 255f);
            Refresh();
        }

        public bool CanSee(Vector3 point)
        {
            EnsureObstacles();
            return VisibilityMap.CanSee(player.position.x, player.position.z, point.x, point.z, obstacles);
        }

        private void EnsureObstacles()
        {
            if (cachedVersion == house.OcclusionVersion) return;
            house.CopySightObstacles(obstacles);
            cachedVersion = house.OcclusionVersion;
        }

        private void LateUpdate()
        {
            if (house == null) return;
            bool doorChanged = renderedVersion != house.OcclusionVersion;
            if (!doorChanged && Mathf.Abs(lastExtent - ViewExtent) < 0.0001f
                && (player.position - lastPosition).sqrMagnitude < 0.000001f) return;
            Refresh();
        }

        private void Refresh()
        {
            EnsureObstacles();
            lastPosition = player.position;
            lastExtent = ViewExtent;
            transform.position = new Vector3(lastPosition.x, 8f, lastPosition.z);
            transform.localScale = new Vector3(lastExtent, 1f, lastExtent);
            map.Calculate(lastPosition.x, lastPosition.z, obstacles,
                lastPosition.x - lastExtent * 0.5f, lastPosition.z - lastExtent * 0.5f, lastExtent / Resolution);
            for (int cell = 0; cell < pixels.Length; cell++)
            {
                float x = Origin + (cell % Resolution + 0.5f) * Extent / Resolution;
                float z = Origin + (cell / Resolution + 0.5f) * Extent / Resolution;
                // The garden is scenery. Interior visibility and click access still use wall/door sight lines.
                bool visible = !HouseExit.IsInsideHouse(x, z) || map.IsVisible(cell);
                pixels[cell] = new Color32(30, 32, 36, visible ? (byte)0 : (byte)255);
            }
            texture.SetPixels32(pixels); texture.Apply(false);
            renderedVersion = cachedVersion;
        }

        private void OnDestroy()
        {
            if (texture != null) Destroy(texture);
            if (material != null) Destroy(material);
            if (mesh != null) Destroy(mesh);
        }
    }
}
