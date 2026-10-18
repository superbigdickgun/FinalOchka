using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    [Header("=== ЦЕЛЬ ===")]
    public Transform target;

    [Header("=== СМЕЩЕНИЕ ===")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Header("=== СГЛАЖИВАНИЕ ===")]
    [Range(0.1f, 20f)] public float smooth = 5f;

    [Header("=== ГРАНИЦЫ (опционально) ===")]
    public bool useBounds = false;
    public Vector2 minBounds = new Vector2(-50f, -50f);
    public Vector2 maxBounds = new Vector2(50f, 50f);

    void LateUpdate()
    {
        if (target == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) target = p.transform;
            else return;
        }

        Vector3 desired = target.position + offset;
        desired.z = -10f;

        // Границы
        if (useBounds)
        {
            desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
            desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);
        }

        transform.position = Vector3.Lerp(transform.position, desired, smooth * Time.deltaTime);
    }
}