One file per system. Each describes how that system works right now, where it lives, and what to tune. Read the docs your task touches before changing anything, and update them in the same change.

| Doc | Covers | Read it when you touch: |
|---|---|---|
| design.md | What the game is meant to be, and a status table of what exists | Scope, new features, anything that changes how the game plays |
| fog.md | The fog texture, fog colliders, clearing and regrowth (`FogGrid.cs`) | Fog, the Fog Grid object, the start clearing, anything that blocks the cow |
| players.md | Cow and crow movement, input, sprites, shadows, cow ram, crow energy and drowning (`PlayerControl.cs`, `CowRam.cs`, `CrowEnergy.cs`, `Water.cs`) | Cow or Crow objects, controls, speed, sprites, ram, energy, water |
| trees.md | Fruit trees and fruit, wall trees with random looks, tree sway (`FruitTree.cs`, `Fruit.cs`, `TreeLook.cs`, `TreeSway.shader`) | Trees, wall trees, tree art, fruit, anything the cow rams for food |
| level.md | Level1 and Level2 flow and layout, shared camera, moo text, exit fade, tree walls, physics layers (`SharedCamera.cs`, `MooPrompt.cs`, `LevelExit.cs`, moo bubble in `PF_HUD`) | Level layout, camera, level start or end, layers and collision matrix |
| menu.md | Main menu: title art with floating motion, start (resumes the save), shared settings panel (fullscreen, volume), credits, saved level and volume; pause (Esc or P); opening video with skip button; home screen button (`MainMenu.cs`, `ButtonPop.cs`, `SettingsPanel.cs`, `PauseMenu.cs`, `TitleFloat.shader`, `Intro.cs`, `HomeScreen.cs`) | Menu scene, settings panel, pause, PlayerPrefs saves, build scene order |
| tutorial.md | Tutorial signs ("fruit. RAM!" etc.) and the wobbly key drawings (`TutorialTip.cs`, `Scribble.shader`, `PF_TutorialTip`) | Tutorial tips, the Tutorial drawings, the Scribble material |
| grass.md | Swaying grass tufts that follow the fog wind (`GrassSway.shader`, `PF_GrassTuft`, `Fog Grid > Wind`) | Grass, the wind, the tuft sprites, anything that sways |
| snakes.md | Roaming snakes that warn, eat the grounded crow, and die to the cow's ram (`Snake.cs`) | Snakes, lose-by-snake, anything that checks crow grounded or ram strength |

## Run it

1. Unity 6000.6.3f1.
2. Open `Assets/Scenes/Level1.unity`.
3. (Level2: open `Assets/Scenes/Level2.unity`. Build order in `File > Build Profiles`: MainMenu, Intro, Level1, Level2, HomeScreen, SampleScene unticked.)
3. Press Play, then click the Game view so it gets keyboard focus.
4. WASD moves the cow. Arrow keys move the crow.

## Scenes

| Scene | Purpose | In build? |
|---|---|---|
| `Assets/Scenes/SampleScene.unity` | Prototype. | No. Listed in `File > Build Profiles > Scene List` but unticked. |
| `Assets/Scenes/MainMenu.unity` | Start menu. See menu.md. | Yes, index 0. |
| `Assets/Scenes/Intro.unity` | Opening video, then Level1. See menu.md. | Yes, index 1. |
| `Assets/Scenes/Level1.unity` | First level: the misty look from `FogTest_Mist` (Sandbox copy kept). Exit loads Level2. See level.md. | Yes, index 2. |
| `Assets/Scenes/Level2.unity` | Second level "The Pond": 3x3 screens, river, pond, snakes. Exit loads HomeScreen. See level.md. | Yes, index 3. |
| `Assets/Scenes/HomeScreen.unity` | "You're home." end screen with a Back to menu button. See menu.md. | Yes, index 4. |

Root objects in SampleScene:

| Object | Holds | Doc |
|---|---|---|
| Main Camera | Orthographic camera (size 6), AudioListener, URP 2D camera data. `SharedCamera` (Level1) | design.md, level.md |
| Global Light 2D | The 2D light for the scene | design.md |
| Fog Grid | `FogGrid` script. Builds the fog sprite and collider children at Start | fog.md |
| Clearing | Empty marker. Its position and X scale set the start clearing | fog.md |
| Grass | Parent of the `GrassTuft` prefab instances, hand placed | grass.md |
| Cow | `PlayerControl` (WASD), Rigidbody2D, BoxCollider2D, sprite, Shadow child | players.md |
| Crow | `PlayerControl` (arrows), Rigidbody2D, sprite, Shadow and FlyShadow children | players.md |

New objects go in the matching group above, and the doc for that group gets a line.

## Build (WebGL)

Active build profile: `Assets/Settings/Build Profiles/Web.asset`. Product Name "Cow and Crow", Company Name "Y2K", Version 1.0.

1. `File > Build Profiles > Web`, click Build, pick the `CowAndCrow/` folder at the project root. It is gitignored (`/CowAndCrow/`). Output: `index.html`, `Build/` (`CowAndCrow.wasm`, `.data`, `.framework.js`, `.loader.js`), `TemplateData/`.
2. Zip the contents of `CowAndCrow/`, not the folder itself: `index.html` must sit at the zip root. About 18 MB.
3. Upload to https://kkaechi.itch.io/cow-and-crow as an HTML project, tick "This file will be played in the browser". Jam: https://itch.io/jam/gimersia-inspire-2026.

Gotchas:

* The Web profile has its own Player Settings. While it is active, `Edit > Project Settings > Player` is ignored (yellow warning on that page). The "Player Settings" tab button at the top right of the Build Profiles window opens the global page, not the profile's copy. To edit the profile's copy, expand the "Player Settings" foldout in the middle of `File > Build Profiles > Web`.
* `Player Settings > Web > Publishing Settings > Compression Format` is Brotli (see "Web build" below). In the YAML, `webGLCompressionFormat: 0` is Brotli and `2` is Disabled.
* Audio is Unity's own (AudioSource). FMOD was removed on 24 Nov 2026. Browsers only start sound after the first click or key, so nothing plays before that.
* Checked 18 Nov 2026 on itch.io: the build loads, fog renders, and WASD and arrow keys reach the game after clicking it. Full level not played through in the browser yet.

## Web build

Web profile (`File > Build Profiles > Web`) settings:

* Canvas 1280 x 720.
* Compression Brotli. itch.io serves `.br` correctly, and only over HTTPS. A plain local file server will not: test with Build And Run.
* Managed Stripping Level Medium.
* IL2CPP Code Generation: optimise for size.
* Code Optimization: Runtime Speed.

Art rule: PNGs need width AND height divisible by 4, or Unity silently skips compression (the `title earth` image alone took 24 MB uncompressed). Pad transparent pixels on the top and right so sprite slices stay put. Check with the Inspector preview footer: it should say DXT1 or DXT5, not RGBA32.

Padded on 24 Nov 2026: title earth, title fog, title fog background, title cow, title crow, title cow shadow, cow and crow ui, cow and crow controls, cow and crow_cow, cow and crow trees, cow and crow (2). Re-pad when the artist re-exports.

Removed to shrink the build: packages Timeline, 2D Animation, 2D Aseprite, 2D PSD Importer, 2D Tilemap Extras, 2D SpriteShape, Visual Scripting, 2D Tooling, 2D Tilemap, Version Control (collab-proxy), Tutorial Framework, Rider, FMOD (`Assets/Plugins`); folders `Assets/Welcome` and `TextMesh Pro/Examples & Extras` (its Resources folder shipped in every build).

## Writing docs

Sections, skip the ones that don't apply:

* What it is: one paragraph, plain language, what the player sees.
* Where it lives: scripts, assets, scene objects, with paths.
* How it works: the behaviour, including the order things happen in.
* Adding a {thing}: a numbered recipe, when the system holds content people keep adding (quests, characters, items, levels).
* Tuning: a table, what / object or asset / field / current value. Values are the real current values; change the table when you change the scene.
* Gotchas: sync points, edge cases, things that broke once.
* Not built yet: gaps against design.md, plus options sketched for decisions not made yet.

Inspector paths as `Object > Component > Field`, menus as `Edit > Project Settings > …`. Current state only; delete what stopped being true.
