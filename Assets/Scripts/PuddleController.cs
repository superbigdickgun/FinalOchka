using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class PuddleController : MonoBehaviour
{
    [Header("=== ЛУЖА ===")]
    public float width = 4f;
    public float height = 3f;
    public float cellSize = 0.2f;
    public Color puddleColor = new Color(0.1f, 0.15f, 0.25f, 0.7f);
    [Range(0f, 0.5f)] public float colorVariation = 0.1f;
    [Range(0f, 0.5f)] public float edgeIrregularity = 0.2f;
    [Range(0f, 0.1f)] public float cellJitter = 0.02f;
    public int puddleSortingOrder = -5;

    [Header("=== РЯБЬ ===")]
    public float rippleInterval = 0.3f;
    public float rippleCellSize = 0.1f;
    public float rippleExpandSpeed = 2f;
    public float rippleLifetime = 1.5f;
    public float minMoveDistance = 0.1f;
    public int rippleSegments = 16;
    public Color rippleColor = new Color(1f, 1f, 1f, 0.5f);
    public int rippleSortingOrder = -4;

    [Header("=== ОТРАЖЕНИЕ ===")]
    public bool enableReflection = true;
    public Transform reflectionTarget;
    public Sprite reflectionSprite;
    public float reflectionCellSize = 0.15f;
    [Range(0f, 1f)] public float reflectionAlpha = 0.35f;
    public Color reflectionColor = new Color(0.05f, 0.08f, 0.15f, 1f);
    public float reflectionYOffset = 0f;
    public int reflectionSortingOrder = -4;
    public float reflectionVisibleDistance = 3f;

    [Header("=== ГЕНЕРАЦИЯ ===")]
    public bool generateNow = false;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private Transform puddleCells;
    private Transform reflectionCells;
    private Transform player;
    private Vector3 lastPlayerPos;
    private float rippleTimer = 0f;
    private float distanceTraveled = 0f;

    private Texture2D workTex;
    private Color[] pixels;
    private int texW, texH;
    private float ppu;
    private List<GameObject> refCellObjects = new List<GameObject>();

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            lastPlayerPos = player.position;
        }

        if (reflectionTarget == null && player != null)
            reflectionTarget = player;

        GeneratePuddle();
        if (enableReflection) BuildReflection();
    }

    void Update()
    {
        if (generateNow)
        {
            generateNow = false;
            GeneratePuddle();
            if (enableReflection) BuildReflection();
        }

        if (Application.isPlaying && player != null)
            UpdateRipple();
    }

    void LateUpdate()
    {
        if (Application.isPlaying && enableReflection && reflectionTarget != null)
            UpdateReflectionPosition();
    }

    // ============================================================
    // УДАЛЕНИЕ СТАРЫХ ОБЪЕКТОВ (с проверкой null)
    // ============================================================
    void ClearContainer(string name)
    {
        // Собираем всех дочерних
        var all = transform.GetComponentsInChildren<Transform>(true);

        // Удаляем в обратном порядке
        for (int i = all.Length - 1; i >= 0; i--)
        {
            var t = all[i];
            if (t == null) continue;
            if (t == transform) continue;
            if (t.name == name || t.name.StartsWith(name))
            {
                DestroyImmediate(t.gameObject);
            }
        }

        // Удаляем Ripple в сцене
        var ripples = FindObjectsOfType<PuddleRippleSingle>();
        foreach (var r in ripples)
        {
            if (r != null) DestroyImmediate(r.gameObject);
        }
    }

    // ============================================================
    // ЛУЖА
    // ============================================================
    public void GeneratePuddle()
    {
        ClearContainer("PuddleCells");

        if (cellSize <= 0.001f || width <= 0.001f || height <= 0.001f)
        {
            Debug.LogError("[Puddle] Неверные размеры!");
            return;
        }

        int countX = Mathf.Max(1, Mathf.RoundToInt(width / cellSize));
        int countY = Mathf.Max(1, Mathf.RoundToInt(height / cellSize));

        var containerGO = new GameObject("PuddleCells");
        containerGO.transform.SetParent(transform, false);
        containerGO.transform.localPosition = Vector3.zero;
        puddleCells = containerGO.transform;

        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        int created = 0;

        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                float lx = -halfW + cellSize * (x + 0.5f);
                float ly = -halfH + cellSize * (y + 0.5f);

                float nx = lx / halfW;
                float ny = ly / halfH;
                float distFromCenter = Mathf.Sqrt(nx * nx + ny * ny);

                float threshold = 1f;
                float noise = Mathf.PerlinNoise(lx * 3f + 10f, ly * 3f + 10f);
                threshold += (noise - 0.5f) * 2f * edgeIrregularity;

                if (distFromCenter > threshold) continue;

                var go = new GameObject("Cell_" + x + "_" + y);
                go.transform.SetParent(puddleCells, false);

                float jx = Random.Range(-cellJitter, cellJitter);
                float jy = Random.Range(-cellJitter, cellJitter);
                go.transform.localPosition = new Vector3(lx + jx, ly + jy, 0f);
                go.transform.localScale = new Vector3(cellSize, cellSize, 1f);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = LightSpriteGenerator.WhitePixel;
                sr.sortingOrder = puddleSortingOrder;

                float v = Random.Range(-colorVariation, colorVariation);
                Color c = puddleColor;
                c.r = Mathf.Clamp01(c.r + v);
                c.g = Mathf.Clamp01(c.g + v);
                c.b = Mathf.Clamp01(c.b + v);
                sr.color = c;

                created++;
            }
        }

        if (debugLogs)
            Debug.Log("[Puddle] Ячеек: " + created);
    }

    // ============================================================
    // РЯБЬ
    // ============================================================
    void UpdateRipple()
    {
        bool inPuddle = IsInPuddle(player.position);
        if (!inPuddle) { lastPlayerPos = player.position; return; }

        float moved = Vector3.Distance(player.position, lastPlayerPos);
        distanceTraveled += moved;

        if (moved > 0.001f)
        {
            rippleTimer -= Time.deltaTime;
            if (rippleTimer <= 0f && distanceTraveled > minMoveDistance)
            {
                rippleTimer = rippleInterval;
                distanceTraveled = 0f;
                SpawnRipple(player.position);
            }
        }
        else rippleTimer = Mathf.Min(rippleTimer, rippleInterval);

        lastPlayerPos = player.position;
    }

    bool IsInPuddle(Vector3 worldPos)
    {
        Vector3 p = transform.position;
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        return worldPos.x >= p.x - halfW && worldPos.x <= p.x + halfW &&
               worldPos.y >= p.y - halfH && worldPos.y <= p.y + halfH;
    }

    float GetDistanceToEdge(Vector3 worldPos)
    {
        Vector3 p = transform.position;
        float halfW = width * 0.5f;
        float halfH = height * 0.5f;
        float dL = worldPos.x - (p.x - halfW);
        float dR = (p.x + halfW) - worldPos.x;
        float dB = worldPos.y - (p.y - halfH);
        float dT = (p.y + halfH) - worldPos.y;
        return Mathf.Min(Mathf.Min(dL, dR), Mathf.Min(dB, dT));
    }

    void SpawnRipple(Vector2 worldPos)
    {
        float maxRadius = GetDistanceToEdge(worldPos);
        if (maxRadius <= 0.01f) return;

        var go = new GameObject("Ripple");
        go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

        var ripple = go.AddComponent<PuddleRippleSingle>();
        ripple.rippleSegments = rippleSegments;
        ripple.rippleCellSize = rippleCellSize;
        ripple.expandSpeed = rippleExpandSpeed;
        ripple.lifetime = maxRadius / rippleExpandSpeed;
        ripple.rippleColor = rippleColor;
        ripple.sortingOrder = rippleSortingOrder;
        ripple.maxRadius = maxRadius;

        if (debugLogs)
            Debug.Log("[Puddle] Рябь в " + worldPos + " до края " + maxRadius);
    }

    // ============================================================
    // ОТРАЖЕНИЕ
    // ============================================================
    void BuildReflection()
    {
        ClearContainer("ReflectionCells");
        refCellObjects.Clear();

        if (reflectionTarget == null)
        {
            Debug.LogWarning("[Puddle] reflectionTarget не задан!");
            return;
        }

        var sr = reflectionTarget.GetComponentInChildren<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogWarning("[Puddle] У target нет SpriteRenderer!");
            return;
        }

        Sprite src = reflectionSprite != null ? reflectionSprite : sr.sprite;
        Texture2D srcTex = src.texture;

        if (srcTex == null || !srcTex.isReadable)
        {
            Debug.LogWarning("[Puddle] Спрайт должен быть Read/Write Enabled!");
            return;
        }

        texW = srcTex.width;
        texH = srcTex.height;
        ppu = src.pixelsPerUnit;

        workTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        workTex.filterMode = FilterMode.Point;
        workTex.SetPixels(srcTex.GetPixels());
        workTex.Apply();
        pixels = workTex.GetPixels();

        var containerGO = new GameObject("ReflectionCells");
        containerGO.transform.SetParent(transform, false);
        reflectionCells = containerGO.transform;

        int countX = Mathf.Max(1, Mathf.RoundToInt(texW / ppu / reflectionCellSize));
        int countY = Mathf.Max(1, Mathf.RoundToInt(texH / ppu / reflectionCellSize));

        float spriteW = texW / ppu;
        float spriteH = texH / ppu;
        float cw = spriteW / countX;
        float ch = spriteH / countY;

        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                int pxStart = Mathf.RoundToInt((x / (float)countX) * texW);
                int pxEnd = Mathf.RoundToInt(((x + 1) / (float)countX) * texW);
                int pyStart = Mathf.RoundToInt((y / (float)countY) * texH);
                int pyEnd = Mathf.RoundToInt(((y + 1) / (float)countY) * texH);

                Color avg = Color.clear;
                int cnt = 0;
                for (int py = pyStart; py < pyEnd; py++)
                    for (int px = pxStart; px < pxEnd; px++)
                    {
                        if (px < 0 || px >= texW || py < 0 || py >= texH) continue;
                        Color c = pixels[py * texW + px];
                        if (c.a > 0.1f) { avg += c; cnt++; }
                    }

                if (cnt == 0) continue;
                avg /= cnt;

                float lx = -spriteW * 0.5f + cw * (x + 0.5f);
                float ly = -spriteH * 0.5f + ch * (y + 0.5f);

                var go = new GameObject("Ref_" + x + "_" + y);
                go.transform.SetParent(reflectionCells, false);
                go.transform.localPosition = new Vector3(lx, ly, 0f);
                go.transform.localScale = new Vector3(cw, ch, 1f);

                var rsr = go.AddComponent<SpriteRenderer>();
                rsr.sprite = LightSpriteGenerator.WhitePixel;
                rsr.sortingOrder = reflectionSortingOrder;

                Color final = reflectionColor;
                final.a = reflectionAlpha;
                rsr.color = final;

                refCellObjects.Add(go);
            }
        }

        if (debugLogs)
            Debug.Log("[Puddle] Отражение: " + refCellObjects.Count + " ячеек");
    }

    void UpdateReflectionPosition()
    {
        if (reflectionCells == null) return;

        Vector3 center = transform.position;
        Vector3 tp = reflectionTarget.position;
        float ry = center.y - (tp.y - center.y) + reflectionYOffset;

        reflectionCells.position = new Vector3(tp.x, ry, tp.z);
        reflectionCells.localScale = new Vector3(1f, -1f, 1f);

        float dist = Vector2.Distance(center, tp);
        reflectionCells.gameObject.SetActive(dist < reflectionVisibleDistance);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 0.5f, 1f, 0.3f);
        Gizmos.DrawWireCube(transform.position, new Vector3(width, height, 0f));
    }
}