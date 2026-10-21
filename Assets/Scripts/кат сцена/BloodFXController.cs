using UnityEngine;

public class BloodFXController : MonoBehaviour
{
    [Header("=== ЛУЖА КРОВИ ===")]
    public Color puddleColor = new Color(0.4f, 0f, 0f, 0.9f);
    public Vector2 puddleSizeRange = new Vector2(1.5f, 2.5f);

    [Header("=== БРЫЗГИ (ФОНТАН) ===")]
    public Color dropColor = new Color(0.6f, 0f, 0f, 0.9f);
    public int dropCount = 35;
    [Range(0.5f, 10f)] public float splatterRadius = 2.5f;
    public float dropLifetime = 1.2f;
    public float dropSize = 0.15f;

    [Header("=== ПОЛОСА (СЛЕД) ===")]
    public Color trailColor = new Color(0.3f, 0f, 0f, 0.8f);
    public float trailSize = 0.3f;

    // ✅ ЖЁСТКИЕ СЛОИ (НИКАКИХ НАСТРОЕК)
    private const int PUDDLE_ORDER = 1;
    private const int TRAIL_ORDER = 2;
    private const int SPLATTER_ORDER = 20;

    private Sprite puddleSprite;
    private Sprite dropSprite;
    private Sprite trailSprite;

    void Awake()
    {
        puddleSprite = CreateBlobSprite(32);
        dropSprite = CreateSquareSprite(8);
        trailSprite = CreateSquareSprite(6);
    }

    public void SpawnPuddle(Vector3 position)
    {
        var go = new GameObject("BloodPuddle");
        go.transform.position = position;
        go.transform.SetParent(transform);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = puddleSprite;
        sr.color = puddleColor;
        sr.sortingOrder = PUDDLE_ORDER; // ✅ 1

        float size = Random.Range(puddleSizeRange.x, puddleSizeRange.y);
        go.transform.localScale = new Vector3(size, size * 0.8f, 1f);
        go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
    }

    public void SpawnSplatter(Vector3 position)
    {
        for (int i = 0; i < dropCount; i++)
        {
            var go = new GameObject("BloodDrop");
            go.transform.position = position;
            go.transform.SetParent(transform);

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = dropSprite;
            sr.color = dropColor;
            sr.sortingOrder = SPLATTER_ORDER; // ✅ 20

            go.transform.localScale = Vector3.one * dropSize;

            var drop = go.AddComponent<BloodDrop>();
            
            Vector2 dir = Random.insideUnitCircle.normalized;
            float speed = splatterRadius / dropLifetime; 
            speed *= Random.Range(0.7f, 1.3f);

            drop.velocity = dir * speed;
            drop.lifetime = dropLifetime;
        }
    }

    public void SpawnTrail(Vector3 position)
    {
        var go = new GameObject("BloodTrail");
        go.transform.position = position;
        go.transform.SetParent(transform);

        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = trailSprite;
        sr.color = trailColor;
        sr.sortingOrder = TRAIL_ORDER; // ✅ 2
        
        go.transform.localScale = Vector3.one * trailSize;
        go.transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
    }

    public void FreezeAll()
    {
        var drops = GetComponentsInChildren<BloodDrop>();
        foreach (var drop in drops) drop.Freeze();
    }

    private Sprite CreateBlobSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        
        Color[] px = new Color[size * size];
        for (int i = 0; i < px.Length; i++) px[i] = Color.clear;
        tex.SetPixels(px);

        float center = size * 0.5f;
        int drops = Random.Range(4, 7);

        for (int d = 0; d < drops; d++)
        {
            float ox = center + Random.Range(-size * 0.2f, size * 0.2f);
            float oy = center + Random.Range(-size * 0.2f, size * 0.2f);
            float radius = Random.Range(size * 0.2f, size * 0.4f);
            float noiseOff = Random.Range(0f, 100f);

            for (int y = 0; y < size; y++)
            {
                for (int x = 0; x < size; x++)
                {
                    float dx = x - ox;
                    float dy = y - oy;
                    float dist = Mathf.Sqrt(dx * dx + dy * dy);
                    float noise = Mathf.PerlinNoise((x + noiseOff) * 0.3f, (y + noiseOff) * 0.3f);
                    
                    if (dist < radius * (0.8f + noise * 0.4f))
                        tex.SetPixel(x, y, Color.white);
                }
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    private Sprite CreateSquareSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        for (int y = 0; y < size; y++)
            for (int x = 0; x < size; x++)
                tex.SetPixel(x, y, Color.white);
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }
}