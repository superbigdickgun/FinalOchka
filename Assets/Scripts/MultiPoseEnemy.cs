using UnityEngine;

public class MultiPoseEnemy : MonoBehaviour
{
    public enum PartType
    {
        Body,
        Head,
        ArmLeft,
        ArmRight,
        LegLeft,
        LegRight,
        Other
    }

    [Header("=== ТИП ЧАСТИ (для сортировки) ===")]
    public PartType partType = PartType.Other;

    [Header("=== ПОЗИЦИИ (4 ракурса) ===")]
    public Vector2 posFront = Vector2.zero;
    public Vector2 posBack  = Vector2.zero;
    public Vector2 posLeft  = Vector2.zero;
    public Vector2 posRight = Vector2.zero;

    [Header("=== УГЛЫ ===")]
    public float angleFront = 0f;
    public float angleBack  = 0f;
    public float angleLeft  = 0f;
    public float angleRight = 0f;

    [Header("=== FLIP ===")]
    public bool flipFront = false;
    public bool flipBack  = false;
    public bool flipLeft  = true;
    public bool flipRight = false;

    [Header("=== СПРАЙТЫ ===")]
    public Sprite spriteFront;
    public Sprite spriteBack;
    public Sprite spriteLeft;
    public Sprite spriteRight;

    [Header("=== ЗНАЧЕНИЯ SORTING ORDER ===")]
    public int bodyOrder = 10;
    public int headOrder = 11;
    public int armUnderOrder = 8;
    public int armOverOrder = 14;
    public int legOrder = 7;

    [Header("=== ССЫЛКИ ===")]
    public Enemy enemy;
    public SpriteRenderer sr;

    private Vector3 baseLocalPos;

    void Awake()
    {
        if (sr == null) sr = GetComponent<SpriteRenderer>();
        if (enemy == null) enemy = GetComponentInParent<Enemy>();
        baseLocalPos = transform.localPosition;
    }

    void LateUpdate()
    {
        if (enemy == null) return;

        int dir = enemy.BodyDir;

        Vector2 pos;
        float ang;
        bool flip;
        Sprite spr;

        switch (dir)
        {
            case 0:  pos = posFront; ang = angleFront; flip = flipFront; spr = spriteFront; break;
            case 1:  pos = posBack;  ang = angleBack;  flip = flipBack;  spr = spriteBack;  break;
            case 2:  pos = posLeft;  ang = angleLeft;  flip = flipLeft;  spr = spriteLeft;  break;
            case 3:  pos = posRight; ang = angleRight; flip = flipRight; spr = spriteRight; break;
            default: pos = posFront; ang = angleFront; flip = flipFront; spr = spriteFront; break;
        }

        transform.localPosition = new Vector3(pos.x, pos.y, baseLocalPos.z);
        transform.localRotation = Quaternion.Euler(0f, 0f, ang);

        if (sr != null)
        {
            sr.flipX = flip;
            if (spr != null) sr.sprite = spr;

            // Жёсткая сортировка
            sr.sortingOrder = GetSortingOrder(dir);
        }
    }

    int GetSortingOrder(int dir)
    {
        switch (partType)
        {
            case PartType.Body:    return bodyOrder;
            case PartType.Head:    return headOrder;

            case PartType.ArmLeft:
                if (dir == 2) return armOverOrder;
                else return armUnderOrder;

            case PartType.ArmRight:
                if (dir == 3) return armOverOrder;
                else return armUnderOrder;

            case PartType.LegLeft:
            case PartType.LegRight: return legOrder;

            default: return sr != null ? sr.sortingOrder : 0;
        }
    }
}