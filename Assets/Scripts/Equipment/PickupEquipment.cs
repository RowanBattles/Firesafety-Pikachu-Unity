using System;
using FireSafety.Interaction;
using UnityEngine;

namespace FireSafety.Equipment
{
    public sealed class PickupEquipment : WorldInteractable
    {
        public EquipmentItem Item { get; private set; }
        private EquipmentSystem equipment;
        private Collider hitbox;
        private GameObject water;
        public override string DisplayName => Item.Name;
        public override string StatusText => equipment.Inventory.Held == null
            ? "Inventory: Empty" : "Holding: " + equipment.Inventory.Held.Name;
        public override string Prompt => equipment.Inventory.Held == null
            ? "Put this item in your inventory?"
            : "Swap items and leave your item here?";
        public override string ActionLabel => equipment.Inventory.Held == null ? "Pick up" : "Swap items";

        public void Initialize(EquipmentSystem owner, EquipmentKind kind, Collider collider, GameObject waterSurface)
        {
            equipment = owner; Item = new EquipmentItem(kind); hitbox = collider; water = waterSurface;
            equipment.Register(this);
            RefreshContents();
        }

        public override Vector3 InteractionPoint(Vector3 from)
        {
            Bounds bounds = hitbox.bounds;
            return new Vector3(Mathf.Clamp(from.x, bounds.min.x, bounds.max.x), from.y,
                Mathf.Clamp(from.z, bounds.min.z, bounds.max.z));
        }

        public override bool TryInteract(Func<Bounds, bool> canPlaceDoor, out string message)
        { equipment.PickUp(this); message = ""; return true; }

        public void Carry(Transform character)
        {
            hitbox.enabled = false;
            transform.SetParent(character, false);
            transform.localPosition = new Vector3(0.43f, 0.2f, 0.1f);
            transform.localRotation = Quaternion.identity;
            transform.localScale = Vector3.one * 0.7f;
        }

        public void Drop(Transform room, Vector3 position, Quaternion rotation)
        {
            transform.SetParent(room, false);
            transform.position = position;
            transform.rotation = rotation;
            transform.localScale = Vector3.one;
            hitbox.enabled = true;
            Physics.SyncTransforms();
        }

        public void RefreshContents() { if (water != null) water.SetActive(Item.HasWater); }
    }
}
