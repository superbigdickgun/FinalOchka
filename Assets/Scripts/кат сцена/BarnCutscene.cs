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

    [Header("=== ЗВУК ===")]
    public AudioSource audioSource;
    public AudioClip chickenScream;
    public AudioClip ripSound;
    
    [Header("=== КРОВЬ ===")]
    public BloodFXController bloodFX;

    [Header("=== ПОВЕДЕНИЕ ===")]
    public float chickenSpeed = 3f;
    public float ratSpeed = 2f;
    public float rotationOffset = 90f;

    [Header("=== СЛОИ ===")]
    [Tooltip("Слой для крыс и куриц (должен быть ниже тьмы).")]
    public int characterOrder = 90;
    
    [Tooltip("Слой для тьмы (DarknessOverlay). Обычно 100.")]
    public int darknessOrder = 100;

    private bool isPlaying = false;
    private bool hasPlayed = false;

    private Vector3 savedCamPos;
    private float savedCamSize;

    private SpriteRenderer pistolSprite;
    private LineRenderer pistolLaser;

    private Dictionary<SpriteRenderer, int> savedSortingOrders = new Dictionary<SpriteRenderer, int>();

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
            if (rb != null) 
            {
                rb.linearVelocity = Vector2.zero; 
                rb.angularVelocity = 0f;
                rb.simulated = false; 
            }
        }
        
        if (ratKing != null) ratKing.spawnEnabled = false;
        if (kingTransform != null) kingTransform.gameObject.SetActive(false);

        // ✅ НАСТРАИВАЕМ СЛОИ (КРЫСЫ ПОД ТЬМОЙ, НО НАД КРОВЬЮ)
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
        
        isPlaying = false;
    }

    // ==========================================
    // НАСТРОЙКА СЛОЁВ
    // ==========================================
    void SetupSortingOrders()
    {
        savedSortingOrders.Clear();

        // 1. Сохраняем старые слои у участников
        foreach (var obj in GetAllParticipants())
        {
            if (obj == null) continue;
            var srs = obj.GetComponentsInChildren<SpriteRenderer>();
            foreach (var sr in srs)
            {
                savedSortingOrders[sr] = sr.sortingOrder;
            }
        }

        // 2. Ставим крысам и курицам слой 90 (ПОД тьмой 100)
        if (chickenAlive != null) SetSorting(chickenAlive, characterOrder);
        if (chickenEaten != null) SetSorting(chickenEaten, characterOrder);
        
        if (rat1 != null) SetSorting(rat1, characterOrder);
        if (rat2 != null) SetSorting(rat2, characterOrder);
        foreach (var rat in extraRats) if (rat != null) SetSorting(rat, characterOrder);

        // 3. Принудительно ищем ВСЮ кровь в сцене и опускаем её вниз
        var allSr = FindObjectsOfType<SpriteRenderer>();
        foreach (var sr in allSr)
        {
            if (sr == null) continue;
            
            if (sr.name.Contains("Blood") || sr.GetComponent<BloodDrop>() != null)
            {
                if (!savedSortingOrders.ContainsKey(sr))
                    savedSortingOrders[sr] = sr.sortingOrder;
                
                sr.sortingOrder = 1; // Кровь ниже крыс (90)
            }
        }
    }

    void RestoreSortingOrders()
    {
        foreach (var kvp in savedSortingOrders)
        {
            if (kvp.Key != null) kvp.Key.sortingOrder = kvp.Value;
        }
        savedSortingOrders.Clear();
    }

    void SetSorting(GameObject obj, int order)
    {
        var srs = obj.GetComponentsInChildren<SpriteRenderer>();
        foreach (var sr in srs) sr.sortingOrder = order;
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
        sr.sortingOrder = characterOrder + 1; // 91 (чуть выше крыс)
        var piece = go.AddComponent<CutscenePiece>();
        piece.bloodTrailSprite = legSprite;
        return go;
    }
}