using UnityEngine;

public class WhaleBloodDrop : MonoBehaviour
{
    public float lifetime = 3f;
    public System.Action onDestroyCallback;

    private Rigidbody2D rb;
    private SpriteRenderer sr;
    private float timer = 0f;
    private bool isLanded = false;

    void Start()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
    }

    void Update()
    {
        timer += Time.deltaTime;

        if (isLanded)
        {
            // Капля лежит на полу и ждет исчезновения
            if (timer >= lifetime)
            {
                onDestroyCallback?.Invoke();
                Destroy(gameObject);
            }
            return;
        }

        // Если капля остановилась — она упала
        if (rb != null && rb.linearVelocity.magnitude < 0.1f && timer > 0.3f)
        {
            isLanded = true;
            rb.linearVelocity = Vector2.zero;
            rb.simulated = false;
            timer = 0f;

            if (sr != null)
            {
                Color c = sr.color;
                c.a = 0.6f;
                sr.color = c;
                transform.localScale = new Vector3(transform.localScale.x, transform.localScale.y * 0.3f, 1f);
            }
        }

        if (timer >= lifetime)
        {
            onDestroyCallback?.Invoke();
            Destroy(gameObject);
        }
    }
}