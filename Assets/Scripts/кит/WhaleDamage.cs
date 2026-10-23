using UnityEngine;
using System.Collections.Generic;

public class WhaleDamage : MonoBehaviour
{
    [Header("=== ЧАСТИ, ПОЛУЧАЮЩИЕ УРОН ===")]
    public Collider2D[] damageableParts;

    [Header("=== МАСКА КОНТУРА ===")]
    public SpriteMask whaleMask;

    [Header("=== НАСТРОЙКИ УРОНА ===")]
    [Tooltip("Максимум попаданий. 0 = бесконечно.")]
    public int maxHits = 0;

    [Header("=== ПЯТНА НА ТЕЛЕ ===")]
    [Tooltip("Включить/выключить появление пятен на самом теле кита.")]
    public bool enableBodyDecals = true; // ✅ НОВЫЙ ПЕРЕКЛЮЧАТЕЛЬ

    public int decalTextureSize = 16;
    public float decalSize = 0.5f;
    [Range(0f, 1f)] public float decalAlpha = 0.7f;
    public int maxDecals = 200;

    [Header("=== КАПЛИ НА ПОЛУ ===")]
    public GameObject bloodDropPrefab;
    public int dropCount = 10;
    public float dropSpeed = 4f;
    public float dropLifetime = 3f;
    public float dropSizeMin = 0.1f;
    public float dropSizeMax = 0.25f;
    public int maxDrops = 50;

    [Header("=== ПЯТНА НА ПОЛУ ===")]
    public int floorPuddleCount = 2;
    public float floorPuddleSize = 1f;
    [Range(0f, 1f)] public float floorPuddleAlpha = 0.8f;
    public float floorPuddleLifetime = 10f;
    public int maxFloorPuddles = 20;

    [Header("=== ЦВЕТА ===")]
    public Color[] bloodColors = new Color[]
    {
        new Color(0.5f, 0f, 0f, 0.9f),
        new Color(0.3f, 0f, 0f, 0.9f),
        new Color(0.6f, 0.1f, 0.1f, 0.8f),
    };

    private int currentHits = 0;
    
    private List<GameObject> activeDropObjects = new List<GameObject>();
    private List<GameObject> activeDecalObjects = new List<GameObject>();
    private List<GameObject> activeFloorPuddles = new List<GameObject>();

    private Queue<GameObject> dropPool = new Queue<GameObject>();
    private Queue<GameObject> floorPuddlePool = new Queue<GameObject>();
    
    private Sprite generatedDecalSprite;
    private Sprite generatedFloorPuddleSprite;

    void Awake()
    {
        generatedDecalSprite = CreateBloodDecalSprite(decalTextureSize);
        generatedFloorPuddleSprite = CreateBloodDecalSprite(decalTextureSize);
    }

    void CleanupDestroyedObjects()
    {
        for (int i = activeDropObjects.Count - 1; i >= 0; i--)
            if (activeDropObjects[i] == null) activeDropObjects.RemoveAt(i);

        for (int i = activeDecalObjects.Count - 1; i >= 0; i--)
            if (activeDecalObjects[i] == null) activeDecalObjects.RemoveAt(i);

        for (int i = activeFloorPuddles.Count - 1; i >= 0; i--)
            if (activeFloorPuddles[i] == null) activeFloorPuddles.RemoveAt(i);
    }

    public bool IsDamageable(Collider2D col)
    {
        if (damageableParts == null || damageableParts.Length == 0) return true;
        foreach (var part in damageableParts)
        {
            if (part == null) continue;
            if (part == col) return true;
            if (col.transform.IsChildOf(part.transform) || part.transform.IsChildOf(col.transform)) 
                return true;
        }
        return false;
    }

    public void TakeHit(Vector2 hitPoint)
    {
        if (maxHits > 0 && currentHits >= maxHits) return;
        currentHits++;

        CleanupDestroyedObjects();

        SpawnSplatter(hitPoint);
        
        // ✅ ПРОВЕРКА: ВКЛЮЧЕНЫ ЛИ ПЯТНА НА ТЕЛЕ
        if (enableBodyDecals) 
            SpawnDecalOnBody(hitPoint);

        SpawnFloorPuddles(hitPoint);
    }

    void SpawnDecalOnBody(Vector2 position)
    {
        if (generatedDecalSprite == null) return;
        if (maxDecals > 0 && activeDecalObjects.Count >= maxDecals) return;

        GameObject decal = new GameObject("BodyDecal");
        decal.transform.SetParent(transform, false);
        decal.transform.position = new Vector3(position.x, position.y, 0f);
        decal.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
        decal.transform.localScale = Vector3.one * decalSize * Random.Range(0.8f, 1.2f);

        var sr = decal.AddComponent<SpriteRenderer>();
        sr.sprite = generatedDecalSprite;
        
        Color c = bloodColors.Length > 0 
            ? bloodColors[Random.Range(0, bloodColors.Length)] 
            : new Color(0.6f, 0f, 0f, 1f);
        c.a = decalAlpha;
        sr.color = c;

        sr.sortingOrder = 3;

        if (whaleMask != null)
            sr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        activeDecalObjects.Add(decal);
    }

    void SpawnSplatter(Vector2 position)
    {
        if (bloodDropPrefab == null) return;

        for (int i = 0; i < dropCount; i++)
        {
            if (maxDrops > 0 && activeDropObjects.Count >= maxDrops) break;

            GameObject drop = GetFromPool(dropPool, bloodDropPrefab, position);
            if (drop == null) continue;

            drop.SetActive(true);
            drop.transform.position = position;
            drop.transform.localScale = Vector3.one * Random.Range(dropSizeMin, dropSizeMax);

            var sr = drop.GetComponent<SpriteRenderer>();
            if (sr != null) 
            {
                sr.color = bloodColors.Length > 0 
                    ? bloodColors[Random.Range(0, bloodColors.Length)] 
                    : new Color(0.6f, 0f, 0f, 1f);
                sr.maskInteraction = SpriteMaskInteraction.None;
            }

            var rb = drop.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                Vector2 dir = Random.insideUnitCircle.normalized;
                rb.linearVelocity = dir * dropSpeed * Random.Range(0.7f, 1.3f);
            }

            var dropScript = drop.GetComponent<WhaleBloodDrop>();
            if (dropScript == null) dropScript = drop.AddComponent<WhaleBloodDrop>();
            dropScript.lifetime = dropLifetime;
            
            var capturedDrop = drop;
            dropScript.onDestroyCallback = () => 
            { 
                if (capturedDrop != null) 
                {
                    activeDropObjects.Remove(capturedDrop);
                    dropPool.Enqueue(capturedDrop); 
                }
            };

            activeDropObjects.Add(drop);
        }
    }

    void SpawnFloorPuddles(Vector2 hitPoint)
    {
        if (generatedFloorPuddleSprite == null) return;

        for (int i = 0; i < floorPuddleCount; i++)
        {
            if (maxFloorPuddles > 0 && activeFloorPuddles.Count >= maxFloorPuddles) break;

            Vector2 offset = Random.insideUnitCircle * 1.5f;
            Vector2 puddlePos = hitPoint + offset;

            GameObject puddle = GetFromPool(floorPuddlePool, null, puddlePos);
            
            if (puddle == null)
            {
                puddle = new GameObject("FloorPuddle");
                puddle.transform.SetParent(transform.parent);
                var sr = puddle.AddComponent<SpriteRenderer>();
                sr.sprite = generatedFloorPuddleSprite;
                sr.sortingOrder = -1;
                puddle.AddComponent<WhaleFloorPuddle>();
            }

            puddle.SetActive(true);
            puddle.transform.position = new Vector3(puddlePos.x, puddlePos.y, 0f);
            puddle.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
            puddle.transform.localScale = Vector3.one * floorPuddleSize * Random.Range(0.8f, 1.2f);

            var puddleSr = puddle.GetComponent<SpriteRenderer>();
            if (puddleSr != null)
            {
                Color c = bloodColors.Length > 0 
                    ? bloodColors[Random.Range(0, bloodColors.Length)] 
                    : new Color(0.6f, 0f, 0f, 1f);
                c.a = floorPuddleAlpha;
                puddleSr.color = c;
                puddleSr.maskInteraction = SpriteMaskInteraction.None;
            }

            var puddleScript = puddle.GetComponent<WhaleFloorPuddle>();
            if (puddleScript == null) puddleScript = puddle.AddComponent<WhaleFloorPuddle>();
            puddleScript.lifetime = floorPuddleLifetime;
            
            var capturedPuddle = puddle;
            puddleScript.onDestroyCallback = () => 
            { 
                if (capturedPuddle != null) 
                {
                    activeFloorPuddles.Remove(capturedPuddle);
                    floorPuddlePool.Enqueue(capturedPuddle);
                }
            };

            activeFloorPuddles.Add(puddle);
        }
    }

    GameObject GetFromPool(Queue<GameObject> pool, GameObject prefab, Vector3 position)
    {
        while (pool.Count > 0)
        {
            var pooled = pool.Dequeue();
            if (pooled != null) return pooled;
        }
        if (prefab != null) return Instantiate(prefab, position, Quaternion.identity);
        return null;
    }

    Sprite CreateBloodDecalSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        float center = size * 0.5f;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - center) / center;
                float dy = (y - center) / center;
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                float noise = Mathf.PerlinNoise(x * 0.5f + 10f, y * 0.5f + 10f);
                float threshold = 0.8f + (noise - 0.5f) * 0.4f;

                if (dist < threshold)
                {
                    float alpha = Random.Range(0.5f, 1f);
                    tex.SetPixel(x, y, new Color(1f, 1f, 1f, alpha));
                }
                else
                {
                    tex.SetPixel(x, y, Color.clear);
                }
            }
        }
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}