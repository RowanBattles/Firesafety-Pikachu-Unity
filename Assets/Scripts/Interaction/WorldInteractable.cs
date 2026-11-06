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
        public virtual float MinimumUseRange => 0f;
        public virtual string SecondaryActionLabel => null;
        public virtual Vector3 SelectionPosition => transform.position + Vector3.up;
        public abstract Vector3 InteractionPoint(Vector3 from);
        public abstract bool TryInteract(Func<Bounds, bool> canPlaceDoor, out string message);
        public virtual bool TrySecondaryInteract(out string message) { message = ""; return false; }
        public virtual bool OwnsCollider(Collider part) => part.GetComponentInParent<WorldInteractable>() == this;
    }
}
