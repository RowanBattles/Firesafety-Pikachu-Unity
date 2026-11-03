using System;
using System.Collections.Generic;
using FireSafety.Movement;
using FireSafety.Visibility;
using FireSafety.Fire;
using FireSafety.Smoke;

public static class HouseExitTests
{
    private const int Width = 40, Height = 40;
    private const float Radius = 0.375f, CellSize = 0.5f;
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }
    private static float X(int cell) => -10f + (cell % Width + 0.5f) * CellSize;
    private static float Z(int cell) => HouseExit.PathEndZ + (cell / Width + 0.5f) * CellSize;
    private static int Cell(float x, float z) => (int)Math.Floor((z - HouseExit.PathEndZ) / CellSize) * Width
        + (int)Math.Floor((x + 10f) / CellSize);
    private static readonly SightObstacle[] Walls = {
        new SightObstacle(-10.15f, 7.85f, 10.15f, 8.15f),
        new SightObstacle(-10.15f, -8, -9.85f, 8), new SightObstacle(9.85f, -8, 10.15f, 8),
        new SightObstacle(-10, -8.15f, -6.4f, -7.85f), new SightObstacle(-3.6f, -8.15f, 10, -7.85f),
        new SightObstacle(-0.15f, -8, 0.15f, -5), new SightObstacle(-0.15f, -3, 0.15f, 3),
        new SightObstacle(-0.15f, 5, 0.15f, 8), new SightObstacle(0, -0.15f, 10, 0.15f) };
    private static SightObstacle Door(bool open) => open
        ? new SightObstacle(-6.48f, -10.8f, -6.32f, -8)
        : new SightObstacle(-6.4f, -8.08f, -3.6f, -7.92f);
    private static bool Overlaps(SightObstacle box, float x, float z, float margin)
        => x >= box.MinX - margin && x <= box.MaxX + margin && z >= box.MinZ - margin && z <= box.MaxZ + margin;
    private static bool[] Walkable(bool doorOpen, bool fireAcrossDoorway = false)
    {
        var mask = new bool[Width * Height];
        for (int cell = 0; cell < mask.Length; cell++)
        {
            float x = X(cell), z = Z(cell);
            mask[cell] = HouseExit.IsWalkableArea(x, z, Radius);
            foreach (SightObstacle wall in Walls) if (Overlaps(wall, x, z, Radius)) mask[cell] = false;
            if (Overlaps(Door(doorOpen), x, z, Radius)) mask[cell] = false;
            if (fireAcrossDoorway && z == -7.75f && x > -6.4f && x < -3.6f) mask[cell] = false;
        }
        return mask;
    }

    public static void Main()
    {
        Check(!HouseExit.HasEscaped(-5, -7.75f, Radius), "Standing inside the doorway is not an escape");
        Check(!HouseExit.HasEscaped(-5, -8.25f, Radius), "The player's whole body must clear the outside of the doorway");
        Check(HouseExit.HasEscaped(-5, -8.5f, Radius), "Clearing the front doorway counts as escaped");
        Check(!HouseExit.HasEscaped(5, -9, Radius) && !HouseExit.HasEscaped(-10.5f, 0, Radius),
            "Other exterior walls and windows cannot act as exits");
        Check(!HouseExit.IsWalkableArea(-8, -9, Radius), "Outdoor navigation must stay on the front path");
        Check(!HouseExit.IsWalkableArea(-5, -12, Radius), "The path's far edge needs player clearance");

        int start = Cell(-4.75f, -3.75f), outside = Cell(-4.75f, -9.75f);
        var path = new List<int>();
        var closed = new GridPathfinder(Width, Height, Walkable(false));
        var open = new GridPathfinder(Width, Height, Walkable(true));
        Check(!closed.TryFindPath(start, outside, path), "A closed front door must prevent escape routing");
        Check(open.TryFindPath(start, outside, path), "An open front door must connect the living room to the outside path");
        bool crossedDoorway = false, clearedDoorway = false;
        foreach (int cell in path)
        {
            Check(HouseExit.IsWalkableArea(X(cell), Z(cell), Radius), "The route must stay in the house or its doorway/path");
            if (Z(cell) == -7.75f || Z(cell) == -8.25f) crossedDoorway = true;
            if (HouseExit.HasEscaped(X(cell), Z(cell), Radius)) clearedDoorway = true;
        }
        Check(crossedDoorway && clearedDoorway, "Escape requires walking through the doorway, rather than teleporting outside");
        Check(!new GridPathfinder(Width, Height, Walkable(true, true)).TryFindPath(start, outside, path),
            "Fire blocking the doorway must prevent an escape route");
        Check(!closed.TryFindPath(start, outside, path), "Closing the door again invalidates a previously open route");

        foreach (float clickedX in new[] { -6.49f, -5f, -3.51f })
            foreach (float clickedZ in new[] { -8.2f, -10f, -11.99f })
            {
                int target = Cell(HouseExit.TargetX(clickedX, Radius, CellSize), HouseExit.TargetZ(clickedZ, Radius, CellSize));
                Check(open.IsWalkable(target) && HouseExit.HasEscaped(X(target), Z(target), Radius),
                    "Clicks near the doorway or path edges must select a reachable point fully outside");
                Check(open.TryFindPath(start, target, path), "Adjusted exterior click destinations must have a route through the open door");
            }

        var closedSight = new List<SightObstacle>(Walls) { Door(false) };
        var openSight = new List<SightObstacle>(Walls) { Door(true) };
        Check(!VisibilityMap.CanSee(-4.75f, -3.75f, -4.75f, -9.75f, closedSight), "Fog must hide the outside behind a closed front door");
        Check(VisibilityMap.CanSee(-4.75f, -3.75f, -4.75f, -9.75f, openSight), "The open doorway must reveal a clickable patch of outside path");
        Check(!VisibilityMap.CanSee(-4.75f, -3.75f, -8, -9.75f, openSight), "Opening the front door must not reveal through the adjacent exterior wall");

        var passage = new bool[Width * Height];
        for (int cell = 0; cell < passage.Length; cell++)
        {
            passage[cell] = HouseExit.IsInsideHouse(X(cell), Z(cell));
            foreach (SightObstacle wall in Walls) if (Overlaps(wall, X(cell), Z(cell), CellSize * 0.5f)) passage[cell] = false;
        }
        var fire = new FireSimulation(Width, Height, CellSize, Cell(-4.75f, -7.25f), FireType.A, FireLocation.Table);
        fire.Advance(340, passage);
        var smoke = new SmokeSimulation(Width, Height, CellSize);
        smoke.Advance(10, passage, fire.IsBurning);
        bool outsideSafe = true, indoorFire = false;
        for (int cell = 0; cell < passage.Length; cell++)
        {
            if (Z(cell) < HouseExit.ThresholdZ)
                outsideSafe &= !fire.IsBurning(cell) && smoke.DensityAt(cell) == 0;
            else indoorFire |= fire.IsBurning(cell);
        }
        Check(indoorFire && outsideSafe, "Expanding the navigation grid must not let fire or smoke grow onto the outside path");
        Console.WriteLine("Passed " + checks + " escape-route, visibility and exterior containment checks.");
    }
}
