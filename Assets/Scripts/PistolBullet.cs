using UnityEngine;

public class PistolBullet : MonoBehaviour
{
    [Header("=== НАСТРОЙКИ ===")]
    public float speed = 25f;
    public float range = 15f;
    public float damage = 10f;
    public Vector2 size = new Vector2(0.3f, 0.08f);
    public Color color = new Color(1f, 0.95f, 0.6f);

    [Header("=== SORTING ===")]
    public int sortingOrder = 950;

    [Header("=== Z ===")]
    public float zPos = -1f;

    private Vector2 direction;
    private float traveled;
    private int bulletLayerMask;

    public void Init(Vector2 dir)
    {
        direction = dir.normalized;
        traveled = 0f;

        // Исключаем Floor и Triggers из маски пули
        int floorLayer = LayerMask.NameToLayer("Floor");
        int triggerLayer = LayerMask.NameToLayer("Triggers");

        bulletLayerMask = ~0;
        if (floorLayer >= 0) bulletLayerMask &= ~(1 << floorLayer);
        if (triggerLayer >= 0) bulletLayerMask &= ~(1 << triggerLayer);

        Vector3 p = transform.position;
        transform.position = new Vector3(p.x, p.y, zPos);

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) sr = gameObject.AddComponent<SpriteRenderer>();
        sr.sprite = LightSpriteGenerator.WhitePixel;
        sr.color = color;
        sr.sortingOrder = sortingOrder;

        transform.localScale = new Vector3(size.x, size.y, 1f);

        float angle = Mathf.Atan2(direction.y, direction.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.Euler(0f, 0f, angle);

        Debug.Log("[Bullet] Init. Floor layer=" + floorLayer + " Trigger layer=" + triggerLayer + " mask=" + bulletLayerMask);
    }

    void Update()
    {
        float step = speed * Time.deltaTime;

        RaycastHit2D hit = Physics2D.Raycast(transform.position, direction, step, bulletLayerMask);

        if (hit.collider != null)
        {
            transform.position = new Vector3(hit.point.x, hit.point.y, zPos);
            OnHit(hit);
            return;
        }
        else
        {
            Vector2 next = (Vector2)transform.position + direction * step;
            transform.position = new Vector3(next.x, next.y, zPos);
        }

        traveled += step;
        if (traveled >= range) Destroy(gameObject);
    }

    void OnHit(RaycastHit2D hit)
    {
        Debug.Log("[Bullet] Hit " + hit.collider.name);

        // 1. Крыса
        var rat = hit.collider.GetComponentInParent<Rat>();
        if (rat != null) { rat.TakeHit(hit.point); Destroy(gameObject); return; }

        // 2. Стена
        var wall = hit.collider.GetComponentInParent<Wall>();
        if (wall != null) { wall.BreakAtPoint(hit.point, 1f); Destroy(gameObject); return; }

        // 3. Враг
        var enemy = hit.collider.GetComponentInParent<Enemy>();
        if (enemy != null) { enemy.TakeHit(hit.point); Destroy(gameObject); return; }

        // 4. ✅ КИТ (добавлено)
        var whale = hit.collider.GetComponentInParent<WhaleDamage>();
        if (whale != null) { whale.TakeHit(hit.point); Destroy(gameObject); return; }

        // 5. Если ничего не найдено
        Destroy(gameObject);
    }
}