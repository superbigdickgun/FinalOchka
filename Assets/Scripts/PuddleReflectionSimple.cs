using UnityEngine;

public class PuddleReflectionSimple : MonoBehaviour
{
    [Header("=== ОТРАЖЕНИЕ ===")]
    public Transform target;
    public SpriteRenderer targetSprite;

    [Header("=== РАЗМЫТИЕ ===")]
    [Range(0f, 1f)] public float blur = 0.5f;
    public float pixelSize = 0.05f;

    [Header("=== ЦВЕТ ===")]
    [Range(0f, 1f)] public float alpha = 0.3f;
    public Color reflectionColor = new Color(0.05f, 0.08f, 0.15f, 1f);

    [Header("=== ПОЗИЦИЯ ===")]
    public float yOffset = 0f;
    public float visibleDistance = 3f;

    [Header("=== SORTING ===")]
    public int sortingOrder = -4;

    private SpriteRenderer reflectionSr;
    private Transform reflectionTransform;
    private SpriteMask reflectionMask;
    private Texture2D workTex;
    private Sprite workSprite;
    private Color[] pixels;
    private int texW, texH;
    private bool initialized = false;

    void Start()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
        }

        if (targetSprite == null && target != null)
            targetSprite = target.GetComponentInChildren<SpriteRenderer>();

        if (targetSprite == null || targetSprite.sprite == null)
        {
            Debug.LogWarning("[PuddleReflectionSimple] Нет targetSprite!");
            enabled = false;
            return;
        }

        // Контейнер отражения
        var go = new GameObject("ReflectionSimple");
        go.transform.SetParent(transform, false);
        reflectionTransform = go.transform;

        // SpriteRenderer
        reflectionSr = go.AddComponent<SpriteRenderer>();
        reflectionSr.sortingOrder = sortingOrder;
        reflectionSr.color = new Color(reflectionColor.r, reflectionColor.g, reflectionColor.b, alpha);
        reflectionSr.maskInteraction = SpriteMaskInteraction.VisibleInsideMask;

        // SpriteMask (обрезает по границам лужи)
        reflectionMask = go.AddComponent<SpriteMask>();
        reflectionMask.sprite = GetPuddleMaskSprite();
        reflectionMask.isCustomRangeActive = true;
        reflectionMask.frontSortingOrder = sortingOrder;
        reflectionMask.backSortingOrder = sortingOrder - 1;

        BuildReflection();
        initialized = true;
    }

    Sprite GetPuddleMaskSprite()
    {
        var gen = GetComponent<PuddleController>();
        if (gen == null) return LightSpriteGenerator.WhitePixel;

        int w = Mathf.RoundToInt(gen.width / Mathf.Max(0.05f, gen.cellSize));
        int h = Mathf.RoundToInt(gen.height / Mathf.Max(0.05f, gen.cellSize));

        w = Mathf.Max(1, w);
        h = Mathf.Max(1, h);

        Texture2D tex = new Texture2D(w, h, TextureFormat.RGBA32, false);
        tex.filterMode = FilterMode.Point;

        Color[] px = new Color[w * h];
        for (int i = 0; i < px.Length; i++) px[i] = Color.white;
        tex.SetPixels(px);
        tex.Apply();

        return Sprite.Create(tex, new Rect(0, 0, w, h), new Vector2(0.5f, 0.5f), 1f);
    }

    void BuildReflection()
    {
        Sprite src = targetSprite.sprite;
        Texture2D srcTex = src.texture;

        if (srcTex == null || !srcTex.isReadable)
        {
            Debug.LogWarning("[PuddleReflectionSimple] Спрайт должен быть Read/Write Enabled!");
            return;
        }

        texW = srcTex.width;
        texH = srcTex.height;

        workTex = new Texture2D(texW, texH, TextureFormat.RGBA32, false);
        workTex.filterMode = FilterMode.Point;
        workTex.SetPixels(srcTex.GetPixels());
        workTex.Apply();
        pixels = workTex.GetPixels();

        if (blur > 0f)
            BlurPixels((int)(blur * 5f));

        workTex.SetPixels(pixels);
        workTex.Apply();

        workSprite = Sprite.Create(workTex,
            new Rect(0, 0, texW, texH),
            new Vector2(0.5f, 0.5f),
            src.pixelsPerUnit);

        reflectionSr.sprite = workSprite;
    }

    void BlurPixels(int radius)
    {
        if (radius <= 0) return;

        Color[] copy = new Color[pixels.Length];
        System.Array.Copy(pixels, copy, pixels.Length);

        for (int y = 0; y < texH; y++)
        {
            for (int x = 0; x < texW; x++)
            {
                Color sum = Color.clear;
                int count = 0;

                for (int dy = -radius; dy <= radius; dy++)
                {
                    for (int dx = -radius; dx <= radius; dx++)
                    {
                        int nx = x + dx;
                        int ny = y + dy;
                        if (nx < 0 || nx >= texW || ny < 0 || ny >= texH) continue;

                        Color c = copy[ny * texW + nx];
                        if (c.a > 0.1f) { sum += c; count++; }
                    }
                }

                if (count > 0)
                {
                    sum /= count;
                    pixels[y * texW + x] = new Color(sum.r, sum.g, sum.b, sum.a > 0.1f ? 1f : 0f);
                }
                else
                {
                    pixels[y * texW + x] = Color.clear;
                }
            }
        }
    }

    void LateUpdate()
    {
        if (!initialized || reflectionSr == null || reflectionTransform == null) return;

        Vector3 tp = target.position;
        float ry = tp.y + yOffset;

        reflectionTransform.position = new Vector3(tp.x, ry, tp.z);
        reflectionTransform.localScale = new Vector3(1f, 1f, 1f);

        reflectionSr.enabled = Vector2.Distance(transform.position, tp) < visibleDistance;
    }
}