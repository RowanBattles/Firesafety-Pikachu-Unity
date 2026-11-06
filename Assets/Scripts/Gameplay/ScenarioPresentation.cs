using FireSafety.Equipment;
using FireSafety.Fire;

namespace FireSafety.Gameplay
{
    // Keep learning/debug hints and running scores out of the assessed Game mode.
    public static class ScenarioPresentation
    {
        public static bool ShowLiveScore(ScenarioMode mode) => mode == ScenarioMode.Dev;
        public static bool ShowFinalScore(ScenarioMode mode) => mode != ScenarioMode.Tutorial;
        public static string FireTitle(ScenarioMode mode, FireType type)
            => mode == ScenarioMode.Game ? "Fire" : "Class " + type + " fire";
        public static string FireHint(ScenarioMode mode, double sizeCm)
            => mode == ScenarioMode.Dev && sizeCm >= ExtinguishingRules.MaximumSizeCm ? "Too large to extinguish. Evacuate." : "";

        public static string AttemptMessage(ScenarioMode mode, ExtinguishingOutcome outcome, ExtinguishingBlock block)
        {
            if (mode == ScenarioMode.Game)
                return outcome.Extinguished ? "Fire successfully extinguished."
                    : block == ExtinguishingBlock.TooLarge ? "Fire too big to extinguish." : "Fire was not extinguished.";
            if (mode == ScenarioMode.Tutorial) return outcome.Message;
            string points = outcome.Points > 0 ? "+" + outcome.Points + " points"
                : outcome.Points < 0 ? outcome.Points + " points" : "No points scored";
            return outcome.Message + "\n\n" + points;
        }
    }
}
