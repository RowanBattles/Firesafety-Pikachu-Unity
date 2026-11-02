# Fire Safety — equipment and extinguishing showcase

Unity **6000.6.7f1**, Universal Render Pipeline, New Input System.

## Play

1. Open the project in Unity and open `Assets/Scenes/SampleScene.unity`.
2. Press **Play**, click inside the **Game** window and choose fire class **A**,
   **B**, **C** or **F**. Class A also lets you choose **Table** or **Bed**.
3. Left-click an open spot on the floor. The character walks there, going around
   furniture and through doorways to reach other rooms.
4. Click again while walking to change the destination. The yellow dot marks
   the destination. Clicking a wall, furniture or an unreachable spot is rejected;
   the character continues along any existing valid route.
5. Click an orange door or a blue window. The character walks to a reachable
   spot beside its frame first; only then does its action menu appear. Select
   **Do nothing**, or **Open** / **Close** depending on its current state.
   Press **Escape** or click outside the menu to dismiss it without taking action.
6. Click the flames to walk to a safe, reachable spot nearby. The fire popup
   shows its current size in centimetres. With an item in your inventory and a
   fire smaller than 50 cm, it also offers **Extinguish the fire with <item>**.
   The fire keeps growing while menus and the distance question are open.
7. Find the red **ABC powder extinguisher** and blue **empty bucket** in the
   southern part of the living room. Click one, walk to it and choose **Pick up**.
   Carrying another item changes this to **Swap items**: the old item is left at
   the new item's exact location, preserving its water/remaining charge.
8. To fill the bucket, open the kitchen door, enter and click the **water tap** on the
   utility sink near the kitchen's southwest corner. Select **Fill bucket** and
   stay there for 4 seconds. **Cancel filling** or **Escape** leaves it empty.
9. For a small class A fire, a filled bucket can put it out directly. With the
   ABC extinguisher, the character first walks to a reachable working position
   3–4 m from the source. Choose its starting-distance range in the question.

## Fire scenarios

| Class | Starting location |
| --- | --- |
| A | Coffee table or bed |
| B | Paint bottle in the living room |
| C | Gas hob on the kitchen counter |
| F | Deep fryer on the kitchen counter |

Every scenario starts at **25 cm** and stays at that size without spreading for
the first **20 seconds**. Exponential growth then begins, with a 15-second
doubling time: 50 cm at 35 seconds, then 100 cm at 50 seconds.
The spread is resolved on a 0.5-metre grid; the popup
reports the occupied footprint's width in cm. Room boundaries naturally limit
this footprint even though the fire continues developing inside the room.

Furniture can burn. Solid walls stop spread, and a closed door contains fire
in its room. Open doors allow it into connected rooms; the outside of the house
never burns. If a door is opened late, the fire advances through the doorway
rather than instantly appearing throughout the next room. Closing a door does
not remove fire that has already spread to the other side. A burning doorway
cannot be closed or opened safely.

Burning space is excluded from walking and interaction routes. Routes are
rechecked as the fire grows. If fire reaches the character, the demo ends with
**Caught by the fire** and a **Restart demo** button. Stop and restart Play mode
to choose another scenario at any time. Escape, the three-minute timer and
calling for help are not part of this showcase yet.

## Inventory, extinguishing and scoring

There is exactly one inventory slot. The carried item is visible beside the
character and named in the small inventory/score panel. Items can only be
picked up after walking within reach; hidden or inaccessible items cannot be
used through closed doors. Swapping exchanges the incoming world item and the
held item at the incoming item's position. Picking up a filled bucket later
does not empty it, and picking up a used extinguisher does not recharge it.

The two equipment choices follow the supplied `fire extinguishing.docx`:

| Fire | Filled water bucket | ABC powder extinguisher |
| --- | --- | --- |
| A — solid material | Puts out a small fire | Puts out a small fire after the distance question |
| B — burning liquid | Rejected: water can spread burning liquid | Puts out a small fire after the distance question |
| C — leaking gas | Rejected | Rejected while the gas supply is on |
| F — cooking oil | Rejected: water can eject burning oil | Rejected: this extinguisher is not F-rated |

Gas isolation and F-rated equipment are not included in this first equipment
version, so those scenarios require evacuation rather than extinguishing with
the supplied items. A generic extinguisher does not suit every fire class.

An attempt is allowed only **below 50 cm**, using the current footprint. The
size is checked again when the distance question is answered. The action is
hidden once the fire becomes too large; waiting in a question does not preserve
eligibility. Empty equipment cannot discharge. An extinguished fire loses its
flames, collider and walking hazard and stays out, allowing movement through
the cleared space. A successful discharge empties the bucket/extinguisher.

The reference gives **3–4 m** for the ABC powder model used here. This is not a
universal distance for all extinguishers. The question offers four choices:
less than 1 m, 1–2 m, 3–4 m and more than 6 m. A correct, completed attempt earns
**+10 points**; a wrong distance or unsafe agent choice earns **−5 points** and
leaves the fire burning without discharging the item. An expired attempt or
empty item gives no points. One answer is scored once, and an out fire cannot
award more points. Cancelling the question makes no attempt and changes no score.
Bucket success also earns +10 points; there is no extinguisher-distance question
for a bucket. The result explains the decision and the score change.

Filling takes **4 seconds** and requires the same empty bucket and continued
access to the tap. Cancelling, losing access or dying interrupts it without
creating water. Fire and smoke continue during filling, questions and result
panels; smoke still kills after prolonged exposure. There is no health bar or
smoke-warning panel. Restarting the demo resets inventory, contents and score.

## Smoke and coughing

Smoke begins when the fire footprint reaches **at least 200 cm** (the initial
fully-developed threshold). It spreads at **2 metres per second**, much faster
than the developing fire. Walls and closed doors contain it; opening a door
lets it flow into connected rooms. Closing a door does not remove smoke that
has already crossed. Particle shading clips puffs to the smoky cells so their
visual edges do not bleed through walls or closed doors.

The character can walk through smoke, but exposure accumulates with density.
Coughing starts before the lethal threshold and becomes more frequent with
exposure. Coughing audio is the warning: there is no smoke message, exposure
bar or health bar. Clear air stops new coughs and gradually reduces accumulated
exposure. The current default is **12 equivalent seconds
in fully dense smoke** before **Overcome by smoke** and **Restart demo**.
Lighter smoke takes longer to become lethal. The simulator continues while
inspecting objects; menus do not protect the character from smoke.

The cough uses the supplied `freesound_community-cough-104521.mp3`, bundled in
`Assets/Resources/Audio`. Playback never overlaps itself. Enable Game-view audio
to hear it. Smoke onset, spread speed, lethal dose and cough volume are
configurable on `SmokeHazard`.

## Room visibility

Only the character's current line of sight is revealed. Unseen areas are
covered with opaque **dark gray**, including fire and smoke in hidden rooms.
Closed doors hide the next room. Opening one reveals the view through its
doorway; walls, corners and the opened door leaf still block sight. Connected
rooms are not automatically revealed in full. Previously seen space darkens
again when a door closes or the character moves behind a corner.

Hidden floors and objects reject mouse clicks, so the mask cannot be bypassed
to walk blindly into a hidden room or inspect an unseen fire. Doors on the
visible boundary remain visible and clickable. The overlay refreshes as the
character moves and immediately after a door changes state.

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
Each window has a fixed blue sill on the inside: click this visible sill to
close an opened window even when its sash is outside the visible area.
The front door is interactive, but the outside path is still decorative.

The camera looks straight down and keeps the character exactly in the centre.
Destinations snap to a 0.5-metre grid. This prototype contains a living room,
kitchen and bedroom. Escape mechanics and the timer will follow
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
- `Interaction/WorldInteractable`: shares the walk-first interaction contract
  between doors, windows, equipment, the tap and fire. It supports an optional
  second action and a minimum working distance for extinguisher use.
- `Equipment/InventoryState`: engine-independent single-slot item state and
  timed bucket filling, including contents preserved across swaps.
- `Equipment/ExtinguishingRules`: class/size/contents checks, distance assessment,
  one-shot scoring and consumption. These rules use the supplied reference.
- `Equipment/EquipmentSystem`: pickup swaps, filling, quiz/result UI and score.
- `Equipment/EquipmentWorld`, `PickupEquipment`, `WaterTap`: clickable equipment,
  carried visuals and the kitchen sink, using the shared walk-first interaction.
- `Fire/FireSimulation`: engine-independent exponential growth and room/door
  spread model, with fixed simulation steps to avoid frame-rate-dependent leaks.
- `Fire/FireHazard`: draws orange/yellow flames, supplies hazard footprints and
  updates the walking grid as the fire expands.
- `Fire/FireShowcase`: handles class selection, starts the matching scenario,
  advances fire and smoke and shows the appropriate fire/smoke restart screen.
- `Smoke/SmokeSimulation`: fast, fixed-step smoke spread through open passages.
- `Smoke/SmokeExposure`: accumulated inhalation dose, cough warning and death.
- `Smoke/SmokeHazard`: smoke particles, density clipping, exposure and cough audio.
- `Resources/Audio`: the supplied coughing recording, included in player builds.
- `Visibility/VisibilityMap`: engine-independent wall/door line-of-sight checks.
- `Visibility/FogOfWar`: dark gray visibility overlay and hidden-click filtering.
- `Resources/Effects`: smoke and visibility shaders, included through Resources
  so runtime materials also work in player builds.

## Validation

`Tests/GridPathfinderTests.cs` is a standalone C# regression check outside
`Assets`. Compile it alongside `Assets/Scripts/Movement/GridPathfinder.cs`
and run the program with a C# compiler/runtime. The checks cover routes through
a doorway, sealed rooms, wall edges, invalid destinations, new route requests
and selecting reachable approach spots rather than inaccessible nearby cells.
`Tests/FireSimulationTests.cs` can be compiled separately alongside
`Assets/Scripts/Fire/FireSimulation.cs`. It covers all class/location pairs,
small starts, exponential growth, containment, opening doors late, persistence
after a door closes, frame slicing and sealed corners.
`Tests/SmokeSimulationTests.cs` runs with `FireSimulation.cs`, `SmokeSimulation.cs`,
`SmokeExposure.cs`; it covers smoke containment/speed, frame slicing, exposure
and clear-air recovery.
`Tests/VisibilityTests.cs` runs with `VisibilityMap.cs`; it covers closed/open
doors, corners, the visible door surface and re-hiding previously seen cells.
`Tests/EquipmentTests.cs` runs with `InventoryState.cs`, `ExtinguishingRules.cs`,
`FireSimulation.cs` and `SmokeSimulation.cs`. It covers swaps and preserved
contents, timed/cancelled filling, all class/tool combinations, distance scoring,
the strict 50 cm limit, growth during the question and permanent extinguishing.
Also check the following in the Unity Editor:

- Pick up each item and swap back: exactly one item is carried and the other
  appears at the newly picked item's location. Repeat with a filled bucket and
  a used extinguisher to check that contents are preserved.
- Click the kitchen tap behind the closed door: no remote menu should appear.
  Open the door, approach the tap with the bucket and wait 4 seconds. Verify
  **Cancel filling**, **Escape**, fire death and smoke death interrupt filling.
- With no item, inspect a fire: only **Continue** should appear. With an item,
  verify the extra **Extinguish the fire with ...** action appears below 50 cm.
- Use ABC powder on a small A/B fire: approach to 3–4 m, answer incorrectly for
  −5 without discharging, then correctly for +10 and a permanently extinguished
  fire. Verify no walking destination is created by a quiz or result click.
- Wait in the distance question past 50 cm: the attempt must stop without
  discharging or awarding points. No attempt may bypass walls or closed doors.
- Use a filled bucket on A, B, C and F, and ABC powder on C/F. Check the class
  restrictions and score feedback; water must never put out burning oil.
- Put out a small A fire with the bucket. Verify it is empty afterward, the
  flames disappear, the old hazard no longer blocks navigation and no smoke
  starts from that out fire. The smoke-exposure mechanics must still work in
  a separate, unchecked growing-fire run without showing a health bar.
- Start each class and both A locations: verify the right object burns and
  movement is blocked until selection is finished.
- Inspect a small fire: walk first, display size in cm, **Continue** closes the
  popup and fire continues growing during inspection.
- Leave both internal doors closed: verify kitchen/bedroom fire stays in its room.
- Open a door while fire is developing: verify spread through the gap, without
  flames appearing through the solid wall or outside the house.
- Attempt to close an engulfed doorway: the action must be refused.
- Click a path crossing fire: the character must find a safe route or stay put.
- Let fire reach the character: verify **Restart demo** returns to class selection
  and the next scenario starts small again.
- Start a kitchen fire with the kitchen door closed: the room, its fire and its
  smoke should remain dark gray from the living room; coughing must not start there.
- Open the kitchen door: only the directly visible part of the room should be
  revealed. Check that the kitchen-to-bedroom wall still hides the bedroom even
  when both internal doors are open.
- Close a door again: the room behind it must immediately darken, and clicking
  the hidden room must not create a destination or object popup.
- Let a fire pass the 200 cm threshold: smoke should appear and overtake fire.
- Enter smoke with Game-view audio enabled: verify the supplied recording plays
  without overlapping itself and no smoke message or health bar appears. Move
  to clear air to stop further coughing.
- Remain in smoke: verify **Overcome by smoke**, stopped audio and a fresh,
  unexposed character after **Restart demo**.
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
- Open each window and click its fixed blue inner sill to close it. The sash
  may be hidden outside the house; its sill must remain visible and clickable.
  Windows in rooms behind closed doors must still be hidden and inaccessible.
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
compilation and model checks do not replace Play-mode verification of shaders,
particles, sound playback and the complete input/rendering flow.
