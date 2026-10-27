# Fire Safety — house prototype

Unity **6000.6.7f1**, Universal Render Pipeline, New Input System.

## Play

1. Open the project in Unity and open `Assets/Scenes/SampleScene.unity`.
2. Press **Play** and click inside the **Game** window.
3. Left-click an open spot on the floor. The character walks there, going around
   furniture and through doorways to reach other rooms.
4. Click again while walking to change the destination. The yellow dot marks
   the destination. Clicking a wall, furniture or an unreachable spot is rejected;
   the character continues along any existing valid route.
5. Click an orange door or a blue window. The character walks to a reachable
   spot beside its frame first; only then does its action menu appear. Select
   **Do nothing**, or **Open** / **Close** depending on its current state.
   Press **Escape** or click outside the menu to dismiss it without taking action.

The character must be within 0.9 metres of the frame, with no wall, other door
or tall furniture blocking access. Objects in rooms behind closed doors cannot
be used remotely: open the blocking door and enter the room first. If no usable
approach spot can be reached, the click is rejected without opening a popup.
Click another object or the floor to cancel an approach and issue a new order.
Press **Escape** while approaching to cancel and stop. If a door changes during
the approach, the route is recalculated or the interaction is cancelled.

Doors and windows start closed. Open the kitchen and bedroom doors before
walking into those rooms. Closed doors block routes; open door leaves also
occupy space, so the character walks around them. The character stops on arrival
before showing the menu. A door cannot open or close onto the character: move clear of it
and try again if the menu asks you to do so.

There are three doors (front, kitchen and bedroom) and four windows mounted on
the outside walls. Both doors and window sashes visibly rotate when toggled.
Windows open outward and remain part of the wall; they are not walking exits.
The front door is interactive, but the outside path is still decorative.

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

- `HousePrototype`: builds the house and updates walkable grid cells with
  clearance around walls, furniture and door leaves whenever a door changes state.
- `GridPathfinder`: finds a shortest route without cutting diagonally through corners,
  including routes to the nearest reachable interaction spot.
- `ClickToMovePlayer`: handles mouse clicks through the existing New Input System,
  moves the character and displays the destination.
- `TopDownCamera`: follows the character in `LateUpdate` without lag.
- `Interaction/HouseOpenings`: places the door leaves, frames and window sashes.
- `Interaction/OpenableObject`: owns each object's state, hinge rotation,
  fixed interaction frame and open/closed navigation footprints.
- `Interaction/ObjectInteraction`: finds a reachable approach, waits for arrival,
  checks interaction distance and obstruction, then displays the English action
  popup separately from movement. Menu clicks never set a walking destination.

## Validation

`Tests/GridPathfinderTests.cs` is a standalone C# regression check outside
`Assets`. Compile it alongside `Assets/Scripts/Movement/GridPathfinder.cs`
and run the program with a C# compiler/runtime. The checks cover routes through
a doorway, sealed rooms, wall edges, invalid destinations, new route requests
and selecting reachable approach spots rather than inaccessible nearby cells.
Also check the following in the Unity Editor:

- Walk from the living room to the kitchen and bedroom, and around the coffee table.
- With both internal doors closed, verify the character cannot enter either room.
- From far away, click a living-room window: the character should walk to its
  frame before the popup appears. No popup should appear during the walk.
- With the bedroom door closed, click the bedroom window: no remote popup or
  toggle should occur. Open the bedroom door and click the window again: the
  character should walk into the bedroom and show the popup at the window.
- Click a second object or the floor during an approach: the old object should
  never show a delayed popup. **Escape** should cancel the approach and stop.
- Walk next to an object separated by a wall: proximity alone must not allow use.
- Check that opened windows can still be closed from their inside frames.
- Click each door and window: **Open** becomes **Close** after opening, and vice versa.
- Select **Do nothing**, press **Escape** and click outside the menu: none should
  toggle the object or trigger movement.
- Open both internal doors and verify the character can reach both rooms.
- Close a door again and check that the route through it is blocked immediately.
- Stand in a doorway and try to close it: the door should stay open with an
  English message asking you to move away. Move clear and try again.
- Click another room while moving to change the route.
- Click walls, furniture and outside the house: the character stays on the floor.
- Check that the larger character clears doorways and furniture without clipping.
- Check that the popup appears only when an object is selected, with no permanent
  instruction or feedback panel, and that shadows stay short and close to objects.
- Check that the character stays centred at different Game-view aspect ratios.

The cloud machine currently has no activated Unity licence. Standalone C#
compilation and navigation checks do not replace a Play-mode test.
