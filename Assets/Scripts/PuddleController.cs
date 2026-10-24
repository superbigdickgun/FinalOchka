using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PuddleController : MonoBehaviour
{
    [Header("=== ЛУЖА ===")]
    [Tooltip("Размер текстуры лужи (например, 256 или 512).")]
    public int textureSize = 256;

    [Tooltip("Радиус лужи в юнитах.")]
    public float puddleRadius = 3f;

    [Tooltip("Цвет лужи.")]
    public Color puddleColor = new Color(0.4f, 0f, 0f, 0.9f);

    [Tooltip("Неровность краев (0 = ровный круг, 1 = очень неровный).")]
    [Range(0f, 1f)] public float edgeIrregularity = 0.3f;

    // ✅ ДЛЯ СОВМЕСТИМОСТИ С PuddleReflectionSimple
    [Tooltip("Размер ячейки (для совместимости).")]
    public float cellSize = 0.2f;

    [Tooltip("Ширина лужи (для совместимости).")]
    public float width = 4f;

    [Tooltip("Высота лужи (для совместимости).")]
    public float height = 3f;

    [Header("=== РЯБЬ ===")]
    [Tooltip("Радиус ряби (в юнитах).")]
    public float rippleRadius = 1f;

    [Tooltip("Скорость расширения ряби.")]
    public float rippleExpandSpeed = 3f;

    [Tooltip("Время жизни ряби (сек).")]
    public float rippleLifetime = 1f;

    [Tooltip("Цвет ряби.")]
    public Color rippleColor = new Color(1f, 0f, 0f, 0.5f);

    [Header("=== ИГРОК ===")]
    public Transform player;

    [Header("=== ОПТИМИЗАЦИЯ ===")]
    [Tooltip("Максимум ряби на текстуре.")]
    public int maxRipples = 5;

    private Texture2D texture;
    private SpriteRenderer sr;
    private Color[] pixels;
    private float[] rippleTimers = new float[10];
    private Vector2[] ripplePositions = new Vector2[10];
    private int currentRippleIndex = 0;
    private float lastRippleTime = 0f;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();

        // Создаем текстуру
        texture = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        texture.filterMode = FilterMode.Point;
        texture.wrapMode = TextureWrapMode.Clamp;

        pixels = new Color[textureSize * textureSize];

        // Рисуем форму лужи один раз
        DrawPuddleShape();

        // Применяем к спрайту
        texture.SetPixels(pixels);
        texture.Apply();
        sr.sprite = Sprite.Create(texture, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 1f);
        sr.sortingOrder = -1;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }

        for (int i = 0; i < 10; i++) rippleTimers[i] = 0f;
    }

    void Update()
    {
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            if (dist < puddleRadius)
            {
                // Создаем рябь каждые 0.3 секунды
                if (Time.time - lastRippleTime > 0.3f)
                {
                    lastRippleTime = Time.time;
                    CreateRipple(player.position);
                }
            }
        }

        UpdateRipples();
    }

    void DrawPuddleShape()
    {
        float center = textureSize * 0.5f;

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                // Неровность края
                float noise = Mathf.PerlinNoise(x * 0.1f, y * 0.1f);
                float threshold = 1f + (noise - 0.5f) * edgeIrregularity;

                if (dist < threshold)
                {
                    pixels[y * textureSize + x] = puddleColor;
                }
                else
                {
                    pixels[y * textureSize + x] = Color.clear;
                }
            }
        }
    }

    void CreateRipple(Vector2 worldPos)
    {
        int slot = currentRippleIndex % 10;
        currentRippleIndex++;

        Vector3 localPos = transform.InverseTransformPoint(worldPos);
        ripplePositions[slot] = new Vector2(
            (localPos.x / (puddleRadius * 2f) + 0.5f) * textureSize,
            (localPos.y / (puddleRadius * 2f) + 0.5f) * textureSize
        );

        rippleTimers[slot] = rippleLifetime;
    }

    void UpdateRipples()
    {
        bool needsUpdate = false;

        for (int i = 0; i < 10; i++)
        {
            if (rippleTimers[i] > 0f)
            {
                rippleTimers[i] -= Time.deltaTime;
                needsUpdate = true;
            }
        }

        if (needsUpdate)
        {
            // Перерисовываем форму лужи, затем рисуем рябь
            DrawPuddleShape();

            for (int i = 0; i < 10; i++)
            {
                if (rippleTimers[i] > 0f)
                {
                    DrawRipple(i);
                }
            }

            texture.SetPixels(pixels);
            texture.Apply();
        }
    }

    void DrawRipple(int slot)
    {
        float timer = rippleTimers[slot];
        float alpha = timer / rippleLifetime;

        Vector2 center = ripplePositions[slot];
        float currentRadius = (rippleLifetime - timer) * rippleExpandSpeed;

        float pixelRadius = currentRadius * (textureSize / (puddleRadius * 2f));

        for (int y = 0; y < textureSize; y++)
        {
            for (int x = 0; x < textureSize; x++)
            {
                float dx = x - center.x;
                float dy = y - center.y;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);

                if (Mathf.Abs(dist - pixelRadius) < 3f)
                {
                    int index = y * textureSize + x;
                    Color current = pixels[index];
                    pixels[index] = Color.Lerp(current, rippleColor, alpha * 0.5f);
                }
            }
        }
    }
}