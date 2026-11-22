# Level (Level1 "The Forest")

Status: the three scripts are written. `Level1.unity` is built (camera, fog, clearings, trees, exit, HUD). Physics layers and the crow's solid collider are not finished, see "Scene setup".

## What it is

The player starts in a small clearing surrounded by fog and walls of trees. A faint "moo!" speech bubble pops up every few seconds and points toward the goal. The crow clears fog to the right and the cow follows. The camera slides along with both animals. On the second screen there is a second clearing with a fruit tree. The crow arrives low on energy, so the players learn the loop: cow rams the tree, fruit drops, crow eats it. Past the tree, the exit is on the right edge of screen 2. When the cow and the crow are both inside it the screen fades to white and Level2 loads (`LevelExit > Next Scene`). The level opens with a fade from white.

## Where it lives

- Scripts: `Assets/Scripts/SharedCamera.cs`, `Assets/Scripts/MooPrompt.cs`, `Assets/Scripts/LevelExit.cs`
- Scene: `Assets/Level1.unity` (started as a copy of SampleScene; since the mist swap it is the `FogTest_Mist` scene, see `fog.md`)
- Look objects: `Mist` (two layers of drifting wisps), `Motes` (floating specks), `Global Volume` (post-processing, `Assets/Settings/VP_Mist.asset`), `Crow > Light 2D` (warm glow), `Grass` (swaying tufts, see `grass.md`). Assets: `Assets/Shaders/FogSwirl.shader`, `Assets/Materials/M_FogSwirl.mat`, `M_MoteGlow.mat`, scripts `MistDrift.cs`, `MistMask.cs`.
- Scene objects: `Snake` is inactive (hidden in Level1; snakes start in Level2, see `snakes.md`). `Main Camera` holds `SharedCamera`. `Exit` holds `LevelExit`. `HUD > Fade` is the fade. `HUD > MooBubble` is the moo bubble (see below).

## Layout

Two screens side by side, each about 21.35 x 12 units (camera size 6, 16:9). Screen 1 centre is x = 0, screen 2 centre is x = 21.35. The level spans about x -10.7 to 32, y -6 to 6.

- Clearing 1 on screen 1: the start. `Clearing` at (-4.8, 0.01), X scale 8.4. Cow start (-4.85, -0.06), crow start (-2.71, -1.19), both inside it.
- Clearing 2 on screen 2: the fruit tree. `Clearing2` at (21.6, -0.12), X scale 14.4, second entry in `Fog Grid > Open At Start`. `Tree` (the fruit tree, see `trees.md`) at (21.6, -1.2). The crow reaches it low on energy.
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
5. Both the cow and the crow are inside the exit trigger. `LevelExit` saves `Next Scene` (if set) and starts fading to white. When alpha reaches 1 it loads `Next Scene`, or reloads the current scene if `Next Scene` is empty. If one animal steps out before that, the fade still finishes: the fade starts once and does not cancel.

`MooPrompt` (moo bubble on `HUD > MooBubble`): timing is `t = Time.timeSinceLevelLoad % Interval` (so a restart opens on a moo too). Alpha rises 0 to 1 over `Fade Time`, holds for `Hold Time`, falls back over `Fade Time`, then stays 0 until the next interval; times `Max Alpha`, written to the `CanvasGroup`. Placement runs in `LateUpdate` (after the smoothed camera): `cam.WorldToScreenPoint(target)`. If the goal is inside the screen shrunk by `Edge Padding`, the bubble sits at that point plus `On Screen Offset` pixels up. If not, the bubble sits on the padded screen edge, along the line from screen centre to the goal. The `Tail` child turns to point along that line and moves `Tail Distance` px from the bubble centre on that side (straight down when the goal is on screen); `Tail Angle Offset` fixes the art direction. Body and text stay upright. Padding and offset are multiplied by the canvas scale factor so they match 1920x1080. No target = bubble stays hidden; `SetTarget(Transform)` swaps the goal (a later AreaGate will call it when an area opens). FMOD positional moo goes here later.

Moo bubble fields (`MooPrompt`): `Interval` 6, `Fade Time` 0.6, `Hold Time` 1, `Max Alpha` 0.6, `Bubble` (RectTransform), `Tail` (RectTransform), `Group` (CanvasGroup), `Cam` (empty = Main Camera), `Target` (Transform, e.g. `Exit`), `Edge Padding` 80 px, `On Screen Offset` 60 px, `Tail Angle Offset` 45 deg (square art), `Tail Distance` 50 px (scene values; script defaults are 0 and 60).

Placed hierarchy: `HUD > MooBubble` [RectTransform, CanvasGroup, Image body, MooPrompt] with children `Tail` (first child so the body draws over it; a 40 x 40 square, `UISprite`, peach #F4C7A8, turned by code so its corner points at the goal) and `Text` (TMP "moo!", 40, plum #2E1A2B). `MooBubble` is 200 x 100, `UISprite` sliced, peach, CanvasGroup alpha 0, sits before `Fade` in sibling order so the white fade covers it. Set: `Target` = `Exit`, `Tail Distance` 50, `Tail Angle Offset` 45, other fields default. `Bubble` and `Group` point at `MooBubble`; `Tail` at `Tail`. Anchors/pivot centred on `MooBubble` (the script sets its screen position directly).

`LevelExit`: needs both the `Cow` and `Crow` colliders inside the trigger (it tracks each with an in/out flag). The crow's collider must be the trigger CircleCollider2D. A trigger-vs-trigger overlap reports in 2D when one of the two has a Rigidbody2D; the crow has one. The Exit's layer must collide with the Crow layer in the matrix. When leaving starts and `Next Scene` is not empty, it writes `PlayerPrefs` "SavedLevel" for the menu's Continue button (see `menu.md`). Only the Image's alpha changes, its RGB stays as set in the Inspector.

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
- `Meadow1` at (3.55, -0.35), size 10.5 x 7.5.
- `Meadow2` at (21.6, -0.8), size 9.6 x 9.6.
- Both use Draw Mode Sliced, Order in Layer -90, scale 1, and are not children of the `Clearing` objects (those are empty fog markers).
- Fit rule: the rim line is about 1.5 units in from the sprite edge; top-row trunk roots touch the top rim, bottom-row canopies cover the bottom rim.

Meadow is 9-sliced: `SPR_GroundMeadow` has a 200 px border on all four sides (`SPR_GroundMeadowRound` 150 px) (Sprite Editor), so the bumpy rim corners stay unstretched. Resize a meadow with `Sprite Renderer > Size` or the Rect Tool (T), never Transform scale (scale also scales the corners). Sliced stretches the edges and flowers a bit; Tiled showed seams, so keep Sliced.

Draw order: ground is -100 to -80, shadows -50, grass tufts -40, trunks 5, cow 15, crow 16, canopies 17, fruit 18, fog 19 (translucent, so it tints everything below it), moo text 20, mist wisps 21, motes 22. Ground and grass are always below the animals and the fog.

Adding pieces: drag a sprite from the tileset into `Ground` in the Hierarchy. Order in Layer -95 for fills (`SPR_GroundDarkFill`, `SPR_GroundWaterFill`, `SPR_GroundPond`), -80 for decor (`SPR_GroundBush`, `SPR_GroundPuddle`). The bush carries a light-green box around it, so only use it on light-green ground; the puddle carries a dark-green box, so only use it on dark ground.

## Tuning

| What | Object | Field | Current value |
|---|---|---|---|
| Players followed | Main Camera | SharedCamera > Players | Cow, Crow (Rigidbody2Ds) |
| Camera catch-up (s) | Main Camera | SharedCamera > Smooth Time | 0.3 |
| Camera limits | Main Camera | SharedCamera > Min Position / Max Position | (0, 0) and (21.35, 0) |
| Hold-inside margin | Main Camera | SharedCamera > Edge Margin | 0.5 |
| Shake fade (s) | Main Camera | SharedCamera > Shake Time | 0.25 (`Shake(strength)` is called by `CowRam` on a ram hit, see players.md) |
| Seconds between moos | MooBubble | MooPrompt > Interval | 6 |
| Moo fade (s) | MooBubble | MooPrompt > Fade Time | 0.6 |
| Moo hold (s) | MooBubble | MooPrompt > Hold Time | 1 |
| Moo strength | MooBubble | MooPrompt > Max Alpha | 0.6 |
| Cow | Exit | LevelExit > Cow | Cow's BoxCollider2D |
| Crow | Exit | LevelExit > Crow | Crow's trigger CircleCollider2D (to set) |
| Fade image | Exit | LevelExit > Fade | HUD > Fade |
| Fade length (s) | Exit | LevelExit > Fade Time | 1.5 |
| Next level | Exit | LevelExit > Next Scene | Level1: `Level2`. Level2: `HomeScreen` |
| Fog area / position / puffs / start clearings | Fog Grid | see `fog.md` | 45.6 x 14.4 at (10.68, 0), 94 puffs, 2 clearings |

## Gotchas

- With Next Scene empty nothing is saved, so Continue stays hidden until a level with a Next Scene is finished.
- Any scene loaded by `LevelExit` must be in `File > Build Profiles` scene list, or the load fails. Level1 itself must be added too.
- The layers and the collision matrix live in shared Project Settings, so changing them affects every scene.
- The fog area must cover the full range the camera can see: camera centre limits plus half the view on each side (about x -10.7 to 32, y -6 to 6). Bare ground outside the fog area shows no fog.
- Camera limits assume 16:9. A wider window shows more than 21.35 units per screen, and the clamp won't hide the extra.
- The fade Image must have Raycast Target off, or it blocks clicks later when menus exist.

## Not built yet

- FMOD moo. Right now it is only text.
- A solid collider on the crow, and the `Cow` layer, so tree walls stop the crow.
- The `Snake` object is in Level1 but inactive; snakes live in Level2.

## Level2 "The Pond"

Scene `Assets/Scenes/Level2.unity`, made by copying Level1 (camera, HUD, moo bubble, cow, crow, fog, mist, motes, lights kept). Layout v4 built 23 Nov 2026 by a placement script run through the bridge; tune by hand from here.

What it is: 3 x 3 screens (21.35 x 12 each), map about x -10.7 to 53.4, y 6 to -30. The cow and crow start top-left. A river borders the top and right. From the start a path drifts down-right into the fork clearing, which has a fruit tree in its middle. The short branch runs down the pond's west and south shore to the meeting clearing (4 snakes). The long branch runs along the river past the pond's north shore, through two fruit-tree clearings, and back west to the meeting clearing (2 snakes). The meeting clearing has a fruit tree in its middle. From there the final stretch curves down and runs off the bottom-right edge, where the exit is: 6 chasing snakes and fast fog regrowth. The crow can fly over the pond to a fruit on the island; the cow cannot. A crow that runs out of energy over water drowns (see `players.md`).

Layout (world coordinates):

| Part | Where | Objects |
|---|---|---|
| Start clearing | (-6, -0.2), radius 4 | `Ground > Meadow_Start`; `Clearing` marker is the only `Fog Grid > Open At Start`. Cow (-7, -1), crow (-4.8, 0.2). |
| Fork clearing | (6.3, -5.1), radius 5 | `Ground > Meadow_Fork`, `Tree_Fork` in the middle |
| Fruit clearing A | (25.5, -1.2), radius 4.5 | `Ground > Meadow_FruitA`, `Tree_FruitA` in the middle |
| Fruit clearing B | (40.5, -9.5), radius 4.5 | `Ground > Meadow_FruitB`, `Tree_FruitB` in the middle |
| Meeting clearing | (19, -17.5), radius 5.5 | `Ground > Meadow_Meeting`, `Tree_Meeting` in the middle |
| Pond | oval 14.8 x 10.8 at (19.5, -8.3) | `Ground > Pond`, `Ground > PondMask` |
| Island | (19.5, -8.3), radius 1.5 | `Ground > Island`, `Fruit_Island` (PF_Fruit, lifetime 0) |
| Exit | (44.5, -29), trigger 8 x 1.4 on the bottom edge | `Exit` |
| River | one piece over the whole map, 65 x 40.3 at (18.5, -13.85) | `Ground > River` |

Paths are plain dark ground, like Level1: meadow only at the clearings, the tree lines show the way. Each path centreline is a chain of curves through waypoints (in the placement script); every path is 7 units wide between tree trunks (the cow is 1.6 x 0.8). There is no home clearing: the final path leaves the screen and the exit sits on the edge.

Ground pieces:
- `River`: `SPR_GroundPond` (land inside water), Draw Mode Sliced, border 320 px on all sides (set in the Sprite Editor), order -95. Its top and right water bands show; past its right edge (x 51) the dark `GroundFill` reads as forest beyond the river. Two BoxCollider2D on it (top band 65 x 1.6, right band 1.6 x 40.3) and a `Water` component.
- Clearings: `SPR_GroundMeadowRound`, Sliced, size 2 x radius + 2 (so the rim sits under the ring of trees), order -90.
- `Pond`: `SPR_GroundPuddle` at Draw Mode Simple, scaled to 14.8 x 10.8, order -88, `Mask Interaction` Visible Inside Mask. `PondMask` is a Sprite Mask using Unity's built-in round `Knob` sprite scaled to the same oval, so the tile's square corners are cut off. A PolygonCollider2D with two paths: an oval 0.5 inside the rim (blocks the cow) and a hole of radius 1.5 over the island, so a crow landing on the island is not over water. A `Water` component.
- `Island`: `SPR_GroundPond` at Simple, scale 0.3 (land in water, blends into the pond), order -85.

Trees (`Walls`, 76 `WallTree` instances, scale 1): each tree's collider is a circle r 1.4 at 1.3 below the tree's position, and its canopy reaches about 6.4 above that collider. Placement rules:
1. Trunk collider centres sit 4.9 from the path centreline (3.5 half-width + 1.4), 2.4 apart along the line, so neighbours touch: no gap that looks open but isn't.
2. Trees on the south side of a path or clearing move a further 4.4 south (scaled by how south-facing that side is), so their canopy ends at the path edge instead of covering it.
3. Each clearing gets a ring of trees just outside its radius, with the same south push.
4. No tree inside any path or clearing, in the river (y > 4.6 or x > 49.4), touching a fruit tree, or with its canopy over the pond. The water itself is the wall there: the short branch's inner side is the pond shore.
5. A flood fill from the start finds any gap where the cow could leave the paths and puts a tree in it (`Tree_Patch*`). `Tree_PondSeal0` closes the one gap between the pond's east shore and the long branch's inner trees.
Fruit trees are never part of a wall: each stands in the middle of its own clearing, with room to walk round it.

Fruit trees (`FruitTrees`): `Tree_Fork` PF_Fruit, cooldown 6; `Tree_FruitA`, `Tree_FruitB` PF_FruitQuick (`Fruit > Lifetime` 8), cooldown 0.5; `Tree_Meeting` PF_Fruit, cooldown 0.5. Each tree's position is its clearing centre minus (0, 1.58), which centres its box collider.

Snakes (`Snakes`, scale 0.5, `Dead` = `SPR_SnakeDead`): `Snake_Short0..3` spread across the short branch, `Snake_Long0..1` on the long branch, `Snake_Final0..5` along the final stretch with `Chase Range` 4 and `Chase Speed` 2.1 (cow walks at 3, crow flies at 5). The others have `Chase Range` 0.

Fog: Fog Grid at (21.35, -12), `Area Size` 65.6 x 38.4, `Pixels Per Unit` 12, `Cell Size` 0.4, `Puff Count` 370: a 788 x 461 texture and 15,744 collider cells (Level1 at 16/0.25 would be 1050 x 614 px and 40k colliders here). `FogGrid.Update` measured in the editor at about 2.7 ms idle, 5 ms while clearing; test WebGL. `Fast Regrow Zones` = `FastFogZone` at (33, -27), scale (26, 8, 1), over the final stretch.

Checked through the bridge (23 Nov, v4): a 1.9 x 1.0 box (bigger than the cow) gets from the start to the exit through each branch alone; a 0.8 probe can't reach the exit with both branches cut and can't reach the island. In Play mode the crow eats the island fruit, sits on the island at zero energy without drowning, and drowns at zero energy over open water. No console errors.

Tuning (Level2 values):

| What | Object | Field | Value |
|---|---|---|---|
| Camera limits | Main Camera | SharedCamera > Min Position / Max Position | (0, -24) / (42.7, 0) |
| Fog area | Fog Grid | FogGrid > Area Size / Pixels Per Unit / Cell Size | 65.6 x 38.4 / 12 / 0.4 |
| Start clearing | Fog Grid | FogGrid > Open At Start | Clearing |
| Final-stretch regrow | Fog Grid | FogGrid > Fast Regrow Zones | FastFogZone |
| Final snakes chase | Snake_Final0..5 | Snake > Chase Range / Chase Speed | 4 / 2.1 |
| Crow energy | Crow | CrowEnergy > Drain Per Second / Tired Speed Scale | 4 / 0.3 (a full bar is 25 s of flight, a fruit 7.5 s) |
| Fork tree cooldown | Tree_Fork | FruitTree > Cooldown | 6 |
| Long-path fruit life | PF_FruitQuick | Fruit > Lifetime | 8 |
| Exit target | Exit | LevelExit > Next Scene | HomeScreen |
| Motes box | Motes | Particle System > Shape > Scale / Emission | (66, 38) / 35 per s, max 240 |

Reshaping by hand: move trees in pairs along a line and keep trunks touching (2.4 apart at scale 1). After moving trees, walk the cow through every bend in Play mode. A south-side tree closer than 4.4 below a path hides the path under its canopy. Moving the pond: move `Pond`, `PondMask`, `Island` and `Fruit_Island` together, and check the gap by `Tree_PondSeal0`.

Not done / check by hand: snake balance is unplayed; energy at 4 per second is a guess; a full walk-through to the exit and the HomeScreen button has not been done in Play mode.
