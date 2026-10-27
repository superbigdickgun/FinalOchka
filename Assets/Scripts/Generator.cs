using UnityEngine;

public class Generator : MonoBehaviour
{
    [Header("=== СОСТОЯНИЕ ===")]
    public bool isOn = false;

    [Header("=== ВИБРАЦИЯ ===")]
    public float vibrationStrength = 0.04f;
    public float vibrationSpeed = 40f;

    [Header("=== СВЕТ ДОМА ===")]
    public HouseLighting houseLighting;

    [Header("=== СВЕТ В АМБАРЕ ===")]
    [Tooltip("Камера со скриптом DarknessOverlay.")]
    public DarknessOverlay darknessOverlay;

    [Tooltip("Индексы слотов в DarknessOverlay, которые включаются от генератора (например, 2 и 3).")]
    public int[] lightIndices = new int[] { 2, 3 };

    [Tooltip("Яркость, которая ставится при включении (1 = полная).")]
    [Range(0f, 1f)] public float lightIntensity = 1f;

    [Header("=== ПОДСКАЗКА ===")]
    public WorldPrompt prompt;
    public string promptOffText = "Нажмите E — включить генератор";
    public string promptOnText = "Генератор работает (E — выключить)";
    public float promptRadius = 2f;
    public bool showPromptAlways = false;

    [Header("=== ВЗАИМОДЕЙСТВИЕ ===")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 1.5f;

    [Header("=== ЗВУК ===")]
    public GeneratorSound generatorSound;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    private Transform player;
    private bool playerInRange;
    private Vector3 basePosition;

    void Start()
    {
        basePosition = transform.localPosition;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (generatorSound == null)
            generatorSound = GetComponent<GeneratorSound>();

        if (prompt == null)
            prompt = GetComponent<WorldPrompt>();

        // Выключаем свет в амбаре на старте
        SetBarnLight(false);
    }

    void Update()
    {
        if (isOn)
        {
            float offsetX = (Mathf.PerlinNoise(Time.time * vibrationSpeed, 0f) - 0.5f) * 2f * vibrationStrength;
            float offsetY = (Mathf.PerlinNoise(0f, Time.time * vibrationSpeed) - 0.5f) * 2f * vibrationStrength;
            transform.localPosition = basePosition + new Vector3(offsetX, offsetY, 0f);
        }
        else
        {
            transform.localPosition = basePosition;
        }

        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        playerInRange = dist < interactRadius;

        if (prompt != null)
        {
            float promptDist = Vector2.Distance(transform.position, player.position);
            bool show = showPromptAlways || promptDist < promptRadius;

            if (show) prompt.SetText(isOn ? promptOnText : promptOffText);
            else prompt.SetText("");
        }

        if (playerInRange && Input.GetKeyDown(interactKey))
            Toggle();
    }

    public void Toggle()
    {
        isOn = !isOn;

        if (generatorSound != null)
            generatorSound.SetOn(isOn);

        SetBarnLight(isOn);

        if (!isOn && houseLighting != null)
            houseLighting.SetPower(false);

        if (debugLogs)
            Debug.Log("[Generator] " + (isOn ? "ВКЛ" : "ВЫКЛ"));
    }

    void SetBarnLight(bool on)
    {
        if (darknessOverlay == null)
        {
            if (debugLogs) Debug.LogWarning("[Generator] Не задан Darkness Overlay!");
            return;
        }

        if (darknessOverlay.lights == null || darknessOverlay.lights.Length == 0)
        {
            if (debugLogs) Debug.LogWarning("[Generator] В DarknessOverlay нет слотов света!");
            return;
        }

        foreach (int index in lightIndices)
        {
            if (index >= 0 && index < darknessOverlay.lights.Length)
            {
                var light = darknessOverlay.lights[index];
                if (light != null)
                {
                    light.intensity = on ? lightIntensity : 0f;
                }
            }
        }

        if (debugLogs)
            Debug.Log("[Generator] Свет в амбаре (слоты " + string.Join(",", lightIndices) + ") " + (on ? "ВКЛ" : "ВЫКЛ"));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);

        if (prompt != null)
        {
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireSphere(transform.position, promptRadius);
        }
    }
}