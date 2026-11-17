# Fog

## What it is

A cloudy plum-grey fog covers the whole level (Level1: 45.6 x 14.4 units). The crow cuts a soft-edged round hole in it wherever it flies. The cow cannot walk into fog, so it can only go where the crow has cleared. A hole stays open for 5 seconds after the crow leaves, then the fog fades back in. A start clearing and the ground around the cow never fog back in.

## Where it lives

- Script: `Assets/Scripts/FogGrid.cs`
- Cloud texture: `Assets/Art/Fog/fog.png`. A white cloud puff with the shape stored in alpha (transparent = no cloud). The older grey-on-black `T_FogCloud.png` and `T_FogPuff.png` also still work: the script reads brightness x alpha, and their alpha is 1.
- Scene object: `Fog Grid` (has the `FogGrid` component)
- Scene object: `Clearing` (empty marker for the start clearing; `Fog Grid > Fog Grid > Open At Start` points at it)
- Created at runtime, as children of Fog Grid: `Fog Visual` (one SpriteRenderer with the fog texture) and `Fog Colliders` (one BoxCollider2D per cell, all on one object)

## How it works

In `Start`, once:

1. Work out the fog's bottom-left corner: Fog Grid position minus half of `Area Size`.
2. Build the cloud look: stamp `Puff Count` random copies of the `Puff Textures` (random image, position, size within `Puff Size Range`, rotation; layout fixed by `Seed`) into a 0..1 brightness map. Where stamps overlap, the brightest wins. A pixel's strength is its brightness times its alpha, so white-on-transparent and grey-on-black images both work. Each image is scaled by its own strongest pixel, so a dim image still reaches full brightness.
3. Make a texture of `Area Size x Pixels Per Unit` pixels (about 730 x 230 at Level1's 45.6 x 14.4 and 16). Each pixel's colour is `Fog Color` darkened by the map: thinnest spots at `Thin Shade` x the colour, thickest at the full colour. Alpha stays `Fog Color`'s alpha, so the fog is as opaque as before. Keep a 0..1 fog amount per pixel and a "last cleared" time per pixel.
4. Create `Fog Visual` with that texture as a sprite, at `Sorting Order`.
5. Create `Fog Colliders` with one box collider per `Cell Size` square (about 182 x 58 = 10.5k), all on.
6. For each object in `Open At Start`, clear a circle at its position. Radius is half its X scale. (Level1: `Clearing` at (-4.8, 0.01), X scale 8.4, radius 4.2; `Clearing2` at (21.6, -0.12), X scale 9.6, radius 4.8.)
7. Snapshot the result as the "rest" state: the fog amounts, and which colliders are on. Regrowth never goes past this.

Every frame, in `Update`, in this order:

1. If `Crow` is set, clear a circle of `Clear Radius` at the crow's position.
2. Regrow.
3. If any pixel changed, copy fog amounts into the texture's alpha and upload it.

Clearing a hole:
- Shape: if `Clear Shape` is set (planned: `fog.png`), the hole is that cloud image stamped `2 x radius` wide, centred on the crow, unrotated: fog drops by the image's brightness x alpha (scaled so its densest spot clears fully). That amount is multiplied by `Clear Falloff`, a curve from the hole's centre (0) to its radius (1): 1 = remove all, 0 = remove none. The default keeps the inner half fully clear, then steps down (0.6 at 0.7, 0.3 at 0.85, 0 at the edge), so outer rings leave thin fog behind. It must reach 0 at the edge, because the cloud image touches its own borders and would otherwise leave straight edges. The distance fed to the curve is pushed in and out by the same `Edge Noise` wobble, so the rings are ragged rather than a smooth brush stroke. `Edge Softness` is not used for this hole. The trail is many overlapping stamps. The start clearings use the same shape. If `Clear Shape` is empty, the hole is the round one below.
- Round hole (no `Clear Shape`): fog amount drops to 0 inside the circle and rises back to full over `Edge Softness` at the rim. The rim is pushed in or out by up to `Edge Noise` units using Perlin noise (`Edge Noise Scale` wobbles per unit), fixed in world space, so holes and the crow's trail have ragged, cloudy edges instead of round ones. It only ever lowers the amount. Every pixel inside the circle (including the soft rim) gets its "last cleared" time set to now.
- Colliders: every cell in range whose centre now looks clear (fog below `Reveal Threshold`, same test as `IsClear`) is switched off, so blocking follows the ragged edge. Cells are not partly cleared, so the blocking edge is a little rougher than the visible edge.

Regrowing:
- Pixels: a pixel grows by `Regrow Speed x deltaTime` toward its rest amount when it was last cleared more than `Regrow Delay` seconds ago and it is outside the cow's safe zone: `Cow Safe Radius` around the cow, with its edge wobbled by the same noise as the holes (`Edge Noise`, `Edge Noise Scale`), so the safe bubble is ragged too. (At 0.5 per second, a fully clear pixel takes 2 s to refog.)
- Colliders: a cell comes back on only if all of these hold. It was on at rest. Its centre is not inside the cow collider's bounds, grown by `Cell Size`. The pixel under its centre is back to its rest amount.

Blocking works because the cow has a Rigidbody2D and a BoxCollider2D on its feet only (top-down: the body may overlap fog, the feet may not), and the fog cells are solid colliders. The crow has only its trigger collider, so it flies over fog colliders freely.

## Tuning

All on `Fog Grid > Fog Grid`.

| What | Object | Field | Current value |
|---|---|---|---|
| Who clears fog | Fog Grid | Crow | Crow |
| Who fog won't grow under | Fog Grid | Cow | Cow |
| Start clearings | Fog Grid | Open At Start | Clearing, Clearing2 (2 entries) |
| Fog area in units | Fog Grid | Area Size | 45.6 x 14.4 in Level1 (covers both screens, camera size 6, with margin) |
| Crow's hole radius | Fog Grid | Clear Radius | 2 in Level1 (script default 0.75). With fog.png and the default `Clear Falloff`, the walkable part is about 1.34 x this, so 2 gives about 2.7 units walkable. The cow's feet collider is 1.6 wide and 0.8 tall, so there is plenty of room. Shrink the curve or the shape a lot and the cow stops fitting. |
| Crow's hole shape | Fog Grid | Clear Shape | fog.png (empty = round) |
| How much each ring clears (cloud hole) | Fog Grid | Clear Falloff | not set up yet (default curve: 1 to 0.5, 0.6 at 0.7, 0.3 at 0.85, 0 at 1) |
| Soft edge width (round hole only) | Fog Grid | Edge Softness | 0.2 |
| Hole edge wobble in units | Fog Grid | Edge Noise | 0.4 (0 = round) |
| Wobbles per unit | Fog Grid | Edge Noise Scale | 2 (higher = wispier) |
| Fog look resolution | Fog Grid | Pixels Per Unit | 16 |
| Blocking resolution | Fog Grid | Cell Size | 0.25 |
| Fog colour | Fog Grid | Fog Color | dusky plum-grey r 0.42, g 0.38, b 0.44, a 1 (≈ #6B6170) |
| Cloud images | Fog Grid | Puff Textures | fog.png (empty = flat colour) |
| Number of puffs | Fog Grid | Puff Count | 94 in Level1 (keeps density over the bigger area) |
| Puff size in units | Fog Grid | Puff Size Range | 3 to 6 |
| Thinnest fog brightness | Fog Grid | Thin Shade | 0.7 (lower = stronger cloud shapes) |
| Puff layout | Fog Grid | Seed | 0 |
| Draw order | Fog Grid | Sorting Order | 10 |
| Seconds before fog grows back | Fog Grid | Regrow Delay | 10 |
| Regrow rate (fraction/s) | Fog Grid | Regrow Speed | 0.25 (full fog 4 s after regrowth starts, so about 14 s in all) |
| No-regrow radius around cow | Fog Grid | Cow Safe Radius | 2 (script default 0.75) |
| How clear counts as revealed (snakes wake) | Fog Grid | Reveal Threshold | 0.5 (default) |
| Fog area centre | Fog Grid | Transform > Position | (10.68, 0) in Level1 |
| Start clearing centre and size | Clearing | Transform > Position / Scale | (-4.8, 0.01) / X scale 8.4 (radius 4.2). `Clearing2`: (21.6, -0.12) / X scale 9.6 |
| Camera view | Main Camera | Camera > Size | 6 (orthographic) |

Fog opacity is the alpha of `Fog Color` (currently 1, fully opaque).

## Gotchas

- The cow can only walk where `Clear Falloff` removes more than half the fog (`Reveal Threshold` 0.5). Thin-fog outer rings look open but still block the cow.

- `Clear Shape` needs Read/Write ticked too, or the script logs `Fog Grid: clear shape X needs Read/Write ticked` and falls back to the round hole.
- The cloud fills less of its square than a circle does, so with `Clear Shape` set the hole looks smaller at the same `Clear Radius`. Raise it to compensate.

- Puff textures (including `fog.png`) need **Read/Write** ticked (`Inspector > Advanced > Read/Write`), or the script logs `Fog Grid: puff texture X needs Read/Write ticked` and stops stamping.
- The cloud look is built once in `Start`. Changing the puff fields in Play mode does nothing until the next Play. Building it costs a moment at start, nothing per frame.
- Detail is limited by `Pixels Per Unit` (16), so the cloud edges look soft. Raising it shows more grain but costs 4x per frame at 32 (WebGL risk).
- Performance ceiling (ponytail note in the script): every frame loops about 168k pixels and about 10.5k cells in Level1 in `Regrow`, and the whole texture is re-uploaded whenever anything changed. Fine for one screen on desktop. It may be slow in WebGL (single thread). If it is, the planned fix is to move regrowth to a shader or render texture. Level1's 45.6 x 14.4 area was 117k pixels and 7.3k cells before the map grew 1.2x, so the cost keeps rising: a WebGL test is needed.
- `Open At Start` clearings are the rest state: they never refog. Anything cleared later does.
- The fog is cleared wherever the crow is, even when the crow is sitting still. There is no "landed" state yet (see `players.md`).
- Fog does not regrow within `Cow Safe Radius` (± `Edge Noise`) of the cow. With the current 2, that's a wide bubble; the cow's feet collider is 1.6 x 0.8 units, so the bubble is much bigger than the collider.
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
- Fog doesn't drift or animate. Puffs are static. Moving fog would need a shader.
