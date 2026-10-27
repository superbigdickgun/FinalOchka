using UnityEngine;

public class BarnRoof : MonoBehaviour
{
    [Header("=== ССЫЛКИ ===")]
    [Tooltip("Спрайт крыши (ваш склеенный спрайт).")]
    public SpriteRenderer roofSprite;

    [Tooltip("Скрипт CameraFollow (на главной камере).")]
    public CameraFollow cameraFollow;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    private Transform player;
    private bool isHidden = false;

    void Start()
    {
        // Ищем SpriteRenderer, если не задан
        if (roofSprite == null) roofSprite = GetComponent<SpriteRenderer>();
        if (roofSprite == null) roofSprite = GetComponentInChildren<SpriteRenderer>();

        if (roofSprite == null)
        {
            Debug.LogError("[BarnRoof] Не найден SpriteRenderer!");
            enabled = false;
            return;
        }

        // Ищем игрока
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        // Ищем CameraFollow, если не задан
        if (cameraFollow == null) cameraFollow = FindObjectOfType<CameraFollow>();
    }

    void Update()
    {
        if (player == null || roofSprite == null) return;

        // 1. Проверяем, находится ли игрок внутри границ прямоугольного спрайта
        Bounds bounds = roofSprite.bounds;
        Vector3 playerPos = player.position;
        
        bool isInsideBounds = (playerPos.x >= bounds.min.x && playerPos.x <= bounds.max.x &&
                               playerPos.y >= bounds.min.y && playerPos.y <= bounds.max.y);

        // 2. Проверяем, играет ли кат-сцена (камера выключена)
        bool isCutscenePlaying = false;
        if (cameraFollow != null)
        {
            isCutscenePlaying = !cameraFollow.enabled;
        }

        // 3. Крыша скрыта, если мы внутри спрайта ИЛИ играет кат-сцена
        bool shouldBeHidden = isInsideBounds || isCutscenePlaying;

        // 4. Применяем (включаем/выключаем только видимость спрайта, чтобы объект не пропадал)
        if (shouldBeHidden != isHidden)
        {
            isHidden = shouldBeHidden;
            
            if (roofSprite != null)
            {
                roofSprite.enabled = !isHidden;
                
                if (debugLogs) 
                    Debug.Log("[BarnRoof] Крыша " + (isHidden ? "СКРЫТА" : "ПОКАЗАНА") 
                        + " (Внутри: " + isInsideBounds + ", Сцена: " + isCutscenePlaying + ")");
            }
        }
    }
}