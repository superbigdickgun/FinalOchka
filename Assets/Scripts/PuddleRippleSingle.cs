using UnityEngine;

public class PuddleRippleSingle : MonoBehaviour
{
    public int rippleSegments = 16;
    public float rippleCellSize = 0.1f;
    public float expandSpeed = 2f;
    public float lifetime = 1.5f;
    public float maxRadius = 0f;
    public Color rippleColor = new Color(1f, 1f, 1f, 0.5f);
    public int sortingOrder = -4;

    private Transform container;
    private float timer = 0f;
    private GameObject[] cells;
    private bool initialized = false;

    void Start()
    {
        container = new GameObject("RippleCells").transform;
        container.SetParent(transform, false);
        cells = new GameObject[rippleSegments];

        for (int i = 0; i < rippleSegments; i++)
        {
            float angle = (i / (float)rippleSegments) * Mathf.PI * 2f;
            float x = Mathf.Cos(angle);
            float y = Mathf.Sin(angle);

            var go = new GameObject("RippleCell_" + i);
            go.transform.SetParent(container, false);
            go.transform.localPosition = new Vector3(x, y, 0f);
            go.transform.localScale = Vector3.one * rippleCellSize;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LightSpriteGenerator.WhitePixel;
            sr.color = rippleColor;
            sr.sortingOrder = sortingOrder;
            cells[i] = go;
        }

        initialized = true;
    }

    void Update()
    {
        if (!initialized) return;

        timer += Time.deltaTime;
        float radius = timer * expandSpeed;

        if (maxRadius > 0f && radius > maxRadius)
        {
            Destroy(gameObject);
            return;
        }

        container.localScale = Vector3.one * radius;

        float alpha = Mathf.Lerp(rippleColor.a, 0f, timer / lifetime);
        Color c = new Color(rippleColor.r, rippleColor.g, rippleColor.b, alpha);

        foreach (var cell in cells)
        {
            if (cell == null) continue;
            var sr = cell.GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = c;
        }

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}