using UnityEngine;
using System.Collections.Generic;

public class RatKing : MonoBehaviour
{
    [Header("=== СПАВН ===")]
    [Tooltip("Префаб обычной крысы.")]
    public GameObject ratPrefab;

    [Tooltip("Спавн включён.")]
    public bool spawnEnabled = true;

    [Tooltip("Интервал между спавнами (сек).")]
    public float spawnInterval = 3f;

    [Tooltip("Сколько крыс за спавн.")]
    public int ratsPerSpawn = 1;

    [Tooltip("Максимум крыс одновременно.")]
    public int maxRatsAlive = 10;

    [Header("=== РАЗБРОС ===")]
    [Tooltip("Радиус разброса спавна вокруг короля (0 = точно в центр).")]
    public float spawnRadius = 0f;

    [Header("=== ССЫЛКИ ===")]
    public PlayerModular player;

    private float spawnTimer;
    private List<Rat> aliveRats = new List<Rat>();

    void Awake()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.GetComponent<PlayerModular>();
        }
    }

    void Update()
    {
        if (!spawnEnabled) return;
        if (ratPrefab == null) return;

        aliveRats.RemoveAll(r => r == null);

        if (aliveRats.Count >= maxRatsAlive) return;

        spawnTimer -= Time.deltaTime;
        if (spawnTimer > 0f) return;

        spawnTimer = spawnInterval;

        for (int i = 0; i < ratsPerSpawn; i++)
            SpawnRat();
    }

    void SpawnRat()
    {
        // Спавн из ЦЕНТРА короля
        Vector2 spawnPos = transform.position;

        // Если spawnRadius > 0 — разброс вокруг
        if (spawnRadius > 0f)
            spawnPos += Random.insideUnitCircle * spawnRadius;

        var go = Instantiate(ratPrefab, spawnPos, Quaternion.identity);
        var rat = go.GetComponent<Rat>();
        if (rat != null)
        {
            rat.isKing = false;
            rat.player = player;
            aliveRats.Add(rat);
        }

        Debug.Log("[RatKing] Спавн из центра " + spawnPos + ". Всего: " + aliveRats.Count);
    }
}