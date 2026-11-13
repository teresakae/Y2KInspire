# Trees and fruit

## What it is

Fruit trees stand in the forest. When the cow charges into one with a strong enough ram, the canopy shakes and a fruit drops nearby. The crow eats it by flying over it, which refills its energy.

## Where it lives

- Scripts: `Assets/Scripts/FruitTree.cs` (on each tree), `Assets/Scripts/Fruit.cs` (on the fruit prefab). The ram itself is `CowRam.cs`, see `players.md`.
- Prefab: `Assets/Prefabs/PF_Fruit`. Not made yet.
- Not in the scene yet.

## How it works

1. The cow dashes into the tree's collider. `CowRam` checks its charge and calls `FruitTree.Hit()`.
2. `Hit()` is ignored if `Cooldown` hasn't passed since the last hit (stops one dash dropping many). Otherwise it spawns `Fruit Prefab` at a random spot within `Drop Radius` of the tree, and starts a shake.
3. The shake moves `Shake Target` sideways with a sine wave for `Shake Time` seconds, then puts it back. With no target set it shakes the whole tree.
4. The crow touches the fruit's trigger: `Fruit` calls `CrowEnergy.Eat(Energy)` and destroys itself. The cow's touch is ignored.

## Adding a tree

1. Create an empty object `Tree` in the scene.
2. Add the trunk as a SpriteRenderer on `Tree`. Add a Box Collider 2D covering the trunk base only, so the cow can walk behind the canopy.
3. Add a child `Canopy` with the canopy sprite, sorted above the cow. No collider.
4. Add `FruitTree` to `Tree`. Set `Fruit Tree > Fruit Prefab` to `Assets/Prefabs/PF_Fruit`.
5. Drag `Canopy` into `Fruit Tree > Shake Target`.
6. Select the tree to see the drop radius as a wire circle in the Scene view.

## Making the fruit prefab

1. Create an empty object `PF_Fruit` with a SpriteRenderer. Until there is art, use a flat circle sprite tinted plum #7B3F6B.
2. Add a Circle Collider 2D and tick `Is Trigger`.
3. Add the `Fruit` script.
4. Drag it into `Assets/Prefabs` to make the prefab, then delete it from the scene.

## Tuning

| What | Object | Field | Current value |
|---|---|---|---|
| Fruit prefab | Tree | FruitTree > Fruit Prefab | not set up yet |
| Drop distance | Tree | FruitTree > Drop Radius | 1.2 (default) |
| Time between hits | Tree | FruitTree > Cooldown | 0.5 (default) |
| Canopy to shake | Tree | FruitTree > Shake Target | not set up yet |
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

- Fruit per tree is unlimited.
- Fruit never rots or despawns.
