using UnityEngine;
using UnityEngine.InputSystem;

/// <summary>Moves one animal from the keyboard and swaps its sprite; the crow also flies, snaps to face its direction and shows a shadow.</summary>
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
    [Tooltip("Fly shadow size in world units: x = along the wings, y = across the body. Wingspan is about 1.8, so 1.45 is about 80%.")]
    [SerializeField] private Vector2 flyShadowSize = new Vector2(1.45f, 0.5f);
    [Tooltip("The always-on ground shadow child (optional). Hidden while the fly shadow shows, because as a child it would spin with the crow and slide to the side.")]
    [SerializeField] private GameObject groundShadow;
    [Tooltip("Walk frames while moving left (optional). Empty = plain facing sprite.")]
    [SerializeField] private Sprite[] walkLeft;
    [Tooltip("Walk frames while moving right (optional). Empty = plain facing sprite.")]
    [SerializeField] private Sprite[] walkRight;
    [Tooltip("Walk frames while moving up (optional). Empty = plain facing sprite.")]
    [SerializeField] private Sprite[] walkUp;
    [Tooltip("Walk frames while moving down (optional). Empty = plain facing sprite.")]
    [SerializeField] private Sprite[] walkDown;
    [Tooltip("Walk frames per second.")]
    [SerializeField] private float walkFps = 8f;
    [Tooltip("Rotation offset so the fly sprite's head points along travel. fly.png has its head at the bottom, so 90.")]
    [SerializeField] private float flyAngle = 90f;
    [Tooltip("Tired (speedScale below 1): sway side to side this many degrees instead of flying.")]
    [SerializeField] private float waddleAngle = 8f;
    [Tooltip("Waddle sway speed (radians per second).")]
    [SerializeField] private float waddleSpeed = 12f;
    [Tooltip("Optional. Emits only while the animal is flying (moving, not tired). Leave empty for none.")]
    [SerializeField] private ParticleSystem flyParticles;

    [Header("Sound")]
    [Tooltip("Looping movement sound (crow wing flaps). Ignored when Step Clips has entries. Empty = silent.")]
    [SerializeField] private AudioClip moveLoop;
    [Tooltip("Volume of the movement loop and of footsteps.")]
    [Range(0f, 1f)]
    [SerializeField] private float moveVolume = 0.5f;
    [Tooltip("Lowest random pitch each time the loop starts.")]
    [SerializeField] private float pitchMin = 0.9f;
    [Tooltip("Highest random pitch each time the loop starts.")]
    [SerializeField] private float pitchMax = 1.1f;
    [Tooltip("Footstep clips (cow). Played in turn, one per Step Interval while walking. Empty = use the loop instead.")]
    [SerializeField] private AudioClip[] stepClips;
    [Tooltip("Seconds between footsteps.")]
    [SerializeField] private float stepInterval = 0.3f;
    [Tooltip("Footstep pitch varies by this much either way around 1.")]
    [SerializeField] private float stepPitchJitter = 0.05f;
    [Tooltip("Seconds the movement loop takes to fade in and out, so short stops don't chop it.")]
    [SerializeField] private float loopFadeTime = 0.15f;

    // Set by CrowEnergy: 1 = normal, lower = tired.
    [HideInInspector] public float speedScale = 1f;
    // Last non-zero input direction. CowRam aims with it.
    [HideInInspector] public Vector2 lastDir = Vector2.down;
    // Set by CowRam while charging: keys only turn lastDir and the facing sprite, the animal stays put.
    [HideInInspector] public bool aimOnly;

    Rigidbody2D rb;
    SpriteRenderer sr;
    Sprite lastFacing;
    Vector3 shadowOffset;
    AudioSource loopSource;
    bool loopOn;        // whether the loop should be audible; the fade follows it
    float stepTimer;    // counts down to the next footstep; 0 = step now
    int nextStep;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        lastFacing = sr.sprite;
        // Its own source: stopping or pausing it must not cut the one-shot sounds CowRam/CrowEnergy play on the first source.
        loopSource = gameObject.AddComponent<AudioSource>();
        loopSource.loop = true;
        loopSource.playOnAwake = false;
        loopSource.clip = moveLoop;
        loopSource.volume = 0f;
        AudioSource own = GetComponent<AudioSource>();   // the mixer group, if one is set on the animal's own source
        if (own != null) loopSource.outputAudioMixerGroup = own.outputAudioMixerGroup;
        if (flyShadow != null)
        {
            shadowOffset = flyShadow.transform.position - transform.position;
            // Scale so the shadow comes out at Fly Shadow Size in the world, whatever the sprite and parent scale are.
            Vector3 sprite = flyShadow.GetComponent<SpriteRenderer>().sprite.bounds.size;
            Vector3 parent = transform.lossyScale;
            flyShadow.transform.localScale = new Vector3(flyShadowSize.x / (sprite.x * parent.x), flyShadowSize.y / (sprite.y * parent.y), 1f);
        }
    }

    void Update()
    {
        FadeLoop();
        if (Time.timeScale == 0f) { SetSound(false); return; } // paused
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
        if (aimOnly)
        {
            sr.sprite = lastFacing;
            SetSound(false);
            return;
        }

        // Optional: while a key is held, show the flying sprite and the shadow. Leave both empty for no change.
        bool tired = speedScale < 1f;
        SetSound(moving && !tired);
        bool flying = moving && fly != null && !tired;
        Sprite[] walk = x > 0 ? walkRight : x < 0 ? walkLeft : y > 0 ? walkUp : y < 0 ? walkDown : null;
        if (flyParticles != null)
        {
            var em = flyParticles.emission;
            em.enabled = flying;
        }
        if (flying) sr.sprite = fly;
        else if (walk != null && walk.Length > 0) sr.sprite = walk[(int)(Time.time * walkFps) % walk.Length];
        else sr.sprite = lastFacing;
        if (flyShadow != null) flyShadow.SetActive(moving && !tired);
        if (groundShadow != null) groundShadow.SetActive(!(moving && !tired) || flyShadow == null);

        // Flying: snap to right/left/up/down (left/right wins on diagonals, like the facing sprite). Tired and moving: waddle. Stopped: upright.
        float heading = x > 0 ? 0f : x < 0 ? 180f : y > 0 ? 90f : -90f;
        transform.rotation = flying ? Quaternion.Euler(0, 0, heading + flyAngle)
                           : tired && moving ? Quaternion.Euler(0, 0, Mathf.Sin(Time.time * waddleSpeed) * waddleAngle)
                           : Quaternion.identity;

        // The shadow is a child: keep it straight below the crow (world offset, not rotated with it), but turn it with the crow so its long side follows the wings.
        if (flyShadow != null)
        {
            flyShadow.transform.rotation = transform.rotation;
            flyShadow.transform.position = transform.position + shadowOffset;
        }

        rb.linearVelocity = new Vector2(x, y) * movSpeed * speedScale;
    }

    void OnDisable()
    {
        // Drown disables this script while the animal may still be "moving": cut at once, no fade.
        loopOn = false;
        stepTimer = 0f;
        if (loopSource != null) { loopSource.Stop(); loopSource.volume = 0f; }
    }

    // Cow: a footstep every Step Interval, the first one the moment walking starts. Crow: the loop fades in or out.
    void SetSound(bool walking)
    {
        if (loopSource == null) return;
        if (stepClips == null || stepClips.Length == 0) { loopOn = walking; return; }
        if (!walking) { stepTimer = 0f; return; }
        stepTimer -= Time.deltaTime;
        if (stepTimer > 0f) return;
        stepTimer = stepInterval;
        loopSource.pitch = 1f + Random.Range(-stepPitchJitter, stepPitchJitter);
        loopSource.PlayOneShot(stepClips[nextStep++ % stepClips.Length], moveVolume);
    }

    // Fades toward Move Volume or 0. Only a fully stopped loop restarts (new pitch, random start), so brief stops don't retrigger the clip.
    void FadeLoop()
    {
        if (moveLoop == null || loopSource == null || (stepClips != null && stepClips.Length > 0)) return;
        if (loopOn && !loopSource.isPlaying)
        {
            loopSource.pitch = Random.Range(pitchMin, pitchMax);
            loopSource.time = Random.value * moveLoop.length * 0.99f;   // random start so two animals don't step in sync
            loopSource.volume = 0f;
            loopSource.Play();
        }
        float goal = loopOn ? moveVolume : 0f;
        loopSource.volume = Mathf.MoveTowards(loopSource.volume, goal, moveVolume / loopFadeTime * Time.unscaledDeltaTime);
        if (!loopOn && loopSource.volume <= 0f && loopSource.isPlaying) loopSource.Stop();
    }
}
