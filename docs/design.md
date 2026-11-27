# Design

## Pitch

Cowsncrows is a local co-op game for two players on one keyboard (gamepads later), sharing one camera. The cow is a calf lost from its herd in a foggy forest. It can't walk into fog. The crow flies and clears it. Neither gets home alone.

## Tone

"Warm, chunky sticker critters in a hushed, foggy forest. Cozy with a quiet unease; not horror, not sugary-cute."

Inspirations: Lamaa & Lizard for the co-op mechanics. TOEM and Tiny Sticker Tale for art style and juice.

## Setting

A forest the player can't see into. Dusty golden grass, green trees, a dirt path lined with yellow flowers. The herd is somewhere beyond the fog. Its moos tell the players where to go.

## Camera and art

- 2D, top-down, orthographic. One camera for both players.
- Sticker or paper-cutout sprites with thick dark-plum outlines.
- Squash-and-stretch juice on movement and impacts.
- URP 2D Lights for the fog mood.

Prototype colours (not artist-final): ground = `Main Camera > Camera > Background` dusty golden grass `#C2A55E`; fog = dusky plum-grey `#6B6170` (`Fog Grid > FogGrid > Fog Color`). Warm ground against cool plum fog keeps the two apart and stays away from the olive canopy.

Draw order (`SpriteRenderer > Order in Layer`, all on the Default sorting layer):

| Order | What |
|---|---|
| 30 | Crow energy bar (world-space `Canvas > Sort Order`) |
| 20 | Snake's "!" eaten mark |
| 18 | Hanging fruit on the fruit tree (above its canopy) |
| 17 | Tree canopies (wall trees and fruit tree) |
| 16 | Crow |
| 15 | Cow |
| 14 | Cow and crow shadows (`Shadow`, `FlyShadow`) |
| 13 | Dropped fruit (`PF_Fruit`) |
| 11 | Tree trunks (wall trees and fruit tree) |
| 10 | Fog (`Fog Grid > Sorting Order`) |
| 14 | Snake in Level2 (under the cow, over the ground; fog 19 hides it) |
| -5 | Snake in SampleScene (anything below the fog hides under it) |

Trees, fruit and characters draw above the fog. Characters draw above trunks but below canopies, so the crow is hidden when it flies over a canopy.

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

## Theme fit

Gimersia Inspire 2026 themes:

- Gotong royong (mutual help): the cow and crow each do what the other can't. The cow shakes fruit down for the crow. The crow opens the fog for the cow.
- Pulang (going home): a lost calf returns to the herd, guided by moos.

A third theme, "melestarikan", exists. We don't need to fit it.

## Core mechanic

Crow:
- Fly around and clear fog.
- Land.
- Eat fruit to refill energy.

Cow:
- Walk (not into fog).
- Ram trees: the tree shakes, stays standing, fruit falls.
- Ram snakes: kills them.

Both:
- Follow the moo toward the herd.

## Systems

- Fog: covers the level. The crow clears it. Fog blocks the cow. Fog regrowth (cleared fog grows back) is confirmed by the owner; keep it, the current speed is fine. See `fog.md`.
- Crow energy: drains while flying. At zero the crow is forced to land, and stays down (snake-vulnerable) until it eats fruit.
- Trees and fruit: fruit hangs in trees. The cow rams a tree, fruit falls, the crow eats it.
- Moo guidance: the herd moos. Panned audio (an AudioSource), so the sound comes from the direction to go.
- Snakes: hidden in fog, revealed when the crow clears it. They roam freely, warn with sfx/vfx, and eat the crow if it is on the ground. The cow kills them by ramming.
- Level flow: win by reaching the herd (or the next stretch of path toward it). Lose when a snake eats the crow; the level restarts.
- UI: start menu, restart, feedback for energy and win/lose.
- Audio: Unity's built-in audio (AudioSource, AudioClip). FMOD was removed on 24 Nov 2026, unused.

## Structure and scope

Vertical slice:
- Start menu.
- Level1: teaches the loop (fog, energy, ram tree, fruit, moo).
- Level2: adds snakes.

One scene per level. Snakes are the cut line if time runs short.

Later / maybe:
- Flies: bug the cow when it is idle without the crow. The crow sits on the cow to eat them.
- Nicer, natural-looking fog.
- Crow spreads seeds that sprout.

Jam minimum: clear win and lose, a defined obstacle, a repeating core loop, basic UI (start, restart, feedback), art and audio, playable start to end.

Scoring weights at playtest curation: game design 40, visual 35, audio 15, implementation 10.

Builds: WebGL (main, for itch.io) and Windows (backup for the finals booth). Risks: FogGrid loops about 168k pixels and 10.5k cells every frame in Level1 on the CPU and may be slow in WebGL.

## Jam deliverables

Gimersia Inspire 2026 (Indonesia).

- [ ] Thu 19 Nov 2026: itch.io checkpoint page with a description and a current screenshot or GIF.
- [ ] Sun 29 Nov 2026, 23:59 WIB: submission, with:
  - [ ] itch.io link with a WebGL and/or Windows build
  - [ ] gameplay video, 60 seconds to 2 minutes
  - [ ] description and how to play
  - [ ] at least 3 screenshots
  - [ ] AI-use disclosure (what for, which tools)
  - [ ] third-party asset attribution
- [ ] Backup build for the finals booth.

## Build status

| Design item | Status | Notes |
|---|---|---|
| Fog clearing and blocking | Partial | Crow clears fog and the cow is blocked. Fog also grows back, which the owner confirmed is wanted. See `fog.md`. |
| Two-player movement | Partial | Cow on WASD, crow on arrows, same keyboard. No gamepad. See `players.md`. |
| Crow energy and forced landing | Partial | Energy drains while moving, bar, slow hop at zero. No landing state. Not wired in the scene yet. See `players.md`. |
| Crow lands / eats fruit | Partial | Eats by flying over the fruit; "landed" is not a state. Not wired in the scene yet. See `players.md`. |
| Trees and fruit | Partial | `FruitTree` and `Fruit` scripts exist. Not wired in the scene yet. See `trees.md`. |
| Cow rams trees | Partial | Charged ram on Space (`CowRam`). Not wired in the scene yet. See `players.md`. |
| Moo guidance (audio) | Placeholder: speech bubble on a timer (`MooPrompt`, in `PF_HUD`). Moo sound plays on each bubble, panned toward the goal (`Cow_mooHerd`). | Player moo clips `Cow_mooPlayer1/2` unused. See `level.md`. |
| Music, ambience, sfx | Menu and level music, level ambience, button, tree, fruit, eat, snake hiss, tip and exit sounds plus moo, crow drown, snake hit and attack, cow and crow move loops. All clips assigned (see `level.md` Audio). Menu music carries through MainMenu, Intro, HomeScreen (`MenuMusic`). | Missing: finish-area bells (clip not delivered). |
| Snakes (hidden, warnings, kill, eat) | Partial | `Snake` script: roams, coiled warning, eats a grounded crow. Hiss sound, no vfx. Not wired in the scene yet. See `snakes.md`. |
| Cow rams snakes | Partial | Strong dash kills a snake (`CowRam.IsStrongDash`). Not wired in the scene yet. See `snakes.md`. |
| Win condition | Placeholder: exit fades to white and restarts Level1 once both animals are in it (`LevelExit`). | See `level.md`. |
| Lose and restart | Partial | Snake bite reloads the scene, no lose screen. Not wired in the scene yet. |
| Start menu | Built (`MainMenu`: Start begins a fresh game, Settings, Credits). | See `menu.md`. |
| Pause | Built: Esc or P, settings panel, Back to resume, Save and quit to the menu. | See `menu.md`. |
| Tutorial | Built: wobbly key drawings and signs in both levels. Unplayed by testers. | See `tutorial.md`. |
| UI feedback | Not yet | |
| Level1 | Partial — scene built (camera, fog, clearings, tree walls, fruit tree, exit, HUD). See `level.md`. | Moo text not placed; crow still flies through tree walls. |
| Level2 | Built, untuned: 3x3 screens, river, pond, 12 snakes, 5 fruit trees. See `level.md` "Level2". | Needs a hand-played pass for balance. |
| Home screen | Built: ending cutscene (white, text, "The End." picture, Back to menu; `HomeScreen`). See `menu.md`. | |
| Final art and juice | Not yet | Prototype sprites only. |
| WebGL build tested | Partial | Built and uploaded to itch.io 18 Nov 2026. Loads in the browser and takes keyboard input; full level not played through there yet. See `README.md` Build (WebGL). |
| Windows build | Not yet | |
