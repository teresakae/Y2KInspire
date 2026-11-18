# Players

## What it is

Two animals on one keyboard. The cow (WASD) walks. The crow (arrow keys) flies faster. Both face the way they move. While the crow moves it switches to a wings-out sprite, turns toward its direction of travel, and shows a shadow on the ground below it. Standing still, each goes back to its last facing sprite. The cow can charge and dash (Space) to ram trees; the crow has an energy bar that drains while it moves.

## Where it lives

- Scripts: `Assets/Scripts/PlayerControl.cs`, one copy on each of `Cow` and `Crow`. `CowRam.cs` goes on `Cow`, `CrowEnergy.cs` on `Crow`.
- Scene objects: `Cow` (Rigidbody2D, BoxCollider2D, SpriteRenderer, child `Shadow`) and `Crow` (Rigidbody2D, SpriteRenderer, children `Shadow` and `FlyShadow`).
- Sprites: `Assets/Sprites/Cow/` (front, back, left, right) and `Assets/Sprites/Crow/` (front, back, left, right, fly).
- Artist cow sheet: `Assets/Art/Cow/cow and crow_cow.png`, sliced into `cow_front/back/left/right`, `cow_walk_front/back/left/right_0..2` and `cow_crow_head`. Pivot Bottom Center on every slice; front/back slices are 316 px wide, side slices 276 px, so the feet stay put between frames. Hooked up to `Cow`: facing sprites and all four walks (back frames = Walk Up, front frames = Walk Down).

## How it works

Everything happens in `Update`, in this order:

1. Read the keyboard (`Keyboard.current`). Cow `Controls` = WASD: W up, S down, A left, D right. Arrows: the four arrow keys. `x` and `y` are each -1, 0 or 1. Opposite keys cancel to 0. If there is no keyboard, input is 0.
2. `moving` = any key held. While moving, `lastDir` (public, hidden) = `(x, y)`. `CowRam` aims with it.
3. Pick the facing sprite: right if x > 0, left if x < 0, otherwise back if y > 0, front if y < 0. Left/right wins on diagonals. If nothing is pressed, keep the last facing.
4. Pick the shown sprite. Flying (moving and `Fly` is set): `Fly`. Otherwise, moving right with `Walk Right` frames: cycle them, frame = `(int)(Time.time * Walk Fps) % length`. Same for left with `Walk Left`. With no left/right input: up uses `Walk Up`, down uses `Walk Down`. Otherwise the last facing sprite. Standing still always shows the facing sprite. Show `Fly Shadow` only while moving (if set).
5. If flying, the object rotates toward its direction of travel at up to `Turn Rate` degrees per second, plus `Fly Angle` as an offset: `fly.png` has its head at the bottom, so 90. Otherwise it rotates back to upright at the same rate.
6. If `Fly Shadow` is set, keep it unrotated and at its starting offset from the object, so it stays flat while the crow spins.
7. Set the Rigidbody2D's `linearVelocity` to `(x, y) * Mov Speed * speedScale`. `speedScale` (public, hidden, default 1) is set by `CrowEnergy`.

`Start` caches the Rigidbody2D and SpriteRenderer, remembers the starting sprite as the facing, and records the shadow's offset from the object.

The cow has `Fly` and `Fly Shadow` empty, so the fly parts of steps 4 to 6 do nothing for it: it never rotates and swaps only the facing sprites and walk frames. Its `Shadow` child is static.

## Cow ram

`CowRam` on the cow. Hold Space (`Ram Key`) to charge, release to dash. Order of events:

1. Space pressed: `PlayerControl` is disabled (it would overwrite the velocity every frame), velocity set to zero, charge = 0.
2. Held: charge rises 0 to 1 over `Charge Time`. The cow squashes: Y scale lerps toward `Squash`, X grows by `(1 - Squash) * charge`.
3. Released: scale restored, dash starts for `Dash Time` seconds at `lastDir.normalized * Dash Speed * charge`. The charge is remembered as the dash charge.
4. Dash hits anything (`OnCollisionEnter2D`): if the dash charge is at least `Min Charge` and the thing has a `FruitTree`, the cow calls its `Hit()`. Any new hit ends the dash. A trunk the cow was already touching when it started charging also counts (`OnCollisionStay2D`), but only if the cow is dashing toward it, so backing off a tree drops nothing.
5. Dash ends (timer or hit): velocity zero, `PlayerControl` enabled again.

The cow calls the tree, not the other way round, so there is no collision-callback order to worry about. See `trees.md`.

`IsStrongDash` (public) is true while dashing with a dash charge of at least `Min Charge`. `Snake` checks it to die to the cow, see `snakes.md`.

## Crow energy

`CrowEnergy` on the crow. Energy starts at `Max Energy` and drains `Drain Per Second` while the Rigidbody2D moves (speed above about 0.1). It is clamped to 0..max and written to `PlayerControl.speedScale`: 1 with energy, `Tired Speed Scale` at zero (the crow hops slowly instead of being stuck). If `Bar` is set, `fillAmount = energy / max`. `Eat(amount)` adds energy, called by `Fruit` when the crow touches it. `IsGrounded` (public) is true when energy is 0 or the Rigidbody2D is standing still, so standing still counts as landed and a tired hopping crow still counts as down. `Snake` reads it, see `snakes.md`.

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
| Moving shadow | Crow | PlayerControl > Fly Shadow | FlyShadow (child) |
| Walk frames | Cow | PlayerControl > Walk Left / Walk Right / Walk Up / Walk Down | cow_walk_left_0..2 / cow_walk_right_0..2 / cow_walk_back_0..2 / cow_walk_front_0..2 |
| Walk frames | Crow | PlayerControl > Walk Left / Walk Right / Walk Up / Walk Down | not set up yet (empty) |
| Walk speed (frames/s) | Cow, Crow | PlayerControl > Walk Fps | 8 (default, not set up yet) |
| Fly sprite rotation offset | Crow | PlayerControl > Fly Angle | 90 (default, check in scene) |
| Turn rate (deg/s) | Cow, Crow | PlayerControl > Turn Rate | 900 (default, check in scene) |
| Ram key | Cow | CowRam > Ram Key | not set up yet (default Space) |
| Ram charge / dash | Cow | CowRam > Charge Time / Min Charge / Dash Speed / Dash Time / Squash | not set up yet (defaults 0.8 / 0.5 / 12 / 0.25 / 0.8) |
| Energy | Crow | CrowEnergy > Max Energy / Drain Per Second / Tired Speed Scale / Bar | not set up yet (defaults 100 / 10 / 0.3 / empty) |
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
- The flying turn sets `transform.rotation` directly even though Rigidbody2D rotation is frozen. This works now. Watch it if a collider is added to the crow.
- `Fly` and `Fly Shadow` are both optional and independent. If `Fly` is empty but `Fly Shadow` is set, the shadow still shows while moving but the object doesn't rotate.
- The sprite for the cow facing and the crow facing are set per-object through the Inspector; no assets are looked up by name.
- `PlayerControl` is disabled during the cow's charge and dash, so WASD does nothing then, and the walk sprites stop updating.
- The crow needs a trigger Circle Collider 2D (Is Trigger on) to eat fruit. It has no collider otherwise, and a non-trigger one would make it collide with things.
- The fly sprite still shows while the crow is tired; only its speed drops.
- Both animals share one script and one `Controls` enum. Click the Game view before testing, or the keyboard won't register.

## Not built yet

- Gamepad support (design: keyboard now, gamepads later). The script reads `Keyboard.current` only. Option: use `InputSystem_Actions.inputactions` with one action map per player.
- True landing state for the crow. "Landed" is just "not moving". No landing key, no vulnerability.
- The crow still clears fog while tired.
- Fruit tree hanging fruit never regrows: after the last one is rammed down the tree drops nothing (see `trees.md`).
- Juice beyond the charge squash.
- Restart, win and lose: no link between player state and level flow.
