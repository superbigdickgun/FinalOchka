using UnityEngine;

[DefaultExecutionOrder(150)]
public class ShadowCaster2D : MonoBehaviour
{
    [Header("=== ТЕНЬ ===")]
    public Vector2 shadowSize = new Vector2(0.6f, 1f);
    public float maxShadowLength = 1.5f;
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);

    [Header("=== ИСТОЧНИКИ СВЕТА (перетащи свечи сюда) ===")]
    public Transform[] lightSources;

    [Header("=== ДИСТАНЦИИ ===")]
    public float maxLightDistance = 15f;
    public float minLightDistance = 0.3f;

    [Header("=== ФОРМА ===")]
    public Vector2 baseOffset = new Vector2(0f, -0.15f);

    [Header("=== SORTING ===")]
    public int sortingOrder = 3;

    [Header("=== МЯГКОСТЬ ===")]
    public bool useSoftCircle = true;

    private Transform shadowTransform;
    private SpriteRenderer shadowSr;
    private Transform target;

    void Awake()
    {
        target = transform;

        var go = new GameObject("Shadow2D");
        go.transform.SetParent(null);
        shadowTransform = go.transform;

        shadowSr = go.AddComponent<SpriteRenderer>();
        shadowSr.sprite = useSoftCircle
            ? LightSpriteGenerator.SoftCircle
            : LightSpriteGenerator.WhitePixel;
        shadowSr.material = new Material(Shader.Find("Sprites/Default"));
        shadowSr.color = shadowColor;
        shadowSr.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        if (shadowSr == null || target == null) return;

        if (lightSources == null || lightSources.Length == 0)
        {
            // Нет источников — фиксированная тень
            shadowTransform.rotation = Quaternion.identity;
            shadowTransform.position = target.position + (Vector3)baseOffset;
            shadowTransform.localScale = new Vector3(shadowSize.x * 0.6f, shadowSize.y * 0.3f, 1f);
            shadowSr.color = shadowColor;
            return;
        }

        // Ищем ближайший источник
        Transform nearest = null;
        float bestDist = float.MaxValue;

        foreach (var l in lightSources)
        {
            if (l == null) continue;
            float d = Vector2.Distance(l.position, target.position);
            if (d < bestDist) { bestDist = d; nearest = l; }
        }

        if (nearest != null && bestDist < maxLightDistance)
        {
            Vector2 toTarget = (Vector2)target.position - (Vector2)nearest.position;
            float dist = toTarget.magnitude;

            if (dist < 0.001f)
            {
                shadowTransform.rotation = Quaternion.identity;
                shadowTransform.position = target.position + (Vector3)baseOffset;
                shadowTransform.localScale = new Vector3(shadowSize.x * 0.5f, shadowSize.y * 0.3f, 1f);
                shadowSr.color = shadowColor;
                return;
            }

            Vector2 dir = toTarget / dist;

            float t = 1f - Mathf.Clamp01((dist - minLightDistance) / (maxLightDistance - minLightDistance));
            float length = maxShadowLength * t;

            Vector3 shadowPos = target.position + (Vector3)(dir * length * 0.5f) + (Vector3)baseOffset;

            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            shadowTransform.rotation = Quaternion.Euler(0f, 0f, angle);

            shadowTransform.localScale = new Vector3(shadowSize.y * (0.5f + length), shadowSize.x, 1f);
            shadowTransform.position = shadowPos;

            Color c = shadowColor;
            c.a *= Mathf.Lerp(0.4f, 1f, t);
            shadowSr.color = c;
        }
        else
        {
            shadowTransform.rotation = Quaternion.identity;
            shadowTransform.position = target.position + (Vector3)baseOffset;
            shadowTransform.localScale = new Vector3(shadowSize.x * 0.6f, shadowSize.y * 0.3f, 1f);
            shadowSr.color = shadowColor;
        }
    }

    void OnDestroy()
    {
        if (shadowSr != null) Destroy(shadowSr.gameObject);
    }
}