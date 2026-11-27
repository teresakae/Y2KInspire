# Tutorial

Status: built in both levels. Not play-tested by anyone but the owner.

## What it is

Hand-drawn key pictures and small signs teach the controls in the moment. Level1 has three drawings in the world at the start (cow keys, crow keys, space key). Signs ("the herd is calling. / follow it!", "fruit tree. RAM!", "danger. RAM!", "water. crow scout!") pop up over the thing they are about once players can see it, then close when the player does it or walks away. Everything wobbles like a boiling pencil line.

## Where it lives

- Scripts: `Assets/Scripts/TutorialTip.cs`. Shader: `Assets/Shaders/Scribble.shader` (`Cowncrow/Scribble`). Material: `Assets/Materials/M_Scribble.mat`.
- Prefab: `Assets/Prefabs/PF_TutorialTip.prefab`.
- Art: `Assets/Art/Title/cow and crow controls.png` (slices `controls_cow`, `controls_crow`, `controls_space`) and `Assets/Art/UI/cow and crow ui.png` (slice `cow and crow ui_6`, the sign background, 50 px 9-slice borders). Font: `Kkscribble SDF`.
- Scene objects: Level1 `Tutorial` (3 SpriteRenderers) and `TutorialTips`; Level2 `TutorialTips` (only `Tip_Snake` and `Tip_Water`).

## Scribble material

`M_Scribble` makes lines wobble in steps (a redraw every 1/4 s). It works on SpriteRenderers and on world-space UI Images. It reads `_Time`, so it freezes while paused. The controls png has Mesh Type Full Rect, so the wobble isn't cut off by a tight mesh.

| What | Property | Value |
|---|---|---|
| Wobble size (fraction of the texture) | `_Amount` | 0.002 (shader default 0.004) |
| Wobble lines across the texture | `_Scale` | 14 (default 40) |
| Redraws per second | `_Fps` | 4 |
| Texture width / height (keeps noise cells square) | `_Aspect` | 1.74 |
| Whole-drawing shake (object units) | `_Jitter` | 0 |
| Whole-drawing tilt (degrees) | `_Tilt` | 0.25 |

`_Aspect` is a copy of the texture's width / height. Set it again if you use another picture.

## PF_TutorialTip

```
PF_TutorialTip      TutorialTip (active root)
  Sign              world-space Canvas, sorting order 31, scale 0.01, inactive
                    Image = cow and crow ui_6 (sliced), VerticalLayoutGroup, ContentSizeFitter
    Row             Text (Kkscribble SDF)
    Key             Image with M_Scribble, sprite controls_space by default
```

The root stays active so it can wait. The `Sign` is off until the tip opens.

## How it works

1. `Start`: checks `Cam`, `Sign`, `Tip Id` (LogError if empty). If this id is already in the seen list, the whole tip turns itself off.
2. Waiting: after `Show Delay` seconds, each frame it looks through `Targets` (skipping destroyed or inactive ones). A target counts as seen when it is on screen (inside `View Margin` of the edge) and, if `Fog` is set, `FogGrid.IsClear` is true there. The first one seen opens the sign.
3. Open: the sign pops in (`Pop In` curve), floats at the target plus `Offset`, and is kept inside the camera view by `Edge Padding`. It slides off screen with its target instead of sticking to the edge when the players walk away. `Key` squashes like a pressed key (`Key Press` curve, loop `Key Loop Time`); leave the `Key` field empty for no press animation.
4. Close (any of these): `Max Time` passes (0 = never), the `Mover` walks `Move Distance` away from where it was when the sign opened, the target is destroyed or inactive (fruit eaten, snake dead), or something calls `Done()`. Then it pops out (`Pop Out`) and the tip turns itself off.
5. `Done()` on a waiting tip skips it for good (the player did it already).
6. `Show Count` (default 1): how many different targets get a sign before the tip is finished. After each close the tip goes back to waiting (words, key and tilt reset) until `Show Count` signs have shown; each target gets at most one. The id is added to the seen list only after the last one.
7. `Hit(Transform)`: if the sign is open on that target, it swaps the `Label` text to `Hit Text` (if set) and closes with a hit animation: sign shakes (`Hit Shake`, `Hit Shake Speed`), punches (`Hit Pop` curve over `Hit Time`), and the key slams down (`Key Slam`) then springs back. A hit on any other transform is ignored.

Seen list: a static list of tip ids, per play session. Closing a tip adds its id. A death reload does not replay finished tips. A browser refresh does (the list is empty again), and so does the menu's Start (`MainMenu.StartGame` calls `TutorialTip.ForgetAll`). A tip still open when you die shows again. It is reset at play start, so the editor's skipped domain reload doesn't keep it.

Level1 only: FruitTree `On Drop` is wired to `Tip_Tree.Done`, so the tree sign closes when a fruit falls. (`Tip_Fruit` and `TutorialTip.SetTarget`, which pointed a sign at the fallen fruit, were removed.)

## Tips in the levels

No sign explains the fog any more.

Level1 `TutorialTips`:

| Tip | Sign text | Target | Key | Offset | Closes when |
|---|---|---|---|---|---|
| `Tip_Herd` (id `herd`) | the herd is calling. / follow it! (two lines) | `Cow` (no fog check; the sign sits above the calf and follows it) | none | (0, 3.5) | `Max Time` 6, long enough to overlap the next moo bubble (one every 6 s). No `Mover`. `Show Delay` 1.6 |
| `Tip_Tree` | fruit tree. RAM! | the fruit tree | Space | (0, 5.3) | a fruit drops (On Drop) |

Level2 `TutorialTips` (no tree or fruit tips; those exist in Level1 only):

| Tip | Sign text | Targets | Key | Offset | Closes when |
|---|---|---|---|---|---|
| `Tip_Snake` | danger. RAM! | the 16 snakes | Space | (0, 1.9) | the snake is hit (`Hit`: shake, punch, key slam, words become "hit!"), or `Max Time` 6. `Show Count` 2: the first two snakes seen, whichever they are. |
| `Tip_Water` | water. crow scout! | `PondShore/Shore0` to `Shore3` around the pond | none | (0, 2.4) | `Max Time` 5 |

`Tip_Snake` is a small board: `Sign` scale 0.0062, board alpha 0.9, gentle pop. `Label` = `Sign/Row/Text`, `Hit Text` = "hit!". Every snake's `Snake > On Hit` has a dynamic `TutorialTip.Hit` call to `Tip_Snake` (passes the snake's Transform).

Level1 `Tutorial` (root): `controls_cow`, `controls_space` and `controls_crow` SpriteRenderers, order 23 (above fog 19, mist 21, motes 22), tinted plum #2E1A2B, material `M_Scribble`. Their positions follow the start clearing, so move them with it.

## Adding a tip

1. Drag `PF_TutorialTip` under the level's `TutorialTips`.
2. On its `TutorialTip`: set `Tip Id` (unique), `Cam` (Main Camera), `Fog` (Fog Grid, or empty to ignore fog), `Targets`, `Offset`, and `Sign` (child `Sign`). Set `Key` to the child inside it.
3. Write the text in `Sign > Row > Text`.
4. Change the sprite on `Sign > Key`, or disable `Key` (and clear the `Key` field) if no picture is needed.
5. Optional: `Max Time`, `Mover`, `Show Delay`. To close on an event, add a `TutorialTip.Done` call to that event's list.

## Tuning

All on `TutorialTip`: `Show Delay` 0.4 (script default), `View Margin` 0.05, `Edge Padding` 0.3, `Pop In Time` 0.4, `Pop Out Time` 0.25, `Key Loop Time` 1.4, `Key Squash` 0.5. Per-tip values are in the tables above. `Guide Clip` (`UI_guide`) plays in `Open()` when a tip appears, through an AudioSource found in `Awake` (added with Play On Awake off if missing); empty = silent.

## Gotchas

- White art vanishes on the white fog, so the drawings are tinted plum.
- A tip whose `Fog` is set never opens if its target stays under fog.
- Reusing a `Tip Id` hides the second tip once the first has closed.
