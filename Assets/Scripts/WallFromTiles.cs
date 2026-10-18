using UnityEngine;

[ExecuteInEditMode]
public class WallFromTiles : MonoBehaviour
{
    [Header("=== ТАЙЛ ===")]
    public Sprite tileSprite;

    [Tooltip("Размер одного тайла в юнитах. Спрайт растягивается пропорционально.")]
    public Vector2 tileSize = new Vector2(1f, 1f);

    public Color tileColor = Color.white;

    [Header("=== SORTING ===")]
    public int sortingOrder = 1;

    [Header("=== HP ===")]
    public int tileHP = 2;

    [Header("=== ДЫРА ===")]
    public float holeRadiusMin = 0.15f;
    public float holeRadiusMax = 0.3f;

    [Header("=== РАЗЛЁТ ===")]
    public bool spawnDebris = true;
    public int debrisCountMin = 4;
    public int debrisCountMax = 8;
    public float debrisSpeed = 4f;
    public float debrisLifetime = 0.6f;
    public float debrisSize = 0.08f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = false;
    public bool rebuildNow = false;

    void OnEnable()
    {
        if (Application.isPlaying || rebuildNow)
            Rebuild();
    }

    void Update()
    {
        if (rebuildNow)
        {
            rebuildNow = false;
            Rebuild();
        }
    }

    public void Rebuild()
    {
        // Убираем старый контейнер
        var old = transform.Find("WallTilesContainer");
        if (old != null)
        {
            if (Application.isPlaying) Destroy(old.gameObject);
            else DestroyImmediate(old.gameObject);
        }

        // Убираем коллайдеры с самого Wall
        var baseBox = GetComponent<BoxCollider2D>();
        if (baseBox != null) DestroyImmediate(baseBox);

        var baseCircle = GetComponent<CircleCollider2D>();
        if (baseCircle != null) DestroyImmediate(baseCircle);

        if (tileSprite == null)
        {
            Debug.LogError("[WallFromTiles] Не задан Tile Sprite!");
            return;
        }

        if (tileSprite.texture == null || !tileSprite.texture.isReadable)
        {
            Debug.LogError("[WallFromTiles] Read/Write Enabled на спрайте!");
            return;
        }

        // МИРОВОЙ размер области = Scale Wall
        float worldW = Mathf.Abs(transform.lossyScale.x);
        float worldH = Mathf.Abs(transform.lossyScale.y);

        if (worldW <= 0.001f || worldH <= 0.001f)
        {
            Debug.LogError("[WallFromTiles] Scale Wall = 0!");
            return;
        }

        // Сколько тайлов влезает
        int countX = Mathf.Max(1, Mathf.RoundToInt(worldW / tileSize.x));
        int countY = Mathf.Max(1, Mathf.RoundToInt(worldH / tileSize.y));

        // Мировой размер одного тайла
        float tileWorldW = worldW / countX;
        float tileWorldH = worldH / countY;

        // Контейнер — на верхнем уровне (не под Wall), чтобы не наследовать Scale
        var containerGO = new GameObject("WallTilesContainer");
        containerGO.transform.SetParent(null);
        containerGO.transform.position = transform.position;
        containerGO.transform.rotation = transform.rotation;
        containerGO.transform.localScale = Vector3.one;

        Vector3 wallPos = transform.position;

        // Спрайт тайла
        float spriteW = tileSprite.bounds.size.x;
        float spriteH = tileSprite.bounds.size.y;

        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                var go = new GameObject("Tile_" + x + "_" + y);
                go.transform.SetParent(containerGO.transform, false);

                // Мировая позиция тайла
                float wx = wallPos.x - worldW * 0.5f + tileWorldW * (x + 0.5f);
                float wy = wallPos.y - worldH * 0.5f + tileWorldH * (y + 0.5f);

                go.transform.position = new Vector3(wx, wy, wallPos.z);

                // SpriteRenderer
                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = tileSprite;
                sr.color = tileColor;
                sr.sortingOrder = sortingOrder;

                // Масштаб — под мировой размер тайла (БЕЗ компенсации)
                go.transform.localScale = new Vector3(
                    tileWorldW / spriteW,
                    tileWorldH / spriteH,
                    1f);

                // Коллайдер — по спрайту (в локальных единицах)
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(spriteW, spriteH);
                box.offset = Vector2.zero;
                box.isTrigger = false;

                // WallTile
                var tileScript = go.AddComponent<WallTile>();
                tileScript.maxHP = tileHP;
                tileScript.holeRadiusMin = holeRadiusMin;
                tileScript.holeRadiusMax = holeRadiusMax;
                tileScript.spawnDebris = spawnDebris;
                tileScript.debrisCountMin = debrisCountMin;
                tileScript.debrisCountMax = debrisCountMax;
                tileScript.debrisSpeed = debrisSpeed;
                tileScript.debrisLifetime = debrisLifetime;
                tileScript.debrisSize = debrisSize;
                tileScript.debugLogs = debugLogs;
            }
        }

        if (debugLogs)
            Debug.Log("[WallFromTiles] Тайлов: " + (countX * countY) + " (" + countX + "x" + countY + "), мировой размер: " + tileWorldW + "x" + tileWorldH);
    }
}