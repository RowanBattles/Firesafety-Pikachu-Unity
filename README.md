# Fire Safety — movement prototype

Unity **6000.6.7f1**, Universal Render Pipeline, New Input System.

## Play

1. Open the project in Unity and open `Assets/Scenes/SampleScene.unity`.
2. Press **Play** and click inside the **Game** window.
3. Left-click an open spot on the floor. The character walks there, going around
   furniture and through doorways to reach other rooms.
4. Click again while walking to change the destination. The yellow dot marks
   the destination. Clicking a wall, furniture or an unreachable spot is rejected;
   the character continues along any existing valid route.

The camera looks straight down and keeps the character exactly in the centre.
Destinations snap to a 0.5-metre grid. This prototype contains a living room,
kitchen and bedroom. Fire, escape mechanics, the timer and scoring will follow
in later steps; the front path is currently decorative.

The **House Prototype** scene object creates the house and character when you
press **Play**, so they do not appear in the Scene view beforehand. No NavMesh
baking or extra package is required.

The character is scaled up by 25%, with matching navigation clearance around
obstacles. The directional light points almost vertically down (86 degrees)
for short, soft shadows at 30% strength. There is no instruction or movement feedback panel.

## Code

- `HousePrototype`: builds the house and calculates walkable grid cells with
  enough clearance for the character around walls and furniture.
- `GridPathfinder`: finds a shortest route without cutting diagonally through corners.
- `ClickToMovePlayer`: handles mouse clicks through the existing New Input System,
  moves the character and displays the destination.
- `TopDownCamera`: follows the character in `LateUpdate` without lag.

## Validation

`Tests/GridPathfinderTests.cs` is a standalone C# regression check outside
`Assets`. Compile it alongside `Assets/Scripts/Movement/GridPathfinder.cs`
and run the program with a C# compiler/runtime. The checks cover routes through
a doorway, sealed rooms, wall edges, invalid destinations and new route requests.
Also check the following in the Unity Editor:

- Walk from the living room to the kitchen and bedroom, and around the coffee table.
- Click another room while moving to change the route.
- Click walls, furniture and outside the house: the character stays on the floor.
- Check that the larger character clears doorways and furniture without clipping.
- Check that no instruction or feedback panel appears while idle, moving or
  clicking a blocked destination, and that shadows stay short and close to objects.
- Check that the character stays centred at different Game-view aspect ratios.

The cloud machine currently has no activated Unity licence. Standalone C#
compilation and navigation checks do not replace a Play-mode test.
