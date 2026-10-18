using UnityEngine;

public class CandleLight : MonoBehaviour
{
    [Header("=== СВЕТ ===")]
    public float radius = 3f;
    public Color lightColor = new Color(1f, 0.85f, 0.55f, 0.95f);

    [Header("=== МЕРЦАНИЕ ===")]
    public bool flicker = true;
    public float flickerAmount = 0.1f;
    public float flickerSpeed = 8f;

    [Header("=== SORTING ===")]
    public int sortingOrder = 101;

    private SpriteRenderer sr;
    private Transform srTransform;
    private float baseRadius;

    void Awake()
    {
        baseRadius = radius;

        var go = new GameObject("LightSprite");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        srTransform = go.transform;

        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LightSpriteGenerator.SoftCircle;
        sr.material = new Material(Shader.Find("Sprites/Default"));
        sr.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        if (sr == null) return;

        float r = baseRadius;
        if (flicker)
            r += Mathf.PerlinNoise(Time.time * flickerSpeed, 0f) * flickerAmount * baseRadius;

        srTransform.localScale = Vector3.one * r * 2f;
        sr.color = lightColor;
    }
}