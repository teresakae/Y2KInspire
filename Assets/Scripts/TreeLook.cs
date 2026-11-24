using UnityEngine;

/// <summary>Gives a wall tree a random trunk, canopy, canopy height and flip, so a forest of one prefab does not look copy-pasted.</summary>
public class TreeLook : MonoBehaviour
{
    [SerializeField, Tooltip("The Trunk child's SpriteRenderer.")]
    private SpriteRenderer trunk;

    [SerializeField, Tooltip("The Canopy child's SpriteRenderer.")]
    private SpriteRenderer canopy;

    [SerializeField, Tooltip("Trunk sprites to pick from. Empty keeps the prefab's sprite.")]
    private Sprite[] trunks;

    [SerializeField, Tooltip("Canopy sprites to pick from. Empty keeps the prefab's sprite.")]
    private Sprite[] canopies;

    [SerializeField, Tooltip("Canopy's local Y above the trunk, min (x) to max (y). Set by eye: look at Canopy > Transform > Position Y now and pick a range around it.")]
    private Vector2 canopyHeight = new Vector2(1.5f, 2.5f);

    // Awake, not Start: the canopy must be placed before FruitTree.Start caches its shake position, in case this ever goes on a fruit tree.
    // ponytail: the look is picked at Play, so the Scene view shows the prefab's default sprites. Previewing in the editor would need editor-only code.
    // ponytail: the seed comes from the position, so moving a tree changes its look.
    void Awake()
    {
        if (trunk == null || canopy == null)
        {
            Debug.LogError($"{name}: missing Trunk/Canopy");
            return;
        }

        // Same position, same look every Play, so screenshots are stable.
        var oldState = Random.state;
        Random.InitState(Mathf.RoundToInt(transform.position.x * 100f) * 7919 + Mathf.RoundToInt(transform.position.y * 100f));

        if (trunks.Length > 0) trunk.sprite = trunks[Random.Range(0, trunks.Length)];
        if (canopies.Length > 0) canopy.sprite = canopies[Random.Range(0, canopies.Length)];

        Vector3 p = canopy.transform.localPosition;
        p.y = Random.Range(canopyHeight.x, canopyHeight.y);
        canopy.transform.localPosition = p;

        trunk.flipX = Random.value < 0.5f;
        canopy.flipX = Random.value < 0.5f;

        Random.state = oldState;   // don't disturb other scripts' randomness
    }

    /// <summary>True when p is under the canopy: inside the ellipse that fits its sprite bounds, grown by margin. Snakes use it to stay out from under leaves.</summary>
    public bool CanopyCovers(Vector2 p, float margin)
    {
        if (canopy == null) return false;
        Bounds b = canopy.bounds;
        Vector2 d = p - (Vector2)b.center;
        float x = d.x / (b.extents.x + margin), y = d.y / (b.extents.y + margin);
        return x * x + y * y < 1f;
    }
}
