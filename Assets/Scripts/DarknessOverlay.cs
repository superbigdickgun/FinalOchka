using UnityEngine;

[RequireComponent(typeof(Camera))]
public class DarknessOverlay : MonoBehaviour
{
    [System.Serializable]
    public class LightData
    {
        public Transform lightTransform;
        public float radius = 3f;
        public float softness = 0.5f;
        [Range(0f, 1f)] public float intensity = 0.9f;
        public bool flicker = false;
        public float flickerAmount = 0.1f;
        public float flickerSpeed = 8f;

        [Header("=== КВАДРАТНЫЙ СВЕТ ===")]
        [Tooltip("Если true — свет вырезается прямоугольником, а не кругом.")]
        public bool isSquare = false;

        [Tooltip("Полуразмер квадрата по X.")]
        public float sizeX = 2f;

        [Tooltip("Полуразмер квадрата по Y.")]
        public float sizeY = 2f;

        [Tooltip("Мягкость края квадрата (в юнитах).")]
        public float squareSoftness = 0.5f;
    }

    [Header("=== ТЬМА ===")]
    [Range(0f, 1f)] public float darkness = 1f;
    public Color darknessColor = new Color(0.02f, 0.02f, 0.05f);
    public int sortingOrder = 100;

    [Header("=== КОНУС ИГРОКА ===")]
    public Transform playerTransform;
    public PlayerModular player;
    public float coneRange = 4f;
    [Range(5f, 360f)] public float coneAngle = 80f;
    public float coneSoftness = 1.5f;

    [Header("=== СВЕЧИ (до 6) ===")]
    public LightData[] lights = new LightData[0];

    private SpriteRenderer sr;
    private Transform srTransform;
    private Material mat;
    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();

        var go = new GameObject("DarknessLayer");
        go.transform.SetParent(transform, false);
        srTransform = go.transform;

        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LightSpriteGenerator.WhitePixel;
        mat = new Material(Shader.Find("Custom/DarknessFog"));
        sr.material = mat;
        sr.sortingOrder = sortingOrder;

        if (playerTransform == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) playerTransform = p.transform;
        }
        if (player == null && playerTransform != null)
            player = playerTransform.GetComponent<PlayerModular>();

        UpdateSize();
    }

    void LateUpdate()
    {
        if (sr == null || cam == null || mat == null) return;

        srTransform.position = new Vector3(cam.transform.position.x, cam.transform.position.y, 5f);
        UpdateSize();

        // === КОНУС ИГРОКА (без изменений) ===
        if (playerTransform != null)
        {
            mat.SetVector("_PlayerPos", new Vector4(
                playerTransform.position.x,
                playerTransform.position.y, 0, 0));

            Vector2 dir = (player != null) ? player.AimDirection : Vector2.right;
            mat.SetVector("_PlayerDir", new Vector4(dir.x, dir.y, 0, 0));

            mat.SetFloat("_PlayerRange", coneRange);
            mat.SetFloat("_PlayerAngle", coneAngle);
            mat.SetFloat("_PlayerSoftness", Mathf.Max(0.01f, coneSoftness));
        }

        // === СВЕЧИ / СВЕТ (без изменений + новые квадратные параметры) ===
        int count = Mathf.Min(lights.Length, 6);
        mat.SetInt("_LightCount", count);

        for (int i = 0; i < count; i++)
        {
            var l = lights[i];
            if (l == null || l.lightTransform == null)
            {
                mat.SetFloat("_LightIntensity" + i, 0f);
                // Новые параметры — сбрасываем в 0, чтобы старые круги работали как раньше
                mat.SetFloat("_LightIsSquare" + i, 0f);
                continue;
            }

            Vector2 pos = l.lightTransform.position;

            float r = l.radius;
            if (l.flicker)
                r += Mathf.PerlinNoise(Time.time * l.flickerSpeed, i * 10f) * l.flickerAmount * l.radius;

            mat.SetVector("_LightPos" + i, new Vector4(pos.x, pos.y, 0, 0));
            mat.SetFloat("_LightRadius" + i, r);
            mat.SetFloat("_LightSoftness" + i, Mathf.Max(0.01f, l.softness));
            mat.SetFloat("_LightIntensity" + i, l.intensity);

            // === НОВОЕ: параметры квадрата ===
            mat.SetFloat("_LightIsSquare" + i, l.isSquare ? 1f : 0f);
            mat.SetFloat("_LightSizeX" + i, l.sizeX);
            mat.SetFloat("_LightSizeY" + i, l.sizeY);
            mat.SetFloat("_LightSquareSoftness" + i, Mathf.Max(0.01f, l.squareSoftness));
        }

        mat.SetColor("_Color", new Color(
            darknessColor.r, darknessColor.g, darknessColor.b, darkness));
    }

    void UpdateSize()
    {
        if (cam == null) return;
        float h = cam.orthographicSize * 2f + 4f;
        float w = h * cam.aspect + 4f;
        srTransform.localScale = new Vector3(w, h, 1f);
    }
}