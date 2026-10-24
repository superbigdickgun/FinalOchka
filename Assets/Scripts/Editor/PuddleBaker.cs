using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class PuddleBaker : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ЗАПЕКАНИЯ ===")]
    [Tooltip("Разрешение текстуры (чем больше, тем детальнее, но тяжелее).")]
    public int textureResolution = 512;

    [Tooltip("Цвет лужи.")]
    public Color puddleColor = new Color(0.4f, 0f, 0f, 0.9f);

    [Tooltip("Слой отрисовки.")]
    public int sortingOrder = -1;

    [Header("=== ДЕЙСТВИЕ ===")]
    [Tooltip("Нажмите, чтобы объединить все пиксели в один спрайт и удалить лишние объекты.")]
    public bool bakeNow = false;

    void Update()
    {
        if (bakeNow)
        {
            bakeNow = false;
            BakePuddle();
        }
    }

    public void BakePuddle()
    {
        // 1. Собираем все дочерние SpriteRenderer
        var children = GetComponentsInChildren<SpriteRenderer>();
        if (children.Length == 0)
        {
            Debug.LogWarning("[PuddleBaker] Нет дочерних SpriteRenderer для объединения.");
            return;
        }

        // 2. Создаем текстуру
        Texture2D tex = new Texture2D(textureResolution, textureResolution, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] pixels = new Color[textureResolution * textureResolution];
        for (int i = 0; i < pixels.Length; i++) pixels[i] = Color.clear;
        tex.SetPixels(pixels);

        // 3. Проходим по всем пикселям
        foreach (var child in children)
        {
            if (child == null) continue;

            Vector3 localPos = transform.InverseTransformPoint(child.transform.position);

            // Переводим в координаты текстуры (центр = 0.5)
            int cx = Mathf.RoundToInt((localPos.x + 0.5f) * textureResolution);
            int cy = Mathf.RoundToInt((localPos.y + 0.5f) * textureResolution);

            // Размер пикселя в текстуре (берем из Scale дочернего объекта)
            int size = Mathf.RoundToInt(child.transform.localScale.x * textureResolution * 10f);
            size = Mathf.Max(1, size);

            for (int y = cy - size / 2; y < cy + size / 2; y++)
            {
                for (int x = cx - size / 2; x < cx + size / 2; x++)
                {
                    if (x < 0 || x >= textureResolution || y < 0 || y >= textureResolution) continue;

                    int index = y * textureResolution + x;
                    pixels[index] = puddleColor;
                }
            }

            // Удаляем дочерний объект
            DestroyImmediate(child.gameObject);
        }

        tex.SetPixels(pixels);
        tex.Apply();

        // 4. Создаем спрайт
        Sprite puddleSprite = Sprite.Create(tex, new Rect(0, 0, textureResolution, textureResolution), new Vector2(0.5f, 0.5f), 1f);

        // 5. Создаем один объект с этим спрайтом
        var go = new GameObject("BakedPuddle");
        go.transform.SetParent(transform);
        go.transform.localPosition = Vector3.zero;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = puddleSprite;
        sr.sortingOrder = sortingOrder;

        Debug.Log("[PuddleBaker] Лужа объединена в один спрайт! Дочерних объектов удалено: " + children.Length);
    }
}