# Fog

## What it is

A green-brown fog covers a 20 x 12 unit area. The crow cuts a soft-edged round hole in it wherever it flies. The cow cannot walk into fog, so it can only go where the crow has cleared. A hole stays open for 5 seconds after the crow leaves, then the fog fades back in. A start clearing and the ground around the cow never fog back in.

## Where it lives

- Script: `Assets/Scripts/FogGrid.cs`
- Scene object: `Fog Grid` (has the `FogGrid` component)
- Scene object: `Clearing` (empty marker for the start clearing; `Fog Grid > Fog Grid > Open At Start` points at it)
- Created at runtime, as children of Fog Grid: `Fog Visual` (one SpriteRenderer with the fog texture) and `Fog Colliders` (one BoxCollider2D per cell, all on one object)

## How it works

In `Start`, once:

1. Work out the fog's bottom-left corner: Fog Grid position minus half of `Area Size`.
2. Make a texture of `Area Size x Pixels Per Unit` pixels (320 x 192 = 61,440), all full fog colour. Keep a 0..1 fog amount per pixel and a "last cleared" time per pixel.
3. Create `Fog Visual` with that texture as a sprite, at `Sorting Order`.
4. Create `Fog Colliders` with one box collider per `Cell Size` square (80 x 48 = 3,840), all on.
5. For each object in `Open At Start`, clear a circle at its position. Radius is half its X scale. (Clearing: position -1.621, 0.01; X scale 4, so radius 2.)
6. Snapshot the result as the "rest" state: the fog amounts, and which colliders are on. Regrowth never goes past this.

Every frame, in `Update`, in this order:

1. If `Crow` is set, clear a circle of `Clear Radius` at the crow's position.
2. Regrow.
3. If any pixel changed, copy fog amounts into the texture's alpha and upload it.

Clearing a circle:
- Pixels: fog amount drops to 0 inside the circle and rises back to full over `Edge Softness` at the rim. It only ever lowers the amount. Every pixel inside the circle (including the soft rim) gets its "last cleared" time set to now.
- Colliders: every cell whose centre is inside the circle is switched off. Cells are not partly cleared, so the blocking edge is a little rougher than the visible edge.

Regrowing:
- Pixels: a pixel grows by `Regrow Speed x deltaTime` toward its rest amount when it was last cleared more than `Regrow Delay` seconds ago and it is further than `Cow Safe Radius` from the cow. (At 0.5 per second, a fully clear pixel takes 2 s to refog.)
- Colliders: a cell comes back on only if all of these hold. It was on at rest. Its centre is further than `Cow Safe Radius + Cell Size / 2` from the cow. Its centre is not inside the cow collider's bounds, grown by `Cell Size`. The pixel under its centre is back to its rest amount.

Blocking works because the cow has a Rigidbody2D and BoxCollider2D, and the fog cells are solid colliders. The crow has no collider, so it flies over fog colliders freely.

## Tuning

All on `Fog Grid > Fog Grid`.

| What | Object | Field | Current value |
|---|---|---|---|
| Who clears fog | Fog Grid | Crow | Crow |
| Who fog won't grow under | Fog Grid | Cow | Cow |
| Start clearings | Fog Grid | Open At Start | Clearing (1 entry) |
| Fog area in units | Fog Grid | Area Size | 24 x 12 (covers the camera view up to about 2.4:1 at camera size 5) |
| Crow's hole radius | Fog Grid | Clear Radius | 1 (script default 0.75) |
| Soft edge width | Fog Grid | Edge Softness | 0.2 |
| Fog look resolution | Fog Grid | Pixels Per Unit | 16 |
| Blocking resolution | Fog Grid | Cell Size | 0.25 |
| Fog colour | Fog Grid | Fog Color | dusky plum-grey r 0.42, g 0.38, b 0.44, a 1 (≈ #6B6170) |
| Draw order | Fog Grid | Sorting Order | 10 |
| Seconds before fog grows back | Fog Grid | Regrow Delay | 10 |
| Regrow rate (fraction/s) | Fog Grid | Regrow Speed | 0.25 (full fog 4 s after regrowth starts, so about 14 s in all) |
| No-regrow radius around cow | Fog Grid | Cow Safe Radius | 2 (script default 0.75) |
| How clear counts as revealed (snakes wake) | Fog Grid | Reveal Threshold | 0.5 (default) |
| Fog area centre | Fog Grid | Transform > Position | 0, 0, 0 (camera centre) |
| Start clearing centre and size | Clearing | Transform > Position / Scale | -1.621, 0.01 / X scale 4 (radius 2) |
| Camera view | Main Camera | Camera > Size | 5 (orthographic) |

Fog opacity is the alpha of `Fog Color` (currently 1, fully opaque).

## Gotchas

- Performance ceiling (ponytail note in the script): every frame loops about 61k pixels and about 3.8k cells in `Regrow`, and the whole texture is re-uploaded whenever anything changed. Fine for one screen on desktop. It may be slow in WebGL (single thread). If it is, the planned fix is to move regrowth to a shader or render texture. Test a WebGL build early.
- `Open At Start` clearings are the rest state: they never refog. Anything cleared later does.
- The fog is cleared wherever the crow is, even when the crow is sitting still. There is no "landed" state yet (see `players.md`).
- Fog does not regrow within `Cow Safe Radius` of the cow. With the current 2, that's a wide bubble; the cow's BoxCollider2D is 2.32 x 2.46 units, so the radius is close to the collider's size.
- Fog colliders and the fog sprite are built in `Start`. Changing `Area Size`, `Pixels Per Unit` or `Cell Size` in Play mode does nothing until the next Play.
- `Fog Colliders` copies the Fog Grid's layer. If layers are added later, check that the cow collides with it.
- The `Cow` and `Crow` fields are scene references. If either is empty, the matching behaviour is skipped without an error (no clearing, or no cow protection).
- `IsClear(position)` is the one public call: true where fog is below `Reveal Threshold` or outside the fog area, false before `Start`. Snakes use it to wake (see `snakes.md`).
- The fields are `[SerializeField] private` now (same names, so scene values kept). Other scripts can't change them at runtime.
- The start-clearing Gizmo (cyan circle) shows in the Scene view. Selecting Fog Grid shows the fog area as a white box.

## Not built yet

- Crow energy is not tied to fog. The crow clears fog for free.
- Crow clears fog even when it is "landed" (standing still). Design says a landed crow should be in danger, not a free light.
- Snakes are not revealed by clearing, because snakes don't exist.
- Nicer natural-looking fog (design "later / maybe"). Option: noise texture or shader, which would also fix the WebGL cost.
- Fog regrowth is not in design.md. Decide whether to keep it. It currently makes the crow keep working to hold a path open.
