using UnityEngine;

// Test only: trees draw above the fog, but show as flat dark silhouettes wherever the fog still covers them.
// Waits for FogGrid to build "Fog Visual", masks with it, then copies every tree sprite into a masked silhouette child.
public class FogSilhouettes : MonoBehaviour
{
    [Tooltip("The Fog Grid object (its child Fog Visual is made at runtime).")]
    [SerializeField] private Transform fogGrid;

    [Tooltip("Parents whose SpriteRenderers get a silhouette: Walls and Tree.")]
    [SerializeField] private Transform[] treeRoots;

    [Tooltip("Fog alpha above this counts as fogged (silhouette shows).")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaCutoff = 0.3f;

    [Tooltip("Flat silhouette colour. Keep alpha at 1: see-through pieces stack darker where trunk and canopies overlap.")]
    [SerializeField] private Color silhouetteColor = new Color(0.86f, 0.83f, 0.87f, 1f);

    [Tooltip("Added to the tree sprite's Order in Layer. Must land above the fog (10) and below the animals (15).")]
    [SerializeField] private int orderOffset = 6;

    bool done;

    void Awake()
    {
        if (fogGrid == null) Debug.LogError($"{name}: missing Fog Grid");
        if (treeRoots == null || treeRoots.Length == 0) Debug.LogError($"{name}: missing Tree Roots");
    }

    void Update()
    {
        if (done || fogGrid == null || treeRoots == null) return;

        Transform visual = fogGrid.Find("Fog Visual");
        if (visual == null) return;   // FogGrid.Start has not run yet

        // ponytail: built-in GUI/Text Shader draws one flat colour in the sprite's shape, so a tree's parts merge into one shape. Editor test only (Shader.Find may be stripped in builds).
        var flat = new Material(Shader.Find("GUI/Text Shader"));

        var mask = visual.gameObject.AddComponent<SpriteMask>();
        mask.sprite = visual.GetComponent<SpriteRenderer>().sprite;
        mask.alphaCutoff = alphaCutoff;

        foreach (Transform root in treeRoots)
        {
            if (root == null) continue;
            foreach (SpriteRenderer sr in root.GetComponentsInChildren<SpriteRenderer>())
            {
                if (sr.sortingOrder < 0) continue;   // shadows
                var child = new GameObject("Silhouette");
                child.transform.SetParent(sr.transform, false);
                var copy = child.AddComponent<SpriteRenderer>();
                copy.sprite = sr.sprite;
                copy.flipX = sr.flipX;
                copy.flipY = sr.flipY;
                copy.sortingLayerID = sr.sortingLayerID;
                copy.sortingOrder = sr.sortingOrder + orderOffset;
                copy.sharedMaterial = flat;
                copy.color = silhouetteColor;
                copy.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
            }
        }
        done = true;
    }
}
