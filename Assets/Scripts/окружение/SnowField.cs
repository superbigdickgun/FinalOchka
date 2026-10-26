using UnityEngine;

[ExecuteInEditMode]
public class SnowField : MonoBehaviour
{
    [Header("=== СНЕГ (КАК МОРЕ) ===")]
    [Tooltip("Размер одного квадрата (в юнитах).")]
    public float cellSize = 0.5f;

    [Tooltip("Неровность края (0 = ровный, 1 = очень неровный).")]
    [Range(0f, 1f)] public float edgeIrregularity = 0.5f;

    [Tooltip("Случайное смещение квадратов.")]
    [Range(0f, 0.2f)] public float cellJitter = 0.02f;

    [Tooltip("Оттенки белого.")]
    public Color[] snowColors = new Color[]
    {
        new Color(1f, 1f, 1f, 0.9f),
        new Color(0.95f, 0.95f, 1f, 0.9f),
        new Color(0.9f, 0.92f, 1f, 0.9f),
    };

    [Tooltip("Разброс цвета.")]
    [Range(0f, 0.5f)] public float colorVariation = 0.1f;

    [Header("=== СЛОЙ ===")]
    public int sortingOrder = 0;

    [Header("=== ПЕРЕГЕНЕРАЦИЯ ===")]
    [Tooltip("Перегенерировать снег сейчас.")]
    public bool regenerateNow = false;

    private Transform container;

    void OnEnable()
    {
        Generate();
    }

    void OnValidate()
    {
        // При изменении параметров в инспекторе — перегенерируем
        if (isActiveAndEnabled)
        {
            // Отложенная генерация (безопасно для редактора)
            UnityEditor.EditorApplication.delayCall += () =>
            {
                if (this != null) Generate();
            };
        }
    }

    void Update()
    {
        if (regenerateNow)
        {
            regenerateNow = false;
            Generate();
        }
    }

    public void Generate()
    {
        // Удаляем старый контейнер
        var old = transform.Find("SnowCells");
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }

        var containerGO = new GameObject("SnowCells");
        containerGO.transform.SetParent(transform);
        containerGO.transform.localPosition = Vector3.zero;
        containerGO.transform.localRotation = Quaternion.identity;
        containerGO.transform.localScale = Vector3.one;
        container = containerGO.transform;

        // ✅ Размер области — из Scale объекта
        float worldW = Mathf.Abs(transform.lossyScale.x);
        float worldH = Mathf.Abs(transform.lossyScale.y);

        if (worldW <= 0.001f || worldH <= 0.001f) return;

        int countX = Mathf.Max(1, Mathf.CeilToInt(worldW / cellSize));
        int countY = Mathf.Max(1, Mathf.CeilToInt(worldH / cellSize));

        float stepX = worldW / countX;
        float stepY = worldH / countY;

        float halfW = worldW * 0.5f;
        float halfH = worldH * 0.5f;

        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                float lx = -halfW + stepX * (x + 0.5f);
                float ly = -halfH + stepY * (y + 0.5f);

                // Неровность края (шум)
                float nx = lx / halfW;
                float ny = ly / halfH;
                float distFromCenter = Mathf.Sqrt(nx * nx + ny * ny);

                float noise = Mathf.PerlinNoise(lx * 3f + 10f, ly * 3f + 10f);
                float threshold = 1f + (noise - 0.5f) * 2f * edgeIrregularity;

                if (distFromCenter > threshold) continue;

                // Случайное смещение
                float jitterX = Random.Range(-cellJitter, cellJitter);
                float jitterY = Random.Range(-cellJitter, cellJitter);

                var go = new GameObject("Cell_" + x + "_" + y);
                go.transform.SetParent(container, false);
                go.transform.localPosition = new Vector3(lx + jitterX, ly + jitterY, 0f);
                go.transform.localScale = new Vector3(stepX, stepY, 1f);
                go.transform.localRotation = Quaternion.identity;

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = LightSpriteGenerator.WhitePixel;
                sr.sortingOrder = sortingOrder;

                Color baseColor = snowColors[Random.Range(0, snowColors.Length)];
                float v = Random.Range(-colorVariation, colorVariation);
                baseColor.r = Mathf.Clamp01(baseColor.r + v);
                baseColor.g = Mathf.Clamp01(baseColor.g + v);
                baseColor.b = Mathf.Clamp01(baseColor.b + v);
                sr.color = baseColor;
            }
        }

        Debug.Log("[SnowField] Сгенерировано: " + (countX * countY) + " ячеек.");
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, new Vector3(
            Mathf.Abs(transform.lossyScale.x),
            Mathf.Abs(transform.lossyScale.y),
            0f));
    }
}