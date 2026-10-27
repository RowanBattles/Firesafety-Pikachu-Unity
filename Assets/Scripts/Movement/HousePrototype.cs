using System.Collections.Generic;
using FireSafety.Interaction;
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
        private ClickToMovePlayer player;
        public GridPathfinder Navigation { get; private set; }

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
            Furniture("Coffee Table", -4.5f, 2, 1.3f, 2, 0.45f, wood);
            Furniture("Rug", -5.4f, 2, 5, 5, 0.015f, cream, false);
            Furniture("Kitchen Cabinets", 7, -6.8f, 5, 1.1f, 0.8f, white);
            Furniture("Countertop", 8.7f, -4.5f, 1.1f, 3.5f, 0.8f, white);
            Furniture("Dining Table", 4, -3.5f, 2, 1.5f, 0.55f, wood);
            Furniture("Bed", 6.5f, 5, 2.8f, 4, 0.45f, teal);
            Furniture("Pillow", 6.5f, 6.2f, 2.4f, 0.8f, 0.6f, white);
            Furniture("Wardrobe", 8.7f, 1.2f, 1.1f, 1.3f, 1f, wood);
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
        }

        public bool IsFloor(Collider surface) => floors.Contains(surface);

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
            }
            Navigation = new GridPathfinder(Width, Height, walkable);
        }

        private static bool OverlapsPlayer(Bounds bounds, Vector3 point)
            => point.x >= bounds.min.x - PlayerRadius && point.x <= bounds.max.x + PlayerRadius
                && point.z >= bounds.min.z - PlayerRadius && point.z <= bounds.max.z + PlayerRadius;

        private bool CanPlaceDoor(Bounds bounds)
        {
            Vector3 position = player.transform.position;
            // Protect the actual player and the grid anchor needed for their next route.
            return !OverlapsPlayer(bounds, position) && !OverlapsPlayer(bounds, CellToWorld(WorldToCell(position)));
        }

        private void OnDoorChanged()
        {
            BuildNavigation();
            if (player != null) player.StopMovement();
        }

        private void Floor(string name, float x, float z, float width, float depth, Material material)
        {
            GameObject floor = Box(name, new Vector3(x, -0.1f, z), new Vector3(width, 0.2f, depth), material);
            floors.Add(floor.GetComponent<Collider>());
        }

        private void Wall(string name, float x, float z, float width, float depth, Material material)
            => Furniture(name, x, z, width, depth, 1.2f, material);

        private void Furniture(string name, float x, float z, float width, float depth, float height,
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
