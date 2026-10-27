// Standalone regression checks; outside Assets so this is not compiled into the game.
using System;
using System.Collections.Generic;
using FireSafety.Movement;

public static class GridPathfinderTests
{
    private static int checks;

    private static void Check(bool condition, string message)
    {
        if (!condition) throw new Exception(message);
        checks++;
    }

    public static void Main()
    {
        var cells = new bool[35];
        for (int i = 0; i < cells.Length; i++) cells[i] = true;
        // A wall across the room, with a single doorway at the far end.
        for (int z = 0; z < 4; z++) cells[z * 7 + 3] = false;
        var grid = new GridPathfinder(7, 5, cells);
        var path = new List<int>();
        Check(grid.TryFindPath(8, 12, path), "Route should reach the other room through the door");
        Check(path[0] == 8 && path[path.Count - 1] == 12, "Route should include both endpoints");
        Check(path.Contains(31), "Route must pass through the doorway");
        foreach (int cell in path) Check(cells[cell], "Route must not enter a wall");
        for (int i = 1; i < path.Count; i++)
        {
            int dx = Math.Abs(path[i] % 7 - path[i - 1] % 7);
            int dz = Math.Abs(path[i] / 7 - path[i - 1] / 7);
            Check(dx + dz == 1, "Steps must be adjacent without diagonal corner cutting or row wrapping");
        }
        Check(path.Count == 11, "Route should be a shortest path");
        Check(!grid.TryFindPath(8, 3, path) && path.Count == 0, "Clicking a wall must fail and clear stale search output");
        Check(!grid.TryFindPath(-1, 12, path), "Outside starting cell must be rejected");
        Check(!grid.TryFindPath(8, 35, path), "Outside target must be rejected");
        Check(grid.TryFindPath(8, 8, path) && path.Count == 1, "Clicking the current cell must stop safely");
        Check(grid.TryFindPath(12, 8, path) && path[0] == 12 && path[path.Count - 1] == 8,
            "A second request must replace the previous route");

        cells[31] = false;
        var sealedRoom = new GridPathfinder(7, 5, cells);
        Check(!sealedRoom.TryFindPath(8, 12, path) && path.Count == 0, "A sealed room must be unreachable");
        Check(grid.TryFindPath(8, 12, path), "Grid owns its data independently of caller mutations");
        var corner = new GridPathfinder(2, 2, new[] { true, false, false, true });
        Check(!corner.TryFindPath(0, 3, path), "A diagonal gap between obstacles is not a doorway");
        var edge = new GridPathfinder(3, 2, new[] { false, false, true, true, false, false });
        Check(!edge.TryFindPath(2, 3, path), "Neighbour lookup must never wrap across a row");
        Console.WriteLine("Passed " + checks + " navigation checks.");
    }
}
