using UnityEngine;

public class BarnZone : MonoBehaviour
{
    [Tooltip("Ссылка на BarnFloor. Если пусто — найдётся автоматически.")]
    public BarnFloor barnFloor;

    public static BarnZone Instance;

    public bool PlayerInside { get; private set; } = false;

    private Transform player;

    void Awake()
    {
        Instance = this;

        if (barnFloor == null)
            barnFloor = FindObjectOfType<BarnFloor>();

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (barnFloor == null)
            Debug.LogWarning("[BarnZone] BarnFloor не найден на сцене!");
    }

    void Update()
    {
        if (player == null || barnFloor == null || barnFloor.areaCollider == null)
        {
            PlayerInside = false;
            return;
        }

        // Игрок внутри зоны пола амбара?
        PlayerInside = barnFloor.areaCollider.OverlapPoint(player.position);
    }

    void OnDrawGizmosSelected()
    {
        if (barnFloor != null && barnFloor.areaCollider != null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawCube(barnFloor.areaCollider.bounds.center, barnFloor.areaCollider.bounds.size);
        }
    }
}