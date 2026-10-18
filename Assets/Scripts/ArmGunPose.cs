using UnityEngine;

[DefaultExecutionOrder(200)]
public class ArmGunPose : MonoBehaviour
{
    [Header("=== ССЫЛКИ ===")]
    public PlayerModular player;

    [Header("=== ПОЗИЦИЯ ПЛЕЧА ПО РАКУРСАМ ===")]
    public Vector2 posFront = new Vector2(0.15f, 0.1f);
    public Vector2 posBack  = new Vector2(0.15f, 0.05f);
    public Vector2 posLeft  = new Vector2(-0.1f, 0.15f);
    public Vector2 posRight = new Vector2(0.15f, 0.15f);

    [Header("=== УГОЛ ПО РАКУРСАМ ===")]
    public float angleOffsetFront = 0f;
    public float angleOffsetBack  = 0f;
    public float angleOffsetLeft  = 0f;
    public float angleOffsetRight = 0f;

    [Header("=== FLIP ===")]
    public bool flipFront = false;
    public bool flipBack  = false;
    public bool flipLeft  = true;
    public bool flipRight = false;

    [Header("=== SORTING ORDER ПО РАКУРСАМ ===")]
    [Tooltip("Смотрим ВНИЗ — рука поверх тела.")]
    public int sortingFront = 100;

    [Tooltip("Смотрим ВВЕРХ — рука ПОД головой (голова перекрывает).")]
    public int sortingBack = 9;

    [Tooltip("Смотрим ВЛЕВО — рука поверх тела.")]
    public int sortingLeft = 100;

    [Tooltip("Смотрим ВПРАВО — рука НИЖЕ ВСЕХ (под ногами).")]
    public int sortingRight = 1;

    [Header("=== ОГРАНИЧЕНИЕ УГЛА ===")]
    public bool limitAngle = false;
    public float minAngle = -180f;
    public float maxAngle = 180f;

    [Header("=== РАБОТА ===")]
    public bool onlyWhenHasPistol = true;

    private PistolInventory pistolInv;
    private SpriteRenderer sr;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerModular>();
        pistolInv = GetComponentInParent<PistolInventory>();
        sr = GetComponent<SpriteRenderer>();
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (onlyWhenHasPistol)
        {
            bool has = (pistolInv != null && pistolInv.hasPistol);
            if (!has) return;
        }

        int dir = player.BodyDir;

        Vector2 pos;
        float angOffset;
        bool flip;
        int order;

        switch (dir)
        {
            case 0:  pos = posFront; angOffset = angleOffsetFront; flip = flipFront; order = sortingFront; break;
            case 1:  pos = posBack;  angOffset = angleOffsetBack;  flip = flipBack;  order = sortingBack;  break;
            case 2:  pos = posLeft;  angOffset = angleOffsetLeft;  flip = flipLeft;  order = sortingLeft;  break;
            case 3:  pos = posRight; angOffset = angleOffsetRight; flip = flipRight; order = sortingRight; break;
            default: pos = posFront; angOffset = angleOffsetFront; flip = flipFront; order = sortingFront; break;
        }

        // Позиция плеча
        transform.localPosition = new Vector3(pos.x, pos.y, transform.localPosition.z);

        // Угол на курсор + смещение
        Vector2 aim = player.AimDirection;
        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg + angOffset;

        if (limitAngle)
            angle = Mathf.Clamp(angle, minAngle, maxAngle);

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);

        // Flip + Sorting Order
        if (sr != null)
        {
            sr.flipX = flip;
            sr.sortingOrder = order;
        }
    }
}