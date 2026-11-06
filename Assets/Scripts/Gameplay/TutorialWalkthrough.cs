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
        private string answerFeedback = "";
        private Transform marker;
        private Material markerMaterial;
        private Mesh markerMesh;
        private GUIStyle titleStyle, bodyStyle, buttonStyle, hintStyle;
        private static readonly Vector3 WalkTarget = new Vector3(-4.75f, 0f, -1.75f);
        public static readonly Vector3 SmokeTarget = new Vector3(-3.25f, 0f, -1.75f);
        public bool BlocksGameplay => progress != null && !progress.IsComplete
            && (progress.NeedsBriefing || consumedFrame == Time.frameCount);
        public bool GrowFire => progress != null && progress.Step == TutorialStep.Escape && !progress.NeedsBriefing;
        public bool IsComplete => progress != null && progress.IsComplete;
        private float Scale => Mathf.Min(1f, Mathf.Min((Screen.width - 32f) / 680f, (Screen.height - 32f) / 560f));
        private Rect Card => new Rect((Screen.width - 680f * Scale) * 0.5f, (Screen.height - 560f * Scale) * 0.5f, 680f * Scale, 560f * Scale);
        private Rect Within(float x, float y, float width, float height) => new Rect(Card.x + x * Scale, Card.y + y * Scale, width * Scale, height * Scale);
        private Rect ContinueButton => Within(24, 484, 632, 48);
        private Rect Answer(int index) => Within(24, 330 + index * 52, 632, 44);
        private Rect Hint => new Rect(Screen.width - Mathf.Min(465f, Screen.width - 24f) - 12f,
            Screen.height - 144f, Mathf.Min(465f, Screen.width - 24f), 130f);
        private bool ShowObjective => progress != null && !progress.NeedsBriefing && !progress.IsComplete
            && !house.Showcase.HasFinished && !house.Equipment.BlocksGameplay && !interaction.BlocksMovementInput;

        public static void ResetEntry() { requestedStep = TutorialStep.Welcome; requestedScore = 0; }

        public void Initialize(HousePrototype layout, ClickToMovePlayer character)
        {
            house = layout; player = character; interaction = house.GetComponent<ObjectInteraction>();
            progress = new TutorialProgress(requestedStep); checkpointScore = requestedScore;
            ResetEntry();
            house.Equipment.SetTutorialScore(checkpointScore);
            PrepareCheckpoint();
            var target = new GameObject("Tutorial target"); target.transform.SetParent(house.transform, false);
            markerMesh = CreateRing();
            target.AddComponent<MeshFilter>().sharedMesh = markerMesh;
            markerMaterial = RuntimeMaterials.Flame(new Color(0.25f, 0.95f, 0.9f));
            target.AddComponent<MeshRenderer>().sharedMaterial = markerMaterial;
            target.GetComponent<Renderer>().shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            marker = target.transform; marker.gameObject.SetActive(false);
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
                            answerFeedback = points < 0 ? "Try again. Choose the action that gets you safely out of danger. (-5 points)" : "";
                            consumedFrame = Time.frameCount;
                            break;
                        }
                }
                else if (ContinueButton.Contains(pointer))
                {
                    progress.ContinueBriefing(); consumedFrame = Time.frameCount;
                    if (!progress.NeedsBriefing) BeginPractice();
                }
                if (progress.Step != before) Advanced();
                return;
            }
            ObservePractice();
            if (ShowObjective && Mouse.current != null && Mouse.current.leftButton.wasPressedThisFrame)
            {
                Vector2 pointer = Mouse.current.position.ReadValue();
                if (Hint.Contains(new Vector2(pointer.x, Screen.height - pointer.y))) consumedFrame = Time.frameCount;
            }
            if (!progress.NeedsBriefing)
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
            TutorialAction action = TutorialAction.None;
            switch (progress.Step)
            {
                case TutorialStep.Movement: if ((player.transform.position - WalkTarget).sqrMagnitude < 0.2f) action = TutorialAction.Moved; break;
                case TutorialStep.OpenWindow: if (house.Opening(HouseOpening.LivingRoomWindow).IsOpen) action = TutorialAction.WindowOpened; break;
                case TutorialStep.CloseWindow: if (!house.Opening(HouseOpening.LivingRoomWindow).IsOpen) action = TutorialAction.WindowClosed; break;
                case TutorialStep.OpenDoor: if (house.Opening(HouseOpening.KitchenDoor).IsOpen) action = TutorialAction.DoorOpened; break;
                case TutorialStep.CloseDoor: if (!house.Opening(HouseOpening.KitchenDoor).IsOpen) action = TutorialAction.DoorClosed; break;
                case TutorialStep.PickUpBucket: if (held != null && held.Kind == EquipmentKind.Bucket) action = TutorialAction.BucketPickedUp; break;
                case TutorialStep.FillBucket: if (held != null && held.HasWater) action = TutorialAction.BucketFilled; break;
                case TutorialStep.SwapForExtinguisher: if (held != null && held.Kind == EquipmentKind.AbcPowderExtinguisher) action = TutorialAction.SwappedForExtinguisher; break;
                case TutorialStep.RetrieveWater: if (held != null && held.Kind == EquipmentKind.Bucket && held.HasWater) action = TutorialAction.WaterRetrieved; break;
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
                if (action == TutorialAction.LeftSmoke) house.Showcase.ClearLessonHazards();
                Advanced();
            }
        }

        private void Advanced()
        {
            checkpointScore = house.Equipment.Score;
            answerFeedback = ""; consumedFrame = Time.frameCount;
            player.StopMovement(); marker.gameObject.SetActive(false);
        }

        public bool CanExtinguishWith(EquipmentItem item, out string message)
        {
            message = "";
            if (progress.Step != TutorialStep.ExtinguishA || item == null || item.Kind == EquipmentKind.Bucket) return true;
            message = "ABC and wet chemical can also suit class A. This drill practises water: fill your bucket and use it here.";
            return false;
        }

        public bool FireExtinguished(FireType type)
        {
            TutorialAction action = type == FireType.A ? TutorialAction.ExtinguishedA
                : type == FireType.B ? TutorialAction.ExtinguishedB : TutorialAction.ExtinguishedF;
            if (!progress.Observe(action)) return false;
            house.Showcase.ClearLessonHazards(); Advanced(); return true;
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
            }
            TutorialLesson lesson = TutorialLessons.Get(progress.Step);
            if (!progress.NeedsBriefing)
            {
                if (!ShowObjective) return;
                GUI.Box(Hint, GUIContent.none);
                GUI.Label(new Rect(Hint.x + 12, Hint.y + 9, Hint.width - 24, 27), lesson.Title, hintStyle);
                string objective = progress.Step == TutorialStep.SmokePractice && smokeDrill.HasCoughed
                    ? "Now walk back into clear air, away from the smoke patch." : lesson.Objective;
                GUI.Label(new Rect(Hint.x + 12, Hint.y + 42, Hint.width - 24, 78), objective, hintStyle);
                return;
            }
            titleStyle.fontSize = Mathf.Max(12, Mathf.RoundToInt(22 * Scale));
            bodyStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(16 * Scale));
            buttonStyle.fontSize = Mathf.Max(10, Mathf.RoundToInt(15 * Scale));
            Color old = GUI.color; GUI.color = new Color(0f, 0f, 0f, 0.7f);
            GUI.DrawTexture(new Rect(0, 0, Screen.width, Screen.height), Texture2D.whiteTexture); GUI.color = old;
            GUI.Box(Card, GUIContent.none, GUI.skin.window);
            GUI.Label(Within(24, 18, 632, 58), lesson.Title, titleStyle);
            GUI.Label(Within(24, 80, 632, 238), lesson.Body, bodyStyle);
            if (lesson.Kind == LessonKind.Question)
            {
                for (int i = 0; i < lesson.Answers.Length; i++) GUI.Box(Answer(i), lesson.Answers[i], buttonStyle);
                GUI.Label(Within(24, 492, 632, 50), answerFeedback, bodyStyle);
            }
            else GUI.Box(ContinueButton, lesson.Kind == LessonKind.Practice ? "Practice" : "Continue", buttonStyle);
            GUI.Label(Within(24, 540, 632, 18), "Lesson " + ((int)progress.Step + 1) + " / " + (int)TutorialStep.Complete, bodyStyle);
        }

        private static Mesh CreateRing()
        {
            const int segments = 40;
            var vertices = new Vector3[segments * 2]; var triangles = new int[segments * 6];
            for (int i = 0; i < segments; i++)
            {
                float angle = i * Mathf.PI * 2f / segments;
                Vector3 direction = new Vector3(Mathf.Cos(angle), 0f, Mathf.Sin(angle));
                vertices[i * 2] = direction * 0.32f; vertices[i * 2 + 1] = direction * 0.48f;
                int a = i * 2, b = a + 1, c = ((i + 1) % segments) * 2, d = c + 1;
                int t = i * 6;
                triangles[t] = a; triangles[t + 1] = c; triangles[t + 2] = b;
                triangles[t + 3] = b; triangles[t + 4] = c; triangles[t + 5] = d;
            }
            var mesh = new Mesh { name = "Tutorial target ring" }; mesh.vertices = vertices; mesh.triangles = triangles; mesh.RecalculateBounds();
            return mesh;
        }

        private void OnDestroy()
        {
            if (markerMaterial != null) Destroy(markerMaterial);
            if (markerMesh != null) Destroy(markerMesh);
        }
    }
}
