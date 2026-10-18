using UnityEngine;

public class BarnFloor : MonoBehaviour
{
    [Header("=== ЗОНА ===")]
    [Tooltip("Коллайдер пола. Только зона, невидимая.")]
    public Collider2D areaCollider;

    void Awake()
    {
        if (areaCollider == null) areaCollider = GetComponent<Collider2D>();

        if (areaCollider == null)
        {
            var box = gameObject.AddComponent<BoxCollider2D>();
            box.size = new Vector2(1f, 1f);
            box.isTrigger = true;
            areaCollider = box;
            Debug.Log("[BarnFloor] BoxCollider2D СОЗДАН автоматически");
        }
    }

    public Vector2 GetRandomPoint()
    {
        if (areaCollider == null) return transform.position;

        Bounds bounds = areaCollider.bounds;

        for (int i = 0; i < 20; i++)
        {
            float x = Random.Range(bounds.min.x + 0.3f, bounds.max.x - 0.3f);
            float y = Random.Range(bounds.min.y + 0.3f, bounds.max.y - 0.3f);
            Vector2 point = new Vector2(x, y);

            if (areaCollider.OverlapPoint(point)) return point;
        }

        return (Vector2)transform.position;
    }

    public Vector2 GetRandomCorner()
    {
        if (areaCollider == null) return transform.position;

        Bounds bounds = areaCollider.bounds;

        int corner = Random.Range(0, 4);
        float x = (corner == 0 || corner == 3) ? bounds.min.x + 0.5f : bounds.max.x - 0.5f;
        float y = (corner == 0 || corner == 1) ? bounds.min.y + 0.5f : bounds.max.y - 0.5f;

        return new Vector2(x, y);
    }
}