using UnityEngine;

// Test only: mist wisps draw over the fog but show only where fog remains, so cleared paths stay readable.
// Waits for FogGrid to build "Fog Visual", masks with it, then makes every child SpriteRenderer show only inside the mask.
public class MistMask : MonoBehaviour
{
    [Tooltip("The Fog Grid object (its child Fog Visual is made at runtime).")]
    [SerializeField] private Transform fogGrid;

    [Tooltip("Fog alpha above this keeps the mist; raise it so mist fades out sooner near clearings.")]
    [Range(0f, 1f)]
    [SerializeField] private float alphaCutoff = 0.3f;

    bool done;

    void Awake()
    {
        if (fogGrid == null) Debug.LogError($"{name}: missing Fog Grid");
    }

    void Update()
    {
        if (done || fogGrid == null) return;

        Transform visual = fogGrid.Find("Fog Visual");
        if (visual == null) return;   // FogGrid.Start has not run yet

        // Get first so this coexists with FogSilhouettes if both sit in one scene.
        var mask = visual.GetComponent<SpriteMask>();
        if (mask == null) mask = visual.gameObject.AddComponent<SpriteMask>();
        mask.sprite = visual.GetComponent<SpriteRenderer>().sprite;
        mask.alphaCutoff = alphaCutoff;

        foreach (SpriteRenderer sr in GetComponentsInChildren<SpriteRenderer>())
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;
        done = true;
    }
}
