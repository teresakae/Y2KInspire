One file per system. Each describes how that system works right now, where it lives, and what to tune. Read the docs your task touches before changing anything, and update them in the same change.

| Doc | Covers | Read it when you touch: |
|---|---|---|
| design.md | What the game is meant to be, and a status table of what exists | Scope, new features, anything that changes how the game plays |
| fog.md | The fog texture, fog colliders, clearing and regrowth (`FogGrid.cs`) | Fog, the Fog Grid object, the start clearing, anything that blocks the cow |
| players.md | Cow and crow movement, input, sprites, shadows, cow ram, crow energy (`PlayerControl.cs`, `CowRam.cs`, `CrowEnergy.cs`) | Cow or Crow objects, controls, speed, sprites, ram, energy |
| trees.md | Fruit trees and fruit, and wall trees with random looks (`FruitTree.cs`, `Fruit.cs`, `TreeLook.cs`) | Trees, wall trees, tree art, fruit, anything the cow rams for food |
| level.md | Level1 flow and layout, shared camera, moo text, exit fade, tree walls, physics layers (`SharedCamera.cs`, `MooPrompt.cs`, `LevelExit.cs`) | Level layout, camera, level start or end, layers and collision matrix |
| snakes.md | Roaming snakes that warn, eat the grounded crow, and die to the cow's ram (`Snake.cs`) | Snakes, lose-by-snake, anything that checks crow grounded or ram strength |

## Run it

1. Unity 6000.6.3f1.
2. Open `Assets/Scenes/SampleScene.unity`.
3. Press Play, then click the Game view so it gets keyboard focus.
4. WASD moves the cow. Arrow keys move the crow.

## Scenes

| Scene | Purpose | In build? |
|---|---|---|
| `Assets/Scenes/SampleScene.unity` | Prototype. The only scene. | Yes, it is the only entry in `ProjectSettings/EditorBuildSettings.asset` (enabled). Check `File > Build Profiles` if this changes. |
| `Assets/Level1.unity` | First level, copy of SampleScene. See level.md. | Yes (being set up) |

Root objects in SampleScene:

| Object | Holds | Doc |
|---|---|---|
| Main Camera | Orthographic camera (size 6), AudioListener, URP 2D camera data. `SharedCamera` (Level1) | design.md, level.md |
| Global Light 2D | The 2D light for the scene | design.md |
| Fog Grid | `FogGrid` script. Builds the fog sprite and collider children at Start | fog.md |
| Clearing | Empty marker. Its position and X scale set the start clearing | fog.md |
| Cow | `PlayerControl` (WASD), Rigidbody2D, BoxCollider2D, sprite, Shadow child | players.md |
| Crow | `PlayerControl` (arrows), Rigidbody2D, sprite, Shadow and FlyShadow children | players.md |

New objects go in the matching group above, and the doc for that group gets a line.

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
