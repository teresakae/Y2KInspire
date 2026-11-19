# Level (Level1 "The Forest")

Status: the three scripts are written. `Level1.unity` is built (camera, fog, clearings, trees, exit, HUD). Physics layers and the crow's solid collider are not finished, see "Scene setup".

## What it is

The player starts in a small clearing surrounded by fog and walls of trees. A faint "moo!" speech bubble pops up every few seconds and points toward the goal. The crow clears fog to the right and the cow follows. The camera slides along with both animals. On the second screen there is a second clearing with a fruit tree. The crow arrives low on energy, so the players learn the loop: cow rams the tree, fruit drops, crow eats it. Past the tree, the exit is on the right edge of screen 2. When the cow and the crow are both inside it the screen fades to white and the level restarts (Level2 does not exist yet). The level opens with a fade from white.

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

Art: `Assets/Art/Ground/cow and crow tileset.png`, sliced into 7 sprites (Full Rect, PPU 100, Max Size 4096): `SPR_GroundMeadow` and `SPR_GroundPond` (12.8 x 12.8 units), `SPR_GroundDarkFill`, `SPR_GroundWaterFill`, `SPR_GroundFill` (tileable 2.56 x 2.56), `SPR_GroundBush`, `SPR_GroundPuddle`.

Scene objects, all under the root `Ground` at (0, 0, 0):
- `GroundFill`: SpriteRenderer, `SPR_GroundDarkFill` (same dark green as the meadow margin, so meadow edges have no visible square), Draw Mode Tiled, Size 44 x 13, position (10.7, 0), Order in Layer -100.
- `Meadow1` at (3.55, -0.35), size 10.5 x 7.5.
- `Meadow2` at (21.6, -0.8), size 9.6 x 9.6.
- Both use Draw Mode Sliced, Order in Layer -90, scale 1, and are not children of the `Clearing` objects (those are empty fog markers).
- Fit rule: the rim line is about 1.5 units in from the sprite edge; top-row trunk roots touch the top rim, bottom-row canopies cover the bottom rim.

Meadow is 9-sliced: `SPR_GroundMeadow` has a 200 px border on all four sides (Sprite Editor), so the bumpy rim corners stay unstretched. Resize a meadow with `Sprite Renderer > Size` or the Rect Tool (T), never Transform scale (scale also scales the corners). Sliced stretches the edges and flowers a bit; Tiled showed seams, so keep Sliced.

Draw order: ground is -100 to -80, shadows -50, grass tufts -40, trunks 5, cow 15, crow 16, canopies 17, fruit 18, fog 19 (translucent, so it tints everything below it), moo text 20, mist wisps 21, motes 22. Ground and grass are always below the animals and the fog.

Adding pieces: drag a sprite from the tileset into `Ground` in the Hierarchy. Order in Layer -95 for fills (`SPR_GroundDarkFill`, `SPR_GroundWaterFill`, `SPR_GroundPond`), -80 for decor (`SPR_GroundBush`, `SPR_GroundPuddle`). Bush and puddle carry a light-green box around them, so only use them on light-green ground (the meadow or the fill).

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
| Next level | Exit | LevelExit > Next Scene | empty (reloads Level1, saves nothing) |
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
- Level2, and the exit loading it (`Next Scene` is empty).
- A solid collider on the crow, and the `Cow` layer, so tree walls stop the crow.
- The `Snake` object is in Level1 but inactive; snakes start in Level2.
