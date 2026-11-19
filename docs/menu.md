# Main menu

Status: `MainMenu.cs` is written. The `MainMenu` scene is not built yet (editor setup below).

## What it is

The first screen. A title and a column of buttons: Start, Continue, Settings, Exit. Start begins Level1. Continue resumes the last level the players reached; it only shows if a save exists. Settings opens a panel with a controls card, a fullscreen toggle, a volume slider and a Back button. Exit quits the game and is hidden on WebGL.

## Where it lives

- Script: `Assets/Scripts/MainMenu.cs`, on the menu Canvas.
- Scene: `Assets/Scenes/MainMenu.unity` (to be created). Must be build index 0.
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

## Editor setup

1. `File > New Scene`, pick the 2D template (Basic 2D (URP) if listed). Save as `Assets/Scenes/MainMenu.unity`.
2. `GameObject > UI > Canvas`. Canvas: Render Mode Screen Space - Overlay. Canvas Scaler: UI Scale Mode Scale With Screen Size, Reference Resolution 1920 x 1080. Unity adds an EventSystem; make sure it uses `Input System UI Input Module` (click the "Replace with InputSystemUIInputModule" button if it shows).
3. Under the Canvas: `Title` (`GameObject > UI > Text - TextMeshPro`), and a `Buttons` empty object with a `Vertical Layout Group` (Control Child Size on, Child Alignment Middle Center, Spacing about 20). Under `Buttons`: four `Button - TextMeshPro` objects named `StartButton`, `ContinueButton`, `SettingsButton`, `ExitButton`.
4. Under the Canvas: `SettingsPanel` (an Image, stretched, `GameObject > UI > Panel`). Inside it: a TMP text for the controls card, a `Toggle` (fullscreen), a `Slider` (Min 0, Max 1, Whole Numbers off), a `BackButton`. Untick the `SettingsPanel` active checkbox. Build it as a prefab if it grows.
5. Add `MainMenu` to the Canvas. Fill `MainMenu > Continue Button`, `Exit Button`, `Settings Panel`, `Fullscreen Toggle`, `Volume Slider`. `First Level` stays "Level1".
6. OnClick for each button: drag the Canvas into the slot, pick `MainMenu > method` as in the table above. Toggle: `Toggle > On Value Changed > MainMenu > SetFullscreen` under Dynamic bool. Slider: `Slider > On Value Changed > MainMenu > SetVolume` under Dynamic float.
7. `File > Build Profiles`: scene list with `MainMenu` first (index 0), then `Level1`.

## Gotchas

- The menu scene must be build index 0, or the built game opens on Level1.
- Every scene loaded by name (Level1, later Level2) must be in the `File > Build Profiles` scene list, or `LoadScene` fails.
- The `Fade` image in the level HUD must keep Raycast Target off. It is the same kind of full-screen Image; if it blocks clicks, buttons stop working.
- Continue loads whatever name is saved. If a level is renamed or removed, Continue breaks: clear PlayerPrefs.
- `Screen.fullScreen` in the editor does nothing; test it in a build. On WebGL the browser may ask for a click first.
- Exit is hidden on WebGL (`Application.Quit` does nothing in a browser). In the editor `Application.Quit` does nothing either.

## Not built yet

- The scene itself and its art.
- Volume routed to an FMOD bus.
- A way to start over after Continue shows (no "reset save" button).
