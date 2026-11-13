# Players

## What it is

Two animals on one keyboard. The cow (WASD) walks. The crow (arrow keys) flies faster. Both face the way they move. While the crow moves it switches to a wings-out sprite, turns toward its direction of travel, and shows a shadow on the ground below it. Standing still, each goes back to its last facing sprite.

## Where it lives

- Script: `Assets/Scripts/PlayerControl.cs`, one copy on each of `Cow` and `Crow`.
- Scene objects: `Cow` (Rigidbody2D, BoxCollider2D, SpriteRenderer, child `Shadow`) and `Crow` (Rigidbody2D, SpriteRenderer, children `Shadow` and `FlyShadow`).
- Sprites: `Assets/Sprites/Cow/` (front, back, left, right) and `Assets/Sprites/Crow/` (front, back, left, right, fly).

## How it works

Everything happens in `Update`, in this order:

1. Read the keyboard (`Keyboard.current`). Cow `Controls` = WASD: W up, S down, A left, D right. Arrows: the four arrow keys. `x` and `y` are each -1, 0 or 1. Opposite keys cancel to 0. If there is no keyboard, input is 0.
2. Pick the facing sprite: right if x > 0, left if x < 0, otherwise back if y > 0, front if y < 0. Left/right wins on diagonals. If nothing is pressed, keep the last facing.
3. `moving` = any key held. Show `Fly` while moving (if `Fly` is set), otherwise the last facing sprite. Show `Fly Shadow` only while moving (if set).
4. If moving and `Fly` is set, the object counts as flying: it rotates toward its direction of travel (the sprite's top is forward) at up to 900 degrees per second. Otherwise it rotates back to upright at the same rate.
5. If `Fly Shadow` is set, keep it unrotated and at its starting offset from the object, so it stays flat while the crow spins.
6. Set the Rigidbody2D's `linearVelocity` to `(x, y) * Mov Speed`.

`Start` caches the Rigidbody2D and SpriteRenderer, remembers the starting sprite as the facing, and records the shadow's offset from the object.

The cow has `Fly` and `Fly Shadow` empty, so steps 3 to 5 do nothing for it: it never rotates and swaps only the four facing sprites. Its `Shadow` child is static.

## Tuning

On the `PlayerControl` component of each object.

| What | Object | Field | Current value |
|---|---|---|---|
| Which keys | Cow | PlayerControl > Controls | WASD |
| Which keys | Crow | PlayerControl > Controls | Arrows |
| Speed (units/s) | Cow | PlayerControl > Mov Speed | 3 |
| Speed (units/s) | Crow | PlayerControl > Mov Speed | 5 |
| Facing sprites | Cow | PlayerControl > Front / Back / Left / Right | Sprites/Cow/front, back, left, right |
| Facing sprites | Crow | PlayerControl > Front / Back / Left / Right | Sprites/Crow/front, back, left, right |
| Moving sprite | Cow | PlayerControl > Fly | empty |
| Moving sprite | Crow | PlayerControl > Fly | Sprites/Crow/fly |
| Moving shadow | Cow | PlayerControl > Fly Shadow | empty |
| Moving shadow | Crow | PlayerControl > Fly Shadow | FlyShadow (child) |
| Crow size | Crow | Transform > Scale | 0.5 |
| Cow collider size | Cow | BoxCollider2D > Size | 2.32 x 2.46 |
| Body type, gravity | Cow, Crow | Rigidbody2D > Body Type / Gravity Scale | Dynamic / 0 |
| Rotation locked | Cow, Crow | Rigidbody2D > Constraints | Freeze Rotation Z |
| Smoothing | Cow, Crow | Rigidbody2D > Interpolate | Interpolate |

Not in the Inspector (hard-coded in the script): turn rate 900 degrees per second.

## Gotchas

- Diagonal movement is not normalised, so moving diagonally is about 1.41 times faster.
- Speed is set directly every frame, with no acceleration. Any push from physics (knockback, ramming force) is overwritten the next frame. Ramming will need a different approach, such as a short timer that skips this line.
- Velocity is set in `Update`, not `FixedUpdate`. Interpolate on the Rigidbody2D smooths this.
- The crow has no collider, so nothing stops it. It flies over fog colliders and the cow. The cow stops at fog because its own BoxCollider2D hits the fog cells (see `fog.md`).
- The flying turn sets `transform.rotation` directly even though Rigidbody2D rotation is frozen. This works now. Watch it if a collider is added to the crow.
- `Fly` and `Fly Shadow` are both optional and independent. If `Fly` is empty but `Fly Shadow` is set, the shadow still shows while moving but the object doesn't rotate.
- The sprite for the cow facing and the crow facing are set per-object through the Inspector; no assets are looked up by name.
- Both animals share one script and one `Controls` enum. Click the Game view before testing, or the keyboard won't register.

## Not built yet

- Gamepad support (design: keyboard now, gamepads later). The script reads `Keyboard.current` only. Option: use `InputSystem_Actions.inputactions` with one action map per player.
- "Landed" for the crow is not a state. The crow is just "not moving". Nothing stops it clearing fog, no vulnerability, no landing key.
- Crow energy: nothing drains while flying, nothing happens at zero, no fruit to eat.
- Cow ramming trees or snakes: no ram action, no ram collision handling.
- Squash-and-stretch and other juice.
- Restart, win and lose: no link between player state and level flow.
