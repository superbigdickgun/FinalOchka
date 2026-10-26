using UnityEngine;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Rendering.Universal;

public class BarnCutscene : MonoBehaviour
{
    [Header("=== КАМЕРА И ИГРОК ===")]
    public CameraFollow cameraFollow;
    public Camera mainCamera;
    public PlayerModular player;

    [Header("=== НАСТРОЙКИ КАМЕРЫ ===")]
    public Transform cameraCorner;
    public float cornerCameraSize = 3f;
    public float cameraFlightSpeed = 1.5f;

    [Header("=== УЧАСТНИКИ ===")]
    public GameObject chickenAlive;
    public GameObject chickenEaten;
    public GameObject rat1;
    public GameObject rat2;
    public GameObject[] extraRats;
    public Sprite legSprite;

    [Header("=== СВЕТ И КОРОЛЬ ===")]
    public Light2D spotLight; 
    public RatKing ratKing;
    public Transform kingTransform;

    [Header("=== ЗВУКИ ===")]
    public AudioSource audioSource;
    public AudioClip chickenScream;
    public AudioClip ripSound;
    
    [Tooltip("4 звука чавканья.")]
    public AudioClip[] chewSounds;
    [Range(0f, 1f)] public float chewVolume = 0.8f;

    [Tooltip("Звук фонтана крови.")]
    public AudioClip bloodFountainClip;
    [Range(0f, 1f)] public float fountainVolume = 0.5f;

    [Tooltip("4 звука писка крыс.")]
    public AudioClip[] ratSqueakSounds;
    [Range(0f, 1f)] public float ratSqueakVolume = 0.4f;
    public float ratSqueakInterval = 2.5f;

    [Header("=== ЗВУК ПОСЛЕ СЦЕНЫ ===")]
    [Tooltip("Звук, который играет ПОСЛЕ окончания кат-сцены.")]
    public AudioClip afterCutsceneSound;
    [Range(0f, 1f)] public float afterCutsceneVolume = 0.8f;

    [Header("=== КРОВЬ ===")]
    public BloodFXController bloodFX;

    [Header("=== ПОВЕДЕНИЕ ===")]
    public float chickenSpeed = 3f;
    public float ratSpeed = 2f;
    public float rotationOffset = 90f;

    [Header("=== СЛОИ ===")]
    [Tooltip("Слой для крыс и куриц во время сцены (под тьмой).")]
    public int characterOrder = 90;
    [Tooltip("Слой для крови и лап (под игроком).")]
    public int bloodOrder = -1;

    private bool isPlaying = false;
    private bool hasPlayed = false;

    private Vector3 savedCamPos;
    private float savedCamSize;

    private SpriteRenderer pistolSprite;
    private LineRenderer pistolLaser;
    private Dictionary<SpriteRenderer, int> savedSortingOrders = new Dictionary<SpriteRenderer, int>();

    private AudioSource chewSource;
    private AudioSource fountainSource;
    private AudioSource ratSqueakSource;
    
    private int lastChewIndex = -1;
    private int lastRatSqueakIndex = -1;
    private float ratSqueakTimer = 0f;

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (isPlaying || hasPlayed || !other.CompareTag("Player")) return;
        PlayScene();
    }

    public void PlayScene()
    {
        if (isPlaying || hasPlayed) return;
        StartCoroutine(SceneRoutine());
    }

    IEnumerator SceneRoutine()
    {
        isPlaying = true;
        hasPlayed = true;

        chewSource = gameObject.AddComponent<AudioSource>();
        chewSource.playOnAwake = false;
        chewSource.spatialBlend = 0f;
        chewSource.volume = chewVolume;

        fountainSource = gameObject.AddComponent<AudioSource>();
        fountainSource.playOnAwake = false;
        fountainSource.loop = true;
        fountainSource.spatialBlend = 0f;
        fountainSource.volume = 0f;
        if (bloodFountainClip != null) { fountainSource.clip = bloodFountainClip; fountainSource.Play(); }

        ratSqueakSource = gameObject.AddComponent<AudioSource>();
        ratSqueakSource.playOnAwake = false;
        ratSqueakSource.spatialBlend = 0f;
        ratSqueakSource.volume = ratSqueakVolume;

        ratSqueakTimer = ratSqueakInterval;

        if (mainCamera != null)
        {
            savedCamPos = mainCamera.transform.position;
            savedCamSize = mainCamera.orthographicSize;
        }

        if (cameraFollow != null) cameraFollow.enabled = false;

        if (player != null) 
        {
            player.enabled = false; 
            var pistol = player.GetComponentInChildren<Pistol>();
            if (pistol != null)
            {
                pistolSprite = pistol.GetComponent<SpriteRenderer>();
                if (pistolSprite != null) pistolSprite.enabled = false;
                pistolLaser = pistol.GetComponentInChildren<LineRenderer>();
                if (pistolLaser != null) pistolLaser.enabled = false;
            }
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) { rb.linearVelocity = Vector2.zero; rb.simulated = false; }
        }
        
        if (ratKing != null) ratKing.spawnEnabled = false;
        if (kingTransform != null) kingTransform.gameObject.SetActive(false);

        SetupSortingOrders();

        chickenEaten.SetActive(true);
        var bloodyChicken = chickenEaten.GetComponent<BloodyChicken>();
        if (bloodyChicken == null) bloodyChicken = chickenEaten.AddComponent<BloodyChicken>();
        bloodyChicken.bloodFX = bloodFX;
        bloodyChicken.StartFountain();

        Vector3 eatenChickenPos = chickenEaten.transform.position;
        foreach (var rat in extraRats) if (rat != null) rat.SetActive(false);

        Vector3 chickenStart = chickenAlive.transform.position;
        Vector3 rat1Start = rat1.transform.position;
        Vector3 rat2Start = rat2.transform.position;

        // ЭТАП 1
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / cameraFlightSpeed;
            
            Vector3 playerPos = new Vector3(player.transform.position.x, player.transform.position.y, mainCamera.transform.position.z);
            Vector3 cornerPos = new Vector3(cameraCorner.position.x, cameraCorner.position.y, mainCamera.transform.position.z);
            mainCamera.transform.position = Vector3.Lerp(playerPos, cornerPos, t);
            mainCamera.orthographicSize = Mathf.Lerp(savedCamSize, cornerCameraSize, t);

            float chickenAngle = t * 10f;
            Vector3 chickenTarget = chickenStart + new Vector3(Mathf.Cos(chickenAngle) * 2f, Mathf.Sin(chickenAngle) * 2f, 0f);
            chickenAlive.transform.position = Vector3.MoveTowards(chickenAlive.transform.position, chickenTarget, chickenSpeed * Time.deltaTime);
            RotateTowards(chickenAlive.transform, chickenTarget);

            Vector3 rat1Target = chickenAlive.transform.position + new Vector3(-1f, 0f, 0f);
            Vector3 rat2Target = chickenAlive.transform.position + new Vector3(1f, 0f, 0f);
            
            rat1.transform.position = Vector3.MoveTowards(rat1.transform.position, rat1Target, ratSpeed * Time.deltaTime);
            rat2.transform.position = Vector3.MoveTowards(rat2.transform.position, rat2Target, ratSpeed * Time.deltaTime);
            
            RotateTowards(rat1.transform, chickenAlive.transform.position);
            RotateTowards(rat2.transform, chickenAlive.transform.position);

            yield return null;
        }

        Vector3 catchPos = chickenAlive.transform.position;
        
        if (spotLight != null) spotLight.enabled = true;
        yield return new WaitForSeconds(0.5f);

        // ЭТАП 2
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime * 4f;
            
            Vector3 r1Target = catchPos + new Vector3(0.4f, 0f, 0f);
            Vector3 r2Target = catchPos + new Vector3(-0.4f, 0f, 0f);
            
            rat1.transform.position = Vector3.Lerp(rat1.transform.position, r1Target, t);
            rat2.transform.position = Vector3.Lerp(rat2.transform.position, r2Target, t);
            
            RotateTowards(rat1.transform, catchPos);
            RotateTowards(rat2.transform, catchPos);

            yield return null;
        }

        // ЭТАП 3
        if (audioSource != null && chickenScream != null) audioSource.PlayOneShot(chickenScream);
        if (bloodFX != null) {
            bloodFX.SpawnPuddle(catchPos);
            bloodFX.SpawnSplatter(catchPos);
        }
        
        PlayChew();

        GameObject leg1 = CreateLeg(rat1.transform.position);
        GameObject leg2 = CreateLeg(rat2.transform.position);
        
        t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / 1.5f;
            
            chickenAlive.transform.position = catchPos + new Vector3(Mathf.Sin(t * 60f) * 0.1f, 0f, 0f);
            
            Vector3 rat1Pull = catchPos + new Vector3(2f, -1f, 0f);
            Vector3 rat2Pull = catchPos + new Vector3(-2f, -1f, 0f);
            rat1.transform.position = Vector3.Lerp(rat1.transform.position, rat1Pull, t);
            rat2.transform.position = Vector3.Lerp(rat2.transform.position, rat2Pull, t);
            
            leg1.transform.position = rat1.transform.position;
            leg2.transform.position = rat2.transform.position;

            if (bloodFX != null && Random.value < 0.5f) {
                bloodFX.SpawnTrail(leg1.transform.position);
                bloodFX.SpawnTrail(leg2.transform.position);
            }
            yield return null;
        }
        
        chickenAlive.transform.position = catchPos; 
        if (audioSource != null && ripSound != null) audioSource.PlayOneShot(ripSound);

        // ЭТАП 4
        Vector3 eat1 = eatenChickenPos + new Vector3(1f, 0.5f, 0f);
        Vector3 eat2 = eatenChickenPos + new Vector3(-1f, 0.5f, 0f);
        
        t = 0f;
        while (t < 1f) {
            t += Time.deltaTime / 1.5f;
            rat1.transform.position = Vector3.Lerp(rat1.transform.position, eat1, t);
            rat2.transform.position = Vector3.Lerp(rat2.transform.position, eat2, t);
            RotateTowards(rat1.transform, eatenChickenPos);
            RotateTowards(rat2.transform, eatenChickenPos);
            yield return null;
        }

        foreach (var rat in extraRats) 
        {
            if (rat != null) 
            {
                rat.SetActive(true);
                Vector3 startR = eatenChickenPos + new Vector3(Random.Range(-4f, 4f), 4f, 0f);
                rat.transform.position = startR;
                Vector3 targetR = eatenChickenPos + new Vector3(Random.Range(-1.5f, 1.5f), Random.Range(-1.5f, 1.5f), 0f);
                StartCoroutine(MoveAndRotate(rat.transform, targetR, eatenChickenPos, 0.8f));
            }
        }
        yield return new WaitForSeconds(1.2f);

        // ЭТАП 5
        GameObject[] allRats = new GameObject[2 + extraRats.Length];
        allRats[0] = rat1; allRats[1] = rat2;
        for (int i = 0; i < extraRats.Length; i++) allRats[i + 2] = extraRats[i];

        int pokes = 0;
        while (pokes < 8)
        {
            GameObject attacker = allRats[Random.Range(0, allRats.Length)];
            if (attacker == null) { pokes++; continue; }

            Vector3 startP = attacker.transform.position;
            Vector3 dir = (eatenChickenPos - startP).normalized;
            Vector3 pokeP = eatenChickenPos - dir * 0.6f;

            float p = 0f;
            while (p < 1f) {
                p += Time.deltaTime * 10f; 
                attacker.transform.position = Vector3.Lerp(startP, pokeP, p);
                RotateTowards(attacker.transform, eatenChickenPos);
                yield return null;
            }

            PlayChew();

            if (bloodFX != null) {
                bloodFX.SpawnSplatter(pokeP);
                bloodFX.SpawnSplatter(eatenChickenPos);
                bloodFX.SpawnTrail(eatenChickenPos);
            }
            if (bloodyChicken != null) bloodyChicken.TriggerSplatter();
            
            p = 0f;
            while (p < 1f) {
                p += Time.deltaTime * 5f;
                attacker.transform.position = Vector3.Lerp(pokeP, startP, p);
                yield return null;
            }

            pokes++;
            yield return new WaitForSeconds(0.3f);
        }

        if (bloodyChicken != null) bloodyChicken.StopFountain();

        // ЭТАП 6
        StartCoroutine(MoveCamera(savedCamPos, savedCamSize, cameraFlightSpeed));
        
        foreach (var rat in allRats) 
        {
            if (rat != null) 
            {
                Vector3 fleeDir = (rat.transform.position - eatenChickenPos).normalized;
                StartCoroutine(MoveAndDestroy(rat.transform, rat.transform.position + fleeDir * 10f, 1.5f));
            }
        }

        yield return new WaitForSeconds(1.5f);

        // ЭТАП 7: ВОЗВРАЩАЕМ ВСЁ
        if (fountainSource != null) fountainSource.Stop();
        if (ratSqueakSource != null) ratSqueakSource.Stop();

        if (spotLight != null) spotLight.enabled = false;
        if (cameraFollow != null) cameraFollow.enabled = true;

        if (player != null) 
        {
            player.enabled = true;
            
            var rb = player.GetComponent<Rigidbody2D>();
            if (rb != null) rb.simulated = true;

            if (pistolSprite != null) pistolSprite.enabled = true;
            if (pistolLaser != null) pistolLaser.enabled = true;
        }

        if (bloodFX != null) bloodFX.FreezeAll();

        RestoreSortingOrders();

        if (ratKing != null) ratKing.spawnEnabled = true;
        if (kingTransform != null) kingTransform.gameObject.SetActive(true);

        // ✅ ЗВУК ПОСЛЕ КАТ-СЦЕНЫ (ОТДЕЛЬНЫЙ ИСТОЧНИК)
        if (afterCutsceneSound != null)
        {
            var tempAudio = gameObject.AddComponent<AudioSource>();
            tempAudio.playOnAwake = false;
            tempAudio.spatialBlend = 0f;
            tempAudio.volume = afterCutsceneVolume;
            tempAudio.clip = afterCutsceneSound;
            tempAudio.Play();
            
            Destroy(tempAudio, afterCutsceneSound.length + 1f);

            Debug.Log("[BarnCutscene] Звук после сцены: " + afterCutsceneSound.name);
        }
        else
        {
            Debug.LogWarning("[BarnCutscene] afterCutsceneSound НЕ НАЗНАЧЕН!");
        }
        
        isPlaying = false;
    }

    void Update()
    {
        if (!isPlaying) return;
        UpdateFountainVolume();
        UpdateRatSqueaks();
    }

    void PlayChew()
    {
        if (chewSounds == null || chewSounds.Length == 0 || chewSource == null) return;
        int index = Random.Range(0, chewSounds.Length);
        if (chewSounds.Length > 1 && index == lastChewIndex) index = (index + 1) % chewSounds.Length;
        lastChewIndex = index;
        if (chewSounds[index] != null) chewSource.PlayOneShot(chewSounds[index], chewVolume);
    }

    void UpdateRatSqueaks()
    {
        if (ratSqueakSource == null || ratSqueakSounds == null || ratSqueakSounds.Length == 0) return;

        ratSqueakTimer -= Time.deltaTime;
        if (ratSqueakTimer <= 0f)
        {
            int index = Random.Range(0, ratSqueakSounds.Length);
            if (ratSqueakSounds.Length > 1 && index == lastRatSqueakIndex) index = (index + 1) % ratSqueakSounds.Length;
            lastRatSqueakIndex = index;

            if (ratSqueakSounds[index] != null)
            {
                ratSqueakSource.pitch = 1f + Random.Range(-0.1f, 0.1f);
                ratSqueakSource.PlayOneShot(ratSqueakSounds[index], ratSqueakVolume);
            }

            ratSqueakTimer = ratSqueakInterval + Random.Range(-0.5f, 0.5f);
        }
    }

    void UpdateFountainVolume()
    {
        if (fountainSource == null || mainCamera == null || cameraCorner == null) return;
        float dist = Vector2.Distance(mainCamera.transform.position, cameraCorner.position);
        float t = Mathf.InverseLerp(10f, 2f, dist);
        fountainSource.volume = Mathf.Lerp(fountainSource.volume, t * fountainVolume, Time.deltaTime * 2f);
    }

    void SetupSortingOrders()
    {
        savedSortingOrders.Clear();

        foreach (var obj in GetAllParticipants())
        {
            if (obj == null) continue;
            var srs = obj.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs) savedSortingOrders[sr] = sr.sortingOrder;
        }

        if (chickenAlive != null) SetSorting(chickenAlive, characterOrder);
        if (chickenEaten != null) SetSorting(chickenEaten, characterOrder);
        if (rat1 != null) SetSorting(rat1, characterOrder);
        if (rat2 != null) SetSorting(rat2, characterOrder);
        foreach (var rat in extraRats) if (rat != null) SetSorting(rat, characterOrder);

        var allSr = FindObjectsOfType<SpriteRenderer>();
        foreach (var sr in allSr)
        {
            if (sr == null) continue;
            if (sr.name.Contains("Blood") || sr.GetComponent<BloodDrop>() != null)
            {
                sr.sortingOrder = bloodOrder;
            }
        }
    }

    void RestoreSortingOrders()
    {
        foreach (var kvp in savedSortingOrders)
        {
            if (kvp.Key != null) 
                kvp.Key.sortingOrder = kvp.Value;
        }
        savedSortingOrders.Clear();

        var allSr = FindObjectsOfType<SpriteRenderer>();
        foreach (var sr in allSr)
        {
            if (sr == null) continue;
            if (sr.name.Contains("Blood") || sr.name.Contains("ChickenLeg") || sr.GetComponent<BloodDrop>() != null)
            {
                sr.sortingOrder = bloodOrder;
            }
        }
    }

    void SetSorting(GameObject obj, int order)
    {
        foreach (var sr in obj.GetComponentsInChildren<SpriteRenderer>())
            sr.sortingOrder = order;
    }

    GameObject[] GetAllParticipants()
    {
        var list = new List<GameObject>();
        if (chickenAlive != null) list.Add(chickenAlive);
        if (chickenEaten != null) list.Add(chickenEaten);
        if (rat1 != null) list.Add(rat1);
        if (rat2 != null) list.Add(rat2);
        foreach (var rat in extraRats) if (rat != null) list.Add(rat);
        return list.ToArray();
    }

    void RotateTowards(Transform obj, Vector3 target)
    {
        Vector3 dir = target - obj.position;
        if (dir.magnitude < 0.01f) return;
        float targetAngle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg + rotationOffset;
        obj.rotation = Quaternion.Euler(0, 0, targetAngle);
    }

    IEnumerator MoveAndRotate(Transform obj, Vector3 targetPos, Vector3 lookAt, float duration)
    {
        Vector3 startPos = obj.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            obj.position = Vector3.Lerp(startPos, targetPos, t);
            RotateTowards(obj, lookAt);
            yield return null;
        }
    }

    IEnumerator MoveAndDestroy(Transform obj, Vector3 targetPos, float duration)
    {
        Vector3 startPos = obj.position;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            obj.position = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }
        Destroy(obj.gameObject);
    }

    IEnumerator MoveCamera(Vector3 targetPos, float targetSize, float duration)
    {
        Vector3 startPos = mainCamera.transform.position;
        float startSize = mainCamera.orthographicSize;
        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / duration;
            mainCamera.transform.position = Vector3.Lerp(startPos, targetPos, t);
            mainCamera.orthographicSize = Mathf.Lerp(startSize, targetSize, t);
            yield return null;
        }
    }

    GameObject CreateLeg(Vector3 pos)
    {
        var go = new GameObject("ChickenLeg");
        go.transform.position = pos;
        var sr = go.AddComponent<SpriteRenderer>();
        sr.sprite = legSprite;
        sr.sortingOrder = bloodOrder;
        var piece = go.AddComponent<CutscenePiece>();
        piece.bloodTrailSprite = legSprite;
        return go;
    }
}