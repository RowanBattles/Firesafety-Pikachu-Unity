using System;
using FireSafety.Equipment;
using FireSafety.Fire;
using FireSafety.Smoke;

public static class EquipmentTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }

    private static FireSimulation Fire(FireType type = FireType.A, double size = 25)
    {
        FireLocation location = type == FireType.B ? FireLocation.PaintBottle
            : type == FireType.C ? FireLocation.Hob : type == FireType.F ? FireLocation.DeepFryer : FireLocation.Table;
        return new FireSimulation(5, 5, 0.5, 12, type, location, size);
    }

    private static EquipmentItem Bucket(bool full = false)
    {
        var bucket = new EquipmentItem(EquipmentKind.Bucket);
        if (full) bucket.Fill();
        return bucket;
    }

    private static EquipmentItem Powder() => new EquipmentItem(EquipmentKind.AbcPowderExtinguisher);
    private static EquipmentItem WetChemical() => new EquipmentItem(EquipmentKind.WetChemicalExtinguisher);

    public static void Main()
    {
        var inventory = new SingleSlotInventory();
        var bucket = Bucket(); var powder = Powder();
        Check(inventory.Held == null, "A scenario begins with one empty inventory slot");
        Check(inventory.Swap(bucket) == null && inventory.Held == bucket, "First pickup fills the slot");
        bucket.Fill();
        Check(inventory.Swap(powder) == bucket && inventory.Held == powder,
            "A swap returns exactly the old item for dropping at the incoming item's position");
        Check(inventory.Swap(bucket) == powder && inventory.Held.HasWater,
            "Picking a dropped bucket up again preserves its water");
        bucket.Discharge();
        inventory.Swap(powder); inventory.Swap(bucket);
        Check(!inventory.Held.HasWater, "An empty bucket remains empty after being dropped and swapped back");
        bool duplicateRejected = false;
        try { inventory.Swap(bucket); } catch (ArgumentException) { duplicateRejected = true; }
        Check(duplicateRejected && inventory.Held == bucket, "A held item cannot also be picked up as a second item");

        var filling = new BucketFilling(bucket);
        filling.Advance(3.9, bucket, true);
        Check(!bucket.HasWater && !filling.IsComplete && filling.RemainingSeconds > 0,
            "The tap must not fill the bucket before the four-second wait");
        filling.Advance(0.1, bucket, true);
        Check(bucket.HasWater && filling.IsComplete && filling.RemainingSeconds == 0, "Four seconds at the tap fills the bucket");
        bucket.Discharge();
        filling.Advance(10, bucket, true);
        Check(!bucket.HasWater, "A completed filling operation must not refill a later emptied bucket");
        var cancelled = new BucketFilling(bucket);
        cancelled.Advance(2, bucket, true); cancelled.Cancel(); cancelled.Advance(10, bucket, true);
        Check(cancelled.IsCancelled && !bucket.HasWater, "Cancelling must leave an empty bucket");
        var swapped = new BucketFilling(bucket);
        swapped.Advance(3, bucket, true); swapped.Advance(1, powder, true);
        Check(swapped.IsCancelled && !bucket.HasWater, "Changing the held item interrupts filling");
        var unreachable = new BucketFilling(bucket);
        unreachable.Advance(4, bucket, false);
        Check(unreachable.IsCancelled && !bucket.HasWater, "Losing access to the tap prevents filling");
        var sliced = new BucketFilling(bucket);
        for (int i = 0; i < 40; i++) sliced.Advance(0.1, bucket, true);
        Check(sliced.IsComplete && bucket.HasWater, "Frame slicing must not change filling duration");

        for (int answer = 0; answer < 4; answer++)
        {
            var fire = Fire(); var item = Powder();
            var attempt = new ExtinguishingAttempt(fire, item);
            ExtinguishingOutcome outcome = attempt.Resolve(answer);
            Check(outcome.Extinguished == (answer == 2), "Only the model's 3–4 m answer should extinguish the fire");
            Check(outcome.Points == (answer == 2 ? 10 : -5), "Correct and incorrect distance choices have the right score");
            Check(item.IsSpent == (answer == 2), "Wrong distance must not discharge the extinguisher");
            Check(attempt.Resolve(2).Points == 0, "Repeated clicks cannot award the same answer twice");
        }

        foreach (FireType type in new[] { FireType.A, FireType.B, FireType.C, FireType.F })
        {
            var fire = Fire(type); var item = Powder();
            ExtinguishingOutcome result = new ExtinguishingAttempt(fire, item).Resolve(2);
            bool suitable = type == FireType.A || type == FireType.B;
            Check(result.Extinguished == suitable && item.IsSpent == suitable,
                "ABC powder must not extinguish cooking oil or a still-fed gas flame");
            Check(result.Points == (suitable ? 10 : -5), "Unsuitable or unsafe powder attempts lose points");
            var waterFire = Fire(type); var water = Bucket(true);
            result = new ExtinguishingAttempt(waterFire, water).Resolve();
            Check(result.Extinguished == (type == FireType.A), "Bucket water can extinguish only a small class A fire");
            Check(water.HasWater == (type != FireType.A), "An unsafe water attempt must not discharge water onto the fire");
            Check(result.Points == (type == FireType.A ? 10 : -5), "Wrong water decisions lose points");
        }

        for (int answer = 0; answer < 4; answer++)
        {
            var fire = Fire(FireType.F); var item = WetChemical();
            ExtinguishingOutcome outcome = new ExtinguishingAttempt(fire, item).Resolve(answer);
            Check(outcome.Extinguished == (answer == 2), "Only the 3–4 m answer (3–3.7 m) should let wet chemical put out cooking oil");
            Check(outcome.Points == (answer == 2 ? 10 : -5), "Wet chemical distance choices have the right score");
            Check(item.IsSpent == (answer == 2), "A wrong distance must not discharge the wet chemical extinguisher");
        }

        foreach (FireType type in new[] { FireType.A, FireType.B, FireType.C, FireType.F })
        {
            var fire = Fire(type); var item = WetChemical();
            ExtinguishingOutcome result = new ExtinguishingAttempt(fire, item).Resolve(2);
            bool suitable = type == FireType.A || type == FireType.F;
            Check(result.Extinguished == suitable && item.IsSpent == suitable,
                "Wet chemical puts out class A and F fires only");
            Check(result.Points == (suitable ? 10 : -5), "Using wet chemical on burning liquid or gas loses points");
        }

        var usedWetChemical = WetChemical(); usedWetChemical.Discharge();
        Check(usedWetChemical.Name == "empty wet chemical extinguisher"
            && new ExtinguishingAttempt(Fire(FireType.F), usedWetChemical).Resolve(2).Points == 0,
            "An empty wet chemical extinguisher cannot be reused");
        Check(ExtinguishingRules.Check(Fire(FireType.F, 50), WetChemical()) == ExtinguishingBlock.TooLarge,
            "Wet chemical also follows the strict 50 cm limit");

        Check(ExtinguishingRules.Check(Fire(FireType.C), Powder()) == ExtinguishingBlock.GasStillOn,
            "Class C requires gas isolation even with C-rated powder");
        Check(ExtinguishingRules.Check(Fire(FireType.F), Powder()) == ExtinguishingBlock.WrongAgent,
            "ABC powder is not automatically F-rated");
        Check(ExtinguishingRules.Check(Fire(), null) == ExtinguishingBlock.NoItem, "No held item means no extinguishing attempt");
        Check(new ExtinguishingAttempt(Fire(), Bucket()).Resolve().Points == 0, "An empty bucket cannot extinguish or earn points");
        var spent = Powder(); spent.Discharge();
        Check(new ExtinguishingAttempt(Fire(), spent).Resolve().Points == 0, "An empty extinguisher cannot be reused");

        foreach (double size in new[] { 49.99, 50.0, 50.01, 200.0 })
        {
            var fire = Fire(size: size); var item = Powder();
            ExtinguishingOutcome result = new ExtinguishingAttempt(fire, item).Resolve(2);
            Check(result.Extinguished == (size < 50), "Only fire strictly below 50 cm is eligible");
            Check(item.IsSpent == (size < 50), "A too-large fire must not consume the tool");
            Check(result.Points == (size < 50 ? 10 : -5), "Deliberately trying a fire of 50 cm or more deducts five points");
        }

        var passage = new bool[25];
        for (int i = 0; i < passage.Length; i++) passage[i] = true;
        var growing = Fire(); var waitingItem = Powder();
        var delayed = new ExtinguishingAttempt(growing, waitingItem);
        growing.Advance(36, passage);
        ExtinguishingOutcome late = delayed.Resolve(2);
        Check(!late.Extinguished && late.Points == -5 && !waitingItem.IsSpent,
            "Answering after the fire grows too large is a failed attempt with a five-point penalty");
        var expired = new ExtinguishingAttempt(growing, waitingItem);
        Check(expired.Resolve(playerInitiated: false).Points == 0 && !waitingItem.IsSpent,
            "Automatically cancelling an expired question must not charge for a new player decision");
        Check(expired.Resolve(2).Points == 0, "Resolving a cancelled attempt again must not charge a second time");

        var extinguished = Fire();
        Check(new ExtinguishingAttempt(extinguished, Bucket(true)).Resolve().Extinguished, "A prepared bucket puts out the small fire");
        Check(extinguished.IsExtinguished && extinguished.SizeCm == 0 && extinguished.GrowthSizeCm == 0
            && extinguished.BurningCellCount == 0, "Successful extinguishing removes the whole hazard");
        extinguished.Advance(600, passage);
        Check(extinguished.SizeCm == 0 && extinguished.BurningCellCount == 0, "Extinguished fire must not regrow");
        for (int cell = 0; cell < 25; cell++) Check(!extinguished.IsBurning(cell), "No burning cells may remain after extinguishing");
        Check(!extinguished.TryExtinguish(), "An already extinguished fire cannot be extinguished twice");
        Check(new ExtinguishingAttempt(extinguished, Powder()).Resolve(2).Points == 0, "An out fire cannot be used to farm score");
        var smoke = new SmokeSimulation(5, 5, 0.5);
        smoke.Advance(30, passage, extinguished.IsBurning);
        Check(smoke.DensityAt(12) == 0, "An extinguished fire cannot start emitting smoke");
        var freshInventory = new SingleSlotInventory();
        Check(freshInventory.Held == null && !Fire().IsExtinguished && !Bucket().HasWater,
            "Restarting creates fresh inventory, tools and a small live fire");
        Console.WriteLine("Passed " + checks + " inventory, filling, scoring and extinguishing checks.");
    }
}
