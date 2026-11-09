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
            return new TutorialFeedback(choice == lesson.CorrectAnswer,
                AnswerText(lesson.Answers, choice, lesson.CorrectAnswer) + "\n\n" + reason);
        }

        private static string AnswerText(string[] answers, int choice, int correct)
        {
            string wrong = "";
            for (int i = 0; i < answers.Length; i++)
                if (i != correct) wrong += (wrong.Length == 0 ? "" : "; ") + answers[i];
            return "Your answer: " + answers[choice] + "\n\nCorrect answer: " + answers[correct]
                + "\n\nIncorrect answers: " + wrong;
        }

        public static TutorialFeedback Practice(TutorialStep step, EquipmentKind? chosen = null)
        {
            string correct = TutorialLessons.Get(step).Objective;
            string wrong;
            switch (step)
            {
                case TutorialStep.Movement: wrong = "Clicking walls or an unreachable floor tile does not create a route."; break;
                case TutorialStep.OpenWindow: wrong = "Opening windows during a fire feeds it more air. This exercise demonstrates the control."; break;
                case TutorialStep.CloseWindow: wrong = "Leaving it open continues to feed ventilation. Closing it slows growth but does not extinguish fire."; break;
                case TutorialStep.OpenDoor: wrong = "Objects behind a closed door cannot be used remotely."; break;
                case TutorialStep.CloseDoor: wrong = "Leaving doors open lets fire and smoke spread between connected rooms."; break;
                case TutorialStep.PickUpBucket: wrong = "Picking an extinguisher instead would not practise filling the bucket."; break;
                case TutorialStep.FillBucket: wrong = "Trying to extinguish with an empty bucket cannot put a fire out."; break;
                case TutorialStep.SwapForExtinguisher:
                    bool requested = chosen == EquipmentKind.AbcPowderExtinguisher;
                    return new TutorialFeedback(requested, requested
                        ? "You swapped the full bucket for ABC powder.\n\nCorrect choice for this step: ABC powder.\nIncorrect choice for this step: wet chemical, which we practise later.\n\nThe water is preserved in the bucket you left on the floor. Next, pick that bucket back up."
                        : "You swapped the wrong items, but that's okay.\n\nYour choice: wet chemical.\nCorrect choice for this step: ABC powder. Wet chemical is useful too, and we practise it later.\n\nContinue to the next step: retrieve the full bucket you left where you picked up the extinguisher.");
                case TutorialStep.RetrieveWater: wrong = "Carrying a different item would leave the water behind. Swapping preserves the bucket's contents."; break;
                case TutorialStep.ExtinguishA: wrong = "Water is not suitable for burning paint, leaking gas or cooking oil. ABC and wet chemical can also suit A; this drill practises water."; break;
                case TutorialStep.ExtinguishB: wrong = "Water can spread burning liquid; wet chemical is not the tool for this paint fire."; break;
                case TutorialStep.ExtinguishF: wrong = "Water can eject burning oil. This ABC powder model is not F-rated."; break;
                case TutorialStep.SmokePractice: wrong = "Staying in smoke after coughing can kill you. Leave for clear air immediately."; break;
                default: wrong = "Staying indoors with a still-fed gas fire is unsafe."; break;
            }
            return new TutorialFeedback(true, "Correct action: " + correct + "\n\nIncorrect action: " + wrong);
        }

        public static TutorialFeedback Extinguishing(EquipmentKind kind, FireType type, int choice,
            ExtinguishingBlock block, ExtinguishingOutcome outcome)
        {
            if (outcome.Extinguished && kind == EquipmentKind.Bucket) return Practice(TutorialStep.ExtinguishA);
            if (block == ExtinguishingBlock.None && ExtinguishingRules.IsExtinguisher(kind)
                && choice >= 0 && choice < ExtinguishingRules.DistanceAnswers.Length)
                return new TutorialFeedback(outcome.Extinguished, AnswerText(ExtinguishingRules.DistanceAnswers,
                    choice, ExtinguishingRules.CorrectDistanceChoice(kind)) + "\n\n" + outcome.Message);
            string correct = block == ExtinguishingBlock.TooLarge ? "Evacuate instead of fighting a fire of 50 cm or more."
                : block == ExtinguishingBlock.Empty ? "Fill the bucket or choose a charged extinguisher."
                : type == FireType.C ? "Evacuate while the gas supply cannot be isolated."
                : type == FireType.B ? "Use ABC powder for the small paint fire."
                : type == FireType.F ? "Use wet chemical for the small cooking-oil fire."
                : "Use a full bucket for this class A water drill.";
            return new TutorialFeedback(outcome.Extinguished, "Your choice: use " + new EquipmentItem(kind).Name
                + ".\n\nCorrect action: " + correct + "\n\n" + outcome.Message);
        }
    }
}
