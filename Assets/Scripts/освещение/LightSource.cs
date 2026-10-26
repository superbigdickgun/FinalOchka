using UnityEngine;

[RequireComponent(typeof(SpriteRenderer))]
public class LightSource : MonoBehaviour
{
    [Header("=== СВЕТ ===")]
    public float radius = 3f;
    public float softness = 1.5f;
    [Range(0f, 1f)] public float intensity = 0.9f;

    [Header("=== МЕРЦАНИЕ ===")]
    public bool flicker = false;
    public float flickerAmount = 0.1f;
    public float flickerSpeed = 8f;

    [Header("=== ЦВЕТ ===")]
    public Color lightColor = new Color(1f, 0.85f, 0.55f);

    [Header("=== ССЫЛКИ ===")]
    public DarknessOverlay darknessOverlay;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    private SpriteMask mask;
    private Transform maskTransform;
    private float baseRadius;
    private int lightIndex = -1;

    void Awake()
    {
        baseRadius = radius;

        // ✅ Создаём маску
        var go = new GameObject("LightMask");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        maskTransform = go.transform;

        mask = go.AddComponent<SpriteMask>();
        mask.sprite = LightSpriteGenerator.SoftCircle;
        mask.isCustomRangeActive = true;
        mask.frontSortingOrder = 100;
        mask.backSortingOrder = 0;

        // ✅ Находим DarknessOverlay
        if (darknessOverlay == null)
            darknessOverlay = FindObjectOfType<DarknessOverlay>();

        if (darknessOverlay == null)
        {
            Debug.LogError("[LightSource] DarknessOverlay НЕ НАЙДЕН! Свет не будет работать.");
            return;
        }

        // ✅ Проверяем, что массив lights не пустой
        if (darknessOverlay.lights == null || darknessOverlay.lights.Length == 0)
        {
            Debug.LogError("[LightSource] Массив lights в DarknessOverlay ПУСТ! Увеличьте Size в инспекторе камеры.");
            return;
        }

        // ✅ Ищем свободный слот
        lightIndex = AddLightToOverlay();

        if (debugLogs)
            Debug.Log("[LightSource] Свет добавлен в слот: " + lightIndex + " (всего слотов: " + darknessOverlay.lights.Length + ")");
    }

    int AddLightToOverlay()
    {
        if (darknessOverlay == null) return -1;

        for (int i = 0; i < darknessOverlay.lights.Length; i++)
        {
            if (darknessOverlay.lights[i] == null || darknessOverlay.lights[i].lightTransform == null)
            {
                darknessOverlay.lights[i] = new DarknessOverlay.LightData();
                darknessOverlay.lights[i].lightTransform = maskTransform;
                darknessOverlay.lights[i].radius = radius;
                darknessOverlay.lights[i].softness = softness;
                darknessOverlay.lights[i].intensity = intensity;
                darknessOverlay.lights[i].flicker = flicker;
                darknessOverlay.lights[i].flickerAmount = flickerAmount;
                darknessOverlay.lights[i].flickerSpeed = flickerSpeed;
                return i;
            }
        }

        Debug.LogWarning("[LightSource] Нет свободного слота! Увеличьте Size массива Lights в DarknessOverlay.");
        return -1;
    }

    void LateUpdate()
    {
        if (mask == null || lightIndex < 0) return;

        float r = baseRadius;
        if (flicker)
            r += Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) * flickerAmount * baseRadius;

        maskTransform.localScale = Vector3.one * r * 2f;

        if (darknessOverlay != null && lightIndex < darknessOverlay.lights.Length)
        {
            var light = darknessOverlay.lights[lightIndex];
            if (light != null)
            {
                light.intensity = intensity;
                light.radius = r;
                light.softness = softness;
                light.flicker = flicker;
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);
    }
}