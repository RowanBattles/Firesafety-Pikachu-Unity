using FireSafety.Equipment;
using FireSafety.Fire;
using FireSafety.Interaction;
using FireSafety.Movement;
using FireSafety.Rendering;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FireSafety.Gameplay
{
    [DefaultExecutionOrder(-250)]
    public sealed class TutorialWalkthrough : MonoBehaviour
    {
        private static TutorialStep requestedStep = TutorialStep.Welcome;
        private static int requestedScore;
        private HousePrototype house;
        private ClickToMovePlayer player;
        private ObjectInteraction interaction;
        private TutorialProgress progress;
        private int checkpointScore, consumedFrame = -1;
        private SmokeDrillProgress smokeDrill = new SmokeDrillProgress();
        private TutorialFeedback decision;
        private Transform marker;
        private GUIStyle titleStyle, bodyStyle, buttonStyle, hintStyle, hintTitleStyle;
        private static readonly Vector3 WalkTarget = new Vector3(-4.75f, 0f, -1.75f);
        public static readonly Vector3 SmokeTarget = new Vector3(-3.25f, 0f, -1.75f);
        public bool BlocksGameplay => progress != null && !progress.IsComplete
            && (decision != null || progress.NeedsBriefing || consumedFrame == Time.frameCount);
        public bool GrowFire => progress != null && progress.Step == TutorialStep.Escape && !progress.NeedsBriefing;
        public bool IsComplete => progress != null && progress.IsComplete;
        private float CardWidth => decision == null ? 680f : decision.Correct ? 480f : 520f;
        private float CardHeight => decision == null ? 560f : decision.Correct ? 220f : 340f;
        private float Scale => Mathf.Min(1f, Mathf.Min((Screen.width - 32f) / CardWidth, (Screen.height - 32f) / CardHeight));
        private Rect Card => new Rect((Screen.width - CardWidth * Scale) * 0.5f,
            (Screen.height - CardHeight * Scale) * 0.5f, CardWidth * Scale, CardHeight * Scale);
        private Rect Within(float x, float y, float width, float height) => new Rect(Card.x + x * Scale, Card.y + y * Scale, width * Scale, height * Scale);
        private Rect ContinueButton => decision == null ? Within(24, 484, 632, 48)
            : Within(20, CardHeight - 62, CardWidth - 40, 42);
        private Rect Answer(int index) => Within(24, 330 + index * 52, 632, 44);
        private Rect Hint => new Rect(Screen.width - Mathf.Min(300f, Screen.width - 24f) - 12f,
            Screen.height - 100f, Mathf.Min(300f, Screen.width - 24f), 88f);
        private bool ShowObjective => progress != null && decision == null && !progress.NeedsBriefing && !progress.IsComplete
            && !house.Showcase.HasFinished && !house.Equipment.BlocksGameplay && !interaction.BlocksMovementInput;

        public static void ResetEntry() { requestedStep = TutorialStep.Welcome; requestedScore = 0; }

        public void Initialize(HousePrototype layout, ClickToMovePlayer character)
        {
            house = layout; player = character; interaction = house.GetComponent<ObjectInteraction>();
            progress = new TutorialProgress(requestedStep); checkpointScore = requestedScore;
            ResetEntry();
            house.Equipment.SetTutorialScore(checkpointScore);
            PrepareCheckpoint();
            marker = WorldRing.Create(house.transform, "Tutorial target", new Color(0.25f, 0.95f, 0.9f), false);
            consumedFrame = Time.frameCount;
        }

        public void RetryLesson()
        {
            requestedStep = progress.Step; requestedScore = checkpointScore;
            SceneNavigation.Load(SceneCatalog.Tutorial, false);
        }

        private PickupEquipment Item(EquipmentKind kind)
        {
            foreach (PickupEquipment item in house.Equipment.AvailableItems) if (item.Item.Kind == kind) return item;
            return null;
        }

        private void PrepareCheckpoint()
        {
            TutorialStep step = progress.Step;
            if (step == TutorialStep.CloseWindow) SetOpen(HouseOpening.LivingRoomWindow, true);
            if (step == TutorialStep.CloseDoor || step >= TutorialStep.FillBucket) SetOpen(HouseOpening.KitchenDoor, true);
            if (step == TutorialStep.FillBucket) house.Equipment.PickUp(Item(EquipmentKind.Bucket));
            if (step == TutorialStep.SwapForExtinguisher || step == TutorialStep.RetrieveWater
                || step == TutorialStep.SmallFireSafety || step == TutorialStep.ExtinguishA)
            {
                PickupEquipment bucket = Item(EquipmentKind.Bucket);
                bucket.Item.Fill(); bucket.RefreshContents(); house.Equipment.PickUp(bucket);
                if (step == TutorialStep.RetrieveWater) house.Equipment.PickUp(Item(EquipmentKind.AbcPowderExtinguisher));
            }
            if (step == TutorialStep.ExtinguishB) house.Equipment.PickUp(Item(EquipmentKind.AbcPowderExtinguisher));
            if (step == TutorialStep.ExtinguishF) house.Equipment.PickUp(Item(EquipmentKind.WetChemicalExtinguisher));
        }

        private void SetOpen(HouseOpening id, bool open)
        {
            OpenableObject opening = house.Opening(id);
            if (opening.IsOpen != open) opening.TryToggle(bounds => true, out _);
        }

        private void Update()
        {
            if (progress == null || house.Showcase.HasFinished || progress.IsComplete) { if (marker != null) marker.gameObject.SetActive(false); return; }
            if (decision != null)
            {
                marker.gameObject.SetActive(false);
                if (Time.frameCount == consumedFrame || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
                Vector2 pointer = Mouse.current.position.ReadValue(); pointer.y = Screen.height - pointer.y;
                if (ContinueButton.Contains(pointer)) { decision = null; consumedFrame = Time.frameCount; }
                return;
            }
            if (progress.NeedsBriefing)
            {
                marker.gameObject.SetActive(false);
                if (Time.frameCount == consumedFrame || Mouse.current == null || !Mouse.current.leftButton.wasPressedThisFrame) return;
                Vector2 pointer = Mouse.current.position.ReadValue(); pointer.y = Screen.height - pointer.y;
                TutorialLesson lesson = TutorialLessons.Get(progress.Step);
                TutorialStep before = progress.Step;
                if (lesson.Kind == LessonKind.Question)
                {
                    for (int i = 0; i < lesson.Answers.Length; i++)
                        if (Answer(i).Contains(pointer))
                        {
                            int points = progress.Answer(i);
                            house.Equipment.SetTutorialScore(house.Equipment.Score + points);
                            if (progress.Step != before) Advanced();
                            ShowDecision(TutorialFeedback.Question(before, i));
                            consumedFrame = Time.frameCount;
                            break;
                        }
                }
                else if (ContinueButton.Contains(pointer))
                {
                    progress.ContinueBriefing(); consumedFrame = Time.frameCount;
                    if (!progress.NeedsBriefing) BeginPractice();
                }
                if (decision == null && progress.Step != before) Advanced();
                return;
            }
            ObservePractice();
            // The passive objective is click-through; floor input beneath it remains usable.
            if (decision == null && !progress.NeedsBriefing)
            {
                Vector3 target = Target();
                marker.position = new Vector3(target.x, Mathf.Max(1.55f, target.y + 0.05f), target.z);
                marker.gameObject.SetActive(true);
            }
        }

        private void BeginPractice()
        {
            switch (progress.Step)
            {
                case TutorialStep.ExtinguishA: house.Showcase.StartLessonFire(FireType.A, FireLocation.Table); break;
                case TutorialStep.ExtinguishB: house.Showcase.StartLessonFire(FireType.B, FireLocation.PaintBottle); break;
                case TutorialStep.ExtinguishF: house.Showcase.StartLessonFire(FireType.F, FireLocation.DeepFryer); break;
                case TutorialStep.SmokePractice: smokeDrill = new SmokeDrillProgress(); house.Showcase.StartSmokeLesson(SmokeTarget); break;
                case TutorialStep.Escape: SetOpen(HouseOpening.FrontDoor, false); house.Showcase.StartLessonFire(FireType.C, FireLocation.Hob); break;
            }
        }

        private void ObservePractice()
        {
            EquipmentItem held = house.Equipment.Inventory.Held;
            TutorialStep completed = progress.Step;
            if (progress.ObserveEquipment(held)) { PracticeCompleted(completed, held); return; }
            TutorialAction action = TutorialAction.None;
            switch (progress.Step)
            {
                case TutorialStep.Movement: if ((player.transform.position - WalkTarget).sqrMagnitude < 0.2f) action = TutorialAction.Moved; break;
                case TutorialStep.OpenWindow: if (house.Opening(HouseOpening.LivingRoomWindow).IsOpen) action = TutorialAction.WindowOpened; break;
                case TutorialStep.CloseWindow: if (!house.Opening(HouseOpening.LivingRoomWindow).IsOpen) action = TutorialAction.WindowClosed; break;
                case TutorialStep.OpenDoor: if (house.Opening(HouseOpening.KitchenDoor).IsOpen) action = TutorialAction.DoorOpened; break;
                case TutorialStep.CloseDoor: if (!house.Opening(HouseOpening.KitchenDoor).IsOpen) action = TutorialAction.DoorClosed; break;
                case TutorialStep.SmokePractice:
                    if (house.Showcase.Smoke != null)
                    {
                        var exposure = house.Showcase.Smoke.Exposure;
                        if (smokeDrill.Observe(exposure.ShouldCough, exposure.CurrentDensity, exposure.IsDead)) action = TutorialAction.LeftSmoke;
                    }
                    break;
            }
            if (progress.Observe(action))
            {
                PracticeCompleted(completed, held);
            }
        }

        private void PracticeCompleted(TutorialStep completed, EquipmentItem held)
        {
            if (completed == TutorialStep.SmokePractice) house.Showcase.ClearLessonHazards();
            Advanced();
            ShowDecision(TutorialFeedback.Practice(completed, held == null ? (EquipmentKind?)null : held.Kind));
        }

        public void ShowDecision(TutorialFeedback feedback)
        {
            if (progress.IsComplete) return;
            decision = feedback; consumedFrame = Time.frameCount;
            player.StopMovement(); marker.gameObject.SetActive(false);
        }

        public void InteractionDecision(WorldInteractable target, string choice, bool success, string failure)
        {
            if (progress.NeedsBriefing || decision != null || progress.IsComplete) return;
            bool expected = true;
            var opening = target as OpenableObject;
            if (progress.Step == TutorialStep.OpenWindow || progress.Step == TutorialStep.CloseWindow)
                expected = opening != null && opening.Id == HouseOpening.LivingRoomWindow
                    && choice == (progress.Step == TutorialStep.OpenWindow ? "Open" : "Close");
            else if (progress.Step == TutorialStep.OpenDoor || progress.Step == TutorialStep.CloseDoor)
                expected = opening != null && opening.Id == HouseOpening.KitchenDoor
                    && choice == (progress.Step == TutorialStep.OpenDoor ? "Open" : "Close");
            else if (target is PickupEquipment pickup)
            {
                if (progress.Step == TutorialStep.PickUpBucket || progress.Step == TutorialStep.RetrieveWater || progress.Step == TutorialStep.ExtinguishA)
                    expected = pickup.Item.Kind == EquipmentKind.Bucket;
                if (progress.Step == TutorialStep.ExtinguishB) expected = pickup.Item.Kind == EquipmentKind.AbcPowderExtinguisher;
                if (progress.Step == TutorialStep.ExtinguishF) expected = pickup.Item.Kind == EquipmentKind.WetChemicalExtinguisher;
            }
            if (!success || !expected)
                ShowDecision(new TutorialFeedback(false, "Your choice: " + choice + " " + target.DisplayName
                    + ".\n\nCorrect action: " + TutorialLessons.Get(progress.Step).Objective
                    + (string.IsNullOrEmpty(failure) ? "\n\nThis action does not complete the current exercise. You can try again." : "\n\n" + failure)));
        }

        private void Advanced()
        {
            checkpointScore = house.Equipment.Score;
            consumedFrame = Time.frameCount;
            player.StopMovement(); marker.gameObject.SetActive(false);
        }

        public bool CanExtinguishWith(EquipmentItem item, out string message)
        {
            message = "";
            if (progress.Step != TutorialStep.ExtinguishA || item == null || item.Kind == EquipmentKind.Bucket) return true;
            message = "ABC and wet chemical can also suit class A. This drill practises water: fill your bucket and use it here.";
            ShowDecision(new TutorialFeedback(false, "Correct action for this drill: use a full bucket.\n\n" + message));
            return false;
        }

        public bool FireExtinguished(FireType type)
        {
            TutorialStep completed = progress.Step;
            TutorialAction action = type == FireType.A ? TutorialAction.ExtinguishedA
                : type == FireType.B ? TutorialAction.ExtinguishedB : TutorialAction.ExtinguishedF;
            if (!progress.Observe(action)) return false;
            house.Showcase.ClearLessonHazards(); Advanced();
            ShowDecision(TutorialFeedback.Practice(completed)); return true;
        }

        public void Escaped()
        {
            if (progress.Observe(TutorialAction.Escaped)) Advanced();
        }

        private Vector3 Target()
        {
            TutorialStep step = progress.Step;
            if (step == TutorialStep.Movement) return WalkTarget;
            if (step == TutorialStep.OpenWindow || step == TutorialStep.CloseWindow) return house.Opening(HouseOpening.LivingRoomWindow).InteractionBounds.center;
            if (step == TutorialStep.OpenDoor || step == TutorialStep.CloseDoor) return house.Opening(HouseOpening.KitchenDoor).InteractionBounds.center;
            if (step == TutorialStep.SmokePractice) return smokeDrill.HasCoughed ? new Vector3(-4.75f, 0f, -3.75f) : SmokeTarget;
            if (step == TutorialStep.Escape) return house.FrontDoor.IsOpen ? new Vector3(-5f, 0f, -9.25f) : house.FrontDoor.InteractionBounds.center;
            EquipmentKind needed = step == TutorialStep.ExtinguishF ? EquipmentKind.WetChemicalExtinguisher
                : step == TutorialStep.SwapForExtinguisher || step == TutorialStep.ExtinguishB
                    ? EquipmentKind.AbcPowderExtinguisher : EquipmentKind.Bucket;
            if (house.Equipment.Inventory.Held != null && house.Equipment.Inventory.Held.Kind == needed)
            {
                if (step == TutorialStep.FillBucket) return house.GetComponentInChildren<WaterTap>().transform.position;
                if (house.Showcase.ActiveFire != null) return house.Showcase.ActiveFire.SourcePosition;
            }
            PickupEquipment item = Item(needed);
            if (!house.IsVisiblePoint(item.transform.position) && !house.Opening(HouseOpening.KitchenDoor).IsOpen)
                return house.Opening(HouseOpening.KitchenDoor).InteractionBounds.center;
            return item.transform.position;
        }

        private void OnGUI()
        {
            if (progress == null || progress.IsComplete || house.Showcase.HasFinished) return;
            if (titleStyle == null)
            {
                titleStyle = new GUIStyle(GUI.skin.label) { fontSize = 22, fontStyle = FontStyle.Bold, wordWrap = true };
                bodyStyle = new GUIStyle(GUI.skin.label) { fontSize = 16, wordWrap = true };
                buttonStyle = new GUIStyle(GUI.skin.button) { fontSize = 15, wordWrap = true };
                hintStyle = new GUIStyle(GUI.skin.label) { fontSize = 14, wordWrap = true };
                hintTitleStyle = new GUIStyle(GUI.skin.label) { fontSize = 13, fontStyle = FontStyle.Bold };
            }
            TutorialLesson lesson = TutorialLessons.Get(progress.Step);
            if (decision == null && !progress.NeedsBriefing)
            {
                if (!ShowObjective) return;
                GUI.Box(Hint, GUIContent.none);
                GUI.Label(new Rect(Hint.x + 10, Hint.y + 6, Hint.width - 20, 20), lesson.Title, hintTitleStyle);
                string objective = progress.Step == TutorialStep.SmokePractice && smokeDrill.HasCoughed
                    ? "Now walk back into clear air, away from the smoke patch." : lesson.Objective;
                GUI.Label(new Rect(Hint.x + 10, Hint.y + 28, Hint.width - 20, 54), objective, hintStyle);
                return;
            }
            titleStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(22 * Scale));
            bodyStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(16 * Scale));
            buttonStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(15 * Scale));
            Color old = GUI.color; GUI.color = new Color(0f, 0f, 0f, decision == null ? 0.7f : 0.28f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = old;
            GUI.Box(Card, GUIContent.none, GUI.skin.window);
            if (decision != null)
            {
                GUI.Label(Within(20, 16, CardWidth - 40, 34), decision.Title, titleStyle);
                GUI.Label(Within(20, 60, CardWidth - 40, CardHeight - 132), decision.Text, bodyStyle);
                GUI.Box(ContinueButton, "Continue", buttonStyle);
                return;
            }
            GUI.Label(Within(24, 18, 632, 58), lesson.Title, titleStyle);
            GUI.Label(Within(24, 80, 632, 238), lesson.Body, bodyStyle);
            if (lesson.Kind == LessonKind.Question)
            {
                for (int i = 0; i < lesson.Answers.Length; i++) GUI.Box(Answer(i), lesson.Answers[i], buttonStyle);
            }
            else GUI.Box(ContinueButton, lesson.Kind == LessonKind.Practice ? "Practice" : "Continue", buttonStyle);
            GUI.Label(Within(24, 540, 632, 18), "Lesson " + ((int)progress.Step + 1) + " / " + (int)TutorialStep.Complete, bodyStyle);
        }
    }
}
