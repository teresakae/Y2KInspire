using UnityEngine;

// Fog = one texture the crow paints round holes into, plus a grid of invisible colliders
// that block the cow until the crow clears them.
// Fog the crow cleared grows back after regrowDelay (plus a patchy extra wait and a patchy speed, so it creeps back in blotches),
// except start clearings and where the cow stands.
// ponytail: every pixel (~60k) and cell (~4k) is looped each frame, and the whole texture is
// re-uploaded when anything changed; fine for one screen. Move regrowth to a shader / render
// texture (fog amount + last-cleared time in textures) when maps get big.
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
    [Tooltip("Shape of the crow's hole (and start clearings), same format as the puffs; Read/Write ticked. Its width = 2 x the radius. Empty = round hole with a noisy edge.")]
    [SerializeField] private Texture2D clearShape;
    [Tooltip("With Clear Shape: how much fog is removed (up) from the hole's centre (left, 0) to its edge (right, 1). Up 1 = fully clear, 0 = untouched. A low step near the right leaves thin fog in the outer ring. The cow only walks where this is above 0.5 (Reveal Threshold).")]
    [SerializeField] private AnimationCurve clearFalloff = new AnimationCurve(new Keyframe(0f, 1f), new Keyframe(0.5f, 1f), new Keyframe(0.7f, 0.6f), new Keyframe(0.85f, 0.3f), new Keyframe(1f, 0f));
    [Tooltip("Width of the fade at the hole's edge, in units (round hole only).")]
    [SerializeField] private float edgeSoftness = 0.2f;
    [Tooltip("How far the hole's edge wobbles in and out, in units. 0 = perfect circle.")]
    [SerializeField] private float edgeNoise = 0.4f;
    [Tooltip("Wobbles per unit along the edge. Higher = finer, wispier edge.")]
    [SerializeField] private float edgeNoiseScale = 2f;
    [Tooltip("How fine the fog looks.")]
    [SerializeField] private int pixelsPerUnit = 16;
    [Tooltip("How fine the blocking is.")]
    [SerializeField] private float cellSize = 0.25f;
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
    [SerializeField] private float cowSafeRadius = 0.75f;
    [Tooltip("Width, in units, of the soft ring just outside Cow Safe Radius where fog only regrows part of the way. Stops a hard line where the safe bubble ends. 0 = hard edge.")]
    [SerializeField] private float cowSafeFade = 1f;
    [Tooltip("A spot counts as revealed when its fog is below this (0 = clear, 1 = full fog).")]
    [Range(0f, 1f)]
    [SerializeField] private float revealThreshold = 0.5f;

    Collider2D cowCollider;
    Texture2D tex;
    Color32[] pixels;
    BoxCollider2D[,] cells;
    Vector2 origin;                          // bottom-left corner of the fog, world space
    bool dirty;
    float[] fog;                             // 0..1 per pixel, source of truth (bytes are too coarse for slow regrowth)
    float[] lastCleared;                     // Time.time each pixel was last under the crow
    float[] restFog;                         // fog after the start clearings: regrowth stops here
    bool[,] restEnabled;                     // cells that were blocking after the start clearings
    float clearShapeMax;                     // brightest spot of clearShape, so a faint image still clears fully
    float[] maxAlpha;                        // full-fog alpha per pixel (thin puff gaps are lower)
    float[] waitTime;                        // regrowDelay + this pixel's patchy extra wait
    float[] speedMul;                        // this pixel's regrow speed multiplier (around 1)

    void Start()
    {
        origin = (Vector2)transform.position - areaSize / 2f;
        if (cow != null) cowCollider = cow.GetComponent<Collider2D>();
        if (clearShape != null && !clearShape.isReadable) { Debug.LogError($"{name}: clear shape {clearShape.name} needs Read/Write ticked"); clearShape = null; }
        if (clearShape != null) clearShapeMax = Brightest(clearShape);

        // Visual: one texture, fully fogged.
        int w = Mathf.CeilToInt(areaSize.x * pixelsPerUnit);
        int h = Mathf.CeilToInt(areaSize.y * pixelsPerUnit);
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        pixels = new Color32[w * h];
        float[] look = BuildLook(w, h);
        maxAlpha = new float[pixels.Length];
        waitTime = new float[pixels.Length];
        speedMul = new float[pixels.Length];
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = fogColor * Mathf.Lerp(thinShade, 1f, look[i]);
            c.a = fogColor.a;
            pixels[i] = c;
            maxAlpha[i] = fogColor.a * Mathf.Lerp(thinAlpha, 1f, look[i]) * 255f;
            Vector2 p = new Vector2(i % w, i / w) / pixelsPerUnit;
            if (thickBorder > 0f && openAtStart != null)
            {
                // Distance outside the nearest start clearing's circle: thin fog there reads like a path, so keep it full.
                Vector2 world = origin + p + Vector2.one * (0.5f / pixelsPerUnit);
                float outside = 1000f;
                foreach (var s in openAtStart)
                    if (s != null) outside = Mathf.Min(outside, Vector2.Distance(world, s.position) - s.lossyScale.x / 2f);
                float k = Mathf.SmoothStep(0f, 1f, Mathf.Max(0f, outside) / thickBorder);
                maxAlpha[i] = Mathf.Lerp(fogColor.a * 255f, maxAlpha[i], k);
            }
            waitTime[i] = regrowDelay + Mathf.PerlinNoise(p.x * regrowNoiseScale + 50f, p.y * regrowNoiseScale + 50f) * regrowJitter;   // +50: Perlin repeats near 0
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
        sr.sprite = Sprite.Create(tex, new Rect(0, 0, w, h), Vector2.zero, pixelsPerUnit);
        sr.sortingOrder = sortingOrder;
        if (fogMaterial != null) sr.sharedMaterial = fogMaterial;

        // Blocking: one box collider per cell, on a child so the Fog object's Inspector stays light.
        var blockers = new GameObject("Fog Colliders");
        blockers.layer = gameObject.layer;
        blockers.transform.SetParent(transform, false);
        blockers.transform.position = origin;
        int cols = Mathf.CeilToInt(areaSize.x / cellSize);
        int rows = Mathf.CeilToInt(areaSize.y / cellSize);
        cells = new BoxCollider2D[cols, rows];
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
            {
                var box = blockers.AddComponent<BoxCollider2D>();
                box.size = Vector2.one * cellSize;
                box.offset = new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                cells[x, y] = box;
            }

        if (openAtStart != null)
            foreach (var c in openAtStart)
                if (c != null) Clear(c.position, c.lossyScale.x / 2f);

        // Snapshot: start clearings are the "rest" state and never regrow.
        restFog = (float[])this.fog.Clone();
        restEnabled = new bool[cols, rows];
        for (int x = 0; x < cols; x++)
            for (int y = 0; y < rows; y++)
                restEnabled[x, y] = cells[x, y].enabled;

        dirty = true;
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
        if (crow != null) Clear(crow.position, clearRadius);
        Regrow();
        if (dirty)
        {
            for (int i = 0; i < pixels.Length; i++) pixels[i].a = (byte)(fog[i] * maxAlpha[i]);
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
        for (int n = 0; n < puffCount; n++)
        {
            Texture2D t = puffTextures[Random.Range(0, puffTextures.Length)];
            if (t == null) continue;
            if (!t.isReadable) { Debug.LogError($"{name}: puff texture {t.name} needs Read/Write ticked"); break; }
            float bright = Brightest(t);   // so a dim image and a bright image both reach full shade
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
                    Color px = t.GetPixelBilinear(u, v);
                    float b = Mathf.Clamp01(px.grayscale * px.a / bright);
                    int i = y * w + x;
                    if (b > look[i]) look[i] = b;
                }
        }
        Random.state = oldState;   // don't disturb other scripts' randomness
        int blurPx = Mathf.RoundToInt(lookBlur * pixelsPerUnit);
        if (blurPx > 0) Blur(look, w, h, blurPx);
        return look;
    }

    // Box blur along rows, then columns, twice, so puff edges fade smoothly. Runs once in Start.
    // ponytail: plain sum per pixel (cost grows with the radius); a running sum if Start gets slow.
    void Blur(float[] a, int w, int h, int r)
    {
        var tmp = new float[a.Length];
        for (int pass = 0; pass < 2; pass++)
        {
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f; int n = 0;
                    for (int k = Mathf.Max(0, x - r); k <= Mathf.Min(w - 1, x + r); k++) { sum += a[y * w + k]; n++; }
                    tmp[y * w + x] = sum / n;
                }
            for (int y = 0; y < h; y++)
                for (int x = 0; x < w; x++)
                {
                    float sum = 0f; int n = 0;
                    for (int k = Mathf.Max(0, y - r); k <= Mathf.Min(h - 1, y + r); k++) { sum += tmp[k * w + x]; n++; }
                    a[y * w + x] = sum / n;
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

    void Clear(Vector2 center, float radius)
    {
        // Fog pixels: clear inside, fading back to full fog over edgeSoftness at the rim.
        // The rim is pushed in and out by Perlin noise fixed in world space, so edges look ragged, not round.
        float soft = Mathf.Clamp(edgeSoftness, 0.001f, radius);
        float reach = radius + edgeNoise;
        int x0 = Mathf.Max(0, Mathf.FloorToInt((center.x - reach - origin.x) * pixelsPerUnit));
        int x1 = Mathf.Min(tex.width - 1, Mathf.CeilToInt((center.x + reach - origin.x) * pixelsPerUnit));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((center.y - reach - origin.y) * pixelsPerUnit));
        int y1 = Mathf.Min(tex.height - 1, Mathf.CeilToInt((center.y + reach - origin.y) * pixelsPerUnit));
        // Turn the cloud stamp by a noise angle that depends on where it lands. An identical stamp slid along a path
        // lines its widest side up into a dead-straight edge; a turning one gives bumpy edges on both sides.
        float turn = Mathf.PerlinNoise(center.x * 1.5f + 300f, center.y * 1.5f + 300f) * 360f * Mathf.Deg2Rad;
        float cos = Mathf.Cos(turn), sin = Mathf.Sin(turn);
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = origin + new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit;
                float t = clearShape != null ? ShapeFog(p, center, radius, cos, sin)
                        : Mathf.Clamp01((Vector2.Distance(p, center) + Wobble(p) - (radius - soft)) / soft);
                int i = y * tex.width + x;
                if (t < fog[i]) { fog[i] = t; dirty = true; }
                if (t < 1f) lastCleared[i] = Time.time;     // anywhere inside the circle: keep it clear while the crow stays
            }

        // Colliders: switch off every cell whose centre now looks clear, so blocking follows the ragged edge.
        int cx0 = Mathf.Max(0, Mathf.FloorToInt((center.x - reach - origin.x) / cellSize));
        int cx1 = Mathf.Min(cells.GetLength(0) - 1, Mathf.FloorToInt((center.x + reach - origin.x) / cellSize));
        int cy0 = Mathf.Max(0, Mathf.FloorToInt((center.y - reach - origin.y) / cellSize));
        int cy1 = Mathf.Min(cells.GetLength(1) - 1, Mathf.FloorToInt((center.y + reach - origin.y) / cellSize));
        for (int x = cx0; x <= cx1; x++)
            for (int y = cy0; y <= cy1; y++)
            {
                Vector2 c = origin + new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                if (cells[x, y].enabled && IsClear(c))
                    cells[x, y].enabled = false;
            }
    }

    // Fog left at world point p when stamping clearShape at center: 0 where the image is solid, 1 outside it.
    // clearFalloff thins the clearing toward the radius, so outer rings keep some fog (it must reach 0 at the edge,
    // because the image's cloud touches its borders and would leave straight edges).
    // Wobble pushes the rings in and out, so the trail's edge is ragged instead of a smooth brush stroke.
    float ShapeFog(Vector2 p, Vector2 center, float radius, float cos, float sin)
    {
        Vector2 d = p - center;
        float dist = d.magnitude;
        if (dist >= radius) return 1f;
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
        int w = tex.width;

        // Fog pixels: fade back in towards the rest state, but not in the cow's ragged safe zone.
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (fog[i] >= restFog[i] || Time.time - lastCleared[i] < waitTime[i]) continue;
                float target = restFog[i];
                if (cow != null)
                {
                    Vector2 p = origin + new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit;
                    float d = Vector2.Distance(cow.position, p);
                    if (d <= cowSafeRadius + cowSafeFade + edgeNoise)   // first test skips the noise for far pixels
                    {
                        // 0 inside the safe bubble, rising to 1 over cowSafeFade: fog near the edge only regrows partway, so no hard line.
                        float k = Mathf.Clamp01((d + Wobble(p) - cowSafeRadius) / Mathf.Max(cowSafeFade, 0.001f));
                        target = restFog[i] * Mathf.SmoothStep(0f, 1f, k);
                    }
                }
                if (fog[i] >= target) continue;
                fog[i] = Mathf.Min(target, fog[i] + grow * speedMul[i]);
                dirty = true;
            }

        // Colliders: re-enable a cell only if it was blocking at rest and its centre pixel is fully fogged
        // again (never true inside the safe zone, so blocking follows its ragged edge).
        // Also never re-enable a cell touching the cow's own collider, whatever its size.
        Bounds cowBox = cowCollider != null ? cowCollider.bounds : default;
        cowBox.Expand(cellSize);
        for (int x = 0; x < cells.GetLength(0); x++)
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                if (cells[x, y].enabled || !restEnabled[x, y]) continue;
                Vector2 c = origin + new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                if (cowCollider != null && cowBox.Contains(new Vector3(c.x, c.y, cowBox.center.z))) continue;
                int px = Mathf.Clamp(Mathf.FloorToInt((c.x - origin.x) * pixelsPerUnit), 0, w - 1);
                int py = Mathf.Clamp(Mathf.FloorToInt((c.y - origin.y) * pixelsPerUnit), 0, tex.height - 1);
                int i = py * w + px;
                if (fog[i] >= restFog[i]) cells[x, y].enabled = true;
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
    }
}
