using UnityEngine;

/// <summary>Fruit on the ground. Can fall from a tree first. The crow eats it by flying over; the cow ignores it.</summary>
public class Fruit : MonoBehaviour
{
    [Tooltip("Energy the crow gets.")]
    [SerializeField] private float energy = 30f;
    [Tooltip("How fast a falling fruit speeds up (units per second, per second).")]
    [SerializeField] private float gravity = 30f;
    [Tooltip("Order in Layer while falling, so it draws over the canopy (17).")]
    [SerializeField] private int fallingOrder = 20;
    [Tooltip("Bounce height on landing, in units. 0 = no bounce.")]
    [SerializeField] private float bounce = 0.15f;
    [Tooltip("Sprite shown once a falling fruit lands (fruit_cut). Empty = keep the whole fruit.")]
    [SerializeField] private Sprite landedSprite;
    [Tooltip("Seconds on the ground before it fades away. 0 = stays forever.")]
    [SerializeField] private float lifetime = 0f;
    [Tooltip("Seconds the fade-out takes, at the end of the lifetime.")]
    [SerializeField] private float fadeTime = 1f;

    SpriteRenderer sr;
    Collider2D col;
    int groundOrder;
    float groundY, speed;
    bool falling, bounced;
    float groundTimer;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        col = GetComponent<Collider2D>();
        groundOrder = sr.sortingOrder;
    }

    // FruitTree calls this right after spawning it at the hanging fruit's spot.
    public void Fall(float landY)
    {
        groundY = landY;
        falling = true;
        col.enabled = false;   // crow can't eat it mid-air
        sr.sortingOrder = fallingOrder;
    }

    void Update()
    {
        if (!falling)
        {
            if (lifetime <= 0f) return;
            groundTimer += Time.deltaTime;
            float left = lifetime - groundTimer;
            if (left <= fadeTime)
            {
                Color c = sr.color;
                c.a = Mathf.Clamp01(left / fadeTime);
                sr.color = c;
            }
            if (left <= 0f) Destroy(gameObject);
            return;
        }
        speed += gravity * Time.deltaTime;
        Vector3 p = transform.position;
        p.y -= speed * Time.deltaTime;
        if (p.y <= groundY)
        {
            p.y = groundY;
            if (!bounced && bounce > 0f)
            {
                bounced = true;
                speed = -Mathf.Sqrt(2f * gravity * bounce);   // upward speed that reaches bounce height
            }
            else
            {
                falling = false;
                col.enabled = true;
                sr.sortingOrder = groundOrder;
                if (landedSprite != null) sr.sprite = landedSprite;   // splits open on the ground
            }
        }
        transform.position = p;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.TryGetComponent(out CrowEnergy crow)) return;
        crow.Eat(energy);
        Destroy(gameObject);
    }
}
