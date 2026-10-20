using UnityEngine;

public class RyeTile : MonoBehaviour
{
    [Header("=== РАЗДВИГАНИЕ ===")]
    public float pushRadius = 1f;
    public float pushStrength = 0.35f;
    public float maxTilt = 35f;
    public float returnSpeed = 6f;

    [Header("=== ПЕРЕКРЫТИЕ ===")]
    [Tooltip("Насколько сильно трава под ногами перекрывает игрока (0 = не перекрывает, 1 = полностью поверх).")]
    [Range(0f, 1f)] public float overlapAmount = 0.6f;

    [Tooltip("Смещение порога по Y. Если трава перекрывает слишком высоко — уменьшите (например, -0.1). Если не перекрывает ноги — увеличьте (0.1).")]
    public float footThresholdOffset = 0f;

    public int playerSortingOrder = 10;

    [Header("=== SORTING ===")]
    public int sortingOrder = 0;

    private Vector3 basePosition;
    private Quaternion baseRotation;
    private Transform player;
    private SpriteRenderer sr;
    private float playerFootY;

    private Vector3 lastPlayerPos;
    private Vector2 moveDir = Vector2.right;
    private const float MOVE_THRESHOLD = 0.0005f;

    void Start()
    {
        basePosition = transform.localPosition;
        baseRotation = transform.localRotation;
        sr = GetComponent<SpriteRenderer>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null)
        {
            player = p.transform;
            lastPlayerPos = player.position;
        }
    }

    void LateUpdate()
    {
        if (player == null)
        {
            transform.localPosition = Vector3.Lerp(transform.localPosition, basePosition, returnSpeed * Time.deltaTime);
            transform.localRotation = Quaternion.Slerp(transform.localRotation, baseRotation, returnSpeed * Time.deltaTime);
            if (sr != null) sr.sortingOrder = sortingOrder;
            return;
        }

        Vector2 delta = (Vector2)(player.position - lastPlayerPos);
        lastPlayerPos = player.position;

        if (delta.sqrMagnitude > MOVE_THRESHOLD)
            moveDir = delta.normalized;

        Vector2 toPlayer = (Vector2)player.position - (Vector2)transform.position;
        float dist = toPlayer.magnitude;

        Vector3 targetPos = basePosition;
        Quaternion targetRot = baseRotation;

        if (dist < pushRadius && dist > 0.0001f)
        {
            float t = 1f - (dist / pushRadius);
            Vector2 tileDir = ((Vector2)transform.position - (Vector2)player.position).normalized;
            float dot = Vector2.Dot(tileDir, moveDir);
            float forwardFactor = Mathf.Lerp(0.3f, 1f, Mathf.Clamp01(dot));

            Vector3 offset = new Vector3(moveDir.x, moveDir.y, 0f) * pushStrength * t * forwardFactor;
            targetPos = basePosition + offset;

            float tilt = maxTilt * t * forwardFactor * (moveDir.x >= 0f ? -1f : 1f);
            targetRot = baseRotation * Quaternion.Euler(0f, 0f, tilt);
        }

        transform.localPosition = Vector3.Lerp(transform.localPosition, targetPos, returnSpeed * Time.deltaTime);
        transform.localRotation = Quaternion.Slerp(transform.localRotation, targetRot, returnSpeed * Time.deltaTime);

        if (sr != null)
        {
            // Основание тайла (pivot Bottom → это его позиция)
            float tileBaseY = transform.position.y;

            // Ноги игрока (позиция игрока + смещение порога)
            playerFootY = player.position.y + footThresholdOffset;

            // Если основание тайла НИЖЕ ног игрока — тайл поверх игрока
            if (tileBaseY < playerFootY)
            {
                // Чем ближе к ногам по Y, тем сильнее перекрытие
                float yDiff = playerFootY - tileBaseY;
                float overlapT = Mathf.Clamp01(1f - yDiff / 1f); // в пределах 1 юнита

                // overlapAmount контролирует, насколько высоко рисуем
                int order = Mathf.RoundToInt(
                    Mathf.Lerp(playerSortingOrder, playerSortingOrder + 10, overlapT * overlapAmount));

                // Но если тайл далеко по X от игрока — он не должен лезть поверх
                if (Mathf.Abs(transform.position.x - player.position.x) > 1f)
                    order = sortingOrder;

                sr.sortingOrder = order;
            }
            else
            {
                sr.sortingOrder = sortingOrder;
            }
        }
    }
}