using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>Crow energy: drains while moving, refilled by fruit; at zero the crow hops slowly, and it drowns if it lands on Water.</summary>
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
    [Tooltip("Seconds between drowning and the lose screen (or a restart if the scene has none).")]
    [SerializeField] private float drownDelay = 0.6f;
    [Tooltip("Seconds the crow can stand still over Water before it drowns. Stops a quick change of direction from counting as landing.")]
    [Range(0f, 2f)]
    [SerializeField] private float landGrace = 0.3f;
    [Tooltip("Camera shake when the crow drowns (units). 0 = none.")]
    [SerializeField] private float drownShake = 0.12f;

    [Tooltip("Played when the crow eats fruit (Crow_eat). Empty = silent.")]
    [SerializeField] private AudioClip eatClip;
    [Tooltip("Played when the crow drowns. Empty = silent.")]
    [SerializeField] private AudioClip drownClip;

    private AudioSource audioSource;
    PlayerControl player;
    Rigidbody2D rb;
    float energy;
    bool drowned;
    float drownTimer;
    float wetTime;
    LoseScreen loseScreen;
    SharedCamera cam;

    void Awake()
    {
        loseScreen = FindAnyObjectByType<LoseScreen>();   // none in test scenes: that is fine
        cam = FindAnyObjectByType<SharedCamera>();
        player = GetComponent<PlayerControl>();
        rb = GetComponent<Rigidbody2D>();
        energy = maxEnergy;
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();   // ponytail: auto-added so no scene setup is needed
        audioSource.playOnAwake = false;
        if (bar == null) Debug.LogError($"{name}: missing Bar");
    }

    void Update()
    {
        if (drowned)
        {
            drownTimer -= Time.deltaTime;
            if (drownTimer <= 0f)
            {
                if (loseScreen != null) loseScreen.ShowDrowned();
                else SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);   // No lose screen in this scene (test scenes): restart directly.
            }
            return;
        }
        if (rb.linearVelocity.sqrMagnitude > 0.01f) energy -= drainPerSecond * Time.deltaTime;
        energy = Mathf.Clamp(energy, 0f, maxEnergy);
        player.speedScale = energy > 0f ? 1f : tiredSpeedScale;
        if (bar != null) bar.rectTransform.localScale = new Vector3(energy / maxEnergy, 1f, 1f);
        // Landed (standing still, or out of energy) over water: drown after a short grace.
        wetTime = IsGrounded && OverWater() ? wetTime + Time.deltaTime : 0f;
        if (wetTime > landGrace) Drown();
    }

    bool OverWater()
    {
        foreach (var c in Physics2D.OverlapPointAll(transform.position))
            if (c.GetComponent<Water>() != null) return true;
        return false;
    }

    // After the delay the lose screen shows (a direct restart if the scene has none), same as a snake bite.
    void Drown()
    {
        drowned = true;
        if (drownClip != null) audioSource.PlayOneShot(drownClip);
        drownTimer = drownDelay;
        if (cam != null) cam.Shake(drownShake);
        // Tutorial signs would cover the death bubble / drowning.
        foreach (TutorialTip tip in FindObjectsByType<TutorialTip>()) tip.Hide();
        player.enabled = false;
        rb.linearVelocity = Vector2.zero;
        foreach (var sr in GetComponentsInChildren<SpriteRenderer>()) sr.enabled = false;
        if (bar != null) bar.enabled = false;
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
        if (eatClip != null) audioSource.PlayOneShot(eatClip);
    }
}
