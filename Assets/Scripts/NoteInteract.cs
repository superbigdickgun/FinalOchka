using UnityEngine;
using System.Collections;

public class NoteInteract : MonoBehaviour
{
    [Header("=== ЗАПИСКА ===")]
    public GameObject noteObject;

    [Header("=== ПОДСКАЗКА (НАЖМИТЕ E) ===")]
    public GameObject hintText;

    [Header("=== ТЕКСТ ЗАПИСКИ ===")]
    public GameObject readText;

    [Header("=== НАСТРОЙКИ ===")]
    public float showRadius = 2f;
    public KeyCode interactKey = KeyCode.E;

    [Header("=== ТАЙМЕР ===")]
    [Tooltip("Через сколько секунд текст записки исчезнет сам.")]
    public float readTextDuration = 6f;

    [Header("=== СЛОЙ (ИЗ ИНСПЕКТОРА) ===")]
    [Tooltip("Слой для текста и подсказки. Задается в инспекторе и НЕ перезаписывается.")]
    public int sortingOrder = 101;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private Transform player;
    private bool isNear = false;
    private Coroutine hideCoroutine;

    void Start()
    {
        if (noteObject == null) noteObject = gameObject;

        if (hintText != null) hintText.SetActive(false);
        if (readText != null) readText.SetActive(false);

        // ✅ Принудительно ставим слой из инспектора
        SetSortingOrder(hintText, sortingOrder);
        SetSortingOrder(readText, sortingOrder);

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    void SetSortingOrder(GameObject obj, int order)
    {
        if (obj == null) return;

        // SpriteRenderer
        var sr = obj.GetComponent<SpriteRenderer>();
        if (sr != null) sr.sortingOrder = order;

        // TextMesh (3D)
        var tm = obj.GetComponent<TextMesh>();
        if (tm != null)
        {
            var mr = obj.GetComponent<MeshRenderer>();
            if (mr != null) mr.sortingOrder = order;
        }

        // UI Text (Canvas)
        var canvas = obj.GetComponent<Canvas>();
        if (canvas != null) canvas.sortingOrder = order;
    }

    void Update()
    {
        if (player == null) return;

        float dist = Vector2.Distance(transform.position, player.position);
        bool near = dist < showRadius;

        // ✅ Подсказка «Нажмите E» — ВСЕГДА в зоне активности
        if (near != isNear)
        {
            isNear = near;

            if (hintText != null)
                hintText.SetActive(near);

            if (debugLogs)
                Debug.Log("[NoteInteract] " + (near ? "Подошёл" : "Отошёл") + " (dist=" + dist.ToString("F2") + ")");
        }

        // ✅ Нажатие E — показываем текст записки
        if (isNear && Input.GetKeyDown(interactKey))
        {
            if (readText != null)
            {
                if (readText.activeSelf)
                {
                    // Скрываем текст по повторному нажатию
                    readText.SetActive(false);
                    if (hideCoroutine != null) StopCoroutine(hideCoroutine);
                }
                else
                {
                    // Показываем текст
                    readText.SetActive(true);

                    // ✅ Таймер на исчезновение (только текст записки)
                    if (hideCoroutine != null) StopCoroutine(hideCoroutine);
                    hideCoroutine = StartCoroutine(HideReadTextAfterDelay());

                    if (debugLogs)
                        Debug.Log("[NoteInteract] Записка прочитана. Текст скроется через " + readTextDuration + " сек.");
                }
            }
        }
    }

    IEnumerator HideReadTextAfterDelay()
    {
        yield return new WaitForSeconds(readTextDuration);

        if (readText != null && readText.activeSelf)
        {
            readText.SetActive(false);

            if (debugLogs)
                Debug.Log("[NoteInteract] Текст записки скрыт автоматически.");
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, showRadius);
    }
}