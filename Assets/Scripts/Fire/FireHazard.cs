using System;
using System.Collections.Generic;
using FireSafety.Interaction;
using FireSafety.Movement;
using UnityEngine;
using UnityEngine.Rendering;

namespace FireSafety.Fire
{
    public sealed class FireHazard : WorldInteractable
    {
        public FireSimulation Simulation { get; private set; }
        public override string DisplayName => "Class " + Simulation.Type + " fire";
        public override string StatusText => "Current fire size: " + Simulation.SizeCm.ToString("0") + " cm";
        public override string ActionLabel => "Continue";
        public override string Prompt => "";
        public override bool ShowCancel => false;
        public override float UseRange => 1.4f;
        private HousePrototype house;
        private FireSite site;
        private Mesh mesh;
        private MeshCollider hitSurface;
        private Material outerMaterial, coreMaterial;
        private readonly List<Bounds> footprint = new List<Bounds>();
        private bool[] danger;

        public void Initialize(HousePrototype layout, FireType type, FireLocation location, FireSite ignition)
        {
            house = layout; site = ignition;
            Simulation = new FireSimulation(house.GridWidth, house.GridHeight, house.GridCellSize,
                house.WorldToCell(site.Position), type, location);
            mesh = new Mesh { name = "Fire flame mesh" };
            gameObject.AddComponent<MeshFilter>().sharedMesh = mesh;
            var renderer = gameObject.AddComponent<MeshRenderer>();
            outerMaterial = FlameMaterial(new Color(1f, 0.22f, 0.025f));
            coreMaterial = FlameMaterial(new Color(1f, 0.82f, 0.12f));
            renderer.sharedMaterials = new[] { outerMaterial, coreMaterial };
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            hitSurface = gameObject.AddComponent<MeshCollider>();
            Refresh();
        }

        public void Tick(float seconds)
        {
            Simulation.Advance(seconds, house.CreateFirePassageMask());
            Refresh();
        }

        public bool BlocksCell(int cell) => danger != null && cell >= 0 && cell < danger.Length && danger[cell];

        public bool Overlaps(Bounds area, float margin = 0f)
        {
            foreach (Bounds tile in footprint)
                if (area.max.x + margin >= tile.min.x && area.min.x - margin <= tile.max.x
                    && area.max.z + margin >= tile.min.z && area.min.z - margin <= tile.max.z) return true;
            return false;
        }

        public override Vector3 InteractionPoint(Vector3 from)
        {
            Vector3 closest = from;
            float best = float.PositiveInfinity;
            foreach (Bounds tile in footprint)
            {
                Vector3 point = new Vector3(Mathf.Clamp(from.x, tile.min.x, tile.max.x), from.y,
                    Mathf.Clamp(from.z, tile.min.z, tile.max.z));
                float distance = (point - from).sqrMagnitude;
                if (distance < best) { best = distance; closest = point; }
            }
            return closest;
        }

        public override bool OwnsCollider(Collider part) => base.OwnsCollider(part)
            || part == site.Support || part.transform.IsChildOf(site.Prop);

        public override bool TryInteract(Func<Bounds, bool> canPlaceDoor, out string message)
        {
            message = "";
            return true; // Continue closes the inspection; fire keeps growing throughout.
        }

        private void Refresh()
        {
            footprint.Clear();
            var vertices = new List<Vector3>();
            var outer = new List<int>();
            var core = new List<int>();
            danger = new bool[house.GridWidth * house.GridHeight];
            for (int cell = 0; cell < danger.Length; cell++)
            {
                if (!Simulation.IsBurning(cell)) continue;
                bool seed = cell == Simulation.Seed;
                Vector3 position = seed ? site.Position : house.CellToWorld(cell);
                if (!seed) position.y = house.SurfaceHeightAt(position);
                float size = seed ? (float)Simulation.SourceWidthMeters : house.GridCellSize;
                Bounds tile = new Bounds(new Vector3(position.x, 0f, position.z), new Vector3(size, 1f, size));
                footprint.Add(tile);
                float height = seed ? Mathf.Clamp(0.4f * Mathf.Sqrt((float)(Simulation.GrowthSizeCm / 20)), 0.35f, 2.2f)
                    : 0.65f + 0.2f * Mathf.Sin(cell * 2.3f);
                float flicker = 1f + 0.1f * Mathf.Sin(Time.time * 8f + cell);
                float flameHeight = height * flicker;
                Cone(position, size * 0.5f, flameHeight, vertices, outer);
                Cone(position + Vector3.up * (flameHeight * 0.35f + 0.015f), size * 0.3f,
                    flameHeight * 0.8f, vertices, core);
                MarkDanger(tile);
            }
            hitSurface.sharedMesh = null;
            mesh.Clear();
            mesh.SetVertices(vertices);
            mesh.subMeshCount = 2;
            mesh.SetTriangles(outer, 0); mesh.SetTriangles(core, 1);
            mesh.RecalculateBounds();
            hitSurface.sharedMesh = mesh;
            Physics.SyncTransforms();
            house.RefreshFireNavigation();
        }

        private void MarkDanger(Bounds tile)
        {
            Vector3 origin = house.GridOrigin;
            float step = house.GridCellSize, radius = house.CharacterRadius;
            int minX = Mathf.Clamp(Mathf.FloorToInt((tile.min.x - radius - origin.x) / step), 0, house.GridWidth - 1);
            int maxX = Mathf.Clamp(Mathf.FloorToInt((tile.max.x + radius - origin.x) / step), 0, house.GridWidth - 1);
            int minZ = Mathf.Clamp(Mathf.FloorToInt((tile.min.z - radius - origin.z) / step), 0, house.GridHeight - 1);
            int maxZ = Mathf.Clamp(Mathf.FloorToInt((tile.max.z + radius - origin.z) / step), 0, house.GridHeight - 1);
            for (int z = minZ; z <= maxZ; z++)
                for (int x = minX; x <= maxX; x++)
                {
                    int cell = z * house.GridWidth + x;
                    Vector3 point = house.CellToWorld(cell);
                    if (point.x >= tile.min.x - radius && point.x <= tile.max.x + radius
                        && point.z >= tile.min.z - radius && point.z <= tile.max.z + radius) danger[cell] = true;
                }
        }

        private static void Cone(Vector3 centre, float radius, float height, List<Vector3> vertices, List<int> indices)
        {
            int first = vertices.Count;
            for (int i = 0; i < 8; i++)
            {
                float angle = i * Mathf.PI * 0.25f;
                vertices.Add(centre + new Vector3(Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius));
            }
            vertices.Add(centre + new Vector3(radius * 0.16f, height, 0f));
            for (int i = 0; i < 8; i++) { indices.Add(first + i); indices.Add(first + 8); indices.Add(first + (i + 1) % 8); }
        }

        private static Material FlameMaterial(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            material.SetColor("_BaseColor", color);
            return material;
        }

        private void OnDestroy()
        {
            if (mesh != null) Destroy(mesh);
            if (outerMaterial != null) Destroy(outerMaterial);
            if (coreMaterial != null) Destroy(coreMaterial);
        }
    }
}
