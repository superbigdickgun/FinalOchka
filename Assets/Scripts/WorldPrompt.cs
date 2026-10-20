using UnityEngine;
using UnityEngine.UI;

#if TMP_PRESENT
using TMPro;
#endif

public class WorldPrompt : MonoBehaviour
{
    [Header("=== ОБЪЕКТ ПОДСКАЗКИ ===")]
    [Tooltip("Объект в сцене, который нужно включать/выключать (ваш текст).")]
    public GameObject promptObject;

    [Header("=== КОМПОНЕНТ ТЕКСТА (для смены текста) ===")]
    [Tooltip("TextMesh (3D Text), UI Text или TMP_Text. Можно оставить пусто, если текст менять не нужно.")]
    public Component textComponent;

    [Header("=== ДИСТАНЦИЯ ===")]
    [Tooltip("Радиус, в котором подсказка видна.")]
    public float showRadius = 2f;

    [Header("=== СЛЕДОВАНИЕ ===")]
    public bool followObject = false;
    public Vector3 offset = new Vector3(0f, 0.8f, 0f);

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private Transform player;
    private bool isVisible = false;
    private string currentText = "";

    void Start()
    {
        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        if (promptObject != null)
            promptObject.SetActive(false);
        else
            Debug.LogWarning("[WorldPrompt] Не задан Prompt Object!");

        // Автопоиск компонента текста, если не задан
        if (textComponent == null && promptObject != null)
        {
            textComponent = promptObject.GetComponent<TextMesh>();
            if (textComponent == null) textComponent = promptObject.GetComponent<Text>();
#if TMP_PRESENT
            if (textComponent == null) textComponent = promptObject.GetComponent<TMP_Text>();
#endif
        }
    }

    void Update()
    {
        if (player == null || promptObject == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool shouldShow = dist < showRadius;

        if (shouldShow != isVisible)
        {
            isVisible = shouldShow;
            promptObject.SetActive(shouldShow);

            if (debugLogs)
                Debug.Log("[WorldPrompt] " + (shouldShow ? "Показан" : "Скрыт") + " (dist=" + dist.ToString("F2") + ")");
        }

        if (isVisible && followObject)
            promptObject.transform.position = transform.position + offset;
    }

    /// <summary>
    /// Меняет текст подсказки. Поддерживает TextMesh, UI Text и TMP_Text.
    /// </summary>
    public void SetText(string text)
    {
        if (text == currentText) return;
        currentText = text;

        if (textComponent == null)
        {
            if (debugLogs) Debug.LogWarning("[WorldPrompt] textComponent не задан — текст не меняется");
            return;
        }

        if (textComponent is TextMesh tm)
            tm.text = text;
        else if (textComponent is Text ui)
            ui.text = text;
#if TMP_PRESENT
        else if (textComponent is TMP_Text tmp)
            tmp.text = text;
#endif

        if (debugLogs)
            Debug.Log("[WorldPrompt] Текст: " + text);
    }

    /// <summary>
    /// Принудительно показать/скрыть (например, после взаимодействия).
    /// </summary>
    public void SetVisible(bool visible)
    {
        isVisible = visible;
        if (promptObject != null) promptObject.SetActive(visible);
    }
}