using UnityEngine;

public class LightCircle2D : MonoBehaviour
{
    [Header("=== СВЕТ ===")]
    public float radius = 4f;
    public Vector2 offset = Vector2.zero;

    [Header("=== SORTING ===")]
    [Tooltip("НИЖЕ тьмы (100). Обычно 99.")]
    public int sortingOrder = 99;

    private SpriteRenderer sr;
    private Transform srTransform;

    void Awake()
    {
        var go = new GameObject("LightMask");
        go.transform.SetParent(transform, false);
        go.transform.localPosition = Vector3.zero;
        srTransform = go.transform;

        sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = LightSpriteGenerator.SoftCircle;
        sr.material = new Material(Shader.Find("Custom/StencilLight"));
        sr.sortingOrder = sortingOrder;
    }

    void LateUpdate()
    {
        if (sr == null) return;

        srTransform.localPosition = new Vector3(offset.x, offset.y, 0f);
        srTransform.localScale = Vector3.one * radius * 2f;

        sr.color = new Color(1, 1, 1, 1);
    }
}