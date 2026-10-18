using UnityEngine;

public class WallDebris : MonoBehaviour
{
    public float lifetime = 0.6f;

    private SpriteRenderer sr;
    private Color startColor;
    private float startLife;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) startColor = sr.color;
        startLife = lifetime;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;

        if (sr != null)
        {
            float a = Mathf.Clamp01(lifetime / startLife);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, a);
        }

        if (lifetime <= 0f) Destroy(gameObject);
    }
}