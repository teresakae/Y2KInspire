using UnityEngine;

/// <summary>Drops a fruit near the trunk when the cow rams it, and shakes the canopy.</summary>
public class FruitTree : MonoBehaviour
{
    [Tooltip("Fruit prefab to drop (PF_Fruit).")]
    [SerializeField] private GameObject fruitPrefab;
    [Tooltip("Fruit lands at a random spot this far from the tree at most.")]
    [SerializeField] private float dropRadius = 1.2f;
    [Tooltip("Seconds before the next hit counts. Stops one dash dropping many.")]
    [SerializeField] private float cooldown = 0.5f;
    [Tooltip("Fruit sprites hanging on the canopy (children of Canopy). Each ram hides one and drops a real fruit; when all are gone the tree only shakes. Empty = unlimited drops.")]
    [SerializeField] private GameObject[] hangingFruit;
    [Tooltip("The canopy child to shake. Empty = shake the whole tree.")]
    [SerializeField] private Transform shakeTarget;
    [Tooltip("How far the canopy swings sideways (units).")]
    [SerializeField] private float shakeAmount = 0.1f;
    [Tooltip("Seconds the shake lasts.")]
    [SerializeField] private float shakeTime = 0.2f;
    [Tooltip("How fast the canopy wobbles.")]
    [SerializeField] private float shakeSpeed = 40f;

    Vector3 basePos;
    float nextHit, shakeTimer;

    void Start()
    {
        if (fruitPrefab == null) Debug.LogError($"{name}: missing Fruit Prefab");
        if (shakeTarget == null) shakeTarget = transform;
        basePos = shakeTarget.localPosition;
    }

    public void Hit()
    {
        if (Time.time < nextHit) return;
        nextHit = Time.time + cooldown;
        shakeTimer = shakeTime;
        if (!TakeHangingFruit()) return;   // empty tree still shakes, drops nothing
        if (fruitPrefab != null)
            Instantiate(fruitPrefab, (Vector2)transform.position + Random.insideUnitCircle * dropRadius, Quaternion.identity);
    }

    // Hides the next hanging fruit. False when none are left.
    // ponytail: fruit never grows back; add a regrow timer if players can run out and get stuck.
    bool TakeHangingFruit()
    {
        if (hangingFruit == null || hangingFruit.Length == 0) return true;   // none set up: unlimited, as before
        foreach (var f in hangingFruit)
            if (f != null && f.activeSelf) { f.SetActive(false); return true; }
        return false;
    }

    void Update()
    {
        if (shakeTimer <= 0f) return;
        shakeTimer -= Time.deltaTime;
        if (shakeTimer > 0f) shakeTarget.localPosition = basePos + Vector3.right * Mathf.Sin(Time.time * shakeSpeed) * shakeAmount;
        else shakeTarget.localPosition = basePos;
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.DrawWireSphere(transform.position, dropRadius);
    }
}
