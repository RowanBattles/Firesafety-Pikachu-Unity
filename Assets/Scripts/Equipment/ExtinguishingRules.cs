using FireSafety.Fire;

namespace FireSafety.Equipment
{
    public enum ExtinguishingBlock { None, NoItem, FireOut, TooLarge, Empty, WrongAgent, GasStillOn }

    public static class ExtinguishingRules
    {
        public const double MaximumSizeCm = 50;
        public const int CorrectPoints = 10, IncorrectPoints = -5;

        public static bool IsExtinguisher(EquipmentKind kind) => kind != EquipmentKind.Bucket;

        // Which fire classes each item is suitable for:
        // bucket of water: A only. ABC powder: A, B and C (C still needs the gas turned off).
        // Wet chemical: A and F (cooking oil). It is not for burning liquids (B) or gas (C).
        public static bool Suits(EquipmentKind kind, FireType type)
        {
            switch (kind)
            {
                case EquipmentKind.Bucket: return type == FireType.A;
                case EquipmentKind.WetChemicalExtinguisher: return type == FireType.A || type == FireType.F;
                default: return type != FireType.F;
            }
        }

        // Index into the distance question's answers:
        // 0 = less than 1 m, 1 = 1–2 m, 2 = 3–4 m, 3 = more than 6 m.
        // Both extinguishers start in the 3–4 m range (wet chemical: 3–3.7 m from the team's research).
        public static int CorrectDistanceChoice(EquipmentKind kind) => 2;

        // Where the character stands to use each extinguisher (minimum and maximum metres from the fire).
        public static float MinimumWorkingDistance(EquipmentKind kind) => 3f;
        public static float MaximumWorkingDistance(EquipmentKind kind)
            => kind == EquipmentKind.WetChemicalExtinguisher ? 3.7f : 4f;

        public static ExtinguishingBlock Check(FireSimulation fire, EquipmentItem item)
        {
            if (item == null) return ExtinguishingBlock.NoItem;
            if (fire == null || fire.IsExtinguished) return ExtinguishingBlock.FireOut;
            if (fire.SizeCm >= MaximumSizeCm) return ExtinguishingBlock.TooLarge;
            if (!item.CanDischarge) return ExtinguishingBlock.Empty;
            if (!Suits(item.Kind, fire.Type)) return ExtinguishingBlock.WrongAgent;
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
                    if (item.Kind == EquipmentKind.WetChemicalExtinguisher)
                        return type == FireType.C
                            ? "A wet chemical extinguisher is not for gas fires. Gas must be turned off at the supply, and this scenario cannot do that. Evacuate."
                            : "A wet chemical extinguisher is made for cooking oil (class F) and solid materials (class A), not for burning liquids such as paint (class B).";
                    if (type == FireType.F) return item.Kind == EquipmentKind.Bucket
                        ? "Never use water on a cooking-oil fire: it can make the burning oil erupt. Cooking-oil fires need a wet chemical (F-rated) extinguisher."
                        : "This ABC powder extinguisher is not F-rated. Cooking-oil fires need a wet chemical (F-rated) extinguisher.";
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

        // Extinguishers ask a distance question; see ExtinguishingRules.CorrectDistanceChoice.
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
            bool extinguisher = ExtinguishingRules.IsExtinguisher(item.Kind);
            bool wetChemical = item.Kind == EquipmentKind.WetChemicalExtinguisher;
            if (extinguisher && (distanceChoice < 0 || distanceChoice > 3))
                throw new System.ArgumentOutOfRangeException(nameof(distanceChoice));
            IsResolved = true;
            if (extinguisher && distanceChoice != ExtinguishingRules.CorrectDistanceChoice(item.Kind))
                return new ExtinguishingOutcome(false, ExtinguishingRules.IncorrectPoints, wetChemical
                    ? "For this wet chemical extinguisher, start at 3–3.7 metres so the burning oil does not splash onto you. The fire is still burning; no agent was discharged."
                    : "For this ABC powder extinguisher, start at 3–4 metres. The fire is still burning; no agent was discharged.");
            if (!fire.TryExtinguish(ExtinguishingRules.MaximumSizeCm))
                return new ExtinguishingOutcome(false, 0, "The fire can no longer be extinguished safely. Evacuate.");
            item.Discharge();
            return new ExtinguishingOutcome(true, ExtinguishingRules.CorrectPoints,
                item.Kind == EquipmentKind.Bucket
                    ? "The small class A fire is out. The bucket is now empty."
                    : wetChemical
                        ? "Correct: 3–3.7 metres for this wet chemical extinguisher. The fire is out. Apply it gently in slow circles so the burning oil does not splash, and turn off the heat if it is safe to do so."
                        : "Correct: 3–4 metres for this ABC powder model. The fire is out. Powder can reduce visibility and impair breathing indoors; keep an escape route.");
        }
    }
}
