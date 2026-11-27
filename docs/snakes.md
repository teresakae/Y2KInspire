# Snakes

## What it is

A snake sleeps coiled under the fog. It doesn't move until the crow first clears the fog over it. From then on it is awake for good: it stays coiled for a beat, then stretches out and wanders around its spot (back under fog too, hidden but still moving), mirroring its sprite left and right as it slithers, and coils up again whenever it rests. If the crow is on the ground within range, the snake coils as a warning. If the crow stays there for `Warn Time`, the snake eats it: the crow vanishes, a red "!" pops up on the snake, and the scene restarts after `Eaten Delay`. If the crow takes off or leaves range first, the snake calms down and carries on. If `Chase Range` is above 0 and the crow (grounded or flying) is within it, the snake slithers straight at the crow at `Chase Speed` instead of wandering; the warn and bite rules above still apply when it gets close. The cow kills a snake by ramming it with a charged dash: the snake switches to the `Dead` sprite (X eye), stops moving and fades out over `Dead Fade Time`, then is removed. While charging, the cow soft-locks onto the nearest live snake in front of it and dashes at it (see `players.md`, Cow ram); `Snake.CanBeLocked` (public) lets it skip snakes not yet revealed and ones already fading.

## Where it lives

- Script: `Assets/Scripts/Snake.cs`, on each snake object.
- Reads `CrowEnergy.IsGrounded` and `CowRam.IsStrongDash` (see `players.md`) and `FogGrid.IsClear` (see `fog.md`).
- Art: `Assets/Art/Snake/snake_coiled.png`, `snake_stretched.png` (130 pixels per unit); dead sprite `SPR_SnakeDead` (sub-sprite of `Assets/Art/Snake/cow and crow (2).png`, set to 130 pixels per unit so sizes match; the rest of that sheet is unused).
- Scene: `Snake` in SampleScene. In `Assets/Scenes/Level1.unity` the `Snake` object is inactive: snakes start in Level2, so Level1 has none.

## How it works

`Update`, in this order:

1. If the crow was eaten: count `Eaten Delay` down, then reload the active scene (direct reload until a lose screen exists). Nothing else runs.
2. Revealed check, until it first passes: `FogGrid.IsClear` at the snake's centre (fog below `Reveal Threshold` on the Fog Grid). Outside the fog area counts as clear. Once true it stays true. If `Fog` is empty the snake is awake from the start.
3. Warn check. Only if revealed, the crow is grounded and within `Warn Range`. On entering the warning the warn timer starts at `Warn Time`. At 0 the crow is eaten: the Crow object is switched off, `Eaten Mark` is switched on, and the delay in step 1 starts. The warning ends when the crow takes off or gets more than `Warn Range` + `Calm Margin` away. The margin stops a tired crow waddling at the edge of `Warn Range` from flipping the snake between coiled and chasing every frame (each restart also reset the bite timer).

`FixedUpdate` (`MovePosition` applies once per physics step, so moving from `Update` would lose distance on fast screens):

1. If eaten, not yet revealed, or warning: coiled sprite, no flip, upright, and the rest timer restarts at `Pause Time`. So after being revealed or calming down it stays coiled for one pause before moving.
2. Otherwise roam. First, if `Chase Range` > 0 and the Crow object is active and within that distance of the snake, it skips resting and slithers toward the crow at `Chase Speed` (same stretched sprite, head turn and flip as roaming). Otherwise, while resting it is coiled and counts `Pause Time` down; at 0 it picks a new target at `home + random point in Roam Radius`. While moving it shows `Stretched`, moves toward the target at `Move Speed`, turns its head toward travel (`atan2` plus `Sprite Angle`) and toggles `flipX` every `Flip Interval`. Within 0.05 units of the target it rests.

Kill: `OnTriggerEnter2D` and `OnTriggerStay2D`. If the other object has `CowRam` and `IsStrongDash` is true, the snake calls `CowRam.HitSnake()` (camera shake `Snake Shake` and the same squash-and-hop-back as a tree hit) and dies. With `Dead` empty it is destroyed at once. With `Dead` set it swaps to that sprite (unflipped, upright), turns its collider off, stops all behaviour (no roam, warn or bite), fades its sprite alpha to 0 over `Dead Fade Time`, then is destroyed. The collider is a trigger; the hop in `HitSnake` is what stops the dash.

Every kill by a strong dash (also with `Dead` empty) spawns `Bonk` (`Assets/Prefabs/PF_BonkPop`) at `Bonk Height` above the snake and fires `On Hit` (UnityEvent passing the snake's Transform; Level2 wires it to `Tip_Snake.Hit`, see `tutorial.md`). `PF_BonkPop`: TextMeshPro (3D) "bonk!" in `M_BonkText` (gold, plum outline), sorting order 32, script `BonkPop` (punch in, shake, rise 0.5, fade, destroys itself), child `PF_RamPuff` burst.

`home` is the snake's position at Awake.

Obstacles: snakes obey what the cow does. `Blocked(p)` is true when a non-trigger collider with `TreeLook` or `FruitTree` (trunks), or a `Water` collider, is within `Body Radius` of p (`Physics2D.OverlapCircle` with `Obstacles` as the layer mask), or when p is under a canopy (`TreeLook.CanopyCovers` / `FruitTree.CanopyCovers` with `Body Radius` as margin, so a snake never hides under leaves). `Start` caches every `TreeLook` and `FruitTree` (not `Awake`: `TreeLook` places canopies in its Awake) and warns "starts inside a tree, its leaves or water" if the home is blocked. A step into a blocked spot tries x only, then y only, so it slides along trunks like the cow; if both are blocked (or the slide is shorter than const `minSlide` 0.4 of a full step, so it doesn't wriggle on the spot) a roaming snake coils and rests for `Pause Time`, then picks a new target, and a chasing snake coils that step. A snake that is already inside something moves freely so it can get out. A new roam target is the first of 10 random points near home that is not blocked, else home.

## Adding a snake

1. Create an empty object `Snake`.
2. Add a SpriteRenderer with the coiled sprite. Set `Order in Layer` below the fog's (Level2 uses 14, fog 19).
3. Add a Rigidbody2D, `Body Type` Kinematic.
4. Add a Capsule Collider 2D with `Is Trigger` on, sized to the body.
5. Add the `Snake` script. Drag `Crow` into `Crow`, `Fog Grid` into `Fog`, and the two sprites into `Coiled` and `Stretched`. Optional: `SPR_SnakeDead` into `Dead`; `Chase Range` above 0 to make it hunt the crow.
6. Eaten mark: right-click Snake > `3D Object > Text - TextMeshPro`, name it `EatenMark`, text `!`, red, Font Size about 8, placed just above the snake. In `Extra Settings > Order in Layer` set 20 so it draws above the fog. Untick it (inactive). Drag it into `Snake > Eaten Mark`.
7. Set `Obstacles` to Default + Water (not Fog, not Crow). Size: set `Transform > Scale` (around 0.5 with the current art). The collider and the "!" scale with it; ranges don't.
8. Select the snake to see roam radius, warn range and chase range as wire circles.

## Tuning

| What | Object | Field | Current value |
|---|---|---|---|
| Size | Snake | Transform > Scale | 1 (feels too big; try 0.5) |
| "!" time before restart (s) | Snake | Snake > Eaten Delay | 1.5 (default) |
| Roam distance | Snake | Snake > Roam Radius | 3 (default) |
| Speed (units/s) | Snake | Snake > Move Speed | 1 (default) |
| Rest between points (s) | Snake | Snake > Pause Time | 1.5 (default) |
| Slither flip (s) | Snake | Snake > Flip Interval | 0.2 (default) |
| Head offset (deg) | Snake | Snake > Sprite Angle | -90 (default) |
| Warn distance | Snake | Snake > Warn Range | 1.5 (default) |
| Extra distance before a warning snake calms down | Snake | Snake > Calm Margin | 1 (default) |
| Warn time (s) | Snake | Snake > Warn Time | 1 (default) |
| Chase distance (0 = never chase) | Snake | Snake > Chase Range | 0 (default) |
| Chase speed (units/s) | Snake | Snake > Chase Speed | 2 (default) |
| What blocks it | Snake | Snake > Obstacles | Default + Water in Level2 (script default: everything) |
| Body size for bumping (units) | Snake | Snake > Body Radius | 0.35 (default) |
| Dead sprite (empty = vanish at once) | Snake | Snake > Dead | SPR_SnakeDead (to set) |
| Dead fade (s) | Snake | Snake > Dead Fade Time | 1 (default) |
| "bonk!" prefab (empty = none) | Snake | Snake > Bonk | PF_BonkPop |
| "bonk!" height (units) | Snake | Snake > Bonk Height | 0.6 (default), 0.2 in Level2 |
| Runs on a kill | Snake | Snake > On Hit | Level2: `Tip_Snake.Hit` |
| How clear counts as revealed | Fog Grid | FogGrid > Reveal Threshold | 0.5 (default) |
| Aim lock range / cone (units / deg) | Cow | CowRam > Aim Range / Aim Angle | 4 / 35 |
| Min charge to kill | Cow | CowRam > Min Charge | 0.5 (default) |

## Gotchas

- Hidden on screen = sorting order: the snake's `Order in Layer` must stay below the fog's (Level2: snakes 14, fog 19; the cow is 15, the crow 16, canopies 17). A snake at 14 draws over the ground and tufts but under the animals. Asleep = never yet revealed (`FogGrid.IsClear` at its centre). A snake partly out of the fog can be asleep but partly visible.
- Once awake it roams, warns and bites under fog too. In practice the crow clears fog around itself, so a bite usually happens in the open.
- `Stretched` empty: the snake keeps the coiled sprite while moving (logs an error at start). Before this guard, an empty field blanked the sprite and the snake looked like it teleported.
- The reload needs the scene in the build list. `SampleScene` is.
- Leave `Obstacles` without Fog: the fog colliders (pool) are on the Fog layer and would stop snakes under fog. Leave Crow off too, so the crow's collider doesn't block them.
- The crow counts as grounded when it stands still or is out of energy (`CrowEnergy.IsGrounded`). A tired crow hopping slowly is still grounded.
- A kill needs a dash charged to at least `CowRam > Min Charge`. A weak dash passes through.
- The snake ignores the cow otherwise and only targets the crow.
- The crow clears fog within 1 unit but the snake warns from 1.5, so a warning snake can sit at the fog edge.

## Not built yet

- Warning sound, vfx, screen shake.
- Lose screen. The bite reloads the scene directly.
- Balance pass on Level2's 16 snakes (see `level.md`).
