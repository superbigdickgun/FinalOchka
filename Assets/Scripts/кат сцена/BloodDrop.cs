using UnityEngine;

public class BloodDrop : MonoBehaviour
{
    [Header("=== ДВИЖЕНИЕ ===")]
    public Vector2 velocity;
    public float lifetime = 1.5f;

    private SpriteRenderer sr;
    private float timer = 0f;

    void Start()
    {
        sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        
        // Случайный поворот для разнообразия
        transform.rotation = Quaternion.Euler(0, 0, Random.Range(0f, 360f));
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Просто летим в заданном направлении
        transform.position += (Vector3)velocity * Time.deltaTime;

        // Плавно уменьшаем размер и прозрачность в конце жизни
        if (timer >= lifetime)
        {
            Destroy(gameObject);
        }
    }
}