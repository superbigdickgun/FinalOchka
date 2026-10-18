using UnityEngine;

[RequireComponent(typeof(SpriteMask))]
public class PlayerLightMask : MonoBehaviour
{
    [Header("=== СВЕТ ===")]
    [Tooltip("Радиус круга света (в юнитах).")]
    public float radius = 4f;

    [Header("=== СГЛАЖИВАНИЕ КРАЯ ===")]
    [Tooltip("0 = жёсткий край, 1 = очень мягкий. 0.3 = стандарт.")]
    [Range(0f, 1f)] public float edgeSoftness = 0.3f;

    [Tooltip("Компенсация: увеличивает радиус, чтобы мягкий край не «съел» размер.")]
    [Range(1f, 2f)] public float softnessRadiusCompensation = 1.3f;

    [Header("=== SORTING ===")]
    public int frontSortingOrder = 100;
    public int backSortingOrder = 99;

    [Header("=== МЕРЦАНИЕ (для свечи) ===")]
    public bool flicker = false;
    public float flickerAmount = 0.1f;
    public float flickerSpeed = 8f;

    private SpriteMask mask;
    private float baseRadius;

    void Awake()
    {
        mask = GetComponent<SpriteMask>();
        mask.sprite = LightSpriteGenerator.SoftCircle;

        mask.isCustomRangeActive = true;
        mask.frontSortingOrder = frontSortingOrder;
        mask.backSortingOrder = backSortingOrder;

        baseRadius = radius;

        ApplySoftness();
    }

    void Update()
    {
        // Меняем мягкость в Play — сразу видно
        ApplySoftness();
    }

    void ApplySoftness()
    {
        if (mask == null) return;
        mask.alphaCutoff = Mathf.Clamp01(edgeSoftness);
    }

    void LateUpdate()
    {
        if (mask == null) return;

        float r = baseRadius;
        if (flicker)
            r += Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) * flickerAmount * baseRadius;

        // Компенсация радиуса — чем мягче, тем больше спрайт
        float compensate = Mathf.Lerp(1f, softnessRadiusCompensation, edgeSoftness);
        float finalRadius = r * compensate;

        transform.localScale = Vector3.one * finalRadius * 2f;
    }
}