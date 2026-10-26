using UnityEngine;
using System.Collections.Generic;

public class SnowFootprints : MonoBehaviour
{
    [Header("=== ИГРОК ===")]
    public Transform player;

    [Header("=== СНЕГ (ВСЕ ЗАПЕЧЁННЫЕ СПРАЙТЫ) ===")]
    [Tooltip("Перетащите сюда все BakedSnow (запечённые снежные пятна).")]
    public SpriteRenderer[] snowPatches;

    [Header("=== СЛЕД ===")]
    public float holeSize = 0.3f;
    public Color holeColor = new Color(0.3f, 0.3f, 0.3f, 0.8f);
    public int holeSortingOrder = 10;

    [Header("=== ТАЙМЕР ===")]
    public float holeLifetime = 4f;
    public float spawnInterval = 0.3f;

    [Header("=== ОПТИМИЗАЦИЯ ===")]
    public int maxHoles = 50;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private float timer = 0f;
    private List<GameObject> activeHoles = new List<GameObject>();
    private Queue<GameObject> holePool = new Queue<GameObject>();
    private Vector3 lastPlayerPos;

    void Start()
    {
        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (player != null) lastPlayerPos = player.position;
    }

    void Update()
    {
        if (player == null || snowPatches == null || snowPatches.Length == 0) return;

        float moved = Vector3.Distance(player.position, lastPlayerPos);
        if (moved < 0.05f) return;

        lastPlayerPos = player.position;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = spawnInterval;

            // ✅ Проверяем, есть ли снег в позиции игрока
            if (IsOnSnow(player.position))
            {
                SpawnHole(player.position);
            }
        }
    }

    bool IsOnSnow(Vector3 worldPos)
    {
        foreach (var patch in snowPatches)
        {
            if (patch == null || patch.sprite == null) continue;

            // Позиция игрока в локальных координатах снега
            Vector3 localPos = patch.transform.InverseTransformPoint(worldPos);

            // Размер спрайта в юнитах
            Bounds bounds = patch.sprite.bounds;
            float spriteW = bounds.size.x;
            float spriteH = bounds.size.y;

            // Нормализуем в [0..1]
            float u = (localPos.x / spriteW) + 0.5f;
            float v = (localPos.y / spriteH) + 0.5f;

            if (u < 0f || u > 1f || v < 0f || v > 1f) continue;

            // Читаем пиксель
            Texture2D tex = patch.sprite.texture;
            if (tex == null || !tex.isReadable) continue;

            int px = Mathf.Clamp(Mathf.RoundToInt(u * tex.width), 0, tex.width - 1);
            int py = Mathf.Clamp(Mathf.RoundToInt(v * tex.height), 0, tex.height - 1);

            Color pixel = tex.GetPixel(px, py);
            if (pixel.a > 0.1f) return true; // Видимый снег
        }

        return false;
    }

    void SpawnHole(Vector2 position)
    {
        if (activeHoles.Count >= maxHoles)
        {
            var old = activeHoles[0];
            activeHoles.RemoveAt(0);
            if (old != null) ReturnToPool(old);
        }

        GameObject hole = GetFromPool();
        if (hole == null) return;

        hole.SetActive(true);
        hole.transform.position = new Vector3(position.x, position.y, 0f);
        hole.transform.localScale = Vector3.one * holeSize;
        hole.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));

        var sr = hole.GetComponent<SpriteRenderer>();
        if (sr != null)
        {
            sr.color = holeColor;
            sr.sortingOrder = holeSortingOrder;
        }

        var holeScript = hole.GetComponent<SnowHole>();
        if (holeScript == null) holeScript = hole.AddComponent<SnowHole>();
        holeScript.lifetime = holeLifetime;
        holeScript.onDestroyCallback = () => 
        {
            if (hole != null) 
            {
                activeHoles.Remove(hole);
                ReturnToPool(hole);
            }
        };

        activeHoles.Add(hole);

        if (debugLogs)
            Debug.Log("[SnowFootprints] След создан.");
    }

    GameObject GetFromPool()
    {
        while (holePool.Count > 0)
        {
            var pooled = holePool.Dequeue();
            if (pooled != null) return pooled;
        }

        var go = new GameObject("SnowHole");
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LightSpriteGenerator.WhitePixel;
        sr.color = holeColor;
        sr.sortingOrder = holeSortingOrder;
        go.transform.localScale = Vector3.one * holeSize;

        return go;
    }

    void ReturnToPool(GameObject hole)
    {
        if (hole == null) return;
        hole.SetActive(false);
        holePool.Enqueue(hole);
    }
}