# Level (Level1 "The Forest")

Status: the three scripts are written. `Level1.unity` is built (camera, fog, clearings, trees, exit, HUD). Physics layers and the crow's solid collider are not finished, see "Scene setup".

## What it is

The player starts in a small clearing surrounded by fog and walls of trees. A faint "moo!" speech bubble pops up every few seconds and points toward the goal. The crow clears fog to the right and the cow follows. The camera slides along with both animals. On the second screen there is a second clearing with a fruit tree. The crow arrives low on energy, so the players learn the loop: cow rams the tree, fruit drops, crow eats it. Past the tree, the exit is on the right edge of screen 2. When the cow and the crow are both inside it the screen fades to white and Level2 loads (`LevelExit > Next Scene`). The level opens with a fade from white.

## Where it lives

- Scripts: `Assets/Scripts/SharedCamera.cs`, `Assets/Scripts/MooPrompt.cs`, `Assets/Scripts/LevelExit.cs`
- Scene: `Assets/Scenes/Level1.unity` (started as a copy of SampleScene; since the mist swap it is the `FogTest_Mist` scene, see `fog.md`)
- Look objects: `Mist` (two layers of drifting wisps), `Motes` (floating specks), `Global Volume` (post-processing, `Assets/Settings/VP_Mist.asset`), `Crow > Light 2D` (warm glow), `Grass` (swaying tufts, see `grass.md`). Assets: `Assets/Shaders/FogSwirl.shader`, `Assets/Materials/M_FogSwirl.mat`, `M_MoteGlow.mat`, scripts `MistDrift.cs`, `MistMask.cs`.
- Scene objects: `Snake` is inactive (hidden in Level1; snakes start in Level2, see `snakes.md`). `Main Camera` holds `SharedCamera`. `Exit` holds `LevelExit`. `HUD > Fade` is the fade. `HUD > MooBubble` is the moo bubble, part of `PF_HUD` (see below).

## Layout

Two screens side by side, each about 21.35 x 12 units (camera size 6, 16:9). Screen 1 centre is x = 0, screen 2 centre is x = 21.35. The level spans about x -10.7 to 32, y -6 to 6.

- Clearing 1 on screen 1: the start. `Clearing` at (4.64, -0.30) (the same spot as `Ground > Meadow1`), X scale 8.4. Cow start (4.12, -1.16), crow start (5.55, -0.42), both inside it.
- Clearing 2 on screen 2: the fruit tree. `Clearing2` at (21.63, 0.38), X scale 14, second entry in `Fog Grid > Open At Start`. `Tree` (the fruit tree, see `trees.md`) at (21.6, -1.2). The crow reaches it low on energy.
- `Exit` at (31.2, 0): BoxCollider2D, Is Trigger, size 1 x 4.8, in the right-edge gap. Holds `LevelExit`.
- `HUD` (prefab `Assets/Prefabs/PF_HUD`): Screen Space Overlay canvas, Sort Order 100, Canvas Scaler 1920 x 1080. Child `Fade`: full-screen stretched white Image, alpha 0, Raycast Target off.
- `Walls`: 23 `WallTree` prefab instances, placed through the editor bridge from the sketch, scale 0.9 to 1.1. Top row and bottom row follow a corridor: open around clearing 1 (wall edges at y about +4.4 / -4.4), narrow path between the clearings (about +2.7 / -2.7, so about 5.4 tall, room for the cow's feet collider), wide around clearing 2 (about +4.1 / -4.3), then about +2.4 / -2.4 at the right edge, which is the exit gap. The map was scaled 1.2x: each tree moved so its path-facing collider edge is 1.2x further from the level centre; tree size is unchanged. Three trees close the left side at x about -10.7. The owner adjusts by hand from here.
- `Walls_Old` (inactive): the owner's first 6 test trees, kept in case. Safe to delete.

## How it works

Order of events:

1. Scene loads. `LevelExit` sets the fade Image to alpha 1 (white) and fades it to 0 over `Fade Time`. `MooPrompt` starts its cycle at the beginning of a moo.
2. Every frame, `SharedCamera` moves toward the midpoint of the players (limited by `Min Position` / `Max Position`), smoothed by `Smooth Time`.
3. Every physics step, `SharedCamera` clamps each player's Rigidbody2D position into the camera's view, shrunk by `Edge Margin`. This is a hard invisible wall: neither animal can leave the screen, so the cow can't be left behind. (ponytail note in the script: fine for two players on one screen.)
4. The crow clears fog (see `fog.md`), the cow follows, rams the tree, the crow eats (see `players.md`, `trees.md`).
5. Both the cow and the crow are inside the exit trigger. `LevelExit` saves `Next Scene` and starts fading to white. When the fade starts, `LevelExit` begins loading `Next Scene` in the background (`LoadSceneAsync`, activation held); when alpha reaches 1 it lets the scene switch on. One frame's fade step is capped at 0.05 s so a slow frame after a load can't jump the fade. If one animal steps out before that, the fade still finishes: the fade starts once and does not cancel.

`MooPrompt` (moo bubble on `HUD > MooBubble`): timing is `t = Time.timeSinceLevelLoad % Interval` (so a restart opens on a moo too). Alpha rises 0 to 1 over `Fade Time`, holds for `Hold Time`, falls back over `Fade Time`, then stays 0 until the next interval; times `Max Alpha`, written to the `CanvasGroup`. Placement runs in `LateUpdate` (after the smoothed camera): `cam.WorldToScreenPoint(target)`. If the goal is inside the screen shrunk by `Edge Padding`, the bubble sits at that point plus `On Screen Offset` pixels up. If not, the bubble sits on the padded screen edge, along the line from screen centre to the goal. The `Tail` child turns to point along that line and sits on the bubble's edge on that side (straight down when the goal is on screen; there is no Tail Distance field, the script uses the bubble's size); `Tail Angle Offset` fixes the art direction. Each moo also pops (`Pop Curve` over `Pop Time`, 0.35 s) and the bubble wiggles `Wiggle Angle` degrees each way at `Wiggle Speed`. Body and text stay upright. Padding and offset are multiplied by the canvas scale factor so they match 1920x1080. No target = bubble stays hidden; the moo sound (AudioSource) goes here later.

Moo bubble fields (`MooPrompt`, scene values): `Interval` 6, `Fade Time` 0.2, `Hold Time` 1.6, `Max Alpha` 0.95, `Bubble`, `Tail` (RectTransforms), `Group` (CanvasGroup), `Cam` (empty = Main Camera), `Target` (Transform), `Edge Padding` 175 px, `On Screen Offset` 110 px, `Tail Angle Offset` 90 (the tail art points down), `Pop Curve`, `Pop Time` 0.35, `Wiggle Angle` 4, `Wiggle Speed` 14. Script defaults differ (Fade Time 0.6, Hold Time 1, Max Alpha 0.6, Edge Padding 80, On Screen Offset 60).

`MooBubble` lives inside `PF_HUD`, so both levels get it. Per scene only `Target` is overridden (`Exit`); the rest comes from the prefab. Hierarchy: `HUD > MooBubble` [RectTransform 250 x 110, CanvasGroup, MooPrompt] with children in this order: `Tail` (behind), `Body`, `Text` (TMP "moo!", `Kkscribble SDF` 64, plum). `Body` Image = `cow and crow ui_bubble` (oval speech bubble), Image Type Simple. `Tail` Image = `cow and crow ui_tail` (points down), Simple, 48 x 57. The bubble sits before `Fade` in sibling order so the white fade covers it. Anchors and pivot are centred (the script sets the screen position directly).

`LevelExit`: needs both the `Cow` and `Crow` colliders inside the trigger (it tracks each with an in/out flag). The crow's collider must be the trigger CircleCollider2D. A trigger-vs-trigger overlap reports in 2D when one of the two has a Rigidbody2D; the crow has one. The Exit's layer must collide with the Crow layer in the matrix. When leaving starts and `Next Scene` is not empty, it writes `PlayerPrefs` "SavedLevel" (Start clears it; kept for a later Continue button, see `menu.md`). Only the Image's alpha changes, its RGB stays as set in the Inspector.

## Scene setup

Tree walls: wall trees (see `trees.md`, "Wall trees"). Fog is translucent and drawn over them, so they show through as misty silhouettes. They block the cow. They should block the crow too, but that is not working yet (below).

Physics layers (all in shared Project Settings). Setup status: the layers `Fog` and `Crow` exist, and `Fog Grid` and `Crow` are on them. `Cow` is still on the Default layer. The collision matrix change below is not verified.

| Object | Layer |
|---|---|
| Fog Grid | Fog |
| Crow | Crow |
| Cow | Cow |

`Edit > Project Settings > Physics 2D > Layer Collision Matrix`: Crow x Fog off, Crow x Cow off. So the crow is stopped by tree walls but still flies over fog and over the cow. The cow still collides with fog and walls.

The crow needs two CircleCollider2Ds: one with Is Trigger on (eats fruit) and one solid (stopped by tree walls). Right now it has only the trigger, so it still flies through tree walls.

## Ground

Art: `Assets/Art/Ground/cow and crow tileset.png` (2560 x 2048), Sprite Mode Multiple, Full Rect, PPU 100, Max Size 4096. Slices (Unity rects, origin bottom-left, x y w h):

| Sprite | Rect | Border | What it is |
|---|---|---|---|
| `SPR_GroundMeadow` | 0 768 1280 1280 | 200 | light-green meadow with bumpy rim on dark-green margin (12.8 x 12.8 units) |
| `SPR_GroundPond` | 1280 768 1280 1280 | 320 | dark-green land inside blue water with a blue rim (name kept from the old art) |
| `SPR_GroundDarkFill` | 0 512 256 256 | 0 | plain dark green, tileable |
| `SPR_GroundFill` | 258 508 250 250 | 0 | plain light green, tileable |
| `SPR_GroundWaterFill` | 1280 512 256 256 | 0 | plain blue, tileable |
| `SPR_GroundBush` | 78 102 330 301 | 0 | bush on a light-green box |
| `SPR_GroundPuddle` | 1395 107 286 293 | 140 | small round pond on dark green (dark-green corners, so only on dark ground) |
| `SPR_GroundMeadowRound` | 1840 48 680 680 | 150 | round light-green meadow on dark green (6.8 x 6.8 units) |
| `grass_tuft_0` to `_3` | (319 1632 105 67), (531 1710 100 61), (639 1640 104 62), (561 1574 95 60) | 0 | grass tufts cut from the meadow's grass marks, bottom-centre pivot, used by `PF_GrassTuft` |

The grass/flower decor block at the bottom middle (about x 512..1280, y 0..768 in Unity coordinates) is not sliced.

Replacing the tileset png resets its slices. Scenes and prefabs reference a sprite by the file's GUID plus a per-name fileID, so after a replacement keep the `.meta` GUID and re-cut the slices with the same names (Sprite Editor keeps IDs only if the names stay). The IDs of every sprite above are unchanged since the first import.

Scene objects, all under the root `Ground` at (0, 0, 0):
- `GroundFill`: SpriteRenderer, `SPR_GroundDarkFill` (same dark green as the meadow margin, so meadow edges have no visible square), Draw Mode Tiled, Size 44 x 13, position (10.7, 0), Order in Layer -100.
- `Meadow1` at (4.64, -0.30), `SPR_IslandPatch`, Draw Mode Simple, scale 0.8 (round, about 5 across).
- `Meadow2` at (21.63, 0.38), `SPR_IslandPatch`, Draw Mode Simple, scale 1.2 (round, about 7.4 across, centred on the fruit tree's collider centre).
- Both: Order in Layer -90, not children of the `Clearing` objects (those are empty fog markers). `SPR_IslandPatch` is the round meadow tile with transparent corners, so no square shows (Level1 only; Level2's clearings still use `SPR_GroundMeadowRound`).
- Fit rule: the rim line is about 1.5 units in from the sprite edge; top-row trunk roots touch the top rim, bottom-row canopies cover the bottom rim.

Meadow is 9-sliced: `SPR_GroundMeadow` has a 200 px border on all four sides (`SPR_GroundMeadowRound` 150 px) (Sprite Editor), so the bumpy rim corners stay unstretched. Resize a meadow with `Sprite Renderer > Size` or the Rect Tool (T), never Transform scale (scale also scales the corners). Sliced stretches the edges and flowers a bit; Tiled showed seams, so keep Sliced.

Draw order: ground is -100 to -80, shadows -50, grass tufts -40, trunks 5, cow 15, crow 16, canopies 17, fruit 18, fog 19 (translucent, so it tints everything below it), moo text 20, mist wisps 21, motes 22. Ground and grass are always below the animals and the fog.

Adding pieces: drag a sprite from the tileset into `Ground` in the Hierarchy. Order in Layer -95 for fills (`SPR_GroundDarkFill`, `SPR_GroundWaterFill`, `SPR_GroundPond`), -80 for decor (`SPR_GroundBush`, `SPR_GroundPuddle`). The bush carries a light-green box around it, so only use it on light-green ground; the puddle carries a dark-green box, so only use it on dark ground.

Corner glitch: `SPR_GroundPuddle` and `SPR_GroundMeadowRound` both have an opaque dark-green box around the art. Rule: use `SPR_IslandPatch` (same art, transparent corners) wherever a meadow's corner overlaps something. `Meadow_FruitA` uses it because it overlaps the pond; `PondWater` sits at -92 under the meadows.

## Tuning

| What | Object | Field | Current value |
|---|---|---|---|
| Players followed | Main Camera | SharedCamera > Players | Cow, Crow (Rigidbody2Ds) |
| Camera catch-up (s) | Main Camera | SharedCamera > Smooth Time | 0.3 |
| Camera limits | Main Camera | SharedCamera > Min Position / Max Position | (0, 0) and (21.35, 0) |
| Hold-inside margin | Main Camera | SharedCamera > Edge Margin | 0.5 |
| Shake fade (s) | Main Camera | SharedCamera > Shake Time | 0.25 (`Shake(strength)` is called by `CowRam` on a ram hit, see players.md) |
| Seconds between moos | MooBubble | MooPrompt > Interval | 6 |
| Moo fade (s) | MooBubble | MooPrompt > Fade Time | 0.2 |
| Moo hold (s) | MooBubble | MooPrompt > Hold Time | 1.6 |
| Moo strength | MooBubble | MooPrompt > Max Alpha | 0.95 |
| Cow | Exit | LevelExit > Cow | Cow's BoxCollider2D |
| Crow | Exit | LevelExit > Crow | Crow's trigger CircleCollider2D (to set) |
| Fade image | Exit | LevelExit > Fade | HUD > Fade |
| Fade length (s) | Exit | LevelExit > Fade Time | 1.5 |
| Next level | Exit | LevelExit > Next Scene | Level1: `Level2`. Level2: `HomeScreen` |
| Fog area / position / puffs / start clearings | Fog Grid | see `fog.md` | 48 x 24 at (12.2, -1.4), 2 clearings |
| Crow energy (Level1) | Crow | CrowEnergy > Drain Per Second | 15 |

## Gotchas

- `Next Scene` is required: `LevelExit` logs `missing nextScene` if it is empty.
- Any scene loaded by `LevelExit` must be in `File > Build Profiles` scene list, or the load fails. Level1 itself must be added too.
- The layers and the collision matrix live in shared Project Settings, so changing them affects every scene.
- The fog area must cover the full range the camera can see: camera centre limits plus half the view on each side (about x -10.7 to 32, y -6 to 6). Bare ground outside the fog area shows no fog.
- Camera limits assume 16:9. A wider window shows more than 21.35 units per screen, and the clamp won't hide the extra.
- `HUD > Fade` must stay stretched with 0 offsets. Both scenes once had a +21.25 Pos X override on it (from the prefab), which left an uncovered strip on the left; it was reverted.
- The fade Image must have Raycast Target off, or it blocks clicks later when menus exist.

## Not built yet

- Moo sound. Right now it is only text.
- A solid collider on the crow, and the `Cow` layer, so tree walls stop the crow.
- The `Snake` object is in Level1 but inactive; snakes live in Level2.

## Level2 "The Pond"

Scene `Assets/Scenes/Level2.unity`, made by copying Level1 (camera, HUD, moo bubble, cow, crow, fog, mist, motes, lights kept). Layout v4 built 23 Nov 2026 by a placement script run through the bridge; tune by hand from here.

What it is: 3 x 3 screens (21.35 x 12 each), map about x -10.7 to 53.4, y 6 to -30. The cow and crow start top-left. A river borders the top and right. From the start a path drifts down-right into the fork clearing, which has a fruit tree in its middle. The short branch runs down the pond's west and south shore to the meeting clearing (5 snakes). The long branch runs along the river past the pond's north shore, through two fruit-tree clearings, and back west to the meeting clearing (4 snakes). The meeting clearing has a fruit tree in its middle. From there the final stretch curves down and runs off the bottom-right edge, where the exit is: 7 chasing snakes and fast fog regrowth. The crow can fly over the pond to a fruit on the island; the cow cannot. A crow that lands on water (stops, or runs out of energy) drowns (see `players.md`).

Layout (world coordinates):

| Part | Where | Objects |
|---|---|---|
| Start clearing | (-5.20, 0.84), `Clearing` X scale 8.4 (radius 4.2) | `Ground > Meadow_Start` (`SPR_IslandPatch`, Simple, scale 0.8, 5.44 wide, at (-5.53, -0.64), same size as Level1's); `Clearing` marker is the only `Fog Grid > Open At Start`. Cow (-7, -1), crow (-4.8, 0.2). |
| Fork clearing | (7.3, -5.1), round meadow 7.1 across | `Ground > Meadow_Fork`, `Tree_Fork` in the middle |
| Fruit clearing A | (25.5, -1.2), round meadow 7.1 across | `Ground > Meadow_FruitA`, `Tree_FruitA` in the middle |
| Fruit clearing B | (40.5, -9.5), radius 4.5 | `Ground > Meadow_FruitB`, `Tree_FruitB` in the middle |
| Meeting clearing | (21.8, -17.8), round meadow 7.1 across | `Ground > Meadow_Meeting`, `Tree_Meeting` in the middle, centred in the junction of the west, east and south paths |
| Pond | blob about 9.2 x 9.4 at (19.5, -8.3) | `Ground > Pond` (collider), `Ground > PondWater` (art) |
| Island | (19.5, -8.3), about 3.3 across | `Ground > Island`, `Fruit_Island` (PF_Fruit, lifetime 0) |
| Exit | (43.2, -28.6), trigger 4.4 x 1.8 on the open ground of the bottom-right clearing (x 41 to 45.4, y -29.5 to -27.7); the trees beside it don't count. Keep it above y -29.5: the camera stops at y -24 (`SharedCamera > Min Position`), so the players can't go lower and anything below -30 is off screen. Was 8 x 1.4 at (44.5, -29) until 27 Nov 2026; a try at (44.9, -31.6) sat off screen and could never fire | `Exit` |
| River | one piece over the whole map, 65 x 40.3 at (18.5, -13.85) | `Ground > River` |

Paths are plain dark ground, like Level1: meadow only at the clearings, the tree lines show the way. Each path centreline is a chain of curves through waypoints (in the placement script); every path is 7 units wide between tree trunks (the cow is 1.6 x 0.8). There is no home clearing: the final path leaves the screen and the exit sits on the edge.

Ground pieces:
- `River`: `SPR_GroundPond` (land inside water), Draw Mode Sliced, border 320 px on all sides (set in the Sprite Editor), order -95. Its top and right water bands show; past its right edge (x 51) the dark `GroundFill` reads as forest beyond the river. Two BoxCollider2D on it (top band 65 x 1.6, right band 1.6 x 40.3) and a `Water` component.
- Clearings: `SPR_GroundMeadowRound`, Sliced, size 2 x radius + 2 (so the rim sits under the ring of trees), order -90.
- `Pond`: holds the `Water` component and the PolygonCollider2D (scale 1, not a trigger, Default layer, so it blocks the cow and marks water for the crow). Path 0 is the traced outline of the puddle art, 0.2 inside the visible rim; path 1 is a hole of radius 1.6 over the island. The art is the separate object `Ground > PondWater` (not a child, so Pond keeps scale 1): `SPR_GroundPuddle`, Draw Mode Simple, uniform scale 3.2, order -92 (below the meadows at -90, so a meadow corner never draws over it). Uniform scale keeps the artist's outline from thickening; the 9-slice option (border 140 already set) stretched into a rounded box, and the old 5x / 3.7x stretch made a thick dark ring. If you resize it, scale X and Y the same, then re-trace the collider.
- `Island`: `Assets/Sprites/SPR_IslandPatch.png`, Simple, uniform scale 0.63, order -85. It is a copy of the tileset's `SPR_GroundMeadowRound` with the square ground-coloured corners made see-through, so only the scalloped patch shows over the water. Not a SpriteMask: `MistMask` adds an unbounded SpriteMask in Play, which makes other masks show square corners.

Trees (`Walls`, 76 `WallTree` instances, scale 1): each tree's collider is a circle r 1.4 at 1.3 below the tree's position, and its canopy reaches about 6.4 above that collider. Placement rules:
1. Trunk collider centres sit 4.9 from the path centreline (3.5 half-width + 1.4), 2.4 apart along the line, so neighbours touch: no gap that looks open but isn't.
2. Trees on the south side of a path or clearing move a further 4.4 south (scaled by how south-facing that side is), so their canopy ends at the path edge instead of covering it.
3. Each clearing gets a ring of trees just outside its radius, with the same south push.
4. No tree inside any path or clearing, in the river (y > 4.6 or x > 49.4), touching a fruit tree, or with its canopy over the pond. The water itself is the wall there: the short branch's inner side is the pond shore.
5. A flood fill from the start finds any gap where the cow could leave the paths and puts a tree in it (`Tree_Patch*`). `Tree_PondSeal0` closes the one gap between the pond's east shore and the long branch's inner trees.
Fruit trees are never part of a wall: each stands in the middle of its own clearing, with room to walk round it.

Fruit trees (`FruitTrees`): `Tree_Fork` PF_Fruit, cooldown 6; `Tree_FruitA`, `Tree_FruitB` PF_FruitQuick (`Fruit > Lifetime` 8), cooldown 0.5; `Tree_Meeting` PF_Fruit, cooldown 0.5. Each tree's position is its clearing centre minus (0, 1.58). Trunk collider 2.2 x 1.7 at offset (0.03, 0.135), trunk only (see `trees.md`): the old 2.2 x 4.59 box reached up into the canopy and left `Tree_FruitA` a 0.5 gap under the top wall, too narrow for the cow's 0.8-tall feet box.

Snakes (`Snakes`, 16, scale 0.5, `Dead` = `SPR_SnakeDead`, Order in Layer 14 so they draw under the cow 15, crow 16, canopies 17 and fog 19, `Obstacles` = Default + Water): staggered so no straight line down a path misses them. `Short0` (4.8, -10.8), `Short1` (11.8, -11.2), `Short2` (6.6, -14.4), `Short3` (14.6, -14.4), `Short4` (9.6, -13.6) on the short branch; `Long0` (33.6, 0.6), `Long1` (36.0, -14.4), `Long2` (16.8, -0.6), `Long3` (27.9, -14.9) on the long branch; `Final0` (22.4, -22.4), `Final1` (26.4, -25.2), `Final2` (23.6, -27.4), `Final3` (30.0, -27.4), `Final4` (31.7, -25.5), `Final5` (34.8, -27.2), `Final6` (39.2, -25.4) along the final stretch with `Chase Range` 4 and `Chase Speed` 2.1 (cow walks at 3, crow flies at 5). The others have `Chase Range` 0. All are in `Tip_Snake > Targets`. Checked in Play: no snake starts blocked, each has 1 unit of free space around it. `Final4` and `Long3` were nudged from the plan to get that. `Final2`, `Final3`, `Final5` are kept off the map's bottom edge (y -30).

Fog: Fog Grid at (21.35, -12), `Area Size` 65.6 x 38.4, `Pixels Per Unit` 12, `Cell Size` 0.4, `Puff Count` 370: a 788 x 461 texture and 15,744 cells (Level1 at 16/0.25 would be 1050 x 614 px and 40k cells here) and an 11 x 11 collider pool. `FogGrid.Update` measured in the editor at about 2.7 ms idle, 5 ms while clearing; test WebGL. `Fast Regrow Zones` = `FastFogZone` at (33, -27), scale (26, 8, 1), over the final stretch.

Checked through the bridge (23 Nov, v4): a 1.9 x 1.0 box (bigger than the cow) gets from the start to the exit through each branch alone; a 0.8 probe can't reach the exit with both branches cut and can't reach the island. In Play mode the crow eats the island fruit, sits on the island at zero energy without drowning, and drowns at zero energy over open water. (Since 27 Nov the crow also drowns when it stops over water with energy left; not re-checked in Play yet.) No console errors.

Tuning (Level2 values):

| What | Object | Field | Value |
|---|---|---|---|
| Camera limits | Main Camera | SharedCamera > Min Position / Max Position | (0, -24) / (42.7, 0) |
| Fog area | Fog Grid | FogGrid > Area Size / Pixels Per Unit / Cell Size | 65.6 x 38.4 / 12 / 0.4 |
| Start and tree clearings | Fog Grid | FogGrid > Open At Start | Clearing + 4 `TreeClearings` children, X scale 14, each at its meadow's position |
| Final-stretch regrow | Fog Grid | FogGrid > Fast Regrow Zones | FastFogZone |
| Final snakes chase | Snake_Final0..6 | Snake > Chase Range / Chase Speed | 4 / 2.1 |
| Crow energy | Crow | CrowEnergy > Drain Per Second / Tired Speed Scale | 4 / 0.3 (a full bar is 25 s of flight, a fruit 7.5 s) |
| Fork tree cooldown | Tree_Fork | FruitTree > Cooldown | 6 |
| Long-path fruit life | PF_FruitQuick | Fruit > Lifetime | 8 |
| Exit target | Exit | LevelExit > Next Scene | HomeScreen |
| Motes box | Motes | Particle System > Shape > Scale / Emission | (66, 38) / 35 per s, max 240 |

Reshaping by hand: move trees in pairs along a line and keep trunks touching (2.4 apart at scale 1). After moving trees, walk the cow through every bend in Play mode. A south-side tree closer than 4.4 below a path hides the path under its canopy. Moving the pond: move `Pond`, `PondWater`, `Island` and `Fruit_Island` together, and check the gap by `Tree_PondSeal0` (the pond is smaller than in v4, so that gap is wider now; the cow still cannot pass it).

Not done / check by hand: snake balance is unplayed; energy at 4 per second is a guess; a full walk-through to the exit and the HomeScreen button has not been done in Play mode.


## Home marker (both levels)

None. The exit has no marker; the moo is what guides the players there.

Tried and removed on 27 Nov 2026:
- A gold glow at the exit.
- An arrow on the ground. It was drawn under the fog, and Level2's exit sits in the fast-regrow fog zone, so the arrow was never seen.
- A `Tip_Home` sign ("home. / both go in!") over the exit, first with an arrow in its key slot, then text only. Deleted from both scenes. `Assets/Sprites/SPR_HomeArrow.png` is left unused.

## Depth and clearings (both levels, 23 Nov)

Decoration only, no colliders, no scripts:
- `Darkness` (root): sprites of `Assets/Art/Fog/darkness.png` (`darkness_0`), Simple, uniform scale, random turn, colour alpha 0.6, Order in Layer -30. Placed only outside the walkable area (found by flood fill with the cow's box), at least 0.5 + 0.4 x diameter from any walkable cell, so they sit behind the wall tree trunks and never on a path. Level1 145 puffs, Level2 101 (kept away from the pond and the river's water bands).
- `BackgroundTrees` (root): plain objects (`BGTree` > `Trunk`, `Canopy`) with the wall tree's sprites and materials (`M_TreeTrunk`, `M_TreeCanopy`), scale 0.7 to 1.5, colour (0.35, 0.35, 0.4) alpha 0.6. Order in Layer -25 to -6 (higher further down the screen), above `Darkness`, below grass, shadows (-50) are below them, trunks (5) and canopies (17) are above. Level1 100 trees, Level2 66. Canopies never touch a walkable cell.
- Both were scattered by a one-off script through the bridge, not by hand; delete a parent to remove it. Moving the paths means re-scattering.

### Level1
- Start area (23 Nov): wall trees moved straight up or down (away from the patch, rows keep their x) until their collider and sprites clear the round patches: `Top_00` +0.35, `Top_02` +1.6, `Bottom_03` -0.1, `Top_08` +1.4, `Top_14` +3.1, `Top_16` +0.65 (y). Neighbour gaps did not grow past 1.6 anywhere they were not already that wide. Six `BackgroundTrees` and 14 `Darkness` puffs that the move uncovered next to the patches were deleted (Darkness now 131 puffs, BackgroundTrees 94).
- Thicker fog: `Fog Grid > Fog Color` alpha 0.7 to 1, `Thin Alpha` 0.45 to 1; every `Darkness` puff colour alpha 0.6 to 0.8 (per-renderer colour, no material change). `Clear Radius` 2 to 3 (see `fog.md`; cow collider 1.6 x 0.8, so the cleared path is about 4 wide).
- `Tutorial` (root): three SpriteRenderers (controls drawings, plum tint, order 23, wobbling `M_Scribble` material) and the tutorial signs. See `tutorial.md`.
- `Clearing2` at (21.63, 0.38), X scale 14. `Meadow2` is the round meadow described above. `Clearing` moved onto `Meadow1` (4.64, -0.30) so the fog clearing and the meadow match.
- Crow energy was 10, then 35, now 15. Route from start to `Exit` is 27.6 units for the cow; fog on it: 10.6 units before the tree clearing and 6.4 after (17 total). At 15 per second one bar is about 33 units of flight, so the first stretch is easy and the second needs a fruit or two (each fruit, 30, is 10 units). The crow only drains while moving, so hovering is free.

### Level2
- `TreeClearings` (root) holds `Clearing_TreeFork`, `_TreeFruitA`, `_TreeFruitB`, `_TreeMeeting` at the four meadow centres (snapped to the meadows' live positions), X scale 14, all in `Fog Grid > Open At Start` after `Clearing` (now at `Meadow_Start`, (-5.20, 0.84)).
- `Ground > Meadow_Fork`, `_FruitA`, `_FruitB`, `_Meeting`: now `SPR_GroundMeadowRound`, Draw Mode Simple, scale 1.4 (round, about 7.1 across), order -90. `Meadow_Start` unchanged. 12 grass tufts moved inside the new circles (radius 2 from the tree).
- Pond and island: see the Pond and Island lines under "Ground pieces". `PondMask` and the plain `SPR_Circle` pond/island shapes are gone; `SPR_Circle.png` is deleted; shadows use `SPR_ShadowBlob`.
- Tree positions moved to centre them on the paths: `Tree_Fork`, `Clearing_TreeFork`, `Meadow_Fork` and its 3 tufts moved +1.0 in X (clearing now (7.3, -5.1)). `Tree_Meeting`, `Clearing_TreeMeeting`, `Meadow_Meeting` and its 3 tufts moved by (+2.8, -0.3) (clearing now (21.8, -17.8), the middle of the three paths).
- Clip fix: the permanent fog clearing is 7 units in radius, bigger than the wall ring, so gaps between wall trunks that fog used to fill are open there. `Walls > Tree_PatchFruitB` (WallTree, (33.9, -7.4)) closes a 0.95 slit between `Tree_36` and `Tree_68` on the west side of the FruitB clearing, which let the cow into the forest behind the trees. Still-open slits outside any permanent clearing (fog covers them until the crow clears it): `Tree_0`/`Tree_62` (0.9), `Tree_8`/`Tree_9` (1.7), `Tree_17`/`Tree_18` and `Tree_64` (1.5 to 1.75).
- Checked: cow box reaches the exit through the short branch alone and the long branch alone; not with both cut.
