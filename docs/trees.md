# Trees and fruit

Two kinds of tree: fruit trees (below) and wall trees (last part of this doc).

## What it is

Fruit trees stand in the forest. When the cow charges into one with a strong enough ram, the canopy shakes and a fruit drops nearby. The crow eats it by flying over it, which refills its energy.

## Where it lives

- Scripts: `Assets/Scripts/FruitTree.cs` (on each tree), `Assets/Scripts/Fruit.cs` (on the fruit prefab). The ram itself is `CowRam.cs`, see `players.md`.
- Prefab: `Assets/Prefabs/PF_Fruit`.
- Scene: `Tree` in `Assets/Level1.unity`, at (21.6, -1.2), in clearing 2. Trunk sprite `cow and crow trees_4` (order 11); child `Canopy`, sprite `cow and crow trees_1` (order 12), local position (0.06, 2.6) so it sits on the trunk; the canopy has three children `HangingFruit1` to `HangingFruit3` (sprite `fruit`, order 13). They shake with the canopy.

## How it works

1. The cow dashes into the tree's collider. `CowRam` checks its charge and calls `FruitTree.Hit()`.
2. `Hit()` is ignored if `Cooldown` hasn't passed since the last hit (stops one dash dropping many). Otherwise it starts a shake. If `Hanging Fruit` is empty it also spawns `Fruit Prefab` on the ground: `Drop Below` under the tree's pivot (the trunk centre, so below the trunk's base), at a random sideways spot within `Drop Radius` (unlimited drops). It never lands on the trunk or canopy. If `Hanging Fruit` has entries, each counting ram hides the next active hanging fruit and drops one real `Fruit Prefab` the same way. When all are hidden the tree only shakes and drops nothing. Hanging fruit never regrows.
3. The shake moves `Shake Target` sideways with a sine wave for `Shake Time` seconds, then puts it back. With no target set it shakes the whole tree.
4. The crow touches the fruit's trigger: `Fruit` calls `CrowEnergy.Eat(Energy)` and destroys itself. The cow's touch is ignored.

## Adding a tree

1. Create an empty object `Tree` in the scene.
2. Add the trunk as a SpriteRenderer on `Tree`. Add a Box Collider 2D covering the trunk base only, so the cow can walk behind the canopy.
3. Add a child `Canopy` with the canopy sprite (order 12, above the trunk's 11). No collider.
4. Add `FruitTree` to `Tree`. Set `Fruit Tree > Fruit Prefab` to `Assets/Prefabs/PF_Fruit`.
5. Drag `Canopy` into `Fruit Tree > Shake Target`.
   Optional: add sprite children on `Canopy` (order 13) and drag them into `Fruit Tree > Hanging Fruit` to limit the drops to that many.
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
| Hanging fruit (drops available) | Tree | FruitTree > Hanging Fruit | HangingFruit1, 2, 3 (3 drops; empty = unlimited) |
| Drop sideways spread | Tree | FruitTree > Drop Radius | 1.2 (default) |
| Drop distance below trunk centre | Tree | FruitTree > Drop Below | 2 (default; trunk `_4` is about 3.1 tall, so its base is 1.55 below) |
| Dropped fruit look | PF_Fruit | Sprite Renderer > Sprite | `fruit_cut` (hanging fruit keep `fruit`) |
| Time between hits | Tree | FruitTree > Cooldown | 0.5 (default) |
| Canopy to shake | Tree | FruitTree > Shake Target | Canopy |
| Trunk collider | Tree | BoxCollider2D > Size / Offset | (2.2, 0.6) / (0.03, 0.3) |
| Shake size, length, speed | Tree | FruitTree > Shake Amount / Shake Time / Shake Speed | 0.1 / 0.2 / 40 (defaults) |
| Energy per fruit | PF_Fruit | Fruit > Energy | 30 (default) |
| Min charge to drop | Cow | CowRam > Min Charge | 0.5 (default) |

## Gotchas

- The cow calls `Hit()`; the tree never checks for the cow. This avoids collision-callback order problems.
- The tree's collider must be on the same object as `FruitTree` (the root), because `CowRam` looks for the component on the thing it hit.
- Put the collider on the trunk base only. A canopy collider would stop the cow far from the trunk.
- The crow needs a trigger Circle Collider 2D, or fruit never gets eaten.
- If `Fruit Prefab` is empty, the tree logs an error and drops nothing, but still shakes.

## Not built yet

- Hanging fruit never regrows. A tree with `Hanging Fruit` set gives only that many fruit, ever. Option: re-show them on a timer.
- Fruit never rots or despawns.

# Wall trees

## What it is

Wall trees are the forest edge: decoration that is also a wall. They block the cow. They are meant to block the crow too, but the crow has no solid collider yet (see `level.md`). Each one picks a random trunk, canopy, canopy height and mirror, so a row of them does not look copy-pasted.

## Where it lives

- Script: `Assets/Scripts/TreeLook.cs`, on the root of the prefab.
- Art: tile sheet `Assets/Art/Tree/cow and crow trees.png`, sliced into 6 sprites. Canopies: `cow and crow trees_0` (light green), `_1` (dark olive), `_5` (bumpy). Trunks: `_2`, `_3` (thin), `_4` (with hole). Planned sheet `Pixels Per Unit`: 130, to match the old tree art.
- Prefab: `Assets/Prefabs/WallTree.prefab` (named without the `PF_` prefix). Root `WallTree` with a Circle Collider 2D and `TreeLook`; child `Trunk` and child `Canopy`, each a SpriteRenderer.
- Scene: all wall trees are under `Walls` in Level1 (see `level.md`).
- The old `tree_canopy.png` and `tree_trunk.png` are no longer used. Not deleted.

## How it works

1. At `Awake`, `TreeLook` seeds Unity's random from the tree's world position, so a tree looks the same every Play.
2. It picks a trunk from `Trunks` and a canopy from `Canopies`, sets the canopy's local Y between the two values of `Canopy Height`, and (if `Random Flip`) mirrors the trunk and canopy independently.
3. It restores the previous random state, so other scripts (`FogGrid`) are not disturbed.
4. The collider on the root is the wall. `TreeLook` never touches it.

## Adding a wall tree

1. Drag `Assets/Prefabs/WallTree` into Level1 under `Walls`.
2. Place it so its collider circle overlaps its neighbours' circles. A gap lets the cow through.
3. Press Play to see its look. The Scene view shows the prefab's default sprites.

## Making the wall tree prefab

1. Set the sheet's `Pixels Per Unit` to 130 and check the 6 slices in the Sprite Editor.
2. Create an empty `WallTree`. Add a Circle Collider 2D: radius about the canopy's half-width, `Offset` Y raised to cover the trunk and lower canopy.
3. Add child `Trunk` (SpriteRenderer, `Order in Layer` 11) and child `Canopy` (SpriteRenderer, `Order in Layer` 12). Both are above the fog (10), so walls read as trees in the mist. Use lower numbers than 10 to hide them in fog.
4. Add `TreeLook` to the root. Drag `Trunk` and `Canopy` into its fields. Fill `Trunks` with `_2`, `_3`, `_4` and `Canopies` with `_0`, `_1`, `_5`.
5. Look at `Canopy > Transform > Position Y` and set `Canopy Height` to a range around it.
6. Drag the object into `Assets/Prefabs` to make the prefab.

## Fruit tree with sheet art

The fruit tree `Tree` uses the same sheet: trunk `_4` (order 11), `Canopy` `_1` (order 12, local position (0.06, 2.6)), and three `fruit` sprite children on `Canopy` (order 13) that shake with it. The hanging fruit is real: it limits the drops, see "How it works".

## Tuning

| What | Object | Field | Current value |
|---|---|---|---|
| Trunk / Canopy renderers | WallTree prefab root | TreeLook > Trunk / Canopy | the two children |
| Trunk sprites | WallTree prefab root | TreeLook > Trunks | `_2`, `_3`, `_2`, `_3`, `_4` (a sprite listed twice is picked twice as often, so `_4`, the trunk with the hole, shows on about 1 tree in 5) |
| Canopy sprites | WallTree prefab root | TreeLook > Canopies | `_0`, `_1`, `_5` |
| Canopy height | WallTree prefab root | TreeLook > Canopy Height | 2 to 3.2 (lower lets the canopy hide its trunk) |
| Mirror at random | WallTree prefab root | TreeLook > Random Flip | on |
| Wall shape | WallTree prefab root | CircleCollider2D > Radius / Offset | 2.7 / (0, 2.6) |
| Draw order | Trunk / Canopy | SpriteRenderer > Order in Layer | 11 / 12 (characters are 15 and 16, above both) |

## Gotchas

- Neighbouring colliders must overlap, or the cow slips through.
- The look is picked at Play, so the Scene view shows default sprites.
- Moving a tree changes its look, because the seed is its position.
- If `Trunk` or `Canopy` is empty, `TreeLook` logs an error and does nothing.
- If `TreeLook` ever goes on a fruit tree, it still works: `Awake` runs before `FruitTree.Start` caches the shake position.
