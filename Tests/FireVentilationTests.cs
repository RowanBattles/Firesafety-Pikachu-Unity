using System;
using System.Collections.Generic;
using FireSafety.Fire;
using FireSafety.Equipment;
using FireSafety.Gameplay;

public static class FireVentilationTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static bool Near(double actual, double expected) => Math.Abs(actual - expected) < 1e-7;
    private static List<VentilationOpening> Openings(bool initial = false)
    {
        var result = new List<VentilationOpening>();
        foreach (HouseOpening id in Enum.GetValues(typeof(HouseOpening)))
        {
            VentilationOpening opening = FireVentilation.InitialState(id);
            result.Add(new VentilationOpening(opening.FirstRoom, opening.SecondRoom, opening.IsDoor, initial && opening.IsOpen));
        }
        return result;
    }
    private static void Toggle(List<VentilationOpening> openings, HouseOpening id, bool open)
    {
        VentilationOpening old = openings[(int)id];
        openings[(int)id] = new VentilationOpening(old.FirstRoom, old.SecondRoom, old.IsDoor, open);
    }
    private static FireSimulation Fire(double rate = 1)
    {
        var fire = new FireSimulation(7, 5, 0.5, 15, FireType.A, FireLocation.Table);
        fire.SetGrowthRateMultiplier(rate);
        return fire;
    }
    private static bool[] Passage(bool doorOpen = true)
    {
        var cells = new bool[35];
        for (int i = 0; i < cells.Length; i++) cells[i] = i % 7 != 3;
        cells[17] = doorOpen;
        return cells;
    }

    public static void Main()
    {
        var defaults = Openings(true);
        int doors = 0, windows = 0;
        foreach (VentilationOpening opening in defaults)
            if (opening.IsOpen) { if (opening.IsDoor) doors++; else windows++; }
        Check(doors == 1 && windows == 1, "Every fresh run must have exactly one open door and one open window");
        Check(defaults[(int)HouseOpening.KitchenDoor].IsOpen && defaults[(int)HouseOpening.LivingRoomWindow].IsOpen,
            "The kitchen door and west living-room window are the initial open pair");
        Check(Near(FireVentilation.GrowthRate(HouseRoom.LivingRoom, defaults), 1.25)
            && Near(FireVentilation.GrowthRate(HouseRoom.Kitchen, defaults), 1.25),
            "The initial open pair must ventilate the connected living room and kitchen");
        Check(Near(FireVentilation.GrowthRate(HouseRoom.Bedroom, defaults), 0.5),
            "The closed bedroom must not inherit ventilation through its closed door");
        Toggle(defaults, HouseOpening.LivingRoomWindow, false);
        Check(Near(FireVentilation.GrowthRate(HouseRoom.Kitchen, defaults), 0.85), "Closing the connected window must slow growth");
        Toggle(defaults, HouseOpening.KitchenDoor, false);
        foreach (HouseRoom room in new[] { HouseRoom.LivingRoom, HouseRoom.Kitchen, HouseRoom.Bedroom })
            Check(Near(FireVentilation.GrowthRate(room, defaults), 0.5), "Closing all openings leaves slow growth in every room");

        var rooms = Openings();
        Toggle(rooms, HouseOpening.BedroomWindow, true);
        Check(Near(FireVentilation.GrowthRate(HouseRoom.Bedroom, rooms), 0.9), "A room's own open window increases its growth rate");
        Check(Near(FireVentilation.GrowthRate(HouseRoom.LivingRoom, rooms), 0.5), "A window behind a closed bedroom door must not accelerate the living-room fire");
        Toggle(rooms, HouseOpening.BedroomDoor, true);
        Check(Near(FireVentilation.GrowthRate(HouseRoom.LivingRoom, rooms), 1.25), "An open internal door connects the other room's open window");
        Toggle(rooms, HouseOpening.KitchenDoor, true);
        Check(Near(FireVentilation.GrowthRate(HouseRoom.Kitchen, rooms), 1.6), "Ventilation must cross a chain of open internal doors");
        rooms.Reverse();
        Check(Near(FireVentilation.GrowthRate(HouseRoom.Kitchen, rooms), 1.6), "Connected-room airflow must not depend on opening-list order");
        rooms = Openings(); Toggle(rooms, HouseOpening.FrontDoor, true); Toggle(rooms, HouseOpening.BedroomWindow, true);
        Check(Near(FireVentilation.GrowthRate(HouseRoom.LivingRoom, rooms), 0.85), "The open front door adds ventilation without connecting closed rooms through outdoors");
        Check(Near(FireVentilation.GrowthRate(HouseRoom.Bedroom, rooms), 0.9), "The exterior must not act as a shortcut between rooms");
        rooms = Openings();
        foreach (HouseOpening id in Enum.GetValues(typeof(HouseOpening))) Toggle(rooms, id, true);
        Check(Near(FireVentilation.GrowthRate(HouseRoom.LivingRoom, rooms), 3), "Fully open ventilation is capped at the gameplay maximum");
        Check(FireVentilation.RoomAt(-4.75f, 1.25f) == HouseRoom.LivingRoom
            && FireVentilation.RoomAt(5.25f, 4.25f) == HouseRoom.Bedroom
            && FireVentilation.RoomAt(8.25f, -6.75f) == HouseRoom.Kitchen
            && FireVentilation.RoomAt(-5, -9) == HouseRoom.Outside, "Each ignition site must select its actual room");

        foreach (double rate in new[] { 0.5, 1.25, 3.0 })
        {
            var held = Fire(rate); held.Advance(20, Passage());
            Check(Near(held.SizeCm, 25) && held.BurningCellCount == 1, "Ventilation must preserve the full 20-second initial hold");
            held.Advance(10, Passage());
            Check(Near(held.GrowthSizeCm, 25 * Math.Pow(2, 10 * rate / 15)), "Ventilation scales exponential growth only after the hold");
        }
        var slow = Fire(0.5); var fast = Fire(3);
        slow.Advance(55, Passage()); fast.Advance(55, Passage());
        Check(fast.BurningCellCount > slow.BurningCellCount, "More ventilation must accelerate spread as well as source size");

        var changed = Fire(3);
        changed.Advance(19.99, Passage()); changed.SetGrowthRateMultiplier(0.5); changed.Advance(0.03, Passage());
        Check(Near(changed.GrowthElapsedSeconds, 0.01), "A rate switch crossing the hold boundary must weight only post-hold time");
        changed.SetGrowthRateMultiplier(2); changed.Advance(7.007, Passage());
        Check(Near(changed.GrowthElapsedSeconds, 14.024), "Opening changes accumulate growth time rather than recalculating the fire's past");
        double sizeBeforeClosing = changed.SizeCm;
        changed.SetGrowthRateMultiplier(0.5);
        Check(Near(changed.SizeCm, sizeBeforeClosing), "Closing a window must not shrink existing fire");
        changed.SetGrowthRateMultiplier(3);
        Check(Near(changed.SizeCm, sizeBeforeClosing), "Opening a door must not instantly enlarge existing fire");

        var whole = Fire(1.25); var sliced = Fire(1.25);
        whole.Advance(48.137, Passage());
        for (int i = 0; i < 4813; i++) sliced.Advance(0.01, Passage());
        sliced.Advance(0.007, Passage());
        bool sameCells = true;
        for (int cell = 0; cell < 35; cell++) sameCells &= whole.IsBurning(cell) == sliced.IsBurning(cell);
        Check(Near(whole.GrowthSizeCm, sliced.GrowthSizeCm) && sameCells, "Ventilated growth and spread must be independent of frame slicing");
        var fragmented = Fire(3); fragmented.Advance(19.99, Passage()); fragmented.SetGrowthRateMultiplier(0.5);
        for (int i = 0; i < 3; i++) fragmented.Advance(0.01, Passage());
        fragmented.SetGrowthRateMultiplier(2);
        for (int i = 0; i < 700; i++) fragmented.Advance(0.01, Passage());
        fragmented.Advance(0.007, Passage());
        Check(Near(fragmented.SizeCm, changed.SizeCm), "Changing ventilation during a partial simulation step must preserve earlier contributions");

        var spreadingWhole = Fire(1.25); var spreadingSliced = Fire(1.25);
        spreadingWhole.Advance(47.913, Passage()); spreadingSliced.Advance(47.913, Passage());
        spreadingWhole.SetGrowthRateMultiplier(0.5); spreadingSliced.SetGrowthRateMultiplier(0.5);
        spreadingWhole.Advance(0.087, Passage());
        for (int i = 0; i < 8; i++) spreadingSliced.Advance(0.01, Passage());
        spreadingSliced.Advance(0.007, Passage());
        spreadingWhole.SetGrowthRateMultiplier(3); spreadingSliced.SetGrowthRateMultiplier(3);
        spreadingWhole.Advance(2.157, Passage());
        for (int i = 0; i < 2157; i++) spreadingSliced.Advance(0.001, Passage());
        sameCells = true;
        for (int cell = 0; cell < 35; cell++) sameCells &= spreadingWhole.IsBurning(cell) == spreadingSliced.IsBurning(cell);
        Check(Near(spreadingWhole.SizeCm, spreadingSliced.SizeCm) && sameCells,
            "Rate changes partway through active spread must preserve both pending heat and frame independence");

        var contained = Fire(3); contained.Advance(340, Passage(false));
        bool staysContained = true;
        for (int cell = 0; cell < 35; cell++) staysContained &= contained.IsBurning(cell) == (cell % 7 < 3);
        Check(staysContained, "Extra ventilation must not bypass walls or closed doors");
        contained.Advance(0.05, Passage());
        Check(contained.IsBurning(17) && !contained.IsBurning(18), "Mature ventilated fire still advances only one cell per simulation step through a late-opened door");
        var outFire = Fire(3); outFire.Advance(10, Passage()); outFire.TryExtinguish();
        outFire.SetGrowthRateMultiplier(0.5); outFire.Advance(500, Passage());
        Check(outFire.IsExtinguished && outFire.SizeCm == 0, "Ventilation changes cannot reignite an extinguished fire");
        foreach (double invalid in new[] { 0, -1, double.NaN, double.PositiveInfinity })
        {
            bool rejected = false;
            try { Fire().SetGrowthRateMultiplier(invalid); } catch (ArgumentOutOfRangeException) { rejected = true; }
            Check(rejected, "An invalid ventilation rate must be rejected");
        }

        var powder = new EquipmentItem(EquipmentKind.AbcPowderExtinguisher);
        var initial = Fire(1.25);
        Check(ExtinguishingOpportunity.HasTimeToUse(initial, powder, 31.9)
            && !ExtinguishingOpportunity.HasTimeToUse(initial, powder, 32), "Opportunity scoring must use the initial ventilated 32-second limit");
        var shut = Fire(0.5);
        Check(ExtinguishingOpportunity.HasTimeToUse(shut, powder, 49.9)
            && !ExtinguishingOpportunity.HasTimeToUse(shut, powder, 50), "Closing openings gives the player more time to reach and use equipment");
        initial.Advance(20, Passage()); initial.SetGrowthRateMultiplier(3);
        Check(ExtinguishingOpportunity.HasTimeToUse(initial, powder, 4.9)
            && !ExtinguishingOpportunity.HasTimeToUse(initial, powder, 5), "A new opening immediately updates the remaining opportunity time");
        initial.Advance(2.5, Passage()); initial.SetGrowthRateMultiplier(0.5);
        Check(ExtinguishingOpportunity.HasTimeToUse(initial, powder, 14.9)
            && !ExtinguishingOpportunity.HasTimeToUse(initial, powder, 15.1), "Closing after growth must use the accumulated fire size and the new slower rate");
        Console.WriteLine("Passed " + checks + " startup, ventilation, dynamic growth and scoring checks.");
    }
}
