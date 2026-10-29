using System;
using System.Collections.Generic;
using System.Diagnostics;
using FireSafety.Visibility;

public static class VisibilityTests
{
    private static int checks;
    private static void Check(bool condition, string message)
    { if (!condition) throw new Exception(message); checks++; }

    public static void Main()
    {
        var walls = new List<SightObstacle> {
            new SightObstacle(0f, -10f, 0.2f, -1f), new SightObstacle(0f, 1f, 0.2f, 10f) };
        var closed = new List<SightObstacle>(walls) { new SightObstacle(0f, -1f, 0.2f, 1f) };
        var open = new List<SightObstacle>(walls) { new SightObstacle(0f, -1.2f, 2f, -1f) };
        Check(VisibilityMap.CanSee(-2, 0, -3, 3, closed), "The current room must stay visible");
        Check(!VisibilityMap.CanSee(-2, 0, 2, 0, closed), "A closed door must hide the next room");
        Check(VisibilityMap.CanSee(-2, 0, 0.1f, 0, closed), "The closed door itself must remain visible and clickable");
        Check(VisibilityMap.CanSee(-2, 0, 2, 0, open), "An open doorway must reveal the line of sight into the next room");
        Check(!VisibilityMap.CanSee(-2, 0, 2, 3, open), "An open door must not reveal around the adjacent corner");
        Check(!VisibilityMap.CanSee(-2, 0, 3, -2f, open), "An open door leaf still blocks the space behind it");
        var farWall = new List<SightObstacle>(closed) { new SightObstacle(2, -1, 2.2f, 1) };
        Check(!VisibilityMap.CanSee(-2, 0, 2.1f, 0, farWall), "A distant wall must not reveal through a closed door");
        Check(!VisibilityMap.CanSee(-1, 1, 1, -1, new[] { new SightObstacle(0, 0, 1, 1) }),
            "Sight must not leak diagonally through a sealed corner");
        Check(VisibilityMap.CanSee(-2, 0, -2, 0, closed), "The player position must stay visible");
        Check(VisibilityMap.CanSee(-2, 0, -2, 5, closed), "Parallel rays on the near side of a wall must stay clear");
        Check(!VisibilityMap.CanSee(-2, 0, 2, 0.5f, closed), "Slightly angled rays must still respect a closed door");
        var map = new VisibilityMap(8, 4, -2, -1, 0.5f);
        map.Calculate(-1.25f, 0.25f, closed);
        Check(map.IsVisible(17) && !map.IsVisible(22), "Visibility texture must hide the far room");
        map.Calculate(1.25f, 0.25f, closed);
        Check(!map.IsVisible(17) && map.IsVisible(22), "Previously seen rooms must darken again when blocked");
        Check(!map.IsVisible(-1) && !map.IsVisible(32), "Invalid visibility cells must not reveal anything");

        // A window's moving sash can be behind the exterior wall, but its inner sill must
        // remain a visible click target. Opening it must not reveal through that wall.
        var house = new List<SightObstacle> {
            new SightObstacle(-10.152f, -8, -9.848f, 8),
            new SightObstacle(9.848f, -8, 10.152f, 8),
            new SightObstacle(-10.152f, 7.848f, 10.152f, 8.152f),
            new SightObstacle(-0.152f, -8, 0.152f, 8),
            new SightObstacle(0, -0.152f, 10, 0.152f) };
        Check(VisibilityMap.CanSee(-9.25f, -2, -9.65f, -2, house), "West window sill stays visible inside the wall");
        Check(!VisibilityMap.CanSee(-9.25f, -2, -10.5f, -2, house), "West window must not reveal its outside sash through the wall");
        Check(VisibilityMap.CanSee(-6, 7.25f, -6, 7.65f, house), "Living room north window sill stays visible");
        Check(!VisibilityMap.CanSee(-6, 7.25f, -6, 8.5f, house), "Living room north wall must still hide the outside");
        Check(VisibilityMap.CanSee(4, 7.25f, 4, 7.65f, house), "Bedroom north window sill stays visible from its room");
        Check(!VisibilityMap.CanSee(4, 7.25f, 4, 8.5f, house), "Bedroom north wall must still hide the outside");
        Check(VisibilityMap.CanSee(9.25f, -3.8f, 9.65f, -3.8f, house), "East window sill stays visible inside the wall");
        Check(!VisibilityMap.CanSee(9.25f, -3.8f, 10.5f, -3.8f, house), "East wall must still hide the outside sash");
        Check(!VisibilityMap.CanSee(-5, -4, 4, 7.65f, house), "A window behind a closed bedroom door must stay hidden");
        Check(!VisibilityMap.CanSee(-5, -4, 9.65f, -3.8f, house), "A window behind a closed kitchen door must stay hidden");

        var full = new VisibilityMap(256, 256, -16, -16, 0.125f);
        var timer = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++) full.Calculate(-2 + i * 0.1f, 0, closed);
        timer.Stop();
        Console.WriteLine("Passed " + checks + " visibility checks; 256x256 mask average " + (timer.Elapsed.TotalMilliseconds / 10).ToString("0.0") + " ms.");
    }
}
