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
                && Math.Abs(scenario.SizeCm - 20) < 1e-8 && scenario.BurningCellCount == 1,
                "Every valid scenario must begin as a small 20 cm fire");
        }
        bool rejected = false;
        try { new FireSimulation(7, 5, 0.5, 15, FireType.F, FireLocation.Bed); }
        catch (ArgumentException) { rejected = true; }
        Check(rejected, "A fire must use a source matching its class");

        var growth = Fire();
        growth.Advance(15, Passage(false));
        Check(Math.Abs(growth.GrowthSizeCm - 40) < 1e-7 && Math.Abs(growth.SizeCm - 40) < 1e-7,
            "A small fire doubles in size after 15 seconds");
        growth.Advance(15, Passage(false));
        Check(Math.Abs(growth.GrowthSizeCm - 80) < 1e-7, "The next 15 seconds double size again");
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
        late.Advance(310, Passage(false));
        Check(!late.IsBurning(18), "A closed room remains protected even after a long wait");
        late.Advance(1, Passage(true));
        Check(late.IsBurning(18), "Numerical growth limits must not stop a mature fire spreading after a late opening");

        var single = Fire(); var sliced = Fire();
        single.Advance(65, Passage(true));
        for (int i = 0; i < 650; i++) sliced.Advance(0.1, Passage(true));
        Check(Math.Abs(single.GrowthSizeCm - sliced.GrowthSizeCm) < 1e-6, "Growth is independent of frame slicing");
        for (int cell = 0; cell < 35; cell++)
            Check(single.IsBurning(cell) == sliced.IsBurning(cell), "Spread is independent of frame slicing");

        var corner = new FireSimulation(2, 2, 0.5, 0, FireType.B, FireLocation.PaintBottle);
        corner.Advance(180, new[] { true, false, false, true });
        Check(!corner.IsBurning(3), "Fire cannot cut diagonally through a sealed corner");
        var edge = new FireSimulation(3, 2, 0.5, 2, FireType.C, FireLocation.Hob);
        edge.Advance(180, new[] { false, false, true, true, false, false });
        Check(!edge.IsBurning(3) && !edge.IsBurning(-1) && !edge.IsBurning(6), "Fire cannot wrap rows or leave the grid");
        Console.WriteLine("Passed " + checks + " fire simulation checks.");
    }
}
