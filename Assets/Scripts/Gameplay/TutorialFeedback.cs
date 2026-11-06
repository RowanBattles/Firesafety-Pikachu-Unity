using System;
using FireSafety.Equipment;
using FireSafety.Fire;

namespace FireSafety.Gameplay
{
    // Feedback is deliberately score-free, including after incorrect decisions.
    public sealed class TutorialFeedback
    {
        public readonly bool Correct;
        public readonly string Title, Text;
        public TutorialFeedback(bool correct, string text)
        { Correct = correct; Title = correct ? "Correct decision" : "Incorrect decision"; Text = text; }

        public static TutorialFeedback Question(TutorialStep step, int choice)
        {
            TutorialLesson lesson = TutorialLessons.Get(step);
            if (lesson.Answers == null || choice < 0 || choice >= lesson.Answers.Length)
                throw new ArgumentOutOfRangeException(nameof(choice));
            string reason = step == TutorialStep.GasSafety
                ? "Extinguishing a still-fed gas flame leaves unburned gas escaping. Without a safe way to isolate the supply, evacuate."
                : "Smoke can incapacitate and kill. Stay low and get out; coughing is a warning to leave, not a reason to wait.";
            bool correct = choice == lesson.CorrectAnswer;
            return new TutorialFeedback(correct, correct ? reason
                : AnswerText(lesson.Answers, choice, lesson.CorrectAnswer) + "\n\n" + reason);
        }

        private static string AnswerText(string[] answers, int choice, int correct)
        {
            return "Your answer: " + answers[choice] + "\nCorrect answer: " + answers[correct];
        }

        public static TutorialFeedback Practice(TutorialStep step, EquipmentKind? chosen = null)
        {
            string information;
            switch (step)
            {
                case TutorialStep.Movement: information = "You can only walk on visible, reachable floor."; break;
                case TutorialStep.OpenWindow: information = "Opening windows during a fire feeds it more air. Close them if it is safe to do so."; break;
                case TutorialStep.CloseWindow: information = "Closed windows slow fire growth. They do not put the fire out."; break;
                case TutorialStep.OpenDoor: information = "Open doors let you reach the next room, but also let fire and smoke spread."; break;
                case TutorialStep.CloseDoor: information = "Closed doors help contain fire and smoke."; break;
                case TutorialStep.PickUpBucket: information = "You can carry one item at a time."; break;
                case TutorialStep.FillBucket: information = "The bucket is full. Water can put out small class A fires."; break;
                case TutorialStep.SwapForExtinguisher:
                    bool requested = chosen == EquipmentKind.AbcPowderExtinguisher;
                    return new TutorialFeedback(requested, requested
                        ? "Swapping leaves your previous item on the floor and preserves its contents."
                        : "You swapped the wrong items, but that's okay. This step asked for ABC powder. Next step: pick your full bucket back up.");
                case TutorialStep.RetrieveWater: information = "Swapping items preserves the water in your bucket."; break;
                case TutorialStep.ExtinguishA: information = "Water cools solid fuel in class A fires. Never use it on burning oil."; break;
                case TutorialStep.ExtinguishB: information = "Use ABC powder for this paint fire. Water can spread burning liquid."; break;
                case TutorialStep.ExtinguishF: information = "Use wet chemical for cooking-oil fires. Water can eject burning oil."; break;
                case TutorialStep.SmokePractice: information = "Coughing warns you to leave smoke. Stay low and get to clear air."; break;
                default: information = "Get outside and call 112 once you are safe."; break;
            }
            return new TutorialFeedback(true, information);
        }

        public static TutorialFeedback Extinguishing(EquipmentKind kind, FireType type, int choice,
            ExtinguishingBlock block, ExtinguishingOutcome outcome)
        {
            if (outcome.Extinguished && kind == EquipmentKind.Bucket) return Practice(TutorialStep.ExtinguishA);
            if (block == ExtinguishingBlock.None && ExtinguishingRules.IsExtinguisher(kind)
                && choice >= 0 && choice < ExtinguishingRules.DistanceAnswers.Length)
            {
                string distance = kind == EquipmentKind.WetChemicalExtinguisher
                    ? "Start 3–3.7 metres away with this wet chemical extinguisher."
                    : "Start 3–4 metres away with this ABC powder extinguisher.";
                return new TutorialFeedback(outcome.Extinguished, outcome.Extinguished ? "The fire is out. " + distance
                    : AnswerText(ExtinguishingRules.DistanceAnswers, choice, ExtinguishingRules.CorrectDistanceChoice(kind))
                        + "\n\n" + distance + " The fire is still burning.");
            }
            string correct = block == ExtinguishingBlock.TooLarge ? "Evacuate instead of fighting a fire of 50 cm or more."
                : block == ExtinguishingBlock.Empty ? "Fill the bucket or choose a charged extinguisher."
                : type == FireType.C ? "Evacuate while the gas supply cannot be isolated."
                : type == FireType.B ? "Use ABC powder for the small paint fire."
                : type == FireType.F ? "Use wet chemical for the small cooking-oil fire."
                : "Use a full bucket for this class A water drill.";
            string reason = block == ExtinguishingBlock.TooLarge ? "The fire is too big to fight safely."
                : block == ExtinguishingBlock.Empty ? "Empty equipment cannot discharge."
                : type == FireType.C ? "Unburned gas can collect and explode if the supply keeps flowing."
                : type == FireType.F ? "Water can eject burning oil; this ABC model is not F-rated."
                : type == FireType.B ? "Water can spread burning liquid."
                : "This drill practises using water on solid fuel.";
            return new TutorialFeedback(outcome.Extinguished, "Your choice: " + new EquipmentItem(kind).Name
                + ".\n\n" + correct + "\n\n" + reason);
        }
    }
}
