using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class WhaleInsideTrigger : MonoBehaviour
{
    [Header("=== ССЫЛКИ ===")]
    public WhaleBodyCutout bodyCutout;
    
    [Tooltip("Спрайт внутренностей (должен быть включен в сцене!).")]
    public GameObject insideBody;

    [Header("=== СЛОИ ПИСТОЛЕТА ===")]
    public int pistolHiddenOrder = 5;
    public int laserHiddenOrder = 5;
    public int pistolNormalOrder = 14;
    public int laserNormalOrder = 940;

    private Transform currentPlayer;

    void Start()
    {
        var col = GetComponent<BoxCollider2D>();
        if (col != null) col.isTrigger = true;
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        Debug.Log("[WhaleInsideTrigger] Игрок вошёл!");
        currentPlayer = other.transform;

        // Включаем режим выреза
        if (bodyCutout != null) bodyCutout.StartCutout();
        
        // Сразу делаем первый вырез
        if (bodyCutout != null) bodyCutout.UpdateCutoutPosition(other.transform.position);

        SetPistolOrder(other.gameObject, pistolHiddenOrder, laserHiddenOrder);
    }

    // ✅ ВЫЗЫВАЕТСЯ КАЖДЫЙ КАДР, ПОКА ИГРОК ВНУТРИ
    void OnTriggerStay2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        // Обновляем позицию дырки
        if (bodyCutout != null && currentPlayer != null) 
            bodyCutout.UpdateCutoutPosition(currentPlayer.position);
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        
        Debug.Log("[WhaleInsideTrigger] Игрок вышел!");
        currentPlayer = null;

        // Выключаем режим выреза и возвращаем текстуру
        if (bodyCutout != null) bodyCutout.StopCutout();

        SetPistolOrder(other.gameObject, pistolNormalOrder, laserNormalOrder);
    }

    void SetPistolOrder(GameObject player, int pistolOrder, int laserOrder)
    {
        if (player == null) return;
        var pistol = player.GetComponentInChildren<Pistol>();
        if (pistol == null) return;

        var sr = pistol.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = pistolOrder;

        var laser = pistol.GetComponentInChildren<LineRenderer>();
        if (laser != null) laser.sortingOrder = laserOrder;
    }
}