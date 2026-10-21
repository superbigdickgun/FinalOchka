using UnityEngine;

public class BloodDecal : MonoBehaviour
{
    [Header("=== ПЯТНО ===")]
    public Vector2 sizeRange = new Vector2(0.3f, 0.8f);
    public int decalCount = 1;
    public float spreadBetweenDecals = 0.2f;

    [Header("=== ЦВЕТА ===")]
    public Color[] bloodColors = new Color[]
    {
        new Color(0.5f, 0f, 0f, 0.9f),
        new Color(0.3f, 0f, 0f, 0.9f),
        new Color(0.6f, 0.1f, 0.1f, 0.8f),
    };

    [Header("=== SORTING ===")]
    public int sortingOrder = 1;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    void Start()
    {
        for (int i = 0; i < decalCount; i++)
            SpawnDecal(i);

        // НЕ уничтожаем себя — пятна остаются
        // Destroy(gameObject, 0.1f); // ← УБРАНО
    }

    void SpawnDecal(int index)
    {
        var go = new GameObject("BloodDecal_" + index);
        go.transform.SetParent(null);
        go.transform.position = transform.position + (Vector3)(Random.insideUnitCircle * spreadBetweenDecals * index);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LightSpriteGenerator.SoftCircle;
        sr.color = bloodColors[Random.Range(0, bloodColors.Length)];
        sr.sortingOrder = sortingOrder;

        float size = Random.Range(sizeRange.x, sizeRange.y);
        go.transform.localScale = new Vector3(size, size * 0.7f, 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        // ❌ НЕ добавляем коллайдер (оптимизация)

        if (debugLogs)
            Debug.Log("[BloodDecal] Пятно " + index + " создано");
    }
}