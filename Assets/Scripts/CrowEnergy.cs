using UnityEngine;
using UnityEngine.UI;

/// <summary>Crow energy: drains while moving, refilled by fruit; at zero the crow hops slowly.</summary>
[RequireComponent(typeof(PlayerControl))]
[RequireComponent(typeof(Rigidbody2D))]
public class CrowEnergy : MonoBehaviour
{
    [Tooltip("Energy at full.")]
    [SerializeField] private float maxEnergy = 100f;
    [Tooltip("Energy lost per second while moving.")]
    [SerializeField] private float drainPerSecond = 10f;
    [Tooltip("Speed multiplier at zero energy. The crow hops slowly instead of being stuck.")]
    [Range(0f, 1f)]
    [SerializeField] private float tiredSpeedScale = 0.3f;
    [Tooltip("A plain Image rectangle with its pivot on the left; it shrinks as energy drops.")]
    [SerializeField] private Image bar;
    [Tooltip("If the bar's Canvas is World Space, it floats this far above the crow.")]
    [SerializeField] private Vector2 barOffset = new Vector2(0f, 1.2f);

    PlayerControl player;
    Rigidbody2D rb;
    float energy;

    void Awake()
    {
        player = GetComponent<PlayerControl>();
        rb = GetComponent<Rigidbody2D>();
        energy = maxEnergy;
        if (bar == null) Debug.LogError($"{name}: missing Bar");
    }

    void Update()
    {
        if (rb.linearVelocity.sqrMagnitude > 0.01f) energy -= drainPerSecond * Time.deltaTime;
        energy = Mathf.Clamp(energy, 0f, maxEnergy);
        player.speedScale = energy > 0f ? 1f : tiredSpeedScale;
        if (bar != null) bar.rectTransform.localScale = new Vector3(energy / maxEnergy, 1f, 1f);
    }

    // The crow spins when it flies, so the bar's Canvas is its own object that just follows.
    void LateUpdate()
    {
        if (bar == null) return;
        var canvas = bar.canvas.rootCanvas;
        if (canvas.renderMode == RenderMode.WorldSpace) canvas.transform.position = (Vector2)transform.position + barOffset;
    }

    // Standing still counts as landed; out of energy counts as forced down (still true while hopping).
    public bool IsGrounded => energy <= 0f || rb.linearVelocity.sqrMagnitude <= 0.01f;

    public void Eat(float amount)
    {
        energy = Mathf.Clamp(energy + amount, 0f, maxEnergy);
    }
}
