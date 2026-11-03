using System;
using FireSafety.Equipment;
using FireSafety.Fire;

namespace FireSafety.Gameplay
{
    public static class ExtinguishingOpportunity
    {
        // Reachability and visibility are checked in the world; this checks the remaining time.
        public static bool HasTimeToUse(FireSimulation fire, EquipmentItem item, double secondsRequired)
        {
            if (secondsRequired < 0 || double.IsNaN(secondsRequired) || double.IsInfinity(secondsRequired)
                || ExtinguishingRules.Check(fire, item) != ExtinguishingBlock.None) return false;
            double holdRemaining = Math.Max(0, FireSimulation.InitialHoldSeconds - fire.ElapsedSeconds);
            double growthRemaining = fire.DoublingSeconds
                * Math.Log(ExtinguishingRules.MaximumSizeCm / fire.GrowthSizeCm, 2);
            return secondsRequired < holdRemaining + growthRemaining;
        }
    }
}
