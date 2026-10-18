using UnityEngine;

[DefaultExecutionOrder(150)]
public class PlayerShadow2D : MonoBehaviour
{
    [Header("=== ТЕНЬ ===")]
    [Tooltip("Смещение тени при максимальной длине (в юнитах).")]
    public float maxShadowLength = 0.6f;

    [Tooltip("Размер спрайта тени (X × Y).")]
    public Vector2 shadowSize = new Vector2(0.6f, 0.25f);

    [Tooltip("Цвет тени.")]
    public Color shadowColor = new Color(0f, 0f, 0f, 0.6f);

    [Header("=== ИСТОЧНИК СВЕТА ===")]
    [Tooltip("Тег объектов-света. Если пусто — ищем по скрипту LightCircle2D.")]
    public string lightTag = "";

    [Tooltip("Максимальное расстояние, при котором тень реагирует (в юнитах).")]
    public float maxLightDistance = 10f;

    [Tooltip("Минимальное расстояние — ближе тень не тянется (стоит под ногами).")]
    public float minLightDistance = 0.5f;

    [Header("=== SORTING ===")]
    public int sortingOrder = 3;

    [Header("=== ФИКСИРОВАННАЯ ТЕНЬ (если света нет) ===")]
    [Tooltip("Смещение тени когда нет источника света.")]
    public Vector2 fallbackOffset = new Vector2(0f, -0.2f);

    [Tooltip("Размер фиксированной тени.")]
    public Vector2 fallbackSize = new Vector2(0.6f, 0.2f);

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
        shadowSr.sprite = LightSpriteGenerator.SoftCircle;
        shadowSr.material = new Material(Shader.Find("Sprites/Default"));
        shadowSr.color = shadowColor;
        shadowSr.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        if (shadowSr == null || target == null) return;

        // Ищем ближайший источник света
        Transform nearest = null;
        float bestDist = float.MaxValue;

        if (!string.IsNullOrEmpty(lightTag))
        {
            var lights = GameObject.FindGameObjectsWithTag(lightTag);
            foreach (var l in lights)
            {
                float d = Vector2.Distance(l.transform.position, target.position);
                if (d < bestDist) { bestDist = d; nearest = l.transform; }
            }
        }
        else
        {
            var circles = FindObjectsOfType<LightCircle2D>();
            foreach (var l in circles)
            {
                float d = Vector2.Distance(l.transform.position, target.position);
                if (d < bestDist) { bestDist = d; nearest = l.transform; }
            }
        }

        if (nearest != null && bestDist < maxLightDistance)
        {
            // Направление ОТ света к игроку
            Vector2 toTarget = ((Vector2)target.position - (Vector2)nearest.position);
            float dist = toTarget.magnitude;
            Vector2 dir = dist > 0.001f ? toTarget / dist : Vector2.up;

            // Насколько вытянута тень — чем ближе к свету, тем длиннее
            float t = 1f - Mathf.Clamp01((dist - minLightDistance) / (maxLightDistance - minLightDistance));
            float length = maxShadowLength * t;

            // Позиция тени — от игрока в сторону от света
            Vector3 shadowPos = target.position + (Vector3)(dir * length);

            // Растягиваем спрайт по направлению
            float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
            shadowTransform.rotation = Quaternion.Euler(0f, 0f, angle);

            // Чем дальше от света — тем меньше и бледнее тень
            float scaleFactor = Mathf.Lerp(0.5f, 1f, t);
            shadowTransform.localScale = new Vector3(
                shadowSize.x * (1f + length),
                shadowSize.y * scaleFactor,
                1f);

            shadowTransform.position = shadowPos;

            Color c = shadowColor;
            c.a *= Mathf.Lerp(0.3f, 1f, t);
            shadowSr.color = c;
        }
        else
        {
            // Нет света — фиксированная тень под ногами
            shadowTransform.rotation = Quaternion.identity;
            shadowTransform.position = target.position + (Vector3)fallbackOffset;
            shadowTransform.localScale = new Vector3(fallbackSize.x, fallbackSize.y, 1f);
            shadowSr.color = shadowColor;
        }
    }

    void OnDestroy()
    {
        if (shadowSr != null) Destroy(shadowSr.gameObject);
    }
}