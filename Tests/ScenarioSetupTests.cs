using System;
using System.Collections.Generic;
using FireSafety.Fire;
using FireSafety.Gameplay;

public static class ScenarioSetupTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }

    public static void Main()
    {
        var types = new HashSet<FireType>();
        var aLocations = new HashSet<FireLocation>();
        var patterns = new HashSet<string>();
        var observedOpen = new HashSet<HouseOpening>();
        var observedClosed = new HashSet<HouseOpening>();
        foreach (ScenarioMode mode in Enum.GetValues(typeof(ScenarioMode)))
        {
            for (int seed = 0; seed < 256; seed++)
            {
                var setup = new ScenarioSetup(mode, new Random(seed));
                var replay = new ScenarioSetup(mode, new Random(seed));
                Check(setup.Mode == mode, "Scenario must keep its requested mode");
                Check(setup.Fire.Type == replay.Fire.Type && setup.Fire.Location == replay.Fire.Location,
                    "A seed must reproduce its fire");
                var fire = new FireSimulation(3, 3, 0.5, 4, setup.Fire.Type, setup.Fire.Location);
                Check(Math.Abs(fire.SizeCm - 25) < 1e-7,
                    "Random scenarios must place each fire on a matching fuel and start at 25 cm");
                Check(!setup.Opening(HouseOpening.FrontDoor).IsOpen,
                    "The escape door must start closed in every mode");
                string pattern = "";
                foreach (HouseOpening id in Enum.GetValues(typeof(HouseOpening)))
                {
                    VentilationOpening opening = setup.Opening(id), original = FireVentilation.InitialState(id);
                    Check(opening.IsDoor == original.IsDoor && opening.FirstRoom == original.FirstRoom
                        && opening.SecondRoom == original.SecondRoom, "Random states must preserve house connections");
                    Check(opening.IsOpen == replay.Opening(id).IsOpen, "A seed must reproduce opening states");
                    if (mode == ScenarioMode.Dev) Check(opening.IsOpen == original.IsOpen, "Dev must retain its existing opening states");
                    if (mode == ScenarioMode.Tutorial) Check(!opening.IsOpen, "Tutorial must start with closed openings for its exercises");
                    if (mode == ScenarioMode.Game)
                    {
                        (opening.IsOpen ? observedOpen : observedClosed).Add(id);
                        pattern += opening.IsOpen ? "1" : "0";
                    }
                }
                if (mode == ScenarioMode.Game)
                {
                    types.Add(setup.Fire.Type); patterns.Add(pattern);
                    if (setup.Fire.Type == FireType.A) aLocations.Add(setup.Fire.Location);
                }
            }
        }
        Check(types.Count == 4, "Game must be able to choose A, B, C and F");
        Check(aLocations.Contains(FireLocation.Table) && aLocations.Contains(FireLocation.Bed), "Class A must use both fuel locations");
        Check(patterns.Count > 16, "Game must vary door/window combinations, not just fire type");
        foreach (HouseOpening id in Enum.GetValues(typeof(HouseOpening)))
        {
            Check(observedClosed.Contains(id), "Every opening must sometimes start closed");
            Check(observedOpen.Contains(id) == (id != HouseOpening.FrontDoor), "Each non-exit opening must sometimes start open");
        }
        Check(SceneCatalog.BuildOrder.Length == 4 && SceneCatalog.BuildOrder[0] == SceneCatalog.Menu,
            "The menu must be the startup scene");
        Console.WriteLine("Scenario setup: " + checks + " checks passed.");
    }
}
