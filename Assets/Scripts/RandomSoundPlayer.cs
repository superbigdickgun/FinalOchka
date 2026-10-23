using UnityEngine;

public class RandomSoundPlayer : MonoBehaviour
{
    [Header("=== ЗВУКИ ===")]
    [Tooltip("Сюда перетащите 4 звука писка.")]
    public AudioClip[] sounds;

    [Header("=== ИНТЕРВАЛ ===")]
    [Tooltip("Минимальная пауза между писками (сек).")]
    public float minInterval = 3f;
    
    [Tooltip("Максимальная пауза между писками (сек).")]
    public float maxInterval = 10f;

    [Header("=== ГРОМКОСТЬ ПО РАССТОЯНИЮ ===")]
    [Tooltip("Общая громкость вблизи.")]
    [Range(0f, 1f)] public float maxVolume = 0.6f;

    [Tooltip("Расстояние, на котором звук максимальный.")]
    public float minDistance = 2f;

    [Tooltip("Расстояние, на котором звук полностью пропадает.")]
    public float maxDistance = 15f;

    [Tooltip("Разброс высоты тона.")]
    public float pitchVariation = 0.1f;

    private AudioSource audioSource;
    private Transform player;
    private float timer;
    private int lastIndex = -1;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (audioSource == null) audioSource = gameObject.AddComponent<AudioSource>();
        
        audioSource.playOnAwake = false;
        audioSource.spatialBlend = 0f; // 2D (громкость считаем сами)
        audioSource.loop = false;

        var p = GameObject.FindGameObjectWithTag("Player");
        if (p != null) player = p.transform;

        timer = Random.Range(minInterval, maxInterval);
    }

    void Update()
    {
        if (sounds == null || sounds.Length == 0) return;

        // Обновляем громкость каждый кадр (плавно)
        if (player != null)
        {
            float dist = Vector2.Distance(transform.position, player.position);
            float t = Mathf.InverseLerp(maxDistance, minDistance, dist);
            audioSource.volume = Mathf.Lerp(0f, maxVolume, t);
        }
        else
        {
            audioSource.volume = maxVolume;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            PlayRandomSound();
            timer = Random.Range(minInterval, maxInterval);
        }
    }

    void PlayRandomSound()
    {
        if (sounds.Length == 0) return;
        if (audioSource.volume <= 0.01f) return; // Не играем, если игрок далеко

        // Случайный звук, не повторяя предыдущий
        int index = Random.Range(0, sounds.Length);
        if (sounds.Length > 1 && index == lastIndex)
        {
            index = (index + 1) % sounds.Length;
        }
        lastIndex = index;

        audioSource.pitch = 1f + Random.Range(-pitchVariation, pitchVariation);
        audioSource.PlayOneShot(sounds[index], audioSource.volume);
    }
}