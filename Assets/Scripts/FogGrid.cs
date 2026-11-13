using UnityEngine;

// Fog = one texture the crow paints round holes into, plus a grid of invisible colliders
// that block the cow until the crow clears them.
// Fog the crow cleared grows back after regrowDelay, except start clearings and where the cow stands.
// ponytail: every pixel (~60k) and cell (~4k) is looped each frame, and the whole texture is
// re-uploaded when anything changed; fine for one screen. Move regrowth to a shader / render
// texture (fog amount + last-cleared time in textures) when maps get big or the art fog arrives.
public class FogGrid : MonoBehaviour
{
    public Transform crow;                   // clears fog as it moves
    public Transform[] openAtStart;          // pre-cleared circles; diameter = the object's X scale
    public Vector2 areaSize = new Vector2(20, 12);
    public float clearRadius = 0.75f;
    public float edgeSoftness = 0.2f;        // width of the fade at the hole's edge, in units
    public int pixelsPerUnit = 16;           // how fine the fog looks
    public float cellSize = 0.25f;           // how fine the blocking is
    public Color fogColor = new Color(0.29f, 0.33f, 0.16f);
    public int sortingOrder = 10;
    public Transform cow;                    // fog never regrows under the cow
    public float regrowDelay = 5f;           // seconds after the crow last cleared a spot
    public float regrowSpeed = 0.5f;         // fraction of full fog per second
    public float cowSafeRadius = 0.75f;

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
        Color32 fog = fogColor;
        for (int i = 0; i < pixels.Length; i++) pixels[i] = fog;
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

    void Clear(Vector2 center, float radius)
    {
        // Fog pixels: clear inside, fading back to full fog over edgeSoftness at the rim.
        float soft = Mathf.Clamp(edgeSoftness, 0.001f, radius);
        int x0 = Mathf.Max(0, Mathf.FloorToInt((center.x - radius - origin.x) * pixelsPerUnit));
        int x1 = Mathf.Min(tex.width - 1, Mathf.CeilToInt((center.x + radius - origin.x) * pixelsPerUnit));
        int y0 = Mathf.Max(0, Mathf.FloorToInt((center.y - radius - origin.y) * pixelsPerUnit));
        int y1 = Mathf.Min(tex.height - 1, Mathf.CeilToInt((center.y + radius - origin.y) * pixelsPerUnit));
        for (int y = y0; y <= y1; y++)
            for (int x = x0; x <= x1; x++)
            {
                Vector2 p = origin + new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit;
                float t = Mathf.Clamp01((Vector2.Distance(p, center) - (radius - soft)) / soft);
                int i = y * tex.width + x;
                if (t < fog[i]) { fog[i] = t; dirty = true; }
                if (t < 1f) lastCleared[i] = Time.time;     // anywhere inside the circle: keep it clear while the crow stays
            }

        // Colliders: switch off every cell whose centre is inside the circle.
        int cx0 = Mathf.Max(0, Mathf.FloorToInt((center.x - radius - origin.x) / cellSize));
        int cx1 = Mathf.Min(cells.GetLength(0) - 1, Mathf.FloorToInt((center.x + radius - origin.x) / cellSize));
        int cy0 = Mathf.Max(0, Mathf.FloorToInt((center.y - radius - origin.y) / cellSize));
        int cy1 = Mathf.Min(cells.GetLength(1) - 1, Mathf.FloorToInt((center.y + radius - origin.y) / cellSize));
        for (int x = cx0; x <= cx1; x++)
            for (int y = cy0; y <= cy1; y++)
            {
                Vector2 c = origin + new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                if (cells[x, y].enabled && (c - center).sqrMagnitude <= radius * radius)
                    cells[x, y].enabled = false;
            }
    }

    void Regrow()
    {
        float grow = regrowSpeed * Time.deltaTime;
        float safeSqr = cowSafeRadius * cowSafeRadius;
        int w = tex.width;

        // Fog pixels: fade back in towards the rest state, but not under the cow.
        for (int y = 0; y < tex.height; y++)
            for (int x = 0; x < w; x++)
            {
                int i = y * w + x;
                if (fog[i] >= restFog[i] || Time.time - lastCleared[i] < regrowDelay) continue;
                if (cow != null)
                {
                    Vector2 p = origin + new Vector2(x + 0.5f, y + 0.5f) / pixelsPerUnit;
                    if (((Vector2)cow.position - p).sqrMagnitude <= safeSqr) continue;
                }
                fog[i] = Mathf.Min(restFog[i], fog[i] + grow);
                dirty = true;
            }

        // Colliders: re-enable a cell only if it was blocking at rest, its centre pixel is fully fogged
        // again, and the cow isn't close.
        float cellSafe = cowSafeRadius + cellSize / 2f;
        // Also never re-enable a cell touching the cow's own collider, whatever its size.
        Bounds cowBox = cowCollider != null ? cowCollider.bounds : default;
        cowBox.Expand(cellSize);
        for (int x = 0; x < cells.GetLength(0); x++)
            for (int y = 0; y < cells.GetLength(1); y++)
            {
                if (cells[x, y].enabled || !restEnabled[x, y]) continue;
                Vector2 c = origin + new Vector2(x + 0.5f, y + 0.5f) * cellSize;
                if (cow != null && ((Vector2)cow.position - c).sqrMagnitude <= cellSafe * cellSafe) continue;
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
