# Grass

## What it is

Small dark grass tufts scattered on the ground that sway in the same wind that drifts the fog. A gust wave rolls across the field, so neighbouring tufts lean one after another instead of all together, and each tuft has a little jitter of its own. The grass is drawn under the fog, so the fog tints it like misty silhouettes. With the wind at (0, 0) the grass still sways gently, it just stops leaning one way.

## Where it lives

- Sprites: `grass_tuft_0` to `grass_tuft_3`, sliced from `Assets/Art/Ground/cow and crow tileset.png` (Sprite Editor, pivot Bottom Center, about 1 x 0.6 units at PPU 100). The existing slices were not touched.
- Shader: `Assets/Shaders/GrassSway.shader` ("Cowncrow/GrassSway"). Lit like the other sprites (URP 2D lights), so the crow's glow and the global light affect it.
- Material: `Assets/Materials/M_GrassSway.mat`
- Prefab: `Assets/Prefabs/PF_GrassTuft.prefab` (one SpriteRenderer, no script). Sorting Order -40: above ground (-100 to -80) and shadows (-50), below trunks (5), the animals and the fog (19).
- Scene: `Grass` in `Level1`, 20 tufts in 4 clumps (two in the start clearing, one in clearing 2, one in the fog corridor between). Mixed sprites, random Flip X, scale 0.6 to 0.85. Hand placed, so move them freely.
- Scene: `Grass` in `Level2`, 110 tufts: the original 15 plus 95 (`GrassTuft16` to `GrassTuft110`) spread over the whole walkable ground. Placed by a one-off bridge script: ground reachable from the cow's start (flood fill, 0.5 grid, a tuft's 0.8 radius clear of trunks and water), not under a canopy (ellipse grown 10%), not within 1.2 of a snake, one clump (1 to 3 tufts, mostly 1) per 4 x 4 unit cell with a random spot, at least 2 between clump centres, 0.45 to 0.9 within a clump. Mixed sprites, random Flip X, scale 0.6 to 0.85. Free to move by hand; delete the extras to thin it.
- Wind: `Fog Grid > Fog Grid > Wind` (`FogGrid.cs`, global `_FogWind`).

## How it works

The tileset's tufts are painted on a flat opaque ground colour (155, 175, 94), not on transparency. The fragment shader keys that colour out: a pixel's opacity is its colour distance from `Ground Colour`, divided by `Key Range`. What is left is the tuft's shape, drawn in `Tuft Colour` (a dark olive, `#6F7D35`). So the sprite rects can be square and tinting is one colour field.

The vertex shader bends the tuft:

1. Wind direction = `_FogWind.xy` normalised (+x if the wind is zero). Strength = its length.
2. Weight = (height above the sprite's bottom / `Tuft Height`), squared, so the base stays planted and the tip moves most.
3. Gust = `sin(dot(tuft position, wind direction) * Wave Scale - time * Wave Speed)`. It uses the tuft's own position (its base), so the whole tuft shares one value and the wave travels along the wind. Flutter = a sine with a phase taken from the tuft position, so neighbours are not in step.
4. Push (world units along the wind) = `(strength * Wind Lean + Sway Amount * (gust + Flutter * flutter)) * weight`.

`DisableBatching` is on because the shader reads the object's position.

## Tuning

| What | Where | Default |
|---|---|---|
| Wind direction and strength (fog and grass) | `Fog Grid > Fog Grid > Wind` | (0.1, 0.02) |
| Tuft colour (alpha = overall opacity) | `M_GrassSway > Tuft colour` | `#6F7D35`, alpha 1. Try `#2E1A2B` for darker |
| Ground colour keyed out | `M_GrassSway > Ground colour` | (155, 175, 94). Change only if the tileset's ground colour changes |
| Key range | `M_GrassSway > Key range` | 0.1. Lower = harder, thinner strokes; higher = softer, fatter |
| Tuft height | `M_GrassSway > Tuft height` | 0.6 (the sprite's height in units; where the bend reaches full) |
| Sway amount (tip movement, units) | `M_GrassSway > Sway amount` | 0.08 |
| Wave scale (higher = shorter waves) | `M_GrassSway > Wave scale` | 0.7 (a gust about 9 units long) |
| Wave speed (radians per second) | `M_GrassSway > Wave speed` | 1.2 (a gust every 5 s) |
| Flutter (per-tuft jitter, fraction of sway) | `M_GrassSway > Flutter` | 0.25 |
| Flutter speed | `M_GrassSway > Flutter speed` | 2.5 |
| Steady lean with the wind | `M_GrassSway > Wind lean` | 0.5 (about 0.05 units at default wind) |
| Draw order | `PF_GrassTuft > Sprite Renderer > Order in Layer` | -40 |
| Tuft size | `PF_GrassTuft > Transform > Scale` | 0.7 |

Material changes apply live, also in Play, and to every tuft.

The Settings Motion slider scales the sway (global `_ReduceMotion`, see menu.md); 0 holds the grass still.

`TreeSway.shader` (see `trees.md`, "Sway") reads the same global `_FogWind`, with much smaller amplitudes.

The same `Wind` also moves the fog (`M_FogSwirl` drift = its own drift plus the wind). `M_FogSwirl`'s own drift values were lowered by the default wind, so fog looks as before; raising the wind speeds up fog too.

## Add more tufts

Drag `Assets/Prefabs/PF_GrassTuft.prefab` into `Grass` in the Hierarchy (keeps it tidy), move it, and optionally swap `Sprite Renderer > Sprite` for another `grass_tuft_N`, tick `Flip X`, or scale it a little. Keep the pivot on the ground: the pivot is the planted base. Don't use `Sprite Renderer > Material` other than `M_GrassSway`.

More tuft shapes: `Window > 2D > Sprite Editor` on the tileset, add a rect around a clean tuft with Pivot Bottom Center (the rect only needs to hold the tuft and flat ground, nothing else such as flowers or pebbles).

## Gotchas

- Sway only runs in Play mode, because `FogGrid` sets `_FogWind` in `Update`. Without `FogGrid` running (edit mode) `_FogWind` is zero, so tufts get no wind lean; they still wave.
- Sprites placed over a different ground colour (pond, dark fill) would not key cleanly: the key is made for the meadow green only.
- Only the 2D Lit pass exists. The shader has no normal-map or unlit pass.
- The sway was not seen moving in a headless screenshot run (the editor did not tick time). Check the feel by eye.
- WebGL: not tested yet. The vertex work is trivial and the keying is a few ALU ops, so it should be fine.
