using UnityEngine;
using UnityEngine.Events;

/// <summary>When the cow rams it, a hanging fruit falls straight down to the ground below the trunk, and the canopy shakes.</summary>
public class FruitTree : MonoBehaviour
{
    [Tooltip("Fruit prefab to drop (PF_Fruit).")]
    [SerializeField] private GameObject fruitPrefab;
    [Tooltip("How far below the tree's pivot (trunk centre) the fruit lands. More than half the trunk's height puts it on the ground in front of the trunk, not on the trunk or canopy.")]
    [SerializeField] private float dropBelow = 2f;
    [Tooltip("Seconds before the next hit counts. Stops one dash dropping many.")]
    [SerializeField] private float cooldown = 0.5f;
    [Tooltip("Fruit sprites hanging on the canopy (children of Canopy). Each ram hides one and drops a real fruit; when all are gone the tree only shakes.")]
    [SerializeField] private GameObject[] hangingFruit;
    [Tooltip("The canopy child to shake. Empty = shake the whole tree.")]
    [SerializeField] private Transform shakeTarget;
    [Tooltip("How far the canopy swings sideways (units).")]
    [SerializeField] private float shakeAmount = 0.1f;
    [Tooltip("Seconds the shake lasts.")]
    [SerializeField] private float shakeTime = 0.2f;
    [Tooltip("How fast the canopy wobbles.")]
    [SerializeField] private float shakeSpeed = 40f;
    [Tooltip("Seconds after a hanging fruit is picked before it grows back on the canopy. 0 = never.")]
    [SerializeField] private float regrowTime = 20f;

    [Tooltip("Runs when a fruit drops. Passes the fallen fruit. Used by tutorial tips.")]
    [SerializeField] private UnityEvent<Transform> onDrop;

    SpriteRenderer canopy;   // the shaken child's sprite, for CanopyCovers
    Vector3 basePos;
    float nextHit, shakeTimer;
    float[] regrowAt;   // time each hanging fruit is due back, parallel to hangingFruit; 0 = not waiting

    void Start()
    {
        if (fruitPrefab == null) Debug.LogError($"{name}: missing Fruit Prefab");
        if (shakeTarget == null) shakeTarget = transform;
        basePos = shakeTarget.localPosition;
        canopy = shakeTarget.GetComponent<SpriteRenderer>();
        regrowAt = new float[hangingFruit.Length];
    }

    public void Hit()
    {
        if (Time.time < nextHit) return;
        nextHit = Time.time + cooldown;
        shakeTimer = shakeTime;
        if (fruitPrefab == null) return;
        GameObject f = TakeHangingFruit();
        if (f == null) return;   // empty tree still shakes, drops nothing
        // Falls straight down from where it hung, so it lands in front of the trunk.
        GameObject fruit = Instantiate(fruitPrefab, f.transform.position, Quaternion.identity);
        fruit.GetComponent<Fruit>().Fall(transform.position.y - dropBelow);
        onDrop.Invoke(fruit.transform);
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

    // Hides the next hanging fruit and returns it. Null when none are left.
    GameObject TakeHangingFruit()
    {
        for (int i = 0; i < hangingFruit.Length; i++)
        {
            GameObject f = hangingFruit[i];
            if (f != null && f.activeSelf)
            {
                f.SetActive(false);
                if (regrowTime > 0f) regrowAt[i] = Time.time + regrowTime;
                return f;
            }
        }
        return null;
    }

    void Update()
    {
        for (int i = 0; i < regrowAt.Length; i++)
            if (regrowAt[i] > 0f && Time.time >= regrowAt[i])
            {
                regrowAt[i] = 0f;
                if (hangingFruit[i] != null) hangingFruit[i].SetActive(true);
            }

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
