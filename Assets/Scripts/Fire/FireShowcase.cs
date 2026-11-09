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
        public bool HasFinished => result.IsFinished;
        public FireHazard ActiveFire => fire;
        public SmokeHazard Smoke => smoke;
        public bool BlocksGameplay => choosing || result.IsFinished || consumedFrame == Time.frameCount;
        private Rect Panel => new Rect((Screen.width - PanelWidth) * 0.5f, (Screen.height - 310f) * 0.5f, PanelWidth, 310f);
        private float PanelWidth => Mathf.Min(430f, Screen.width - 24f);
        private Rect Button(int index) => new Rect(Panel.x + 20f, Panel.y + 92f + index * 48f, Panel.width - 40f, 38f);
        private float ResultsWidth => Mathf.Min(510f, Screen.width - 24f);
        private float ResultsHeight => house != null && house.Mode == ScenarioMode.Tutorial ? 270f : 390f;
        private Rect ResultsPanel => new Rect((Screen.width - ResultsWidth) * 0.5f, (Screen.height - ResultsHeight) * 0.5f, ResultsWidth, ResultsHeight);
        private Rect PlayAgainButton => new Rect(ResultsPanel.x + 20f, ResultsPanel.yMax - 58f, ResultsWidth - 40f, 38f);

        public void Initialize(HousePrototype layout, ClickToMovePlayer character)
        {
            house = layout; player = character; consumedFrame = Time.frameCount;
            choosing = house.Mode == ScenarioMode.Dev;
            if (house.Mode == ScenarioMode.Game) StartFire(house.Setup.Fire.Type, house.Setup.Fire.Location);
        }

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
                        if (house.Tutorial != null && !house.Tutorial.IsComplete) house.Tutorial.RetryLesson();
                        else SceneNavigation.Load(SceneManager.GetActiveScene().name);
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
            if (house.Mode == ScenarioMode.Tutorial && (house.Tutorial == null || house.Tutorial.BlocksGameplay)) return;
            if (fire == null && smoke == null) return;
            if (fire != null)
            {
                if (fire.Simulation.IsExtinguished) { house.CompleteExtinguishing(""); return; }
                if (house.Tutorial == null || house.Tutorial.GrowFire) fire.Tick(Time.deltaTime);
            }
            if (smoke != null && (house.Tutorial == null || house.Tutorial.GrowFire || fire == null)) smoke.Tick(Time.deltaTime);
            if (house.IsFireDanger(player.transform.position)) { Finish(ScenarioEnding.FireDeath); return; }
            if (smoke != null && smoke.Exposure.IsDead) { Finish(ScenarioEnding.SmokeDeath); return; }
            if (house.HasEscaped(player.transform.position))
            {
                if (house.Tutorial != null) house.Tutorial.Escaped();
                Finish(ScenarioEnding.Escaped); return;
            }
            if (house.Mode != ScenarioMode.Tutorial && fire != null && !result.HadSafeExtinguishingOpportunity && Time.time >= nextOpportunityCheck)
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
            if (smoke != null) smoke.StopCoughing();
        }

        public void StartLessonFire(FireType type, FireLocation location)
        {
            ClearLessonHazards(); StartFire(type, location);
        }

        public void StartSmokeLesson(Vector3 source)
        {
            ClearLessonHazards();
            var root = new GameObject("Controlled tutorial smoke"); root.transform.SetParent(house.transform, false);
            smoke = root.AddComponent<SmokeHazard>(); smoke.InitializeTraining(house, player.transform, source);
            consumedFrame = Time.frameCount;
        }

        public void ClearLessonHazards()
        {
            if (smoke != null) { smoke.StopCoughing(); Destroy(smoke.gameObject); smoke = null; }
            if (fire != null) { Destroy(fire.gameObject); fire = null; }
            house.AttachFire(null); house.RefreshFireNavigation();
        }

        private void StartFire(FireType type, FireLocation location)
        {
            var root = new GameObject(ScenarioPresentation.FireTitle(house.Mode, type));
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
            bool tutorialComplete = house.Tutorial != null && house.Tutorial.IsComplete;
            string title = tutorialComplete ? "Tutorial complete!" : result.Title;
            string feedback = tutorialComplete ? "Well done! You practised movement, openings, equipment, A/B/F firefighting, gas safety, smoke and escape.\n\nChoose Game from the menu for a random scenario."
                : result.Feedback;
            if (house.Tutorial != null && !tutorialComplete) feedback += "\n\nRetry this lesson from its checkpoint, or return to Menu.";
            GUI.Label(new Rect(panel.x + 20f, panel.y + 16f, panel.width - 40f, 32f), title, titleStyle);
            GUI.Label(new Rect(panel.x + 20f, panel.y + 57f, panel.width - 40f, 132f), feedback, textStyle);
            if (ScenarioPresentation.ShowFinalScore(house.Mode))
            {
                GUI.Label(new Rect(panel.x + 20f, panel.y + 200f, panel.width - 40f, 34f),
                    "Final score: " + result.TotalPoints + " points", titleStyle);
                string breakdown = "Decision points: " + result.DecisionPoints;
                if (result.Ending == ScenarioEnding.Escaped)
                {
                    breakdown += "\nEscape bonus: +" + result.EscapePoints;
                    if (result.MissedOpportunityPoints != 0) breakdown += "\nMissed opportunity: " + result.MissedOpportunityPoints;
                }
                if (result.DeathPoints != 0) breakdown += "\nDeath: " + result.DeathPoints;
                GUI.Label(new Rect(panel.x + 20f, panel.y + 244f, panel.width - 40f, 76f), breakdown, textStyle);
            }
            GUI.Box(PlayAgainButton, house.Tutorial != null ? (tutorialComplete ? "Play tutorial again" : "Retry lesson") : "Play again", buttonStyle);
        }

        private void DrawButton(int index, string label) => GUI.Box(Button(index), label, buttonStyle);
    }
}
