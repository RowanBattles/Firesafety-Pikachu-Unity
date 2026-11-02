using System;
using FireSafety.Fire;

public static class FireSimulationTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    private static bool[] Passage(bool doorOpen)
    {
        var mask = new bool[35];
        for (int i = 0; i < mask.Length; i++) mask[i] = i % 7 != 3;
        mask[17] = doorOpen; // The only doorway through the wall.
        return mask;
    }

    private static FireSimulation Fire() => new FireSimulation(7, 5, 0.5, 15, FireType.A, FireLocation.Table);

    public static void Main()
    {
        Check(Enum.GetValues(typeof(FireType)).Length == 4, "Only A, B, C and F are offered");
        var pairs = new[] {
            Tuple.Create(FireType.A, FireLocation.Table), Tuple.Create(FireType.A, FireLocation.Bed),
            Tuple.Create(FireType.B, FireLocation.PaintBottle), Tuple.Create(FireType.C, FireLocation.Hob),
            Tuple.Create(FireType.F, FireLocation.DeepFryer) };
        foreach (var pair in pairs)
        {
            var scenario = new FireSimulation(7, 5, 0.5, 15, pair.Item1, pair.Item2);
            Check(scenario.Type == pair.Item1 && scenario.Location == pair.Item2
                && Math.Abs(scenario.SizeCm - 25) < 1e-8 && scenario.BurningCellCount == 1,
                "Every valid scenario must begin as a small 25 cm fire");
        }
        bool rejected = false;
        try { new FireSimulation(7, 5, 0.5, 15, FireType.F, FireLocation.Bed); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "A fire must use a source matching its class");

        var held = Fire();
        held.Advance(19.9, Passage(true));
        Check(Math.Abs(held.SizeCm - 25) < 1e-8 && held.BurningCellCount == 1,
            "The fire must stay at its initial size without spreading before 20 seconds");
        held.Advance(0.1, Passage(true));
        Check(Math.Abs(held.GrowthSizeCm - 25) < 1e-8 && held.BurningCellCount == 1,
            "The initial size must still be held at exactly 20 seconds");
        held.Advance(0.05, Passage(true));
        Check(Math.Abs(held.SizeCm - 25 * Math.Pow(2, 0.05 / 15)) < 1e-7,
            "Growth must begin smoothly after the hold, without catching up the first 20 seconds");
        var custom = new FireSimulation(7, 5, 0.5, 15, FireType.A, FireLocation.Table, initialSizeCm: 30);
        custom.Advance(20, Passage(true));
        Check(Math.Abs(custom.SizeCm - 30) < 1e-8, "The hold must preserve a custom initial size too");
        custom.Advance(15, Passage(true));
        Check(Math.Abs(custom.SizeCm - 60) < 1e-8, "A custom initial size must double after the hold");

        var growth = Fire();
        growth.Advance(20, Passage(false));
        growth.Advance(15, Passage(false));
        Check(Math.Abs(growth.GrowthSizeCm - 50) < 1e-7 && Math.Abs(growth.SizeCm - 50) < 1e-7,
            "A small fire doubles to 50 cm at 35 seconds, after holding for 20 seconds");
        growth.Advance(15, Passage(false));
        Check(Math.Abs(growth.GrowthSizeCm - 100) < 1e-7, "The next 15 seconds double size again");
        growth.Advance(150, Passage(false));
        for (int cell = 0; cell < 35; cell++)
            Check(growth.IsBurning(cell) == (cell % 7 < 3), "A closed door confines the fire to its original room");
        Check(growth.SizeCm <= 250.0001, "The reported footprint cannot grow beyond the available room");

        growth.Advance(0.05, Passage(true));
        Check(growth.IsBurning(17), "Opening a hot doorway lets the fire enter the doorway");
        Check(!growth.IsBurning(18), "Opening a door must not ignite the entire next room in the same step");
        growth.Advance(1, Passage(true));
        Check(growth.IsBurning(18) && growth.IsBurning(20), "Fire then propagates into the connected room");
        for (int z = 0; z < 5; z++)
            if (z != 2) Check(!growth.IsBurning(z * 7 + 3), "Fire must not pass through the solid parts of a wall");
        int burned = growth.BurningCellCount;
        growth.Advance(2, Passage(false));
        Check(growth.BurningCellCount >= burned && growth.IsBurning(18),
            "Closing a door cannot extinguish fire that has already reached the other room");

        var late = Fire();
        late.Advance(330, Passage(false));
        Check(!late.IsBurning(18), "A closed room remains protected even after a long wait");
        late.Advance(1, Passage(true));
        Check(late.IsBurning(18), "Numerical growth limits must not stop a mature fire spreading after a late opening");

        var single = Fire(); var sliced = Fire();
        single.Advance(65, Passage(true));
        for (int i = 0; i < 650; i++) sliced.Advance(0.1, Passage(true));
        Check(Math.Abs(single.GrowthSizeCm - sliced.GrowthSizeCm) < 1e-6, "Growth is independent of frame slicing");
        for (int cell = 0; cell < 35; cell++)
            Check(single.IsBurning(cell) == sliced.IsBurning(cell), "Spread is independent of frame slicing");

        var wholeHold = Fire(); var slicedHold = Fire();
        wholeHold.Advance(20.05, Passage(true));
        for (int i = 0; i < 401; i++) slicedHold.Advance(0.05, Passage(true));
        Check(Math.Abs(wholeHold.SizeCm - slicedHold.SizeCm) < 1e-7
            && wholeHold.BurningCellCount == slicedHold.BurningCellCount,
            "One frame crossing the entire hold must match small frames across its boundary");
        var putOutDuringHold = Fire();
        putOutDuringHold.Advance(10, Passage(true));
        Check(putOutDuringHold.TryExtinguish(), "A fire must remain extinguishable during its initial hold");
        putOutDuringHold.Advance(100, Passage(true));
        Check(putOutDuringHold.IsExtinguished && putOutDuringHold.SizeCm == 0,
            "An extinguished fire must not start growing when its hold would have ended");

        var corner = new FireSimulation(2, 2, 0.5, 0, FireType.B, FireLocation.PaintBottle);
        corner.Advance(180, new[] { true, false, false, true });
        Check(!corner.IsBurning(3), "Fire cannot cut diagonally through a sealed corner");
        var edge = new FireSimulation(3, 2, 0.5, 2, FireType.C, FireLocation.Hob);
        edge.Advance(180, new[] { false, false, true, true, false, false });
        Check(!edge.IsBurning(3) && !edge.IsBurning(-1) && !edge.IsBurning(6), "Fire cannot wrap rows or leave the grid");
        Console.WriteLine("Passed " + checks + " fire simulation checks.");
    }
}
