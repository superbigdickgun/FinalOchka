using UnityEngine;

[RequireComponent(typeof(Collider2D))]
public class BarnTrigger : MonoBehaviour
{
    [Header("=== ССЫЛКИ ===")]
    [Tooltip("Король крыс, который начнёт спавнить.")]
    public RatKing ratKing;

    [Header("=== НАСТРОЙКИ ===")]
    [Tooltip("Включать спавн только один раз (при первом входе).")]
    public bool onlyOnce = true;

    [Tooltip("Выключать спавн, когда игрок выходит из зоны.")]
    public bool stopOnExit = false;

    private bool triggered = false;

    void Awake()
    {
        // Коллайдер должен быть триггером
        var col = GetComponent<Collider2D>();
        if (col != null) col.isTrigger = true;

        // На старте спавн выключен
        if (ratKing != null) ratKing.spawnEnabled = false;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        if (onlyOnce && triggered) return;

        triggered = true;

        if (ratKing != null)
            ratKing.spawnEnabled = true;

        Debug.Log("[BarnTrigger] Игрок вошёл — спавн крыс ВКЛЮЧЁН");
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!stopOnExit) return;
        if (!other.CompareTag("Player")) return;

        if (ratKing != null)
            ratKing.spawnEnabled = false;

        Debug.Log("[BarnTrigger] Игрок вышел — спавн крыс ВЫКЛЮЧЕН");
    }
}