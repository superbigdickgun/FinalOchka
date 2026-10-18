using UnityEngine;

[RequireComponent(typeof(SpriteMask))]
public class CandleStencilLight : MonoBehaviour
{
    [Header("=== СВЕТ ===")]
    [Tooltip("Радиус света в юнитах.")]
    public float radius = 3f;

    [Tooltip("Смещение центра света.")]
    public Vector2 offset = Vector2.zero;

    [Header("=== СГЛАЖИВАНИЕ КРАЯ ===")]
    [Tooltip("0.1 = жёсткий, 0.5 = мягче, 0.9 = маленький яркий центр. Больше = меньше видимый радиус.")]
    [Range(0.05f, 0.95f)] public float edgeSoftness = 0.3f;

    [Tooltip("Компенсация: увеличивает радиус, чтобы мягкий край не «съел» видимый размер.")]
    [Range(1f, 2f)] public float softnessCompensation = 1.3f;

    [Header("=== МЕРЦАНИЕ ===")]
    public bool flicker = false;
    [Tooltip("Насколько сильно мерцает. 0.1 = слабо, 0.5 = сильно.")]
    public float flickerAmount = 0.1f;
    [Tooltip("Скорость мерцания.")]
    public float flickerSpeed = 8f;

    [Header("=== SORTING ===")]
    [Tooltip("Должно быть НИЖЕ тьмы (100).")]
    public int frontSortingOrder = 100;
    public int backSortingOrder = 99;

    [Tooltip("Порядок спрайта свечи. Обычно 5.")]
    public int spriteSortingOrder = 5;

    [Header("=== СОЗДАВАТЬ МАСКУ ===")]
    [Tooltip("Если выкл — использует SpriteMask на этом же объекте.")]
    public bool createMaskOnStart = true;

    private SpriteMask mask;
    private Transform maskTransform;
    private float baseRadius;

    void Awake()
    {
        baseRadius = radius;

        if (createMaskOnStart)
        {
            var go = new GameObject("StencilMask");
            go.transform.SetParent(transform, false);
            go.transform.localPosition = Vector3.zero;
            maskTransform = go.transform;

            mask = go.AddComponent<SpriteMask>();
        }
        else
        {
            mask = GetComponent<SpriteMask>();
            maskTransform = transform;
        }

        if (mask != null)
        {
            mask.sprite = LightSpriteGenerator.SoftCircle;
            mask.alphaCutoff = edgeSoftness;
            mask.isCustomRangeActive = true;
            mask.frontSortingOrder = frontSortingOrder;
            mask.backSortingOrder = backSortingOrder;
        }
    }

    void LateUpdate()
    {
        if (mask == null) return;

        // Меняем мягкость в Play
        mask.alphaCutoff = Mathf.Clamp(edgeSoftness, 0.05f, 0.95f);

        // Мерцание
        float r = baseRadius;
        if (flicker)
            r += Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) * flickerAmount * baseRadius;

        // Компенсация размера от мягкости
        float comp = Mathf.Lerp(1f, softnessCompensation, edgeSoftness);
        float finalR = r * comp;

        maskTransform.localPosition = new Vector3(offset.x, offset.y, 0f);
        maskTransform.localScale = Vector3.one * finalR * 2f;
    }
}