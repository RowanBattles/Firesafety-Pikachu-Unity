using UnityEngine;

namespace FireSafety.Equipment
{
    public static class EquipmentWorld
    {
        public static void Build(Transform room, EquipmentSystem equipment, GameObject sink,
            Material red, Material white, Material dark, Material blue)
        {
            var extinguisher = Root(room, "ABC powder extinguisher", new Vector3(-9.64f, 0f, 5f));
            Shape(extinguisher, "Red cylinder", PrimitiveType.Cylinder, new Vector3(0f, 0.4f, 0f),
                new Vector3(0.42f, 0.35f, 0.42f), red);
            Shape(extinguisher, "White label", PrimitiveType.Cube, new Vector3(0f, 0.45f, -0.21f),
                new Vector3(0.22f, 0.28f, 0.025f), white);
            Shape(extinguisher, "Handle", PrimitiveType.Cube, new Vector3(0.08f, 0.83f, 0f),
                new Vector3(0.36f, 0.1f, 0.12f), dark);
            Shape(extinguisher, "Hose", PrimitiveType.Cube, new Vector3(0.27f, 0.53f, 0f),
                new Vector3(0.09f, 0.55f, 0.09f), dark);
            var extinguisherHitbox = extinguisher.gameObject.AddComponent<BoxCollider>();
            extinguisherHitbox.center = new Vector3(0f, 0.45f, 0f);
            extinguisherHitbox.size = new Vector3(0.65f, 0.9f, 0.55f);
            extinguisher.gameObject.AddComponent<PickupEquipment>().Initialize(equipment,
                EquipmentKind.AbcPowderExtinguisher, extinguisherHitbox, null);

            var bucket = Root(room, "Water bucket", new Vector3(9.25f, 0f, -0.75f));
            Shape(bucket, "Bucket body", PrimitiveType.Cylinder, new Vector3(0f, 0.26f, 0f),
                new Vector3(0.6f, 0.25f, 0.6f), blue);
            Shape(bucket, "Empty interior", PrimitiveType.Cylinder, new Vector3(0f, 0.515f, 0f),
                new Vector3(0.5f, 0.015f, 0.5f), dark);
            var water = Shape(bucket, "Water", PrimitiveType.Cylinder, new Vector3(0f, 0.54f, 0f),
                new Vector3(0.49f, 0.012f, 0.49f), white);
            Shape(bucket, "Handle", PrimitiveType.Cube, new Vector3(0f, 0.71f, 0f),
                new Vector3(0.65f, 0.07f, 0.07f), dark);
            var bucketHitbox = bucket.gameObject.AddComponent<BoxCollider>();
            bucketHitbox.center = new Vector3(0f, 0.4f, 0f);
            bucketHitbox.size = new Vector3(0.7f, 0.8f, 0.7f);
            bucket.gameObject.AddComponent<PickupEquipment>().Initialize(equipment,
                EquipmentKind.Bucket, bucketHitbox, water);

            var tap = sink.AddComponent<WaterTap>();
            tap.Initialize(equipment, sink.GetComponent<Collider>());
            // Sink roots are scaled cubes. Keep the tap geometry in world metre proportions.
            var fittings = Root(room, "Tap fittings", sink.transform.position + Vector3.up * 0.4f);
            fittings.SetParent(sink.transform, true);
            Shape(fittings, "Basin", PrimitiveType.Cube, Vector3.zero, new Vector3(0.9f, 0.06f, 0.65f), dark);
            Shape(fittings, "Tap stem", PrimitiveType.Cylinder, new Vector3(0f, 0.23f, 0.25f),
                new Vector3(0.1f, 0.22f, 0.1f), white);
            Shape(fittings, "Spout", PrimitiveType.Cube, new Vector3(0f, 0.45f, 0.05f),
                new Vector3(0.1f, 0.1f, 0.5f), white);
            Shape(fittings, "Blue tap handle", PrimitiveType.Cube, new Vector3(0f, 0.51f, 0.25f),
                new Vector3(0.35f, 0.07f, 0.1f), blue);
        }

        private static Transform Root(Transform parent, string name, Vector3 position)
        {
            var root = new GameObject(name).transform;
            root.SetParent(parent, false);
            root.localPosition = position;
            return root;
        }

        private static GameObject Shape(Transform parent, string name, PrimitiveType kind,
            Vector3 position, Vector3 scale, Material material)
        {
            var part = GameObject.CreatePrimitive(kind);
            part.name = name;
            part.transform.SetParent(parent, false);
            part.transform.localPosition = position;
            part.transform.localScale = scale;
            part.GetComponent<Renderer>().sharedMaterial = material;
            var collider = part.GetComponent<Collider>();
            collider.enabled = false;
            Object.Destroy(collider);
            return part;
        }
    }
}
