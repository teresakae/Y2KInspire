# Level (LV_01 "The Forest")

Status: the three scripts are written. `LV_01.unity` is not created yet, so everything under "Scene setup" is planned, not built.

## What it is

The player starts in a small clearing surrounded by fog and walls of trees. A faint "moo!" shows at the bottom-right of the screen every few seconds. The crow clears fog to the right and the cow follows. The camera slides along with both animals. On the second screen there is a second clearing with a fruit tree. The crow arrives low on energy, so the players learn the loop: cow rams the tree, fruit drops, crow eats it. Past the tree, the exit is on the right edge of screen 2. When the cow walks into it the screen fades to white and the level restarts (LV_02 does not exist yet). The level opens with a fade from white.

## Where it lives

- Scripts: `Assets/Scripts/SharedCamera.cs`, `Assets/Scripts/MooPrompt.cs`, `Assets/Scripts/LevelExit.cs`
- Scene: `Assets/Scenes/LV_01.unity` (copy of SampleScene, not created yet)
- Scene objects (planned): `Main Camera` holds `SharedCamera`. A TextMeshPro text on a Screen Space Overlay canvas holds `MooPrompt`. An exit object with a BoxCollider2D (Is Trigger on) holds `LevelExit`. A full-screen white Image on the canvas is the fade.

## Layout

Two screens side by side, each about 17.8 x 10 units (camera size 5, 16:9). Screen 1 centre is x = 0, screen 2 centre is x = 17.8. The level spans about x -8.9 to 26.7, y -5 to 5.

- Clearing 1 on screen 1: the start. Cow and crow start inside it.
- Clearing 2 on screen 2: the fruit tree. The crow reaches it low on energy.
- Exit on the right of screen 2.
- Tree walls frame the level and close off side paths.

## How it works

Order of events:

1. Scene loads. `LevelExit` sets the fade Image to alpha 1 (white) and fades it to 0 over `Fade Time`. `MooPrompt` starts its cycle at the beginning of a moo.
2. Every frame, `SharedCamera` moves toward the midpoint of the players (limited by `Min Position` / `Max Position`), smoothed by `Smooth Time`.
3. Every physics step, `SharedCamera` clamps each player's Rigidbody2D position into the camera's view, shrunk by `Edge Margin`. This is a hard invisible wall: neither animal can leave the screen, so the cow can't be left behind. (ponytail note in the script: fine for two players on one screen.)
4. The crow clears fog (see `fog.md`), the cow follows, rams the tree, the crow eats (see `players.md`, `trees.md`).
5. The cow enters the exit trigger. `LevelExit` starts fading to white. When alpha reaches 1 it loads `Next Scene`, or reloads the current scene if `Next Scene` is empty.

`MooPrompt`: `t = Time.timeSinceLevelLoad % Interval` (so a restart opens on a moo too). Alpha rises 0 to 1 over `Fade Time`, holds for `Hold Time`, falls back over `Fade Time`, then stays 0 until the next interval. The result is multiplied by `Max Alpha` and set on the text. Fixed screen corner, not the real direction of the herd. The owner places it with anchors on the canvas, not in code.

`LevelExit`: only the cow's collider triggers it (the `Cow` field). Only the Image's alpha changes, its RGB stays as set in the Inspector.

## Scene setup (planned)

Tree walls: plain circle sprites with a CircleCollider2D, colour olive `#6F7D35`, `SpriteRenderer > Order in Layer` 11, above the fog (order 10), so walls stay readable when fogged. Trees block the cow and the crow.

Physics layers (all in shared Project Settings):

| Object | Layer |
|---|---|
| Fog Grid | Fog |
| Crow | Crow |
| Cow | Cow |

`Edit > Project Settings > Physics 2D > Layer Collision Matrix`: Crow x Fog off, Crow x Cow off. So the crow is stopped by tree walls but still flies over fog and over the cow. The cow still collides with fog and walls.

The crow needs two CircleCollider2Ds: one with Is Trigger on (eats fruit) and one solid (stopped by tree walls).

## Tuning

| What | Object | Field | Current value |
|---|---|---|---|
| Players followed | Main Camera | SharedCamera > Players | not set up yet (planned: Cow, Crow Rigidbody2Ds) |
| Camera catch-up (s) | Main Camera | SharedCamera > Smooth Time | not set up yet (planned: 0.3) |
| Camera limits | Main Camera | SharedCamera > Min Position / Max Position | not set up yet (planned: (0, 0) and (17.8, 0)) |
| Hold-inside margin | Main Camera | SharedCamera > Edge Margin | not set up yet (planned: 0.5) |
| Seconds between moos | Moo text | MooPrompt > Interval | not set up yet (planned: 6) |
| Moo fade (s) | Moo text | MooPrompt > Fade Time | not set up yet (planned: 0.6) |
| Moo hold (s) | Moo text | MooPrompt > Hold Time | not set up yet (planned: 1) |
| Moo strength | Moo text | MooPrompt > Max Alpha | not set up yet (planned: 0.6) |
| Cow | Exit | LevelExit > Cow | not set up yet (planned: Cow's collider) |
| Fade image | Exit | LevelExit > Fade | not set up yet (planned: white full-screen Image) |
| Fade length (s) | Exit | LevelExit > Fade Time | not set up yet (planned: 1.5) |
| Next level | Exit | LevelExit > Next Scene | not set up yet (planned: empty, reloads LV_01) |
| Fog area / position / puffs / start clearings | Fog Grid | see `fog.md` | not set up yet (planned: 38 x 12 at (8.9, 0), 65 puffs, 2 clearings) |

## Gotchas

- Any scene loaded by `LevelExit` must be in `File > Build Profiles` scene list, or the load fails. LV_01 itself must be added too.
- The layers and the collision matrix live in shared Project Settings, so changing them affects every scene.
- The fog area must cover the full range the camera can see: camera centre limits plus half the view on each side (about x -8.9 to 26.7, y -5 to 5). Bare ground outside the fog area shows no fog.
- Camera limits assume 16:9. A wider window shows more than 17.8 units per screen, and the clamp won't hide the extra.
- The fade Image must have Raycast Target off, or it blocks clicks later when menus exist.

## Not built yet

- FMOD moo. Right now it is only text.
- Moo direction: the text does not point toward the herd.
- LV_02, and the exit loading it (`Next Scene` is empty).
- Real tree art for the walls (plain circles for now).
