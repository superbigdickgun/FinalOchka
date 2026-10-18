using UnityEngine;

[DefaultExecutionOrder(200)]
public class VisionCone2D : MonoBehaviour
{
    [Header("=== ФОРМА ===")]
    [Tooltip("Длина конуса в юнитах.")]
    public float radius = 4f;

    [Tooltip("Полный угол в градусах. 360 = круг, 5 = тонкая линия.")]
    [Range(1f, 360f)] public float angle = 80f;

    [Tooltip("Сегментов. Больше = плавнее.")]
    [Range(3, 128)] public int segments = 48;

    [Header("=== СМЕЩЕНИЕ ===")]
    public Vector2 offset = Vector2.zero;

    [Header("=== ПРЕПЯТСТВИЯ ===")]
    [Tooltip("Слои, которые обрезают конус. Player НЕ включать.")]
    public LayerMask blockerLayers = ~0;

    [Header("=== SORTING ===")]
    [Tooltip("НИЖЕ тьмы (100). Обычно 99.")]
    public int sortingOrder = 99;

    [Header("=== ССЫЛКИ ===")]
    public PlayerModular player;

    private Transform coneTransform;
    private MeshFilter meshFilter;
    private MeshRenderer meshRenderer;
    private Mesh mesh;

    void Start()
    {
        if (player == null) player = GetComponentInParent<PlayerModular>();

        var go = new GameObject("VisionCone");
        go.transform.SetParent(null);
        coneTransform = go.transform;

        meshFilter = go.AddComponent<MeshFilter>();
        mesh = new Mesh();
        mesh.MarkDynamic();
        meshFilter.mesh = mesh;

        meshRenderer = go.AddComponent<MeshRenderer>();
        meshRenderer.material = new Material(Shader.Find("Custom/StencilLightMesh"));
        meshRenderer.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        if (mesh == null || meshRenderer == null) return;

        int seg = Mathf.Clamp(segments, 3, 128);

        Vector2 dir = Vector2.right;

        if (player != null)
        {
            Vector2 aim = player.AimDirection;
            if (aim.sqrMagnitude > 0.01f) dir = aim;
        }
        else if (Camera.main != null)
        {
            Vector3 m = Camera.main.ScreenToWorldPoint(Input.mousePosition);
            m.z = 0;
            Vector2 toMouse = (Vector2)m - (Vector2)transform.position;
            if (toMouse.sqrMagnitude > 0.0001f) dir = toMouse.normalized;
        }

        Vector2 origin = (Vector2)transform.position + offset;
        float halfAngle = angle * 0.5f;
        float baseAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;

        int vertsPerRing = seg + 1;
        int totalVerts = 1 + vertsPerRing;
        int totalTris = seg * 3;

        Vector3[] verts = new Vector3[totalVerts];
        Color[] colors = new Color[totalVerts];
        int[] tris = new int[totalTris];

        verts[0] = Vector3.zero;
        colors[0] = new Color(1, 1, 1, 1);

        for (int i = 0; i <= seg; i++)
        {
            float t = i / (float)seg;
            float a = baseAngle - halfAngle + angle * t;
            float rad = a * Mathf.Deg2Rad;
            Vector2 dirSeg = new Vector2(Mathf.Cos(rad), Mathf.Sin(rad));

            RaycastHit2D hit = Physics2D.Raycast(origin, dirSeg, radius, blockerLayers);
            float dist = hit.collider != null ? hit.distance : radius;

            int idx = 1 + i;
            if (idx < verts.Length)
            {
                verts[idx] = dirSeg * dist;
                colors[idx] = new Color(1, 1, 1, 1);
            }
        }

        int triIdx = 0;
        for (int i = 0; i < seg; i++)
        {
            if (triIdx + 2 >= tris.Length) break;
            tris[triIdx++] = 0;
            tris[triIdx++] = 1 + i;
            tris[triIdx++] = 1 + i + 1;
        }

        mesh.Clear();
        mesh.vertices = verts;
        mesh.colors = colors;
        mesh.triangles = tris;
        mesh.RecalculateBounds();

        coneTransform.position = origin;
    }

    void OnDestroy()
    {
        if (coneTransform != null) Destroy(coneTransform.gameObject);
    }
}