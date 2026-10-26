using UnityEngine;

public class PuddleSink : MonoBehaviour
{
    [Header("=== ЛУЖА ===")]
    [Tooltip("Ссылка на лужу (объект с PixelPuddle или SpriteRenderer).")]
    public SpriteRenderer puddleRenderer;

    [Tooltip("Радиус, в котором игрок начинает тонуть.")]
    public float sinkRadius = 3f;

    [Tooltip("Скорость погружения (0 = мгновенно, 1 = медленно).")]
    public float sinkSpeed = 1.5f;

    [Header("=== СЛОИ ===")]
    [Tooltip("Обычный слой игрока (когда не в луже).")]
    public int normalSortingOrder = 10;

    [Tooltip("Слой игрока в луже (должен быть НИЖЕ слоя лужи).")]
    public int sunkSortingOrder = -2;

    private SpriteRenderer[] playerRenderers;
    private int[] originalOrders;
    private float currentSink = 0f; // 0 = на поверхности, 1 = полностью под водой

    void Start()
    {
        playerRenderers = GetComponentsInChildren<SpriteRenderer>();
        originalOrders = new int[playerRenderers.Length];

        for (int i = 0; i < playerRenderers.Length; i++)
        {
            originalOrders[i] = playerRenderers[i].sortingOrder;
        }

        // Если лужа не назначена — ищем на сцене
        if (puddleRenderer == null)
        {
            var puddle = FindObjectOfType<PixelPuddle>();
            if (puddle != null) puddleRenderer = puddle.GetComponent<SpriteRenderer>();
        }
    }

    void Update()
    {
        if (puddleRenderer == null) return;

        float dist = Vector2.Distance(transform.position, puddleRenderer.transform.position);
        bool inPuddle = dist < sinkRadius;

        // Плавно меняем currentSink (0 → 1, 1 → 0)
        float targetSink = inPuddle ? 1f : 0f;
        currentSink = Mathf.MoveTowards(currentSink, targetSink, Time.deltaTime * sinkSpeed);

        // Применяем слои
        for (int i = 0; i < playerRenderers.Length; i++)
        {
            if (playerRenderers[i] == null) continue;

            // Плавно интерполируем между обычным и утонувшим слоем
            int targetOrder = (int)Mathf.Lerp(originalOrders[i], sunkSortingOrder, currentSink);
            playerRenderers[i].sortingOrder = targetOrder;

            // ✅ Плавное затухание (прозрачность)
            float alpha = Mathf.Lerp(1f, 0.3f, currentSink);
            Color c = playerRenderers[i].color;
            c.a = alpha;
            playerRenderers[i].color = c;
        }
    }
}