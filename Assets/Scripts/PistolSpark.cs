using UnityEngine;

public class PistolSpark : MonoBehaviour
{
    public Vector2 velocity;
    public float life = 0.25f;

    private SpriteRenderer sr;
    private float startLife;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        startLife = life;
    }

    void Update()
    {
        velocity *= 0.92f;
        transform.position += (Vector3)velocity * Time.deltaTime;
        life -= Time.deltaTime;

        if (sr != null)
        {
            float alpha = Mathf.Clamp01(life / startLife);
            sr.color = new Color(sr.color.r, sr.color.g, sr.color.b, alpha);
        }

        if (life <= 0f) Destroy(gameObject);
    }
}