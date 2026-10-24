using UnityEngine;

public class Crow : MonoBehaviour
{
    [Header("=== СПРАЙТЫ ===")]
    public Sprite spriteSit;
    public Sprite spritePeck;
    public Sprite spriteFly1;
    public Sprite spriteFly2;

    [Header("=== АНИМАЦИЯ КЛЕВА ===")]
    public float peckSpeed = 0.15f;
    public int peckCount = 2;
    public float peckIntervalMin = 2f;
    public float peckIntervalMax = 5f;

    [Header("=== БРЫЗГИ ===")]
    public int dropCount = 5;
    public float dropSpeed = 3f;
    public Color bloodColor = new Color(0.6f, 0f, 0f, 0.9f);
    public float dropSize = 0.15f;
    public float dropLifetime = 1.5f;
    public int bloodSortingOrder = 110;

    [Header("=== РАЗЛЁТ ===")]
    public float fleeSpeed = 8f;
    public float fleeDistance = 20f;
    public float wingFlapSpeed = 0.1f;

    public Transform whaleCenter;

    private SpriteRenderer sr;
    private Vector3 basePos;
    private float peckTimer = 0f;
    private bool isPecking = false;
    private bool isFleeing = false;
    private bool isActive = false;
    private float peckAnimTimer = 0f;
    private int currentPeckIndex = 0;
    private Vector3 fleeDirection;
    private float wingTimer = 0f;
    private bool wingUp = false;

    private static Sprite cachedDropSprite;

    void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
        basePos = transform.position;
    }

    public void SetActive(bool active)
    {
        isActive = active;
        if (!active)
        {
            gameObject.SetActive(false);
        }
        else
        {
            gameObject.SetActive(true);
            transform.position = basePos;
            isFleeing = false;
            isPecking = false;
            if (sr != null) sr.sprite = spriteSit;
            peckTimer = Random.Range(peckIntervalMin, peckIntervalMax);
        }
    }

    public void CheckScare(float distanceToPlayer, float scareRadius)
    {
        if (!isActive || isFleeing) return;
        if (distanceToPlayer < scareRadius)
        {
            Flee();
        }
    }

    void Update()
    {
        if (!isActive) return;

        if (isFleeing)
        {
            transform.position += fleeDirection * fleeSpeed * Time.deltaTime;

            wingTimer -= Time.deltaTime;
            if (wingTimer <= 0f)
            {
                wingTimer = wingFlapSpeed;
                wingUp = !wingUp;
                if (sr != null) sr.sprite = wingUp ? spriteFly1 : spriteFly2;
            }

            float angle = Mathf.Atan2(fleeDirection.y, fleeDirection.x) * Mathf.Rad2Deg;
            transform.rotation = Quaternion.Euler(0, 0, angle - 90f);

            // ✅ Если улетел далеко — УНИЧТОЖАЕМ навсегда
            float distFromBase = Vector3.Distance(transform.position, basePos);
            if (distFromBase > fleeDistance)
            {
                Destroy(gameObject); // Удаляем из сцены
            }
            return;
        }

        if (isPecking)
        {
            peckAnimTimer -= Time.deltaTime;
            if (peckAnimTimer <= 0f)
            {
                peckAnimTimer = peckSpeed;

                if (sr.sprite == spriteSit)
                    sr.sprite = spritePeck;
                else
                {
                    sr.sprite = spriteSit;
                    currentPeckIndex++;

                    SpawnBlood();

                    if (currentPeckIndex >= peckCount)
                    {
                        isPecking = false;
                        currentPeckIndex = 0;
                        peckTimer = Random.Range(peckIntervalMin, peckIntervalMax);
                    }
                }
            }
            return;
        }

        peckTimer -= Time.deltaTime;
        if (peckTimer <= 0f)
        {
            isPecking = true;
            currentPeckIndex = 0;
            peckAnimTimer = 0f;
        }
    }

    void Flee()
    {
        isFleeing = true;
        if (sr != null) sr.sprite = spriteFly1;

        if (whaleCenter != null)
        {
            Vector3 dir = (transform.position - whaleCenter.position).normalized;
            dir += new Vector3(Random.Range(-0.3f, 0.3f), Random.Range(-0.3f, 0.3f), 0f);
            fleeDirection = dir.normalized;
        }
        else
        {
            fleeDirection = Vector3.up;
        }

        wingTimer = 0f;
    }

    void SpawnBlood()
    {
        if (cachedDropSprite == null) cachedDropSprite = CreateDropSprite(8);

        Vector3 spawnPos = transform.position;
        if (sr != null && sr.sprite != null)
        {
            Vector2 pivotLocal = sr.sprite.pivot / sr.sprite.rect.size;
            spawnPos = transform.position + new Vector3(
                (pivotLocal.x - 0.5f) * transform.lossyScale.x,
                (pivotLocal.y - 0.5f) * transform.lossyScale.y,
                0f
            );
        }

        for (int i = 0; i < dropCount; i++)
        {
            var go = new GameObject("CrowBloodDrop");
            go.transform.position = spawnPos;
            go.transform.localScale = Vector3.one * dropSize;

            var dropSr = go.AddComponent<SpriteRenderer>();
            dropSr.sprite = cachedDropSprite;
            dropSr.color = bloodColor;
            dropSr.sortingOrder = bloodSortingOrder;

            var mover = go.AddComponent<CrowBloodMover>();
            Vector2 dir = Random.insideUnitCircle.normalized;
            mover.velocity = dir * dropSpeed * Random.Range(0.7f, 1.3f);
            mover.lifetime = dropLifetime;
        }
    }

    Sprite CreateDropSprite(int size)
    {
        Texture2D tex = new Texture2D(size, size, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;
        tex.wrapMode = TextureWrapMode.Clamp;

        for (int y = 0; y < size; y++)
        {
            for (int x = 0; x < size; x++)
            {
                float dx = (x - size * 0.5f) / (size * 0.5f);
                float dy = (y - size * 0.5f) / (size * 0.5f);
                float dist = Mathf.Sqrt(dx * dx + dy * dy);
                
                if (dist < 0.9f + Random.Range(-0.1f, 0.1f))
                    tex.SetPixel(x, y, Color.white);
                else
                    tex.SetPixel(x, y, Color.clear);
            }
        }
        tex.Apply();
        return Sprite.Create(tex, new Rect(0, 0, size, size), new Vector2(0.5f, 0.5f), size);
    }

    void OnDrawGizmosSelected()
    {
        if (sr != null && sr.sprite != null)
        {
            Vector2 pivotLocal = sr.sprite.pivot / sr.sprite.rect.size;
            Vector3 pivotWorld = transform.position + new Vector3(
                (pivotLocal.x - 0.5f) * transform.lossyScale.x,
                (pivotLocal.y - 0.5f) * transform.lossyScale.y,
                0f
            );
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(pivotWorld, 0.1f);
        }
    }
}