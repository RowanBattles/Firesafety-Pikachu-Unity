using System;

namespace FireSafety.Gameplay
{
    public enum ScenarioEnding { None, Escaped, Extinguished, FireDeath, SmokeDeath }

    // A result is finalized once; UI redraws and repeat clicks cannot change its score.
    public sealed class ScenarioResult
    {
        public const int EscapeReward = 10, MissedOpportunityDeduction = 2, DeathDeduction = 10;
        public ScenarioEnding Ending { get; private set; }
        public bool IsFinished => Ending != ScenarioEnding.None;
        public bool HadSafeExtinguishingOpportunity { get; private set; }
        public int DecisionPoints { get; private set; }
        public int EscapePoints { get; private set; }
        public int MissedOpportunityPoints { get; private set; }
        public int DeathPoints { get; private set; }
        public int TotalPoints => DecisionPoints + EscapePoints + MissedOpportunityPoints + DeathPoints;
        public string ExtinguishingFeedback { get; private set; }

        public void ObserveExtinguishingOpportunity(bool safelyAvailable)
        {
            if (!IsFinished && safelyAvailable) HadSafeExtinguishingOpportunity = true;
        }

        public bool Finish(ScenarioEnding ending, int decisionPoints, string extinguishingFeedback = "")
        {
            if (ending == ScenarioEnding.None) throw new ArgumentException("Choose an ending.");
            if (IsFinished) return false;
            Ending = ending;
            DecisionPoints = decisionPoints;
            ExtinguishingFeedback = extinguishingFeedback;
            if (ending == ScenarioEnding.Escaped)
            {
                EscapePoints = EscapeReward;
                if (HadSafeExtinguishingOpportunity) MissedOpportunityPoints = -MissedOpportunityDeduction;
            }
            if (ending == ScenarioEnding.FireDeath || ending == ScenarioEnding.SmokeDeath)
                DeathPoints = -DeathDeduction;
            return true;
        }

        public string Title
        {
            get
            {
                switch (Ending)
                {
                    case ScenarioEnding.Escaped: return "You escaped!";
                    case ScenarioEnding.Extinguished: return "Good job! Fire extinguished";
                    case ScenarioEnding.FireDeath: return "Caught by the fire";
                    case ScenarioEnding.SmokeDeath: return "Overcome by smoke";
                    default: return "";
                }
            }
        }

        public string Feedback
        {
            get
            {
                switch (Ending)
                {
                    case ScenarioEnding.Escaped:
                        return HadSafeExtinguishingOpportunity
                            ? "You made it outside safely. Earlier, this small fire could have been extinguished with suitable, reachable equipment. Missed opportunity: -"
                                + MissedOpportunityDeduction + " points."
                            : "You made it outside safely.";
                    case ScenarioEnding.Extinguished:
                        return "You put out the fire safely.\n\n" + ExtinguishingFeedback;
                    case ScenarioEnding.FireDeath: return "The fire reached you. Play again to try another decision.";
                    case ScenarioEnding.SmokeDeath: return "You stayed in the smoke too long. Play again to try another decision.";
                    default: return "";
                }
            }
        }
    }
}
