# Level (Level1 "The Forest")

Status: the three scripts are written. `Level1.unity` is built (camera, fog, clearings, trees, exit, HUD). Physics layers and the crow's solid collider are not finished, see "Scene setup".

## What it is

The player starts in a small clearing surrounded by fog and walls of trees. A faint "moo!" shows at the bottom-right of the screen every few seconds. The crow clears fog to the right and the cow follows. The camera slides along with both animals. On the second screen there is a second clearing with a fruit tree. The crow arrives low on energy, so the players learn the loop: cow rams the tree, fruit drops, crow eats it. Past the tree, the exit is on the right edge of screen 2. When the cow walks into it the screen fades to white and the level restarts (Level2 does not exist yet). The level opens with a fade from white.

## Where it lives

- Scripts: `Assets/Scripts/SharedCamera.cs`, `Assets/Scripts/MooPrompt.cs`, `Assets/Scripts/LevelExit.cs`
- Scene: `Assets/Level1.unity` (copy of SampleScene; being set up)
- Scene objects: `Snake` is inactive (hidden in Level1; snakes start in Level2, see `snakes.md`). `Main Camera` holds `SharedCamera`. `Exit` holds `LevelExit`. `HUD > Fade` is the fade. Not built yet: the moo text (a TextMeshPro text under `HUD` holding `MooPrompt`).

## Layout

Two screens side by side, each about 21.35 x 12 units (camera size 6, 16:9). Screen 1 centre is x = 0, screen 2 centre is x = 21.35. The level spans about x -10.7 to 32, y -6 to 6.

- Clearing 1 on screen 1: the start. `Clearing` at (-4.8, 0.01), X scale 8.4. Cow start (-4.85, -0.06), crow start (-2.71, -1.19), both inside it.
- Clearing 2 on screen 2: the fruit tree. `Clearing2` at (21.6, -0.12), X scale 9.6, second entry in `Fog Grid > Open At Start`. `Tree` (the fruit tree, see `trees.md`) at (21.6, -1.2). The crow reaches it low on energy.
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
5. The cow enters the exit trigger. `LevelExit` starts fading to white. When alpha reaches 1 it loads `Next Scene`, or reloads the current scene if `Next Scene` is empty.

`MooPrompt`: `t = Time.timeSinceLevelLoad % Interval` (so a restart opens on a moo too). Alpha rises 0 to 1 over `Fade Time`, holds for `Hold Time`, falls back over `Fade Time`, then stays 0 until the next interval. The result is multiplied by `Max Alpha` and set on the text. Fixed screen corner, not the real direction of the herd. The owner places it with anchors on the canvas, not in code.

`LevelExit`: only the cow's collider triggers it (the `Cow` field). Only the Image's alpha changes, its RGB stays as set in the Inspector.

## Scene setup

Tree walls: wall trees (see `trees.md`, "Wall trees"), drawn above the fog so they stay readable when fogged. They block the cow. They should block the crow too, but that is not working yet (below).

Physics layers (all in shared Project Settings). Setup status: the layers `Fog` and `Crow` exist, and `Fog Grid` and `Crow` are on them. `Cow` is still on the Default layer. The collision matrix change below is not verified.

| Object | Layer |
|---|---|
| Fog Grid | Fog |
| Crow | Crow |
| Cow | Cow |

`Edit > Project Settings > Physics 2D > Layer Collision Matrix`: Crow x Fog off, Crow x Cow off. So the crow is stopped by tree walls but still flies over fog and over the cow. The cow still collides with fog and walls.

The crow needs two CircleCollider2Ds: one with Is Trigger on (eats fruit) and one solid (stopped by tree walls). Right now it has only the trigger, so it still flies through tree walls.

## Tuning

| What | Object | Field | Current value |
|---|---|---|---|
| Players followed | Main Camera | SharedCamera > Players | Cow, Crow (Rigidbody2Ds) |
| Camera catch-up (s) | Main Camera | SharedCamera > Smooth Time | 0.3 |
| Camera limits | Main Camera | SharedCamera > Min Position / Max Position | (0, 0) and (21.35, 0) |
| Hold-inside margin | Main Camera | SharedCamera > Edge Margin | 0.5 |
| Seconds between moos | Moo text | MooPrompt > Interval | not set up yet (planned: 6) |
| Moo fade (s) | Moo text | MooPrompt > Fade Time | not set up yet (planned: 0.6) |
| Moo hold (s) | Moo text | MooPrompt > Hold Time | not set up yet (planned: 1) |
| Moo strength | Moo text | MooPrompt > Max Alpha | not set up yet (planned: 0.6) |
| Cow | Exit | LevelExit > Cow | Cow's BoxCollider2D |
| Fade image | Exit | LevelExit > Fade | HUD > Fade |
| Fade length (s) | Exit | LevelExit > Fade Time | 1.5 |
| Next level | Exit | LevelExit > Next Scene | empty (reloads Level1) |
| Fog area / position / puffs / start clearings | Fog Grid | see `fog.md` | 45.6 x 14.4 at (10.68, 0), 94 puffs, 2 clearings |

## Gotchas

- Any scene loaded by `LevelExit` must be in `File > Build Profiles` scene list, or the load fails. Level1 itself must be added too.
- The layers and the collision matrix live in shared Project Settings, so changing them affects every scene.
- The fog area must cover the full range the camera can see: camera centre limits plus half the view on each side (about x -10.7 to 32, y -6 to 6). Bare ground outside the fog area shows no fog.
- Camera limits assume 16:9. A wider window shows more than 21.35 units per screen, and the clamp won't hide the extra.
- The fade Image must have Raycast Target off, or it blocks clicks later when menus exist.

## Not built yet

- FMOD moo. Right now it is only text.
- Moo direction: the text does not point toward the herd.
- Level2, and the exit loading it (`Next Scene` is empty).
- A solid collider on the crow, and the `Cow` layer, so tree walls stop the crow.
- The `Snake` object is in Level1 but inactive; snakes start in Level2.
