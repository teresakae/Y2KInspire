# Players

## What it is

Two animals on one keyboard. The cow (WASD) walks. The crow (arrow keys) flies faster. Both face the way they move. While the crow moves it switches to a wings-out sprite, snaps to face right, left, up or down (no smooth turn), and shows a shadow on the ground below it. Standing still, each goes back to its last facing sprite. The cow can charge and dash (Space) to ram trees; the crow has an energy bar that drains while it moves.

## Where it lives

- Scripts: `Assets/Scripts/PlayerControl.cs`, one copy on each of `Cow` and `Crow`. `CowRam.cs` goes on `Cow`, `CrowEnergy.cs` on `Crow`. `Water.cs` marks water colliders (river, pond).
- Scene objects: `Cow` (Rigidbody2D, BoxCollider2D, SpriteRenderer, child `Shadow`) and `Crow` (Rigidbody2D, SpriteRenderer, children `Shadow` and `FlyShadow`).
- Sprites: `Assets/Sprites/Cow/` (only `front.png` now, used by the SampleScene prototype; the level cow art is `Assets/Art/Cow`) and `Assets/Sprites/Crow/` (front, back, left, right, fly).
- Artist cow sheet: `Assets/Art/Cow/cow and crow_cow.png`, sliced into `cow_front/back/left/right`, `cow_walk_front/back/left/right_0..2` and `cow_crow_head`. Pivot Bottom Center on every slice; front/back slices are 316 px wide, side slices 276 px, so the feet stay put between frames. Hooked up to `Cow`: facing sprites and all four walks (back frames = Walk Up, front frames = Walk Down).

## How it works

Everything happens in `Update`, in this order:

1. Read the keyboard (`Keyboard.current`). Cow `Controls` = WASD: W up, S down, A left, D right. Arrows: the four arrow keys. `x` and `y` are each -1, 0 or 1. Opposite keys cancel to 0. If there is no keyboard, input is 0.
2. `moving` = any key held. While moving, `lastDir` (public, hidden) = `(x, y)`. `CowRam` aims with it. If `aimOnly` (public, hidden, set by `CowRam` while charging) is on, the facing sprite is applied and `Update` returns here: no walk, no velocity.
3. Pick the facing sprite: right if x > 0, left if x < 0, otherwise back if y > 0, front if y < 0. Left/right wins on diagonals. If nothing is pressed, keep the last facing.
4. Pick the shown sprite. Flying (moving and `Fly` is set): `Fly`. Otherwise, moving right with `Walk Right` frames: cycle them, frame = `(int)(Time.time * Walk Fps) % length`. Same for left with `Walk Left`. With no left/right input: up uses `Walk Up`, down uses `Walk Down`. Otherwise the last facing sprite. Standing still always shows the facing sprite. Show `Fly Shadow` only while moving (if set); at the same time hide `Ground Shadow` (if set).
5. If flying, the object snaps (no smooth turn) to one of four headings: right 0°, left 180°, up 90°, down -90°, picked like the facing sprite (left/right wins on diagonals), plus `Fly Angle` as an offset: `fly.png` has its head at the bottom, so 90. Tired and moving: it sways by `Waddle Angle` at `Waddle Speed`. Otherwise it is upright.
6. If `Fly Shadow` is set, keep it at its starting offset from the object in world space (straight below, not swung round by the spin), but give it the crow's rotation, so its long side follows the wings (wings are horizontal flying up/down, vertical flying left/right).
7. If `Fly Particles` is set, its emission is on only while flying (moving, not tired).
8. Set the Rigidbody2D's `linearVelocity` to `(x, y) * Mov Speed * speedScale`. `speedScale` (public, hidden, default 1) is set by `CrowEnergy`.

`Start` caches the Rigidbody2D and SpriteRenderer, remembers the starting sprite as the facing, and records the shadow's offset from the object and sets its scale so it measures `Fly Shadow Size` in the world (sprite and parent scale are divided out).

The cow has `Fly` and `Fly Shadow` empty, so the fly parts of steps 4 to 6 do nothing for it: it never rotates and swaps only the facing sprites and walk frames. Its `Shadow` child is static.

## Cow ram

Juice (all in `CowRam`, strength scales with the charge): while dashing the cow stretches along the dash (`Dash Stretch`, longer and thinner). On touching any tree (fruit or wall) it squashes along the dash (`Hit Squash`) and hops back for `Hop Time` at `Hop Speed` x charge, steering stays off until then, then the scale goes back to normal (`EndDash`, and `OnDisable` as a safety). A strong hit on a fruit tree also calls `SharedCamera.Shake(Hit Shake x charge)`. A weak dash or a wall tree only gets `Weak Shake`. `SharedCamera > Shake Time` (0.25) is how long a shake fades; the shake is taken off before the camera follows, so it never drifts.

`CowRam` on the cow. Hold Space (`Ram Key`) to charge, release to dash. Order of events:

1. Space pressed: charge = 0, velocity zero, `PlayerControl.aimOnly` = true. `PlayerControl` stays on but only reads keys: WASD still updates `lastDir` (8-way) and the facing sprite, and sets no velocity, so the cow stands still and can re-aim while charging.
2. Held: charge rises 0 to 1 over `Charge Time`. The cow squashes: Y scale lerps toward `Squash`, X grows by `(1 - Squash) * charge`. The frame the charge crosses `Min Charge` (once per charge) is the ready cue: the cow tints toward `Ready Flash Color`, fading over `Ready Flash Time`, and the camera kicks once (`Ready Shake`). While it charges the cow trembles: a random squash jitter each frame (`Charge Rumble` at full charge, growing with the charge), x and y opposite so the size holds.
   Aim lock: each frame the nearest live snake (not null, active, revealed and not dying; `Snake.CanBeLocked`) or, if `Lock Trees` is on, fruit tree (active `FruitTree`) within `Aim Range` of the cow and within `Aim Angle` of `lastDir` gets a crosshair (`Crosshair Prefab` instance, created in `Start`, hidden when there is no target). It is `Crosshair Color` below `Min Charge` and `Crosshair Ready Color` at `Crosshair Ready Scale` size above it, and spins at `Crosshair Spin`. At or above `Min Charge` it also swaps to `Crosshair Ready Sprite` (back to the prefab's sprite below it). Empty = keep the prefab sprite. Snakes and fruit trees are cached in `Start` (`FindObjectsByType`), so ones spawned later are not locked.
3. Released: scale and tint restored, crosshair hidden. If a snake or fruit tree is locked, `lastDir` is set to the direction to it (so the dash, `AlongScale` and the tree check all agree). `aimOnly` goes off and `PlayerControl` is disabled for the dash. Dash lasts `Dash Time` seconds at `lastDir.normalized * Dash Speed * charge`. The charge is remembered as the dash charge.
4. Dash hits anything (`OnCollisionEnter2D`): if the dash charge is at least `Min Charge` and the thing has a `FruitTree`, the cow calls its `Hit()`. Any new hit ends the dash. A trunk the cow was already touching when it started charging also counts (`OnCollisionStay2D`), but only if the cow is dashing toward it, so backing off a tree drops nothing.
5. Dash ends (timer or hit): velocity zero, `PlayerControl` enabled again.

Crosshair prefab: `Assets/Prefabs/PF_AimCrosshair` (SpriteRenderer, `crosshairs_ring` from `Assets/Art/UI/crosshairs.png`, a ring with 4 ticks, 172 px at 172 pixels per unit = 1 unit, scale 1.2, default sprite material, Order in Layer 18: above snakes 14 and animals, below fog 19 so it never shows a snake hidden by fog). The colour comes from CowRam. Set in Level1 and Level2 on `Cow > CowRam > Crosshair Prefab`.

The cow calls the tree, not the other way round, so there is no collision-callback order to worry about. See `trees.md`.

`IsStrongDash` (public) is true while dashing with a dash charge of at least `Min Charge`. `Snake` checks it to die to the cow, see `snakes.md`.

## Crow energy

`CrowEnergy` on the crow. Energy starts at `Max Energy` and drains `Drain Per Second` while the Rigidbody2D moves (speed above about 0.1). It is clamped to 0..max and written to `PlayerControl.speedScale`: 1 with energy, `Tired Speed Scale` at zero (the crow hops slowly instead of being stuck). If `Bar` is set, `fillAmount = energy / max`. `Eat(amount)` adds energy, called by `Fruit` when the crow touches it. `IsGrounded` (public) is true when energy is 0 or the Rigidbody2D is standing still, so standing still counts as landed and a tired hopping crow still counts as down. `Snake` reads it, see `snakes.md`.

Drowning: when the crow is landed (standing still, or at zero energy) over a collider with a `Water` component (`Water.cs`, an empty marker) for more than `Land Grace` seconds, it drowns: input stops, its sprites and bar hide, the camera shakes (`Drown Shake`), and after `Drown Delay` seconds the lose screen shows "Crow cant swim!" (see `menu.md`, Lose screen). The grace stops a quick change of direction mid-flight from counting as landing. In Level2 `River` and `Pond` have `Water`; the pond's collider has a hole over the island, so a crow can land there safely.

## Tuning

On the `PlayerControl` component of each object.

| What | Object | Field | Current value |
|---|---|---|---|
| Which keys | Cow | PlayerControl > Controls | WASD |
| Which keys | Crow | PlayerControl > Controls | Arrows |
| Speed (units/s) | Cow | PlayerControl > Mov Speed | 3 |
| Speed (units/s) | Crow | PlayerControl > Mov Speed | 5 |
| Facing sprites | Cow | PlayerControl > Front / Back / Left / Right | Art/Cow/cow and crow_cow.png: cow_front, cow_back, cow_left, cow_right |
| Facing sprites | Crow | PlayerControl > Front / Back / Left / Right | Sprites/Crow/front, back, left, right |
| Moving sprite | Cow | PlayerControl > Fly | empty |
| Moving sprite | Crow | PlayerControl > Fly | Sprites/Crow/fly |
| Moving shadow | Cow | PlayerControl > Fly Shadow | empty |
| Moving shadow | Crow | PlayerControl > Fly Shadow | FlyShadow (child; its scale is set by code from Fly Shadow Size) |
| Fly shadow size (world units: along wings, across body) | Crow | PlayerControl > Fly Shadow Size | 1.45 x 0.5 (wingspan is 1.82, so about 80%) |
| Resting shadow, hidden while the fly shadow shows | Crow | PlayerControl > Ground Shadow | Shadow (child) |
| Walk frames | Cow | PlayerControl > Walk Left / Walk Right / Walk Up / Walk Down | cow_walk_left_0..2 / cow_walk_right_0..2 / cow_walk_back_0..2 / cow_walk_front_0..2 |
| Walk frames | Crow | PlayerControl > Walk Left / Walk Right / Walk Up / Walk Down | not set up yet (empty) |
| Walk speed (frames/s) | Cow, Crow | PlayerControl > Walk Fps | 8 (default, not set up yet) |
| Fly sprite size | `Assets/Sprites/Crow/fly.png` | Import > Pixels Per Unit | 70 (was 100; the other crow sprites are 100). Wing-out art is drawn small in its canvas, so 70 makes it about 1.43x bigger. Lower = bigger |
| Fly sprite rotation offset | Crow | PlayerControl > Fly Angle | 90 (default, check in scene) |
| Ram key | Cow | CowRam > Ram Key | not set up yet (default Space) |
| Ram juice | Cow | CowRam > Cam | Main Camera |
| Ram juice | Cow | CowRam > Dash Stretch / Hit Squash / Hop Time / Hop Speed / Hit Shake / Weak Shake | 1.2 / 0.7 / 0.15 / 5 / 0.3 / 0.04 |
| Ready cue | Cow | CowRam > Ready Flash Color / Ready Flash Time / Ready Shake | (1, 0.95, 0.85) / 0.12 / 0.06 |
| Charge trembles (0 = none) | Cow | CowRam > Charge Rumble | 0.06 (at full charge) |
| Snake kill shake | Cow | CowRam > Snake Shake | 0.35 |
| Aim lock | Cow | CowRam > Crosshair Prefab / Aim Range / Aim Angle / Lock Trees | PF_AimCrosshair / 4 / 35 / on |
| Crosshair look | Cow | CowRam > Crosshair Color / Crosshair Ready Color / Crosshair Ready Scale / Crosshair Spin | gold E8B23A at 60% / (1, 0.95, 0.75) / 1.3 / 90 |
| Crosshair ready sprite | Cow | CowRam > Crosshair Ready Sprite | crosshairs_flower |
| Fly particles (optional) | Crow | PlayerControl > Fly Particles | `Crow > FlySpecks` (see `particles.md`) |
| Ram charge / dash | Cow | CowRam > Charge Time / Min Charge / Dash Speed / Dash Time / Squash | not set up yet (defaults 0.8 / 0.5 / 12 / 0.25 / 0.8) |
| Energy | Crow | CrowEnergy > Max Energy / Drain Per Second / Tired Speed Scale / Bar | not set up yet (defaults 100 / 10 / 0.3 / empty) |
| Energy (Level1) | Crow | CrowEnergy > Drain Per Second | 15 (tired scale 0.07) |
| Energy (Level2) | Crow | CrowEnergy > Drain Per Second / Tired Speed Scale | 4 / 0.3 |
| Drowning | Crow | CrowEnergy > Drown Delay / Land Grace / Drown Shake | 0.6 / 0.3 / 0.4 |
| Crow size | Crow | Transform > Scale | 0.5 |
| Cow collider (feet only) | Cow | BoxCollider2D > Size / Offset | 1.6 x 0.8 / (0, -0.8) |
| Body type, gravity | Cow, Crow | Rigidbody2D > Body Type / Gravity Scale | Dynamic / 0 |
| Rotation locked | Cow, Crow | Rigidbody2D > Constraints | Freeze Rotation Z |
| Smoothing | Cow, Crow | Rigidbody2D > Interpolate | Interpolate |

## Gotchas

- Diagonal movement is not normalised, so moving diagonally is about 1.41 times faster.
- Speed is set directly every frame, with no acceleration. Any push from physics (knockback) is overwritten the next frame. The ram gets around this by disabling `PlayerControl` for the charge and dash.
- Velocity is set in `Update`, not `FixedUpdate`. Interpolate on the Rigidbody2D smooths this.
- The crow has only its trigger collider, so nothing solid stops it. It flies over fog colliders and the cow. The cow stops at fog because its own BoxCollider2D hits the fog cells (see `fog.md`).
- The flying snap sets `transform.rotation` directly even though Rigidbody2D rotation is frozen. This works now. Watch it if a collider is added to the crow.
- The `Shadow` child spins with the crow (it is a child), so while flying it swung to the side of the sprite. `Ground Shadow` hides it during flight and the unrotated `Fly Shadow` takes over. `FlyShadow` had scale (0, -1, 0), so it was invisible; its size now comes from `Fly Shadow Size`.
- `Fly` and `Fly Shadow` are both optional and independent. If `Fly` is empty but `Fly Shadow` is set, the shadow still shows while moving but the object doesn't rotate.
- The sprite for the cow facing and the crow facing are set per-object through the Inspector; no assets are looked up by name.
- `PlayerControl` is aim-only during the cow's charge (WASD turns the aim and facing sprite, no walking) and disabled during the dash and hop, so WASD does nothing then and the walk sprites stop updating.
- The crow needs a trigger Circle Collider 2D (Is Trigger on) to eat fruit. It has no collider otherwise, and a non-trigger one would make it collide with things.
- The fly sprite still shows while the crow is tired; only its speed drops.
- Both animals share one script and one `Controls` enum. Click the Game view before testing, or the keyboard won't register.

## Not built yet

- Gamepad support (design: keyboard now, gamepads later). The script reads `Keyboard.current` only. Option: use `InputSystem_Actions.inputactions` with one action map per player.
- True landing state for the crow. "Landed" is just "not moving". No landing key, no vulnerability.
- The crow still clears fog while tired.
- Fruit tree hanging fruit never regrows: after the last one is rammed down the tree drops nothing (see `trees.md`).
- Juice beyond the charge squash.
- Win: no link between player state and level flow (lose is handled by `LoseScreen`).
