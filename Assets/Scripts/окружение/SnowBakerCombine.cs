using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SnowBakerCombine : MonoBehaviour
{
    [Header("=== ОБЪЕКТЫ СНЕГА (ПЕРЕТАЩИТЬ) ===")]
    [Tooltip("Перетащите сюда оба объекта SnowField.")]
    public GameObject[] snowFields;

    [Header("=== ЗАПЕКАНИЕ ===")]
    [Tooltip("Максимальный размер текстуры.")]
    public int maxTextureSize = 2048;

    [Header("=== ДЕЙСТВИЕ ===")]
    public bool bakeNow = false;

    private void Update()
    {
        if (bakeNow)
        {
            bakeNow = false;
            Bake();
        }
    }

    public void Bake()
    {
        Debug.Log("=== НАЧАЛО ЗАПЕКАНИЯ ===");

        if (snowFields == null || snowFields.Length == 0)
        {
            Debug.LogError("[SnowBakerCombine] Перетащите объекты SnowField!");
            return;
        }

        // 1. Собираем ВСЕ спрайты (включая неактивные)
        var allSprites = new List<SpriteRenderer>();
        foreach (var field in snowFields)
        {
            if (field == null) { Debug.LogWarning("[SnowBakerCombine] Поле null!"); continue; }

            var sprites = field.GetComponentsInChildren<SpriteRenderer>(true);
            Debug.Log("[SnowBakerCombine] " + field.name + ": " + sprites.Length + " спрайтов, детей: " + field.transform.childCount);
            allSprites.AddRange(sprites);
        }

        Debug.Log("[SnowBakerCombine] Всего спрайтов: " + allSprites.Count);

        if (allSprites.Count == 0)
        {
            Debug.LogError("[SnowBakerCombine] НЕТ СПРАЙТОВ. SnowField не создал дочерние SpriteRenderer.");
            return;
        }

        // 2. ppu из первого
        float ppu = allSprites[0].sprite != null ? allSprites[0].sprite.pixelsPerUnit : 1f;
        Debug.Log("[SnowBakerCombine] PPU: " + ppu);

        // 3. Общая область
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var sr in allSprites)
        {
            if (sr == null) continue;
            Vector3 pos = sr.transform.position;
            float halfW = Mathf.Abs(sr.transform.lossyScale.x) * 0.5f;
            float halfH = Mathf.Abs(sr.transform.lossyScale.y) * 0.5f;

            minX = Mathf.Min(minX, pos.x - halfW);
            maxX = Mathf.Max(maxX, pos.x + halfW);
            minY = Mathf.Min(minY, pos.y - halfH);
            maxY = Mathf.Max(maxY, pos.y + halfH);
        }

        float totalW = maxX - minX;
        float totalH = maxY - minY;
        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;

        Debug.Log("[SnowBakerCombine] Область: W=" + totalW + " H=" + totalH + " Center=(" + centerX + "," + centerY + ")");

        if (totalW <= 0.001f || totalH <= 0.001f)
        {
            Debug.LogError("[SnowBakerCombine] Область нулевая!");
            return;
        }

        // 4. Прямоугольная текстура
        float aspect = totalW / totalH;
        int texW, texH;

        if (aspect >= 1f)
        {
            texW = maxTextureSize;
            texH = Mathf.Max(1, Mathf.RoundToInt(maxTextureSize / aspect));
        }
        else
        {
            texH = maxTextureSize;
            texW = Mathf.Max(1, Mathf.RoundToInt(maxTextureSize * aspect));
        }

        Debug.Log("[SnowBakerCombine] Текстура: " + texW + "x" + texH);

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[texW * texH];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        int drawn = 0;

        // 5. Рисуем все спрайты
        foreach (var sr in allSprites)
        {
            if (sr == null) continue;

            Vector3 worldPos = sr.transform.position;

            int cx = Mathf.RoundToInt(((worldPos.x - centerX) / totalW + 0.5f) * texW);
            int cy = Mathf.RoundToInt(((worldPos.y - centerY) / totalH + 0.5f) * texH);

            int drawW = Mathf.Max(1, Mathf.RoundToInt((Mathf.Abs(sr.transform.lossyScale.x) / totalW) * texW));
            int drawH = Mathf.Max(1, Mathf.RoundToInt((Mathf.Abs(sr.transform.lossyScale.y) / totalH) * texH));

            Color color = sr.color;

            for (int y = 0; y < drawH; y++)
            {
                for (int x = 0; x < drawW; x++)
                {
                    int tx = cx + x - drawW / 2;
                    int ty = cy + y - drawH / 2;

                    if (tx < 0 || tx >= texW || ty < 0 || ty >= texH) continue;

                    int dstIndex = ty * texW + tx;
                    pixels[dstIndex] = color;
                }
            }

            drawn++;
        }

        Debug.Log("[SnowBakerCombine] Нарисовано: " + drawn);

        tex.SetPixels(pixels);
        tex.Apply();

        // 6. Спрайт
        Sprite bakedSprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), ppu);

        // 7. Объект
        var go = new GameObject("BakedSnow_Combined");
        go.transform.SetParent(transform);
        go.transform.position = new Vector3(centerX, centerY, 0f);
        go.transform.localScale = Vector3.one;

        var bakedSr = go.AddComponent<SpriteRenderer>();
        bakedSr.sprite = bakedSprite;

        Debug.Log("[SnowBakerCombine] BakedSnow_Combined создан: pos=(" + centerX + "," + centerY + ")");

        // 8. Удаляем старые
        foreach (var field in snowFields)
        {
            if (field != null) DestroyImmediate(field);
        }

        Debug.Log("=== КОНЕЦ ===");
    }
}