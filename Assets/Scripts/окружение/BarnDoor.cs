using UnityEngine;

public class BarnDoor : MonoBehaviour
{
    [Header("=== ГЕНЕРАТОР ===")]
    public Generator generator;

    [Header("=== ЗВУКИ ===")]
    public AudioClip lockedSound;
    public AudioClip openSound;
    private AudioSource audioSource;

    [Header("=== ПОДСКАЗКА ===")]
    public WorldPrompt prompt;
    public string promptNoLight = "Без света там чёрт ногу сломит. Запущу-ка генератор.";
    public string promptCanOpen = "Нажмите E — открыть дверь";
    public string promptOpened = "Дверь открыта";

    [Header("=== ВЗАИМОДЕЙСТВИЕ ===")]
    public KeyCode interactKey = KeyCode.E;
    public float interactRadius = 2f;

    [Header("=== АНИМАЦИЯ ДВЕРИ ===")]
    [Tooltip("Объект двери (квадрат), который вращается. Именно его позиция используется для расстояния.")]
    public Transform doorTransform;
    
    [Tooltip("Угол открытия. -110 = в противоположную сторону от 110.")]
    public float openAngle = -110f;
    
    public float openSpeed = 180f;

    [Tooltip("Коллайдер двери (отключается при открытии).")]
    public Collider2D doorCollider;

    private Quaternion closedRotation;
    private Quaternion openRotation;
    private bool isOpen = false;
    private bool playerInRange;
    private Transform player;

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
        
        if (generator == null) generator = FindObjectOfType<Generator>();
        
        if (doorTransform == null) doorTransform = transform;
        
        closedRotation = transform.localRotation; // Запоминаем поворот ПИВОТА
        openRotation = Quaternion.Euler(0f, 0f, openAngle) * closedRotation;

        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;
    }

    void Update()
    {
        if (player == null) return;

        // ✅ Считаем расстояние от ЦЕНТРА ДВЕРИ (дочернего объекта), а не от пивота
        Vector2 checkPos = doorTransform != null ? (Vector2)doorTransform.position : (Vector2)transform.position;
        float dist = Vector2.Distance(checkPos, player.position);
        playerInRange = dist < interactRadius;

        // Обновляем подсказку
        if (prompt != null)
        {
            if (playerInRange)
            {
                if (isOpen) prompt.SetText(promptOpened);
                else if (generator != null && !generator.isOn) prompt.SetText(promptNoLight);
                else prompt.SetText(promptCanOpen);
            }
            else 
            {
                prompt.SetText("");
            }
        }

        // Взаимодействие
        if (playerInRange && Input.GetKeyDown(interactKey) && !isOpen)
        {
            if (generator != null && generator.isOn)
            {
                OpenDoor();
            }
            else
            {
                // ✅ Проверка: играет ли уже звук, чтобы не спамить
                if (lockedSound != null && audioSource != null && !audioSource.isPlaying)
                {
                    audioSource.PlayOneShot(lockedSound);
                }
            }
        }

        // Анимация поворота
        if (isOpen)
        {
            transform.localRotation = Quaternion.RotateTowards(
                transform.localRotation, 
                openRotation, 
                openSpeed * Time.deltaTime
            );
        }
    }

    void OpenDoor()
    {
        isOpen = true;
        
        if (doorCollider != null) doorCollider.enabled = false;
        
        if (openSound != null && audioSource != null)
            audioSource.PlayOneShot(openSound);
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Vector3 checkPos = doorTransform != null ? doorTransform.position : transform.position;
        Gizmos.DrawWireSphere(checkPos, interactRadius);
    }
}