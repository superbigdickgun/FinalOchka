using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SnowGenerator : MonoBehaviour
{
    [Header("=== ОБЛАСТЬ ===")]
    public float areaWidth = 10f;
    public float areaHeight = 10f;

    [Header("=== СНЕГ (КАК МОРЕ) ===")]
    public float cellSize = 0.5f;
    [Range(0f, 1f)] public float edgeIrregularity = 0.3f;
    [Range(0f, 0.2f)] public float cellJitter = 0.02f;

    [Header("=== ЦВЕТА ===")]
    public Color[] snowColors = new Color[]
    {
        new Color(1f, 1f, 1f, 0.9f),
        new Color(0.95f, 0.95f, 1f, 0.9f),
        new Color(0.9f, 0.92f, 1f, 0.9f),
    };
    [Range(0f, 0.5f)] public float colorVariation = 0.1f;

    [Header("=== СЛОЙ ===")]
    public int sortingOrder = 0;

    [Header("=== ДЕЙСТВИЕ ===")]
    public bool generateNow = false;

    private void Update()
    {
        if (generateNow)
        {
            generateNow = false;
            Generate();
        }
    }

    public void Generate()
    {
        var old = transform.Find("SnowCells");
        if (old != null) DestroyImmediate(old.gameObject);

        var container = new GameObject("SnowCells");
        container.transform.SetParent(transform);
        container.transform.localPosition = Vector3.zero;

        float halfW = areaWidth * 0.5f;
        float halfH = areaHeight * 0.5f;

        int countX = Mathf.Max(1, Mathf.CeilToInt(areaWidth / cellSize));
        int countY = Mathf.Max(1, Mathf.CeilToInt(areaHeight / cellSize));

        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                float lx = -halfW + cellSize * (x + 0.5f);
                float ly = -halfH + cellSize * (y + 0.5f);

                float nx = lx / halfW;
                float ny = ly / halfH;
                float distFromCenter = Mathf.Sqrt(nx * nx + ny * ny);

                float noise = Mathf.PerlinNoise(lx * 3f + 10f, ly * 3f + 10f);
                float threshold = 1f + (noise - 0.5f) * 2f * edgeIrregularity;

                if (distFromCenter > threshold) continue;

                float jitterX = Random.Range(-cellJitter, cellJitter);
                float jitterY = Random.Range(-cellJitter, cellJitter);

                var go = new GameObject("Cell_" + x + "_" + y);
                go.transform.SetParent(container.transform);
                go.transform.localPosition = new Vector3(lx + jitterX, ly + jitterY, 0f);
                go.transform.localScale = new Vector3(cellSize, cellSize, 1f);

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

        Debug.Log("[SnowGenerator] Сгенерировано.");
    }
}