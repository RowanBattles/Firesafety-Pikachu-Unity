using System;
using UnityEngine;

namespace FireSafety.Interaction
{
    public abstract class WorldInteractable : MonoBehaviour
    {
        public abstract string DisplayName { get; }
        public abstract string StatusText { get; }
        public abstract string ActionLabel { get; }
        public virtual string Prompt => "What would you like to do?";
        public virtual bool ShowCancel => true;
        public virtual float UseRange => 0.9f;
        public abstract Vector3 InteractionPoint(Vector3 from);
        public abstract bool TryInteract(Func<Bounds, bool> canPlaceDoor, out string message);
        public virtual bool OwnsCollider(Collider part) => part.GetComponentInParent<WorldInteractable>() == this;
    }
}
