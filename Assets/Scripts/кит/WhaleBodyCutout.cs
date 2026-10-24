using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class WhaleBodyCutout : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ВЫРЕЗА ===")]
    public float cutoutRadius = 2f;
    [Range(0f, 1f)] public float cutoutSoftness = 0.5f;

    [Header("=== ПОДЛОЖКА ===")]
    [Tooltip("Спрайт подложки (то, что должно быть видно сквозь дырку).")]
    public SpriteRenderer backingSprite;

    [Tooltip("Скрывать подложку, когда вырез не активен.")]
    public bool hideBackingWhenInactive = false;

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

        originalPixels = workingTex.GetPixels();
        workingPixels = new Color[originalPixels.Length];
        System.Array.Copy(originalPixels, workingPixels, originalPixels.Length);

        pivotOffset = new Vector2(
            texW * sr.sprite.pivot.x / texW,
            texH * sr.sprite.pivot.y / texH
        );

        // ✅ Скрываем подложку на старте, если нужно
        if (backingSprite != null && hideBackingWhenInactive)
            backingSprite.enabled = false;
    }

    public void StartCutout()
    {
        isCutActive = true;
        lastCutPos = new Vector2(-999, -999);

        // ✅ Показываем подложку
        if (backingSprite != null)
            backingSprite.enabled = true;
    }

    public void StopCutout()
    {
        isCutActive = false;
        RestoreCutout();

        // ✅ Скрываем подложку, если нужно
        if (backingSprite != null && hideBackingWhenInactive)
            backingSprite.enabled = false;
    }

    public void UpdateCutoutPosition(Vector3 worldPoint)
    {
        if (!isCutActive) return;

        Vector3 localPoint = transform.InverseTransformPoint(worldPoint);
        int cx = Mathf.RoundToInt((localPoint.x * ppu) + pivotOffset.x);
        int cy = Mathf.RoundToInt((localPoint.y * ppu) + pivotOffset.y);

        cx = Mathf.Clamp(cx, 0, texW - 1);
        cy = Mathf.Clamp(cy, 0, texH - 1);

        int pixelRadius = Mathf.RoundToInt(cutoutRadius * ppu);
        int softRadius = Mathf.RoundToInt(pixelRadius * cutoutSoftness);
        int totalRadius = pixelRadius + softRadius;

        if (Vector2.Distance(new Vector2(cx, cy), lastCutPos) < 2f) return;

        if (lastCutPos.x != -999)
        {
            RestoreArea((int)lastCutPos.x, (int)lastCutPos.y, lastRadius);
        }

        lastCutPos = new Vector2(cx, cy);
        lastRadius = totalRadius;

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