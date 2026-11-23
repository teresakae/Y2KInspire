# Main menu

Status: script, scene and title art built. Build index 0. Motion strength not tuned by the owner yet.

## What it is

The first screen: a painted title picture with a column of buttons on the left. The picture is the sky, a green hill, fog, the cow and the crow, with their shadows and the title lettering. Every layer drifts a little (bob, tilt, breathe), out of step, so the screen feels alive without any real animation. Buttons: Start, Continue, Settings, Credits, Exit. Start plays the opening video (Intro scene), then Level1. Continue resumes the last level the players reached; it only shows if a save exists. Settings opens a panel with a controls card, a fullscreen toggle, a volume slider and a Back button. Credits opens a panel with placeholder lines. Exit quits the game and is hidden on WebGL. Clicking a button squashes it for 0.2 s, then the action runs.

## Where it lives

- Scripts: `Assets/Scripts/MainMenu.cs` (on the Canvas) and `Assets/Scripts/ButtonPop.cs` (on every button).
- Shader: `Assets/Shaders/TitleFloat.shader`. One material per art layer in `Assets/Materials/M_Title*.mat`.
- Art: `Assets/Art/Title` (layers, button sheet `cow and crow ui.png`, font `Kkscribble-Regular.ttf`, TMP font `Kkscribble SDF.asset`).
- Post-processing: `Assets/Settings/VP_Title.asset`, on `Global Volume` in the scene.
- Scene: `Assets/Scenes/MainMenu.unity`. Must be build index 0 (order: MainMenu, Intro, Level1, Level2, HomeScreen, SampleScene off).
- Save written by `LevelExit` (see `level.md`) when both animals reach an exit with a `Next Scene`.

## How it works

1. `Start`: checks the references (LogError if one is missing), shows Continue only if `PlayerPrefs` has "SavedLevel", hides Exit on WebGL, sets the toggle from `Screen.fullScreen`, loads the saved volume into the slider and applies it. The UI is set with `...WithoutNotify`, so this does not trigger the change events.
2. Each button's `OnClick` calls `ButtonPop.Pop()`; the real method (`StartGame` etc.) sits in that button's `ButtonPop > After Pop` list, so it runs after the squash. The Toggle and Slider use their `On Value Changed` lists, in the Dynamic section (`SetFullscreen`, `SetVolume`), so the value is passed through.
3. Volume sets `AudioListener.volume` and saves. FMOD is not wired, so this does not affect FMOD audio yet: route it to an FMOD bus when audio arrives.

PlayerPrefs keys:

| Key | Type | Written by | Meaning |
|---|---|---|---|
| `SavedLevel` | string | `LevelExit` | Scene name of the level reached. Continue loads it. Missing = no save. |
| `Volume` | float 0-1 | `MainMenu.SetVolume` | Master volume. Default 1. |

Every write is followed by `PlayerPrefs.Save()`, because WebGL does not persist otherwise. To reset the save while testing: `Edit > Clear All PlayerPrefs`.

## Intro (opening video)

`Assets/Scripts/Intro.cs` is on `Main Camera` in `Assets/Scenes/Intro.unity` (build index 1). `MainMenu > First Level` is `Intro`, so Start goes here; Continue skips it and loads the saved level.

- Plays `Assets/StreamingAssets/intro_white.mp4` (H.264, 1920x1080, 36.5 s, 8.4 MB) through the camera's `VideoPlayer` (Render Mode Camera Far Plane, Aspect Ratio Fit Inside, Audio Output None, Play On Awake off; `Intro` sets the URL and starts it).
- When the video ends, or any key or left click is pressed, it loads `Intro > Next Scene` (default `Level1`). If the video fails to load it logs an error and loads Level1 straight away.
- The camera background is white, so letterbox bars match the video's white frames.

| What | Field | Value |
|---|---|---|
| Video file | `Main Camera > Intro > File Name` | `intro_white.mp4` |
| Scene after | `Main Camera > Intro > Next Scene` | `Level1` |

Gotchas:
- The video must stay in `Assets/StreamingAssets`: WebGL can only play video from a URL, not from a VideoClip asset. Everything in that folder ships in the build as-is, so keep source art out of it. The 18 opening illustration PNGs live in `Assets/Art/Cutscene` for that reason; nothing uses them.
- The editor only advances Play mode while Unity has focus, so check the video by clicking into the Game view.

## Home screen

`Assets/Scripts/HomeScreen.cs` is on the Canvas of `Assets/Scenes/HomeScreen.unity` (build index 4, reached from Level2's exit). Field `Menu Scene` (default `MainMenu`). One public method, `BackToMenu()`, loads it; wire it to the scene's button OnClick. It leaves `PlayerPrefs` alone, so Continue still works. The scene must be in `File > Build Profiles`.

## Public methods

| Method | Wire it to |
|---|---|
| `StartGame()` | Start button OnClick |
| `ContinueGame()` | Continue button OnClick |
| `OpenSettings()` | Settings button OnClick |
| `CloseSettings()` | Back button OnClick |
| `QuitGame()` | Exit button OnClick |
| `SetFullscreen(bool)` | Toggle On Value Changed, Dynamic bool |
| `SetVolume(float)` | Slider On Value Changed, Dynamic float |

## Scene

```
Main Camera (ortho 10.8, solid sky blue, Post Processing on)
Global Light 2D
Global Volume (Volume, global, VP_Title)
TitleArt (all SpriteRenderers in world space, one material each)
  Sky, FogBack, Earth, Fog, CowShadow, Cow, CrowShadow, Crow, Title   (back to front, Order in Layer 0-8)
EventSystem (InputSystemUIInputModule)
Canvas (Overlay, scale 1920x1080 match 0.5, MainMenu script)
  Buttons (top left, VerticalLayoutGroup + ContentSizeFitter)
    StartButton, ContinueButton, SettingsButton, CreditsButton, ExitButton
  SettingsPanel (inactive; VerticalLayoutGroup)
    ControlsText, FullscreenToggle, VolumeRow (VolumeLabel, VolumeSlider), BackButton
  CreditsPanel (inactive)
    CreditsTitle, CreditsText, BackButton
```

The camera sees 38.4 x 21.6 units, which is exactly the 3840x2160 sky at 100 pixels per unit. The sky is scaled 1.04 so a drifting edge never shows the camera colour.

Buttons use slices of `cow and crow ui.png` (`cow and crow ui_N`):

| Slice | What | Used by |
|---|---|---|
| 0, 2, 4 | long button, light | Start/Credits (0), Continue/Exit (2), Settings and both Back buttons (4) |
| 1, 3, 5 | long button, dark (same row as 0, 2, 4) | hover and pressed sprite of the matching light one |
| 6 | small square, light | toggle box, slider handle |
| 7 | slider track | slider Background |
| 8 | small square, dark | toggle checkmark |
| 9 | cream strip | slider Fill |
| 10 | big panel | Settings and Credits panels |

Button setup: Image = light slice, Button Transition = Sprite Swap with Highlighted and Pressed = dark slice, Selected and Disabled = light slice. Label: TMP, `Kkscribble SDF`, plum #2E1A2B, size 56. `ButtonPop` on the button; Button `OnClick` = `ButtonPop.Pop`; real action in `After Pop`:

| Button | After Pop |
|---|---|
| Start / Continue / Settings / Exit | `MainMenu.StartGame` / `ContinueGame` / `OpenSettings` / `QuitGame` |
| Credits | `CreditsPanel.SetActive(true)` |
| Settings Back | `MainMenu.CloseSettings` |
| Credits Back | `CreditsPanel.SetActive(false)` |

Credits text is placeholder: edit `CreditsPanel > CreditsText`.

`SettingsPanel` and `CreditsPanel` are 920x900, shifted 280 right of centre (`Rect Transform > Pos X` 280) so they don't cover the title lettering.

## Tuning

Layer motion, all on the layer's own material (`Assets/Materials/M_Title*.mat`). Period of a sine = 6.28 / speed seconds. Bob is in world units, tilt in degrees, scale is a fraction, ripple is in UV units.

| Material | Bob amp / speed | Tilt deg / speed | Scale amp / speed | Phase | Ripple amp |
|---|---|---|---|---|---|
| M_TitleSky | 0 | 0 | 0 | 0 | 0 |
| M_TitleFogBack | 0.2 / 0.9 | 0 | 0 | 1.3 | 0.006 |
| M_TitleEarth | 0 | 0.3 / 0.35 | 0 | 0 | 0 |
| M_TitleFog | 0.18 / 1.2 | 0 | 0 | 0 | 0.004 |
| M_TitleCowShadow | 0 | 0 | -0.04 / 1.5 | 0.8 | 0 |
| M_TitleCow | 0.08 / 1.5 | 0 | 0 | 0.8 | 0 |
| M_TitleCrowShadow | 0 | 0 | -0.08 / 1.1 | 2.1 | 0 |
| M_TitleCrow | 0.2 / 1.1 | 1.5 / 0.8 | 0 | 2.1 | 0 |
| M_TitleLogo | 0 | 1 / 0.9 | 0.015 / 1.3 | 0 | 0 |

Ripple scale 6 and ripple speed 0.6 on all. A shadow shares speed and phase with its animal and has a negative scale amp, so it shrinks as the animal rises.

`Assets/Settings/VP_Title.asset` (do not edit `VP_Mist`):

| Effect | Values |
|---|---|
| Bloom | intensity 0.3, threshold 0.95, scatter 0.6 |
| Vignette | intensity 0.22, smoothness 0.6, colour plum #2E1A2B |
| Color Adjustments | saturation +12, post exposure +0.1, colour filter (1, 0.985, 0.955) |
| Film Grain | Thin1, intensity 0.1 |

`ButtonPop` (on each button): `Curve` scale 1, 0.9 at 40%, 1.05 at 75%, 1 at the end; `Duration` 0.2 s. Repeat clicks while it plays are ignored.

Swapping the title lettering: put a new sprite on `TitleArt > Title > SpriteRenderer`, set Single sprite mode in its import, and move the object to taste.

## Not built yet

- Volume routed to an FMOD bus.
- A way to start over after Continue shows (no "reset save" button).

## Home screen scene

Copied from MainMenu and stripped. Camera solid cream #FBEBD6. Canvas (Overlay, 1920x1080, `HomeScreen` script). `Title` "You're home." (plum #2E1A2B, 120), `Subtitle` "The calf is back with the herd." (plum, 56), `Buttons > BackButton` "Back to menu" (plum button, peach text) with OnClick = `HomeScreen.BackToMenu`. Layout is anchors (title and subtitle at 62% height) and the Buttons Vertical Layout Group. Level2's exit (`LevelExit > Next Scene` = "HomeScreen") loads it; it also writes `SavedLevel` = HomeScreen, so Continue then opens the home screen (clear PlayerPrefs, or change this later).
