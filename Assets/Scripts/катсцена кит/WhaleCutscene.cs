using UnityEngine;
using System.Collections;
using UnityEngine.Rendering.Universal;

public class WhaleCutscene : MonoBehaviour
{
    [Header("=== КАМЕРА И ИГРОК ===")]
    public CameraFollow cameraFollow;
    public Camera mainCamera;
    public PlayerModular player;

    [Header("=== НАСТРОЙКИ КАМЕРЫ ===")]
    public Transform cameraWhaleView;
    public float whaleCameraSize = 8f;
    public float cameraFlightSpeed = 1.5f;
    public float holdTime = 3f;

    [Header("=== ССЫЛКИ ===")]
    public GameObject[] crows;
    public BloodFXController bloodFX;
    public Transform bloodSpawnPoint;

    [Header("=== НАСТРОЙКИ БРЫЗГ ===")]
    public int splatterCount = 3;

    [Header("=== ЗОНА СРАБАТЫВАНИЯ ===")]
    [Tooltip("Перетащите сюда BoxCollider2D (Is Trigger ✅). При входе в него сработает кат-сцена.")]
    public Collider2D triggerZone;

    public bool IsPlaying { get; private set; } = false;

    private bool hasPlayed = false;
    private Transform playerTransform;

    void Start()
    {
        if (player == null) player = FindObjectOfType<PlayerModular>();
        if (player != null) playerTransform = player.transform;

        // Автопоиск коллайдера на этом объекте
        if (triggerZone == null) triggerZone = GetComponent<Collider2D>();

        // Убеждаемся, что коллайдер — триггер
        if (triggerZone != null) triggerZone.isTrigger = true;
    }

    void Update()
    {
        if (hasPlayed || IsPlaying || playerTransform == null || triggerZone == null) return;

        // ✅ Проверяем, находится ли игрок ВНУТРИ коллайдера
        if (triggerZone.OverlapPoint(playerTransform.position))
        {
            StartCoroutine(PlayCutscene());
        }
    }

    IEnumerator PlayCutscene()
    {
        IsPlaying = true;

        // ✅ Принудительно включаем воронов
        if (crows != null)
        {
            foreach (var crow in crows)
            {
                if (crow != null)
                {
                    crow.SetActive(true);
                    var crowScript = crow.GetComponent<Crow>();
                    if (crowScript != null) crowScript.SetActive(true);
                }
            }
        }

        Vector3 savedCamPos = mainCamera.transform.position;
        float savedCamSize = mainCamera.orthographicSize;

        if (cameraFollow != null) cameraFollow.enabled = false;
        if (player != null) player.enabled = false;

        // ✅ КАМЕРА ЛЕТИТ К КИТУ
        float t = 0f;
        Vector3 startPos = mainCamera.transform.position;
        Vector3 endPos = new Vector3(cameraWhaleView.position.x, cameraWhaleView.position.y, startPos.z);

        while (t < 1f)
        {
            t += Time.deltaTime / cameraFlightSpeed;
            mainCamera.transform.position = Vector3.Lerp(startPos, endPos, t);
            mainCamera.orthographicSize = Mathf.Lerp(savedCamSize, whaleCameraSize, t);
            yield return null;
        }

        // ✅ ВЫЗЫВАЕМ БРЫЗГИ (пока камера смотрит)
        if (bloodFX != null && bloodSpawnPoint != null)
        {
            for (int i = 0; i < splatterCount; i++)
            {
                bloodFX.SpawnSplatter(bloodSpawnPoint.position);
                yield return new WaitForSeconds(0.3f);
            }
        }

        yield return new WaitForSeconds(holdTime);

        // ✅ КАМЕРА ВОЗВРАЩАЕТСЯ
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / cameraFlightSpeed;
            mainCamera.transform.position = Vector3.Lerp(endPos, savedCamPos, t);
            mainCamera.orthographicSize = Mathf.Lerp(whaleCameraSize, savedCamSize, t);
            yield return null;
        }

        if (cameraFollow != null) cameraFollow.enabled = true;
        if (player != null) player.enabled = true;

        hasPlayed = true;
        IsPlaying = false;
    }

    void OnDrawGizmos()
    {
        // ✅ Если коллайдера нет — рисуем круг-подсказку
        if (triggerZone == null)
        {
            Gizmos.color = new Color(1f, 0.5f, 0f, 0.3f);
            Gizmos.DrawWireSphere(transform.position, 10f);
        }
    }
}