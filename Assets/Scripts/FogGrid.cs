using UnityEngine;

// Fog = one texture the crow paints round holes into, plus a grid of invisible colliders
// that block the cow until the crow clears them.
// Fog the crow cleared grows back after regrowDelay, except start clearings and where the cow stands.
// ponytail: every pixel (~60k) and cell (~4k) is looped each frame, and the whole texture is
// re-uploaded when anything changed; fine for one screen. Move regrowth to a shader / render
// texture (fog amount + last-cleared time in textures) when maps get big.
// Cloud look: puff textures are stamped at random once in Start into a brightness map, so it costs nothing per frame.
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
    [Tooltip("Width of the fade at the hole's edge, in units.")]
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
    [Tooltip("Grey-on-black cloud images stamped across the fog for its look. Need Read/Write ticked in import settings. Empty = flat colour.")]
    [SerializeField] private Texture2D[] puffTextures;
    [Tooltip("How many puffs are stamped.")]
    [SerializeField] private int puffCount = 40;
    [Tooltip("Puff size in units, min (x) to max (y).")]
    [SerializeField] private Vector2 puffSizeRange = new Vector2(3f, 6f);
    [Tooltip("Brightness of the thinnest fog, relative to Fog Color. Lower = stronger cloud shapes. Fog stays opaque either way.")]
    [Range(0f, 1f)]
    [SerializeField] private float thinShade = 0.7f;
    [Tooltip("Change for a different random puff layout.")]
    [SerializeField] private int seed = 0;
    [Tooltip("Things below this order hide under the fog (snakes).")]
    [SerializeField] private int sortingOrder = 10;
    [Tooltip("Fog never regrows under the cow.")]
    [SerializeField] private Transform cow;
    [Tooltip("Seconds after the crow last cleared a spot before it starts regrowing.")]
    [SerializeField] private float regrowDelay = 5f;
    [Tooltip("Fraction of full fog regrown per second.")]
    [SerializeField] private float regrowSpeed = 0.5f;
    [SerializeField] private float cowSafeRadius = 0.75f;
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

    void Start()
    {
        origin = (Vector2)transform.position - areaSize / 2f;
        if (cow != null) cowCollider = cow.GetComponent<Collider2D>();

        // Visual: one texture, fully fogged.
        int w = Mathf.CeilToInt(areaSize.x * pixelsPerUnit);
        int h = Mathf.CeilToInt(areaSize.y * pixelsPerUnit);
        tex = new Texture2D(w, h, TextureFormat.RGBA32, false) { wrapMode = TextureWrapMode.Clamp };
        pixels = new Color32[w * h];
        float[] look = BuildLook(w, h);
        for (int i = 0; i < pixels.Length; i++)
        {
            Color c = fogColor * Mathf.Lerp(thinShade, 1f, look[i]);
            c.a = fogColor.a;
            pixels[i] = c;
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
        if (crow != null) Clear(crow.position, clearRadius);
        Regrow();
        if (dirty)
        {
            float maxA = fogColor.a * 255f;
            for (int i = 0; i < pixels.Length; i++) pixels[i].a = (byte)(fog[i] * maxA);
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
                    float b = Mathf.Clamp01(t.GetPixelBilinear(u, v).grayscale / bright);
                    int i = y * w + x;
                    if (b > look[i]) look[i] = b;
                }
        }
        Random.state = oldState;   // don't disturb other scripts' randomness
        return look;
    }

    // Coarse 64x64 scan is enough to find the peak of a blurry cloud.
    float Brightest(Texture2D t)
    {
        float max = 0.01f;
        for (int y = 0; y < 64; y++)
            for (int x = 0; x < 64; x++)
                max = Mathf.Max(max, t.GetPixelBilinear(x / 63f, y / 63f).grayscale);
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
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = origin + new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit;
                float t = Mathf.Clamp01((Vector2.Distance(p, center) + Wobble(p) - (radius - soft)) / soft);
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
                if (fog[i] >= restFog[i] || Time.time - lastCleared[i] < regrowDelay) continue;
                if (cow != null)
                {
                    Vector2 p = origin + new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit;
                    float d = Vector2.Distance(cow.position, p);
                    if (d <= cowSafeRadius + edgeNoise && d + Wobble(p) <= cowSafeRadius) continue;   // first test skips the noise for far pixels
                }
                fog[i] = Mathf.Min(restFog[i], fog[i] + grow);
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
