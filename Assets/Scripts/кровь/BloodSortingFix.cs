using UnityEngine;

public class BloodSortingFix : MonoBehaviour
{
    [Header("=== СЛОИ ===")]
    [Tooltip("Слой крови (должен быть НИЖЕ героя).")]
    public int bloodSortingOrder = -1;

    [Tooltip("Слой героя (для проверки).")]
    public int playerSortingOrder = 10;

    [Header("=== ОПТИМИЗАЦИЯ ===")]
    [Tooltip("Как часто проверять (сек). 0.5 = 2 раза в секунду.")]
    public float checkInterval = 0.5f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private float timer = 0f;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer > 0f) return;
        timer = checkInterval;

        // ✅ Находим все объекты с именем "Blood" или компонентом BloodDrop
        var allSr = FindObjectsOfType<SpriteRenderer>();
        foreach (var sr in allSr)
        {
            if (sr == null) continue;

            if (sr.name.Contains("Blood") || sr.GetComponent<BloodDrop>() != null)
            {
                if (sr.sortingOrder >= playerSortingOrder)
                {
                    sr.sortingOrder = bloodSortingOrder;

                    if (debugLogs)
                        Debug.Log("[BloodSortingFix] Кровь " + sr.name + " опущена на слой " + bloodSortingOrder);
                }
            }
        }
    }
}