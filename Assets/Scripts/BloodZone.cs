using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class BloodZone : MonoBehaviour
{
    [Header("=== ЗОНА ===")]
    [Tooltip("Скрипт следов на игроке.")]
    public BloodFootprints footprints;

    [Header("=== ПРОВЕРКА ===")]
    [Tooltip("Как часто проверять (сек). 0.5 = 2 раза в секунду.")]
    public float checkInterval = 0.5f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private float checkTimer = 0f;
    private BoxCollider2D zoneCollider;
    private Transform player;
    private bool playerWasInside = false;

    void Start()
    {
        zoneCollider = GetComponent<BoxCollider2D>();
        zoneCollider.isTrigger = true;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;

            if (footprints == null)
                footprints = p.GetComponent<BloodFootprints>();
            if (footprints == null)
                footprints = p.GetComponentInParent<BloodFootprints>();
        }
    }

    void Update()
    {
        if (player == null || zoneCollider == null) return;

        checkTimer -= Time.deltaTime;
        if (checkTimer > 0f) return;
        checkTimer = checkInterval;

        // Проверяем, внутри ли игрок (без OnTriggerEnter — оптимизация)
        bool inside = zoneCollider.OverlapPoint(player.position);

        if (inside && !playerWasInside)
        {
            // Игрок только что вошёл в зону
            if (footprints != null)
                footprints.OnBloodStep();

            if (debugLogs)
                Debug.Log("[BloodZone] Игрок наступил на кровь!");

            playerWasInside = true;
        }
        else if (!inside && playerWasInside)
        {
            playerWasInside = false;
        }
    }

    void OnDrawGizmosSelected()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null)
        {
            Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
            Gizmos.DrawCube(col.bounds.center, col.bounds.size);
        }
    }
}