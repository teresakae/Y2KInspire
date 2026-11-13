using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerControl : MonoBehaviour
{
    public enum Controls { WASD, Arrows }
    public Controls controls;
    public float movSpeed;
    public Sprite front, back, left, right;
    public Sprite fly;               // shown while moving (optional)
    public GameObject flyShadow;     // shown while moving (optional)
    float speedX, speedY;
    Rigidbody2D rb;
    SpriteRenderer sr;
    Sprite lastFacing;
    Vector3 shadowOffset;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
     rb = GetComponent<Rigidbody2D>();
     sr = GetComponent<SpriteRenderer>();
     lastFacing = sr.sprite;
     if (flyShadow != null) shadowOffset = flyShadow.transform.position - transform.position;
    }

    // Update is called once per frame
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
        // Face the way we're moving; left/right wins on diagonals. Standing still keeps the last facing.
        Sprite facing = x > 0 ? right : x < 0 ? left : y > 0 ? back : y < 0 ? front : null;
        if (facing != null) lastFacing = facing;

        // Optional: while a key is held, show the flying sprite and the shadow. Leave both empty for no change.
        bool moving = x != 0 || y != 0;
        sr.sprite = moving && fly != null ? fly : lastFacing;
        if (flyShadow != null) flyShadow.SetActive(moving);

        // Flying: turn toward travel (sprite's top = forward). Stopped: back upright.
        bool flying = moving && fly != null;
        var turn = flying ? Quaternion.Euler(0, 0, Mathf.Atan2(y, x) * Mathf.Rad2Deg - 90) : Quaternion.identity;
        transform.rotation = Quaternion.RotateTowards(transform.rotation, turn, 900 * Time.deltaTime);

        // The shadow is a child, so undo the spin: keep it flat at its original offset.
        if (flyShadow != null)
        {
            flyShadow.transform.rotation = Quaternion.identity;
            flyShadow.transform.position = transform.position + shadowOffset;
        }

        speedX = x * movSpeed;
        speedY = y * movSpeed;
        rb.linearVelocity = new Vector2(speedX, speedY);
    }
}
