using System;
using UnityEngine;

namespace FireSafety.Interaction
{
    public enum OpeningKind { Door, Window }

    // Owns the open/closed state and hinged visual, independently of mouse input.
    public sealed class OpenableObject : MonoBehaviour
    {
        public string DisplayName { get; private set; }
        public bool IsOpen { get; private set; }
        public bool IsDoor { get; private set; }
        public string ActionLabel => IsOpen ? "Close" : "Open";
        public Bounds NavigationBounds => IsOpen ? openBounds : closedBounds;
        public Bounds InteractionBounds { get; private set; }
        public event Action StateChanged;

        private Transform hinge;
        private Quaternion closedRotation;
        private Quaternion openRotation;
        private Bounds closedBounds;
        private Bounds openBounds;

        public void Initialize(string displayName, OpeningKind kind, Transform leafHinge,
            Collider panel, float openingAngle)
        {
            DisplayName = displayName;
            IsDoor = kind == OpeningKind.Door;
            hinge = leafHinge;
            closedRotation = hinge.localRotation;
            openRotation = closedRotation * Quaternion.Euler(0f, openingAngle, 0f);

            // Cache both footprints, including the house/root rotation, before gameplay begins.
            Physics.SyncTransforms();
            closedBounds = panel.bounds;
            // Keep interaction at the fixed frame, including when a window swings outside the house.
            Bounds frameBounds = closedBounds;
            foreach (Collider part in GetComponentsInChildren<Collider>()) frameBounds.Encapsulate(part.bounds);
            InteractionBounds = frameBounds;
            hinge.localRotation = openRotation;
            Physics.SyncTransforms();
            openBounds = panel.bounds;
            hinge.localRotation = closedRotation;
            Physics.SyncTransforms();
        }

        public Vector3 InteractionPoint(Vector3 from)
        {
            Bounds bounds = InteractionBounds;
            return new Vector3(Mathf.Clamp(from.x, bounds.min.x, bounds.max.x), from.y,
                Mathf.Clamp(from.z, bounds.min.z, bounds.max.z));
        }

        public bool TryToggle(Func<Bounds, bool> canPlaceDoor, out string message)
        {
            Bounds targetBounds = IsOpen ? closedBounds : openBounds;
            if (IsDoor && !canPlaceDoor(targetBounds))
            {
                message = IsOpen ? "Move away from the door before closing it."
                    : "Move away from the door before opening it.";
                return false;
            }

            IsOpen = !IsOpen;
            hinge.localRotation = IsOpen ? openRotation : closedRotation;
            Physics.SyncTransforms();
            StateChanged?.Invoke();
            message = "";
            return true;
        }
    }
}
