using FireSafety.Fire;
using FireSafety.Interaction;
using FireSafety.Movement;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FireSafety.Equipment
{
    [DefaultExecutionOrder(-150)]
    public sealed class EquipmentSystem : MonoBehaviour
    {
        private enum PanelState { None, Filling, Distance, Result }
        public SingleSlotInventory Inventory { get; } = new SingleSlotInventory();
        public int Score { get; private set; }
        public bool BlocksGameplay => panel != PanelState.None || consumedFrame == Time.frameCount;
        private HousePrototype house;
        private ClickToMovePlayer player;
        private ObjectInteraction interaction;
        private PickupEquipment held;
        private WaterTap fillingTap;
        private BucketFilling filling;
        private FireHazard targetFire;
        private ExtinguishingAttempt attempt;
        private PanelState panel;
        private string resultTitle, resultText;
        private int consumedFrame = -1;
        private GUIStyle titleStyle, textStyle, buttonStyle, hudStyle;
        private readonly string[] distances = { "Less than 1 metre", "1–2 metres", "3–4 metres", "More than 6 metres" };
        private float Width => Mathf.Min(490f, Screen.width - 24f);
        private Rect Area => new Rect((Screen.width - Width) * 0.5f, (Screen.height - 390f) * 0.5f, Width, 390f);
        private Rect Choice(int index) => new Rect(Area.x + 18f, Area.y + 116f + index * 45f, Width - 36f, 37f);
        private Rect ContinueButton => new Rect(Area.x + 18f, Area.y + 330f, Width - 36f, 38f);
        private Rect InventoryRect => new Rect(12f, 12f, Mathf.Min(255f, Screen.width - 24f), 76f);

        public void Initialize(HousePrototype layout, ClickToMovePlayer character, ObjectInteraction controller)
        { house = layout; player = character; interaction = controller; }

        public void PickUp(PickupEquipment incoming)
        {
            Vector3 place = incoming.transform.position;
            Quaternion rotation = incoming.transform.rotation;
            PickupEquipment previous = held;
            Inventory.Swap(incoming.Item);
            held = incoming;
            incoming.Carry(player.transform);
            if (previous != null) previous.Drop(house.transform, place, rotation);
            consumedFrame = Time.frameCount;
        }

        public bool BeginFilling(WaterTap tap, out string message)
        {
            message = "";
            if (Inventory.Held == null || Inventory.Held.Kind != EquipmentKind.Bucket)
            { message = "Bring an empty bucket to the tap."; return false; }
            if (Inventory.Held.HasWater) { message = "The bucket is already full."; return false; }
            filling = new BucketFilling(Inventory.Held);
            fillingTap = tap;
            player.StopMovement();
            panel = PanelState.Filling;
            consumedFrame = Time.frameCount;
            return true;
        }

        public bool BeginExtinguishing(FireHazard fire, out string message)
        {
            message = "";
            targetFire = fire;
            attempt = new ExtinguishingAttempt(fire.Simulation, Inventory.Held);
            player.StopMovement();
            consumedFrame = Time.frameCount;
            if (ExtinguishingRules.Check(fire.Simulation, Inventory.Held) != ExtinguishingBlock.None
                || !ExtinguishingRules.IsExtinguisher(Inventory.Held.Kind)) ResolveAttempt();
            else panel = PanelState.Distance;
            return true;
        }

        private void Update()
        {
            if (house == null) return;
            if (house.ScenarioBlocked) { Close(); return; }
            if (panel == PanelState.None)
            {
                // The inventory display must not send a click through to the floor beneath it.
                if (Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
                {
                    Vector2 hudPointer = Mouse.current.position.ReadValue();
                    if (InventoryRect.Contains(new Vector2(hudPointer.x, Screen.height - hudPointer.y)))
                        consumedFrame = Time.frameCount;
                }
                return;
            }
            if (panel == PanelState.Filling)
            {
                filling.Advance(Time.deltaTime, Inventory.Held,
                    fillingTap != null && interaction.CanInteractFrom(fillingTap, player.transform.position));
                if (filling.IsComplete) { held.RefreshContents(); Close(); return; }
                if (filling.IsCancelled) { Close(); return; }
            }
            if (panel == PanelState.Distance)
            {
                if (ExtinguishingRules.Check(targetFire.Simulation, Inventory.Held) != ExtinguishingBlock.None)
                { ResolveAttempt(); return; }
                if (!interaction.CanInteractFrom(targetFire, player.transform.position))
                { ShowResult("Out of reach", "You can no longer reach this fire safely. No points scored."); return; }
            }
            if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            { Close(); return; }
            if (consumedFrame == Time.frameCount || Mouse.current == null
                || !Mouse.current.leftButton.wasPressedThisFrame) return;
            Vector2 pointer = Mouse.current.position.ReadValue();
            pointer.y = Screen.height - pointer.y;
            if (panel == PanelState.Distance)
            {
                for (int choice = 0; choice < distances.Length; choice++)
                    if (Choice(choice).Contains(pointer)) { ResolveAttempt(choice); return; }
            }
            if (ContinueButton.Contains(pointer)) Close();
        }

        private void ResolveAttempt(int choice = -1)
        {
            ExtinguishingOutcome outcome = attempt.Resolve(choice);
            Score += outcome.Points;
            if (held != null) held.RefreshContents();
            if (outcome.Extinguished) targetFire.RefreshAfterExtinguishing();
            string points = outcome.Points > 0 ? "+" + outcome.Points + " points"
                : outcome.Points < 0 ? outcome.Points + " points" : "No points scored";
            ShowResult(outcome.Extinguished ? "Fire extinguished" : "Extinguishing stopped", outcome.Message + "\n\n" + points);
        }

        private void ShowResult(string title, string text)
        { resultTitle = title; resultText = text; panel = PanelState.Result; consumedFrame = Time.frameCount; }

        private void Close()
        {
            if (panel == PanelState.None) return;
            filling?.Cancel();
            filling = null; fillingTap = null; attempt = null; targetFire = null;
            panel = PanelState.None;
            consumedFrame = Time.frameCount;
        }

        private void OnGUI()
        {
            if (house == null || house.ScenarioBlocked) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 19, fontStyle = FontStyle.Bold };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 14, wordWrap = true };
                hudStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, wordWrap = true };
            }
            Rect slot = InventoryRect;
            GUI.Box(slot, GUIContent.none);
            GUI.Label(new Rect(slot.x + 10f, slot.y + 6f, slot.width - 20f, 46f),
                "Inventory (1 slot)\n" + (Inventory.Held == null ? "Empty" : Inventory.Held.Name), hudStyle);
            GUI.Label(new Rect(slot.x + 10f, slot.y + 51f, slot.width - 20f, 22f), "Score: " + Score, hudStyle);
            if (panel == PanelState.None) return;
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.4f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
            Rect area = Area;
            GUI.Box(area, GUIContent.none, GUI.skin.window);
            GUI.Label(new Rect(area.x + 18f, area.y + 14f, Width - 36f, 32f),
                panel == PanelState.Filling ? "Filling the bucket" : panel == PanelState.Distance
                    ? "Extinguishing distance" : resultTitle, titleStyle);
            if (panel == PanelState.Distance)
            {
                GUI.Label(new Rect(area.x + 18f, area.y + 52f, Width - 36f, 58f),
                    "At what distance should you start using this " + Inventory.Held.Name + "?", textStyle);
                for (int i = 0; i < distances.Length; i++) DrawButton(Choice(i), distances[i]);
                DrawButton(ContinueButton, "Cancel");
            }
            else
            {
                GUI.Label(new Rect(area.x + 18f, area.y + 58f, Width - 36f, 248f),
                    panel == PanelState.Filling ? "Keep the bucket at the tap.\n\nTime remaining: "
                        + filling.RemainingSeconds.ToString("0.0") + " s\n\nFire and smoke keep spreading."
                        : resultText, textStyle);
                DrawButton(ContinueButton, panel == PanelState.Filling ? "Cancel filling" : "Continue");
            }
        }

        private void DrawButton(Rect bounds, string label)
        {
            Color previous = GUI.backgroundColor;
            if (Mouse.current != null)
            {
                Vector2 pointer = Mouse.current.position.ReadValue();
                if (bounds.Contains(new Vector2(pointer.x, Screen.height - pointer.y)))
                    GUI.backgroundColor = new Color(0.65f, 0.9f, 1f);
            }
            GUI.Box(bounds, label, buttonStyle);
            GUI.backgroundColor = previous;
        }
    }
}
