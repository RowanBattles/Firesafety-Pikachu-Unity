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

        // Sills on the near side remain visible. Solid wall sections still block outside sight.
        var house = new List<SightObstacle> {
            new SightObstacle(-10.152f, -8, -9.848f, 8),
            new SightObstacle(9.848f, -8, 10.152f, 8),
            new SightObstacle(-10.152f, 7.848f, 10.152f, 8.152f),
            new SightObstacle(-0.152f, -8, 0.152f, 8),
            new SightObstacle(0, -0.152f, 10, 0.152f) };
        Check(VisibilityMap.CanSee(-9.25f, -2, -9.65f, -2, house), "West window sill stays visible inside the wall");
        Check(!VisibilityMap.CanSee(-9.25f, -2, -10.5f, -2, house), "A solid west wall section blocks outside sight");
        Check(VisibilityMap.CanSee(-6, 7.25f, -6, 7.65f, house), "Living room north window sill stays visible");
        Check(!VisibilityMap.CanSee(-6, 7.25f, -6, 8.5f, house), "Living room north wall must still hide the outside");
        Check(VisibilityMap.CanSee(4, 7.25f, 4, 7.65f, house), "Bedroom north window sill stays visible from its room");
        Check(!VisibilityMap.CanSee(4, 7.25f, 4, 8.5f, house), "Bedroom north wall must still hide the outside");
        Check(VisibilityMap.CanSee(9.25f, -3.8f, 9.65f, -3.8f, house), "East window sill stays visible inside the wall");
        Check(!VisibilityMap.CanSee(9.25f, -3.8f, 10.5f, -3.8f, house), "East wall must still hide the outside sash");
        Check(!VisibilityMap.CanSee(-5, -4, 4, 7.65f, house), "A window behind a closed bedroom door must stay hidden");
        Check(!VisibilityMap.CanSee(-5, -4, 9.65f, -3.8f, house), "A window behind a closed kitchen door must stay hidden");

        var roomWalls = new[] { new SightObstacle(-5.15f,-4,-4.85f,4), new SightObstacle(4.85f,-4,5.15f,4),
            new SightObstacle(-5.15f,3.85f,5.15f,4.15f), new SightObstacle(-5.15f,-4.15f,5.15f,-3.85f) };
        var windows = new[] { new SightObstacle(4.4f,-1,5.6f,1) };
        var glazedRoom = new List<SightObstacle>();
        foreach (SightObstacle wall in roomWalls) VisibilityMap.AddWallWithWindows(wall, windows, glazedRoom);
        Check(VisibilityMap.CanSee(0,0,12,0,glazedRoom), "A glass window reveals the garden directly beyond it");
        Check(VisibilityMap.CanSee(0,0,12,1.5f,glazedRoom), "The view through glass widens naturally beyond the window");
        Check(!VisibilityMap.CanSee(0,0,12,4,glazedRoom), "The view must stop where a ray misses the window opening");
        Check(!VisibilityMap.CanSee(0,0,-12,0,glazedRoom) && !VisibilityMap.CanSee(0,0,0,12,glazedRoom),
            "Adding one window must not reveal the garden behind other walls");
        Check(!VisibilityMap.CanSee(0,0,12,12,glazedRoom), "A window must not let the player see around a house corner");
        var closedRoom = new List<SightObstacle>(glazedRoom) { new SightObstacle(-.1f,-4,.1f,4) };
        Check(!VisibilityMap.CanSee(-2,0,12,0,closedRoom), "A window in a room behind a closed door remains hidden");
        var connected = new List<SightObstacle>(glazedRoom) { new SightObstacle(-.1f,-4,.1f,-1), new SightObstacle(-.1f,1,.1f,4) };
        Check(VisibilityMap.CanSee(-2,0,12,0,connected), "An open internal door admits sight through the next room's window");
        var twoWindows = new[] { new SightObstacle(-2.6f,3.4f,-1.4f,4.6f), new SightObstacle(1.4f,3.4f,2.6f,4.6f) };
        var gardenView = new List<SightObstacle>();
        foreach (SightObstacle wall in roomWalls) VisibilityMap.AddWallWithWindows(wall, twoWindows, gardenView);
        Check(VisibilityMap.CanSee(0,0,-4,8,gardenView) && VisibilityMap.CanSee(0,0,4,8,gardenView),
            "Two windows on one wall must each reveal their own view");
        Check(!VisibilityMap.CanSee(0,0,0,8,gardenView), "The wall between windows must still hide the garden");
        Check(!VisibilityMap.CanSee(0,0,12,0,gardenView), "A north window must not cut an unrelated east wall");
        var partial = new List<SightObstacle>();
        VisibilityMap.AddWallWithWindows(roomWalls[1], new[] { new SightObstacle(4.95f,-1,5.05f,1) }, partial);
        Check(!VisibilityMap.CanSee(0,0,12,0,partial), "A gap that does not cross a wall's full thickness must not reveal through it");
        var movingView = new VisibilityMap(8,4,-4,-2,1);
        movingView.Calculate(0,0,glazedRoom,6,-2,1);
        Check(movingView.IsVisible(23), "A moving camera's visibility area must reveal distant scenery through the window");
        movingView.Calculate(-2,0,closedRoom,6,-2,1);
        Check(!movingView.IsVisible(23), "A previously revealed exterior must darken behind a closed internal door");
        foreach (float aspect in new[] { 0.5f,1f,16f/9f,32f/9f,5f })
        {
            float extent = VisibilityMap.ViewExtent(8,aspect);
            Check(extent >= 16*aspect + 4 && extent >= 20,
                "Fog must cover the full camera view plus a guard band, including ultrawide screens");
        }

        var full = new VisibilityMap(256, 256, -16, -16, 0.125f);
        var timer = Stopwatch.StartNew();
        for (int i = 0; i < 10; i++) full.Calculate(-2 + i * 0.1f, 0, closed);
        timer.Stop();
        Console.WriteLine("Passed " + checks + " visibility checks; 256x256 mask average " + (timer.Elapsed.TotalMilliseconds / 10).ToString("0.0") + " ms.");
    }
}
