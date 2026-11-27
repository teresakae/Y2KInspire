using UnityEngine;
using UnityEngine.Events;
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
    [Tooltip("Inactive child speech bubble with a \"!\" (PF_EatenBubble), switched on when the crow is eaten.")]
    [SerializeField] private GameObject eatenMark;
    [Tooltip("Seconds the \"!\" shows before the lose screen fades in (or the level restarts if the scene has none).")]
    [SerializeField] private float eatenDelay = 1f;
    [Tooltip("Camera shake when the snake eats the crow (units). 0 = none.")]
    [SerializeField] private float eatenShake = 0.15f;
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
    [Tooltip("A warning snake calms down only once the crow is this much past Warn Range (or takes off). Stops it flicking between coiled and chasing when a tired crow waddles at the edge.")]
    [SerializeField] private float calmMargin = 1f;
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

    [Tooltip("What blocks the snake: tree trunks and water, like the cow. Tick Default and Water; leave Fog and Crow off.")]
    [SerializeField] private LayerMask obstacles = ~0;
    [Tooltip("Snake's body size for bumping into trunks and water (units).")]
    [SerializeField] private float bodyRadius = 0.35f;

    [Tooltip("Spawned over the snake when the cow's dash hits it (the floating \"bonk!\"). Empty = none.")]
    [SerializeField] private GameObject bonk;
    [Tooltip("How far above the snake the bonk appears (units).")]
    [SerializeField] private float bonkHeight = 0.6f;
    [Tooltip("Runs when the cow's dash kills this snake. Passes the snake. Used by the tutorial sign.")]
    [SerializeField] private UnityEvent<Transform> onHit;

    [Header("Sound")]
    [Tooltip("Hiss takes (SFX_SnakeHiss1..3). One random take plays when first revealed and now and then while roaming. Empty = silent.")]
    [SerializeField] private AudioClip[] hissClips;
    [Tooltip("Sharp attack takes (SFX_SnakeAttack1..3). One random take plays when it eats the crow. Empty = uses a hiss take.")]
    [SerializeField] private AudioClip[] attackClips;
    [Tooltip("Shortest wait between roaming hisses (seconds).")]
    [SerializeField] private float hissMinGap = 6f;
    [Tooltip("Longest wait between roaming hisses (seconds).")]
    [SerializeField] private float hissMaxGap = 12f;
    [Tooltip("Volume of the reveal and roaming hisses.")]
    [Range(0f, 1f)]
    [SerializeField] private float roamVolume = 0.4f;
    [Tooltip("Pitch of the attack hiss. Below 1 = deeper.")]
    [SerializeField] private float attackPitch = 0.9f;

    const float arriveDistance = 0.05f;
    const int roamTries = 10;   // random roam points tried before giving up and heading home: a snake boxed in by trees must not loop forever
    const float minSlide = 0.4f;   // a slide along a tree shorter than this fraction of a full step counts as stuck, or the snake wriggles on the spot

    AudioSource audioSource;
    float hissTimer;
    SpriteRenderer sr;
    Rigidbody2D rb;
    Collider2D col;
    Vector2 home, target;
    bool revealed, warning, eaten, dying;
    float warnTimer, pauseTimer, flipTimer, eatenTimer, dyingTimer;
    TreeLook[] trees;          // for the canopy test; found in Start because TreeLook places canopies in Awake
    FruitTree[] fruitTrees;
    LoseScreen loseScreen;
    SharedCamera cam;
    ContactFilter2D obstacleFilter;
    readonly Collider2D[] hits = new Collider2D[16];

    // CowRam only locks onto snakes the players have seen, and not ones already fading.
    public bool CanBeLocked => revealed && !dying;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        rb = GetComponent<Rigidbody2D>();
        col = GetComponent<Collider2D>();
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();   // ponytail: auto-added so no scene setup is needed
        audioSource.playOnAwake = false;
        home = transform.position;
        target = home;
        loseScreen = FindAnyObjectByType<LoseScreen>();   // none in test scenes: that is fine
        cam = FindAnyObjectByType<SharedCamera>();
        if (crow == null) Debug.LogError($"{name}: missing Crow");
        if (fog == null) Debug.LogError($"{name}: missing Fog");
        if (coiled == null) Debug.LogError($"{name}: missing Coiled");
        if (stretched == null) Debug.LogError($"{name}: missing Stretched");
        if (eatenMark == null) Debug.LogError($"{name}: missing Eaten Mark");
    }

    void Start()
    {
        trees = FindObjectsByType<TreeLook>();
        fruitTrees = FindObjectsByType<FruitTree>();
        obstacleFilter = new ContactFilter2D();
        obstacleFilter.SetLayerMask(obstacles);
        obstacleFilter.useTriggers = true;   // water may be a trigger; trees are checked below
        if (Blocked(home)) Debug.LogWarning($"{name}: starts inside a tree, its leaves or water");
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
            if (eatenTimer <= 0f)
            {
                // Show() ignores repeat calls, so calling it every frame is fine.
                if (loseScreen != null) loseScreen.ShowEaten();
                else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // No lose screen in this scene (test scenes): restart directly.
            }
            return;
        }

        // Once revealed it stays awake, even if it roams back under fog.
        if (!revealed)
        {
            revealed = fog == null || fog.IsClear(transform.position);
            if (revealed)
            {
                PlayHiss();
                hissTimer = Random.Range(hissMinGap, hissMaxGap);
            }
        }
        else if (hissClips != null && hissClips.Length > 0)
        {
            hissTimer -= Time.deltaTime;
            if (hissTimer <= 0f)
            {
                PlayHiss();
                hissTimer = Random.Range(hissMinGap, hissMaxGap);
            }
        }
        bool crowInRange = revealed && crow != null && crow.IsGrounded
            && Vector2.Distance(transform.position, crow.transform.position) <= (warning ? warnRange + calmMargin : warnRange);

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
        AudioClip[] bites = (attackClips != null && attackClips.Length > 0) ? attackClips : hissClips;
        if (bites != null && bites.Length > 0)
        {
            audioSource.pitch = attackPitch;
            audioSource.PlayOneShot(bites[Random.Range(0, bites.Length)], 1f);
        }
        eatenTimer = eatenDelay;
        Debug.Log($"{name} ate the crow");
        crow.gameObject.SetActive(false);
        if (cam != null) cam.Shake(eatenShake);
        // Tutorial signs would cover the death bubble / drowning.
        foreach (TutorialTip tip in FindObjectsByType<TutorialTip>()) tip.Hide();
        if (eatenMark != null) eatenMark.SetActive(true);
    }

    void PlayHiss()
    {
        if (hissClips == null || hissClips.Length == 0) return;
        audioSource.PlayOneShot(hissClips[Random.Range(0, hissClips.Length)], roamVolume);
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
            if (!Slither(((Vector2)crow.transform.position - rb.position).normalized, chaseSpeed)) Coil();   // stuck behind a tree: wait
            return;
        }

        if (pauseTimer > 0f)
        {
            Coil();
            pauseTimer -= Time.deltaTime;
            if (pauseTimer <= 0f) target = PickTarget();
            return;
        }

        Vector2 pos = rb.position;
        Vector2 toTarget = target - pos;
        if (toTarget.magnitude <= arriveDistance)
        {
            pauseTimer = pauseTime;
            return;
        }

        if (!Slither(toTarget.normalized, moveSpeed))
        {
            // Boxed in by a trunk, leaves or water: rest, then pick another point.
            pauseTimer = pauseTime;
            Coil();
        }
    }

    // First random point near home that is not blocked; home itself if all tries are.
    Vector2 PickTarget()
    {
        for (int i = 0; i < roamTries; i++)
        {
            Vector2 p = home + Random.insideUnitCircle * roamRadius;
            if (!Blocked(p)) return p;
        }
        return home;
    }

    // True if a trunk or water is within bodyRadius of p, or p is under a canopy (the snake would hide under the leaves).
    bool Blocked(Vector2 p)
    {
        int count = Physics2D.OverlapCircle(p, bodyRadius, obstacleFilter, hits);
        for (int i = 0; i < count; i++)
        {
            Collider2D c = hits[i];
            if (c.GetComponentInParent<Water>() != null) return true;
            if (!c.isTrigger && (c.GetComponentInParent<TreeLook>() != null || c.GetComponentInParent<FruitTree>() != null)) return true;
        }
        foreach (TreeLook t in trees)
            if (t.CanopyCovers(p, bodyRadius)) return true;
        foreach (FruitTree t in fruitTrees)
            if (t.CanopyCovers(p, bodyRadius)) return true;
        return false;
    }

    // Moves one step, sliding along trees like the cow (x only, then y only). Returns false if it could not move.
    // A snake that is already blocked moves freely, so it can get out.
    bool Slither(Vector2 dir, float speed)
    {
        Vector2 pos = rb.position;
        Vector2 step = dir * speed * Time.deltaTime;
        if (Blocked(pos + step) && !Blocked(pos))
        {
            if (!Blocked(pos + new Vector2(step.x, 0f))) step = new Vector2(step.x, 0f);
            else if (!Blocked(pos + new Vector2(0f, step.y))) step = new Vector2(0f, step.y);
            else return false;
        }
        if (step.magnitude < minSlide * speed * Time.deltaTime) return false;
        if (stretched != null) sr.sprite = stretched;   // empty field would blank the sprite while moving
        rb.MovePosition(pos + step);
        rb.MoveRotation(Mathf.Atan2(step.y, step.x) * Mathf.Rad2Deg + spriteAngle);

        flipTimer -= Time.deltaTime;
        if (flipTimer <= 0f)
        {
            flipTimer = flipInterval;
            sr.flipX = !sr.flipX;
        }
        return true;
    }

    // The dash may start already overlapping the snake, so Stay counts too.
    void OnTriggerEnter2D(Collider2D other) { TryDie(other); }
    void OnTriggerStay2D(Collider2D other) { TryDie(other); }

    void TryDie(Collider2D other)
    {
        if (!eaten && !dying && other.TryGetComponent(out CowRam ram) && ram.IsStrongDash)
        {
            ram.HitSnake();
            Die();
        }
    }

    void Die()
    {
        if (bonk != null) Instantiate(bonk, transform.position + Vector3.up * bonkHeight, Quaternion.identity);
        onHit.Invoke(transform);
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
