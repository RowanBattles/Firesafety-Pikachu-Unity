using System;
using System.Collections.Generic;
using FireSafety.Movement;
using FireSafety.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FireSafety.Interaction
{
    // Consume interaction clicks before ClickToMovePlayer can interpret them as movement.
    [DefaultExecutionOrder(-100)]
    public sealed class ObjectInteraction : MonoBehaviour
    {
        private Camera view;
        private ClickToMovePlayer player;
        private HousePrototype house;
        private Func<Bounds, bool> canPlaceDoor;
        private WorldInteractable selected;
        private WorldInteractable approaching;
        private WorldInteractable highlighted;
        private Transform selectionRing;
        private GridPathfinder approachNavigation;
        private readonly List<int> approachPath = new List<int>();
        private int consumedFrame = -1;
        private string message = "";
        private GUIStyle titleStyle;
        private GUIStyle textStyle;
        private GUIStyle buttonStyle;

        public bool BlocksMovementInput => selected != null || consumedFrame == Time.frameCount;
        private bool HasSecondaryAction => selected != null && !string.IsNullOrEmpty(selected.SecondaryActionLabel);
        private bool HasStatus => selected != null && !string.IsNullOrEmpty(selected.StatusText);
        private bool HasPrompt => selected != null && !string.IsNullOrEmpty(selected.Prompt);
        private float ContentBottom => 48f + (HasStatus ? 28f : 0f) + (HasPrompt ? 36f : 0f);
        private float PopupHeight => ActionY + 104f;
        private float ActionY => ContentBottom + (HasSecondaryAction ? 60f : 0f) + 8f;
        private Rect PopupRect => new Rect((Screen.width - PopupWidth) * 0.5f,
            (Screen.height - PopupHeight) * 0.5f, PopupWidth, PopupHeight);
        private float PopupWidth => Mathf.Min(360f, Screen.width - 24f);
        private Rect CancelButton => new Rect(PopupRect.x + 18f, PopupRect.y + ActionY,
            (PopupWidth - 48f) * 0.5f, 38f);
        private Rect ToggleButton => selected != null && !selected.ShowCancel
            ? new Rect(PopupRect.x + 18f, PopupRect.y + ActionY, PopupWidth - 36f, 38f)
            : new Rect(PopupRect.center.x + 6f, PopupRect.y + ActionY, (PopupWidth - 48f) * 0.5f, 38f);
        private Rect SecondaryButton => new Rect(PopupRect.x + 18f, PopupRect.y + ContentBottom, PopupWidth - 36f, 50f);

        public void Initialize(Camera camera, ClickToMovePlayer character, HousePrototype layout,
            Func<Bounds, bool> doorValidator)
        {
            view = camera;
            player = character;
            house = layout;
            canPlaceDoor = doorValidator;
            selectionRing = WorldRing.Create(house.transform, "Selected object", new Color(1f, 0.76f, 0.15f));
        }

        private void LateUpdate()
        {
            if (selectionRing == null) return;
            selectionRing.gameObject.SetActive(highlighted != null);
            if (highlighted != null) selectionRing.position = highlighted.SelectionPosition;
        }

        private void Update()
        {
            if (view == null || player == null || house == null) return;
            if (house.GameplayBlocked) { Dismiss(); return; }
            if ((selected != null || approaching != null) && Keyboard.current != null
                && Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                player.StopMovement();
                Dismiss();
                return;
            }

            // Recheck access while a menu is open as well as immediately before using the object.
            if (selected != null && !CanInteractFrom(selected, player.transform.position))
            { Dismiss(); return; }

            if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                HandleClick(Mouse.current.position.ReadValue());
                return;
            }
            if (approaching == null) return;
            if (approachNavigation != house.Navigation)
            {
                // A door changed while walking: find a fresh route or cancel the interaction.
                BeginApproach(approaching);
                return;
            }
            if (player.IsMoving) return;
            if (CanInteractFrom(approaching, player.transform.position))
            {
                selected = approaching;
                approaching = null;
                player.StopMovement();
            }
            else Dismiss();
        }

        private void HandleClick(Vector2 pointer)
        {
            if (selected != null)
            {
                consumedFrame = Time.frameCount;
                Vector2 guiPointer = new Vector2(pointer.x, Screen.height - pointer.y);
                // New Input System cannot feed IMGUI controls: handle popup clicks here too.
                if (selected.ShowCancel && CancelButton.Contains(guiPointer)) Dismiss();
                else if (HasSecondaryAction && SecondaryButton.Contains(guiPointer))
                {
                    if (!CanInteractFrom(selected, player.transform.position)) Dismiss();
                    else if (selected.TrySecondaryInteract(out message)) Dismiss();
                }
                else if (ToggleButton.Contains(guiPointer))
                {
                    if (!CanInteractFrom(selected, player.transform.position)) Dismiss();
                    else
                    {
                        WorldInteractable clickedObject = selected;
                        string choice = clickedObject.ActionLabel;
                        bool success = clickedObject.TryInteract(canPlaceDoor, out message);
                        if (house.Tutorial != null) house.Tutorial.InteractionDecision(clickedObject, choice, success, message);
                        if (success) Dismiss();
                    }
                }
                else if (!PopupRect.Contains(guiPointer)) Dismiss();
                return;
            }

            WorldInteractable target = null;
            if (Physics.Raycast(view.ScreenPointToRay(pointer), out RaycastHit hit, 100f))
            {
                if (!house.IsVisiblePoint(hit.point))
                {
                    player.StopMovement();
                    Dismiss();
                    return;
                }
                target = hit.collider.GetComponentInParent<WorldInteractable>();
            }
            if (target == null)
            {
                highlighted = null;
                if (approaching != null)
                {
                    approaching = null;
                    player.StopMovement();
                }
                // Leave this click available for the normal floor-movement handler.
                return;
            }
            consumedFrame = Time.frameCount;
            BeginApproach(target);
        }

        private void BeginApproach(WorldInteractable target)
        {
            highlighted = target;
            approaching = null;
            selected = null;
            message = "";
            player.StopMovement();
            if (CanInteractFrom(target, player.transform.position))
            {
                selected = target;
                return;
            }
            if (!house.Navigation.TryFindPathToAny(house.WorldToCell(player.transform.position),
                cell => CanInteractFrom(target, house.CellToWorld(cell)), approachPath)) return;
            approaching = target;
            approachNavigation = house.Navigation;
            player.FollowPath(approachPath, false);
        }

        public bool CanInteractFrom(WorldInteractable target, Vector3 position)
        {
            if (target == null) return false;
            return CanReachPoint(target, position, target.InteractionPoint(position), target.MinimumUseRange, target.UseRange);
        }

        public bool CanReachPoint(WorldInteractable target, Vector3 position, Vector3 point,
            float minimumRange, float maximumRange)
        {
            if (target == null) return false;
            float distanceSquared = (point - position).sqrMagnitude;
            if (distanceSquared > maximumRange * maximumRange
                || distanceSquared < minimumRange * minimumRange
                || house.IsFireDanger(position)) return false;
            // Check at body height so low walls cannot be bypassed by looking over their tops.
            Vector3 start = new Vector3(position.x, 0.6f, position.z);
            Vector3 end = new Vector3(point.x, 0.6f, point.z);
            Vector3 direction = end - start;
            if (direction.sqrMagnitude < 0.0001f) return true;
            foreach (RaycastHit hit in Physics.RaycastAll(start, direction.normalized, direction.magnitude,
                Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore))
            {
                if (!target.OwnsCollider(hit.collider)) return false;
            }
            return true;
        }

        private void OnGUI()
        {
            if (selected == null) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 18, fontStyle = FontStyle.Bold };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, wordWrap = true };
            }
            Color previousColor = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.35f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previousColor;
            Rect popup = PopupRect;
            GUI.Box(popup, GUIContent.none, GUI.skin.window);
            GUI.Label(new Rect(popup.x + 18f, popup.y + 14f, popup.width - 36f, 28f), selected.DisplayName, titleStyle);
            if (HasStatus) GUI.Label(new Rect(popup.x + 18f, popup.y + 48f, popup.width - 36f, 24f), selected.StatusText, textStyle);
            if (HasPrompt) GUI.Label(new Rect(popup.x + 18f, popup.y + 48f + (HasStatus ? 28f : 0f), popup.width - 36f, 32f), selected.Prompt, textStyle);
            if (HasSecondaryAction) DrawButton(SecondaryButton, selected.SecondaryActionLabel);
            if (selected.ShowCancel) DrawButton(CancelButton, "Do nothing");
            DrawButton(ToggleButton, selected.ActionLabel);
            if (!string.IsNullOrEmpty(message))
                GUI.Label(new Rect(popup.x + 18f, popup.y + ActionY + 48f, popup.width - 36f, 48f), message, textStyle);
        }

        private void DrawButton(Rect bounds, string label)
        {
            Color previousColor = GUI.backgroundColor;
            if (Mouse.current != null)
            {
                Vector2 pointer = Mouse.current.position.ReadValue();
                if (bounds.Contains(new Vector2(pointer.x, Screen.height - pointer.y)))
                    GUI.backgroundColor = new Color(0.65f, 0.9f, 1f);
            }
            GUI.Box(bounds, label, buttonStyle);
            GUI.backgroundColor = previousColor;
        }

        private void Dismiss()
        {
            selected = null;
            approaching = null;
            highlighted = null;
            message = "";
            consumedFrame = Time.frameCount;
        }
    }
}
