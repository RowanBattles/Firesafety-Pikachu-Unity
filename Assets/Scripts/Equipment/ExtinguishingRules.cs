using FireSafety.Fire;

namespace FireSafety.Equipment
{
    public enum ExtinguishingBlock { None, NoItem, FireOut, TooLarge, Empty, WrongAgent, GasStillOn }

    public static class ExtinguishingRules
    {
        public const double MaximumSizeCm = 50;
        public const int CorrectPoints = 10, IncorrectPoints = -5;

        public static ExtinguishingBlock Check(FireSimulation fire, EquipmentItem item)
        {
            if (item == null) return ExtinguishingBlock.NoItem;
            if (fire == null || fire.IsExtinguished) return ExtinguishingBlock.FireOut;
            if (fire.SizeCm >= MaximumSizeCm) return ExtinguishingBlock.TooLarge;
            if (!item.CanDischarge) return ExtinguishingBlock.Empty;
            if (item.Kind == EquipmentKind.Bucket && fire.Type != FireType.A || fire.Type == FireType.F)
                return ExtinguishingBlock.WrongAgent;
            // There is no gas-isolation interaction in this first equipment showcase.
            if (fire.Type == FireType.C) return ExtinguishingBlock.GasStillOn;
            return ExtinguishingBlock.None;
        }

        public static string Explanation(ExtinguishingBlock block, EquipmentItem item, FireType type)
        {
            switch (block)
            {
                case ExtinguishingBlock.NoItem: return "Pick up an item first.";
                case ExtinguishingBlock.FireOut: return "The fire is already out.";
                case ExtinguishingBlock.TooLarge: return "The fire is now 50 cm or larger. Do not attempt to extinguish it. Evacuate.";
                case ExtinguishingBlock.Empty: return item.Kind == EquipmentKind.Bucket
                    ? "The bucket is empty. Fill it at the tap first."
                    : "The extinguisher is empty. Find another way to stay safe.";
                case ExtinguishingBlock.GasStillOn: return "Do not extinguish a gas flame while gas is still escaping. The gas supply cannot be isolated in this scenario. Evacuate.";
                case ExtinguishingBlock.WrongAgent:
                    if (type == FireType.F) return "Cooking-oil fires need an F-rated agent. Water can eject burning oil; this ABC extinguisher is not F-rated. Evacuate.";
                    return "Do not use a bucket of water on burning liquid or leaking gas. Use water on ordinary solid materials (class A).";
                default: return "";
            }
        }
    }

    public struct ExtinguishingOutcome
    {
        public readonly bool Extinguished;
        public readonly int Points;
        public readonly string Message;
        public ExtinguishingOutcome(bool extinguished, int points, string message)
        { Extinguished = extinguished; Points = points; Message = message; }
    }

    // Resolving an attempt is atomic and can award points only once. Recheck the live fire.
    public sealed class ExtinguishingAttempt
    {
        private readonly FireSimulation fire;
        private readonly EquipmentItem item;
        public bool IsResolved { get; private set; }

        public ExtinguishingAttempt(FireSimulation fire, EquipmentItem item)
        { this.fire = fire; this.item = item; }

        // For the ABC powder model in the supplied reference, choice 2 is the 3–4 m range.
        public ExtinguishingOutcome Resolve(int distanceChoice = -1)
        {
            if (IsResolved) return new ExtinguishingOutcome(false, 0, "This attempt has already been resolved.");
            ExtinguishingBlock block = ExtinguishingRules.Check(fire, item);
            if (block != ExtinguishingBlock.None)
            {
                IsResolved = true;
                int points = block == ExtinguishingBlock.WrongAgent || block == ExtinguishingBlock.GasStillOn
                    ? ExtinguishingRules.IncorrectPoints : 0;
                return new ExtinguishingOutcome(false, points,
                    ExtinguishingRules.Explanation(block, item, fire == null ? FireType.A : fire.Type));
            }
            if (item.Kind == EquipmentKind.AbcPowderExtinguisher && (distanceChoice < 0 || distanceChoice > 3))
                throw new System.ArgumentOutOfRangeException(nameof(distanceChoice));
            IsResolved = true;
            if (item.Kind == EquipmentKind.AbcPowderExtinguisher && distanceChoice != 2)
                return new ExtinguishingOutcome(false, ExtinguishingRules.IncorrectPoints,
                    "For this ABC powder extinguisher, start at 3–4 metres. The fire is still burning; no agent was discharged.");
            if (!fire.TryExtinguish(ExtinguishingRules.MaximumSizeCm))
                return new ExtinguishingOutcome(false, 0, "The fire can no longer be extinguished safely. Evacuate.");
            item.Discharge();
            return new ExtinguishingOutcome(true, ExtinguishingRules.CorrectPoints,
                item.Kind == EquipmentKind.Bucket
                    ? "The small class A fire is out. The bucket is now empty."
                    : "Correct: 3–4 metres for this ABC powder model. The fire is out. Powder can reduce visibility and impair breathing indoors; keep an escape route.");
        }
    }
}
