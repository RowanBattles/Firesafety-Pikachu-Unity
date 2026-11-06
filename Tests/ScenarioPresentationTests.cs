using System;
using FireSafety.Equipment;
using FireSafety.Fire;
using FireSafety.Gameplay;

public static class ScenarioPresentationTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static bool HasScore(string text) => text.ToLowerInvariant().Contains("point") || text.Contains("Score:");

    public static void Main()
    {
        foreach (ScenarioMode mode in Enum.GetValues(typeof(ScenarioMode)))
        {
            Check(ScenarioPresentation.ShowLiveScore(mode) == (mode == ScenarioMode.Dev), "Only Dev may reveal the running score");
            Check(ScenarioPresentation.ShowFinalScore(mode) == (mode != ScenarioMode.Tutorial), "Tutorial must hide scores even on results");
            foreach (FireType type in Enum.GetValues(typeof(FireType)))
            {
                string title = ScenarioPresentation.FireTitle(mode, type);
                Check(mode == ScenarioMode.Game ? title == "Fire" : title.Contains("Class " + type),
                    "Game must conceal the class while Dev/Tutorial can explain it");
                Check(ScenarioPresentation.FireHint(mode, 50).Length > 0 == (mode == ScenarioMode.Dev),
                    "Only Dev may pre-announce that fire is too large");
                Check(ScenarioPresentation.FireHint(mode, 25) == "", "Small fire must not show a size-limit warning");
            }
        }
        foreach (ExtinguishingBlock block in Enum.GetValues(typeof(ExtinguishingBlock)))
        {
            var failed = new ExtinguishingOutcome(false, -5, "An explanatory answer that must stay out of Game.");
            string message = ScenarioPresentation.AttemptMessage(ScenarioMode.Game, failed, block);
            Check(message == (block == ExtinguishingBlock.TooLarge ? "Fire too big to extinguish." : "Fire was not extinguished."),
                "Game should reveal the outcome, without the answer or score");
            Check(!HasScore(message) && !message.Contains("explanatory"), "Game failures must not disclose points or answer explanations");
        }
        Check(ScenarioPresentation.AttemptMessage(ScenarioMode.Game, new ExtinguishingOutcome(true, 10, "Correct: 3–4 metres"),
            ExtinguishingBlock.None) == "Fire successfully extinguished.", "Game success should report only the outcome before final scoring");

        foreach (TutorialStep step in new[] { TutorialStep.GasSafety, TutorialStep.SmokeSafety })
        {
            TutorialLesson lesson = TutorialLessons.Get(step);
            for (int choice = 0; choice < lesson.Answers.Length; choice++)
            {
                TutorialFeedback feedback = TutorialFeedback.Question(step, choice);
                Check(feedback.Correct == (choice == lesson.CorrectAnswer), "Tutorial must identify both right and wrong choices");
                Check(feedback.Text.Contains("Your answer: " + lesson.Answers[choice])
                    && feedback.Text.Contains("Correct answer: " + lesson.Answers[lesson.CorrectAnswer])
                    && feedback.Text.Contains("Incorrect answers:"), "Feedback must show the chosen, correct and incorrect answers");
                Check(!HasScore(feedback.Text), "Tutorial explanations must never show points");
            }
        }
        foreach (EquipmentKind kind in new[] { EquipmentKind.AbcPowderExtinguisher, EquipmentKind.WetChemicalExtinguisher })
            for (int choice = 0; choice < 4; choice++)
            {
                var outcome = new ExtinguishingOutcome(choice == 2, choice == 2 ? 10 : -5, "The fire " + (choice == 2 ? "is out." : "is still burning."));
                TutorialFeedback feedback = TutorialFeedback.Extinguishing(kind, kind == EquipmentKind.WetChemicalExtinguisher ? FireType.F : FireType.B,
                    choice, ExtinguishingBlock.None, outcome);
                Check(feedback.Text.Contains("Correct answer: 3–4 metres") && feedback.Text.Contains("Incorrect answers:"),
                    "Both extinguisher questions must explain all answers in Tutorial");
                Check(feedback.Correct == outcome.Extinguished && !HasScore(feedback.Text), "Distance feedback must be accurate and score-free");
            }
        foreach (TutorialStep step in Enum.GetValues(typeof(TutorialStep)))
            Check(!HasScore(TutorialLessons.Get(step).Body), "Tutorial lesson copy must not mention scores");
        string wrongSwap = TutorialFeedback.Practice(TutorialStep.SwapForExtinguisher, EquipmentKind.WetChemicalExtinguisher).Text;
        Check(wrongSwap.Contains("wrong items, but that's okay") && wrongSwap.Contains("next step") && !HasScore(wrongSwap),
            "The wrong swap should reassure the player and direct them onward without points");
        Console.WriteLine("Scenario presentation: " + checks + " checks passed.");
    }
}
