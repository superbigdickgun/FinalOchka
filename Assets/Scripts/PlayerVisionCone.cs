using UnityEngine;

public class PlayerVisionCone : MonoBehaviour
{
    [Header("=== ФОРМА ===")]
    [Tooltip("Длина конуса в юнитах.")]
    public float range = 4f;

    [Tooltip("Полный угол конуса в градусах.")]
    [Range(10f, 360f)] public float angle = 80f;

    [Tooltip("Сегментов. Больше = плавнее.")]
    [Range(3, 64)] public int segments = 32;

    [Header("=== ПЛАВНОСТЬ КРАЯ ===")]
    [Tooltip("Ширина плавного угасания на конце конуса (в юнитах).")]
    public float edgeFade = 0.8f;

    [Header("=== ЦВЕТ ===")]
    public Color coneColor = new Color(1f, 1f, 0.9f, 0.85f);

    [Header("=== ПРЕПЯТСТВИЯ ===")]
    [Tooltip("Слои, которые обрезают конус (стены). Player НЕ включать.")]
    public LayerMask blockerLayers = ~0;

    [Header("=== SORTING ===")]
    [Tooltip("Порядок отрисовки. Выше тьмы (100).")]
    public int sortingOrder = 900;

    [Header("=== ССЫЛКИ ===")]
    public PlayerModular player;

    private Transform coneTransform;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    void Start()
    {
        if (player == null) player = GetComponent<PlayerModular>();

        var go = new GameObject("VisionCone");
        go.transform.SetParent(null);
        coneTransform = go.transform;

        meshFilter = go.AddComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.MarkDynamic();
        meshFilter.mesh = mesh;

        meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Sprites/Default"));
        meshRenderer.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        if (player == null || mesh == null) return;

        Vector2 dir = player.AimDirection;
        if (dir.sqrMagnitude < 0.01f) dir = Vector2.right;

        Vector2 origin = transform.position;
        float halfAngle = angle * 0.5f;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        int vertsPerRing = segments + 1;
        int totalVerts = 1 + vertsPerRing * 2;
        int totalTris = segments * 6;

        Vector3[] verts = new Vector3[totalVerts];
        Color[] colors = new Color[totalVerts];
        int[] tris = new int[totalTris];

        verts[0] = Vector3.zero;
        colors[0] = new Color(coneColor.r, coneColor.g, coneColor.b, coneColor.a);

        float innerRange = Mathf.Max(0.01f, range - edgeFade);
        float outerRange = range;

        // Внутренний ряд — плотный свет
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float a = baseAngle - halfAngle + angle * t;
            float rad = a * Mathf.Deg2Rad;
            Vector2 dirSeg = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            RaycastHit2D hit = Physics2D.Raycast(origin, dirSeg, outerRange, blockerLayers);
            float distMax = hit.collider != null ? hit.distance : outerRange;
            float distInner = Mathf.Min(distMax, innerRange);

            int idx = 1 + i;
            if (idx < verts.Length)
            {
                verts[idx] = dirSeg * distInner;
                colors[idx] = new Color(coneColor.r, coneColor.g, coneColor.b, coneColor.a);
            }
        }

        // Внешний ряд — прозрачный край
        for (int i = 0; i <= segments; i++)
        {
            float t = i / (float)segments;
            float a = baseAngle - halfAngle + angle * t;
            float rad = a * Mathf.Deg2Rad;
            Vector2 dirSeg = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            RaycastHit2D hit = Physics2D.Raycast(origin, dirSeg, outerRange, blockerLayers);
            float distMax = hit.collider != null ? hit.distance : outerRange;

            int idx = 1 + vertsPerRing + i;
            if (idx < verts.Length)
            {
                verts[idx] = dirSeg * distMax;
                colors[idx] = new Color(coneColor.r, coneColor.g, coneColor.b, 0f);
            }
        }

        // Треугольники: центр → внутренний
        int triIdx = 0;
        for (int i = 0; i < segments; i++)
        {
            if (triIdx + 2 >= tris.Length) break;
            tris[triIdx++] = 0;
            tris[triIdx++] = 1 + i;
            tris[triIdx++] = 1 + i + 1;
        }

        // Внутренний → внешний
        for (int i = 0; i < segments; i++)
        {
            if (triIdx + 5 >= tris.Length) break;

            int ia = 1 + i;
            int ib = 1 + i + 1;
            int oa = 1 + vertsPerRing + i;
            int ob = 1 + vertsPerRing + i + 1;

            tris[triIdx++] = ia;
            tris[triIdx++] = oa;
            tris[triIdx++] = ib;

            tris[triIdx++] = ib;
            tris[triIdx++] = oa;
            tris[triIdx++] = ob;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        coneTransform.position = origin;

        if (meshRenderer != null && meshRenderer.material != null)
            meshRenderer.material.color = coneColor;
    }

    void OnDestroy()
    {
        if (coneTransform != null) Destroy(coneTransform.gameObject);
    }
}