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
3. Make a texture of `Area Size x Pixels Per Unit` pixels (about 730 x 230 at Level1's 45.6 x 14.4 and 16). Each pixel's colour is `Fog Color` darkened by the map: thinnest spots at `Thin Shade` x the colour, thickest at the full colour. Alpha is `Fog Color`'s alpha x `Lerp(Thin Alpha, 1, map)`: at `Thin Alpha` 1 the fog is evenly thick; lower makes the gaps between puffs see-through (looks only, the cow is still blocked there). Before that, the map is box-blurred by `Look Blur` units (rows then columns, twice; once in Start), so puff edges fade instead of stepping. Each pixel also gets its own regrow speed multiplier: Perlin noise (own offset, same `Regrow Noise Scale`) mapped to 1 +- `Regrow Speed Jitter` / 2, so patches refill at different rates. Each pixel also gets its own regrow wait: `Regrow Delay` + Perlin noise x `Regrow Jitter` (patch size `Regrow Noise Scale`). Keep a 0..1 fog amount per pixel and a "last cleared" time per pixel.
4. Create `Fog Visual` with that texture as a sprite, at `Sorting Order`.
5. Create `Fog Colliders` with one box collider per `Cell Size` square (about 182 x 58 = 10.5k), all on.
6. For each object in `Open At Start`, clear a circle at its position. Radius is half its X scale. (Level1: `Clearing` at (4.17, 0.14), X scale 8.4; `Clearing2` is the fruit tree's clearing, now at (21.63, 0.38), X scale 14. Level2: `Clearing` plus four tree clearings under `TreeClearings`, X scale 14 each.)
7. Snapshot the result as the "rest" state: the fog amounts, and which colliders are on. Regrowth never goes past this.

Every frame, in `Update`, in this order:

1. If `Crow` is set, clear a circle of `Clear Radius` at the crow's position.
2. Regrow.
3. If any pixel changed, copy fog amounts into the texture's alpha and upload it.

Clearing a hole:
- Shape: if `Clear Shape` is set (planned: `fog.png`), the hole is that cloud image stamped `2 x radius` wide, centred on the crow, turned by an angle taken from Perlin noise at the crow's position (so repeated stamps along a path don't line up one flat side into a straight edge): fog drops by the image's brightness x alpha (scaled so its densest spot clears fully). That amount is multiplied by `Clear Falloff`, a curve from the hole's centre (0) to its radius (1): 1 = remove all, 0 = remove none. The default keeps the inner half fully clear, then steps down (0.6 at 0.7, 0.3 at 0.85, 0 at the edge), so outer rings leave thin fog behind. It must reach 0 at the edge, because the cloud image touches its own borders and would otherwise leave straight edges. The distance fed to the curve is pushed in and out by the same `Edge Noise` wobble, so the rings are ragged rather than a smooth brush stroke. `Edge Softness` is not used for this hole. The trail is many overlapping stamps. The start clearings use the same shape. If `Clear Shape` is empty, the hole is the round one below.
- Round hole (no `Clear Shape`): fog amount drops to 0 inside the circle and rises back to full over `Edge Softness` at the rim. The rim is pushed in or out by up to `Edge Noise` units using Perlin noise (`Edge Noise Scale` wobbles per unit), fixed in world space, so holes and the crow's trail have ragged, cloudy edges instead of round ones. It only ever lowers the amount. Every pixel inside the circle (including the soft rim) gets its "last cleared" time set to now.
- Colliders: every cell in range whose centre now looks clear (fog below `Reveal Threshold`, same test as `IsClear`) is switched off, so blocking follows the ragged edge. Cells are not partly cleared, so the blocking edge is a little rougher than the visible edge.

Regrowing:
- Fast zones: `Fog Grid > Fast Regrow Zones` lists marker objects; each marker's position is a box centre and its Transform X / Y scale is the box width / height (world units; rotation ignored). In Start every pixel whose centre is inside any box is flagged once (a bool per pixel). Flagged pixels use `Fast Regrow Delay` instead of `Regrow Delay` (the patchy `Regrow Jitter` is still added) and `Fast Regrow Speed` instead of `Regrow Speed` (the per-patch speed multiplier still applies). Everything else about regrowth (cow safe zone, rest state) is unchanged. Zones are drawn as yellow wire boxes when the Fog Grid is selected. Zones are not moved at runtime.
- Pixels: a pixel grows by `Regrow Speed x deltaTime` toward its rest amount when it was last cleared more than its own wait ago (`Regrow Delay` plus up to `Regrow Jitter`, varying in soft patches, so a trail creeps back in blotches instead of reappearing as one drawn shape) and it is outside the cow's safe zone: `Cow Safe Radius` around the cow, with its edge wobbled by the same noise as the holes (`Edge Noise`, `Edge Noise Scale`), so the safe bubble is ragged too. Past that edge the regrow target ramps smoothly from 0 to the rest amount over `Cow Safe Fade` (smoothstep), so the bubble fades out instead of ending in a hard step (measured neighbour-pixel jump near the cow: 0.64 at fade 0, 0.28 at fade 1). Cells inside that ring stay unblocked. (At 0.5 per second, a fully clear pixel takes 2 s to refog.)
- Colliders: a cell comes back on only if all of these hold. It was on at rest. Its centre is not inside the cow collider's bounds, grown by `Cell Size`. The pixel under its centre is back to its rest amount.

Blocking works because the cow has a Rigidbody2D and a BoxCollider2D on its feet only (top-down: the body may overlap fog, the feet may not), and the fog cells are solid colliders. The crow has only its trigger collider, so it flies over fog colliders freely.

## Tuning

All on `Fog Grid > Fog Grid`.

| What | Object | Field | Current value |
|---|---|---|---|
| Who clears fog | Fog Grid | Crow | Crow |
| Who fog won't grow under | Fog Grid | Cow | Cow |
| Start and tree clearings | Fog Grid | Open At Start | Level1: Clearing, Clearing2. Level2: Clearing, TreeClearings > Clearing_TreeFork, _TreeFruitA, _TreeFruitB, _TreeMeeting |
| Fog area in units | Fog Grid | Area Size | 45.6 x 14.4 in Level1 (covers both screens, camera size 6, with margin) |
| Crow's hole radius | Fog Grid | Clear Radius | 2 in Level1 (script default 0.75). With fog.png and the default `Clear Falloff`, the walkable part is about 1.34 x this, so 2 gives about 2.7 units walkable. The cow's feet collider is 1.6 wide and 0.8 tall, so there is plenty of room. Shrink the curve or the shape a lot and the cow stops fitting. |
| Crow's hole shape | Fog Grid | Clear Shape | fog.png (empty = round) |
| How much each ring clears (cloud hole) | Fog Grid | Clear Falloff | not set up yet (default curve: 1 to 0.5, 0.6 at 0.7, 0.3 at 0.85, 0 at 1) |
| Soft edge width (round hole only) | Fog Grid | Edge Softness | 0.2 |
| Hole edge wobble in units | Fog Grid | Edge Noise | 0.4 (0 = round) |
| Wobbles per unit | Fog Grid | Edge Noise Scale | 2 (higher = wispier) |
| Fog look resolution | Fog Grid | Pixels Per Unit | 16 |
| Blocking resolution | Fog Grid | Cell Size | 0.25 |
| Fog colour | Fog Grid | Fog Color | (0.93, 0.95, 0.98, 0.85), a cool white; alpha is the see-through amount (script default white, alpha 1) |
| Cloud images | Fog Grid | Puff Textures | fog.png (empty = flat colour) |
| Number of puffs | Fog Grid | Puff Count | 94 in Level1 (keeps density over the bigger area) |
| Puff size in units | Fog Grid | Puff Size Range | 3 to 6 |
| Thinnest fog brightness | Fog Grid | Thin Shade | 0.85 (lower = stronger cloud shapes) |
| Thinnest fog thickness | Fog Grid | Thin Alpha | 0.7 (1 = even) |
| Thick fog around start clearings (units) | Fog Grid | Thick Border | 2.5 (0 = off). Fog within this distance outside an `Open At Start` circle (centre distance minus half X scale) keeps full alpha (`Fog Color` alpha), blending smoothly back to normal thin fog over the band. Looks only. The shader's breathe still thins it a little |
| Cloud softness (units) | Fog Grid | Look Blur | 0.75 (0 = sharp puff edges) |
| Puff layout | Fog Grid | Seed | 0 |
| Draw order | Fog Grid | Sorting Order | 19 (trunk 5, canopy 17, fruit 18: translucent fog lies over them; moo text 20) |
| Fog shader (drifting wisps) | Fog Grid | Fog Material | `Assets/Materials/M_FogSwirl` (empty = plain sprite look) |
| Wind (fog drift and grass sway) | Fog Grid | Wind | (0.1, 0.02) units per second. The fog shader adds it to both drift layers and `GrassSway` leans and waves along it; see `grass.md` |
| Seconds before fog grows back | Fog Grid | Regrow Delay | 3 |
| Extra patchy regrow wait | Fog Grid | Regrow Jitter | 3 s (0 = whole trail at once) |
| Regrow patch size | Fog Grid | Regrow Noise Scale | 0.8 (lower = bigger blotches) |
| Regrow speed variation by patch | Fog Grid | Regrow Speed Jitter | 0.5 (0 = same speed everywhere; 1 = fastest patches about 3x the slowest) |
| Regrow rate (fraction/s) | Fog Grid | Regrow Speed | 0.75 |
| Fast regrow boxes | Fog Grid | Fast Regrow Zones | empty (markers: position = centre, scale = size) |
| Delay inside a fast zone (s) | Fog Grid | Fast Regrow Delay | 1 (default) |
| Regrow rate inside a fast zone (fraction/s) | Fog Grid | Fast Regrow Speed | 1.5 (default) |
| No-regrow radius around cow | Fog Grid | Cow Safe Radius | 2 (script default 0.75) |
| Soft ring outside the safe radius | Fog Grid | Cow Safe Fade | 1 unit (script default 1; 0 = hard edge). Fog in the ring regrows only partway (0 at the safe radius, full at radius + fade), so the bubble has no hard line |
| How clear counts as revealed (snakes wake) | Fog Grid | Reveal Threshold | 0.5 (default) |
| Cow haze: oval height above feet | Fog Grid | Cow Haze Height | 1.2 (needs `M_FogSwirl`; looks only) |
| Cow haze: oval half size | Fog Grid | Cow Haze Radius | (1.1, 1.3) |
| Cow haze: soft rim | Fog Grid | Cow Haze Softness | 0.6 (fraction of the radius; 0 = hard rim) |
| Cow haze: fog left at centre | Fog Grid | Cow Haze Amount | 0.35 (1 = off, 0 = fully clear) |
| Fog area centre | Fog Grid | Transform > Position | (10.68, 0) in Level1 |
| Start clearing centre and size | Clearing | Transform > Position / Scale | (-4.8, 0.01) / X scale 8.4 (radius 4.2). `Clearing2`: (21.6, -0.12) / X scale 14.4 |
| Camera view | Main Camera | Camera > Size | 6 (orthographic) |

Fog opacity is the alpha of `Fog Color` (currently 1, fully opaque).

## Fog shader

Looks only. `Fog Grid > Fog Material` swaps the fog sprite's material (`sr.sharedMaterial` in `Start`). Empty = the plain sprite. Level1 sets it to `Assets/Materials/M_FogSwirl.mat` (shader `Assets/Shaders/FogSwirl.shader`, "Cowncrow/FogSwirl"). Colliders and `IsClear` never see it: they use the fog amounts in the script.

What it does: two layers of value noise (3 octaves each) scroll in different directions (each layer's own drift plus the shared wind, global `_FogWind` from `Fog Grid > Wind`), sampled in world space, so wisp size doesn't depend on the texture size. The shader thins fog softly: opacity follows the fog amount, and the noise only nudges it in the middle of the ramp (a weight that is 0 at clear and at full fog, 1 halfway). Hole edges and regrowing fog therefore fade gradually and unevenly, like mist thinning, while full fog stays full and clear stays clear. (An earlier threshold version drew a crisp moving contour that read as liquid.) The original alpha still multiplies the result, so `Fog Color` alpha and `Thin Alpha` still set how see-through it is. A third, slower noise darkens the colour a little in patches, so the body of the fog rolls.

Three more looks, all on `M_FogSwirl`:
- Edge wobble (`_Wobble`, 0.01, UV units): the fog texture is sampled through a drifting noise offset, so hole edges bend off the 16 px grid and lose their stair-steps. Higher smears the holes.
- Breathe amount (`_BreatheAmount`, 0.25): settled fog is multiplied by a slow, large-scale noise, so it drifts thicker and thinner in places. 0 = steady.
- Breathe speed (`_BreatheSpeed`, 0.08): how fast that noise drifts.

Cow haze: the cow's collider is its feet only, so at a clearing's top edge its head sits under fog. Each frame `FogGrid.Update` sends the oval (`Cow Haze` fields) to the shader as globals `_CowHazeOval` / `_CowHazeLook`, and the shader multiplies fog alpha by a smoothstep from `Cow Haze Amount` (centre) to 1 (rim). Looks only: colliders, `fog[]` and `IsClear` are untouched. No cow = radius 0 = off. The `Mist` wisps (order 21, alpha 0.12 to 0.25) use a different material and are not thinned.

To tweak: select `M_FogSwirl` in `Assets/Materials` and change its properties. Changes apply live, also in Play, and to every scene using the material.

| Property | Default | What it does |
|---|---|---|
| Wisp size (`_NoiseScale`) | 0.45 | Noise per world unit. Lower = bigger, slower-looking wisps |
| Drift A (`_ScrollA`) | (0.02, 0.01) | Units per second the first layer moves, on top of `Fog Grid > Wind`. With wind (0.1, 0.02) the layer drifts at (0.12, 0.03), as before the wind existed |
| Drift B (`_ScrollB`) | (-0.17, 0.03) | Same for the second layer (total (-0.07, 0.05) with the default wind); it crosses A, so the wisps churn |
| Full fog opacity (`_FullAlpha`) | 0.85 | Set to `Fog Color` alpha. Tells the shader what counts as "full fog" |
| Edge breakup (`_EdgeBreak`) | 0.8 | How patchy the thinning zone is. 0 = smooth gradient, higher = more uneven thin mist (too high starts to look like blobs) |
| Body roll (`_BodyShade`) | 0.12 | How much the colour darkens in slow patches. 0 = flat colour |

Gotcha: `_FullAlpha` is a copy of `Fog Color` alpha, not read from it. Change one, change the other.

## Gotchas

- A hole stamp slid along a path makes a straight edge wherever the stamp has a wide flat shelf (the top of `fog.png` did; the bottom had a narrow lobe, so it looked bumpy). Cause was the identical stamp, not a flipped texture. Fixed by turning each stamp by noise of its position (`turn` in `Clear`).
- Straight vertical/horizontal lines around the start clearings are not fog. `Meadow1` / `Meadow2` use the artist's `SPR_GroundMeadow` (from `Assets/Art/Ground/cow and crow tileset.png`), which has an opaque darker-green square rim (RGB about 140,156,72) outside the scalloped edge, against the lighter `GroundFill` (155,175,94). Fog only makes that square rim easier to see. Fix: `Assets/Sandbox/SPR_GroundMeadow_NoRim.png` (still in Sandbox, Level1 does not use it) is the same sprite with the rim made transparent (verified in Play); set it as `Meadow1/2 > Sprite Renderer > Sprite`, or ask the artist for a rim-free tile.

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

## Test scenes

Three copies of Level1 in `Assets/Sandbox` (not in Build Settings): `FogTest_Silhouette`, `FogTest_CanopyFog` and `FogTest_Mist` compare how fog and trees layer. `FogTest_Mist` won and became Level1 (its look is described below; Level1 now has all of it, plus the `Grass`). `FogTest_Mist` stays in Sandbox as an untouched copy, and still works because its assets moved with their GUIDs. The other two are older experiments; `FogSilhouettes.cs` stays in Sandbox.

- `FogTest_Silhouette`: canopies at 7 and hanging fruit at 8, so fog covers whole trees. `Fog Silhouettes` (script `Assets/Sandbox/FogSilhouettes.cs`) waits for `Fog Visual`, adds a SpriteMask from the fog sprite, and copies every tree sprite (order 0 or more) into a `Silhouette` child at order + 6, shown only inside the mask. Silhouettes use the built-in `GUI/Text Shader`, which draws one flat colour in the sprite's shape, and are fully opaque, so trunk and canopies merge into one tree shape. Fields: `Fog Grid` (Fog Grid), `Tree Roots` (Walls, Tree), `Alpha Cutoff` 0.3 (fog alpha above this counts as fogged), `Silhouette Color` r 0.86 g 0.83 b 0.87 a 1 (keep alpha 1, or overlapping parts stack darker), `Order Offset` 6 (trunk 11, canopy 13, fruit 14: above fog 10, below cow 15). Editor test only: `Shader.Find` can be stripped from builds.
- `FogTest_CanopyFog`: trees as in Level1, plus `Canopy Fog` with `FogPuff1` to `FogPuff24`, one per canopy (top, bottom, left walls and the fruit tree). Sprite `fog.png` slice `fog_0`, white, order 18, no colliders, each 1 to 1.3x its canopy's width with a small random offset and rotation. To tweak: select a puff and change `Transform > Position / Rotation / Scale` or the alpha in `Sprite Renderer > Color` (currently 0.3 to 0.8, mixed).
- `FogTest_Mist` (now also Level1): see-through, warm misty look with a cute feel. `Fog Grid > Fog Material` is `M_FogSwirl` (drifting, wispy fog, see "Fog shader"). `Fog Grid` has `Fog Color` (0.93, 0.95, 0.98, 0.85), a cool white, `Thin Alpha` 0.7 so gaps between puffs are thinner, `Look Blur` 0.75 so puffs melt together with no visible edges (thin fog shows the ground tiles' scalloped border art through it; raise Thin Alpha to hide it), and `Sorting Order` 19, `Clear Radius` 1.8 (about 2.2 to 2.35 units walkable, cow feet 1.6 wide), `Thick Border` 2.5, `Edge Softness` 0.7 (unused while `Clear Shape` is set), `Regrow Delay` 4, `Regrow Speed` 0.6, a long soft `Clear Falloff` ramp (1 at 0, 0.97 at 0.25, 0.65 at 0.6, 0.3 at 0.82, 0 at 1, smooth tangents) and `M_FogSwirl` `_EdgeBreak` 1.1, `_BreatheAmount` 0.15, so the hole fades out instead of ending in a step, so translucent fog lies over canopies (17) and fruit (18) and under the moo text (20). `Mist` holds `MistLayer1` and `MistLayer2`, each with 8 `Puff` children (sprite `fog_0`, white, order 21, 6 to 10 units wide, random rotation, alpha 0.12 to 0.25) and a `Mist Drift` script (`Assets/Scripts/MistDrift.cs`) that slides the layer left and right on a sine (layer 1 speed 0.15 distance 3, layer 2 speed 0.1 distance -4). `Mist` also has `Mist Mask` (`Assets/Scripts/MistMask.cs`): it puts a SpriteMask on `Fog Visual` so wisps only show where fog remains and cleared paths stay clean (fields `Fog Grid`, `Alpha Cutoff` 0.3). `Crow` has a Point `Light 2D` (colour FFE2A8, intensity 0.6, radius 0.3 to 3, falloff 0.7) so the spot it clears glows warm. `Global Light 2D` intensity 1. `Motes` (top level, at (10.75, 0)) is a Particle System of soft gold E8B23A and pink E6A6C7 specks: box 46 x 14, life 4 to 7 s, speed 0.1 to 0.3, size 0.1 to 0.2, 9 per second, 60 max, fade in and out (Color over Lifetime), Noise strength 0.3 frequency 0.5, order 22, material `Assets/Materials/M_MoteGlow.mat` (URP Particles/Unlit, Transparent, Additive, base map `SPR_ShadowBlob`), so each mote adds light: gold and pink in clearings, near-white sparkles over the bright fog. Texture Sheet Animation is off. `Global Volume` uses `Assets/Settings/VP_Mist.asset`: Color Adjustments (post exposure 0.2, contrast 0, saturation 0, colour filter white, so nothing tints the fog warm), Vignette (intensity 0.2, smoothness 0.8, not rounded, cool grey-blue 8A96A8, so the screen edges read as thicker fog), Bloom (threshold 0.9, intensity 0.4). `Main Camera > Camera > Rendering > Post Processing` ticked. To tweak: `Fog Grid > Fog Grid > Fog Color` (alpha = see-through amount; above about 0.8 hides what is under the fog, lower lets players see path and snakes before clearing); `Mist > MistLayerN > Mist Drift` (Speed, Distance); `Crow > Light 2D`; `Motes > Particle System` (Start Size, Start Color, Emission); `Global Volume > Volume > Profile` VP_Mist.

## Not built yet

- Crow energy is not tied to fog. The crow clears fog for free.
- Crow clears fog even when it is "landed" (standing still). Design says a landed crow should be in danger, not a free light.
- Snakes are not revealed by clearing, because snakes don't exist.
- The drifting fog shader (see "Fog shader") and the `Mist` wisps are not tested in a WebGL build yet.

## Tree clearings

Every fruit tree sits in a pre-cleared circle so the cow can walk round the trunk and ram it without the crow clearing first. Each is an empty marker (X scale = diameter) in `Fog Grid > Fog Grid > Open At Start`, centred on the tree's collider centre (the tree's position plus 1.58 up). The cleared hole is smaller than the marker: the falloff curve and edge noise leave about 70 percent clear, so a marker X scale of 14 gives roughly 8 units of clear fog, lopsided by a unit or so. Measured in Play (cow box 1.6 x 0.8 against the enabled fog cells): scale 9 to 12 left the cow unable to get round the trunk; 14 lets it reach the left, right, top and bottom of the trunk in all five trees. Don't shrink below about 13.
