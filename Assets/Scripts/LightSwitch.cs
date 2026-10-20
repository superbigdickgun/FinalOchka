using UnityEngine;

public class LightSwitch : MonoBehaviour
{
    [Header("=== СВЕТ ===")]
    public HouseLighting houseLighting;

    [Header("=== ГЕНЕРАТОР ===")]
    [Tooltip("Если генератор не включён — свет не включится.")]
    public Generator generator;

    [Header("=== ПОДСКАЗКА ===")]
    public WorldPrompt prompt;

    [Header("=== ВЗАИМОДЕЙСТВИЕ ===")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 1.5f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private Transform player;
    private bool playerInRange;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        playerInRange = dist < interactRadius;

        if (prompt != null)
        {
            if (generator != null && !generator.isOn)
                prompt.SetText("Нужно завести генератор");
            else
                prompt.SetText(houseLighting != null && houseLighting.hasPower
                    ? "Нажмите E — выключить свет"
                    : "Нажмите E — включить свет");
        }

        if (playerInRange && Input.GetKeyDown(interactKey))
        {
            Interact();
        }
    }

    // ✅ МЕТОД INTERACT — ОН БЫЛ ПОТЕРЯН
    void Interact()
    {
        if (generator != null && !generator.isOn)
        {
            Debug.Log("[LightSwitch] Нет тока — заведите генератор");
            return;
        }

        if (houseLighting != null)
        {
            houseLighting.SetPower(!houseLighting.hasPower);

            if (debugLogs)
                Debug.Log("[LightSwitch] Свет " + (houseLighting.hasPower ? "ВКЛ" : "ВЫКЛ"));
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactRadius);
    }
}