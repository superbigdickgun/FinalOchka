using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class SpriteCombiner : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Максимальный размер текстуры (1024 или 512 для легкости).")]
    public int maxTextureSize = 1024;

    [Tooltip("0 = взять из первого спрайта.")]
    public float pixelsPerUnit = 0f;

    public Color bakeColor = Color.white;
    public string sortingLayer = "Default";
    public int sortingOrder = -5;

    [Header("=== ЧТО ОБЪЕДИНЯТЬ ===")]
    public bool includeInactive = false;
    public bool deleteChildrenAfterBake = true;
    public bool createNewObject = true;
    public string bakedObjectName = "BakedSprite";

    [Header("=== ДЕЙСТВИЕ ===")]
    public bool combineNow = false;

    private void Update()
    {
        if (combineNow)
        {
            combineNow = false;
            Combine();
        }
    }

    public void Combine()
    {
        // 1. Собираем дочерние SpriteRenderer
        var allSprites = GetComponentsInChildren<SpriteRenderer>(includeInactive);
        
        // Исключаем самого себя, если на этом объекте есть SpriteRenderer
        var selfSr = GetComponent<SpriteRenderer>();
        var sprites = new List<SpriteRenderer>();
        foreach (var sr in allSprites)
        {
            if (sr == selfSr) continue;
            if (sr.sprite == null) continue;
            sprites.Add(sr);
        }

        if (sprites.Count == 0)
        {
            Debug.LogWarning("[SpriteCombiner] Нет дочерних SpriteRenderer для объединения. (Возможно, они выключены или пусты).");
            return;
        }

        // 2. PPU
        float ppu = pixelsPerUnit > 0f ? pixelsPerUnit : sprites[0].sprite.pixelsPerUnit;
        if (ppu <= 0.0001f) ppu = 100f;

        // 3. Считаем общую область в локальных координатах
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var sr in sprites)
        {
            Vector3 pos = transform.InverseTransformPoint(sr.transform.position);
            Vector2 size = GetSpriteWorldSize(sr);

            float halfW = size.x * 0.5f;
            float halfH = size.y * 0.5f;

            minX = Mathf.Min(minX, pos.x - halfW);
            maxX = Mathf.Max(maxX, pos.x + halfW);
            minY = Mathf.Min(minY, pos.y - halfH);
            maxY = Mathf.Max(maxY, pos.y + halfH);
        }

        float totalW = maxX - minX;
        float totalH = maxY - minY;

        if (totalW <= 0.0001f || totalH <= 0.0001f)
        {
            Debug.LogError("[SpriteCombiner] Область нулевая. Проверьте спрайты.");
            return;
        }

        // 4. Размер текстуры (сохраняем пропорции и не даем упасть в 0)
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

        // ✅ Защита от 0x0
        if (texW < 1) texW = 1;
        if (texH < 1) texH = 1;

        // 5. Создаем текстуру
        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[texW * texH];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        float centerX = (minX + maxX) * 0.5f;
        float centerY = (minY + maxY) * 0.5f;

        // 6. Рисуем каждый спрайт
        foreach (var sr in sprites)
        {
            Vector3 localPos = transform.InverseTransformPoint(sr.transform.position);
            Vector2 worldSize = GetSpriteWorldSize(sr);

            int cx = Mathf.RoundToInt(((localPos.x - centerX) / totalW + 0.5f) * texW);
            int cy = Mathf.RoundToInt(((localPos.y - centerY) / totalH + 0.5f) * texH);

            int drawW = Mathf.Max(1, Mathf.RoundToInt((worldSize.x / totalW) * texW));
            int drawH = Mathf.Max(1, Mathf.RoundToInt((worldSize.y / totalH) * texH));

            Color srColor = sr.color;
            Color finalColor = new Color(
                srColor.r * bakeColor.r,
                srColor.g * bakeColor.g,
                srColor.b * bakeColor.b,
                srColor.a * bakeColor.a
            );

            Texture2D srcTex = sr.sprite.texture;
            bool canRead = srcTex != null && srcTex.isReadable;
            Color[] srcPixels = canRead ? srcTex.GetPixels() : null;
            Rect srcRect = sr.sprite.textureRect;
            int srcW = (int)srcRect.width;
            int srcH = (int)srcRect.height;

            for (int y = 0; y < drawH; y++)
            {
                for (int x = 0; x < drawW; x++)
                {
                    int tx = cx + x - drawW / 2;
                    int ty = cy + y - drawH / 2;

                    if (tx < 0 || tx >= texW || ty < 0 || ty >= texH) continue;

                    Color pixelColor = finalColor;
                    float pixelAlpha = 1f;

                    if (canRead && srcPixels != null && srcW > 0 && srcH > 0)
                    {
                        int sx = Mathf.Clamp(Mathf.FloorToInt((x / (float)drawW) * srcW), 0, srcW - 1);
                        int sy = Mathf.Clamp(Mathf.FloorToInt((y / (float)drawH) * srcH), 0, srcH - 1);

                        int srcIndex = (int)(srcRect.y + sy) * srcTex.width + (int)(srcRect.x + sx);
                        if (srcIndex >= 0 && srcIndex < srcPixels.Length)
                        {
                            Color sp = srcPixels[srcIndex];
                            pixelColor = new Color(
                                sp.r * finalColor.r,
                                sp.g * finalColor.g,
                                sp.b * finalColor.b,
                                sp.a * finalColor.a
                            );
                            pixelAlpha = sp.a;
                        }
                    }

                    if (pixelAlpha <= 0.01f) continue;

                    int dstIndex = ty * texW + tx;
                    Color existing = pixels[dstIndex];

                    if (existing.a <= 0.01f)
                    {
                        pixels[dstIndex] = pixelColor;
                    }
                    else
                    {
                        float a = pixelColor.a + existing.a * (1f - pixelColor.a);
                        if (a > 0.0001f)
                        {
                            pixels[dstIndex] = new Color(
                                (pixelColor.r * pixelColor.a + existing.r * existing.a * (1f - pixelColor.a)) / a,
                                (pixelColor.g * pixelColor.a + existing.g * existing.a * (1f - pixelColor.a)) / a,
                                (pixelColor.b * pixelColor.a + existing.b * existing.a * (1f - pixelColor.a)) / a,
                                a
                            );
                        }
                    }
                }
            }
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // 7. Создаем спрайт
        Sprite bakedSprite = Sprite.Create(
            tex,
            new Rect(0, 0, texW, texH),
            new Vector2(0.5f, 0.5f),
            ppu
        );

        // 8. Создаем объект
        GameObject target;
        if (createNewObject)
        {
            target = new GameObject(bakedObjectName);
            target.transform.SetParent(transform.parent);
            target.transform.position = transform.position + new Vector3(centerX, centerY, 0f);
            target.transform.rotation = transform.rotation;
            target.transform.localScale = Vector3.one;
        }
        else
        {
            target = gameObject;
            var oldSr = GetComponent<SpriteRenderer>();
            if (oldSr != null) DestroyImmediate(oldSr);
        }

        var newSr = target.GetComponent<SpriteRenderer>();
        if (newSr == null) newSr = target.AddComponent<SpriteRenderer>();
        newSr.sprite = bakedSprite;
        newSr.color = bakeColor;
        newSr.sortingLayerName = sortingLayer;
        newSr.sortingOrder = sortingOrder;

        // 9. Удаляем дочерние
        if (deleteChildrenAfterBake)
        {
            foreach (var sr in sprites)
            {
                if (sr != null) DestroyImmediate(sr.gameObject);
            }
        }

        Debug.Log("[SpriteCombiner] Готово! Текстура: " + texW + "x" + texH
            + ", объединено: " + sprites.Count
            + ", центр: (" + centerX.ToString("F2") + "," + centerY.ToString("F2") + ")");
    }

    Vector2 GetSpriteWorldSize(SpriteRenderer sr)
    {
        if (sr.sprite == null) return Vector2.zero;
        Vector2 spriteSize = sr.sprite.bounds.size;
        Vector3 lossy = sr.transform.lossyScale;
        return new Vector2(
            spriteSize.x * Mathf.Abs(lossy.x),
            spriteSize.y * Mathf.Abs(lossy.y)
        );
    }
}