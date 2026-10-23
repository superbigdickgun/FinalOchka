using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WhaleBodyCutout : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ВЫРЕЗА ===")]
    [Tooltip("Радиус выреза в юнитах.")]
    public float cutoutRadius = 2f;

    [Tooltip("Мягкость краев (0 = резкий, 1 = мягкий).")]
    [Range(0f, 1f)] public float cutoutSoftness = 0.5f;

    private SpriteRenderer sr;
    private Texture2D workingTex;
    private Color[] originalPixels;
    private Color[] workingPixels;
    private int texW, texH;
    private float ppu;
    private Vector2 pivotOffset;
    
    private bool isCutActive = false;
    private Vector2 lastCutPos = new Vector2(-999, -999);
    private int lastRadius = 0;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr.sprite == null) return;

        workingTex = sr.sprite.texture;
        
        if (!workingTex.isReadable)
        {
            Debug.LogError("[WhaleCutout] Включи Read/Write Enabled на спрайте!");
            enabled = false;
            return;
        }

        texW = workingTex.width;
        texH = workingTex.height;
        ppu = sr.sprite.pixelsPerUnit;

        // Сохраняем оригинальные пиксели
        originalPixels = workingTex.GetPixels();
        workingPixels = new Color[originalPixels.Length];
        System.Array.Copy(originalPixels, workingPixels, originalPixels.Length);

        // ✅ ВЫЧИСЛЯЕМ СМЕЩЕНИЕ PIVOT
        pivotOffset = new Vector2(
            texW * sr.sprite.pivot.x / texW,
            texH * sr.sprite.pivot.y / texH
        );
    }

    public void StartCutout()
    {
        isCutActive = true;
        lastCutPos = new Vector2(-999, -999);
    }

    public void StopCutout()
    {
        isCutActive = false;
        RestoreCutout();
    }

    public void UpdateCutoutPosition(Vector3 worldPoint)
    {
        if (!isCutActive) return;

        // 1. Переводим мир в локальные координаты спрайта
        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);

        // 2. Переводим в пиксели (с учетом пивота)
        int cx = Mathf.RoundToInt((localPoint.x * ppu) + pivotOffset.x);
        int cy = Mathf.RoundToInt((localPoint.y * ppu) + pivotOffset.y);

        // Защита от выхода за границы
        cx = Mathf.Clamp(cx, 0, texW - 1);
        cy = Mathf.Clamp(cy, 0, texH - 1);

        int pixelRadius = Mathf.RoundToInt(cutoutRadius * ppu);
        int softRadius = Mathf.RoundToInt(pixelRadius * cutoutSoftness);
        int totalRadius = pixelRadius + softRadius;

        // Если игрок не двигался (меньше 2 пикселей) — не пересчитываем
        if (Vector2.Distance(new Vector2(cx, cy), lastCutPos) < 2f) return;

        // 3. Восстанавливаем область СТАРОГО выреза
        if (lastCutPos.x != -999)
        {
            RestoreArea((int)lastCutPos.x, (int)lastCutPos.y, lastRadius);
        }

        // 4. Запоминаем новую позицию
        lastCutPos = new Vector2(cx, cy);
        lastRadius = totalRadius;

        // 5. Делаем новую дырку
        for (int y = cy - totalRadius; y <= cy + totalRadius; y++)
        {
            for (int x = cx - totalRadius; x <= cx + totalRadius; x++)
            {
                if (x < 0 || x >= texW || y < 0 || y >= texH) continue;

                int index = y * texW + x;
                Color finalColor = originalPixels[index];

                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(cx, cy));
                
                if (dist <= totalRadius)
                {
                    float alpha = 1f;
                    if (dist > pixelRadius)
                    {
                        alpha = 1f - ((dist - pixelRadius) / softRadius);
                    }
                    finalColor.a *= (1f - alpha);
                }

                workingPixels[index] = finalColor;
            }
        }

        workingTex.SetPixels(workingPixels);
        workingTex.Apply();
    }

    // Восстанавливает оригинал в области старой дырки
    void RestoreArea(int cx, int cy, int radius)
    {
        for (int y = cy - radius; y <= cy + radius; y++)
        {
            for (int x = cx - radius; x <= cx + radius; x++)
            {
                if (x < 0 || x >= texW || y < 0 || y >= texH) continue;
                int index = y * texW + x;
                workingPixels[index] = originalPixels[index];
            }
        }
    }

    public void RestoreCutout()
    {
        System.Array.Copy(originalPixels, workingPixels, originalPixels.Length);
        workingTex.SetPixels(workingPixels);
        workingTex.Apply();
        lastCutPos = new Vector2(-999, -999);
    }
}