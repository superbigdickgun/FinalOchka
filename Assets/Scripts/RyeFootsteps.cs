using UnityEngine;

public class RyeFootsteps : MonoBehaviour
{
    [Header("=== ЗВУК ТРАВЫ ===")]
    [Tooltip("Длинный клип (например, 23 сек). Скрипт нарезает его на сегменты логически.")]
    public AudioClip sourceClip;

    [Range(2, 32)] public int segmentCount = 8;
    public float stepInterval = 0.4f;
    [Range(0f, 1f)] public float volume = 0.6f;
    public float pitchVariation = 0.1f;
    public bool avoidRepeat = true;

    [Header("=== ОСТАНОВКА НА ТРАВЕ ===")]
    public bool playStopRustle = true;
    public float stopRustleLength = 0.25f;
    [Range(0f, 1f)] public float stopRustleVolume = 0.4f;

    [Header("=== ССЫЛКИ ===")]
    [Tooltip("Игрок (PlayerModular). Если пусто — найдётся на этом объекте.")]
    public PlayerModular player;

    [Tooltip("Поле ржи. Если пусто — найдётся автоматически.")]
    public RyeField ryeField;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    // Источники звука
    private AudioSource footstepSource;   // шаги игрока (глушим)
    private AudioSource ryeSource;        // трава (свой)
    private AudioSource stopRustleSource; // финальный шорох

    private float timer;
    private int lastSegment = -1;
    private float segmentLength;
    private Vector3 lastPos;

    private bool wasInRye = false;
    private bool wasMoving = false;

    void Awake()
    {
        // === НАХОДИМ ИСТОЧНИКИ ===
        var allSources = GetComponents<AudioSource>();

        // Первый — шаги игрока (его глушим)
        if (allSources.Length >= 1)
            footstepSource = allSources[0];

        // Второй — трава (если нет — создаём)
        if (allSources.Length >= 2)
            ryeSource = allSources[1];
        else
            ryeSource = gameObject.AddComponent<AudioSource>();

        ryeSource.playOnAwake = false;
        ryeSource.loop = false;
        ryeSource.spatialBlend = 0f;

        // Третий — финальный шорох
        stopRustleSource = gameObject.AddComponent<AudioSource>();
        stopRustleSource.playOnAwake = false;
        stopRustleSource.loop = false;
        stopRustleSource.spatialBlend = 0f;

        if (sourceClip == null)
        {
            Debug.LogError("[RyeFootsteps] Не задан Source Clip!");
            enabled = false;
            return;
        }

        segmentLength = sourceClip.length / segmentCount;

        if (player == null) player = GetComponent<PlayerModular>();
        if (ryeField == null) ryeField = FindObjectOfType<RyeField>();

        lastPos = transform.position;

        if (debugLogs)
            Debug.Log("[RyeFootsteps] Клип: " + sourceClip.length + " сек, сегмент: " + segmentLength.ToString("F2")
                + " сек, player=" + (player != null) + ", ryeField=" + (ryeField != null)
                + ", sources=" + allSources.Length);
    }

    void Update()
    {
        if (sourceClip == null || ryeField == null) return;

        float moved = Vector3.Distance(transform.position, lastPos);
        lastPos = transform.position;

        bool isMoving = moved > 0.001f;
        bool inRye = IsInRyeField();

        // === ВХОД В ТРАВУ ===
        if (inRye && !wasInRye)
        {
            SetStepsMuted(true);
            timer = 0f;
            if (debugLogs) Debug.Log("[RyeFootsteps] Вошёл в траву — шаги ЗАГЛУШЕНЫ");
        }

        // === ВЫХОД ИЗ ТРАВЫ ===
        if (!inRye && wasInRye)
        {
            SetStepsMuted(false);
            StopRyeAudio();
            if (debugLogs) Debug.Log("[RyeFootsteps] Вышел из травы — шаги ВКЛЮЧЕНЫ");
        }

        // === НА ТРАВЕ ===
        if (inRye)
        {
            if (isMoving)
            {
                timer -= Time.deltaTime;
                if (timer <= 0f)
                {
                    timer = stepInterval;
                    PlayRandomSegment();
                }
            }
            else if (wasMoving && playStopRustle)
            {
                PlayStopRustle();
            }
        }

        wasInRye = inRye;
        wasMoving = isMoving;
    }

    bool IsInRyeField()
    {
        if (ryeField == null) return false;

        Vector3 p = ryeField.transform.position;
        float halfW = ryeField.fieldWidth * 0.5f;
        float halfH = ryeField.fieldHeight * 0.5f;

        Vector2 pos = transform.position;
        return pos.x >= p.x - halfW && pos.x <= p.x + halfW &&
               pos.y >= p.y - halfH && pos.y <= p.y + halfH;
    }

    // === ЖЁСТКОЕ ГЛУШЕНИЕ ШАГОВ ===
    void SetStepsMuted(bool muted)
    {
        // 1. Флаг в PlayerModular
        if (player != null)
            player.stepsMuted = muted;

        // 2. Принудительно глушим AudioSource шагов
        if (footstepSource != null && footstepSource != ryeSource)
        {
            footstepSource.mute = muted;
            if (debugLogs)
                Debug.Log("[RyeFootsteps] footstepSource.mute = " + muted);
        }

        if (debugLogs)
            Debug.Log("[RyeFootsteps] stepsMuted = " + muted);
    }

    void PlayRandomSegment()
    {
        if (ryeSource == null || sourceClip == null) return;

        int index = Random.Range(0, segmentCount);
        if (avoidRepeat && segmentCount > 1 && index == lastSegment)
            index = (index + 1) % segmentCount;

        lastSegment = index;

        float targetTime = index * segmentLength;
        float maxValidTime = Mathf.Max(0f, sourceClip.length - 0.1f);
        if (targetTime > maxValidTime)
            targetTime = Mathf.Max(0f, maxValidTime - segmentLength);

        ryeSource.Stop();
        ryeSource.clip = sourceClip;
        ryeSource.time = targetTime;
        ryeSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        ryeSource.volume = volume;
        ryeSource.Play();

        CancelInvoke(nameof(StopRyeAudio));
        Invoke(nameof(StopRyeAudio), segmentLength / ryeSource.pitch);

        if (debugLogs)
            Debug.Log("[RyeFootsteps] Сегмент " + index + " @ " + targetTime.ToString("F2") + " сек");
    }

    void PlayStopRustle()
    {
        if (stopRustleSource == null || sourceClip == null) return;

        float start = Random.Range(0f, Mathf.Max(0f, sourceClip.length - stopRustleLength - 0.1f));

        stopRustleSource.Stop();
        stopRustleSource.clip = sourceClip;
        stopRustleSource.time = start;
        stopRustleSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        stopRustleSource.volume = stopRustleVolume;
        stopRustleSource.Play();

        CancelInvoke(nameof(StopStopRustle));
        Invoke(nameof(StopStopRustle), stopRustleLength / stopRustleSource.pitch);

        if (debugLogs)
            Debug.Log("[RyeFootsteps] Финальный шорох @ " + start.ToString("F2") + " сек");
    }

    void StopRyeAudio()
    {
        if (ryeSource != null) ryeSource.Stop();
    }

    void StopStopRustle()
    {
        if (stopRustleSource != null) stopRustleSource.Stop();
    }

    void OnDestroy()
    {
        // Размьючиваем шаги при уничтожении
        if (footstepSource != null) footstepSource.mute = false;
        if (player != null) player.stepsMuted = false;
    }
}