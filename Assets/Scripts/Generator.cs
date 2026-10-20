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

    [Header("=== ПОДСКАЗКА ===")]
    public WorldPrompt prompt;

    [Header("=== ВЗАИМОДЕЙСТВИЕ ===")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 1.5f;

    [Header("=== ЗВУК ===")]
    public GeneratorSound generatorSound;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

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
            prompt.SetText(isOn ? "Генератор работает (E — выключить)" : "Нажмите E — включить генератор");

        if (playerInRange && Input.GetKeyDown(interactKey))
            Toggle();
    }

    public void Toggle()
    {
        isOn = !isOn;

        if (generatorSound != null)
            generatorSound.SetOn(isOn);

        // ✅ При выключении генератора — гасим свет
        if (!isOn && houseLighting != null)
            houseLighting.SetPower(false);

        if (debugLogs)
            Debug.Log("[Generator] " + (isOn ? "ВКЛ" : "ВЫКЛ"));
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}