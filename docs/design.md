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

- Fog: covers the level. The crow clears it. Fog blocks the cow. See `fog.md`.
- Crow energy: drains while flying. At zero the crow is forced to land, and stays down (snake-vulnerable) until it eats fruit.
- Trees and fruit: fruit hangs in trees. The cow rams a tree, fruit falls, the crow eats it.
- Moo guidance: the herd moos. FMOD positional audio, so the sound comes from the direction to go.
- Snakes: hidden in fog, revealed when the crow clears it. They roam freely, warn with sfx/vfx, and eat the crow if it is on the ground. The cow kills them by ramming.
- Level flow: win by reaching the herd (or the next stretch of path toward it). Lose when a snake eats the crow; the level restarts.
- UI: start menu, restart, feedback for energy and win/lose.
- Audio: all through FMOD.

## Structure and scope

Vertical slice:
- Start menu.
- LV_01: teaches the loop (fog, energy, ram tree, fruit, moo).
- LV_02: adds snakes.

One scene per level. Snakes are the cut line if time runs short.

Later / maybe:
- Flies: bug the cow when it is idle without the crow. The crow sits on the cow to eat them.
- Nicer, natural-looking fog.
- Crow spreads seeds that sprout.

Jam minimum: clear win and lose, a defined obstacle, a repeating core loop, basic UI (start, restart, feedback), art and audio, playable start to end.

Scoring weights at playtest curation: game design 40, visual 35, audio 15, implementation 10.

Builds: WebGL (main, for itch.io) and Windows (backup for the finals booth). Risks: FMOD on WebGL needs extra setup. FogGrid loops about 60k pixels and 4k cells every frame on the CPU and may be slow in WebGL.

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
| Fog clearing and blocking | Partial | Crow clears fog and the cow is blocked. Fog also grows back after 5 s, which the design doesn't say either way. See `fog.md`. |
| Two-player movement | Partial | Cow on WASD, crow on arrows, same keyboard. No gamepad. See `players.md`. |
| Crow energy and forced landing | Not yet | |
| Crow lands / eats fruit | Not yet | "Landed" is not a state. See `players.md`. |
| Trees and fruit | Not yet | |
| Cow rams trees | Not yet | |
| Moo guidance (FMOD) | Not yet | FMOD is installed, no events. |
| Snakes (hidden, warnings, kill, eat) | Not yet | |
| Cow rams snakes | Not yet | |
| Win condition | Not yet | |
| Lose and restart | Not yet | |
| Start menu | Not yet | |
| UI feedback | Not yet | |
| LV_01 | Not yet | Only the prototype scene exists. |
| LV_02 | Not yet | |
| Final art and juice | Not yet | Prototype sprites only. |
| WebGL build tested | Not yet | |
| Windows build | Not yet | |
