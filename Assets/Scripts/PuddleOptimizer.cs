using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class PuddleOptimizer : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Размер текстуры (в пикселях).")]
    public int textureSize = 512;
    
    [Tooltip("Цвет лужи (по умолчанию темно-красный).")]
    public Color puddleColor = new Color(0.4f, 0f, 0f, 0.9f);

    [Header("=== ОПТИМИЗАЦИЯ ===")]
    [Tooltip("Удалять ли исходные пиксели после склейки.")]
    public bool deletePixelsAfterOptimize = true;

    void Start()
    {
        Optimize();
    }

    public void Optimize()
    {
        // 1. Собираем все дочерние SpriteRenderer
        var children = GetComponentsInChildren<SpriteRenderer>();
        if (children.Length == 0) return;

        // 2. Создаем новую текстуру
        Texture2D tex = new Texture2D(textureSize, textureSize, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        
        // Очищаем
        Color[] pixels = new Color[textureSize * textureSize];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        tex.SetPixels(pixels);

        // 3. Проходим по всем пикселям и рисуем их в текстуру
        foreach (var child in children)
        {
            if (child == null) continue;
            
            Vector3 worldPos = child.transform.position;
            Vector3 localPos = transform.InverseTransformPoint(worldPos);
            
            // Переводим в координаты текстуры
            int cx = Mathf.RoundToInt((localPos.x + 0.5f) * textureSize);
            int cy = Mathf.RoundToInt((localPos.y + 0.5f) * textureSize);

            // Рисуем "квадратик" (размер пикселя)
            int size = Mathf.RoundToInt(child.transform.localScale.x * textureSize / 10f);
            size = Mathf.Max(1, size);

            for (int y = cy - size / 2; y < cy + size / 2; y++)
            {
                for (int x = cx - size / 2; x < cx + size / 2; x++)
                {
                    if (x < 0 || x >= textureSize || y < 0 || y >= textureSize) continue;
                    
                    int index = y * textureSize + x;
                    pixels[index] = puddleColor;
                }
            }

            if (deletePixelsAfterOptimize)
                Destroy(child.gameObject);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // 4. Создаем спрайт из текстуры
        Sprite puddleSprite = Sprite.Create(tex, new Rect(0, 0, textureSize, textureSize), new Vector2(0.5f, 0.5f), 1f);

        // 5. Создаем один объект с этим спрайтом
        var go = new GameObject("OptimizedPuddle");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = puddleSprite;
        sr.sortingOrder = -1; // Под игроком
    }
}