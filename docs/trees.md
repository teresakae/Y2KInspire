# Trees and fruit

Two kinds of tree: fruit trees (below) and wall trees (last part of this doc).

## What it is

Fruit trees stand in the forest. When the cow charges into one with a strong enough ram, the canopy shakes and a fruit drops nearby. The crow eats it by flying over it, which refills its energy.

## Where it lives

- Scripts: `Assets/Scripts/FruitTree.cs` (on each tree), `Assets/Scripts/Fruit.cs` (on the fruit prefab). The ram itself is `CowRam.cs`, see `players.md`.
- Prefab: `Assets/Prefabs/PF_Fruit`.
- Scene: `Tree` in `Assets/Scenes/Level1.unity`, at (21.6, -1.2), in clearing 2. Trunk sprite `cow and crow trees_4` (order 5, under the fog); child `Shadow` (soft blob, order -50, see "Tree shadow"); child `Canopy`, sprite `cow and crow trees_1` (order 17), local position (0.06, 2.6) so it sits on the trunk; the canopy has three children `HangingFruit1` to `HangingFruit3` (sprite `fruit`, order 18). They shake with the canopy.

## How it works

1. The cow dashes into the tree's collider. `CowRam` checks its charge and calls `FruitTree.Hit(charge)`.
2. `Hit()` is ignored if `Cooldown` hasn't passed since the last hit (stops one dash dropping many). Otherwise it starts a canopy shake. Each counting ram hides the next active hanging fruit and spawns `Fruit Prefab` at that exact spot. The fruit falls straight down (`Fruit > Gravity` 30) to `Drop Below` under the tree's pivot (the trunk centre, so in front of the trunk's base), bounces once (`Fruit > Bounce` 0.15), then lands. While falling it draws at `Fruit > Falling Order` 20 (over the canopy) with its collider off, so the crow can't eat it mid-air; on landing it goes back to its prefab order (13), turns the collider on and swaps to `Fruit > Landed Sprite` (`fruit_cut`), so a whole fruit falls and a split one lies on the ground. When all are hidden the tree only shakes and drops nothing. A hidden hanging fruit grows back on the canopy `Regrow Time` seconds after it was picked (`FruitTree.Update` checks a due time per fruit).
2b. Sounds: every counted ram plays `Hit Clip`; `Shake Clip` plays only when a fruit actually drops. `Fruit` plays `Drop Clip` when the falling fruit lands. Each script uses an AudioSource found in `Awake` (added with Play On Awake off if missing); empty clip = silent.
3. Every counting ram also plays `Leaf Burst` if it is set, even on an empty tree: 4 to 6 leaves (`Assets/Prefabs/PF_LeafBurst`, one child `LeafBurst` per fruit tree, placed at the canopy centre) appear anywhere inside the canopy and fall, fluttering and spinning, fading over 1.8 to 2.6 s. Half are drawn in front of the canopy (`LeafBurst`, order 18), half behind it (child `Back`, order 16, so they only show once they drop below the canopy edge). See `particles.md`.
4. The shake moves `Shake Target` sideways with a sine wave for `Shake Time` seconds, then puts it back. With no target set it shakes the whole tree.
3b. If `Fruit > Lifetime` is above 0, a timer starts once the fruit is on the ground (after landing if it fell, from the first frame if placed on the ground). Over the last `Fade Time` seconds its sprite alpha drops to 0, then it is destroyed. 0 = stays forever.
4. The crow touches the fruit's trigger: `Fruit` calls `CrowEnergy.Eat(Energy)` and destroys itself. The cow's touch is ignored.

## Adding a tree

1. Create an empty object `Tree` in the scene.
2. Add the trunk as a SpriteRenderer on `Tree`. Add a Box Collider 2D covering the trunk base only, so the cow can walk behind the canopy.
3. Add a child `Canopy` with the canopy sprite (order 17, above the trunk's 5 and the animals). No collider. Add the `Shadow` child too (see "Tree shadow").
4. Add `FruitTree` to `Tree`. Set `Fruit Tree > Fruit Prefab` to `Assets/Prefabs/PF_Fruit`.
5. Drag `Canopy` into `Fruit Tree > Shake Target`.
   Add sprite children on `Canopy` (order 18) and drag them into `Fruit Tree > Hanging Fruit`; the tree drops that many fruit at a time.
6. Select the tree to see the drop radius as a wire circle in the Scene view.

## Making the fruit prefab

1. Create an empty object `PF_Fruit` with a SpriteRenderer. Until there is art, use a flat circle sprite tinted plum #7B3F6B.
2. Add a Circle Collider 2D and tick `Is Trigger`.
3. Add the `Fruit` script.
4. Drag it into `Assets/Prefabs` to make the prefab, then delete it from the scene.

## Tuning

| What | Object | Field | Current value |
|---|---|---|---|
| Fruit prefab | Tree | FruitTree > Fruit Prefab | PF_Fruit |
| Hanging fruit (drops available) | Tree | FruitTree > Hanging Fruit | HangingFruit1, 2, 3 (3 drops; every fruit tree needs this list) |
| Fruit fall speed-up | PF_Fruit | Fruit > Gravity | 30 |
| Fruit bounce height | PF_Fruit | Fruit > Bounce | 0.15 (0 = no bounce) |
| Fruit order while falling | PF_Fruit | Fruit > Falling Order | 20 |
| Sprite after landing | PF_Fruit | Fruit > Landed Sprite | fruit_cut (empty = stays whole) |
| Drop distance below trunk centre | Tree | FruitTree > Drop Below | 2 (default; trunk `_4` is about 3.1 tall, so its base is 1.55 below) |
| Dropped fruit look | PF_Fruit | Sprite Renderer > Sprite | `fruit_cut` (hanging fruit keep `fruit`) |
| Time between hits | Tree | FruitTree > Cooldown | 0.5 (default) |
| Runs when a fruit drops | Tree | FruitTree > On Drop | UnityEvent passing the fallen fruit's Transform. Level1 only wires it to the tutorial tips (`TutorialTip.Done`), see `tutorial.md` |
| Hanging fruit regrow | Tree | FruitTree > Regrow Time | 20 seconds (default, all trees including PF_Fruit and PF_FruitQuick ones; 0 = never) |
| Canopy to shake | Tree | FruitTree > Shake Target | Canopy |
| Leaves on a ram (empty = none) | Tree | FruitTree > Leaf Burst | the tree's `LeafBurst` child (all 5 fruit trees) |
| Trunk collider | Tree | BoxCollider2D > Size / Offset | (2.2, 1.7) / (0.03, 0.135), trunk only, top below the canopy's lower edge |
| Shake size, length, speed | Tree | FruitTree > Shake Amount / Shake Time / Shake Speed | 0.1 / 0.2 / 40 (defaults) |
| Energy per fruit | PF_Fruit | Fruit > Energy | 30 (default) |
| Seconds on ground before it fades | PF_Fruit | Fruit > Lifetime | 0 (default = stays forever) |
| Fade-out length (s) | PF_Fruit | Fruit > Fade Time | 1 (default) |
| Min charge to drop | Cow | CowRam > Min Charge | 0.5 (default) |
| Hit sound (every counted ram) | Tree | FruitTree > Hit Clip | `Tree_hit` |
| Shake sound (only when a fruit drops) | Tree | FruitTree > Shake Clip | `Tree_shake` |
| Fruit landing sound | PF_Fruit | Fruit > Drop Clip | `Tree_dropfruit` |
| Sway (trunk / canopy / hanging fruit) | materials | `M_TreeTrunk`, `M_TreeCanopy`, `M_TreeFruit` > Sway amount / Base sway / Wind lean / Wave scale / Wave speed / Flutter | trunk 0.024 / 0 / 0.12; canopy 0.06 / 0.4 / 0.3; fruit 0.03 / 1 / 0.15; shared 0.6 / 1.0 / 0.15 |

## Gotchas

- The cow calls `Hit()`; the tree never checks for the cow. This avoids collision-callback order problems.
- The tree's collider must be on the same object as `FruitTree` (the root), because `CowRam` looks for the component on the thing it hit.
- Put the collider on the trunk base only. A canopy collider stops the cow far from the trunk and closes paths: a 4.59-tall box left a 0.5 gap under Level2's top wall, and the cow needs 0.8 (fixed 27 Nov 2026).
- The crow needs a trigger Circle Collider 2D, or fruit never gets eaten.
- Trunks (5) draw under the fog (10), so a trunk in fog is hidden and only its canopy shows. Canopies (17) draw above the animals (15, 16), so the crow is hidden when it flies over a canopy. Hanging fruit must stay above the canopy (18).
- If `Fruit Prefab` is empty, the tree logs an error and drops nothing, but still shakes.

## Not built yet

- Regrow is per fruit: ramming a tree twice hides two, and each comes back on its own timer. The dropped fruit on the ground is separate (`Fruit > Lifetime`).
- Fruit never rots or despawns.

## Sway

All trees (wall trees and the fruit tree) sway in the same wind as the grass (`Fog Grid > Wind`, global `_FogWind`), much less than the grass. Shader `Assets/Shaders/TreeSway.shader` (`Cowncrow/TreeSway`): lit like the default sprite, draws the sprite's own colours, bends the vertices. The Settings Motion slider scales the sway too (global `_ReduceMotion`, see menu.md).

- `M_TreeTrunk`: tiny sway, planted at the bottom (`Base sway` 0).
- `M_TreeCanopy`: more sway, `Base sway` 0.4, so the canopy's bottom already moves as far as the trunk's top (`Sway amount` 0.06 x 0.4 = 0.024 = trunk's 0.024 x 1). Keep that equation if you retune: canopy `Sway amount` x `Base sway` = trunk `Sway amount`, and `Wind lean` in the same ratio, or the two separate at the join.
- `M_TreeFruit`: the hanging fruit (children of Canopy, own pivot, so `Base sway` 1 = the whole sprite moves evenly, about what the canopy moves at that height).
- Sprites are centre-pivot, so each material has `Bottom Y` (minus half the sprite's height) and `Sway height` (the sprite's height). Defaults match trunk `_2`/`_4` (-1.5 / 3) and canopy `_0` (-2.2 / 4.4). Other canopy sprites bend a little differently, fine.
- Phase comes from the tree's world X only (smooth), so trunk and canopy stay in step and a gust rolls from tree to tree along X.
- Wall trees: `Trunk` and `Canopy` of `WallTree` use the materials. The fruit tree `Tree` uses them in the scene.
- Wall trees have no `FruitTree`: ramming one gives only the cow's squash/hop and a tiny camera bump (see `players.md`).
- Sway only moves in Play. Not seen moving in the headless run (editor time did not tick); the join was checked by pushing the time offset and sway x6 (stayed joined).

# Wall trees

## What it is

Wall trees are the forest edge: decoration that is also a wall. They block the cow. They are meant to block the crow too, but the crow has no solid collider yet (see `level.md`). Each one picks a random trunk, canopy, canopy height and mirror, so a row of them does not look copy-pasted.

## Where it lives

- Script: `Assets/Scripts/TreeLook.cs`, on the root of the prefab.
- Art: tile sheet `Assets/Art/Tree/cow and crow trees.png`, sliced into 6 sprites. Canopies: `cow and crow trees_0` (light green), `_1` (dark olive), `_5` (bumpy). Trunks: `_2`, `_3` (thin), `_4` (with hole). Planned sheet `Pixels Per Unit`: 130, to match the old tree art.
- Prefab: `Assets/Prefabs/WallTree.prefab` (named without the `PF_` prefix). Root `WallTree` with a Circle Collider 2D and `TreeLook`; child `Trunk`, child `Canopy` and child `Shadow`, each a SpriteRenderer. `TreeLook` does not touch `Shadow`.
- Scene: all wall trees are under `Walls` in Level1 (see `level.md`).
- The old `tree_canopy.png` and `tree_trunk.png` are no longer used. Not deleted.

## How it works

1. At `Awake`, `TreeLook` seeds Unity's random from the tree's world position, so a tree looks the same every Play.
2. It picks a trunk from `Trunks` and a canopy from `Canopies`, sets the canopy's local Y between the two values of `Canopy Height`, and mirrors the trunk and canopy independently.
3. It restores the previous random state, so other scripts (`FogGrid`) are not disturbed.
4. The collider on the root is the wall. `TreeLook` never touches it.

`CanopyCovers(Vector2 p, float margin)` (public, on `TreeLook` and on `FruitTree`): true when p is inside the ellipse of the canopy sprite's bounds grown by `margin` (`FruitTree` uses its `Shake Target`'s SpriteRenderer, cached in Start). `Snake` uses it to keep snakes out from under leaves, passing its `Body Radius` as margin. Note `TreeLook` moves the canopy in Awake, so the answer is only right in Play.

## Adding a wall tree

1. Drag `Assets/Prefabs/WallTree` into Level1 under `Walls`.
2. Place it so its collider circle overlaps its neighbours' circles. A gap lets the cow through.
3. Press Play to see its look. The Scene view shows the prefab's default sprites.

## Making the wall tree prefab

1. Set the sheet's `Pixels Per Unit` to 130 and check the 6 slices in the Sprite Editor.
2. Create an empty `WallTree`. Add a Circle Collider 2D: radius about the canopy's half-width, `Offset` Y raised to cover the trunk and lower canopy.
3. Add child `Trunk` (SpriteRenderer, `Order in Layer` 5) and child `Canopy` (SpriteRenderer, `Order in Layer` 17). The trunk is under the fog (10), so fog hides trunk bottoms; the canopy is above it, so tree tops stay readable in the mist. Add child `Shadow` (see "Tree shadow").
4. Add `TreeLook` to the root. Drag `Trunk` and `Canopy` into its fields. Fill `Trunks` with `_2`, `_3`, `_4` and `Canopies` with `_0`, `_1`, `_5`.
5. Look at `Canopy > Transform > Position Y` and set `Canopy Height` to a range around it.
6. Drag the object into `Assets/Prefabs` to make the prefab.

## Fruit tree with sheet art

The fruit tree `Tree` uses the same sheet: trunk `_4` (order 5), a `Shadow` child, `Canopy` `_1` (order 17, local position (0.06, 2.6)), and three `fruit` sprite children on `Canopy` (order 18) that shake with it. The hanging fruit is real: it limits the drops, see "How it works".

## Tuning

| What | Object | Field | Current value |
|---|---|---|---|
| Trunk / Canopy renderers | WallTree prefab root | TreeLook > Trunk / Canopy | the two children |
| Trunk sprites | WallTree prefab root | TreeLook > Trunks | `_2`, `_3`, `_2`, `_3`, `_4` (a sprite listed twice is picked twice as often, so `_4`, the trunk with the hole, shows on about 1 tree in 5) |
| Canopy sprites | WallTree prefab root | TreeLook > Canopies | `_0`, `_1`, `_5` |
| Canopy height | WallTree prefab root | TreeLook > Canopy Height | 2 to 3.2 (lower lets the canopy hide its trunk) |
| Wall shape | WallTree prefab root | CircleCollider2D > Radius / Offset | 2.7 / (0, 2.6) |
| Draw order | Trunk / Canopy | SpriteRenderer > Order in Layer | 5 / 17 (fog 10 covers trunks; characters 15 and 16 sit in front of trunks and behind canopies) |
| Shadow | Shadow | Transform > Position / Scale, SpriteRenderer > Color / Order in Layer | (0, -1.35) / (5.5, 1.5); dark olive (0.23, 0.30, 0.11) alpha 0.4; -50 |

## Tree shadow

Every tree has a child `Shadow`: SpriteRenderer with `Assets/Sprites/SPR_ShadowBlob.png` (128 x 128 white, soft round falloff, PPU 128), colour dark olive (0.23, 0.30, 0.11) alpha 0.4 (green, so it reads as shade on the grass, not a plum smudge), `Order in Layer` -50 (above the ground, below fog, trunk and animals). It is a wide, soft pool of shade under the roots, about as wide as the canopy, so the tree looks planted in the grass. Wall trees: local position (0, -1.35), scale (5.5, 1.5), set in the prefab (trunk `_2` bottom is at -1.47). Fruit tree `Tree`: local position (0, -1.4), scale (5.5, 1.5) (trunk `_4` bottom is at -1.55), set in each scene. Because the trunk is under the fog, a shadow in fog is hidden too. The animals' own shadows (`Cow/Shadow`, `Crow/Shadow`, `Crow/FlyShadow`) are at order 14.

## Gotchas

- Neighbouring colliders must overlap, or the cow slips through.
- The look is picked at Play, so the Scene view shows default sprites.
- Moving a tree changes its look, because the seed is its position.
- If `Trunk` or `Canopy` is empty, `TreeLook` logs an error and does nothing.
- If `TreeLook` ever goes on a fruit tree, it still works: `Awake` runs before `FruitTree.Start` caches the shake position.
