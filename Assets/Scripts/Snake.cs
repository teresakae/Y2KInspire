using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>Sleeps coiled under fog. Once first revealed it roams for good, fog or not (stretched, slithering), coils to warn,
/// eats the crow if it stays grounded nearby, and dies to a strong cow dash.</summary>
[RequireComponent(typeof(SpriteRenderer))]
[RequireComponent(typeof(Rigidbody2D))]
public class Snake : MonoBehaviour
{
    [Tooltip("The crow. Its CrowEnergy says whether it is grounded.")]
    [SerializeField] private CrowEnergy crow;
    [Tooltip("The fog. The snake sleeps until the fog over it is first cleared, then roams freely, fog or not.")]
    [SerializeField] private FogGrid fog;
    [Tooltip("Shown while still: hidden, resting, warning.")]
    [SerializeField] private Sprite coiled;
    [Tooltip("Shown only while moving. Head points up.")]
    [SerializeField] private Sprite stretched;
    [Tooltip("Inactive child (a red \"!\") switched on when the crow is eaten.")]
    [SerializeField] private GameObject eatenMark;
    [Tooltip("Seconds the \"!\" shows before the level restarts.")]
    [SerializeField] private float eatenDelay = 1.5f;
    [Tooltip("The snake wanders to random points this far from where it started.")]
    [SerializeField] private float roamRadius = 3f;
    [Tooltip("Units per second.")]
    [SerializeField] private float moveSpeed = 1f;
    [Tooltip("Seconds to rest (coiled) at each roam point, and after being revealed.")]
    [SerializeField] private float pauseTime = 1.5f;
    [Tooltip("Seconds between left/right mirror flips. This is the slither.")]
    [SerializeField] private float flipInterval = 0.2f;
    [Tooltip("Rotation offset in degrees. The stretched sprite's head points up, so -90.")]
    [SerializeField] private float spriteAngle = -90f;
    [Tooltip("A grounded crow this close makes the snake warn.")]
    [SerializeField] private float warnRange = 1.5f;
    [Tooltip("Seconds of warning before the crow is eaten.")]
    [SerializeField] private float warnTime = 1f;
    [Tooltip("Crow within this distance = slither straight at it. 0 = never chase, just roam.")]
    [SerializeField] private float chaseRange = 0f;
    [Tooltip("Units per second while chasing the crow.")]
    [SerializeField] private float chaseSpeed = 2f;
    [Tooltip("Shown after a strong cow dash kills it (the snake with the X eye). Empty = it vanishes at once.")]
    [SerializeField] private Sprite dead;
    [Tooltip("Seconds the dead snake takes to fade out before it is removed.")]
    [SerializeField] private float deadFadeTime = 1f;

    const float arriveDistance = 0.05f;

    SpriteRenderer sr;
    Rigidbody2D rb;
    Collider2D col;
    Vector2 home, target;
    bool revealed, warning, eaten, dying;
    float warnTimer, pauseTimer, flipTimer, eatenTimer, dyingTimer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        home = transform.position;
        target = home;
        if (crow == null) Debug.LogError($"{name}: missing Crow");
        if (fog == null) Debug.LogError($"{name}: missing Fog");
        if (coiled == null) Debug.LogError($"{name}: missing Coiled");
        if (stretched == null) Debug.LogError($"{name}: missing Stretched");
        if (eatenMark == null) Debug.LogError($"{name}: missing Eaten Mark");
    }

    void Update()
    {
        if (dying)
        {
            dyingTimer -= Time.deltaTime;
            Color c = sr.color;
            c.a = Mathf.Clamp01(dyingTimer / deadFadeTime);
            sr.color = c;
            if (dyingTimer <= 0f) Destroy(gameObject);
            return;
        }
        if (eaten)
        {
            eatenTimer -= Time.deltaTime;
            // ponytail: direct reload until the lose screen exists
            if (eatenTimer <= 0f) SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
            return;
        }

        // Once revealed it stays awake, even if it roams back under fog.
        if (!revealed) revealed = fog == null || fog.IsClear(transform.position);
        bool crowInRange = revealed && crow != null && crow.IsGrounded
            && Vector2.Distance(transform.position, crow.transform.position) <= warnRange;

        if (!crowInRange)
        {
            warning = false;
            return;
        }
        if (!warning)
        {
            warning = true;
            warnTimer = warnTime;
        }
        warnTimer -= Time.deltaTime;
        if (warnTimer <= 0f) EatCrow();
    }

    // MovePosition only applies once per physics step, so moving from Update would
    // lose distance on fast screens. Roam here instead.
    void FixedUpdate()
    {
        if (dying) return;
        if (eaten || !revealed || warning)
        {
            // Restarting the pause means it shows coiled for a beat after being revealed or calming down.
            pauseTimer = pauseTime;
            Coil();
            return;
        }
        Roam();
    }

    void EatCrow()
    {
        eaten = true;
        eatenTimer = eatenDelay;
        Debug.Log($"{name} ate the crow");
        crow.gameObject.SetActive(false);
        if (eatenMark != null) eatenMark.SetActive(true);
    }

    void Coil()
    {
        sr.sprite = coiled;
        sr.flipX = false;
        rb.MoveRotation(0f);
    }

    void Roam()
    {
        bool chasing = chaseRange > 0f && crow != null && crow.gameObject.activeInHierarchy
            && Vector2.Distance(rb.position, crow.transform.position) <= chaseRange;
        if (chasing)
        {
            pauseTimer = 0f;
            Slither(((Vector2)crow.transform.position - rb.position).normalized, chaseSpeed);
            return;
        }

        if (pauseTimer > 0f)
        {
            Coil();
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f) target = home + Random.insideUnitCircle * roamRadius;
            return;
        }

        Vector2 pos = rb.position;
        Vector2 toTarget = target - pos;
        if (toTarget.magnitude <= arriveDistance)
        {
            pauseTimer = pauseTime;
            return;
        }

        Slither(toTarget.normalized, moveSpeed);
    }

    void Slither(Vector2 dir, float speed)
    {
        Vector2 pos = rb.position;
        if (stretched != null) sr.sprite = stretched;   // empty field would blank the sprite while moving
        rb.MovePosition(pos + dir * speed * Time.deltaTime);
        rb.MoveRotation(Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + spriteAngle);

        flipTimer -= Time.deltaTime;
        if (flipTimer <= 0f)
        {
            flipTimer = flipInterval;
            sr.flipX = !sr.flipX;
        }
    }

    // The dash may start already overlapping the snake, so Stay counts too.
    void OnTriggerEnter2D(Collider2D other) { TryDie(other); }
    void OnTriggerStay2D(Collider2D other) { TryDie(other); }

    void TryDie(Collider2D other)
    {
        if (!eaten && !dying && other.TryGetComponent(out CowRam ram) && ram.IsStrongDash) Die();
    }

    void Die()
    {
        if (dead == null)
        {
            Destroy(gameObject);
            return;
        }
        dying = true;
        dyingTimer = deadFadeTime;
        col.enabled = false;
        sr.sprite = dead;
        sr.flipX = false;
        rb.rotation = 0f;
    }

    void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? (Vector3)home : transform.position;
        Gizmos.DrawWireSphere(center, roamRadius);
        Gizmos.DrawWireSphere(transform.position, warnRange);
        Gizmos.DrawWireSphere(transform.position, chaseRange);
    }
}
