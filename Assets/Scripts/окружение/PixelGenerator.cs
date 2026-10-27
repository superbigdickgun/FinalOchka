using UnityEngine;

[ExecuteInEditMode]
public class HayGenerator : MonoBehaviour
{
    [Header("=== ГЕНЕРАЦИЯ ===")]
    [Tooltip("Сколько кучек сена создать.")]
    public int hayPatchesCount = 10;

    [Tooltip("Максимальный радиус разброса от центра.")]
    public float scatterRadius = 3f;

    [Header("=== НАСТРОЙКИ ПЯТНА ===")]
    public int pixelSize = 4;
    public int puddleRadiusPixels = 30;
    public Color hayColor = new Color(0.8f, 0.6f, 0.2f, 0.9f);
    [Range(0f, 0.5f)] public float colorVariation = 0.1f;
    [Range(0f, 1f)] public float edgeIrregularity = 0.8f;

    [Header("=== СЛОЙ ===")]
    public int sortingOrder = -5;

    [Header("=== ДЕЙСТВИЕ ===")]
    public bool generateNow = false;

    private Transform container;

    void Update()
    {
        if (generateNow)
        {
            generateNow = false;
            Generate();
        }
    }

    public void Generate()
    {
        // Удаляем старый контейнер
        var old = transform.Find("HayContainer");
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }

        // Создаем контейнер
        var containerGO = new GameObject("HayContainer");
        containerGO.transform.SetParent(transform);
        containerGO.transform.localPosition = Vector3.zero;
        container = containerGO.transform;

        // Создаем пятна
        for (int i = 0; i < hayPatchesCount; i++)
        {
            var go = new GameObject("HayPatch_" + i);
            go.transform.SetParent(container, false);

            // Случайная позиция
            Vector2 randomPos = Random.insideUnitCircle * scatterRadius;
            go.transform.localPosition = new Vector3(randomPos.x, randomPos.y, 0f);

            // Случайный масштаб (чтобы пятна были разного размера)
            float scale = Random.Range(0.5f, 1.5f);
            go.transform.localScale = Vector3.one * scale;

            // SpriteRenderer
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sortingOrder = sortingOrder;

            // Добавляем PixelPuddle и настраиваем
            var puddle = go.AddComponent<PixelPuddle>();
            puddle.pixelSize = pixelSize;
            puddle.puddleRadiusPixels = puddleRadiusPixels;
            puddle.puddleColor = hayColor;
            puddle.colorVariation = colorVariation;
            puddle.edgeIrregularity = edgeIrregularity;
            puddle.textureSize = Mathf.NextPowerOfTwo(puddleRadiusPixels * 2);
            puddle.generateNow = true; // Запускаем генерацию
        }

        Debug.Log("[HayGenerator] Создано кучек сена: " + hayPatchesCount);
    }
}