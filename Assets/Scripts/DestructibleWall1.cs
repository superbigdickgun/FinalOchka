using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class DestructibleWall : MonoBehaviour
{
    [Header("=== РАЗРУШЕНИЕ ===")]
    [Tooltip("Минимальный радиус дыры (в юнитах).")]
    public float holeRadiusMin = 0.15f;

    [Tooltip("Максимальный радиус дыры (случайный в диапазоне).")]
    public float holeRadiusMax = 0.3f;

    [Tooltip("Разрушается ли стена вообще.")]
    public bool canBeDestroyed = true;

    [Header("=== КОЛЛАЙДЕР-СЕТКА ===")]
    [Tooltip("Размер одной клетки коллайдера (в юнитах). Меньше = точнее, но больше объектов.")]
    public float colliderCellSize = 0.15f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private Texture2D workingTex;
    private SpriteRenderer sr;
    private Color[] pixels;
    private int width, height;
    private float pixelsPerUnit;

    private BoxCollider2D[,] cellColliders;
    private int colsX, colsY;
    private float cellWorldW, cellWorldH;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null || sr.sprite == null)
        {
            Debug.LogError("[Wall] Нет SpriteRenderer или спрайта.");
            enabled = false;
            return;
        }

        Sprite sprite = sr.sprite;
        Texture2D src = sprite.texture;

        if (src == null || !src.isReadable)
        {
            Debug.LogError("[Wall] Включи Read/Write Enabled на спрайте " + gameObject.name);
            enabled = false;
            return;
        }

        width = src.width;
        height = src.height;
        pixelsPerUnit = sprite.pixelsPerUnit;

        // Рабочая текстура
        workingTex = new Texture2D(width, height, TextureFormat.RGBA32, false);
        workingTex.filterMode = FilterMode.Point;
        workingTex.SetPixels(src.GetPixels());
        workingTex.Apply();

        sr.sprite = Sprite.Create(workingTex,
            new Rect(0, 0, width, height),
            new Vector2(0.5f, 0.5f),
            pixelsPerUnit);

        pixels = workingTex.GetPixels();

        // Сетка коллайдеров
        BuildColliderGrid();

        // Удаляем старый BoxCollider2D со стены
        var oldBox = GetComponent<BoxCollider2D>();
        if (oldBox != null) Destroy(oldBox);
    }

    void BuildColliderGrid()
    {
        Vector2 worldSize = new Vector2(
            width / pixelsPerUnit * Mathf.Abs(transform.lossyScale.x),
            height / pixelsPerUnit * Mathf.Abs(transform.lossyScale.y)
        );

        colsX = Mathf.Max(1, Mathf.CeilToInt(worldSize.x / colliderCellSize));
        colsY = Mathf.Max(1, Mathf.CeilToInt(worldSize.y / colliderCellSize));

        cellWorldW = worldSize.x / colsX;
        cellWorldH = worldSize.y / colsY;

        cellColliders = new BoxCollider2D[colsX, colsY];

        for (int y = 0; y < colsY; y++)
        {
            for (int x = 0; x < colsX; x++)
            {
                var go = new GameObject("Cell_" + x + "_" + y);
                go.transform.SetParent(transform, false);

                float lx = -worldSize.x * 0.5f + cellWorldW * (x + 0.5f);
                float ly = -worldSize.y * 0.5f + cellWorldH * (y + 0.5f);

                go.transform.localPosition = new Vector3(
                    lx / Mathf.Abs(transform.lossyScale.x),
                    ly / Mathf.Abs(transform.lossyScale.y),
                    0f);

                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(cellWorldW, cellWorldH);
                box.isTrigger = false;

                cellColliders[x, y] = box;
            }
        }

        if (debugLogs)
            Debug.Log("[Wall] Сетка: " + colsX + "x" + colsY);
    }

    public void BreakAtPoint(Vector2 worldPoint, float power = 1f)
    {
        if (!canBeDestroyed || workingTex == null) return;

        Vector2 worldSize = new Vector2(
            width / pixelsPerUnit * Mathf.Abs(transform.lossyScale.x),
            height / pixelsPerUnit * Mathf.Abs(transform.lossyScale.y)
        );

        Vector2 local = transform.InverseTransformPoint(worldPoint);

        float u = (local.x / (width / pixelsPerUnit)) + 0.5f;
        float v = (local.y / (height / pixelsPerUnit)) + 0.5f;

        int cx = Mathf.RoundToInt(u * width);
        int cy = Mathf.RoundToInt(v * height);

        float worldRadius = Random.Range(holeRadiusMin, holeRadiusMax);

        float pxPerWorldX = width / worldSize.x;
        float pxPerWorldY = height / worldSize.y;

        int radiusX = Mathf.Max(2, Mathf.RoundToInt(worldRadius * pxPerWorldX * power));
        int radiusY = Mathf.Max(2, Mathf.RoundToInt(worldRadius * pxPerWorldY * power));

        for (int y = cy - radiusY; y <= cy + radiusY; y++)
        {
            for (int x = cx - radiusX; x <= cx + radiusX; x++)
            {
                if (x < 0 || x >= width || y < 0 || y >= height) continue;

                float dx = (x - cx) / (float)radiusX;
                float dy = (y - cy) / (float)radiusY;
                float dist = dx * dx + dy * dy;

                if (dist <= 1f)
                {
                    float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);

                    if (noise > 0.3f || dist < 0.5f)
                    {
                        pixels[y * width + x] = Color.clear;
                        DisableColliderAtPixel(x, y);
                    }
                }
            }
        }

        workingTex.SetPixels(pixels);
        workingTex.Apply();

        if (debugLogs)
            Debug.Log("[Wall] Дыра в " + worldPoint + " радиус=" + worldRadius);
    }

    void DisableColliderAtPixel(int px, int py)
    {
        if (cellColliders == null) return;

        float u = px / (float)width;
        float v = py / (float)height;

        int colX = Mathf.Clamp(Mathf.FloorToInt(u * colsX), 0, colsX - 1);
        int colY = Mathf.Clamp(Mathf.FloorToInt(v * colsY), 0, colsY - 1);

        var box = cellColliders[colX, colY];
        if (box != null && box.enabled)
            box.enabled = false;
    }
}