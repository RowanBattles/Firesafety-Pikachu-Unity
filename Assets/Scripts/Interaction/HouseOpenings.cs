using System.Collections.Generic;
using UnityEngine;

namespace FireSafety.Interaction
{
    // Procedural geometry lives here; interaction/state logic stays in the other components.
    public static class HouseOpenings
    {
        public static void Build(Transform house, Material door, Material frame, Material glass,
            Material handle, List<OpenableObject> openings)
        {
            Door(house, "Kitchen door", new Vector3(0f, 0f, -5f), -90f, 2f,
                door, frame, handle, openings);
            Door(house, "Bedroom door", new Vector3(0f, 0f, 3f), -90f, 2f,
                door, frame, handle, openings);
            Door(house, "Front door", new Vector3(-6.4f, 0f, -8f), 0f, 2.8f,
                door, frame, handle, openings);

            Window(house, "Living room window", new Vector3(-10f, 0f, -2f), -90f, frame, glass, openings);
            Window(house, "Living room garden window", new Vector3(-6f, 0f, 8f), 0f, frame, glass, openings);
            Window(house, "Bedroom window", new Vector3(4f, 0f, 8f), 0f, frame, glass, openings);
            Window(house, "Kitchen window", new Vector3(10f, 0f, -3.8f), 90f, frame, glass, openings);
        }

        private static void Door(Transform house, string name, Vector3 position, float yaw,
            float width, Material wood, Material frame, Material handle, List<OpenableObject> openings)
        {
            Transform root = Root(house, name, position, yaw);
            var opening = root.gameObject.AddComponent<OpenableObject>();
            Box(root, "Threshold", new Vector3(width * 0.5f, 0.025f, 0f),
                new Vector3(width, 0.05f, 0.36f), frame);
            Box(root, "Left frame", new Vector3(-0.04f, 0.65f, 0f), new Vector3(0.12f, 1.3f, 0.36f), frame);
            Box(root, "Right frame", new Vector3(width + 0.04f, 0.65f, 0f), new Vector3(0.12f, 1.3f, 0.36f), frame);
            Transform hinge = Root(root, "Door hinge", Vector3.zero, 0f);
            Collider panel = Box(hinge, "Door leaf", new Vector3(width * 0.5f, 0.55f, 0f),
                new Vector3(width, 1.1f, 0.16f), wood);
            Box(hinge, "Door handle", new Vector3(width - 0.2f, 1.12f, 0f), new Vector3(0.18f, 0.06f, 0.28f), handle);
            opening.Initialize(name, OpeningKind.Door, hinge, panel, 90f);
            openings.Add(opening);
        }

        private static void Window(Transform house, string name, Vector3 position, float yaw,
            Material frame, Material glass, List<OpenableObject> openings)
        {
            Transform root = Root(house, name, position, yaw);
            var opening = root.gameObject.AddComponent<OpenableObject>();
            Box(root, "Outer frame", new Vector3(0f, 1.15f, 0.2f), new Vector3(2.4f, 0.2f, 0.1f), frame);
            Box(root, "Inner frame", new Vector3(0f, 1.15f, -0.2f), new Vector3(2.4f, 0.2f, 0.1f), frame);
            Box(root, "Left frame", new Vector3(-1.15f, 1.15f, 0f), new Vector3(0.1f, 0.2f, 0.5f), frame);
            Box(root, "Right frame", new Vector3(1.15f, 1.15f, 0f), new Vector3(0.1f, 0.2f, 0.5f), frame);
            // Stay visible and clickable inside the room when the hinged sash swings into the fog.
            Box(root, "Fixed blue window sill", new Vector3(0f, 1.12f, -0.25f),
                new Vector3(2.2f, 0.2f, 0.4f), glass);
            Transform hinge = Root(root, "Window hinge", new Vector3(-1.1f, 1.15f, 0f), 0f);
            Collider panel = Box(hinge, "Blue window sash", new Vector3(1.1f, 0f, 0f), new Vector3(2.2f, 0.18f, 0.24f), glass);
            Box(hinge, "Window handle", new Vector3(2f, 0.12f, 0f), new Vector3(0.12f, 0.06f, 0.16f), frame);
            // Open outward, away from the walkable house interior. The wall remains a barrier.
            opening.Initialize(name, OpeningKind.Window, hinge, panel, -70f);
            openings.Add(opening);
        }

        private static Transform Root(Transform parent, string name, Vector3 position, float yaw)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            root.localRotation = Quaternion.Euler(0f, yaw, 0f);
            return root;
        }

        private static Collider Box(Transform parent, string name, Vector3 position, Vector3 scale, Material material)
        {
            var box = GameObject.CreatePrimitive(PrimitiveType.Cube);
            box.name = name;
            box.transform.SetParent(parent, false);
            box.transform.localPosition = position;
            box.transform.localScale = scale;
            box.GetComponent<Renderer>().sharedMaterial = material;
            return box.GetComponent<Collider>();
        }
    }
}
