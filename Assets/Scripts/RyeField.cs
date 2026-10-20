using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class RyeField : MonoBehaviour
{
    [Header("=== СПРАЙТЫ (3 разных) ===")]
    [Tooltip("Спрайты ржи. Каждый тайл выбирает случайный. Размер масштабируется через Sprite Scale.")]
    public Sprite[] ryeSprites = new Sprite[3];

    [Header("=== ОБЛАСТЬ ===")]
    [Tooltip("Ширина области поля в юнитах.")]
    public float fieldWidth = 10f;

    [Tooltip("Высота области поля в юнитах.")]
    public float fieldHeight = 10f;

    [Header("=== РАЗМЕР СПРАЙТОВ ===")]
    [Tooltip("Множитель размера спрайта. 1 = оригинал, 2 = вдвое больше, 0.5 = вдвое меньше.")]
    public float spriteScale = 1f;

    [Tooltip("Перекрытие тайлов. 1 = стыкуются вплотную. 0.9 = накладываются (гуще). 1.1 = зазор.")]
    [Range(0.5f, 1.5f)] public float tileOverlap = 1f;

    public Color tileColor = Color.white;
    public int sortingOrder = 0;

    [Header("=== РАЗДВИГАНИЕ (в сторону движения) ===")]
    public float pushRadius = 1f;
    public float pushStrength = 0.35f;
    public float maxTilt = 35f;
    public float returnSpeed = 6f;

    [Header("=== ПЕРЕКРЫТИЕ НОГ ===")]
    [Range(0f, 1f)] public float overlapAmount = 0.6f;
    public float footThresholdOffset = 0f;
    public int playerSortingOrder = 10;

    [Header("=== СЛУЧАЙНОСТЬ ===")]
    [Tooltip("Случайное смещение тайла при создании (юниты).")]
    public float randomOffset = 0.04f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;
    public bool rebuildNow = false;

    private Transform container;
    private List<RyeTile> tiles = new List<RyeTile>();

    void OnEnable()
    {
        if (Application.isPlaying || rebuildNow)
            Rebuild();
    }

    void Update()
    {
        if (rebuildNow)
        {
            rebuildNow = false;
            Rebuild();
        }
    }

    public void Rebuild()
    {
        var old = transform.Find("RyeTilesContainer");
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }

        tiles.Clear();

        if (ryeSprites == null || ryeSprites.Length == 0)
        {
            Debug.LogError("[RyeField] Не заданы Rye Sprites!");
            return;
        }

        List<Sprite> valid = new List<Sprite>();
        foreach (var s in ryeSprites) if (s != null) valid.Add(s);
        if (valid.Count == 0)
        {
            Debug.LogError("[RyeField] Все слоты спрайтов пусты!");
            return;
        }

        if (spriteScale <= 0.0001f) spriteScale = 0.0001f;

        float baseW = valid[0].bounds.size.x * spriteScale;
        float baseH = valid[0].bounds.size.y * spriteScale;
        if (baseW <= 0.0001f) baseW = 1f;
        if (baseH <= 0.0001f) baseH = 1f;

        float stepX = baseW * tileOverlap;
        float stepY = baseH * tileOverlap;
        if (stepX <= 0.0001f) stepX = 1f;
        if (stepY <= 0.0001f) stepY = 1f;

        int countX = Mathf.Max(1, Mathf.CeilToInt(fieldWidth / stepX));
        int countY = Mathf.Max(1, Mathf.CeilToInt(fieldHeight / stepY));

        var containerGO = new GameObject("RyeTilesContainer");
        containerGO.transform.SetParent(transform, false);
        containerGO.transform.localPosition = Vector3.zero;
        containerGO.transform.localRotation = Quaternion.identity;
        containerGO.transform.localScale = Vector3.one;
        container = containerGO.transform;

        float totalW = countX * stepX;
        float totalH = countY * stepY;
        float startX = -totalW * 0.5f + stepX * 0.5f;
        float startY = -totalH * 0.5f + stepY * 0.5f;

        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                var go = new GameObject("Rye_" + x + "_" + y);
                go.transform.SetParent(container, false);

                float wx = startX + stepX * x;
                float wy = startY + stepY * y;

                go.transform.localPosition = new Vector3(wx, wy, 0f);

                if (randomOffset > 0f)
                {
                    go.transform.localPosition += new Vector3(
                        Random.Range(-randomOffset, randomOffset),
                        Random.Range(-randomOffset, randomOffset),
                        0f);
                }

                Sprite chosen = valid[Random.Range(0, valid.Count)];

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = chosen;
                sr.color = tileColor;
                sr.sortingOrder = sortingOrder;

                go.transform.localScale = Vector3.one * spriteScale;
                go.transform.localRotation = Quaternion.identity;

                var tile = go.AddComponent<RyeTile>();
                tile.pushRadius = pushRadius;
                tile.pushStrength = pushStrength;
                tile.maxTilt = maxTilt;
                tile.returnSpeed = returnSpeed;
                tile.overlapAmount = overlapAmount;
                tile.footThresholdOffset = footThresholdOffset;
                tile.playerSortingOrder = playerSortingOrder;
                tile.sortingOrder = sortingOrder;

                tiles.Add(tile);
            }
        }

        if (debugLogs)
            Debug.Log("[RyeField] Тайлов: " + (countX * countY) + " (" + countX + "x" + countY
                + "), размер тайла: " + baseW.ToString("F2") + "x" + baseH.ToString("F2")
                + ", шаг: " + stepX.ToString("F2") + "x" + stepY.ToString("F2")
                + ", область: " + totalW.ToString("F2") + "x" + totalH.ToString("F2"));
    }
}