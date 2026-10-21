using UnityEngine;

public class BloodDrop : MonoBehaviour
{
    [Header("=== ДВИЖЕНИЕ ===")]
    public Vector2 velocity;
    public float lifetime = 1.5f;

    private SpriteRenderer sr;
    private float timer = 0f;
    private bool isFrozen = false;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
    }

    void Update()
    {
        if (isFrozen) return;

        timer += Time.deltaTime;
        transform.position += (Vector3)velocity * Time.deltaTime;

        // Плавное торможение (опционально)
        velocity *= 0.98f;

        if (timer >= lifetime)
        {
            // ✅ НЕ УНИЧТОЖАЕМ, А ЗАМОРАЖИВАЕМ НА МЕСТЕ
            Freeze();
        }
    }

    public void Freeze()
    {
        isFrozen = true;
        velocity = Vector2.zero;
        
        // Делаем каплю темнее и чуть меньше (как засохшее пятно)
        if (sr != null)
        {
            sr.color = new Color(sr.color.r * 0.7f, sr.color.g * 0.7f, sr.color.b * 0.7f, 0.8f);
            transform.localScale *= 0.9f;
        }
    }
}