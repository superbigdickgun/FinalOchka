using UnityEngine;
using System.Collections.Generic;

public class BloodFootprints : MonoBehaviour
{
    [Header("=== СЛЕДЫ ===")]
    public Vector2 sizeRange = new Vector2(0.1f, 0.15f);
    public int textureSize = 16;

    [Header("=== ЦВЕТА ===")]
    public Color[] footprintColors = new Color[]
    {
        new Color(0.5f, 0f, 0f, 0.9f),
        new Color(0.3f, 0f, 0f, 0.9f),
        new Color(0.6f, 0.1f, 0.1f, 0.8f),
    };

    [Header("=== ШАГИ ===")]
    public int stepsAfterBlood = 10;
    public float stepInterval = 0.3f;
    public float footprintLifetime = 30f;
    public int maxFootprints = 100;

    [Header("=== SORTING ===")]
    public int sortingOrder = -6;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private int stepsRemaining = 0;
    private float stepTimer = 0f;
    private Vector3 lastPos;
    private Transform player;

    private Sprite generatedSprite;
    private Queue<GameObject> footprintPool = new Queue<GameObject>();
    private List<GameObject> activeFootprints = new List<GameObject>();

    // Для оптимизации очистки
    private float cleanupTimer = 0f;
    private const float CLEANUP_INTERVAL = 1f;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            lastPos = player.position;
        }

        generatedSprite = CreateSquareSprite(textureSize);
    }

    Sprite CreateSquareSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float noise = Mathf.PerlinNoise(x * 0.3f + 10f, y * 0.3f + 10f);
                bool isEdge = x == 0 || y == 0 || x == size - 1 || y == size - 1;

                if (isEdge && noise < 0.4f)
                    tex.SetPixel(x, y, Color.clear);
                else
                {
                    float alpha = noise > 0.3f ? 1f : 0.9f;
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
            }
        }

        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void Update()
    {
        if (player == null) return;

        float moved = Vector3.Distance(player.position, lastPos);
        if (moved > 0.05f) lastPos = player.position;

        // Спавн следов
        if (stepsRemaining > 0)
        {
            stepTimer -= Time.deltaTime;
            if (stepTimer <= 0f)
            {
                stepTimer = stepInterval;
                SpawnFootprint(player.position);
                stepsRemaining--;

                if (debugLogs)
                    Debug.Log("[Blood] Следов осталось: " + stepsRemaining);
            }
        }

        // Оптимизированная очистка (раз в секунду, не на каждый след)
        cleanupTimer -= Time.deltaTime;
        if (cleanupTimer <= 0f)
        {
            cleanupTimer = CLEANUP_INTERVAL;
            CleanupOldFootprints();
        }
    }

    public void OnBloodStep()
    {
        stepsRemaining = stepsAfterBlood;
        stepTimer = 0f;

        if (debugLogs)
            Debug.Log("[Blood] Активировано " + stepsAfterBlood + " шагов");
    }

    void SpawnFootprint(Vector3 worldPos)
    {
        GameObject go;

        if (footprintPool.Count > 0)
        {
            go = footprintPool.Dequeue();
            go.SetActive(true);
        }
        else
        {
            go = new GameObject("BloodFootprint");
            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = generatedSprite;
            sr.sortingOrder = sortingOrder;
        }

        go.transform.position = new Vector3(worldPos.x, worldPos.y, 0f);

        float size = Random.Range(sizeRange.x, sizeRange.y);
        go.transform.localScale = Vector3.one * size;

        var sr2 = go.GetComponent<SpriteRenderer>();
        if (sr2 != null && footprintColors.Length > 0)
            sr2.color = footprintColors[Random.Range(0, footprintColors.Length)];

        go.transform.rotation = Quaternion.identity;

        // Храним время создания в имени (для очистки)
        go.name = "BloodFootprint_" + Time.time;

        activeFootprints.Add(go);

        // Лимит (если больше max — возвращаем старые)
        while (activeFootprints.Count > maxFootprints)
        {
            var old = activeFootprints[0];
            activeFootprints.RemoveAt(0);
            ReturnToPool(old);
        }
    }

    // Оптимизированная очистка: раз в секунду, не Invoke на каждый след
    void CleanupOldFootprints()
    {
        if (footprintLifetime <= 0f) return;

        float now = Time.time;

        for (int i = activeFootprints.Count - 1; i >= 0; i--)
        {
            var go = activeFootprints[i];
            if (go == null)
            {
                activeFootprints.RemoveAt(i);
                continue;
            }

            // Извлекаем время из имени
            string name = go.name;
            int underscoreIndex = name.LastIndexOf('_');
            if (underscoreIndex >= 0)
            {
                string timeStr = name.Substring(underscoreIndex + 1);
                if (float.TryParse(timeStr, out float createdAt))
                {
                    if (now - createdAt > footprintLifetime)
                    {
                        ReturnToPool(go);
                        activeFootprints.RemoveAt(i);
                    }
                }
            }
        }
    }

    void ReturnToPool(GameObject go)
    {
        if (go == null) return;
        go.SetActive(false);
        footprintPool.Enqueue(go);
    }

    void OnDrawGizmosSelected()
    {
        if (player != null)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(player.position, 0.3f);
        }
    }
}