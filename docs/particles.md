# Ambient particles

## What it is

Small bits of life in both levels. Now and then a butterfly flutters across the screen, leaving a faint trail of glowing specks. A ladybug sometimes appears on open ground, crawls a little way and fades out. The crow trails a few small gold specks while it flies, and none while it waddles tired or stands still.

## Where it lives

- Art: `Assets/Art/Particles/particles.png`, white shapes sliced into 7 sprites. White, so each particle system tints them with its Start Color. `particles_0` and `particles_2` are the butterfly's two wing poses, `particles_1`, `_3`, `_4` and `_6` are leaves (used by `PF_LeafBurst`), and `particles_5` is the ladybug.
- Prefabs in `Assets/Prefabs`, one Particle System each, with no scripts:
  - `PF_Butterflies`, which has a child `Trail` (the specks)
  - `PF_Ladybug`
  - `PF_LeafBurst`, one-shot with a child `Back`, played by `FruitTree` (`Play` also plays the child). See `trees.md`.
  - `PF_FlySpecks`
- Scene objects, the same in Level1 and Level2:
  - `Main Camera > Butterflies` and `Main Camera > Ladybug`, at local position (0, 0, 10)
  - `Crow > FlySpecks`
- Code: only `PlayerControl > Fly Particles` (on the Crow, set to `FlySpecks`). It switches that system's emission on while the crow flies and off otherwise.

## How it works

- Every system simulates in World space, so particles stay where they spawned while the camera or the animal moves on. Scaling Mode is Local, so the crow's 0.5 scale doesn't shrink its specks: sizes are world units.
- Butterflies and Ladybug ride on the camera, so they always spawn near the current view.
  - They spawn from a circle (`Shape > Radius` 8) and fade in and out (Color over Lifetime).
  - Max Particles is 1 for each, so there is never more than one butterfly and one ladybug at a time.
- Butterfly:
  - Noise makes it wander.
  - Texture Sheet Animation (Sprites mode, Cycles 60 over its lifetime) flips between the two wing sprites, which makes the flap.
  - `Trail` is a Birth sub-emitter (`Sub Emitters` module): it follows each butterfly and drops specks in `M_MoteGlow`.
- Ladybug:
  - It crawls right (Velocity over Lifetime, x 0.25 to 0.35, y ±0.08), and Start Rotation -90 turns the sprite to face that way.
  - Shape's Align to Direction doesn't work for flat 2D particles (it tilts them in 3D), so every ladybug heads roughly right.
  - It sits under the fog (order -30), so it only shows on cleared ground.
- FlySpecks: emits by distance, in `M_MoteGlow`. `PlayerControl` turns emission on only while flying.

## Tuning

All on the prefabs, in `Assets/Prefabs`. Change the prefab, not the scene copy, so both levels stay the same.

| What | Prefab | Field | Value |
|---|---|---|---|
| How often a butterfly comes | PF_Butterflies | Emission > Rate over Time | 0.06 per s (about one every 16 s) |
| Butterfly life / speed / size | PF_Butterflies | Main > Start Lifetime / Start Speed / Start Size | 9 to 13 s / 0.5 to 0.9 / 0.45 to 0.55 |
| Butterfly colours | PF_Butterflies | Main > Start Color | random between gold #E8B23A and pink #E6A6C7 |
| Butterfly wander | PF_Butterflies | Noise > Strength / Frequency | 0.8 / 0.4 |
| Flap speed | PF_Butterflies | Texture Sheet Animation > Cycles | 60 per lifetime (about 5 flaps a second) |
| Butterfly draw order | PF_Butterflies and Trail | Renderer > Order in Layer | 18 (over canopies at 17, under the fog at 19, so fog hides them) |
| Trail specks per second / life / size | PF_Butterflies > Trail | Emission > Rate over Time / Main > Start Lifetime / Start Size | 5 / 0.6 to 1 s / 0.08 to 0.15 |
| How often a ladybug comes | PF_Ladybug | Emission > Rate over Time | 0.035 per s (about one every 30 s) |
| Ladybug life / size / colour | PF_Ladybug | Main > Start Lifetime / Start Size / Start Color | 5 to 7 s / 0.3 / #C9573F |
| Ladybug crawl | PF_Ladybug | Velocity over Lifetime > X / Y | 0.25 to 0.35 / -0.08 to 0.08 |
| Ladybug facing | PF_Ladybug | Main > Start Rotation | -90 (head to the right) |
| Leaves per ram | PF_LeafBurst and its child Back | Emission > Bursts | 2 to 3 each (4 to 6 total) |
| Where leaves start | PF_LeafBurst, Back | Shape > Box > Scale | 4.4 x 2.8 (inside the 5.9 x 4.1 canopy) |
| Leaf life / size / colour | PF_LeafBurst, Back | Main > Start Lifetime / Start Size / Start Color | 1.8 to 2.6 s / 0.3 to 0.42 / #586628 (a shade darker than the canopy's #6B7A30) |
| Leaf fall / flutter / spin | PF_LeafBurst, Back | Main > Gravity Modifier (Start Speed 0) / Noise > Strength / Rotation over Lifetime | 0.22 / 0.6 / -2 to 2 rad per s |
| Front / back of the canopy | PF_LeafBurst / Back | Renderer > Order in Layer | 18 (over the canopy) / 16 (behind it, over the trunk) |
| Ladybug draw order | PF_Ladybug | Renderer > Order in Layer | -30 (above grass, below everything else) |
| Crow specks per unit flown | PF_FlySpecks | Emission > Rate over Distance | 2 |
| Crow speck life / size | PF_FlySpecks | Main > Start Lifetime / Start Size | 0.5 to 0.8 s / 0.06 to 0.12 |
| Crow speck draw order | PF_FlySpecks | Renderer > Order in Layer | 15 (behind the crow at 16) |

## Gotchas

- Play On Awake is shared by a particle system and all its child systems: switching it off on `Trail` also switched off the butterflies themselves (fixed 27 Nov 2026). Keep it on for `PF_Butterflies`.
- The editor pauses particle simulation while it isn't focused. To preview a system in edit mode, select it: the Scene view's Particle Effect panel plays it.
- Texture Sheet Animation in Sprites mode needs all its sprites from one texture, which `particles.png` is.
- If the artist re-exports `particles.png`, keep the slice names (`particles_0` to `_6`), or the prefabs lose their sprites.
- The sheet is white shapes only: the ladybug is a flat red silhouette with no spots. Detail would need new art.
