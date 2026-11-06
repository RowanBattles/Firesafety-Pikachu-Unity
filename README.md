# Fire Safety

Unity **6000.6.7f1**, Universal Render Pipeline, New Input System.

## Scenes and modes

Open **Assets/Scenes/MenuPage.unity** and press **Play**. The illustrated menu
offers **Game**, **Tutorial** and **Dev**. A **Menu** button in each house scene
returns to this page, including during lessons or after a result.

| Scene | Behaviour |
| --- | --- |
| MenuPage | Startup page with the three mode choices and original menu artwork. |
| GameScene | Starts one random A/B/C/F fire automatically; class A randomly uses the table or bed. Each door/window has a random open/closed state, except the bottom front escape door, which always starts closed. Play again rolls a fresh scenario. |
| TutorialScene | Guided, hands-on lessons with controlled fires, a smoke drill, explained decisions and a final real-time escape. Scores stay hidden throughout. |
| DevScene | The former SampleScene, preserving its asset GUID. Choose the fire class/location manually. The kitchen door and west living-room window start open as before. |

All three house scenes use the same `HousePrototype` geometry, equipment and
player spawn. Only scenario state changes. Open any scene directly in the Editor
to work on that mode; the procedural house appears when Play starts.

All house modes include a surrounding meadow, grass tufts, trees and garden
stepping stones. Scenery uses batched meshes without colliders; the house,
player spawn and exit navigation remain the same. The garden obeys line of sight:
glass windows and open doors reveal it, while solid walls and corners hide it.
Closed glass still admits sight. Windows remain solid to movement and fire.
The fog follows the camera and covers wide aspect ratios as well.

Clicking an object places a filled yellow circle on the object itself, while the player
walks to a safe, reachable interaction position. Floor clicks have their own
destination circle. Cyan tutorial targets retain their ring shape.
Door/window popups show the appropriate Open/Close action
without a redundant status line.

**Game** shows a fire's current size, without revealing its class or pre-warning
that it is too large. Attempt feedback reports whether the fire was extinguished;
points appear only on the final result. **Dev** retains class/size-limit hints,
explanations and its running score. **Tutorial** shows correct/incorrect choices
and explanations, with no visible points even on its final result.

### Tutorial

The tutorial teaches click movement and walk-first interaction, A/B/C/F classes
(and why D is excluded), windows, doors, visibility, containment and ventilation.
It then has you collect the bucket, fill it at the tap, swap it for ABC powder
and retrieve the same water. Follow the cyan ring and the current objective.

Put out a small class A table fire with water, a class B paint fire with ABC
powder, and a class F fryer fire with wet chemical. The extinguisher distance
questions use the existing working-distance rules. A gas-safety
question teaches why a still-fed class C fire requires evacuation. Another
question covers staying low and leaving smoke. Correct decisions show a brief
safety takeaway. Incorrect answers show your choice, the correct answer and a
short explanation before retrying. Feedback cards are compact, and the passive
300×88-pixel objective panel is click-through so floor clicks beneath it work.
No points are displayed.

Picking wet chemical during the ABC swap exercise explains that you swapped the
wrong items, reassures you and advances to retrieving the full bucket. Its water
is preserved at the chosen extinguisher's former position, so neither choice
blocks the walkthrough.

The controlled smoke patch uses the supplied coughing audio. Enter it until the
warning, then leave for clear air to progress. There is no health bar; prolonged
exposure still kills. The final drill starts a growing gas fire: open the front
door and physically walk outside to finish the tutorial.

Practice A/B/F fires stay at 25 cm. Instructions pause hazards and movement;
practice is completed by world actions, rather than a Next button. Fire contact
and prolonged smoke exposure remain fatal. **Retry lesson** restores that
lesson's starting state and equipment. Returning from Menu starts a
new tutorial. Earlier practice fires do not create a missed-opportunity penalty
for the final gas-fire escape.

## Windows build

1. Fetch and select the current game branch, then let Unity finish importing.
2. Open **File > Build Profiles**, select **Windows**, and open **Scene List**.
   Enable **MenuPage**, **GameScene**, **TutorialScene** and **DevScene**, in that
   order. MenuPage must be first. The global list is already configured; if the
   Windows profile overrides it, update that list too or turn off the override.
3. **Fire Safety > Validate Windows Build** checks the active scene list and
   menu artwork and runtime material assets. Windows builds check this
   automatically and also validate the startup components and scenario mode.
4. Build into a fresh output folder and run that new `.exe`. To move the game
   to another laptop, copy the entire build folder, including `_Data` and
   `UnityPlayer.dll`.

House and flame materials live in `Assets/Resources/Materials`, with explicit
URP shader references. Unity includes these shader dependencies in the player;
runtime construction clones the templates and applies the same game colours.
`Assets/link.xml` preserves the mesh and collider types used by
`GameObject.CreatePrimitive`, including the player's capsule and sphere.
The scene's **House Prototype** also references Unity's built-in default
material, retaining the material used during primitive creation.

A static sky/background with no house or class selector means startup did not
reach the generated scene/camera setup. The screenshot alone cannot distinguish
a wrong scene from an initialization exception. Inspect `Player.log` for the
first error or exception after launching the newly built game. On Windows it is
under `%USERPROFILE%\AppData\LocalLow\<Company Name>\<Product Name>\Player.log`.
With the current Player Settings, the folder is
`Unity Technologies\com.unity.template.urp-blank` under `LocalLow`.
The cloud Editor currently has no active licence, so Windows player execution
must be verified on a licensed local Unity installation.

## Play

1. Open `Assets/Scenes/MenuPage.unity` and press **Play**.
2. Choose **Game** for a random fire, **Tutorial** for the walkthrough, or
   **Dev** for the existing class/location selector. In Dev, choose **A**, **B**,
   **C** or **F**. Class A also lets you choose **Table** or **Bed**.
3. Left-click an open spot on the floor. The character walks there, going around
   furniture and through doorways to reach other rooms.
4. Click again while walking to change the destination. The yellow ring marks
   the destination. Clicking a wall, furniture or an unreachable spot is rejected;
   the character continues along any existing valid route.
5. Click an orange door or a blue window. The character walks to a reachable
   spot beside its frame first; only then does its action menu appear. Select
   **Do nothing**, or **Open** / **Close** depending on its current state.
   Press **Escape** or click outside the menu to dismiss it without taking action.
   Dev starts with the **kitchen door** and west **living-room window** open.
   Game randomizes openings, always keeping the front escape door closed.
   Tutorial starts with all openings closed for its exercises.
6. Click the flames to walk to a safe, reachable spot nearby. The fire popup
   shows its current size in centimetres. With an item in your inventory, it also
   offers **Extinguish the fire with <item>**, including for oversized fires.
   Trying a fire of 50 cm or more shows **Fire too big** and deducts 5 points,
   without discharging the item. Game reveals the score only at the end.
   In Game/Dev and the final tutorial escape, fire keeps growing while menus
   and the distance question are open.
7. Find the red **ABC powder extinguisher** beside the living room's west wall,
   or the blue **empty bucket** in the kitchen's northeast corner.
   Click one, walk to it and choose **Pick up**.
   Carrying another item changes this to **Swap items**: the old item is left at
   the new item's exact location, preserving its water/remaining charge.
8. To fill the bucket, enter the kitchen and click the **water tap** on the
   utility sink near the kitchen's southwest corner. Select **Fill bucket** and
   stay there for 4 seconds. **Cancel filling** or **Escape** leaves it empty.
9. For a small class A fire, a filled bucket can put it out directly. With the
   ABC extinguisher, the character first walks to a reachable working position
   3–4 m from the source. Choose its starting-distance range in the question.
10. For a cooking-oil (class F) fire, enter the kitchen and pick up the red
   **wet chemical extinguisher** with the yellow band in the kitchen's north-west
   corner, just inside the door. The character walks to a working position
   3–3.7 m from the fire before the distance question appears (answer 3–4 m).
11. To escape, open the **front door** in the south wall of the living room.
    Click the visible path outside it. The character walks through the door;
    clearing the doorway ends the run and displays the final score.
12. Extinguishing also ends the run with **Good job! Fire extinguished** and
    the final score. Use **Play again** on either result to choose a new scenario.

## Fire scenarios

| Class | Starting location |
| --- | --- |
| A | Coffee table or bed |
| B | Paint bottle in the living room |
| C | Gas hob on the kitchen counter |
| F | Deep fryer on the kitchen counter |

Game/Dev and the final tutorial escape start at **25 cm** and stay at that size without spreading for
the first **20 seconds**. Exponential growth then begins. Ventilation scales
the base 15-second doubling time: sealed rooms grow more slowly, while open
doors and windows accelerate growth and spread.
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
**Caught by the fire**, a final score including a 10-point death deduction and a **Play again** button. The
three-minute timer and calling for help are not part of this showcase yet.

## Ventilation

Each **Dev** round starts with exactly **one open door and one open window**: the
kitchen door and west living-room window. Their visuals, click actions,
navigation and visibility use the open state immediately. Play again restores
this same pair. Game randomizes all opening states except the initially closed
front escape door; Tutorial starts sealed and guides opening/closing exercises.

Ventilation is calculated for the ignition room and any rooms connected to it
through open internal doors. Each open door in that connected area adds **0.35**
to the growth rate; each open window adds **0.4**. A sealed room has a rate of
**0.5**, and the total rate is capped at **3.0**. An open front door adds airflow
without connecting separate rooms via the outside. A window behind a closed
door affects its own room, rather than a fire in an isolated room.

| Situation | Growth rate | Time to double after the initial hold |
| --- | --- | --- |
| All openings in the fire's connected rooms closed | 0.5× | 30 seconds |
| Initial living-room / kitchen open pair | 1.25× | 12 seconds |
| Maximum ventilation | 3× | 5 seconds |

With the initial pair unchanged, a living-room or kitchen fire reaches 50 cm
at about **32 seconds**. The initially closed bedroom reaches 50 cm at about
**50 seconds** if its openings stay closed. Changing an opening changes future
growth immediately: closing never shrinks fire that is already present, and
opening never jumps its size forward. The full 20-second hold applies at every
rate. Closed doors and walls still contain spread; opening a window does not
create a walking exit or let fire grow outside the house.

The simulation integrates growth every frame and resolves spread in fixed
0.05-second steps, preserving rate changes within a step. Visual meshes refresh
at 0.2-second intervals or immediately when another cell ignites. Gameplay
tuning lives in `Fire/FireVentilation.cs`; mode-specific starting states and
random fire choices live in `Gameplay/ScenarioSetup.cs`.

## Escape and final results

The front path is a walkable exit. The player must open the front door and
actually walk through it: clicking outside never teleports the character or
ends a run while still indoors. Walls, closed doors and burning space continue
to block routes. Exterior clicks near the doorstep select a point far enough
outside for the whole character to clear the door. Other exterior walls and
windows are not exits. Fire and smoke stay indoors despite the larger walking
grid. The exterior scenery uses the same visibility mask as the house.
Closed-door navigation and line-of-sight click checks still prevent escape or
interaction through walls; seeing through glass does not create a walking exit.

Escaping earns **+10 points**, added to existing decision points. A recorded
missed opportunity to extinguish deducts **2 points**, with an explanation in
the final result. For example, 0 decision points + 10 escape points − 2 missed
opportunity points gives **8 points**. The result shows each score component.

An opportunity is recorded only after the player can see a fire below 50 cm,
with ready, suitable equipment visible and reachable (or already held), a
reachable working position and a route to the front door. The estimated walk
plus 4 seconds for pickup/question input must fit before the fire reaches
50 cm, using its current ventilation rate and accumulated growth. Active smoke
disqualifies the opportunity. Empty equipment, an unseen
fire, an unsafe agent or a gas fire with its supply still on do not qualify.
ABC powder and the team's wet chemical extinguisher use their existing class
and distance rules. A filled bucket can qualify for class A; an empty one does
not count as ready equipment.

Once recorded, the earlier opportunity remains in the history if the fire
later grows. Feedback explicitly says **Earlier**; it never asks the player to
return to a now-unsafe fire. Each run's result and score are finalized once.

Putting out the fire goes straight to **Good job! Fire extinguished**, retaining
the existing extinguishing/decision points and question feedback. There is no
extra success award on top of those points. Both successful endings and both
death endings in Game/Dev show a final score and **Play again**. Fire and smoke
death each deduct **10 points**, exactly once. Tutorial endings show no score.
The final screen stops
movement, fire/smoke simulation and cough audio. Play again reloads the scene
and resets inventory, contents, score, opportunity history and the fire's
25 cm / 20-second initial hold. Dev returns to class selection; Game rolls a new
fire and new opening states. Tutorial failures retry the current lesson and
completion offers a fresh tutorial run. Menu is available in all modes.

## Inventory, extinguishing and scoring

There is exactly one inventory slot. The carried item is visible beside the
character and named in the small inventory panel (with a running score in Dev
only). Items can only be
picked up after walking within reach; hidden or inaccessible items cannot be
used through closed doors. Swapping exchanges the incoming world item and the
held item at the incoming item's position. Picking up a filled bucket later
does not empty it, and picking up a used extinguisher does not recharge it.

The equipment choices follow the supplied `fire extinguishing.docx`:

| Fire | Filled water bucket | ABC powder extinguisher | Wet chemical extinguisher |
| --- | --- | --- | --- |
| A — solid material | Puts out a small fire | Puts out a small fire after the distance question | Puts out a small fire after the distance question |
| B — burning liquid | Rejected: water can spread burning liquid | Puts out a small fire after the distance question | Rejected: not for burning liquids |
| C — leaking gas | Rejected | Rejected while the gas supply is on | Rejected: not for gas |
| F — cooking oil | Rejected: water can eject burning oil | Rejected: this extinguisher is not F-rated | Puts out a small fire after the distance question |

Gas isolation is not included in this equipment version, so the class C scenario
requires evacuation rather than extinguishing with the supplied items. A generic
extinguisher does not suit every fire class.

Successful extinguishing is allowed only **below 50 cm**, using the current footprint. The
size is checked again when the distance question is answered. The action is
available at every size while an item is held. A deliberate oversized attempt
fails for **−5 points**, with no discharge; automatically cancelling a question
as the fire grows does not charge for a new decision. Waiting does not preserve
eligibility. Empty equipment cannot discharge. An extinguished fire loses its
flames, collider and walking hazard and stays out, then ends the run with the
final score. A successful discharge empties the bucket/extinguisher.

The reference gives **3–4 m** for the ABC powder model used here. This is not a
universal distance for all extinguishers: the team's research gives **3–3.7 m**
for the wet chemical extinguisher, which is the 3–4 m answer in the question. The question offers four choices:
less than 1 m, 1–2 m, 3–4 m and more than 6 m. A correct, completed attempt earns
**+10 points**; a wrong distance or unsafe agent choice earns **−5 points** and
leaves the fire burning without discharging the item. An expired attempt or
empty item gives no points. One answer is scored once, and an out fire cannot
award more points. Cancelling the question makes no attempt and changes no score.
Bucket success also earns +10 points; there is no extinguisher-distance question
for a bucket. Game reports only the outcome and keeps scores for the final
result; Tutorial explains the right/wrong answers without displaying scores.

Filling takes **4 seconds** and requires the same empty bucket and continued
access to the tap. Cancelling, losing access or dying interrupts it without
creating water. Fire and smoke continue during filling, questions and feedback
for unsuccessful attempts; smoke still kills after prolonged exposure. There
is no health bar or smoke-warning panel. Play again resets the run.

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
in fully dense smoke** before **Overcome by smoke** and **Play again**.
Lighter smoke takes longer to become lethal. The simulator continues while
inspecting objects; menus do not protect the character from smoke.

The cough uses the supplied `freesound_community-cough-104521.mp3`, bundled in
`Assets/Resources/Audio`. Playback never overlaps itself. Enable Game-view audio
to hear it. Smoke onset, spread speed, lethal dose and cough volume are
configurable on `SmokeHazard`.

## Room visibility

Inside the house, only the character's current line of sight is revealed. Unseen areas are
covered with opaque **dark gray**, including fire and smoke in hidden rooms.
Closed doors hide the next room. Opening one reveals the view through its
doorway; walls, corners and the opened door leaf still block sight. Connected
rooms are not automatically revealed in full. Previously seen space darkens
again when a door closes or the character moves behind a corner.
Windows cut sight openings in the exterior walls. Both closed and open glass
reveal the garden within their view, with no visibility around corners. These
sight openings do not alter wall colliders, navigation or fire containment.
The visibility overlay follows the character and resizes to cover the full
camera view, preventing scenery outside a fixed fog rectangle from leaking in.

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

The kitchen door and west living-room window start open. Open the bedroom
door before walking into that room. Closed doors block routes; open door leaves also
occupy space, so the character walks around them. The character stops on arrival
before showing the menu. A door cannot open or close onto the character: move clear of it
and try again if the menu asks you to do so.

There are three doors (front, kitchen and bedroom) and four windows mounted on
the outside walls. Both doors and window sashes visibly rotate when toggled.
Windows open outward and remain part of the wall; they are not walking exits.
Each window has a fixed blue sill on the inside: click this visible sill to
close an opened window even when its sash is outside the visible area.
The front door leads to the outside escape path.

The camera looks straight down and keeps the character exactly in the centre.
Destinations snap to a 0.5-metre grid. This prototype contains a living room,
kitchen and bedroom, with an escape path outside the front door. The timer
will follow in a later step.

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
- `Movement/HouseExit`: doorway/path bounds, fully-outside detection and safe
  click destinations near the doorstep and path edges.
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
- `Equipment/ExtinguishingRules`: class/size/contents checks, which fire classes
  each item suits, each extinguisher's correct distance and working range, distance assessment,
  one-shot scoring and consumption. These rules use the supplied reference.
- `Equipment/EquipmentSystem`: pickup swaps, filling, quiz/feedback UI, decision
  score and world reachability checks for a possible extinguishing opportunity.
- `Equipment/EquipmentWorld`, `PickupEquipment`, `WaterTap`: clickable equipment,
  carried visuals and the kitchen sink, using the shared walk-first interaction.
- `Fire/FireSimulation`: engine-independent exponential growth and room/door
  spread model, integrating ventilation over time with fixed spread steps.
- `Fire/FireVentilation`: room connectivity, opening ownership, initial open
  states and airflow-based growth rates.
- `Fire/FireHazard`: draws orange/yellow flames, supplies hazard footprints and
  updates the walking grid as the fire expands.
- `Fire/FireShowcase`: handles class selection, starts the matching scenario,
  advances fire and smoke, detects endings and displays final points / Play again.
- `Gameplay/ScenarioResult`: immutable final score, escape bonus, missed-opportunity
  history and feedback for escaping, extinguishing or dying.
- `Gameplay/SceneCatalog`, `SceneNavigation`: scene names, shared build order,
  menu navigation and protection against input carrying through a scene change.
- `Gameplay/ScenarioSetup`: reproducible random fire/opening choices for Game,
  original Dev defaults and a sealed initial Tutorial house.
- `Gameplay/MenuPageController` and `Resources/UI/MenuArtwork`: illustrated
  startup page and Game/Tutorial/Dev choices.
- `Gameplay/ScenarioPresentation`, `TutorialFeedback`: mode-specific hints,
  score visibility and score-free correct/incorrect decision explanations.
- `Gameplay/TutorialProgress`, `TutorialWalkthrough`: engine-independent lesson
  progression plus world-action observation, practice hazards, targets, quizzes
  and equipment/score checkpoints.
- `Gameplay/ExtinguishingOpportunity`: checks that estimated travel and interaction
  time fit before the strict 50 cm limit, including the initial hold.
  This estimate updates with the current ventilation rate.
- `Smoke/SmokeSimulation`: fast, fixed-step smoke spread through open passages.
- `Smoke/SmokeExposure`: accumulated inhalation dose, cough warning and death.
- `Smoke/SmokeHazard`: smoke particles, density clipping, exposure and cough audio.
- `Resources/Audio`: the supplied coughing recording, included in player builds.
- `Visibility/VisibilityMap`: engine-independent wall/door line-of-sight checks.
- `Visibility/FogOfWar`: dark gray visibility overlay and hidden-click filtering.
- `Resources/Effects`: smoke and visibility shaders, included through Resources
  so runtime materials also work in player builds.
- `Rendering/RuntimeMaterials` and `Resources/Materials`: explicit shader
  dependencies for the procedural house and flames, retained in standalone builds.
- `Rendering/OutdoorEnvironment`, `WorldRing`: meadow/tree meshes and shared,
  collider-free destination/object/tutorial rings.
- `Editor/WindowsBuildValidation`: active-profile scene/material checks and a
  build-time check for an enabled scene startup component.

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
`Tests/HouseExitTests.cs` runs with `HouseExit.cs`, `GridPathfinder.cs`,
`VisibilityMap.cs`, `FireSimulation.cs` and `SmokeSimulation.cs`. It checks
open/closed/blocked escape routes, doorstep clicks, full-body clearance,
visibility and keeping hazards off the exterior path.
`Tests/ScenarioResultTests.cs` runs with `ScenarioResult.cs`,
`ExtinguishingOpportunity.cs`, `InventoryState.cs`, `ExtinguishingRules.cs` and
`FireSimulation.cs`. It checks final score breakdowns, one-time awards,
opportunity timing/class/contents rules, earlier-opportunity history and replay.
`Tests/FireVentilationTests.cs` runs with `FireVentilation.cs`, `FireSimulation.cs`,
`InventoryState.cs`, `ExtinguishingRules.cs` and `ExtinguishingOpportunity.cs`.
It checks the initial pair, room isolation/connectivity, slower closed-room
growth, faster spread, continuous size across toggles, partial-step rate changes,
the 20-second hold and ventilation-aware opportunity timing.
`Tests/ScenarioSetupTests.cs` runs with `SceneCatalog.cs`, `ScenarioSetup.cs`,
`FireVentilation.cs` and `FireSimulation.cs`. It checks legal random fire/fuel
pairs, all four classes and both A sites, varying openings, deterministic seeds,
unchanged room connections, closed exit doors and mode-specific defaults.
`Tests/TutorialProgressTests.cs` runs with `TutorialProgress.cs`. It checks the
complete lesson sequence, action/briefing gates, quiz scoring, no skipped drills
or repeated completion awards, checkpoint entry and coughing before leaving
smoke, including both successful and mistaken extinguisher-swap paths.
`Tests/ScenarioPresentationTests.cs` checks Game's hidden hints/running score,
Tutorial's hidden final score, outcome-only Game feedback and explained Tutorial
answers. All eleven suites can be compiled separately with all scripts that do
not reference `UnityEngine`.
Also check the following in the Unity Editor:

- Open MenuPage, choose each of its three buttons, return with Menu, and repeat.
  Build with MenuPage first and confirm the same navigation in the player.
- Inspect the garden at different aspect ratios and from the exit: grass and
  trees should be visible through glass and open doors, with solid walls and
  corners blocking the view. Hidden rooms must remain dark. Outdoor
  scenery must never intercept floor/object clicks or create an alternate exit.
- Click each door/window/extinguisher from a distance and nearby: a filled yellow
  circle should mark the object, with no floor-destination marker during its approach.
  Cancel, retarget and let a fire change navigation: the ring must remain on the
  intended object until the interaction is dismissed.
- Read the compact tutorial objective and click the floor underneath it: the
  character should still move. Correct-decision cards should show a short safety
  takeaway. Check that Menu text is centred, the inventory heading has no slot
  suffix, and filling shows only the instruction and remaining time.
- In Game, inspect every class and an oversized fire: show only current size,
  retain the extinguish action, and reveal the 5-point oversized-attempt deduction
  only on the final score. Check both death types deduct ten exactly once.
- In Tutorial, choose wet chemical during the ABC swap: review the wrong-swap
  feedback, retrieve the same full bucket and continue. Check correct/incorrect
  feedback after choices and no visible score on HUD, feedback or results.
- Replay Game repeatedly: fire class/site and opening states must vary; the
  front door always starts closed and house geometry/player spawn never move.
- Complete the Tutorial: follow all opening, inventory and A/B/F exercises;
  try wrong and right quiz answers, hear coughing and leave the smoke patch,
  then escape from the final gas fire. Instructions should pause the scene.
- Die in a tutorial fire or smoke drill and use Retry lesson. Verify the same
  lesson's instructions and appropriate equipment return, with no visible score.
  Leave through Menu and re-enter Tutorial: it must start at Welcome.
- Start and replay each **Dev** scenario: verify exactly the kitchen door and west
  living-room window are open, with **Close** as their action and the kitchen
  already accessible. The bedroom and front door must still start closed.
- Keep a small living-room fire running. Close the initially open window and
  kitchen door: its future growth must slow. Open them again: growth must speed
  up, with no sudden resizing. Check the initial hold remains a full 20 seconds.
- Keep the bedroom door closed and open a living-room window: the bedroom fire
  must keep its sealed-room rate. Open the bedroom door to connect the airflow.
- With the front door closed, click outside: no escape should occur. Open it,
  click the visible path and verify the character walks through before the
  final **You escaped!** screen appears. Check doorstep and path-edge clicks too.
- Try routes from the kitchen and bedroom with their doors open and closed,
  and with fire blocking the exit. There must be no wall or hazard bypass.
- Escape from an unseen or unsafe fire: verify +10 escape points without a
  missed-opportunity deduction. See a small fire with ready reachable equipment
  and a clear exit first, then escape: verify the −2 deduction and its explanation.
- Extinguish with the bucket, ABC powder and wet chemical: verify the correct
  feedback and final accumulated points, with no duplicate bonus and no need
  to escape afterward. Simulation and coughing stop on final results.
- In **Dev**, use **Play again** after escape, extinguishing, fire death and smoke death.
  Verify the class selector returns, items/doors reset, score returns to zero
  and the next fire starts at 25 cm with its full 20-second hold.
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
  flames disappear and the final result appears. No smoke may start from the
  out fire. The smoke-exposure mechanics must still work in
  a separate, unchecked growing-fire run without showing a health bar.
- Start each class and both A locations: verify the right object burns and
  movement is blocked until selection is finished.
- Inspect a small fire: walk first, display size in cm, **Continue** closes the
  popup and fire continues growing during inspection.
- Close both internal doors: verify kitchen/bedroom fire stays in its room.
- Open a door while fire is developing: verify spread through the gap, without
  flames appearing through the solid wall or outside the house.
- Attempt to close an engulfed doorway: the action must be refused.
- Click a path crossing fire: the character must find a safe route or stay put.
- Let fire reach the character: verify **Play again** returns to class selection
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
  unexposed character after **Play again**.
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
