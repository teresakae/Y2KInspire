# Hand-off: reduced-motion toggle and fog tweaks

To use: open a Claude Code session in this project and paste **"Read HANDOFF.md and do task 1"**. Do one task per session. Delete this file when all three are done.

Rules for the session doing this work:
- Read `CLAUDE.md` first and follow it.
- Never run git commands that change anything.
- The owner asked for these scene and prefab edits, so editing through the UnityMCP bridge is allowed.
- After each task: wait for Unity to compile, check the console, update the doc named in the task, then give the owner test steps and a suggested commit message.

---

## Task 1: replace the Shake and Motion sliders with one "Reduced motion" toggle

**Goal:** the Settings panel has one checkbox, "Reduced motion". When it is on, the screen doesn't shake and the sway and drift stop. It takes effect immediately and in every scene, including a level started straight from the editor.

**Facts (already checked):**
- `Assets/Scripts/SettingsPanel.cs` saves PlayerPrefs `Shake` and `Motion`, each a float with default 1.
- `SetShake` is at lines 51-55. `SetMotion` is at lines 57-62.
- `ApplySaved()` (lines 32-37) sets the volume and `Shader.SetGlobalFloat("_ReduceMotion", 1 - Motion)`.
- Shake is read in one place only: `SharedCamera.cs:59`, as `strength *= PlayerPrefs.GetFloat("Shake", 1f)`.
- `_ReduceMotion` is read in:
  - `MistDrift.cs:22`
  - `Shaders/TitleFloat.shader` (lines 77 and 105)
  - `Shaders/GrassSway.shader:99`
  - `Shaders/TreeSway.shader:96`
- The bug: `ApplySaved()` only runs from `MainMenu.cs:19` and from `SetMotion`. If you start Level1 or Level2 directly, the global stays at 0, so motion stays at full even when it is saved as off. The volume has the same gap.
- Prefab `Assets/Prefabs/PF_SettingsPanel.prefab` has these children:
  - `ControlsText`
  - `FullscreenToggle`, which calls `SetFullscreen`
  - `VolumeLabel` and `VolumeSlider`
  - `ShakeRow`, with a label and a slider
  - `MotionRow`, with a label and a slider
  - `BackButton`
- The panel is used in two places: `MainMenu > Canvas` (inactive) and `PF_HUD > PauseScreen`.

**Steps:**
1. In `SettingsPanel.cs`:
   - Replace the `shakeSlider` and `motionSlider` fields with `[SerializeField] private Toggle reducedMotionToggle;` and give it a tooltip.
   - In `OnEnable`, set the toggle with `SetIsOnWithoutNotify(PlayerPrefs.GetInt("ReducedMotion", 0) == 1)`.
   - Replace `SetShake` and `SetMotion` with one method, `public void SetReducedMotion(bool on)`. It saves the int, calls `PlayerPrefs.Save()` and then calls `ApplySaved()`.
   - In `ApplySaved()`, set `_ReduceMotion` to 1 when the setting is on and 0 when it is off.
   - Update the missing-reference checks.
2. In `SharedCamera.cs:59`, replace the multiply with `if (PlayerPrefs.GetInt("ReducedMotion", 0) == 1) return;`.
3. Fix the gap: call `SettingsPanel.ApplySaved();` in `SharedCamera`'s `Awake` or `Start`. Every level has the shared camera, so volume and motion are applied in every level. Leave the `MainMenu` call as it is.
4. Edit the prefab through the bridge with `PrefabUtility.LoadPrefabContents`:
   - Duplicate `FullscreenToggle` as `ReducedMotionToggle` and set its label to "Reduced motion".
   - Wire its `On Value Changed` to `SettingsPanel.SetReducedMotion` as a dynamic bool.
   - Assign it to `SettingsPanel > Reduced Motion Toggle`.
   - Delete `ShakeRow` and `MotionRow`.
   - Keep the layout group order: Fullscreen, Volume, Reduced motion, Back.
5. If the toggle has a ButtonPop or other click sound like the other controls, give it the same one.
6. Check both places the prefab is used (`MainMenu`, and `PF_HUD`'s pause screen) for overrides that bring back the old rows.
7. Update `docs/menu.md` (lines 21-33 and 86-98) and the settings row in `docs/design.md`.

**Owner test:**
1. Menu > Settings > tick Reduced motion. The title stops bobbing and the bottom mist stops drifting.
2. Start Level1 straight from the editor. Grass and trees stay still, and ramming a tree doesn't shake the screen.
3. Untick it in the pause menu. The motion comes back immediately.

**Known limit (tell the owner, don't fix unasked):** some motion ignores the setting:
- the fog swirl
- the scribble line wobble
- the fruit tree canopy shake
- the "bonk" word shake
- the tutorial sign shake
- the moo bubble wiggle
- the player waddle

---

## Task 2: title screen fog should move with the other title animations

**Facts (already checked), all in `Assets/Scenes/MainMenu.unity`:**
- `TitleArt > Fog` and `TitleArt > FogBack` use `Shaders/TitleFloat.shader` (materials `M_TitleFog` and `M_TitleFogBack`). They only bob up and down and ripple at the edges. The ripple direction is set in the shader's fragment stage (lines 99-105) by the signs of the `t` terms, and no field controls it.
- `BottomMist > MistLayer1` uses `MistDrift` with speed 0.15 and distance 1.5. `MistLayer2` uses speed 0.1 and distance **-2**. Each layer swings left and right on a sine, and they swing in opposite directions.
- The other title animations are Earth, Cow, Crow, their shadows and Title. They all use TitleFloat to bob up and down. The Motes particles drift upward.

**Step 0: ask the owner first.** Ask which fog looks wrong (the big Fog or FogBack sprites, or the BottomMist puffs) and which direction it should go. Show what each one does now, using the facts above. Don't guess.

**Likely fixes, depending on the answer:**
- **The mist layers go opposite ways:** in `BottomMist > MistLayer2 > MistDrift > Distance`, change -2 to 2 so both layers swing the same way. The speeds can stay different for depth.
- **The fog bob is out of step with the cow and crow:** match `_BobSpeed` and `_Phase` on `M_TitleFog` and `M_TitleFogBack` to the values on the cow's and crow's materials. Read those first.
- **The fog edge ripple runs the wrong way:** flip the sign of the `t` term in `TitleFloat.shader` lines 99-105. That shader is shared by every title sprite, so this would add a property such as `_RippleDir` (default 1) and set it to -1 only on the two fog materials. Ask the owner before you change the shader.

Then update `docs/menu.md` around line 172 and give the owner test steps.

---

## Task 3: Level2 fog thicker, but still thinner than Level1

**Facts (already checked):**
- The thickness comes from `Fog Grid > FogGrid`, through `Fog Color` alpha and `Thin Alpha`.
- Both levels share the material `M_FogSwirl`. Do not change it.
- Current values:

  | Field | Level1 | Level2 |
  |---|---|---|
  | Fog Color alpha | 1 | 0.7 |
  | Thin Alpha | 1 | 0.45 |

- `MistMask > Alpha Cutoff` is 0.65 in Level1 and 0.3 in Level2.

**Steps:**
1. Open Level2.
2. On `Fog Grid > FogGrid`, set `Fog Color` alpha to **0.85** and `Thin Alpha` to **0.7**. Leave everything else alone, including puff count, area, pixels per unit and cell size, because those size the grid.
3. Save the scene.
4. Tell the owner these are starting values to tune by eye. If the fog is still too thin, try 0.9 and 0.8. It must stay below Level1, which is at 1 and 1.
5. Update the Level2 values in `docs/fog.md` (around lines 99-121) and in `docs/level.md` if it lists them.

**Owner test:** play Level2. The fog should hide the path more than before but still look lighter than Level1's. The crow should still clear it as normal.
