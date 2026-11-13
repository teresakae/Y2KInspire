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

    PlayerControl player;
    Rigidbody2D rb;
    Vector3 baseScale;
    bool charging, dashing;
    float charge, dashCharge, timer;

    void Awake()
    {
        player = GetComponent<PlayerControl>();
        rb = GetComponent<Rigidbody2D>();
        baseScale = transform.localScale;
    }

    void Update()
    {
        var kb = Keyboard.current;
        if (kb == null) return;

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
        }
    }

    // Snakes check this: only a dash charged past Min Charge kills them.
    public bool IsStrongDash => dashing && dashCharge >= minCharge;

    // The cow drives the hit (the tree doesn't check for the cow) so there is no
    // collision-callback order to worry about.
    void OnCollisionEnter2D(Collision2D collision)
    {
        if (!dashing) return;
        TryHitTree(collision);
        EndDash();
    }

    // Charging while already touching the trunk gives no new Enter, so check ongoing contacts too.
    // Only trees here: ending on any Stay would kill a dash that starts pressed against fog.
    void OnCollisionStay2D(Collision2D collision)
    {
        if (dashing && TryHitTree(collision)) EndDash();
    }

    bool TryHitTree(Collision2D collision)
    {
        if (!IsStrongDash || !collision.gameObject.TryGetComponent(out FruitTree tree)) return false;
        // Dashing away from a trunk we're touching shouldn't count as a ram.
        Vector2 toTree = collision.transform.position - transform.position;
        if (Vector2.Dot(player.lastDir, toTree) <= 0f) return false;
        tree.Hit();
        return true;
    }

    void EndDash()
    {
        dashing = false;
        rb.linearVelocity = Vector2.zero;
        player.enabled = true;
    }
}
