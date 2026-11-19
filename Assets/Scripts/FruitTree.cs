using UnityEngine;

/// <summary>When the cow rams it, a hanging fruit falls straight down to the ground below the trunk, and the canopy shakes.</summary>
public class FruitTree : MonoBehaviour
{
    [Tooltip("Fruit prefab to drop (PF_Fruit).")]
    [SerializeField] private GameObject fruitPrefab;
    [Tooltip("How far below the tree's pivot (trunk centre) the fruit lands. More than half the trunk's height puts it on the ground in front of the trunk, not on the trunk or canopy.")]
    [SerializeField] private float dropBelow = 2f;
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
        if (fruitPrefab == null) return;
        Vector3 from = transform.position;   // no hanging fruit set up: drops from the trunk, unlimited
        if (hangingFruit != null && hangingFruit.Length > 0)
        {
            GameObject f = TakeHangingFruit();
            if (f == null) return;   // empty tree still shakes, drops nothing
            from = f.transform.position;
        }
        // Falls straight down from where it hung, so it lands in front of the trunk.
        GameObject fruit = Instantiate(fruitPrefab, from, Quaternion.identity);
        fruit.GetComponent<Fruit>().Fall(transform.position.y - dropBelow);
    }

    // Hides the next hanging fruit and returns it. Null when none are left.
    // ponytail: fruit never grows back; add a regrow timer if players can run out and get stuck.
    GameObject TakeHangingFruit()
    {
        foreach (var f in hangingFruit)
            if (f != null && f.activeSelf) { f.SetActive(false); return f; }
        return null;
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
        // the height where fruit lands
        Vector3 c = transform.position + Vector3.down * dropBelow;
        Gizmos.DrawLine(c + Vector3.left * 1.5f, c + Vector3.right * 1.5f);
    }
}
