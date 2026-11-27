using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Hold the ram key to charge, release to dash; a hard enough dash into a tree shakes fruit down.</summary>
[RequireComponent(typeof(PlayerControl))]
[RequireComponent(typeof(Rigidbody2D))]
public class CowRam : MonoBehaviour
{
    [Tooltip("Key to hold and release.")]
    [SerializeField] private Key ramKey = Key.Space;
    [Tooltip("Seconds to reach full charge.")]
    [SerializeField] private float chargeTime = 0.8f;
    [Tooltip("Below this charge the dash drops no fruit.")]
    [Range(0f, 1f)]
    [SerializeField] private float minCharge = 0.5f;
    [Tooltip("Dash speed at full charge (units/s). Scales with charge.")]
    [SerializeField] private float dashSpeed = 12f;
    [Tooltip("Seconds the dash lasts.")]
    [SerializeField] private float dashTime = 0.25f;
    [Tooltip("Y scale at full charge. X grows the other way.")]
    [Range(0.5f, 1f)]
    [SerializeField] private float squash = 0.8f;

    [Header("Ram juice")]
    [Tooltip("Shared camera, shaken on a hit.")]
    [SerializeField] private SharedCamera cam;
    [Tooltip("Dash stretch along the dash direction. The other axis gets thinner by the same factor.")]
    [Range(1f, 1.6f)]
    [SerializeField] private float dashStretch = 1.2f;
    [Tooltip("On impact the cow is squashed along the dash to this fraction, and fat the other way.")]
    [Range(0.4f, 1f)]
    [SerializeField] private float hitSquash = 0.7f;
    [Tooltip("Seconds of the hop back after hitting a tree. The cow can't be steered meanwhile.")]
    [SerializeField] private float hopTime = 0.15f;
    [Tooltip("Hop-back speed at full charge (units/s). Scales with charge.")]
    [SerializeField] private float hopSpeed = 5f;
    [Tooltip("Camera shake (units) for a strong hit at full charge. Scales with charge.")]
    [SerializeField] private float hitShake = 0.3f;
    [Tooltip("Camera shake (units) for a weak dash into a tree.")]
    [SerializeField] private float weakShake = 0.04f;
    [Tooltip("Camera shake (units) when the dash kills a snake.")]
    [SerializeField] private float snakeShake = 0.35f;

    [Header("Ready cue")]
    [Tooltip("Cow tint at the instant the charge reaches Min Charge.")]
    [SerializeField] private Color readyFlashColor = new Color(1f, 0.95f, 0.85f);
    [Tooltip("Seconds the ready flash takes to fade.")]
    [SerializeField] private float readyFlashTime = 0.12f;

    [Header("Aim lock")]
    [Tooltip("Crosshair shown on the locked snake or fruit tree (a prefab). Empty = no crosshair, but the lock still works.")]
    [SerializeField] private GameObject crosshairPrefab;
    [Tooltip("A snake this close can be locked (units). A full dash covers Dash Speed x Dash Time = 3, plus a margin.")]
    [SerializeField] private float aimRange = 4f;
    [Tooltip("A snake within this many degrees of the aim direction can be locked.")]
    [Range(5f, 90f)]
    [SerializeField] private float aimAngle = 35f;
    [Tooltip("Also lock onto fruit trees (crosshair shows on them, dash aims at the trunk).")]
    [SerializeField] private bool lockTrees = true;
    [Tooltip("Crosshair tint while the charge is still below Min Charge.")]
    [SerializeField] private Color crosshairColor = new Color(0.91f, 0.70f, 0.23f, 0.6f);
    [Tooltip("Crosshair tint once the dash would kill.")]
    [SerializeField] private Color crosshairReadyColor = new Color(1f, 0.95f, 0.75f, 1f);
    [Tooltip("Crosshair sprite once the dash would kill. Empty = keep the prefab's sprite.")]
    [SerializeField] private Sprite crosshairReadySprite;
    [Tooltip("Crosshair size multiplier once ready (1 = same size).")]
    [SerializeField] private float crosshairReadyScale = 1.3f;
    [Tooltip("Crosshair spin (degrees per second).")]
    [SerializeField] private float crosshairSpin = 90f;

    PlayerControl player;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Vector3 baseScale;
    Color baseColor;
    bool charging, dashing, hopping, ready;
    float charge, dashCharge, timer, popTimer;
    Snake[] snakes;
    FruitTree[] trees;
    GameObject crosshair;
    SpriteRenderer crosshairSr;
    Vector3 crosshairBaseScale;
    Sprite crosshairNormalSprite;

    void Awake()
    {
        player = GetComponent<PlayerControl>();
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        baseScale = transform.localScale;
        baseColor = sr.color;
        if (cam == null) Debug.LogError($"{name}: missing Cam");
    }

    void Start()
    {
        snakes = FindObjectsByType<Snake>();
        trees = FindObjectsByType<FruitTree>();
        if (crosshairPrefab == null) return;
        crosshair = Instantiate(crosshairPrefab);
        crosshair.SetActive(false);
        crosshairSr = crosshair.GetComponent<SpriteRenderer>();
        crosshairBaseScale = crosshair.transform.localScale;
        crosshairNormalSprite = crosshairSr.sprite;
    }

    // Whatever interrupts the cow (scene change, disabled) must not leave it squashed or tinted.
    void OnDisable()
    {
        transform.localScale = baseScale;
        if (sr != null) sr.color = baseColor;
        if (player != null) player.aimOnly = false;
        if (crosshair != null) crosshair.SetActive(false);
    }

    void Update()
    {
        if (Time.timeScale == 0f) return; // paused
        var kb = Keyboard.current;
        if (kb == null) return;

        if (hopping)
        {
            timer -= Time.deltaTime;
            // squash eases back to normal over the hop
            transform.localScale = Vector3.Scale(baseScale, AlongScale(Mathf.Lerp(1f, hitSquash, timer / hopTime)));
            if (timer <= 0f) EndDash();
            return;
        }

        if (dashing)
        {
            timer -= Time.deltaTime;
            if (timer <= 0f) EndDash();
            return;
        }

        if (!charging && kb[ramKey].wasPressedThisFrame)
        {
            // PlayerControl stays on but aim-only: WASD turns the aim, it sets no velocity.
            charging = true;
            ready = false;
            charge = 0f;
            player.aimOnly = true;
            rb.linearVelocity = Vector2.zero;
        }

        if (!charging) return;

        if (kb[ramKey].isPressed)
        {
            charge = Mathf.Clamp01(charge + Time.deltaTime / chargeTime);
            if (!ready && charge >= minCharge)
            {
                ready = true;   // once per charge
                popTimer = readyFlashTime;
            }
            popTimer -= Time.deltaTime;
            float pop = Mathf.Clamp01(popTimer / readyFlashTime);
            sr.color = Color.Lerp(baseColor, readyFlashColor, pop);
            Vector3 squashed = new Vector3(1 + (1 - squash) * charge, Mathf.Lerp(1, squash, charge), 1);
            transform.localScale = Vector3.Scale(baseScale, squashed);
            ShowCrosshair(FindTarget());
        }
        else
        {
            Transform target = FindTarget();
            if (target != null)
            {
                // Soft lock: dash at the snake, not along the 8-way aim. lastDir follows so AlongScale and TryHitTree agree.
                player.lastDir = ((Vector2)target.position - (Vector2)transform.position).normalized;
            }
            if (crosshair != null) crosshair.SetActive(false);
            sr.color = baseColor;
            transform.localScale = baseScale;
            charging = false;
            player.aimOnly = false;
            player.enabled = false;   // PlayerControl overwrites velocity every frame, so it stays off while we drive
            dashing = true;
            timer = dashTime;
            dashCharge = charge;
            rb.linearVelocity = player.lastDir.normalized * dashSpeed * charge;
            transform.localScale = Vector3.Scale(baseScale, AlongScale(dashStretch));
        }
    }

    // Nearest live snake or fruit tree inside Aim Range and within Aim Angle of the aim direction, or null.
    Transform FindTarget()
    {
        Transform best = null;
        float bestDist = aimRange;
        foreach (Snake s in snakes)
        {
            if (s == null || !s.gameObject.activeInHierarchy || !s.CanBeLocked) continue;
            Vector2 to = s.transform.position - transform.position;
            if (to.magnitude > bestDist || Vector2.Angle(player.lastDir, to) > aimAngle) continue;
            best = s.transform;
            bestDist = to.magnitude;
        }
        if (lockTrees)
        {
            foreach (FruitTree t in trees)
            {
                if (t == null || !t.gameObject.activeInHierarchy) continue;
                Vector2 to = t.transform.position - transform.position;
                if (to.magnitude > bestDist || Vector2.Angle(player.lastDir, to) > aimAngle) continue;
                best = t.transform;
                bestDist = to.magnitude;
            }
        }
        return best;
    }

    // Crosshair sits on the target; bigger and brighter once the dash would kill. null hides it.
    void ShowCrosshair(Transform target)
    {
        if (crosshair == null) return;
        crosshair.SetActive(target != null);
        if (target == null) return;
        crosshair.transform.position = target.position;
        crosshair.transform.Rotate(0f, 0f, crosshairSpin * Time.deltaTime);
        crosshair.transform.localScale = crosshairBaseScale * (ready ? crosshairReadyScale : 1f);
        crosshairSr.color = ready ? crosshairReadyColor : crosshairColor;
        crosshairSr.sprite = (ready && crosshairReadySprite != null) ? crosshairReadySprite : crosshairNormalSprite;
    }

    // Snakes check this: only a dash charged past Min Charge kills them.
    public bool IsStrongDash => dashing && dashCharge >= minCharge;

    // Called by the snake it kills: same thump as a strong tree hit, so the kill lands.
    public void HitSnake()
    {
        if (cam != null) cam.Shake(snakeShake);
        StartHop();
    }

    // Scale that is `along` times longer on the dash axis (x or y, whichever the cow faces) and the
    // opposite way on the other axis, so the volume stays about the same.
    Vector3 AlongScale(float along)
    {
        float across = 1f / along;
        return Mathf.Abs(player.lastDir.x) >= Mathf.Abs(player.lastDir.y) ? new Vector3(along, across, 1f) : new Vector3(across, along, 1f);
    }

    // The cow drives the hit (the tree doesn't check for the cow) so there is no
    // collision-callback order to worry about.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!dashing) return;
        bool strongHit = TryHitTree(collision);
        bool tree = strongHit || IsTree(collision);
        if (tree && !strongHit && cam != null) cam.Shake(weakShake);   // weak dash, or a wall tree: just a bump
        if (tree) StartHop();
        else EndDash();
    }

    // Charging while already touching the trunk gives no new Enter, so check ongoing contacts too.
    // Only trees here: ending on any Stay would kill a dash that starts pressed against fog.
    void OnCollisionStay2D(Collision2D collision)
    {
        if (dashing && TryHitTree(collision)) StartHop();
    }

    bool IsTree(Collision2D collision)
    {
        return collision.gameObject.GetComponent<FruitTree>() != null || collision.gameObject.GetComponent<TreeLook>() != null;
    }

    bool TryHitTree(Collision2D collision)
    {
        if (!IsStrongDash || !collision.gameObject.TryGetComponent(out FruitTree tree)) return false;
        // Dashing away from a trunk we're touching shouldn't count as a ram.
        Vector2 toTree = collision.transform.position - transform.position;
        if (Vector2.Dot(player.lastDir, toTree) <= 0f) return false;
        tree.Hit();
        if (cam != null) cam.Shake(hitShake * dashCharge);
        return true;
    }

    // Squash and a short hop backwards. PlayerControl stays off until the hop is over.
    void StartHop()
    {
        dashing = false;
        hopping = true;
        timer = hopTime;
        rb.linearVelocity = -player.lastDir.normalized * hopSpeed * dashCharge;
        transform.localScale = Vector3.Scale(baseScale, AlongScale(hitSquash));
    }

    void EndDash()
    {
        dashing = false;
        hopping = false;
        transform.localScale = baseScale;
        rb.linearVelocity = Vector2.zero;
        player.enabled = true;
    }
}
