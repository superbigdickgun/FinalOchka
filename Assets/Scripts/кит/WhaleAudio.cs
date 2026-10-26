using UnityEngine;

public class WhaleAudio : MonoBehaviour
{
    [Header("=== ЗОНА ===")]
    [Tooltip("Радиус, где фон максимальный (у кита).")]
    public float maxVolumeRadius = 4f;

    [Header("=== ФОНОВЫЙ ГУЛ ===")]
    public AudioClip ambientLoop;
    [Range(0f, 1f)] public float ambientMaxVolume = 0.6f;
    [Range(0f, 1f)] public float ambientMinVolume = 0.05f;
    public float ambientFadeSpeed = 1.5f;

    [Header("=== КРИК ВОРОНЫ ===")]
    public AudioClip crowScream;
    [Range(0f, 1f)] public float crowScreamVolume = 1f;

    [Header("=== КРИКИ ВОРОН ===")]
    public AudioClip[] crowPeckSounds;
    [Range(0f, 1f)] public float crowPeckVolume = 0.4f;
    public float crowPeckIntervalMin = 4f;
    public float crowPeckIntervalMax = 10f;

    [Header("=== РАЗЛЁТ ===")]
    public AudioClip crowFleeSound;
    [Range(0f, 1f)] public float crowFleeVolume = 0.8f;

    [Header("=== ССЫЛКИ ===")]
    public Transform player;
    public Transform whaleCenter;
    public WhaleCutscene cutscene;
    public CrowZone crowZone;

    private AudioSource ambientSource;
    private AudioSource oneShotSource;
    private float crowPeckTimer;
    private bool wasScared = false;
    private bool isPlayerInZone = false;

    void Awake()
    {
        ambientSource = gameObject.AddComponent<AudioSource>();
        ambientSource.playOnAwake = false;
        ambientSource.loop = true;
        ambientSource.spatialBlend = 0f;
        ambientSource.volume = 0f;
        if (ambientLoop != null) { ambientSource.clip = ambientLoop; ambientSource.Play(); }

        oneShotSource = gameObject.AddComponent<AudioSource>();
        oneShotSource.playOnAwake = false;
        oneShotSource.spatialBlend = 0f;

        if (player == null)
        {
            var p = GameObject.FindGameObjectWithTag("Player");
            if (p != null) player = p.transform;
        }
        if (cutscene == null) cutscene = FindObjectOfType<WhaleCutscene>();
        if (crowZone == null) crowZone = FindObjectOfType<CrowZone>();
        if (whaleCenter == null) whaleCenter = transform;

        crowPeckTimer = Random.Range(crowPeckIntervalMin, crowPeckIntervalMax);
    }

    // ✅ ВХОД В ЗОНУ (момент пересечения границы)
    void OnTriggerEnter2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInZone = true;
        Debug.Log("[WhaleAudio] Игрок вошёл в зону кита");
    }

    // ✅ ВЫХОД ИЗ ЗОНЫ
    void OnTriggerExit2D(Collider2D other)
    {
        if (!other.CompareTag("Player")) return;
        isPlayerInZone = false;
        Debug.Log("[WhaleAudio] Игрок вышел из зоны кита");
    }

    void Update()
    {
        if (player == null || whaleCenter == null) return;

        float distToCenter = Vector2.Distance(whaleCenter.position, player.position);

        // ✅ ФОНОВЫЙ ГУЛ (только если в зоне)
        float targetVolume = 0f;
        if (isPlayerInZone)
        {
            float t = Mathf.InverseLerp(15f, maxVolumeRadius, distToCenter);
            targetVolume = Mathf.Lerp(ambientMinVolume, ambientMaxVolume, t);
        }

        bool cutscenePlaying = cutscene != null && cutscene.IsPlaying;
        if (cutscenePlaying) targetVolume = Mathf.Max(targetVolume, ambientMaxVolume * 0.5f);

        ambientSource.volume = Mathf.Lerp(ambientSource.volume, targetVolume, Time.deltaTime * ambientFadeSpeed);

        // ✅ КРИКИ ВОРОН (пока клюют)
        bool crowsActive = crowZone != null && isPlayerInZone;
        if (crowsActive && !cutscenePlaying)
        {
            crowPeckTimer -= Time.deltaTime;
            if (crowPeckTimer <= 0f && crowPeckSounds.Length > 0)
            {
                crowPeckTimer = Random.Range(crowPeckIntervalMin, crowPeckIntervalMax);
                AudioClip clip = crowPeckSounds[Random.Range(0, crowPeckSounds.Length)];
                if (clip != null) oneShotSource.PlayOneShot(clip, crowPeckVolume);
            }
        }

        // ✅ РАЗЛЁТ
        if (crowZone != null)
        {
            bool isScared = distToCenter < crowZone.scareRadius;
            if (isScared && !wasScared)
            {
                if (crowFleeSound != null)
                    oneShotSource.PlayOneShot(crowFleeSound, crowFleeVolume);
            }
            wasScared = isScared;
        }
    }

    public void PlayCrowScream()
    {
        if (crowScream != null && oneShotSource != null)
            oneShotSource.PlayOneShot(crowScream, crowScreamVolume);
    }
}