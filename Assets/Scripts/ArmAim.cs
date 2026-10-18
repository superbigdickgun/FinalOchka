using UnityEngine;

[DefaultExecutionOrder(200)]
public class ArmAim : MonoBehaviour
{
    [Header("=== ССЫЛКИ ===")]
    public PlayerModular player;

    [Header("=== ПОВОРОТ ===")]
    [Tooltip("Дополнительный угол. Если рука смотрит не туда — 90 / -90 / 180.")]
    public float angleOffset = 0f;

    [Header("=== ОГРАНИЧЕНИЕ ===")]
    public bool limitAngle = false;
    public float minAngle = -180f;
    public float maxAngle = 180f;

    [Header("=== РАБОТА ===")]
    [Tooltip("Работает только когда пистолет в руках.")]
    public bool onlyWhenHasPistol = true;

    private PistolInventory pistolInv;

    void Awake()
    {
        if (player == null) player = GetComponentInParent<PlayerModular>();
        pistolInv = GetComponentInParent<PistolInventory>();
    }

    void LateUpdate()
    {
        if (player == null) return;

        if (onlyWhenHasPistol)
        {
            bool has = (pistolInv != null && pistolInv.hasPistol);
            if (!has) return;
        }

        Vector2 aim = player.AimDirection;

        float angle = Mathf.Atan2(aim.y, aim.x) * Mathf.Rad2Deg;
        angle += angleOffset;

        if (limitAngle)
            angle = Mathf.Clamp(angle, minAngle, maxAngle);

        transform.localRotation = Quaternion.Euler(0f, 0f, angle);
    }
}