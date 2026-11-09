using System;
using FireSafety.Equipment;

namespace FireSafety.Gameplay
{
    public enum TutorialStep
    {
        Welcome, Movement, FireClasses, OpenWindow, CloseWindow, OpenDoor, CloseDoor,
        PickUpBucket, FillBucket, SwapForExtinguisher, RetrieveWater, SmallFireSafety,
        ExtinguishA, ExtinguishB, ExtinguishF, GasSafety, SmokeSafety, SmokePractice, Escape, Complete
    }
    public enum TutorialAction
    {
        None, Moved, WindowOpened, WindowClosed, DoorOpened, DoorClosed, BucketPickedUp,
        BucketFilled, SwappedForExtinguisher, WaterRetrieved, ExtinguishedA, ExtinguishedB, ExtinguishedF, LeftSmoke, Escaped
    }
    public enum LessonKind { Information, Practice, Question }

    public sealed class TutorialLesson
    {
        public readonly string Title, Body, Objective;
        public readonly LessonKind Kind;
        public readonly TutorialAction Action;
        public readonly string[] Answers;
        public readonly int CorrectAnswer;
        public TutorialLesson(string title, string body, string objective = "", TutorialAction action = TutorialAction.None,
            string[] answers = null, int correctAnswer = -1)
        {
            Title = title; Body = body; Objective = objective; Action = action;
            Answers = answers; CorrectAnswer = correctAnswer;
            Kind = answers != null ? LessonKind.Question : action == TutorialAction.None ? LessonKind.Information : LessonKind.Practice;
        }
    }

    public static class TutorialLessons
    {
        public static TutorialLesson Get(TutorialStep step)
        {
            switch (step)
            {
                case TutorialStep.Welcome: return new TutorialLesson("Welcome to Fire Safety",
                    "Learn by doing: move through the house, manage doors and windows, carry equipment, put out small fires and escape.\n\nThe house layout stays the same in every mode. Practice fires stay small while you learn. Touching fire is still fatal. You can retry a lesson if you make a mistake.");
                case TutorialStep.Movement: return new TutorialLesson("Click to move",
                    "Left-click a visible floor tile. Your character walks around obstacles and stays in the centre of the camera.\n\nObjects are used the same way: click one, walk to it, then choose an action.",
                    "Click the cyan marker on the living-room floor and walk to it.", TutorialAction.Moved);
                case TutorialStep.FireClasses: return new TutorialLesson("Know what is burning",
                    "A: solid materials, such as wood, cloth and a bed.\nB: flammable liquids, such as paint.\nC: flammable gas, such as a leaking gas hob.\nF: cooking oil and fat in a fryer.\n\nDifferent fuels need different agents. Class D means burning metal and needs metal-specific powder; it is outside this game's scenarios.");
                case TutorialStep.OpenWindow: return new TutorialLesson("Windows and airflow",
                    "Click the west living-room window and walk to its frame. Choose Open.\n\nOpen windows feed ventilation and make a fire grow faster in connected rooms. Walls and corners still limit what you can see.",
                    "Open the west living-room window.", TutorialAction.WindowOpened);
                case TutorialStep.CloseWindow: return new TutorialLesson("Close the window",
                    "Click the fixed blue sill on the room side to close an open window, even if its sash has swung outside.\n\nClosing openings slows future growth. It does not shrink or extinguish existing flames.",
                    "Close the same living-room window using its blue inner sill.", TutorialAction.WindowClosed);
                case TutorialStep.OpenDoor: return new TutorialLesson("Doors connect rooms",
                    "Walk to the kitchen door and choose Open. Opening a door reveals the view through it and connects the rooms' airflow.\n\nYou cannot use hidden objects through walls or closed doors.",
                    "Open the kitchen door.", TutorialAction.DoorOpened);
                case TutorialStep.CloseDoor: return new TutorialLesson("Contain the fire",
                    "Close the kitchen door again. A closed door blocks movement, visibility and fire/smoke spread between rooms.\n\nA burning doorway cannot be operated safely. Keep an escape route available.",
                    "Close the kitchen door.", TutorialAction.DoorClosed);
                case TutorialStep.PickUpBucket: return new TutorialLesson("One inventory slot",
                    "Open the kitchen door again. Find the blue bucket near the kitchen's northeast corner, click it and choose Pick up.\n\nYou can carry one item. The inventory panel shows its contents.",
                    "Open the kitchen door and pick up the blue bucket.", TutorialAction.BucketPickedUp);
                case TutorialStep.FillBucket: return new TutorialLesson("Fill it at the tap",
                    "An empty bucket cannot extinguish a fire. Carry it to the kitchen tap and choose Fill bucket. Filling takes four seconds.\n\nStay at the tap. Cancelling or losing access interrupts filling.",
                    "Fill the bucket at the kitchen tap; wait until it contains water.", TutorialAction.BucketFilled);
                case TutorialStep.SwapForExtinguisher: return new TutorialLesson("Swap, don't stack",
                    "With your full bucket, walk to the red ABC powder extinguisher beside the living room's west wall. Choose Swap items.\n\nThe bucket is left exactly where you picked up the extinguisher. Its water is preserved.",
                    "Swap your full bucket for the ABC powder extinguisher.", TutorialAction.SwappedForExtinguisher);
                case TutorialStep.RetrieveWater: return new TutorialLesson("Pick the bucket back up",
                    "Click the filled bucket you just left on the floor. Swap again. You now carry the same water, and the extinguisher is left at that spot.\n\nWe will use that water for a class A fire.",
                    "Swap back to the filled bucket.", TutorialAction.WaterRetrieved);
                case TutorialStep.SmallFireSafety: return new TutorialLesson("Small enough to attempt?",
                    "Attempt extinguishing only below 50 cm, with suitable equipment and a clear way out. Bigger fire means evacuate.\n\nIn Game, fire starts at 25 cm, holds for 20 seconds, then grows exponentially. Openings change its speed. These next practice fires stay at 25 cm so you have time to learn.\n\nWalking into any flame can still kill you.");
                case TutorialStep.ExtinguishA: return new TutorialLesson("Class A: water on solid fuel",
                    "The coffee table is burning. Water cools ordinary solid materials. Click the small fire, walk to a safe spot nearby and choose Extinguish the fire with bucket of water.\n\nThis drill uses your filled bucket. A successful discharge empties it.",
                    "Use your bucket of water on the small coffee-table fire.", TutorialAction.ExtinguishedA);
                case TutorialStep.ExtinguishB: return new TutorialLesson("Class B: never a bucket of water",
                    "The paint bottle is burning. Water can spread burning liquid. Pick up the ABC powder extinguisher, then click the fire.\n\nThe character approaches to 3–4 m. Select Extinguish and answer the distance question. You will get a short explanation after choosing. Powder can reduce indoor visibility and impair breathing.",
                    "Use ABC powder on the paint-bottle fire. Choose 3 - 4 metres in the question.", TutorialAction.ExtinguishedB);
                case TutorialStep.ExtinguishF: return new TutorialLesson("Class F: cooking oil",
                    "The fryer is burning. Get the wet chemical extinguisher with the yellow band in the kitchen's northwest corner.\n\nNever pour water on burning oil: it can eject burning oil. This ABC powder model is not F-rated. Wet chemical cools and forms a protective layer. Its 3–3.7 m range is the 3–4 m answer in the question.",
                    "Use the wet chemical extinguisher on the fryer; choose 3 - 4 metres.", TutorialAction.ExtinguishedF);
                case TutorialStep.GasSafety: return new TutorialLesson("Class C: leaking gas",
                    "C-rated powder can suppress a gas flame, but gas still escaping can collect and explode. The supply must be isolated first.\n\nThis house has no usable gas-isolation control. What is the safe decision?", answers: new[] {
                        "Extinguish the flame while the gas keeps flowing", "Evacuate; do not extinguish a still-fed gas flame", "Pour the bucket of water onto the gas flame" }, correctAnswer: 1);
                case TutorialStep.SmokeSafety: return new TutorialLesson("Smoke is dangerous too",
                    "Developed fire produces smoke that spreads much faster. Closed doors contain it. Stay low when you see smoke and leave.\n\nCoughing is the warning. Prolonged exposure is fatal; there is no health bar. Clear air stops new coughs and gradually reduces exposure. What should you do when smoke reaches you?", answers: new[] {
                        "Stay low and get outside to clear air", "Keep standing in smoke to watch the fire", "Wait indoors until the coughing stops" }, correctAnswer: 0);
                case TutorialStep.SmokePractice: return new TutorialLesson("Hear the warning, then leave",
                    "This is a controlled smoke patch with no fire. Walk to the cyan marker until you hear coughing, then move back into clear air.\n\nThere is no health bar. Staying in the smoke too long is still fatal. Do not wait for coughing to stop before leaving.",
                    "Enter the small smoke patch until you cough, then walk into clear air.", TutorialAction.LeftSmoke);
                case TutorialStep.Escape: return new TutorialLesson("Final drill: get out",
                    "A gas fire has started at the hob. This drill runs in real time: the fire grows, spreads and can produce deadly smoke.\n\nDo not fight a still-fed gas flame. Reach the front door at the bottom of the living room, open it, then click the path outside. Walk all the way through. Once safely outside, call 112.\n\nTouching fire or staying in smoke too long ends the attempt.",
                    "Open the front door and click the path outside. Escape from the gas-fire scenario.", TutorialAction.Escaped);
                default: return new TutorialLesson("Tutorial complete", "You have practised the tools and decisions. Try Game for a random fire and random opening states.");
            }
        }
    }

    public sealed class TutorialProgress
    {
        public TutorialStep Step { get; private set; }
        public bool NeedsBriefing { get; private set; } = true;
        public bool IsComplete => Step == TutorialStep.Complete;
        public TutorialProgress(TutorialStep start = TutorialStep.Welcome) { Step = start; }

        public bool ContinueBriefing()
        {
            if (!NeedsBriefing || IsComplete || TutorialLessons.Get(Step).Kind == LessonKind.Question) return false;
            if (TutorialLessons.Get(Step).Kind == LessonKind.Information) Advance();
            else NeedsBriefing = false;
            return true;
        }

        public bool Observe(TutorialAction action)
        {
            if (NeedsBriefing || IsComplete || TutorialLessons.Get(Step).Action != action || action == TutorialAction.None) return false;
            Advance(); return true;
        }

        public bool ObserveEquipment(EquipmentItem item)
        {
            if (item == null) return false;
            TutorialAction action = TutorialAction.None;
            if (Step == TutorialStep.PickUpBucket && item.Kind == EquipmentKind.Bucket) action = TutorialAction.BucketPickedUp;
            if (Step == TutorialStep.FillBucket && item.HasWater) action = TutorialAction.BucketFilled;
            if (Step == TutorialStep.SwapForExtinguisher && ExtinguishingRules.IsExtinguisher(item.Kind)) action = TutorialAction.SwappedForExtinguisher;
            if (Step == TutorialStep.RetrieveWater && item.Kind == EquipmentKind.Bucket && item.HasWater) action = TutorialAction.WaterRetrieved;
            return Observe(action);
        }

        public int Answer(int choice)
        {
            TutorialLesson lesson = TutorialLessons.Get(Step);
            if (!NeedsBriefing || lesson.Kind != LessonKind.Question || choice < 0 || choice >= lesson.Answers.Length) return 0;
            if (choice != lesson.CorrectAnswer) return -5;
            Advance(); return 10;
        }

        private void Advance() { Step++; NeedsBriefing = true; }
    }

    public sealed class SmokeDrillProgress
    {
        public bool HasCoughed { get; private set; }
        public bool Observe(bool coughing, float density, bool dead)
        {
            if (dead) return false;
            HasCoughed |= coughing;
            return HasCoughed && density < 0.05f;
        }
    }
}
