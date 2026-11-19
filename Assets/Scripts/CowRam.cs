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

    PlayerControl player;
    Rigidbody2D rb;
    Vector3 baseScale;
    bool charging, dashing, hopping;
    float charge, dashCharge, timer;

    void Awake()
    {
        player = GetComponent<PlayerControl>();
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
        if (cam == null) Debug.LogError($"{name}: missing Cam");
    }

    // Whatever interrupts the cow (scene change, disabled) must not leave it squashed.
    void OnDisable()
    {
        transform.localScale = baseScale;
    }

    void Update()
    {
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
            // PlayerControl overwrites velocity every frame, so switch it off while we drive.
            charging = true;
            charge = 0f;
            player.enabled = false;
            rb.linearVelocity = Vector2.zero;
        }

        if (!charging) return;

        if (kb[ramKey].isPressed)
        {
            charge = Mathf.Clamp01(charge + Time.deltaTime / chargeTime);
            transform.localScale = Vector3.Scale(baseScale, new Vector3(1 + (1 - squash) * charge, Mathf.Lerp(1, squash, charge), 1));
        }
        else
        {
            transform.localScale = baseScale;
            charging = false;
            dashing = true;
            timer = dashTime;
            dashCharge = charge;
            rb.linearVelocity = player.lastDir.normalized * dashSpeed * charge;
            transform.localScale = Vector3.Scale(baseScale, AlongScale(dashStretch));
        }
    }

    // Snakes check this: only a dash charged past Min Charge kills them.
    public bool IsStrongDash => dashing && dashCharge >= minCharge;

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
