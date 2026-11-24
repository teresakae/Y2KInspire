using UnityEngine;

// Fog = one texture the crow paints round holes into. Which cells block the cow lives in a bool grid (the truth);
// only a small pool of invisible colliders around the cow mirrors it, because only the cow collides with fog
// and 15k real colliders cost seconds in Start.
// Fog the crow cleared grows back after regrowDelay (plus a patchy extra wait and a patchy speed, so it creeps back in blotches),
// except start clearings and where the cow stands.
// ponytail: rows of fog with nothing cleared in them are skipped, but the whole texture (~300k pixels) is still re-uploaded whenever anything changed. Move regrowth to a shader / render texture if maps get bigger.
// Optional Fog Material (e.g. M_FogSwirl) animates the look in a shader; gameplay never reads it.
// Cloud look: puff textures (white-on-transparent or grey-on-black; shape = brightness x alpha) are stamped at random
// once in Start into a brightness map, so it costs nothing per frame.
public class FogGrid : MonoBehaviour
{
    [Tooltip("Clears fog as it moves.")]
    [SerializeField] private Transform crow;
    [Tooltip("Pre-cleared circles; diameter = the object's X scale.")]
    [SerializeField] private Transform[] openAtStart;
    [Tooltip("Width and height of the fogged area, in units.")]
    [SerializeField] private Vector2 areaSize = new Vector2(20, 12);
    [Tooltip("Radius the crow clears around itself.")]
    [SerializeField] private float clearRadius = 0.75f;
    [Tooltip("Shape of the crow's hole (and start clearings), same format as the puffs; Read/Write ticked. Its width = 2 x the radius. Required.")]
    [SerializeField] private Texture2D clearShape;
    [Tooltip("How much fog is removed (up) from the hole's centre (left, 0) to its edge (right, 1). Up 1 = fully clear, 0 = untouched. A low step near the right leaves thin fog in the outer ring. The cow only walks where this is above 0.5 (Reveal Threshold).")]
    [SerializeField] private AnimationCurve clearFalloff = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 1f), new Keyframe(0.7f, 0.6f), new Keyframe(0.85f, 0.3f), new Keyframe(1f, 0f));
    [Tooltip("How far the hole's edge wobbles in and out, in units. 0 = perfect circle.")]
    [SerializeField] private float edgeNoise = 0.4f;
    [Tooltip("Wobbles per unit along the edge. Higher = finer, wispier edge.")]
    [SerializeField] private float edgeNoiseScale = 2f;
    [Tooltip("How fine the fog looks.")]
    [SerializeField] private int pixelsPerUnit = 16;
    [Tooltip("How fine the blocking is.")]
    [SerializeField] private float cellSize = 0.25f;
    [Tooltip("Units either side of the cow that get real colliders. Must be more than the cow's half width plus one dash step, or it can slip into fog.")]
    [SerializeField] private float cowWallReach = 2f;
    [SerializeField] private Color fogColor = new Color(0.29f, 0.33f, 0.16f);
    [Tooltip("White-on-transparent or grey-on-black cloud images (shape = brightness x alpha) stamped across the fog for its look. Need Read/Write ticked in import settings. Empty = flat colour.")]
    [SerializeField] private Texture2D[] puffTextures;
    [Tooltip("How many puffs are stamped.")]
    [SerializeField] private int puffCount = 40;
    [Tooltip("Puff size in units, min (x) to max (y).")]
    [SerializeField] private Vector2 puffSizeRange = new Vector2(3f, 6f);
    [Tooltip("Brightness of the thinnest fog, relative to Fog Color. Lower = stronger cloud shapes. Fog stays opaque either way.")]
    [Range(0f, 1f)]
    [SerializeField] private float thinShade = 0.7f;
    [Tooltip("See-through-ness of the thinnest fog, relative to Fog Color's alpha. 1 = even thickness everywhere; lower = gaps between puffs show the ground faintly. Looks only: the cow is still blocked there.")]
    [Range(0f, 1f)]
    [SerializeField] private float thinAlpha = 1f;
    [Tooltip("Softens the cloud shapes, in units. Higher = puffs melt into each other with no visible puff edges. 0 = sharp.")]
    [SerializeField] private float lookBlur = 0f;
    [Tooltip("Fog within this many units of an open-at-start spot's edge stays at full thickness, so the starting clearing's border reads clearly. 0 = off.")]
    [SerializeField] private float thickBorder = 0f;
    [Tooltip("Change for a different random puff layout.")]
    [SerializeField] private int seed = 0;
    [Tooltip("Things below this order hide under the fog (snakes).")]
    [SerializeField] private int sortingOrder = 10;
    [Tooltip("Optional material for the fog (e.g. M_FogSwirl for drifting, wispy fog). Empty = plain sprite look.")]
    [SerializeField] private Material fogMaterial;
    [Tooltip("Fog never regrows under the cow.")]
    [SerializeField] private Transform cow;
    [Tooltip("Cow haze (needs the fog shader): fog gets thinner in a soft oval around the cow's body so its head shows through when it stands at the fog's edge. Looks only. Oval centre above the cow's feet, in units.")]
    [SerializeField] private float cowHazeHeight = 1.2f;
    [Tooltip("Half width and half height of the haze oval, in units.")]
    [SerializeField] private Vector2 cowHazeRadius = new Vector2(1.1f, 1.3f);
    [Tooltip("How much of the oval's radius is the soft fade at its rim. 1 = fades from the centre, 0 = hard rim.")]
    [Range(0f, 1f)]
    [SerializeField] private float cowHazeSoftness = 0.6f;
    [Tooltip("Fog left at the oval's centre. 1 = no haze (off), 0 = fully clear, 0.35 = head lightly hazy.")]
    [Range(0f, 1f)]
    [SerializeField] private float cowHazeAmount = 0.35f;
    [Tooltip("Direction and strength of the fog wind, in units per second. The fog shader drift and the swaying grass both follow it. (0, 0) = no wind: grass still sways gently, fog drifts only by its own material scroll.")]
    [SerializeField] private Vector2 wind = new Vector2(0.1f, 0.02f);
    [Tooltip("Seconds after the crow last cleared a spot before it starts regrowing.")]
    [SerializeField] private float regrowDelay = 5f;
    [Tooltip("Fraction of full fog regrown per second.")]
    [SerializeField] private float regrowSpeed = 0.5f;
    [Tooltip("Extra wait, up to this many seconds, that varies in soft patches. Fog creeps back in blotches instead of the whole trail fading at once. 0 = all at once.")]
    [SerializeField] private float regrowJitter = 3f;
    [Tooltip("Size of the regrow patches. Lower = bigger blotches.")]
    [SerializeField] private float regrowNoiseScale = 0.8f;
    [Tooltip("How much regrow speed varies from patch to patch. 0 = every patch refills at the same speed. 1 = the fastest patches refill about 3 times faster than the slowest.")]
    [Range(0f, 1f)]
    [SerializeField] private float regrowSpeedJitter = 0.5f;
    [Tooltip("Boxes where fog regrows fast (e.g. around a snake's den). Each object's position = box centre, its X and Y scale = box width and height.")]
    [SerializeField] private Transform[] fastRegrowZones;
    [Tooltip("Seconds before fog inside a fast zone starts regrowing (replaces Regrow Delay there).")]
    [SerializeField] private float fastRegrowDelay = 1f;
    [Tooltip("Fraction of full fog regrown per second inside a fast zone (replaces Regrow Speed there).")]
    [SerializeField] private float fastRegrowSpeed = 1.5f;
    [SerializeField] private float cowSafeRadius = 0.75f;
    [Tooltip("Width, in units, of the soft ring just outside Cow Safe Radius where fog only regrows part of the way. Stops a hard line where the safe bubble ends. 0 = hard edge.")]
    [SerializeField] private float cowSafeFade = 1f;
    [Tooltip("A spot counts as revealed when its fog is below this (0 = clear, 1 = full fog).")]
    [Range(0f, 1f)]
    [SerializeField] private float revealThreshold = 0.5f;

    Collider2D cowCollider;
    Texture2D tex;
    Color32[] pixels;
    BoxCollider2D[,] pool;                   // N x N colliders that follow the cow
    int[,] poolX, poolY;                     // which grid cell each pool slot currently holds
    bool[,] poolOn;                          // slot's collider state, so only changes touch native objects
    Vector2 origin;                          // bottom-left corner of the fog, world space
    bool dirty;
    bool[] rowOpen;                          // rows that have cleared fog: regrowth skips the rest
    bool[] rowDirty;                         // rows whose alpha must be rebuilt
    bool[,] blocking;                        // which cells block the cow, whole grid; the pool colliders copy it near the cow
    Vector2 lastClearPos;
    float nextClear;
    const int PuffMapSize = 128;             // puff brightness is read once into a 128x128 map: GetPixelBilinear per pixel was the Start hot spot
    const float HoverRefresh = 0.25f;        // seconds; must stay below every regrow delay
    float[] fog;                             // 0..1 per pixel, source of truth (bytes are too coarse for slow regrowth)
    float[] lastCleared;                     // Time.time each pixel was last under the crow
    float[] restFog;                         // fog after the start clearings: regrowth stops here
    bool[,] restEnabled;                     // cells that were blocking after the start clearings
    float clearShapeMax;                     // brightest spot of clearShape, so a faint image still clears fully
    float[] maxAlpha;                        // full-fog alpha per pixel (thin puff gaps are lower)
    float[] waitTime;                        // regrowDelay + this pixel's patchy extra wait
    bool[] inFastZone;                       // pixel centre is inside a fast regrow zone, worked out once in Start
    float[] speedMul;                        // this pixel's regrow speed multiplier (around 1)

    void Start()
    {
        origin = (Vector2)transform.position - areaSize / 2f;
        if (cow != null) cowCollider = cow.GetComponent<Collider2D>();
        if (clearShape == null || !clearShape.isReadable)
        {
            Debug.LogError($"{name}: missing Clear Shape, or it needs Read/Write ticked");
            enabled = false;
            return;
        }
        clearShapeMax = Brightest(clearShape);

        // Visual: one texture, fully fogged.
        int w = Mathf.CeilToInt(areaSize.x * pixelsPerUnit);
        int h = Mathf.CeilToInt(areaSize.y * pixelsPerUnit);
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        pixels = new Color32[w * h];
        rowOpen = new bool[h];
        rowDirty = new bool[h];
        float[] look = BuildLook(w, h);
        // Read the Transforms once: .position per pixel is a native call.
        var spots = new System.Collections.Generic.List<Vector3>();   // x, y, radius
        if (openAtStart != null)
            foreach (var s in openAtStart)
                if (s != null) spots.Add(new Vector3(s.position.x, s.position.y, s.lossyScale.x / 2f));
        var zones = new System.Collections.Generic.List<Vector4>();   // centre x, centre y, half width, half height
        if (fastRegrowZones != null)
            foreach (var z in fastRegrowZones)
                if (z != null) zones.Add(new Vector4(z.position.x, z.position.y, z.lossyScale.x / 2f, z.lossyScale.y / 2f));
        maxAlpha = new float[pixels.Length];
        waitTime = new float[pixels.Length];
        speedMul = new float[pixels.Length];
        inFastZone = new bool[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = fogColor * Mathf.Lerp(thinShade, 1f, look[i]);
            c.a = fogColor.a;
            pixels[i] = c;
            maxAlpha[i] = fogColor.a * Mathf.Lerp(thinAlpha, 1f, look[i]) * 255f;
            Vector2 p = new Vector2(i % w, i / w) / pixelsPerUnit;
            if (thickBorder > 0f && spots.Count > 0)
            {
                // Distance outside the nearest start clearing's circle: thin fog there reads like a path, so keep it full.
                Vector2 world = origin + p + Vector2.one * (0.5f / pixelsPerUnit);
                float outside = 1000f;
                foreach (var s in spots)
                    outside = Mathf.Min(outside, Vector2.Distance(world, new Vector2(s.x, s.y)) - s.z);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, outside) / thickBorder);
                maxAlpha[i] = Mathf.Lerp(fogColor.a * 255f, maxAlpha[i], k);
            }
            if (zones.Count > 0)
            {
                Vector2 world = origin + p + Vector2.one * (0.5f / pixelsPerUnit);
                foreach (var z in zones)
                    if (Mathf.Abs(world.x - z.x) <= z.z && Mathf.Abs(world.y - z.y) <= z.w)
                        inFastZone[i] = true;
            }
            waitTime[i] = (inFastZone[i] ? fastRegrowDelay : regrowDelay) + Mathf.PerlinNoise(p.x * regrowNoiseScale + 50f, p.y * regrowNoiseScale + 50f) * regrowJitter;   // +50: Perlin repeats near 0
            float speedNoise = Mathf.PerlinNoise(p.x * regrowNoiseScale + 200f, p.y * regrowNoiseScale + 200f);   // other offset: slow patches differ from late patches
            speedMul[i] = Mathf.Lerp(1f - regrowSpeedJitter * 0.5f, 1f + regrowSpeedJitter * 0.5f, speedNoise);
        }
        this.fog = new float[pixels.Length];
        lastCleared = new float[pixels.Length];
        for (int i = 0; i < this.fog.Length; i++) { this.fog[i] = 1f; lastCleared[i] = -1000f; }

        var visual = new GameObject("Fog Visual");
        visual.transform.SetParent(transform, false);
        visual.transform.position = origin;
        var sr = visual.AddComponent<SpriteRenderer>();
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.zero, pixelsPerUnit, 0, SpriteMeshType.FullRect);   // Tight would trace an outline over 300k pixels
        sr.sortingOrder = sortingOrder;
        if (fogMaterial != null) sr.sharedMaterial = fogMaterial;

        // Blocking: a pool of box colliders that FixedUpdate moves to the cells around the cow, on a child so the Fog object's Inspector stays light.
        var blockers = new GameObject("Fog Colliders");
        blockers.layer = gameObject.layer;
        blockers.transform.SetParent(transform, false);
        blockers.transform.position = origin;
        int cols = Mathf.CeilToInt(areaSize.x / cellSize);
        int rows = Mathf.CeilToInt(areaSize.y / cellSize);
        blocking = new bool[cols, rows];
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
                blocking[x, y] = true;
        int n = 2 * Mathf.CeilToInt(cowWallReach / cellSize) + 1;
        pool = new BoxCollider2D[n, n];
        poolX = new int[n, n];
        poolY = new int[n, n];
        poolOn = new bool[n, n];
        for (int i = 0; i < n; i++)
            for (int j = 0; j < n; j++)
            {
                var box = blockers.AddComponent<BoxCollider2D>();
                box.size = Vector2.one * cellSize;
                box.enabled = false;
                pool[i, j] = box;
                poolX[i, j] = -1;
                poolY[i, j] = -1;
            }

        if (openAtStart != null)
            foreach (var c in openAtStart)
                if (c != null) Clear(c.position, c.lossyScale.x / 2f, true);

        // Snapshot: start clearings are the "rest" state and never regrow.
        restFog = (float[])this.fog.Clone();
        restEnabled = new bool[cols, rows];
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
                restEnabled[x, y] = blocking[x, y];

        for (int y = 0; y < h; y++) rowDirty[y] = true;
        dirty = true;
    }

    // Runs just before the physics step: park pool colliders on the blocking cells around the cow.
    // Cell (x,y) always uses slot (x mod N, y mod N), so a collider only moves when its cell leaves the window
    // and the ones touching the cow never move.
    void FixedUpdate()
    {
        if (cow == null) return;
        Vector2 c = cowCollider != null ? (Vector2)cowCollider.bounds.center : (Vector2)cow.position;
        int cx = Mathf.FloorToInt((c.x - origin.x) / cellSize);
        int cy = Mathf.FloorToInt((c.y - origin.y) / cellSize);
        int n = pool.GetLength(0);
        int reach = n / 2;
        for (int x = cx - reach; x <= cx + reach; x++)
            for (int y = cy - reach; y <= cy + reach; y++)
            {
                int i = ((x % n) + n) % n;
                int j = ((y % n) + n) % n;
                bool want = x >= 0 && y >= 0 && x < blocking.GetLength(0) && y < blocking.GetLength(1) && blocking[x, y];
                if (want && (poolX[i, j] != x || poolY[i, j] != y))
                {
                    pool[i, j].offset = new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                    poolX[i, j] = x;
                    poolY[i, j] = y;
                }
                if (want != poolOn[i, j])
                {
                    pool[i, j].enabled = want;
                    poolOn[i, j] = want;
                }
            }
    }

    void Update()
    {
        // Haze for the fog shader. No cow = zero radius = effect off. Globals, so no material instance is made.
        Vector2 hazeCenter = cow != null ? (Vector2)cow.position + Vector2.up * cowHazeHeight : Vector2.zero;
        Vector2 hazeSize = cow != null ? cowHazeRadius : Vector2.zero;
        Shader.SetGlobalVector("_CowHazeOval", new Vector4(hazeCenter.x, hazeCenter.y, hazeSize.x, hazeSize.y));
        Shader.SetGlobalVector("_CowHazeLook", new Vector4(cowHazeSoftness, cowHazeAmount, 0f, 0f));
        // One wind for everything that drifts: the fog shader and the grass shader both read this global.
        Shader.SetGlobalVector("_FogWind", wind);
        // Hovering clears nothing new; it only has to keep the lastCleared timers fresh.
        if (crow != null && ((Vector2)crow.position != lastClearPos || Time.time >= nextClear))
        {
            Clear(crow.position, clearRadius);
            lastClearPos = crow.position;
            nextClear = Time.time + HoverRefresh;
        }
        Regrow();
        if (dirty)
        {
            int w = tex.width;
            for (int y = 0; y < tex.height; y++)
            {
                if (!rowDirty[y]) continue;
                rowDirty[y] = false;
                for (int i = y * w; i < (y + 1) * w; i++) pixels[i].a = (byte)(fog[i] * maxAlpha[i]);
            }
            tex.SetPixels32(pixels);
            tex.Apply();
            dirty = false;
        }
    }

    // Stamps random puffs into a 0..1 brightness map, brightest stamp wins.
    float[] BuildLook(int w, int h)
    {
        var look = new float[w * h];
        if (puffTextures == null || puffTextures.Length == 0) return look;
        var oldState = Random.state;
        Random.InitState(seed);
        var maps = new float[puffTextures.Length][];   // brightness map per puff texture, built on first use
        for (int n = 0; n < puffCount; n++)
        {
            int ti = Random.Range(0, puffTextures.Length);
            Texture2D t = puffTextures[ti];
            if (t == null) continue;
            if (!t.isReadable) { Debug.LogError($"{name}: puff texture {t.name} needs Read/Write ticked"); break; }
            if (maps[ti] == null) maps[ti] = BrightnessMap(t);
            float[] map = maps[ti];
            Vector2 c = new Vector2(Random.Range(0f, w), Random.Range(0f, h));
            float size = Random.Range(puffSizeRange.x, puffSizeRange.y) * pixelsPerUnit;
            float a = Random.Range(0f, 360f) * Mathf.Deg2Rad;
            float cos = Mathf.Cos(a), sin = Mathf.Sin(a);
            int r = Mathf.CeilToInt(size * 0.71f);   // half the diagonal, so rotated corners fit
            for (int y = Mathf.Max(0, (int)c.y - r); y < Mathf.Min(h, (int)c.y + r); y++)
                for (int x = Mathf.Max(0, (int)c.x - r); x < Mathf.Min(w, (int)c.x + r); x++)
                {
                    float dx = x - c.x, dy = y - c.y;
                    float u = (dx * cos + dy * sin) / size + 0.5f;
                    float v = (-dx * sin + dy * cos) / size + 0.5f;
                    if (u < 0f || u > 1f || v < 0f || v > 1f) continue;
                    float b = map[(int)(v * (PuffMapSize - 1) + 0.5f) * PuffMapSize + (int)(u * (PuffMapSize - 1) + 0.5f)];
                    int i = y * w + x;
                    if (b > look[i]) look[i] = b;
                }
        }
        Random.state = oldState;   // don't disturb other scripts' randomness
        int blurPx = Mathf.RoundToInt(lookBlur * pixelsPerUnit);
        if (blurPx > 0) Blur(look, w, h, blurPx);
        return look;
    }

    // Puff shape as brightness x alpha, scaled so the image's brightest spot is 1 (a dim image and a bright image both reach full shade).
    float[] BrightnessMap(Texture2D t)
    {
        var map = new float[PuffMapSize * PuffMapSize];
        float max = 0.01f;
        for (int y = 0; y < PuffMapSize; y++)
            for (int x = 0; x < PuffMapSize; x++)
            {
                Color px = t.GetPixelBilinear(x / (PuffMapSize - 1f), y / (PuffMapSize - 1f));
                float v = px.grayscale * px.a;
                map[y * PuffMapSize + x] = v;
                max = Mathf.Max(max, v);
            }
        for (int i = 0; i < map.Length; i++) map[i] = Mathf.Clamp01(map[i] / max);
        return map;
    }

    // Box blur along rows, then columns, twice, so puff edges fade smoothly. Runs once in Start.
    void Blur(float[] a, int w, int h, int r)
    {
        var tmp = new float[a.Length];
        for (int pass = 0; pass < 2; pass++)
        {
            // Running sum: slide the window one step by adding the entering pixel and dropping the leaving one.
            for (int y = 0; y < h; y++)
            {
                int row = y * w;
                float sum = 0f;
                for (int k = 0; k <= Mathf.Min(w - 1, r); k++) sum += a[row + k];
                for (int x = 0; x < w; x++)
                {
                    tmp[row + x] = sum / (Mathf.Min(w - 1, x + r) - Mathf.Max(0, x - r) + 1);
                    if (x + r + 1 <= w - 1) sum += a[row + x + r + 1];
                    if (x - r >= 0) sum -= a[row + x - r];
                }
            }
            for (int x = 0; x < w; x++)
            {
                float sum = 0f;
                for (int k = 0; k <= Mathf.Min(h - 1, r); k++) sum += tmp[k * w + x];
                for (int y = 0; y < h; y++)
                {
                    a[y * w + x] = sum / (Mathf.Min(h - 1, y + r) - Mathf.Max(0, y - r) + 1);
                    if (y + r + 1 <= h - 1) sum += tmp[(y + r + 1) * w + x];
                    if (y - r >= 0) sum -= tmp[(y - r) * w + x];
                }
            }
        }
    }

    // Coarse 64x64 scan is enough to find the peak of a blurry cloud.
    float Brightest(Texture2D t)
    {
        float max = 0.01f;
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
            {
                Color px = t.GetPixelBilinear(x / 63f, y / 63f);
                max = Mathf.Max(max, px.grayscale * px.a);
            }
        return max;
    }

    // Snakes ask this: hidden ones stay asleep.
    public bool IsClear(Vector2 pos)
    {
        if (fog == null) return false;   // before Start everything is fogged
        int x = Mathf.FloorToInt((pos.x - origin.x) * pixelsPerUnit);
        int y = Mathf.FloorToInt((pos.y - origin.y) * pixelsPerUnit);
        if (x < 0 || y < 0 || x >= tex.width || y >= tex.height) return true;   // no fog outside the area
        return fog[y * tex.width + x] < revealThreshold;
    }

    // round = a plain wobbly circle (start clearings); otherwise the clearShape cloud stamp (the crow).
    void Clear(Vector2 center, float radius, bool round = false)
    {
        // Fog pixels: stamp clearShape. The rim is pushed in and out by Perlin noise fixed in world space, so edges look ragged, not round.
        float reach = radius + edgeNoise;
        float now = Time.time;
        int x0 = Mathf.Max(0, Mathf.FloorToInt((center.x - reach - origin.x) * pixelsPerUnit));
        int x1 = Mathf.Min(tex.width - 1, Mathf.CeilToInt((center.x + reach - origin.x) * pixelsPerUnit));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((center.y - reach - origin.y) * pixelsPerUnit));
        int y1 = Mathf.Min(tex.height - 1, Mathf.CeilToInt((center.y + reach - origin.y) * pixelsPerUnit));
        // Turn the cloud stamp by a noise angle that depends on where it lands. An identical stamp slid along a path
        // lines its widest side up into a dead-straight edge; a turning one gives bumpy edges on both sides.
        float turn = Mathf.PerlinNoise(center.x * 1.5f + 300f, center.y * 1.5f + 300f) * 360f * Mathf.Deg2Rad;
        float cos = Mathf.Cos(turn), sin = Mathf.Sin(turn);
        for (int y = y0; y <= y1; y++)
        {
            rowOpen[y] = true;
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = origin + new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit;
                int i = y * tex.width + x;
                float t = ShapeFog(p, center, radius, cos, sin, round);
                if (t < fog[i]) { fog[i] = t; rowDirty[y] = true; dirty = true; }
                if (t < 1f) lastCleared[i] = now;     // anywhere inside the circle: keep it clear while the crow stays
            }
        }

        // Colliders: switch off every cell whose centre now looks clear, so blocking follows the ragged edge.
        int cx0 = Mathf.Max(0, Mathf.FloorToInt((center.x - reach - origin.x) / cellSize));
        int cx1 = Mathf.Min(blocking.GetLength(0) - 1, Mathf.FloorToInt((center.x + reach - origin.x) / cellSize));
        int cy0 = Mathf.Max(0, Mathf.FloorToInt((center.y - reach - origin.y) / cellSize));
        int cy1 = Mathf.Min(blocking.GetLength(1) - 1, Mathf.FloorToInt((center.y + reach - origin.y) / cellSize));
        for (int x = cx0; x <= cx1; x++)
            for (int y = cy0; y <= cy1; y++)
            {
                Vector2 c = origin + new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                if (blocking[x, y] && IsClear(c))
                {
                    blocking[x, y] = false;
                }
            }
    }

    // Fog left at world point p when stamping clearShape at center: 0 where the image is solid, 1 outside it.
    // clearFalloff thins the clearing toward the radius, so outer rings keep some fog (it must reach 0 at the edge,
    // because the image's cloud touches its borders and would leave straight edges).
    // Wobble pushes the rings in and out, so the trail's edge is ragged instead of a smooth brush stroke.
    float ShapeFog(Vector2 p, Vector2 center, float radius, float cos, float sin, bool round)
    {
        Vector2 d = p - center;
        float dist = d.magnitude;
        if (dist >= radius) return 1f;
        if (round) return 1f - Mathf.Clamp01(clearFalloff.Evaluate((dist + Wobble(p)) / radius));   // no image: same reach in every direction
        float u = (d.x * cos + d.y * sin) / (2f * radius) + 0.5f;
        float v = (-d.x * sin + d.y * cos) / (2f * radius) + 0.5f;
        Color c = clearShape.GetPixelBilinear(u, v);
        float a = Mathf.Clamp01(c.grayscale * c.a / clearShapeMax) * Mathf.Clamp01(clearFalloff.Evaluate((dist + Wobble(p)) / radius));
        return 1f - a;
    }

    // -edgeNoise..+edgeNoise, smooth and fixed per world spot. Added to a distance, it bends circles into ragged blobs.
    float Wobble(Vector2 p)
    {
        return (Mathf.PerlinNoise(p.x * edgeNoiseScale + 100f, p.y * edgeNoiseScale + 100f) - 0.5f) * 2f * edgeNoise;   // +100: Perlin repeats near 0
    }

    void Regrow()
    {
        float grow = regrowSpeed * Time.deltaTime;
        float fastGrow = fastRegrowSpeed * Time.deltaTime;
        int w = tex.width;
        float now = Time.time;
        Vector2 cowPos = cow != null ? (Vector2)cow.position : Vector2.zero;

        // Fog pixels: fade back in towards the rest state, but not in the cow's ragged safe zone.
        for (int y = 0; y < tex.height; y++)
        {
            if (!rowOpen[y]) continue;
            bool open = false;
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (fog[i] >= restFog[i]) continue;
                open = true;
                if (now - lastCleared[i] < waitTime[i]) continue;
                float target = restFog[i];
                if (cow != null)
                {
                    Vector2 p = origin + new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit;
                    float d = Vector2.Distance(cowPos, p);
                    if (d <= cowSafeRadius + cowSafeFade + edgeNoise)   // first test skips the noise for far pixels
                    {
                        // 0 inside the safe bubble, rising to 1 over cowSafeFade: fog near the edge only regrows partway, so no hard line.
                        float k = Mathf.Clamp01((d + Wobble(p) - cowSafeRadius) / Mathf.Max(cowSafeFade, 0.001f));
                        target = restFog[i] * Mathf.SmoothStep(0f, 1f, k);
                    }
                }
                if (fog[i] >= target) continue;
                fog[i] = Mathf.Min(target, fog[i] + (inFastZone[i] ? fastGrow : grow) * speedMul[i]);
                rowDirty[y] = true;
                dirty = true;
            }
            rowOpen[y] = open;
        }

        // Colliders: re-enable a cell only if it was blocking at rest and its centre pixel is fully fogged
        // again (never true inside the safe zone, so blocking follows its ragged edge).
        // Also never re-enable a cell touching the cow's own collider, whatever its size.
        Bounds cowBox = cowCollider != null ? cowCollider.bounds : default;
        cowBox.Expand(cellSize);
        for (int x = 0; x < blocking.GetLength(0); x++)
            for (int y = 0; y < blocking.GetLength(1); y++)
            {
                if (blocking[x, y] || !restEnabled[x, y]) continue;
                Vector2 c = origin + new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                if (cowCollider != null && cowBox.Contains(new Vector3(c.x, c.y, cowBox.center.z))) continue;
                int px = Mathf.Clamp(Mathf.FloorToInt((c.x - origin.x) * pixelsPerUnit), 0, w - 1);
                int py = Mathf.Clamp(Mathf.FloorToInt((c.y - origin.y) * pixelsPerUnit), 0, tex.height - 1);
                int i = py * w + px;
                if (fog[i] >= restFog[i])
                {
                    blocking[x, y] = true;
                }
            }
    }

    void OnDrawGizmos()
    {
        // Show start clearings in the Scene view so levels can be laid out by eye.
        Gizmos.color = Color.cyan;
        if (openAtStart != null)
            foreach (var c in openAtStart)
                if (c != null) Gizmos.DrawWireSphere(c.position, c.lossyScale.x / 2f);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.white;
        Gizmos.DrawWireCube(transform.position, new Vector3(areaSize.x, areaSize.y, 0));
        Gizmos.color = Color.yellow;
        if (fastRegrowZones != null)
            foreach (var z in fastRegrowZones)
                if (z != null) Gizmos.DrawWireCube(z.position, new Vector3(z.lossyScale.x, z.lossyScale.y, 0));
    }
}
