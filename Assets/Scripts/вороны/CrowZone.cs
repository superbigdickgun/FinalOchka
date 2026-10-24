using UnityEngine;

public class CrowZone : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ===")]
    public float activeRadius = 15f;
    public float scareRadius = 6f;

    [Header("=== ССЫЛКИ ===")]
    public Transform player;
    public Crow[] crows;
    public Transform whaleCenter;

    [Header("=== КАТ-СЦЕНА ===")]
    public WhaleCutscene cutscene;

    private bool isActive = false;
    private bool playerWasOutside = true;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (whaleCenter == null) whaleCenter = transform;
        if (cutscene == null) cutscene = FindObjectOfType<WhaleCutscene>();
    }

    void Update()
    {
        if (player == null || crows == null || crows.Length == 0) return;

        bool cutscenePlaying = cutscene != null && cutscene.IsPlaying;
        if (cutscenePlaying) return;

        float dist = Vector2.Distance(transform.position, player.position);

        // ✅ ВХОД В ЗОНУ (игрок зашёл)
        if (dist < activeRadius && !isActive)
        {
            isActive = true;

            // Включаем воронов ТОЛЬКО если игрок реально зашёл
            if (playerWasOutside)
            {
                foreach (var crow in crows)
                {
                    if (crow != null)
                    {
                        crow.SetActive(true);
                    }
                }
                playerWasOutside = false;
            }
        }

        // ✅ ВЫХОД ИЗ ЗОНЫ
        if (dist >= activeRadius && isActive)
        {
            isActive = false;
            playerWasOutside = true;

            // Прячем воронов
            foreach (var crow in crows)
            {
                if (crow != null) crow.SetActive(false);
            }
        }

        // ✅ РАЗЛЁТ (только если игрок в зоне)
        if (isActive)
        {
            foreach (var crow in crows)
            {
                if (crow != null && crow.gameObject.activeSelf)
                {
                    crow.CheckScare(dist, scareRadius);
                }
            }
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = new Color(0f, 1f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, activeRadius);
        Gizmos.color = new Color(1f, 0f, 0f, 0.3f);
        Gizmos.DrawWireSphere(transform.position, scareRadius);
    }
}