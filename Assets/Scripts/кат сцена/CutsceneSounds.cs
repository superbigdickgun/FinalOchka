using UnityEngine;

public class CutsceneSounds : MonoBehaviour
{
    [Header("=== ЧАВКАНЬЕ ===")]
    public AudioClip[] chewSounds;
    [Range(0f, 1f)] public float chewVolume = 0.8f;

    [Header("=== ФОНТАН КРОВИ ===")]
    public AudioClip bloodFountainClip;
    [Range(0f, 1f)] public float fountainVolume = 0.5f;
    public float fountainFadeSpeed = 2f;

    [Header("=== КАМЕРА ===")]
    public Transform cameraTransform;
    public Transform cutsceneCenter;
    public float fullVolumeDistance = 2f;
    public float silenceDistance = 10f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;

    private AudioSource chewSource;
    private AudioSource fountainSource;
    private int lastChewIndex = -1;
    private bool fountainActive = false;
    private float targetFountainVolume = 0f;

    void Awake()
    {
        // ✅ ПРИНУДИТЕЛЬНО СОЗДАЁМ AUDIO SOURCE И НАСТРАИВАЕМ
        chewSource = gameObject.AddComponent<AudioSource>();
        chewSource.playOnAwake = false;
        chewSource.loop = false;
        chewSource.spatialBlend = 0f;
        chewSource.volume = chewVolume;
        chewSource.mute = false;
        chewSource.priority = 0; // Самый высокий приоритет

        fountainSource = gameObject.AddComponent<AudioSource>();
        fountainSource.playOnAwake = false;
        fountainSource.loop = true;
        fountainSource.spatialBlend = 0f;
        fountainSource.volume = 0f;
        fountainSource.mute = false;
        fountainSource.priority = 0;

        if (bloodFountainClip != null)
        {
            fountainSource.clip = bloodFountainClip;
            fountainSource.Play();
        }

        // ✅ РАДИКАЛЬНО: УДАЛЯЕМ ВСЕ ЛИШНИЕ AUDIOLISTENER
        var listeners = FindObjectsOfType<AudioListener>();
        if (listeners.Length > 1)
        {
            if (debugLogs) Debug.LogWarning("[CutsceneSounds] Найдено " + listeners.Length + " AudioListener. Удаляю лишние.");
            
            // Оставляем только на Main Camera, остальные удаляем
            for (int i = 0; i < listeners.Length; i++)
            {
                if (listeners[i].gameObject != Camera.main?.gameObject)
                {
                    Destroy(listeners[i]);
                }
            }
        }

        if (debugLogs)
        {
            Debug.Log("[CutsceneSounds] Awake: chew=" + (chewSounds != null ? chewSounds.Length : 0) 
                + " fountain=" + (bloodFountainClip != null) 
                + " listener=" + (FindObjectOfType<AudioListener>() != null));
        }
    }

    void Update()
    {
        if (fountainSource == null) return;

        if (fountainActive && cameraTransform != null && cutsceneCenter != null)
        {
            float dist = Vector2.Distance(cameraTransform.position, cutsceneCenter.position);
            float t = Mathf.InverseLerp(silenceDistance, fullVolumeDistance, dist);
            targetFountainVolume = t * fountainVolume;
        }
        else
        {
            targetFountainVolume = 0f;
        }

        fountainSource.volume = Mathf.Lerp(fountainSource.volume, targetFountainVolume, Time.deltaTime * fountainFadeSpeed);
    }

    public void PlayChew()
    {
        if (chewSounds == null || chewSounds.Length == 0 || chewSource == null) return;

        int index = Random.Range(0, chewSounds.Length);
        if (chewSounds.Length > 1 && index == lastChewIndex)
        {
            index = (index + 1) % chewSounds.Length;
        }
        lastChewIndex = index;

        AudioClip clip = chewSounds[index];
        if (clip == null) return;

        chewSource.pitch = 1f + Random.Range(-0.1f, 0.1f);
        chewSource.PlayOneShot(clip, chewVolume);

        if (debugLogs)
            Debug.Log("[CutsceneSounds] Чавканье: " + clip.name);
    }

    public void StartFountain()
    {
        fountainActive = true;
        if (fountainSource != null && !fountainSource.isPlaying && bloodFountainClip != null)
        {
            fountainSource.Play();
        }
        if (debugLogs) Debug.Log("[CutsceneSounds] Фонтан ВКЛ");
    }

    public void StopFountain()
    {
        fountainActive = false;
        if (debugLogs) Debug.Log("[CutsceneSounds] Фонтан ВЫКЛ");
    }
}