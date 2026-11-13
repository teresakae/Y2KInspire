# cowsncrows

## 1. The game

Cowsncrows is a local two-player co-op game on one shared screen. The cow is a calf lost from its herd in a foggy forest. It walks and rams things but cannot enter fog. The crow flies, clears fog, and eats fruit to keep its energy up. Moos from the herd guide the players home. Tone: "Warm, chunky sticker critters in a hushed, foggy forest. Cozy with a quiet unease; not horror, not sugary-cute." Made for Gimersia Inspire 2026 (checkpoint page Thu 19 Nov 2026, submission Sun 29 Nov 2026 23:59 WIB). Scenes: a start menu, LV_01, LV_02. Full design: `docs/design.md`.

Who does what: the owner does all code and all Unity editor work. The artist delivers art only and never touches the repo; art reaches Unity through the owner.

## 2. Every session

1. Read `docs/README.md`, then the doc for every system the task touches.
2. Check the real editor state through unity-mcp (open scene, hierarchy, console). Never assume earlier work exists.
3. Do what was asked. For anything beyond it, propose and wait for `go`.

## 3. Agent routing

- The main agent (Opus) plans, discusses with the owner, and synthesizes.
- Sonnet subagents do all reading and writing of files, codebase searches, and research lookups. The main agent briefs them with complete context and reviews what comes back.
- Haiku only for trivial mechanical edits where the diff is fully specified.

## 4. Rules

1. Caveman (terse prose) and ponytail (laziest working code) modes are on for all code work, always. Reason: the owner has to read every line.
2. Beginner-level code: the simplest, most basic C# that works. Plain MonoBehaviours. No managers, interfaces, event buses, generics, singletons, LINQ, coroutine tricks, async, or editor tools unless the owner asks. Reason: the owner must read and tune it.
3. The owner does most things in the editor. Code exposes fields; Claude tells the owner what to set up in the Inspector or scene, with exact paths (`Object > Component > Field`), instead of building scene setup in code or doing it unasked through the bridge. Scene and prefab edits through the bridge only when the owner asks.
4. Engine built-ins first: Rigidbody2D/Collider2D, Input System, uGUI + TextMeshPro, ScriptableObjects (only for shared tuning once a second use exists), URP 2D Lights, FMOD for all audio. Cinemachine is not installed: ask before using it.
5. Tunables live in the Inspector: `[SerializeField] private` + `[Tooltip]` + `[Range]` where it fits. No magic numbers. Existing scripts use public fields: convert a script when you next touch it, not in a cleanup pass.
6. Docs before, docs after: read the system doc first, update it in the same task. The code is the truth; if doc and code disagree, fix the doc.
7. Shared assets: Project Settings, `Assets/Settings/InputSystem_Actions.inputactions`, `Assets/Settings/UniversalRP.asset` and `Renderer2D.asset`, FMOD settings (`Assets/Plugins/FMOD/Resources`), and the future core prefabs (Cow, Crow). Say so before changing any of them. Test scenes get their own copy.
8. Artist art arrives through the owner into `Assets/Art`. Don't rename or move art without asking. One person edits a scene at a time (scenes merge badly).
9. Ask first before adding a package, a top-level folder, or a new system. Reason: each one is something the owner has to learn and keep.
10. Experiments go in `Assets/Sandbox`. Nothing in the game references it.
11. UI is uGUI + TMP, built as prefabs. Layout comes from Layout Groups and anchors, not code. Hidden UI is inactive in the prefab.
12. WebGL is the main target: no threads, no `System.IO` file access. Test a WebGL build at least weekly and before the 19 Nov checkpoint. Reason: FogGrid and FMOD are the likely trouble spots.
13. The deadline beats polish of unconfirmed features. Work in slice order: core loop, moo, menus/win/lose, snakes, juice.

## 5. Code style

- No namespace while the project has fewer than about 15 scripts.
- One job per component. One summary comment per class.
- Comments say why, not what.
- Cache `GetComponent` results in `Awake` or `Start`.
- Validate Inspector references and log with the object name: `Debug.LogError($"{name}: missing X");`.

Unity 6 / Input System 1.20 API (use these, not the old names):

- `Rigidbody2D.linearVelocity`, not `velocity`.
- `linearDamping` / `angularDamping`, not `drag` / `angularDrag`.
- `FindFirstObjectByType` / `FindObjectsByType`, not `FindObjectOfType`.
- New Input System only: `Keyboard.current` or an `InputAction`. Never `UnityEngine.Input`.
- TextMeshPro: `using TMPro;` (comes with the uGUI package).
- Check the installed package version in `Packages/manifest.json` before using its API.
- Add lines here as we hit them.

## 6. How changes are made

- Code, shaders, docs: file tools (Read/Edit/Write).
- Scenes, prefabs, materials, project settings: unity-mcp. Never hand-edit `.unity`, `.prefab` or `.asset` YAML.
- Move or rename assets only inside Unity, so `.meta` GUIDs survive.
- After a script change, wait for the compile, read the console, and fix every error and warning you caused.
- No lasting changes in Play mode.
- Check that an object or asset exists before creating it.
- Save the scene after a batch of scene edits.
- Smoke-test through the bridge where possible. For feel and timing, give the owner manual test steps.
- Failure protocol: retry once. If it fails again, stop and report the call, its arguments, the error, and one or two options. If a fix fails twice, stop; do not stack workarounds.
- Every report names the menu or Inspector path involved.
- Git: never commit, push, or touch history. End each task with suggested git commands and a Conventional Commits message. The owner commits.

## 7. Editor bridge

unity-mcp (Unity's relay). Command, stdio:

```
/Users/kaes/.unity/relay/relay_mac_arm64.app/Contents/MacOS/relay_mac_arm64 --mcp --project-path /Users/kaes/Documents/Personal/Unity/cowncrow
```

Currently configured only for another project (Project29). It must be added for this project before the bridge works here.

Tools: (fill in from the tool list on first connect)

FMOD MCP: not set up yet.

Gotchas: (empty). Add a line every time a call fails for a non-obvious reason.

## 8. Project map

Versions: Unity 6000.6.3f1; URP 17.6.0 (2D Renderer); Input System 1.20.0; uGUI 2.6.0 (+TMP); Timeline 6.6.0; 2D Animation 16.0.1; 2D Aseprite 6.0.0; 2D PSD Importer 15.0.1; 2D Tilemap Extras 9.0.1; 2D SpriteShape 16.0.0; FMOD for Unity 2.03.15.

```
Assets/
  Scenes/      SampleScene.unity = only scene, the prototype
  Scripts/     FogGrid.cs, PlayerControl.cs
  Sprites/     Cow/ and Crow/ (4-direction pngs; crow also fly.png)
  Settings/    URP assets, InputSystem_Actions.inputactions, scene templates
  Plugins/     FMOD/
  Welcome/     Unity 2D template leftover, unused
  Art/         create when first needed: artist source art
  Prefabs/     create when first needed
  Audio/       create when first needed: FMOD banks
  Sandbox/     create when first needed: experiments, nothing references it
docs/          one doc per system, see docs/README.md
```

New assets go in the folder for their type. Level scenes go in `Assets/Scenes`.

Placeholder policy: if the art isn't here yet, use engine primitives or flat sprites in palette colours. Never fake drawings.

Naming: PascalCase for assets and scripts. Prefixes: `M_` material, `T_` texture, `SPR_` sprite, `PF_` prefab, `SO_` ScriptableObject, `SFX` / `MUS` for FMOD events, `LV_` level scenes (e.g. `LV_01`).

Palette (approximate, from the artist's sheet):

| Use | Hex |
|---|---|
| Outline, dark plum | #2E1A2B |
| Cow brown, tree trunk | #B87A68 |
| Ear peach | #F4C7A8 |
| Snout, flower pink | #E6A6C7 |
| Flower centre gold | #E8B23A |
| Tree olive green | #6F7D35 |
| Snake moss brown | #7A6530 |
| Fruit plum | #7B3F6B |
| Crow plum-black | #3A1F33 |
