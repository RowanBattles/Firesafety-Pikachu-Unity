using System;
using FireSafety.Gameplay;
using FireSafety.Equipment;
using FireSafety.Fire;

public static class ScenarioResultTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static FireSimulation Fire(FireType type = FireType.A)
    {
        FireLocation location = type == FireType.B ? FireLocation.PaintBottle
            : type == FireType.C ? FireLocation.Hob : type == FireType.F ? FireLocation.DeepFryer : FireLocation.Table;
        return new FireSimulation(5, 5, 0.5, 12, type, location);
    }
    private static EquipmentItem Powder() => new EquipmentItem(EquipmentKind.AbcPowderExtinguisher);
    private static EquipmentItem WetChemical() => new EquipmentItem(EquipmentKind.WetChemicalExtinguisher);

    public static void Main()
    {
        var fresh = new ScenarioResult();
        Check(!fresh.IsFinished && fresh.TotalPoints == 0 && !fresh.HadSafeExtinguishingOpportunity,
            "A new run begins without a result, score or remembered opportunity");
        fresh.ObserveExtinguishingOpportunity(false);
        Check(!fresh.HadSafeExtinguishingOpportunity, "An unsafe or unavailable attempt must not count as a missed opportunity");
        foreach (int decisions in new[] { -10, -5, 0, 5, 10 })
        {
            var escaped = new ScenarioResult();
            Check(escaped.Finish(ScenarioEnding.Escaped, decisions), "The first exit finalizes the run");
            Check(escaped.TotalPoints == decisions + 10 && escaped.EscapePoints == 10 && escaped.MissedOpportunityPoints == 0,
                "Escape adds its bonus to the existing decision score without an unsupported deduction");
            Check(!escaped.Finish(ScenarioEnding.Escaped, 1000) && escaped.TotalPoints == decisions + 10,
                "Repeated exits or UI clicks must not award escape points twice");
            escaped.ObserveExtinguishingOpportunity(true);
            Check(!escaped.HadSafeExtinguishingOpportunity, "Finishing freezes the opportunity history too");
        }

        var missed = new ScenarioResult();
        missed.ObserveExtinguishingOpportunity(true);
        missed.ObserveExtinguishingOpportunity(false);
        Check(missed.HadSafeExtinguishingOpportunity, "A safe opportunity remains recorded after the fire grows or a door closes");
        missed.Finish(ScenarioEnding.Escaped, 0);
        Check(missed.TotalPoints == 8 && missed.MissedOpportunityPoints == -2,
            "Missing a recorded safe opportunity deducts two points from the escape result");
        Check(missed.Feedback.Contains("Earlier"), "Feedback must describe the earlier opportunity rather than recommending a now-unsafe attempt");

        var extinguished = new ScenarioResult();
        extinguished.ObserveExtinguishingOpportunity(true);
        extinguished.Finish(ScenarioEnding.Extinguished, 10, "Correct distance.");
        Check(extinguished.TotalPoints == 10 && extinguished.EscapePoints == 0 && extinguished.MissedOpportunityPoints == 0,
            "Extinguishing shows earned decision points without a second success award or missed-opportunity penalty");
        Check(extinguished.Feedback.Contains("Correct distance."), "The final result retains the extinguishing feedback");
        Check(!extinguished.Finish(ScenarioEnding.Escaped, 10) && extinguished.Ending == ScenarioEnding.Extinguished,
            "A completed extinguishing run cannot turn into an escape result");
        foreach (ScenarioEnding death in new[] { ScenarioEnding.FireDeath, ScenarioEnding.SmokeDeath })
        {
            var failed = new ScenarioResult(); failed.ObserveExtinguishingOpportunity(true);
            failed.Finish(death, -5);
            Check(failed.IsFinished && failed.TotalPoints == -15 && failed.DeathPoints == -10
                && failed.DecisionPoints == -5 && failed.EscapePoints == 0 && failed.MissedOpportunityPoints == 0,
                "Both fire and smoke deaths deduct ten points from the existing decision score");
            Check(!failed.Finish(death, 0) && failed.TotalPoints == -15 && failed.DeathPoints == -10,
                "Death can only deduct its penalty once");
        }

        bool rejected = false;
        try { fresh.Finish(ScenarioEnding.None, 0); } catch (ArgumentException) { rejected = true; }
        Check(rejected && !fresh.IsFinished, "An invalid ending cannot accidentally finish the run");

        foreach (FireType type in new[] { FireType.A, FireType.B, FireType.C, FireType.F })
        {
            Check(ExtinguishingOpportunity.HasTimeToUse(Fire(type), Powder(), 4) == (type == FireType.A || type == FireType.B),
                "Opportunity assessment must respect ABC suitability and ongoing gas supply");
            Check(ExtinguishingOpportunity.HasTimeToUse(Fire(type), WetChemical(), 4) == (type == FireType.A || type == FireType.F),
                "Opportunity assessment must include the team's wet chemical extinguisher");
        }
        var empty = new EquipmentItem(EquipmentKind.Bucket);
        Check(!ExtinguishingOpportunity.HasTimeToUse(Fire(), empty, 4), "An empty bucket is not ready equipment");
        empty.Fill();
        Check(ExtinguishingOpportunity.HasTimeToUse(Fire(), empty, 4), "A reachable filled bucket can count for class A");
        var spent = Powder(); spent.Discharge();
        Check(!ExtinguishingOpportunity.HasTimeToUse(Fire(), spent, 4), "Spent equipment cannot create an opportunity");
        Check(!ExtinguishingOpportunity.HasTimeToUse(Fire(), null, 4), "Absent equipment cannot create an opportunity");

        var fire = Fire(); var tool = Powder(); var passage = new bool[25];
        for (int i = 0; i < passage.Length; i++) passage[i] = true;
        Check(ExtinguishingOpportunity.HasTimeToUse(fire, tool, 34.9), "The initial hold leaves 35 seconds before a 25 cm fire reaches 50 cm");
        Check(!ExtinguishingOpportunity.HasTimeToUse(fire, tool, 35), "An attempt finishing at exactly 50 cm is already too late");
        fire.Advance(20, passage);
        Check(ExtinguishingOpportunity.HasTimeToUse(fire, tool, 14.9)
            && !ExtinguishingOpportunity.HasTimeToUse(fire, tool, 15), "Travel and question time must fit after the hold too");
        fire.Advance(14, passage);
        Check(ExtinguishingOpportunity.HasTimeToUse(fire, tool, 0.5)
            && !ExtinguishingOpportunity.HasTimeToUse(fire, tool, 2), "Long routes do not count when the fire is nearly too large");
        fire.Advance(2, passage);
        Check(!ExtinguishingOpportunity.HasTimeToUse(fire, tool, 0), "A large fire must not create an opportunity");
        Check(!ExtinguishingOpportunity.HasTimeToUse(Fire(), tool, double.NaN)
            && !ExtinguishingOpportunity.HasTimeToUse(Fire(), tool, double.PositiveInfinity)
            && !ExtinguishingOpportunity.HasTimeToUse(Fire(), tool, -1), "Invalid route durations cannot create an opportunity");

        var trainingFire = Fire();
        int decisionScore = new ExtinguishingAttempt(trainingFire, tool).Resolve(0).Points;
        var success = new ExtinguishingAttempt(trainingFire, tool).Resolve(2);
        decisionScore += success.Points;
        var combined = new ScenarioResult(); combined.Finish(ScenarioEnding.Extinguished, decisionScore, success.Message);
        Check(trainingFire.IsExtinguished && combined.TotalPoints == 5,
            "Wrong distance then correct extinguishing must show the accumulated score on the final screen");
        var replay = new ScenarioResult();
        Check(!replay.IsFinished && replay.TotalPoints == 0 && !replay.HadSafeExtinguishingOpportunity,
            "Play again starts with fresh score and opportunity history");
        Console.WriteLine("Passed " + checks + " ending, score and extinguishing-opportunity checks.");
    }
}
