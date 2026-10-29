using System.Collections.Generic;
using FireSafety.Interaction;
using FireSafety.Fire;
using FireSafety.Visibility;
using UnityEngine;

namespace FireSafety.Movement
{
    // Attached to SampleScene. Geometry is generated only in Play mode; no baking or extra package is needed.
    public sealed class HousePrototype : MonoBehaviour
    {
        private const int Width = 40, Height = 32;
        private const float CellSize = 0.5f, PlayerScale = 1.25f;
        private const float PlayerRadius = 0.3f * PlayerScale;
        private static readonly Vector3 Origin = new Vector3(-10f, 0f, -8f);
        private readonly List<Bounds> obstacles = new List<Bounds>();
        private readonly HashSet<Collider> floors = new HashSet<Collider>();
        private readonly List<Material> materials = new List<Material>();
        private readonly List<OpenableObject> openings = new List<OpenableObject>();
        private readonly List<Bounds> walls = new List<Bounds>();
        private readonly Dictionary<FireLocation, FireSite> fireSites = new Dictionary<FireLocation, FireSite>();
        private FireHazard fire;
        private FireShowcase showcase;
        private FogOfWar visibility;
        private bool[] navigationMask;
        private ClickToMovePlayer player;
        public GridPathfinder Navigation { get; private set; }
        public int GridWidth => Width;
        public int GridHeight => Height;
        public float GridCellSize => CellSize;
        public float CharacterRadius => PlayerRadius;
        public Vector3 GridOrigin => Origin;
        public bool GameplayBlocked => showcase != null && showcase.BlocksGameplay;
        public int OcclusionVersion { get; private set; }

        private void Awake()
        {
            Material wall = Material(new Color(0.25f, 0.34f, 0.36f));
            Material wood = Material(new Color(0.64f, 0.43f, 0.27f));
            Material cream = Material(new Color(0.87f, 0.82f, 0.69f));
            Material teal = Material(new Color(0.16f, 0.52f, 0.53f));
            Material white = Material(new Color(0.89f, 0.93f, 0.91f));
            Material dark = Material(new Color(0.13f, 0.23f, 0.27f));
            Floor("Living Room", -5f, 0f, 10f, 16f, wood);
            Floor("Kitchen", 5f, -4f, 10f, 8f, cream);
            Floor("Bedroom", 5f, 4f, 10f, 8f, Material(new Color(0.65f, 0.76f, 0.73f)));
            Box("Front Path", new Vector3(-5f, -0.16f, -10f), new Vector3(3f, 0.15f, 4f), cream);
            // Outer walls; the south wall leaves a visible front-door opening.
            Wall("North Wall", 0, 8, 20.3f, 0.3f, wall);
            Wall("West Wall", -10, 0, 0.3f, 16, wall);
            Wall("East Wall", 10, 0, 0.3f, 16, wall);
            Wall("South Wall Left", -8.2f, -8, 3.6f, 0.3f, wall);
            Wall("South Wall Right", 3.2f, -8, 13.6f, 0.3f, wall);
            // Two 2m doors connect the living room to the rooms on the east side.
            Wall("Partition South", 0, -6.5f, 0.3f, 3, wall);
            Wall("Partition Middle", 0, 0, 0.3f, 6, wall);
            Wall("Partition North", 0, 6.5f, 0.3f, 3, wall);
            Wall("Kitchen / Bedroom Wall", 5, 0, 10, 0.3f, wall);
            Furniture("Sofa", -7.5f, 2, 2.2f, 3.5f, 0.7f, teal);
            var table = Furniture("Coffee Table", -4.5f, 2, 1.3f, 2, 0.45f, wood);
            Furniture("Rug", -5.4f, 2, 5, 5, 0.015f, cream, false);
            var cabinets = Furniture("Kitchen Cabinets", 7, -6.8f, 5, 1.1f, 0.8f, white);
            Furniture("Countertop", 8.7f, -4.5f, 1.1f, 3.5f, 0.8f, white);
            Furniture("Dining Table", 4, -3.5f, 2, 1.5f, 0.55f, wood);
            var bed = Furniture("Bed", 6.5f, 5, 2.8f, 4, 0.45f, teal);
            Furniture("Pillow", 6.5f, 6.2f, 2.4f, 0.8f, 0.6f, white);
            Furniture("Wardrobe", 8.7f, 1.2f, 1.1f, 1.3f, 1f, wood);
            BuildFireSites(table, bed, cabinets, dark, white);
            HouseOpenings.Build(transform, Material(new Color(0.76f, 0.48f, 0.23f)), white,
                Material(new Color(0.28f, 0.72f, 0.88f)), dark, openings);
            foreach (OpenableObject opening in openings)
                if (opening.IsDoor) opening.StateChanged += OnDoorChanged;
            BuildNavigation();

            var character = new GameObject("Player");
            character.transform.SetParent(transform, false);
            character.transform.position = CellToWorld(WorldToCell(new Vector3(-5, 0, -4)));
            character.transform.localScale = Vector3.one * PlayerScale;
            var body = Primitive("Body", PrimitiveType.Capsule, character.transform,
                new Vector3(0, 0.6f, 0), new Vector3(0.55f, 0.6f, 0.55f), teal);
            Destroy(body.GetComponent<Collider>());
            var head = Primitive("Head", PrimitiveType.Sphere, character.transform,
                new Vector3(0, 1.25f, 0.05f), Vector3.one * 0.42f,
                Material(new Color(0.95f, 0.72f, 0.49f)));
            Destroy(head.GetComponent<Collider>());

            Camera view = Camera.main;
            if (view == null)
            {
                var cameraObject = new GameObject("Main Camera", typeof(Camera), typeof(AudioListener));
                cameraObject.tag = "MainCamera";
                view = cameraObject.GetComponent<Camera>();
            }
            view.gameObject.AddComponent<TopDownCamera>().Follow(character.transform);
            var marker = Primitive("Destination", PrimitiveType.Cylinder, transform,
                Vector3.zero, new Vector3(0.45f, 0.015f, 0.45f), Material(new Color(1f, 0.71f, 0.16f)));
            Destroy(marker.GetComponent<Collider>());
            player = character.AddComponent<ClickToMovePlayer>();
            player.Initialize(this, view, marker.transform);
            var interaction = gameObject.AddComponent<ObjectInteraction>();
            interaction.Initialize(view, player, this, CanPlaceDoor);
            player.SetInteraction(interaction);
            showcase = gameObject.AddComponent<FireShowcase>();
            showcase.Initialize(this, player);
            var fog = new GameObject("Room visibility");
            fog.transform.SetParent(transform, false);
            visibility = fog.AddComponent<FogOfWar>();
            visibility.Initialize(this, character.transform, view);
        }

        public bool IsFloor(Collider surface) => floors.Contains(surface);
        public bool IsVisiblePoint(Vector3 point) => visibility == null || visibility.CanSee(point);

        public void CopySightObstacles(List<SightObstacle> result)
        {
            result.Clear();
            foreach (Bounds wall in walls) AddSightObstacle(result, wall);
            foreach (OpenableObject opening in openings)
                if (opening.IsDoor) AddSightObstacle(result, opening.NavigationBounds);
        }

        private static void AddSightObstacle(List<SightObstacle> result, Bounds bounds)
        {
            // Close floating-point cracks where a door leaf meets a wall.
            const float seam = 0.002f;
            result.Add(new SightObstacle(bounds.min.x - seam, bounds.min.z - seam, bounds.max.x + seam, bounds.max.z + seam));
        }

        public int WorldToCell(Vector3 point)
        {
            int x = Mathf.FloorToInt((point.x - Origin.x) / CellSize);
            int z = Mathf.FloorToInt((point.z - Origin.z) / CellSize);
            return x < 0 || x >= Width || z < 0 || z >= Height ? -1 : z * Width + x;
        }

        public Vector3 CellToWorld(int cell) => Origin + new Vector3(
            (cell % Width + 0.5f) * CellSize, 0f, (cell / Width + 0.5f) * CellSize);

        private void BuildNavigation()
        {
            var walkable = new bool[Width * Height];
            for (int cell = 0; cell < walkable.Length; cell++)
            {
                Vector3 point = CellToWorld(cell);
                walkable[cell] = point.x > -10 + PlayerRadius && point.x < 10 - PlayerRadius
                    && point.z > -8 + PlayerRadius && point.z < 8 - PlayerRadius;
                foreach (Bounds obstacle in obstacles)
                {
                    if (OverlapsPlayer(obstacle, point))
                    {
                        walkable[cell] = false;
                        break;
                    }
                }
                foreach (OpenableObject opening in openings)
                    if (opening.IsDoor && OverlapsPlayer(opening.NavigationBounds, point)) walkable[cell] = false;
                if (fire != null && fire.BlocksCell(cell)) walkable[cell] = false;
            }
            // Keep existing paths when growing flames have not changed any walkable cells.
            bool changed = navigationMask == null;
            if (!changed)
                for (int cell = 0; cell < walkable.Length; cell++)
                    if (walkable[cell] != navigationMask[cell]) { changed = true; break; }
            if (!changed) return;
            navigationMask = walkable;
            Navigation = new GridPathfinder(Width, Height, walkable);
        }

        public void AttachFire(FireHazard hazard) => fire = hazard;
        public void RefreshFireNavigation() => BuildNavigation();
        public FireSite GetFireSite(FireLocation location) => fireSites[location];
        public bool IsFireDanger(Vector3 position) => fire != null
            && fire.Overlaps(new Bounds(position, Vector3.zero), PlayerRadius);

        public float SurfaceHeightAt(Vector3 position)
        {
            float height = 0f;
            foreach (Bounds obstacle in obstacles)
                if (position.x >= obstacle.min.x && position.x <= obstacle.max.x
                    && position.z >= obstacle.min.z && position.z <= obstacle.max.z)
                    height = Mathf.Max(height, obstacle.max.y);
            return height;
        }

        public bool[] CreateFirePassageMask()
        {
            var passage = new bool[Width * Height];
            for (int cell = 0; cell < passage.Length; cell++)
            {
                Vector3 point = CellToWorld(cell);
                passage[cell] = true;
                // Test whole cell footprints, not just centres: thin walls must not leak fire.
                foreach (Bounds wall in walls)
                    if (OverlapsCell(wall, point)) { passage[cell] = false; break; }
                foreach (OpenableObject opening in openings)
                    if (opening.IsDoor && !opening.IsOpen && OverlapsCell(opening.NavigationBounds, point)) passage[cell] = false;
            }
            return passage;
        }

        private static bool OverlapsCell(Bounds bounds, Vector3 point)
            => point.x >= bounds.min.x - CellSize * 0.5f && point.x <= bounds.max.x + CellSize * 0.5f
                && point.z >= bounds.min.z - CellSize * 0.5f && point.z <= bounds.max.z + CellSize * 0.5f;

        private static bool OverlapsPlayer(Bounds bounds, Vector3 point)
            => point.x >= bounds.min.x - PlayerRadius && point.x <= bounds.max.x + PlayerRadius
                && point.z >= bounds.min.z - PlayerRadius && point.z <= bounds.max.z + PlayerRadius;

        private bool CanPlaceDoor(Bounds bounds)
        {
            Vector3 position = player.transform.position;
            // Protect the actual player and the grid anchor needed for their next route.
            return !OverlapsPlayer(bounds, position) && !OverlapsPlayer(bounds, CellToWorld(WorldToCell(position)))
                && (fire == null || !fire.Overlaps(bounds, 0.05f));
        }

        private void OnDoorChanged()
        {
            OcclusionVersion++;
            BuildNavigation();
            if (player != null) player.StopMovement();
        }

        private void Floor(string name, float x, float z, float width, float depth, Material material)
        {
            GameObject floor = Box(name, new Vector3(x, -0.1f, z), new Vector3(width, 0.2f, depth), material);
            floors.Add(floor.GetComponent<Collider>());
        }

        private void Wall(string name, float x, float z, float width, float depth, Material material)
        {
            Furniture(name, x, z, width, depth, 1.2f, material);
            walls.Add(new Bounds(new Vector3(x, 0.6f, z), new Vector3(width, 1.2f, depth)));
        }

        private GameObject Furniture(string name, float x, float z, float width, float depth, float height,
            Material material, bool blocks = true)
        {
            Vector3 size = new Vector3(width, height, depth);
            Vector3 position = new Vector3(x, height * 0.5f, z);
            GameObject item = Box(name, position, size, material);
            if (blocks) obstacles.Add(new Bounds(position, size));
            else
            {
                // A rug is a walkable click surface, unlike furniture.
                floors.Add(item.GetComponent<Collider>());
            }
            return item;
        }

        private void BuildFireSites(GameObject table, GameObject bed, GameObject cabinets, Material dark, Material metal)
        {
            fireSites.Add(FireLocation.Table, new FireSite(new Vector3(-4.75f, 0.45f, 1.25f), table.transform, table.GetComponent<Collider>()));
            fireSites.Add(FireLocation.Bed, new FireSite(new Vector3(5.25f, 0.45f, 4.25f), bed.transform, bed.GetComponent<Collider>()));
            var bottle = Primitive("Paint bottle", PrimitiveType.Cylinder, transform, new Vector3(-7.25f, 0.32f, -4.25f),
                new Vector3(0.35f, 0.32f, 0.35f), Material(new Color(0.45f, 0.22f, 0.65f)));
            Primitive("Bottle cap", PrimitiveType.Cylinder, bottle.transform, new Vector3(0f, 1.05f, 0f),
                new Vector3(0.6f, 0.12f, 0.6f), dark);
            obstacles.Add(new Bounds(new Vector3(-7.25f, 0.32f, -4.25f), new Vector3(0.35f, 0.64f, 0.35f)));
            fireSites.Add(FireLocation.PaintBottle, new FireSite(new Vector3(-7.25f, 0.72f, -4.25f), bottle.transform, bottle.GetComponent<Collider>()));
            var hob = new GameObject("Gas hob"); hob.transform.SetParent(transform, false);
            Primitive("Hob surface", PrimitiveType.Cube, hob.transform, new Vector3(6.25f, 0.85f, -6.75f), new Vector3(1.1f, 0.1f, 0.8f), dark);
            Primitive("Gas burner", PrimitiveType.Cylinder, hob.transform, new Vector3(6.25f, 0.92f, -6.75f), new Vector3(0.5f, 0.02f, 0.5f), metal);
            fireSites.Add(FireLocation.Hob, new FireSite(new Vector3(6.25f, 0.95f, -6.75f), hob.transform, cabinets.GetComponent<Collider>()));
            var fryer = new GameObject("Deep fryer"); fryer.transform.SetParent(transform, false);
            Primitive("Fryer body", PrimitiveType.Cube, fryer.transform, new Vector3(8.25f, 0.95f, -6.75f), new Vector3(0.8f, 0.3f, 0.7f), metal);
            Primitive("Oil basket", PrimitiveType.Cube, fryer.transform, new Vector3(8.25f, 1.12f, -6.75f), new Vector3(0.6f, 0.04f, 0.5f), Material(new Color(0.46f, 0.27f, 0.06f)));
            fireSites.Add(FireLocation.DeepFryer, new FireSite(new Vector3(8.25f, 1.15f, -6.75f), fryer.transform, cabinets.GetComponent<Collider>()));
        }

        private GameObject Box(string name, Vector3 position, Vector3 size, Material material)
            => Primitive(name, PrimitiveType.Cube, transform, position, size, material);

        private static GameObject Primitive(string name, PrimitiveType type, Transform parent,
            Vector3 position, Vector3 scale, Material material)
        {
            GameObject item = GameObject.CreatePrimitive(type);
            item.name = name;
            item.transform.SetParent(parent, false);
            item.transform.localPosition = position;
            item.transform.localScale = scale;
            item.GetComponent<Renderer>().sharedMaterial = material;
            return item;
        }

        private Material Material(Color color)
        {
            var material = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            material.SetColor("_BaseColor", color);
            material.SetFloat("_Smoothness", 0.15f);
            materials.Add(material);
            return material;
        }

        private void OnDestroy()
        {
            foreach (OpenableObject opening in openings)
                if (opening != null) opening.StateChanged -= OnDoorChanged;
            foreach (Material material in materials) Destroy(material);
        }
    }
}
