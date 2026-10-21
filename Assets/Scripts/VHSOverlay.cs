using UnityEngine;

public class VHSOverlay : MonoBehaviour
{
    [Header("=== ВКЛЮЧИТЬ / ВЫКЛЮЧИТЬ ===")]
    public bool vhsEnabled = true;

    [Header("=== ГЛАВНЫЙ ПОЛЗУНОК ===")]
    [Tooltip("Общая прозрачность. 0.2 = слабо, 0.5 = сильно.")]
    [Range(0f, 1f)] public float overlayAlpha = 0.35f;

    [Header("=== ЗЕРНО ===")]
    [Range(0f, 1f)] public float grainAmount = 0.12f;
    public int noisePixelSize = 2;

    [Header("=== ПОЛОСЫ ПОМЕХ (TRACKING) ===")]
    [Tooltip("Как часто появляются полосы (сек). 1.5 = редко.")]
    public float trackingInterval = 1.5f;

    [Tooltip("Высота полосы.")]
    public int trackingHeight = 4;

    [Tooltip("Насколько яркая полоса (0.05 = едва заметно, 0.3 = ярко).")]
    [Range(0.01f, 0.5f)] public float trackingIntensity = 0.08f;

    [Header("=== СКАН-ЛИНИИ ===")]
    [Range(0f, 0.5f)] public float scanlineIntensity = 0.15f;
    public int scanlineHeight = 4;

    [Header("=== ВИНЬЕТКА (КРАЯ) ===")]
    [Range(0f, 1f)] public float vignetteIntensity = 0.5f;

    [Header("=== SORTING ===")]
    public int sortingOrder = 900;

    private SpriteRenderer overlaySr;
    private Transform overlayTransform;
    private Texture2D noiseTex;
    private Color[] noisePixels;
    private int texW, texH;
    private Camera cam;

    private float trackingTimer = 0f;
    private int currentTrackingY = -1;

    void Start()
    {
        cam = Camera.main;
        if (cam == null) { enabled = false; return; }

        texW = Mathf.Max(8, Screen.width / noisePixelSize);
        texH = Mathf.Max(8, Screen.height / noisePixelSize);

        noiseTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        noiseTex.filterMode = FilterMode.Point;
        noisePixels = new Color[texW * texH];

        var go = new GameObject("VHSOverlay");
        go.transform.SetParent(cam.transform, false);
        go.transform.localPosition = new Vector3(0f, 0f, 1f);
        overlayTransform = go.transform;

        overlaySr = go.AddComponent<SpriteRenderer>();
        overlaySr.sortingOrder = sortingOrder;

        UpdateNoise();
        var sprite = Sprite.Create(noiseTex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), 1f);
        overlaySr.sprite = sprite;

        float worldHeight = cam.orthographicSize * 2f;
        float worldWidth = worldHeight * cam.aspect;
        overlayTransform.localScale = new Vector3(worldWidth / texW * noisePixelSize, worldHeight / texH * noisePixelSize, 1f);
    }

    void Update()
    {
        if (overlaySr == null) return;
        overlaySr.enabled = vhsEnabled;
        if (!vhsEnabled) return;

        if (cam != null)
        {
            overlayTransform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, cam.transform.position.z + 1f);
        }

        // Таймер полос помех (редко и незаметно)
        trackingTimer -= Time.deltaTime;
        if (trackingTimer <= 0f)
        {
            trackingTimer = trackingInterval + Random.Range(-0.5f, 0.5f);
            // Полоса появляется только в 50% случаев, чтобы не надоедать
            if (Random.value > 0.5f) 
                currentTrackingY = Random.Range(0, texH);
        }

        UpdateNoise();
    }

    void UpdateNoise()
    {
        if (noiseTex == null) return;

        if (currentTrackingY >= 0)
        {
            currentTrackingY -= 3; // Полоса "пролетает" быстро
            if (currentTrackingY < 0) currentTrackingY = -1;
        }

        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                int idx = y * texW + x;

                // Скан-линии
                float scanline = (y % scanlineHeight == 0) ? scanlineIntensity : 0f;

                // Виньетка
                float cx = (x - texW * 0.5f) / (texW * 0.5f);
                float cy = (y - texH * 0.5f) / (texH * 0.5f);
                float dist = Mathf.Sqrt(cx * cx + cy * cy);
                float vignette = Mathf.Clamp01(1f - dist * vignetteIntensity);

                // Полоса помех
                bool isTracking = false;
                if (currentTrackingY >= 0 && y >= currentTrackingY && y <= currentTrackingY + trackingHeight)
                {
                    isTracking = true;
                }

                // Зерно
                float grain = Random.value < grainAmount ? Random.Range(0f, 1f) : 0f;

                Color c = Color.clear;

                if (isTracking)
                {
                    // ✅ ЕЛЕ ЗАМЕТНАЯ ПОЛОСА: серый цвет с малой альфой
                    c = new Color(0.5f, 0.5f, 0.5f, trackingIntensity);
                }
                else if (grain > 0.5f)
                {
                    c = new Color(0.8f, 0.8f, 0.8f, grain * overlayAlpha);
                }
                else if (grain > 0f)
                {
                    c = new Color(0f, 0f, 0f, grain * overlayAlpha);
                }

                float finalAlpha = c.a + (scanline * overlayAlpha);
                finalAlpha *= vignette;

                noisePixels[idx] = new Color(c.r, c.g, c.b, Mathf.Clamp01(finalAlpha));
            }
        }

        noiseTex.SetPixels(noisePixels);
        noiseTex.Apply();
    }

    void OnDestroy()
    {
        if (overlaySr != null) Destroy(overlaySr.gameObject);
    }
}