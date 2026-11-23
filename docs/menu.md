# Main menu

Status: script and scene are built. Build index 0. Feel and art not tuned.

## What it is

The first screen. A title and a column of buttons: Start, Continue, Settings, Exit. Start plays the opening video (Intro scene), then Level1. Continue resumes the last level the players reached; it only shows if a save exists. Settings opens a panel with a controls card, a fullscreen toggle, a volume slider and a Back button. Exit quits the game and is hidden on WebGL.

## Where it lives

- Script: `Assets/Scripts/MainMenu.cs`, on the menu Canvas.
- Scene: `Assets/Scenes/MainMenu.unity`. Must be build index 0 (order: MainMenu, Intro, Level1, Level2, HomeScreen, SampleScene off).
- Save written by `LevelExit` (see `level.md`) when both animals reach an exit with a `Next Scene`.

## How it works

1. `Start`: checks the references (LogError if one is missing), shows Continue only if `PlayerPrefs` has "SavedLevel", hides Exit on WebGL, sets the toggle from `Screen.fullScreen`, loads the saved volume into the slider and applies it. The UI is set with `...WithoutNotify`, so this does not trigger the change events.
2. Buttons call public methods from their `OnClick` lists. The Toggle and Slider use their `On Value Changed` lists, in the Dynamic section (`SetFullscreen`, `SetVolume`), so the value is passed through.
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

Built through the bridge. Edit in the editor from here.

```
Main Camera (ortho 6, solid #2E1A2B)
Global Light 2D
EventSystem (InputSystemUIInputModule)
Canvas (Overlay, scale 1920x1080 match 0.5, MainMenu script)
  Title
  Buttons (VerticalLayoutGroup + ContentSizeFitter)
    StartButton, ContinueButton, SettingsButton, ExitButton
  SettingsPanel (inactive)
    ControlsText, FullscreenToggle, VolumeRow (VolumeLabel, VolumeSlider), BackButton
```

All `MainMenu` fields and OnClick / On Value Changed lists are wired. Buttons use the built-in UI sprite, placeholder look.

## Gotchas

- The menu scene must be build index 0, or the built game opens on Level1.
- Every scene loaded by name (Level1, later Level2) must be in the `File > Build Profiles` scene list, or `LoadScene` fails.
- The `Fade` image in the level HUD must keep Raycast Target off. It is the same kind of full-screen Image; if it blocks clicks, buttons stop working.
- Continue loads whatever name is saved. If a level is renamed or removed, Continue breaks: clear PlayerPrefs.
- `Screen.fullScreen` in the editor does nothing; test it in a build. On WebGL the browser may ask for a click first.
- Exit is hidden on WebGL (`Application.Quit` does nothing in a browser). In the editor `Application.Quit` does nothing either.

## Not built yet

- Menu art (buttons are the default UI sprite).
- Volume routed to an FMOD bus.
- A way to start over after Continue shows (no "reset save" button).

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

## Home screen scene

Copied from MainMenu and stripped. Camera solid cream #FBEBD6. Canvas (Overlay, 1920x1080, `HomeScreen` script). `Title` "You're home." (plum #2E1A2B, 120), `Subtitle` "The calf is back with the herd." (plum, 56), `Buttons > BackButton` "Back to menu" (plum button, peach text) with OnClick = `HomeScreen.BackToMenu`. Layout is anchors (title and subtitle at 62% height) and the Buttons Vertical Layout Group. Level2's exit (`LevelExit > Next Scene` = "HomeScreen") loads it; it also writes `SavedLevel` = HomeScreen, so Continue then opens the home screen (clear PlayerPrefs, or change this later).
