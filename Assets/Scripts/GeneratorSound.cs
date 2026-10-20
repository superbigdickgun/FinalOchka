using UnityEngine;

public class GeneratorSound : MonoBehaviour
{
    [Header("=== ЗВУКИ ===")]
    public AudioClip startClip;
    public AudioClip loopClip;
    public AudioClip stopClip;

    [Header("=== ГРОМКОСТЬ ===")]
    [Range(0f, 1f)] public float startVolume = 0.8f;
    [Range(0f, 1f)] public float loopVolume = 0.6f;
    [Range(0f, 1f)] public float stopVolume = 0.8f;

    [Header("=== ЗАТУХАНИЕ ===")]
    public float minDistance = 2f;
    public float maxDistance = 15f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;

    private AudioSource loopSource;
    private AudioSource startSource;
    private AudioSource stopSource;
    private Transform player;
    private bool isOn = false;

    void Awake()
    {
        // Три отдельных источника — каждый для своего звука
        loopSource = gameObject.AddComponent<AudioSource>();
        startSource = gameObject.AddComponent<AudioSource>();
        stopSource = gameObject.AddComponent<AudioSource>();

        loopSource.playOnAwake = false;
        loopSource.loop = true;
        loopSource.spatialBlend = 0f;

        startSource.playOnAwake = false;
        startSource.loop = false;
        startSource.spatialBlend = 0f;

        stopSource.playOnAwake = false;
        stopSource.loop = false;
        stopSource.spatialBlend = 0f;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;
    }

    public void SetOn(bool on)
    {
        if (isOn == on) return;
        isOn = on;

        float distFactor = GetDistanceFactor();

        if (on)
        {
            // Старт — через отдельный источник (будет затухать в Update)
            if (startClip != null && startSource != null)
            {
                startSource.Stop();
                startSource.clip = startClip;
                startSource.volume = startVolume * distFactor;
                startSource.Play();
            }

            // Луп
            if (loopClip != null && loopSource != null)
            {
                loopSource.clip = loopClip;
                loopSource.volume = loopVolume * distFactor;
                loopSource.Play();
            }

            // Стоп-источник на всякий случай останавливаем
            if (stopSource != null) stopSource.Stop();

            if (debugLogs) Debug.Log("[GeneratorSound] ВКЛ, distFactor=" + distFactor.ToString("F2"));
        }
        else
        {
            // Останавливаем луп и старт
            if (loopSource != null) loopSource.Stop();
            if (startSource != null) startSource.Stop();

            // Выключение — через отдельный источник (затухает в Update)
            if (stopClip != null && stopSource != null)
            {
                stopSource.Stop();
                stopSource.clip = stopClip;
                stopSource.volume = stopVolume * distFactor;
                stopSource.Play();
            }

            if (debugLogs) Debug.Log("[GeneratorSound] ВЫКЛ, distFactor=" + distFactor.ToString("F2"));
        }
    }

    float GetDistanceFactor()
    {
        if (player == null) return 1f;
        float dist = Vector2.Distance(transform.position, player.position);
        return Mathf.InverseLerp(maxDistance, minDistance, dist);
    }

    void Update()
    {
        float distFactor = GetDistanceFactor();

        // Луп — затухает всегда, пока играет
        if (loopSource != null && loopSource.isPlaying)
            loopSource.volume = loopVolume * distFactor;

        // Старт — затухает, пока играет
        if (startSource != null && startSource.isPlaying)
            startSource.volume = startVolume * distFactor;

        // Стоп — затухает, пока играет
        if (stopSource != null && stopSource.isPlaying)
            stopSource.volume = stopVolume * distFactor;

        // Экономия: если луп играет, но игрок очень далеко — стоп
        if (isOn && loopSource != null && distFactor <= 0f && loopSource.isPlaying)
            loopSource.Stop();
        else if (isOn && loopSource != null && distFactor > 0f && !loopSource.isPlaying && loopClip != null)
            loopSource.Play();
    }
}