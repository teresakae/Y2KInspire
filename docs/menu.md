# Main menu

Status: script, scene and title art built. Build index 0. Motion strength not tuned by the owner yet.

## What it is

The first screen: a painted title picture with a column of buttons on the left. The picture is the sky, a green hill, fog, the cow and the crow, with their shadows and the title lettering. Every layer drifts a little (bob, tilt, breathe), out of step, so the screen feels alive without any real animation. Buttons: Start, Settings, Credits. Start always begins a fresh game: it clears the saved level and the tutorial tips already seen, then plays the opening video (Intro scene), then Level1. Settings opens a panel with a controls card, a fullscreen toggle, Volume, Screen shake and Motion sliders and a Back button. Credits opens a panel with placeholder lines. Small gold motes drift up over the picture (`TitleArt > Motes`). Soft fog puffs drift slowly left and right in the bottom-left corner behind the buttons (`TitleArt > BottomMist`). Clicking a button squashes it for 0.2 s and plays a click sound, then the action runs. Menu music loops from the scene's `Music` object.

## Where it lives

- Scripts: `Assets/Scripts/MainMenu.cs` (on the Canvas), `Assets/Scripts/ButtonPop.cs` (on every button), `Assets/Scripts/SettingsPanel.cs` (on the settings panel prefab), `Assets/Scripts/PauseMenu.cs` (on `PF_HUD`).
- Prefab: `Assets/Prefabs/PF_SettingsPanel.prefab`, used in MainMenu and in `PF_HUD > PauseScreen`.
- Shader: `Assets/Shaders/TitleFloat.shader`. One material per art layer in `Assets/Materials/M_Title*.mat`.
- Art: `Assets/Art/Title` (layers, title logo, font `Kkscribble-Regular.ttf`, TMP font `Kkscribble SDF.asset`). Button sheet: `Assets/Art/UI/cow and crow ui.png`.
- Post-processing: `Assets/Settings/VP_Title.asset`, on `Global Volume` in the scene.
- Scene: `Assets/Scenes/MainMenu.unity`. Must be build index 0 (order: MainMenu, Intro, Level1, Level2, HomeScreen, SampleScene off).
- Save written by `LevelExit` (see `level.md`) when both animals reach an exit with a `Next Scene`.

## How it works

1. `MainMenu.Start`: checks the `Settings Panel` reference (LogError if missing) and calls `SettingsPanel.ApplySaved()` to apply the saved volume and motion (the panel starts inactive, so its own script has not run yet).
   `SettingsPanel.OnEnable` (each time the panel opens): checks its references, sets the toggle from `Screen.fullScreen` and the slider from the saved volume. The UI is set with `...WithoutNotify`, so this does not trigger the change events.
2. Each button's `OnClick` calls `ButtonPop.Pop()`; the real method (`StartGame` etc.) sits in that button's `ButtonPop > After Pop` list, so it runs after the squash. The panel's Toggle and Slider use their `On Value Changed` lists, in the Dynamic section (`SettingsPanel.SetFullscreen`, `SettingsPanel.SetVolume`), so the value is passed through.
3. Volume sets `AudioListener.volume` (master volume for every AudioSource) and saves.

PlayerPrefs keys:

| Key | Type | Written by | Meaning |
|---|---|---|---|
| `SavedLevel` | string | `LevelExit`, `PauseMenu.SaveAndQuit` | Scene name of the level reached. Start deletes it and always loads `First Level` (`Intro`), so nothing reads it at the moment (kept for a later Continue button). |
| `Volume` | float 0-1 | `SettingsPanel.SetVolume` | Master volume. Default 1. |
| `Shake` | float 0-1 | `SettingsPanel.SetShake` | Screen shake multiplier. Default 1. |
| `Motion` | float 0-1 | `SettingsPanel.SetMotion` | Motion strength (`_ReduceMotion` = 1 minus this). Default 1. |

Every write is followed by `PlayerPrefs.Save()`, because WebGL does not persist otherwise. To reset the save while testing: `Edit > Clear All PlayerPrefs`.

## Intro (opening video)

`Assets/Scripts/Intro.cs` is on `Main Camera` in `Assets/Scenes/Intro.unity` (build index 1). `Main Camera` also has an `AudioListener` (added 28 Nov 2026; the scene had none, so the menu music carried over by `MenuMusic` was inaudible). `MainMenu > First Level` is `Intro`, so Start goes here when there is no save; with a save, Start skips it and loads the saved level.

- Plays `Assets/StreamingAssets/intro_white.mp4` (H.264, 1920x1080, 36.5 s, 8.4 MB) through the camera's `VideoPlayer` (Render Mode Camera Far Plane, Aspect Ratio Fit Inside, Audio Output None, Play On Awake off; `Intro` sets the URL and starts it).
- `Start` begins loading `Intro > Next Scene` (default `Level1`) in the background with activation held, so the level is ready behind the video. When the video ends or the `SkipButton` is clicked, `Leave()` starts a fade to white (`Canvas > Fade`, `Fade Time` 0.5 s, step capped at 0.05 s per frame); at full white the loaded scene switches on. Input is ignored once leaving. If the video fails to load it logs an error and leaves the same way. No `Fade` Image = logs an error and switches at once.
- The camera background is white, so letterbox bars match the video's white frames.
- `Canvas > Fade`: full-screen white Image, alpha 0, Raycast Target off, last sibling so it covers `SkipButton`; wired to `Main Camera > Intro > Fade`.
- Skip button: `Canvas > SkipButton` (Overlay canvas 1920x1080 match 0.5, with an EventSystem), anchored bottom right, 40 px margin, 260 x 90, styled like the menu buttons (light slice, dark on hover and press, label "skip" in `Kkscribble SDF` 48, plum). `Button > OnClick` = `ButtonPop.Pop`; `ButtonPop > After Pop` = `Main Camera > Intro.Leave` (public). Key presses and clicks elsewhere do nothing.

| What | Field | Value |
|---|---|---|
| Video file | `Main Camera > Intro > File Name` | `intro_white.mp4` |
| Fade to white (s) | `Main Camera > Intro > Fade Time` | 0.5 |
| Scene after | `Main Camera > Intro > Next Scene` | `Level1` |

Gotchas:
- The video must stay in `Assets/StreamingAssets`: WebGL can only play video from a URL, not from a VideoClip asset. Everything in that folder ships in the build as-is, so keep source art out of it. The 18 opening illustration PNGs live in `Assets/Art/Cutscene` for that reason; nothing uses them.
- The editor only advances Play mode while Unity has focus, so check the video by clicking into the Game view.

## Home screen

The ending cutscene. `Assets/Scripts/HomeScreen.cs` is on the Canvas of `Assets/Scenes/HomeScreen.unity` (build index 4, reached from Level2's exit, which fades to white first). A timer in `Update` drives three CanvasGroup alphas:

1. Scene opens white. Text ("You're home." / "The herd was waiting.") fades in, holds, fades out.
2. `EndPicture` (`Assets/Art/Cutscene/end.png`, "The End." drawn in) fades in over the white.
3. After a short beat the Back to menu button fades in. It is not clickable (`interactable` and `blocksRaycasts` off) until it starts to appear.

`BackToMenu()` loads `Menu Scene` (wired to the button OnClick). Start on the menu then begins a fresh game. The scene must be in `File > Build Profiles`.

Tuning, all on `Canvas > HomeScreen`:

| What | Object | Field | Value |
|---|---|---|---|
| Menu to load | Canvas | `Menu Scene` | `MainMenu` |
| Text group | Canvas | `Text Group` | `Canvas > Text` |
| Picture group | Canvas | `Picture` | `Canvas > EndPicture` |
| Button group | Canvas | `Buttons` | `Canvas > Buttons` |
| Text fade in | Canvas | `Text Fade In` | 1 s |
| Text stays | Canvas | `Text Hold` | 2.5 s |
| Text fade out | Canvas | `Text Fade Out` | 1 s |
| Picture fade in | Canvas | `Picture Fade In` | 1.5 s |
| Wait before button | Canvas | `Button Delay` | 1 s |
| Button fade in | Canvas | `Button Fade In` | 0.5 s |

To check later phases without waiting, the timer is the private `elapsed` field (total about 7.5 s before the button starts).

## Settings panel (shared)

Prefab `Assets/Prefabs/PF_SettingsPanel.prefab`, script `SettingsPanel` on its root. One copy in `MainMenu > Canvas` (Pos X 280) and one in `PF_HUD > PauseScreen`. Contents: controls card, fullscreen toggle, Volume, Screen shake and Motion rows (each a label and a slider, Min 0 Max 1, default 1), Back button (the pause copy also has Quit). Layout spacing 24 so the pause copy fits the 900 tall panel. Fields: `Fullscreen Toggle`, `Volume Slider`, `Shake Slider`, `Motion Slider`. Each slider's On Value Changed calls `SetVolume`, `SetShake` or `SetMotion`. All are saved in PlayerPrefs (`Volume`, `Shake`, `Motion`).

| Method | Wire it to |
|---|---|
| `SetFullscreen(bool)` | Toggle On Value Changed, Dynamic bool |
| `SetVolume(float)` | Slider On Value Changed, Dynamic float |
| `SetShake(float)` | Shake Slider On Value Changed, Dynamic float |
| `SetMotion(float)` | Motion Slider On Value Changed, Dynamic float |
| `Close()` | Back button, `ButtonPop > After Pop` (hides the panel; in the pause screen this also resumes, see Pause) |

- Volume: `AudioListener.volume`, master volume for every sound.
- Screen shake: `SharedCamera.Shake` multiplies every shake by it; 0 turns shakes off.
- Motion: sets the global shader value `_ReduceMotion` (1 minus the slider). `TitleFloat`, `GrassSway` and `TreeSway` shaders and `MistDrift` scale their movement by 1 - `_ReduceMotion`, so 0 = still, 1 = full. The global is 0 until set, so full motion by default.
- `SettingsPanel.ApplySaved()` (static) applies saved volume and motion; `MainMenu.Start` calls it because the panel starts inactive.

## Pause

`PauseMenu` is on the `PF_HUD` root, so both levels have it. Esc or P toggles it (P too, because browsers use Esc to leave fullscreen). Pausing sets `Time.timeScale` 0 and shows `PF_HUD > PauseScreen` (plum dim, alpha about 60%) with a `PF_SettingsPanel` inside. The panel has two buttons, both 460 wide here (Back is 360 in the main menu). `Back` resumes (same as Esc/P again). `Save and quit` exists only here: a `QuitButton` added to the `PF_SettingsPanel` instance inside `PF_HUD`, with `ButtonPop > After Pop` = `PauseMenu.SaveAndQuit`. The main menu's panel does not have it. `SaveAndQuit()` writes the current level's scene name to `SavedLevel` and loads `Menu Scene`. Start ignores the save and begins a fresh game, so for now this button just quits to the menu. Fields: `Pause Screen`, `Settings Panel` (the panel inside it), `Menu Scene` (default `MainMenu`, must be in `File > Build Profiles`).

A browser refresh goes to the menu, and Start begins a fresh game.

- `PlayerControl`, `CowRam` and `SharedCamera` return early while `Time.timeScale` is 0. `ButtonPop` uses unscaled time, so button squashes still play.
- `Scribble.shader` uses `_Time`, so the key drawings freeze while paused.
- `PauseMenu.OnDestroy` sets `timeScale` back to 1, because it survives scene loads.

## Lose screen

The game-over screen. When a snake eats the crow or the crow drowns, the screen fades to white, then a panel shows a header, a hint, and two buttons: Play Again and Main Menu. The game freezes behind the panel. The header and hint differ for the two causes.

### Where it lives

- Script: `Assets/Scripts/LoseScreen.cs`, on `PF_HUD > LoseScreen` (active, full-screen). `PF_HUD` is in both levels, so nothing is set per scene.
- `Pause Menu`: set to the `PauseMenu` on the `PF_HUD` root (the LoseScreen's parent). Switched off when the lose screen starts.
- `Whiteout`: inactive at start. Full-screen white Image, Raycast Target on, so nothing behind it can be clicked.
- `Panel`: inactive at start. Image `cow and crow ui_10` (Draw Mode Sliced, 1100 x 560, centred). Vertical Layout Group, padding 60, spacing 24. Holds:
  - `Header`: TMP, `Kkscribble SDF`, size 96.
  - `Subtitle`: TMP, size 54.
  - `Buttons`: Horizontal Layout Group, spacing 40. Holds `PlayAgainButton` and `MenuButton`, copies of the settings panel's button (`cow and crow ui_4`, ButtonPop, 360 x 90). `OnClick` = `ButtonPop.Pop`; `ButtonPop > After Pop` = `LoseScreen.PlayAgain` / `LoseScreen.MainMenu`.
- `cow and crow ui_10` has a 60 px 9-slice border on all sides (Sprite Editor), so the panel stretches without warping. Images that use it as Simple are unaffected.

### How it works

1. `Snake` calls `ShowEaten()` after `Eaten Delay`. `CrowEnergy` calls `ShowDrowned()` after `Drown Delay`. Both find the LoseScreen in `Awake` with `FindAnyObjectByType`. A scene with none (test scenes) reloads directly, as before.
2. `Show` sets the header and hint text, then fades `Whiteout` from 0 to `White Alpha` over `Fade Time`. It uses unscaled time. The game keeps running during the fade. It also switches off `Pause Menu`, so Esc or P can't open pause over the lose screen or unfreeze the game behind it.
3. At full white, `Panel` turns on, `Time.timeScale` goes to 0 (snakes and the cow stop behind the panel), and Play Again is selected. Enter or Space presses it.
4. Repeat calls are ignored.
5. Play Again sets `Time.timeScale` to 1 and reloads the active scene.
6. Main Menu sets `Time.timeScale` to 1, writes the active scene name to PlayerPrefs `SavedLevel` (like Save and quit), and loads `Menu Scene`. Start in the menu clears `SavedLevel` again (`MainMenu.cs`), so for now this does not resume the level. See What it is, above.

### Tuning

All on `PF_HUD > LoseScreen` (script `LoseScreen`) unless noted.

| What | Where | Default |
|---|---|---|
| Fade to white (s) | Fade Time | 0.8 |
| Whiteout opacity once faded (1 = solid white) | White Alpha | 1 |
| Header when eaten | Eaten Header | "Crow's been eaten!" |
| Hint when eaten | Eaten Subtitle | "Fly around the snakes. Let Cow RAM to protect!" |
| Header when drowned | Drown Header | "Crow cant swim!" |
| Hint when drowned | Drown Subtitle | "Don't land in the water." |
| Scene loaded by Main Menu | Menu Scene | `MainMenu` |
| Panel size | `Panel > Rect Transform` | 1100 x 560 |

The delays before the screen are on `Snake > Eaten Delay` and `CrowEnergy > Drown Delay` (see `snakes.md` and `players.md`).

### Gotchas

- Not checked in Play mode: the fade (the editor did not tick while unfocused). The panel layout was checked in a screenshot only.
- `Menu Scene` must be in `File > Build Profiles`, like every scene the game loads.

## MainMenu public methods

| Method | Wire it to |
|---|---|
| `StartGame()` | Start button After Pop |
| `OpenSettings()` | Settings button After Pop |

## Scene

```
Main Camera (ortho 10.8, solid sky blue, Post Processing on)
Global Light 2D
Global Volume (Volume, global, VP_Title)
TitleArt (all SpriteRenderers in world space, one material each)
  Sky, FogBack, Earth, Fog, CowShadow, Cow, CrowShadow, Crow, Title   (back to front, Order in Layer 0-8)
  BottomMist > MistLayer1, MistLayer2 (6 `Puff` each: `Assets/Art/Fog/fog.png` fog_0, Sprite-Unlit-Default, order 4, colour (0.96, 0.96, 1) alpha 0.35 to 0.6, scale 1.6 to 2.8, bottom-left x -21 to -7, y -11.5 to -4.7). `Mist Drift` slides each layer: layer 1 Speed 0.15 Distance 1.5, layer 2 Speed 0.1 Distance -2.
  Motes (Particle System, Order in Layer 4)
EventSystem (InputSystemUIInputModule)
Canvas (Overlay, scale 1920x1080 match 0.5, MainMenu script)
  Buttons (top left, VerticalLayoutGroup + ContentSizeFitter)
    StartButton, SettingsButton, CreditsButton
  SettingsPanel (PF_SettingsPanel instance, inactive, Pos X 280; VerticalLayoutGroup)
    ControlsText, FullscreenToggle, VolumeRow (VolumeLabel, VolumeSlider), BackButton
  CreditsPanel (inactive)
    CreditsTitle, CreditsText, BackButton
```

The camera sees 38.4 x 21.6 units, which is exactly the 3840x2160 sky at 100 pixels per unit. The sky is scaled 1.04 so a drifting edge never shows the camera colour.

Buttons use slices of `Assets/Art/UI/cow and crow ui.png` (`cow and crow ui_N`, plus `_bubble` and `_tail`):

| Slice | What | Used by |
|---|---|---|
| 0, 2, 4 | long button, light | Start/Credits/Intro Skip (0), Settings and both Back buttons (4), 2 unused |
| 1, 3, 5 | long button, dark (same row as 0, 2, 4) | hover and pressed sprite of the matching light one |
| 6 | small square, light, 50 px 9-slice borders | toggle box, slider handle, tutorial `Sign` |
| 7 | slider track | slider Background |
| 8 | small square, dark | toggle checkmark |
| 9 | cream strip | slider Fill |
| 10 | big panel | Credits panel |
| `_bubble` | oval speech bubble, light, Simple (not sliced) | moo bubble Body (`level.md`) |
| `_tail` | speech bubble tail, points down, Simple | moo bubble Tail (`level.md`) |

Button setup: Image = light slice, Button Transition = Sprite Swap with Highlighted and Pressed = dark slice, Selected and Disabled = light slice. Label: TMP, `Kkscribble SDF`, plum #2E1A2B, size 56. `ButtonPop` on the button; Button `OnClick` = `ButtonPop.Pop`; real action in `After Pop`:

| Button | After Pop |
|---|---|
| Start / Settings | `MainMenu.StartGame` / `OpenSettings` |
| Credits | `CreditsPanel.SetActive(true)` |
| Settings Back | `SettingsPanel.Close` (the panel's own) |
| Credits Back | `CreditsPanel.SetActive(false)` |

Credits text is placeholder: edit `CreditsPanel > CreditsText`.

The settings panel and `CreditsPanel` are 920x900, shifted 280 right of centre (`Rect Transform > Pos X` 280) so they don't cover the title lettering.

## Tuning

Layer motion, all on the layer's own material (`Assets/Materials/M_Title*.mat`). Period of a sine = 6.28 / speed seconds. Bob is in world units, tilt in degrees, scale is a fraction, ripple is in UV units.

| Material | Bob amp / speed | Tilt deg / speed | Scale amp / speed | Phase | Ripple amp |
|---|---|---|---|---|---|
| M_TitleSky | 0 | 0 | 0 | 0 | 0 |
| M_TitleFogBack | 0.4 / 0.9 | 0 | 0 | 1.3 | 0.012 |
| M_TitleEarth | 0 | 0.6 / 0.35 | 0 | 0 | 0 |
| M_TitleFog | 0.35 / 1.2 | 0 | 0 | 0 | 0.008 |
| M_TitleCowShadow | 0 | 0 | -0.08 / 1.5 | 0.8 | 0 |
| M_TitleCow | 0.16 / 1.5 | 0 | 0 | 0.8 | 0 |
| M_TitleCrowShadow | 0 | 0 | -0.14 / 1.1 | 2.1 | 0 |
| M_TitleCrow | 0.4 / 1.1 | 3 / 0.8 | 0 | 2.1 | 0 |
| M_TitleLogo | 0 | 2 / 0.9 | 0.03 / 1.3 | 0 | 0 |

Motion was doubled on 23 Nov (still not tuned by the owner). Ripple scale 6 and ripple speed 0.6 on all. A shadow shares speed and phase with its animal and has a negative scale amp, so it shrinks as the animal rises.

`TitleArt > Motes` (Particle System, copy of Level1's motes, material `M_MoteGlow`): box 38 x 22 (the camera view), life 4 to 7 s, start size 0.2 to 0.4, gold E8B23A and pink E6A6C7, 11 per second, 80 max, prewarm on, slow upward drift (Velocity over Lifetime x 0.05, y 0.2), Noise 0.3 / 0.5, fade in and out, Order in Layer 4 (above the fog, behind the animals and title).

`Assets/Settings/VP_Title.asset` (do not edit `VP_Mist`):

| Effect | Values |
|---|---|
| Bloom | intensity 0.3, threshold 0.95, scatter 0.6 |
| Vignette | intensity 0.22, smoothness 0.6, colour plum #2E1A2B |
| Color Adjustments | saturation +12, post exposure +0.1, colour filter (1, 0.985, 0.955) |
| Film Grain | Thin1, intensity 0.1 |

`ButtonPop` (on each button): `Curve` scale 1, 0.9 at 40%, 1.05 at 75%, 1 at the end; `Duration` 0.2 s. Repeat clicks while it plays are ignored. `Click Clip` (`UI_buttonClick`) is played by `Pop()` through an AudioSource on the button (found in `Awake`, added with Play On Awake off if missing). Empty = silent. `Hover Clip` (`UI_hover.mp3`) plays when the pointer enters the button (`IPointerEnterHandler`), only if the Button is interactable. Both clips are wired on every ButtonPop in all scenes, `PF_HUD` and `PF_SettingsPanel`. Clips are assigned per button (or on the prefab) in the Inspector.

Music: root object `Music` in `MainMenu` has an AudioSource: `AudioClip` `BGM_menu`, Loop on, Play On Awake on, Volume 0.5, Spatial Blend 0 (2D). Clips live in `Assets/Audio`. `MenuMusic` (`Assets/Scripts/MenuMusic.cs`) is on `Music` in `MainMenu`, and `Intro` and `HomeScreen` each have their own `Music` object (same AudioSource settings, plus `MenuMusic`). It marks the object `DontDestroyOnLoad`, so `BGM_menu` plays on without restarting from `MainMenu` into `Intro` and from `HomeScreen` back to `MainMenu`; a duplicate copy in the next scene destroys itself. Any scene not in its `Keep In Scenes` list (default `MainMenu`, `Intro`, `HomeScreen`) destroys it, so the level music takes over. `ButtonPop.clickClip` is `UI_buttonClick` on every button (assigned in `PF_HUD`, `PF_SettingsPanel` and the scenes). Browsers only start sound after the first click or key.

Title lettering: `TitleArt > Title > SpriteRenderer` shows `Assets/Art/Title/title logo.png`, slice `title logo` (the whole 688 x 540 image, PPU 100, centre pivot), at position (-11.8, 5.6), order 8, material `M_TitleLogo`. The artist's halves `title (1)_0` (top) and `title (1)_1` (bottom) are unused. To swap: put a new sprite on that SpriteRenderer, set Single sprite mode in its import, and move the object to taste.

## Not built yet

- A way to start over once a save exists (no "reset save" button; use `Edit > Clear All PlayerPrefs`).

## Home screen scene

Copied from MainMenu and stripped. Camera solid white (continues Level2's white fade). Canvas (Overlay, 1920x1080, `HomeScreen` script), children bottom to top:

- `EndPicture`: Image with the end sprite, stretched anchors, Preserve Aspect on, Raycast Target off, CanvasGroup alpha 0. `end.png` import Max Size 2048 (WebGL).
- `Text`: stretched, CanvasGroup alpha 0. Holds `Title` "You're home." (plum #2E1A2B, 120) and `Subtitle` "The herd was waiting." (plum, 56), both in `Kkscribble SDF`, both anchored at 62% height.
- `Buttons`: CanvasGroup alpha 0, not interactable. `BackButton` "Back to menu" (plum button, peach text), OnClick = `HomeScreen.BackToMenu`, Vertical Layout Group anchored at 21% height (middle of the grass band, below the herd in the end picture).

Level2's exit (`LevelExit > Next Scene` = "HomeScreen") loads it; it also writes `SavedLevel` = HomeScreen, which Start clears.
