using UnityEngine;

public class BloodSplatter : MonoBehaviour
{
    [Header("=== ЧАСТИЦЫ ===")]
    [Tooltip("Сколько брызг вылетает при смерти.")]
    public int particleCount = 20;

    [Tooltip("Минимальная скорость брызг.")]
    public float speedMin = 2f;

    [Tooltip("Максимальная скорость брызг.")]
    public float speedMax = 6f;

    [Tooltip("Минимальный радиус разлёта (в юнитах).")]
    public float spreadRadiusMin = 0.1f;

    [Tooltip("Максимальный радиус разлёта.")]
    public float spreadRadiusMax = 0.5f;

    [Header("=== РАЗМЕР ПИКСЕЛЯ ===")]
    [Tooltip("Минимальный размер пикселя.")]
    public float sizeMin = 0.05f;

    [Tooltip("Максимальный размер пикселя.")]
    public float sizeMax = 0.15f;

    [Header("=== ВРЕМЯ ЖИЗНИ ===")]
    public float lifetimeMin = 0.4f;
    public float lifetimeMax = 1f;

    [Header("=== ЦВЕТА (разные оттенки) ===")]
    public Color[] bloodColors = new Color[]
    {
        new Color(0.7f, 0f, 0f, 1f),
        new Color(0.5f, 0f, 0f, 1f),
        new Color(0.9f, 0.1f, 0.1f, 1f),
        new Color(0.4f, 0f, 0f, 1f),
    };

    [Header("=== SORTING ===")]
    public int sortingOrder = 20;

    void Start()
    {
        for (int i = 0; i < particleCount; i++)
            SpawnParticle();

        Destroy(gameObject, lifetimeMax + 0.5f);
    }

    void SpawnParticle()
    {
        var go = new GameObject("BloodParticle");
        go.transform.position = transform.position;

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LightSpriteGenerator.WhitePixel;
        sr.color = bloodColors[Random.Range(0, bloodColors.Length)];
        sr.sortingOrder = sortingOrder;

        float size = Random.Range(sizeMin, sizeMax);
        go.transform.localScale = Vector3.one * size;

        var rb = go.AddComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Dynamic;
        rb.gravityScale = 1f;

        Vector2 dir = Random.insideUnitCircle.normalized;
        float radius = Random.Range(spreadRadiusMin, spreadRadiusMax);
        float speed = Random.Range(speedMin, speedMax);

        go.transform.position += (Vector3)(dir * radius * 0.2f);
        rb.AddForce(dir * speed, ForceMode2D.Impulse);

        var script = go.AddComponent<BloodParticle>();
        script.lifetime = Random.Range(lifetimeMin, lifetimeMax);
    }
}