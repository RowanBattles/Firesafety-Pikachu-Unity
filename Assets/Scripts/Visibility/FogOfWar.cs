using System.Collections.Generic;
using FireSafety.Movement;
using UnityEngine;

namespace FireSafety.Visibility
{
    public sealed class FogOfWar : MonoBehaviour
    {
        private const int Resolution = 256;
        private const float Origin = -16f, Extent = 32f;
        private HousePrototype house;
        private Transform player;
        private VisibilityMap map;
        private readonly List<SightObstacle> obstacles = new List<SightObstacle>();
        private Texture2D texture;
        private Color32[] pixels;
        private Material material;
        private Mesh mesh;
        private int cachedVersion = -1, renderedVersion = -1;
        private Vector3 lastPosition;

        public void Initialize(HousePrototype layout, Transform character, Camera camera)
        {
            house = layout; player = character;
            map = new VisibilityMap(Resolution, Resolution, Origin, Origin, Extent / Resolution);
            texture = new Texture2D(Resolution, Resolution, TextureFormat.RGBA32, false, true)
                { name = "Current line of sight", filterMode = FilterMode.Point, wrapMode = TextureWrapMode.Clamp };
            pixels = new Color32[Resolution * Resolution];
            material = new Material(Resources.Load<Shader>("Effects/VisibilityOverlay"));
            material.SetTexture("_VisibilityTex", texture);
            mesh = new Mesh { name = "Fog of war overlay" };
            mesh.vertices = new[] { new Vector3(Origin, 8f, Origin), new Vector3(Origin + Extent, 8f, Origin),
                new Vector3(Origin + Extent, 8f, Origin + Extent), new Vector3(Origin, 8f, Origin + Extent) };
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
            if (!doorChanged && (player.position - lastPosition).sqrMagnitude < 0.000001f) return;
            Refresh();
        }

        private void Refresh()
        {
            EnsureObstacles();
            lastPosition = player.position;
            map.Calculate(lastPosition.x, lastPosition.z, obstacles);
            for (int cell = 0; cell < pixels.Length; cell++)
                pixels[cell] = new Color32(30, 32, 36, map.IsVisible(cell) ? (byte)0 : (byte)255);
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
