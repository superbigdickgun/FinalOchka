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
    public int splatterCount = 3;

    public bool IsPlaying { get; private set; } = false;

    private bool hasPlayed = false;
    private Vector3 savedPlayerPos;
    private Quaternion savedPlayerRot;
    private Rigidbody2D playerRb;

    // ✅ ВХОД В ЗОНУ (момент пересечения границы)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (hasPlayed || IsPlaying) return;
        if (!other.CompareTag("Player")) return;

        Debug.Log("[WhaleCutscene] Игрок пересёк границу зоны!");

        // ✅ СОХРАНЯЕМ ПОЗИЦИЮ ИГРОКА
        savedPlayerPos = other.transform.position;
        savedPlayerRot = other.transform.rotation;

        // ✅ ЖЁСТКО ОСТАНАВЛИВАЕМ ФИЗИКУ
        playerRb = other.GetComponent<Rigidbody2D>();
        if (playerRb != null)
        {
            playerRb.linearVelocity = Vector2.zero;
            playerRb.angularVelocity = 0f;
            playerRb.simulated = false;
        }

        StartCoroutine(PlayCutscene());
    }

    IEnumerator PlayCutscene()
    {
        IsPlaying = true;

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

        // ✅ КАМЕРА ЛЕТИТ
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

        // ✅ КРИК ВОРОНЫ
        var whaleAudio = FindObjectOfType<WhaleAudio>();
        if (whaleAudio != null) whaleAudio.PlayCrowScream();

        // ✅ БРЫЗГИ
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

        // ✅ ВОЗВРАЩАЕМ ИГРОКА НА МЕСТО
        if (player != null)
        {
            player.transform.position = savedPlayerPos;
            player.transform.rotation = savedPlayerRot;
            player.enabled = true;

            if (playerRb != null)
            {
                playerRb.linearVelocity = Vector2.zero;
                playerRb.angularVelocity = 0f;
                playerRb.simulated = true;
            }
        }

        if (cameraFollow != null) cameraFollow.enabled = true;

        hasPlayed = true;
        IsPlaying = false;
    }
}