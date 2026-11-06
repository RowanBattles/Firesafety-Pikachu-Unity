using FireSafety.Movement;
using FireSafety.Smoke;
using FireSafety.Gameplay;
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
        private bool choosing = true, choosingLocation;
        private readonly ScenarioResult result = new ScenarioResult();
        private int consumedFrame = -1;
        private float nextOpportunityCheck;
        private GUIStyle titleStyle, textStyle, buttonStyle;
        public bool BlocksGameplay => choosing || result.IsFinished || consumedFrame == Time.frameCount;
        private Rect Panel => new Rect((Screen.width - PanelWidth) * 0.5f, (Screen.height - 310f) * 0.5f, PanelWidth, 310f);
        private float PanelWidth => Mathf.Min(430f, Screen.width - 24f);
        private Rect Button(int index) => new Rect(Panel.x + 20f, Panel.y + 92f + index * 48f, Panel.width - 40f, 38f);
        private float ResultsWidth => Mathf.Min(510f, Screen.width - 24f);
        private Rect ResultsPanel => new Rect((Screen.width - ResultsWidth) * 0.5f, (Screen.height - 390f) * 0.5f, ResultsWidth, 390f);
        private Rect PlayAgainButton => new Rect(ResultsPanel.x + 20f, ResultsPanel.y + 332f, ResultsWidth - 40f, 38f);

        public void Initialize(HousePrototype layout, ClickToMovePlayer character)
        { house = layout; player = character; consumedFrame = Time.frameCount; }

        private void Update()
        {
            if (house == null) return;
            if (choosing || result.IsFinished)
            {
                player.StopMovement();
                if (consumedFrame == Time.frameCount) return;
                if (Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
                Vector2 pointer = Mouse.current.position.ReadValue();
                pointer.y = Screen.height - pointer.y;
                if (result.IsFinished)
                {
                    if (PlayAgainButton.Contains(pointer))
                    {
                        consumedFrame = Time.frameCount;
                        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
                    }
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
            if (fire.Simulation.IsExtinguished) { CompleteExtinguishing(""); return; }
            fire.Tick(Time.deltaTime);
            smoke.Tick(Time.deltaTime);
            if (house.IsFireDanger(player.transform.position)) { Finish(ScenarioEnding.FireDeath); return; }
            if (smoke.Exposure.IsDead) { Finish(ScenarioEnding.SmokeDeath); return; }
            if (house.HasEscaped(player.transform.position)) { Finish(ScenarioEnding.Escaped); return; }
            if (!result.HadSafeExtinguishingOpportunity && Time.time >= nextOpportunityCheck)
            {
                result.ObserveExtinguishingOpportunity(house.Equipment.HasSafeExtinguishingOpportunity(fire, smoke));
                nextOpportunityCheck = Time.time + 0.5f;
            }
        }

        public void CompleteExtinguishing(string feedback)
        {
            if (fire != null && fire.Simulation.IsExtinguished) Finish(ScenarioEnding.Extinguished, feedback);
        }

        private void Finish(ScenarioEnding ending, string feedback = "")
        {
            if (!result.Finish(ending, house.Equipment.Score, feedback)) return;
            consumedFrame = Time.frameCount;
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
            if (!choosing && !result.IsFinished) return;
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
            if (result.IsFinished) { DrawResults(); return; }
            Rect panel = Panel;
            GUI.Box(panel, GUIContent.none, GUI.skin.window);
            GUI.Label(new Rect(panel.x + 20, panel.y + 16, panel.width - 40, 30),
                choosingLocation ? "Where should class A start?" : "Which fire class would you like?", titleStyle);
            GUI.Label(new Rect(panel.x + 20, panel.y + 52, panel.width - 40, 36),
                "Each scenario begins with a small, growing fire.", textStyle);
            if (choosingLocation)
            { DrawButton(0, "Table"); DrawButton(1, "Bed"); DrawButton(2, "Back"); return; }
            DrawButton(0, "A - Table or bed");
            DrawButton(1, "B - Paint bottle");
            DrawButton(2, "C - Gas hob");
            DrawButton(3, "F - Deep fryer");
        }

        private void DrawResults()
        {
            Rect panel = ResultsPanel;
            GUI.Box(panel, GUIContent.none, GUI.skin.window);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 16f, panel.width - 40f, 32f), result.Title, titleStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 57f, panel.width - 40f, 132f), result.Feedback, textStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 200f, panel.width - 40f, 34f),
                "Final score: " + result.TotalPoints + " points", titleStyle);
            string breakdown = "Decision points: " + result.DecisionPoints;
            if (result.Ending == ScenarioEnding.Escaped)
            {
                breakdown += "\nEscape bonus: +" + result.EscapePoints;
                if (result.MissedOpportunityPoints != 0) breakdown += "\nMissed opportunity: " + result.MissedOpportunityPoints;
            }
            GUI.Label(new Rect(panel.x + 20f, panel.y + 244f, panel.width - 40f, 76f), breakdown, textStyle);
            GUI.Box(PlayAgainButton, "Play again", buttonStyle);
        }

        private void DrawButton(int index, string label) => GUI.Box(Button(index), label, buttonStyle);
    }
}
