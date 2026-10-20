using UnityEngine;

public class HouseLighting : MonoBehaviour
{
    [Header("=== СВЕТ ===")]
    [Tooltip("Объект DarknessOverlay на камере.")]
    public DarknessOverlay darknessOverlay;

    [Tooltip("Индекс света в массиве Lights у DarknessOverlay (начиная с 0).")]
    public int lightIndex = 0;

    [Header("=== НАСТРОЙКИ СВЕТА ===")]
    [Tooltip("Радиус света при включении.")]
    public float lightRadius = 5f;

    [Tooltip("Мягкость края.")]
    public float lightSoftness = 1.5f;

    [Tooltip("Яркость при включении (0..1).")]
    [Range(0f, 1f)] public float lightIntensity = 1f;

    [Header("=== СОСТОЯНИЕ ===")]
    public bool hasPower = false;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private bool lastState = false;

    void Start()
    {
        SetPower(hasPower);
    }

    public void SetPower(bool on)
    {
        hasPower = on;
        lastState = on;

        if (darknessOverlay != null && darknessOverlay.lights != null &&
            lightIndex >= 0 && lightIndex < darknessOverlay.lights.Length)
        {
            var light = darknessOverlay.lights[lightIndex];
            if (light != null)
            {
                // Включаем/выключаем свет через intensity
                light.intensity = on ? lightIntensity : 0f;
                light.radius = lightRadius;
                light.softness = lightSoftness;

                if (debugLogs)
                    Debug.Log("[HouseLighting] Слот " + lightIndex + " intensity = " + light.intensity);
            }
        }
        else
        {
            if (debugLogs)
                Debug.LogWarning("[HouseLighting] Слот " + lightIndex + " не найден в DarknessOverlay.lights");
        }

        if (debugLogs)
            Debug.Log("[HouseLighting] Свет " + (on ? "ВКЛ" : "ВЫКЛ"));
    }

    void Update()
    {
        // Если состояние изменилось извне (например, через инспектор) — применяем
        if (hasPower != lastState)
            SetPower(hasPower);
    }
}