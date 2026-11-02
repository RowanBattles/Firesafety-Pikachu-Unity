using System;
using FireSafety.Interaction;
using UnityEngine;

namespace FireSafety.Equipment
{
    public sealed class WaterTap : WorldInteractable
    {
        private EquipmentSystem equipment;
        private Collider basin;
        public override string DisplayName => "Water tap";
        public override string StatusText => equipment.Inventory.Held == null
            ? "Bring an empty bucket." : "Holding: " + equipment.Inventory.Held.Name;
        public override string Prompt => "Fill your bucket here? It takes 4 seconds.";
        public override string ActionLabel => "Fill bucket";

        public void Initialize(EquipmentSystem owner, Collider sink)
        { equipment = owner; basin = sink; }

        public override Vector3 InteractionPoint(Vector3 from)
        {
            Bounds bounds = basin.bounds;
            return new Vector3(Mathf.Clamp(from.x, bounds.min.x, bounds.max.x), from.y,
                Mathf.Clamp(from.z, bounds.min.z, bounds.max.z));
        }

        public override bool TryInteract(Func<Bounds, bool> canPlaceDoor, out string message)
            => equipment.BeginFilling(this, out message);
    }
}
