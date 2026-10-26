using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class SnowBakerInPlace : MonoBehaviour
{
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
        var sprites = GetComponentsInChildren<SpriteRenderer>();
        if (sprites == null || sprites.Length == 0)
        {
            Debug.LogError("[SnowBakerInPlace] Нет дочерних SpriteRenderer.");
            return;
        }

        // ppu из первого куска
        float ppu = sprites[0].sprite != null ? sprites[0].sprite.pixelsPerUnit : 1f;

        // Область
        float minX = float.MaxValue, maxX = float.MinValue;
        float minY = float.MaxValue, maxY = float.MinValue;

        foreach (var sr in sprites)
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

        if (totalW <= 0.001f || totalH <= 0.001f)
        {
            Debug.LogError("[SnowBakerInPlace] Область нулевая!");
            return;
        }

        // Прямоугольная текстура
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

        Texture2D tex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        Color[] pixels = new Color[texW * texH];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;

        foreach (var sr in sprites)
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
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // ✅ Создаём спрайт
        Sprite bakedSprite = Sprite.Create(tex, new Rect(0, 0, texW, texH), new Vector2(0.5f, 0.5f), ppu);

        // ✅ НЕ создаём новый объект — используем СУЩЕСТВУЮЩИЙ SpriteRenderer на родителе
        var parentSr = GetComponent<SpriteRenderer>();
        if (parentSr == null)
        {
            parentSr = gameObject.AddComponent<SpriteRenderer>();
        }

        parentSr.sprite = bakedSprite;
        parentSr.enabled = true;

        // ✅ Позиция и масштаб родителя НЕ МЕНЯЮТСЯ
        // (он уже на месте)

        // Удаляем старые дочерние куски
        foreach (var sr in sprites)
        {
            if (sr != null) DestroyImmediate(sr.gameObject);
        }

        Debug.Log("[SnowBakerInPlace] Запечено в родителя! Текстура: " + texW + "x" + texH + ", PPU=" + ppu);
    }
}