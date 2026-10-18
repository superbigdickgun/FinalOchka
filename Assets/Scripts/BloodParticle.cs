using UnityEngine;

public class BloodParticle : MonoBehaviour
{
    public float lifetime = 1f;

    private SpriteRenderer sr;
    private Color startColor;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr != null) startColor = sr.color;
    }

    void Update()
    {
        lifetime -= Time.deltaTime;

        if (sr != null)
        {
            float a = Mathf.Clamp01(lifetime / 0.5f);
            sr.color = new Color(startColor.r, startColor.g, startColor.b, a);
        }

        if (lifetime <= 0f) Destroy(gameObject);
    }
}