# Fog

## What it is

A cloudy plum-grey fog covers a 20 x 12 unit area. The crow cuts a soft-edged round hole in it wherever it flies. The cow cannot walk into fog, so it can only go where the crow has cleared. A hole stays open for 5 seconds after the crow leaves, then the fog fades back in. A start clearing and the ground around the cow never fog back in.

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
3. Make a texture of `Area Size x Pixels Per Unit` pixels (384 x 192 at the current 24 x 12). Each pixel's colour is `Fog Color` darkened by the map: thinnest spots at `Thin Shade` x the colour, thickest at the full colour. Alpha stays `Fog Color`'s alpha, so the fog is as opaque as before. Keep a 0..1 fog amount per pixel and a "last cleared" time per pixel.
4. Create `Fog Visual` with that texture as a sprite, at `Sorting Order`.
5. Create `Fog Colliders` with one box collider per `Cell Size` square (80 x 48 = 3,840), all on.
6. For each object in `Open At Start`, clear a circle at its position. Radius is half its X scale. (Clearing: position -1.621, 0.01; X scale 4, so radius 2.)
7. Snapshot the result as the "rest" state: the fog amounts, and which colliders are on. Regrowth never goes past this.

Every frame, in `Update`, in this order:

1. If `Crow` is set, clear a circle of `Clear Radius` at the crow's position.
2. Regrow.
3. If any pixel changed, copy fog amounts into the texture's alpha and upload it.

Clearing a circle:
- Pixels: fog amount drops to 0 inside the circle and rises back to full over `Edge Softness` at the rim. The rim is pushed in or out by up to `Edge Noise` units using Perlin noise (`Edge Noise Scale` wobbles per unit), fixed in world space, so holes and the crow's trail have ragged, cloudy edges instead of round ones. It only ever lowers the amount. Every pixel inside the circle (including the soft rim) gets its "last cleared" time set to now.
- Colliders: every cell in range whose centre now looks clear (fog below `Reveal Threshold`, same test as `IsClear`) is switched off, so blocking follows the ragged edge. Cells are not partly cleared, so the blocking edge is a little rougher than the visible edge.

Regrowing:
- Pixels: a pixel grows by `Regrow Speed x deltaTime` toward its rest amount when it was last cleared more than `Regrow Delay` seconds ago and it is outside the cow's safe zone: `Cow Safe Radius` around the cow, with its edge wobbled by the same noise as the holes (`Edge Noise`, `Edge Noise Scale`), so the safe bubble is ragged too. (At 0.5 per second, a fully clear pixel takes 2 s to refog.)
- Colliders: a cell comes back on only if all of these hold. It was on at rest. Its centre is not inside the cow collider's bounds, grown by `Cell Size`. The pixel under its centre is back to its rest amount.

Blocking works because the cow has a Rigidbody2D and BoxCollider2D, and the fog cells are solid colliders. The crow has no collider, so it flies over fog colliders freely.

## Tuning

All on `Fog Grid > Fog Grid`.

| What | Object | Field | Current value |
|---|---|---|---|
| Who clears fog | Fog Grid | Crow | Crow |
| Who fog won't grow under | Fog Grid | Cow | Cow |
| Start clearings | Fog Grid | Open At Start | Clearing (1 entry). Not set up yet (planned: 2 entries, Clearing and Clearing 2) |
| Fog area in units | Fog Grid | Area Size | 24 x 12 (covers the camera view up to about 2.4:1 at camera size 5). LV_01: not set up yet (planned: 38 x 12, to cover both screens) |
| Crow's hole radius | Fog Grid | Clear Radius | 1 (script default 0.75) |
| Soft edge width | Fog Grid | Edge Softness | 0.2 |
| Hole edge wobble in units | Fog Grid | Edge Noise | 0.4 (0 = round) |
| Wobbles per unit | Fog Grid | Edge Noise Scale | 2 (higher = wispier) |
| Fog look resolution | Fog Grid | Pixels Per Unit | 16 |
| Blocking resolution | Fog Grid | Cell Size | 0.25 |
| Fog colour | Fog Grid | Fog Color | dusky plum-grey r 0.42, g 0.38, b 0.44, a 1 (≈ #6B6170) |
| Cloud images | Fog Grid | Puff Textures | T_FogCloud, T_FogPuff (empty = flat colour). Not set up yet (planned: fog.png) |
| Number of puffs | Fog Grid | Puff Count | 40. Not set up yet (planned: 65, keeps density over the bigger area) |
| Puff size in units | Fog Grid | Puff Size Range | 3 to 6 |
| Thinnest fog brightness | Fog Grid | Thin Shade | 0.7 (lower = stronger cloud shapes) |
| Puff layout | Fog Grid | Seed | 0 |
| Draw order | Fog Grid | Sorting Order | 10 |
| Seconds before fog grows back | Fog Grid | Regrow Delay | 10 |
| Regrow rate (fraction/s) | Fog Grid | Regrow Speed | 0.25 (full fog 4 s after regrowth starts, so about 14 s in all) |
| No-regrow radius around cow | Fog Grid | Cow Safe Radius | 2 (script default 0.75) |
| How clear counts as revealed (snakes wake) | Fog Grid | Reveal Threshold | 0.5 (default) |
| Fog area centre | Fog Grid | Transform > Position | 0, 0, 0 (camera centre). LV_01: not set up yet (planned: 8.9, 0) |
| Start clearing centre and size | Clearing | Transform > Position / Scale | -1.621, 0.01 / X scale 4 (radius 2) |
| Camera view | Main Camera | Camera > Size | 5 (orthographic) |

Fog opacity is the alpha of `Fog Color` (currently 1, fully opaque).

## Gotchas

- Puff textures (including `fog.png`) need **Read/Write** ticked (`Inspector > Advanced > Read/Write`), or the script logs `Fog Grid: puff texture X needs Read/Write ticked` and stops stamping.
- The cloud look is built once in `Start`. Changing the puff fields in Play mode does nothing until the next Play. Building it costs a moment at start, nothing per frame.
- Detail is limited by `Pixels Per Unit` (16), so the cloud edges look soft. Raising it shows more grain but costs 4x per frame at 32 (WebGL risk).
- Performance ceiling (ponytail note in the script): every frame loops about 61k pixels and about 3.8k cells in `Regrow`, and the whole texture is re-uploaded whenever anything changed. Fine for one screen on desktop. It may be slow in WebGL (single thread). If it is, the planned fix is to move regrowth to a shader or render texture. The planned LV_01 area of 38 x 12 is about 117k pixels and about 7.3k cells per frame, about 2x as much, so a WebGL test is needed.
- `Open At Start` clearings are the rest state: they never refog. Anything cleared later does.
- The fog is cleared wherever the crow is, even when the crow is sitting still. There is no "landed" state yet (see `players.md`).
- Fog does not regrow within `Cow Safe Radius` (± `Edge Noise`) of the cow. With the current 2, that's a wide bubble; the cow's BoxCollider2D is 2.32 x 2.46 units, so the radius is close to the collider's size.
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
