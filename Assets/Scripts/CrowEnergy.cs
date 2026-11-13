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
    [Tooltip("A Filled Image showing the energy.")]
    [SerializeField] private Image bar;

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
        if (bar != null) bar.fillAmount = energy / maxEnergy;
    }

    public void Eat(float amount)
    {
        energy = Mathf.Clamp(energy + amount, 0f, maxEnergy);
    }
}
