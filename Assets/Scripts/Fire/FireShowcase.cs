using FireSafety.Movement;
using FireSafety.Smoke;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;

namespace FireSafety.Fire
{
    public sealed class FireSite
    {
        public readonly Vector3 Position;
        public readonly Transform Prop;
        public readonly Collider Support;
        public FireSite(Vector3 position, Transform prop, Collider support)
        { Position = position; Prop = prop; Support = support; }
    }

    [DefaultExecutionOrder(-200)]
    public sealed class FireShowcase : MonoBehaviour
    {
        private HousePrototype house;
        private ClickToMovePlayer player;
        private FireHazard fire;
        private SmokeHazard smoke;
        private bool diedFromSmoke;
        private bool choosing = true, choosingLocation, caught;
        private int consumedFrame = -1;
        private float tick;
        private GUIStyle titleStyle, textStyle, buttonStyle;
        public bool BlocksGameplay => choosing || caught || consumedFrame == Time.frameCount;
        private Rect Panel => new Rect((Screen.width - PanelWidth) * 0.5f, (Screen.height - 310f) * 0.5f, PanelWidth, 310f);
        private float PanelWidth => Mathf.Min(430f, Screen.width - 24f);
        private Rect Button(int index) => new Rect(Panel.x + 20f, Panel.y + 92f + index * 48f, Panel.width - 40f, 38f);

        public void Initialize(HousePrototype layout, ClickToMovePlayer character)
        { house = layout; player = character; consumedFrame = Time.frameCount; }

        private void Update()
        {
            if (house == null) return;
            if (choosing || caught)
            {
                player.StopMovement();
                if (consumedFrame == Time.frameCount) return;
                if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
                Vector2 pointer = Mouse.current.position.ReadValue();
                pointer.y = Screen.height - pointer.y;
                if (caught)
                {
                    if (Button(0).Contains(pointer)) SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    return;
                }
                if (choosingLocation)
                {
                    if (Button(0).Contains(pointer)) StartFire(FireType.A, FireLocation.Table);
                    else if (Button(1).Contains(pointer)) StartFire(FireType.A, FireLocation.Bed);
                    else if (Button(2).Contains(pointer)) choosingLocation = false;
                    return;
                }
                if (Button(0).Contains(pointer)) choosingLocation = true;
                else if (Button(1).Contains(pointer)) StartFire(FireType.B, FireLocation.PaintBottle);
                else if (Button(2).Contains(pointer)) StartFire(FireType.C, FireLocation.Hob);
                else if (Button(3).Contains(pointer)) StartFire(FireType.F, FireLocation.DeepFryer);
                return;
            }
            if (fire == null) return;
            tick += Time.deltaTime;
            if (tick >= 0.2f) { fire.Tick(tick); tick = 0f; }
            smoke.Tick(Time.deltaTime);
            bool fireCaught = house.IsFireDanger(player.transform.position);
            if (!fireCaught && !smoke.Exposure.IsDead) return;
            diedFromSmoke = !fireCaught;
            caught = true;
            player.StopMovement();
            smoke.StopCoughing();
        }

        private void StartFire(FireType type, FireLocation location)
        {
            var root = new GameObject("Class " + type + " fire");
            root.transform.SetParent(house.transform, false);
            fire = root.AddComponent<FireHazard>();
            house.AttachFire(fire);
            fire.Initialize(house, type, location, house.GetFireSite(location));
            var smokeRoot = new GameObject("Smoke and coughing");
            smokeRoot.transform.SetParent(house.transform, false);
            smoke = smokeRoot.AddComponent<SmokeHazard>();
            smoke.Initialize(house, fire, player.transform);
            choosing = false;
            consumedFrame = Time.frameCount;
        }

        private void OnGUI()
        {
            if (!choosing && !caught) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 20, fontStyle = FontStyle.Bold };
                textStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15 };
            }
            Color previous = GUI.color;
            GUI.color = new Color(0f, 0f, 0f, 0.55f);
            GUI.DrawTexture(new Rect(0f, 0f, Screen.width, Screen.height), Texture2D.whiteTexture);
            GUI.color = previous;
            Rect panel = Panel;
            GUI.Box(panel, GUIContent.none, GUI.skin.window);
            GUI.Label(new Rect(panel.x + 20, panel.y + 16, panel.width - 40, 30),
                caught ? (diedFromSmoke ? "Overcome by smoke" : "Caught by the fire")
                    : choosingLocation ? "Where should class A start?" : "Which fire class would you like?", titleStyle);
            GUI.Label(new Rect(panel.x + 20, panel.y + 52, panel.width - 40, 36),
                caught ? (diedFromSmoke ? "You stayed in the smoke too long. Restart to try again."
                    : "The fire reached you. Restart to try another scenario.")
                    : "Each scenario begins with a small, growing fire.", textStyle);
            if (caught) { DrawButton(0, "Restart demo"); return; }
            if (choosingLocation)
            { DrawButton(0, "Table"); DrawButton(1, "Bed"); DrawButton(2, "Back"); return; }
            DrawButton(0, "A - Table or bed");
            DrawButton(1, "B - Paint bottle");
            DrawButton(2, "C - Gas hob");
            DrawButton(3, "F - Deep fryer");
        }

        private void DrawButton(int index, string label) => GUI.Box(Button(index), label, buttonStyle);
    }
}
