using System;

namespace FireSafety.Equipment
{
    public enum EquipmentKind { AbcPowderExtinguisher, Bucket, WetChemicalExtinguisher }

    // The same item survives being carried, dropped and swapped, including its contents.
    public sealed class EquipmentItem
    {
        public EquipmentKind Kind { get; }
        public bool HasWater { get; private set; }
        public bool IsSpent { get; private set; }
        public bool CanDischarge => Kind == EquipmentKind.Bucket ? HasWater : !IsSpent;
        public string Name
        {
            get
            {
                switch (Kind)
                {
                    case EquipmentKind.Bucket: return HasWater ? "bucket of water" : "empty bucket";
                    case EquipmentKind.WetChemicalExtinguisher:
                        return IsSpent ? "empty wet chemical extinguisher" : "wet chemical extinguisher";
                    default: return IsSpent ? "empty ABC powder extinguisher" : "ABC powder extinguisher";
                }
            }
        }

        public EquipmentItem(EquipmentKind kind) { Kind = kind; }

        public void Fill()
        {
            if (Kind != EquipmentKind.Bucket) throw new InvalidOperationException("Only a bucket can be filled here.");
            HasWater = true;
        }

        public void Discharge()
        {
            if (!CanDischarge) throw new InvalidOperationException("The item is empty.");
            if (Kind == EquipmentKind.Bucket) HasWater = false;
            else IsSpent = true;
        }
    }

    public sealed class SingleSlotInventory
    {
        public EquipmentItem Held { get; private set; }

        public EquipmentItem Swap(EquipmentItem incoming)
        {
            if (incoming == null || incoming == Held) throw new ArgumentException("Choose a different item.");
            EquipmentItem previous = Held;
            Held = incoming;
            return previous;
        }
    }

    public sealed class BucketFilling
    {
        private readonly EquipmentItem bucket;
        private readonly double duration;
        private double elapsed;
        public double RemainingSeconds => Math.Max(0, duration - elapsed);
        public bool IsComplete { get; private set; }
        public bool IsCancelled { get; private set; }

        public BucketFilling(EquipmentItem bucket, double seconds = 4)
        {
            if (bucket == null || bucket.Kind != EquipmentKind.Bucket || bucket.HasWater || seconds <= 0)
                throw new ArgumentException("Filling requires an empty bucket and a positive duration.");
            this.bucket = bucket; duration = seconds;
        }

        public void Advance(double seconds, EquipmentItem held, bool canReachTap)
        {
            if (seconds < 0 || double.IsNaN(seconds) || double.IsInfinity(seconds))
                throw new ArgumentOutOfRangeException(nameof(seconds));
            if (IsComplete || IsCancelled) return;
            if (held != bucket || !canReachTap) { Cancel(); return; }
            elapsed += seconds;
            if (elapsed + 1e-9 < duration) return;
            bucket.Fill();
            IsComplete = true;
        }

        public void Cancel() { if (!IsComplete) IsCancelled = true; }
    }
}
