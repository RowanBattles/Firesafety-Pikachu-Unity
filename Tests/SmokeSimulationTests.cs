using System;
using FireSafety.Fire;
using FireSafety.Smoke;

public static class SmokeSimulationTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static bool[] Passage(bool open)
    {
        var cells = new bool[55];
        for (int i = 0; i < cells.Length; i++) cells[i] = i % 11 != 5;
        cells[27] = open;
        return cells;
    }

    public static void Main()
    {
        const int source = 24;
        var smoke = new SmokeSimulation(11, 5, 0.5);
        smoke.Advance(3, Passage(true), cell => false);
        Check(smoke.DensityAt(source) == 0, "No smoke is produced before the fire starts emitting");
        smoke.Advance(10, Passage(false), cell => cell == source);
        for (int cell = 0; cell < 55; cell++)
            Check((smoke.DensityAt(cell) > 0) == (cell % 11 < 5), "A closed door must contain smoke");
        smoke.Advance(0.25, Passage(true), cell => cell == source);
        Check(smoke.DensityAt(27) > 0 && smoke.DensityAt(28) == 0, "Opening a door advances smoke one cell at a time");
        smoke.Advance(2, Passage(true), cell => cell == source);
        Check(smoke.DensityAt(32) > 0, "Smoke spreads rapidly into the next room through an open door");
        smoke.Advance(2, Passage(false), cell => cell == source);
        Check(smoke.DensityAt(32) > 0, "Closing a door must not remove smoke already in the other room");

        var shutInTime = new SmokeSimulation(11, 5, 0.5);
        shutInTime.Advance(10, Passage(false), cell => cell == source);
        shutInTime.Advance(0.25, Passage(true), cell => cell == source);
        shutInTime.Advance(5, Passage(false), cell => cell == source);
        Check(shutInTime.DensityAt(28) == 0, "Smoke trapped in a newly closed doorway must not leak through it");

        var fast = new SmokeSimulation(11, 5, 0.5);
        fast.Advance(5, Passage(true), cell => cell == source);
        var fire = new FireSimulation(11, 5, 0.5, source, FireType.A, FireLocation.Table);
        fire.Advance(5, Passage(true));
        Check(fast.DensityAt(30) > 0 && !fire.IsBurning(30), "Smoke must spread much faster than a growing fire");
        var whole = new SmokeSimulation(11, 5, 0.5);
        var sliced = new SmokeSimulation(11, 5, 0.5);
        whole.Advance(3.8, Passage(true), cell => cell == source);
        for (int i = 0; i < 38; i++) sliced.Advance(0.1, Passage(true), cell => cell == source);
        for (int cell = 0; cell < 55; cell++)
            Check(whole.DensityAt(cell) == sliced.DensityAt(cell), "Smoke spread must be independent of frame slicing");
        var corner = new SmokeSimulation(2, 2, 0.5);
        corner.Advance(10, new[] { true, false, false, true }, cell => cell == 0);
        Check(corner.DensityAt(3) == 0, "Smoke must not cut across a sealed diagonal corner");
        var edge = new SmokeSimulation(3, 2, 0.5);
        edge.Advance(10, new[] { false, false, true, true, false, false }, cell => cell == 2);
        Check(edge.DensityAt(3) == 0 && edge.DensityAt(-1) == 0 && edge.DensityAt(6) == 0,
            "Smoke cannot wrap rows or leave the house grid");

        var dose = new SmokeExposure();
        dose.Advance(0.5, 1f);
        Check(!dose.ShouldCough && !dose.IsDead, "Short exposure must not immediately kill or cough");
        dose.Advance(0.5, 1f);
        Check(dose.ShouldCough && !dose.IsDead, "Coughing must warn the player before death");
        dose.Advance(10, 1f);
        Check(!dose.IsDead, "The player must have time to leave dense smoke");
        dose.Advance(1, 1f);
        Check(dose.IsDead && !dose.ShouldCough, "Prolonged smoke exposure is fatal and stops coughing");
        dose.Advance(100, 0f);
        Check(dose.IsDead, "Clear air must not revive a dead player");
        var thin = new SmokeExposure();
        thin.Advance(12, 0.2f);
        Check(!thin.IsDead && Math.Abs(thin.DoseSeconds - 2.4) < 1e-6, "Exposure depends on smoke density");
        thin.Advance(2, 0f);
        Check(!thin.ShouldCough && Math.Abs(thin.DoseSeconds - 1.4) < 1e-6, "Clear air stops coughing and gradually restores breathing");
        thin.Advance(10, 0f);
        Check(thin.DoseSeconds == 0, "Recovery cannot make dose negative");
        var fine = new SmokeExposure(); var coarse = new SmokeExposure();
        coarse.Advance(8, 0.8f);
        for (int i = 0; i < 80; i++) fine.Advance(0.1, 0.8f);
        Check(Math.Abs(fine.DoseSeconds - coarse.DoseSeconds) < 1e-6, "Exposure dose is independent of frame slicing");
        Check(new SmokeExposure().DoseSeconds == 0, "A restarted scenario must begin without smoke exposure");

        Console.WriteLine("Passed " + checks + " smoke and exposure checks.");
    }
}
