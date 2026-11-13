using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Moves one animal from the keyboard and swaps its sprite; the crow also flies, turns and shows a shadow.</summary>
public class PlayerControl : MonoBehaviour
{
    public enum Controls { WASD, Arrows }

    [Tooltip("Which keys move this animal.")]
    [SerializeField] private Controls controls;
    [Tooltip("Top speed in units per second.")]
    [SerializeField] private float movSpeed;
    [Tooltip("Facing sprites.")]
    [SerializeField] private Sprite front, back, left, right;
    [Tooltip("Shown while moving (optional). Leave empty for no change.")]
    [SerializeField] private Sprite fly;
    [Tooltip("Shown while moving (optional).")]
    [SerializeField] private GameObject flyShadow;
    [Tooltip("Walk frames while moving left (optional). Empty = plain facing sprite.")]
    [SerializeField] private Sprite[] walkLeft;
    [Tooltip("Walk frames while moving right (optional). Empty = plain facing sprite.")]
    [SerializeField] private Sprite[] walkRight;
    [Tooltip("Walk frames per second.")]
    [SerializeField] private float walkFps = 8f;
    [Tooltip("Rotation offset so the fly sprite's head points along travel. fly.png has its head at the bottom, so 90.")]
    [SerializeField] private float flyAngle = 90f;
    [Tooltip("Degrees per second the sprite turns toward travel.")]
    [SerializeField] private float turnRate = 900f;
    [Tooltip("Tired (speedScale below 1): sway side to side this many degrees instead of flying.")]
    [SerializeField] private float waddleAngle = 8f;
    [Tooltip("Waddle sway speed (radians per second).")]
    [SerializeField] private float waddleSpeed = 12f;

    // Set by CrowEnergy: 1 = normal, lower = tired.
    [HideInInspector] public float speedScale = 1f;
    // Last non-zero input direction. CowRam aims with it.
    [HideInInspector] public Vector2 lastDir = Vector2.down;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Sprite lastFacing;
    Vector3 shadowOffset;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        lastFacing = sr.sprite;
        if (flyShadow != null) shadowOffset = flyShadow.transform.position - transform.position;
    }

    void Update()
    {
        // ponytail: keyboard only; add gamepad or an Input Actions asset if needed
        var kb = Keyboard.current;
        float x = 0, y = 0;
        if (kb != null)
        {
            bool wasd = controls == Controls.WASD;
            x = ((wasd ? kb.dKey : kb.rightArrowKey).isPressed ? 1 : 0) - ((wasd ? kb.aKey : kb.leftArrowKey).isPressed ? 1 : 0);
            y = ((wasd ? kb.wKey : kb.upArrowKey).isPressed ? 1 : 0) - ((wasd ? kb.sKey : kb.downArrowKey).isPressed ? 1 : 0);
        }
        bool moving = x != 0 || y != 0;
        if (moving) lastDir = new Vector2(x, y);

        // Face the way we're moving; left/right wins on diagonals. Standing still keeps the last facing.
        Sprite facing = x > 0 ? right : x < 0 ? left : y > 0 ? back : y < 0 ? front : null;
        if (facing != null) lastFacing = facing;

        // Optional: while a key is held, show the flying sprite and the shadow. Leave both empty for no change.
        bool tired = speedScale < 1f;
        bool flying = moving && fly != null && !tired;
        Sprite[] walk = x > 0 ? walkRight : x < 0 ? walkLeft : null;
        if (flying) sr.sprite = fly;
        else if (walk != null && walk.Length > 0) sr.sprite = walk[(int)(Time.time * walkFps) % walk.Length];
        else sr.sprite = lastFacing;
        if (flyShadow != null) flyShadow.SetActive(moving && !tired);

        // Flying: turn toward travel. Tired and moving: waddle. Stopped: back upright.
        var turn = flying ? Quaternion.Euler(0, 0, Mathf.Atan2(y, x) * Mathf.Rad2Deg + flyAngle)
                 : tired && moving ? Quaternion.Euler(0, 0, Mathf.Sin(Time.time * waddleSpeed) * waddleAngle)
                 : Quaternion.identity;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, turn, turnRate * Time.deltaTime);

        // The shadow is a child, so undo the spin: keep it flat at its original offset.
        if (flyShadow != null)
        {
            flyShadow.transform.rotation = Quaternion.identity;
            flyShadow.transform.position = transform.position + shadowOffset;
        }

        rb.linearVelocity = new Vector2(x, y) * movSpeed * speedScale;
    }
}
