using UnityEngine;
using System.Collections.Generic;

[ExecuteInEditMode]
public class Wall : MonoBehaviour
{
    [Header("=== СПРАЙТ ===")]
    [Tooltip("Спрайт одного тайла. Read/Write Enabled обязателен.")]
    public Sprite tileSprite;

    [Tooltip("Размер одного тайла в юнитах.")]
    public Vector2 tileSize = new Vector2(1f, 1f);

    public Color tileColor = Color.white;
    public int sortingOrder = 1;

    [Header("=== HP ТАЙЛА ===")]
    [Tooltip("Сколько попаданий держит тайл.")]
    public int tileHP = 2;

    [Header("=== ДЫРА ===")]
    public float holeRadiusMin = 0.1f;
    public float holeRadiusMax = 0.2f;

    [Header("=== РАЗЛЁТ ПИКСЕЛЕЙ ===")]
    public bool spawnDebris = true;
    public int debrisCount = 6;
    public float debrisSpeed = 4f;
    public float debrisLifetime = 0.6f;
    public float debrisSize = 0.08f;

    [Header("=== ОТЛАДКА ===")]
    public bool debugLogs = true;
    public bool rebuildNow = false;

    // Внутренние
    private Transform container;
    private List<TileData> tiles = new List<TileData>();
    private float areaW, areaH;

    private class TileData
    {
        public GameObject go;
        public SpriteRenderer sr;
        public BoxCollider2D col;
        public Texture2D tex;
        public int texW, texH;
        public Color[] pixels;
        public int hp;
        public float localX, localY, localW, localH;
    }

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

    // ============================================================
    // СБОРКА СТЕНЫ
    // ============================================================
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
        var b1 = GetComponent<BoxCollider2D>();
        if (b1 != null) DestroyImmediate(b1);
        var b2 = GetComponent<CircleCollider2D>();
        if (b2 != null) DestroyImmediate(b2);

        tiles.Clear();

        if (tileSprite == null)
        {
            Debug.LogError("[Wall] Не задан Tile Sprite!");
            return;
        }

        if (tileSprite.texture == null || !tileSprite.texture.isReadable)
        {
            Debug.LogError("[Wall] Read/Write Enabled на спрайте " + tileSprite.name);
            return;
        }

        // Мировой размер области
        float worldW = Mathf.Abs(transform.lossyScale.x);
        float worldH = Mathf.Abs(transform.lossyScale.y);

        if (worldW <= 0.001f || worldH <= 0.001f)
        {
            Debug.LogError("[Wall] Scale должен быть > 0!");
            return;
        }

        // Сколько тайлов влезает
        int countX = Mathf.Max(1, Mathf.RoundToInt(worldW / tileSize.x));
        int countY = Mathf.Max(1, Mathf.RoundToInt(worldH / tileSize.y));

        // Мировой размер одного тайла
        float tileWorldW = worldW / countX;
        float tileWorldH = worldH / countY;

        // Локальный размер одного тайла (в диапазоне 0..1 от размера Wall)
        float tileLocalW = 1f / countX;
        float tileLocalH = 1f / countY;

        // Контейнер
        var containerGO = new GameObject("WallTilesContainer");
        containerGO.transform.SetParent(transform, false);
        containerGO.transform.localPosition = Vector3.zero;
        containerGO.transform.localRotation = Quaternion.identity;
        containerGO.transform.localScale = Vector3.one;
        container = containerGO.transform;

        // Источник — спрайт тайла
        Texture2D srcTex = tileSprite.texture;
        int srcW = srcTex.width;
        int srcH = srcTex.height;
        Color[] srcPixels = srcTex.GetPixels();

        float spriteW = tileSprite.bounds.size.x;
        float spriteH = tileSprite.bounds.size.y;

        if (spriteW <= 0.0001f) spriteW = 1f;
        if (spriteH <= 0.0001f) spriteH = 1f;

        for (int y = 0; y < countY; y++)
        {
            for (int x = 0; x < countX; x++)
            {
                var go = new GameObject("Tile_" + x + "_" + y);
                go.transform.SetParent(container, false);

                // Локальная позиция (в диапазоне [-0.5..0.5], потому что Wall имеет localScale = 1)
                float lx = -0.5f + tileLocalW * (x + 0.5f);
                float ly = -0.5f + tileLocalH * (y + 0.5f);

                go.transform.localPosition = new Vector3(lx, ly, 0f);

                // Своя текстура на тайл
                var tex = new Texture2D(srcW, srcH, TextureFormat.RGBA32, false);
                tex.filterMode = FilterMode.Point;
                tex.SetPixels(srcPixels);
                tex.Apply();

                var sprite = Sprite.Create(tex,
                    new Rect(0, 0, srcW, srcH),
                    new Vector2(0.5f, 0.5f),
                    tileSprite.pixelsPerUnit);

                var sr = go.AddComponent<SpriteRenderer>();
                sr.sprite = sprite;
                sr.color = tileColor;
                sr.sortingOrder = sortingOrder;

                // Масштаб тайла: делаем так, чтобы спрайт занимал ровно tileLocalW × tileLocalH в локальных единицах Wall
                go.transform.localScale = new Vector3(
                    tileLocalW / spriteW,
                    tileLocalH / spriteH,
                    1f);

                // Коллайдер — по спрайту (в локальных единицах тайла до масштаба)
                var box = go.AddComponent<BoxCollider2D>();
                box.size = new Vector2(spriteW, spriteH);
                box.offset = Vector2.zero;
                box.isTrigger = false;

                // Данные тайла
                var td = new TileData();
                td.go = go;
                td.sr = sr;
                td.col = box;
                td.tex = tex;
                td.texW = srcW;
                td.texH = srcH;
                td.pixels = tex.GetPixels();
                td.hp = tileHP;
                td.localW = tileLocalW;
                td.localH = tileLocalH;
                td.localX = lx - tileLocalW * 0.5f;
                td.localY = ly - tileLocalH * 0.5f;
                tiles.Add(td);
            }
        }

        if (debugLogs)
            Debug.Log("[Wall] Тайлов: " + (countX * countY) + " (" + countX + "x" + countY
                + "), мировой размер тайла: " + tileWorldW.ToString("F2") + "x" + tileWorldH.ToString("F2"));
    }

    // ============================================================
    // РАЗРУШЕНИЕ
    // ============================================================
    public void BreakAtPoint(Vector2 worldPoint, float power = 1f)
    {
        if (tiles.Count == 0) return;

        // Мировая точка → локальная (относительно Wall)
        // transform.InverseTransformPoint даёт локальные координаты Wall
        Vector2 local = transform.InverseTransformPoint(worldPoint);

        if (debugLogs)
            Debug.Log("[Wall] Break world=" + worldPoint + " local=" + local);

        // Ищем тайл, в который попали
        TileData hitTile = FindTileAtLocal(local);
        if (hitTile == null)
        {
            if (debugLogs) Debug.Log("[Wall] Не попал ни в один тайл");
            return;
        }

        // Пробиваем дыру в этом тайле
        PunchHole(hitTile, local, power);

        // Разлёт пикселей
        if (spawnDebris)
            SpawnDebris(worldPoint, hitTile, local);

        // HP -1
        hitTile.hp--;

        if (debugLogs)
            Debug.Log("[Wall] " + hitTile.go.name + " HP: " + hitTile.hp);

        // Если HP = 0 — отключаем коллайдер
        if (hitTile.hp <= 0)
        {
            if (hitTile.col != null) hitTile.col.enabled = false;

            if (debugLogs)
                Debug.Log("[Wall] " + hitTile.go.name + " разрушен");
        }
    }

    TileData FindTileAtLocal(Vector2 local)
    {
        foreach (var t in tiles)
        {
            if (local.x >= t.localX && local.x <= t.localX + t.localW &&
                local.y >= t.localY && local.y <= t.localY + t.localH)
                return t;
        }
        return null;
    }

    void PunchHole(TileData tile, Vector2 holeLocal, float power)
    {
        // Относительные координаты внутри тайла [0..1]
        float relX = (holeLocal.x - tile.localX) / tile.localW;
        float relY = (holeLocal.y - tile.localY) / tile.localH;

        int cx = Mathf.RoundToInt(relX * tile.texW);
        int cy = Mathf.RoundToInt(relY * tile.texH);

        float worldRadius = Random.Range(holeRadiusMin, holeRadiusMax) * power;

        // Радиус в пикселях тайла: tile.localW соответствует tileWorldW в мире
        // Но дыра задаётся в МИРОВЫХ юнитах, поэтому приводим через размер тайла в мире.
        float tileWorldW = tile.localW * Mathf.Abs(transform.lossyScale.x);
        float tileWorldH = tile.localH * Mathf.Abs(transform.lossyScale.y);

        int rX = Mathf.Max(2, Mathf.RoundToInt(worldRadius / tileWorldW * tile.texW));
        int rY = Mathf.Max(2, Mathf.RoundToInt(worldRadius / tileWorldH * tile.texH));

        if (debugLogs)
            Debug.Log("[Wall] Hole rel=(" + relX.ToString("F2") + "," + relY.ToString("F2")
                + ") px=(" + cx + "," + cy + ") r=(" + rX + "," + rY + ")");

        for (int y = cy - rY; y <= cy + rY; y++)
        {
            for (int x = cx - rX; x <= cx + rX; x++)
            {
                if (x < 0 || x >= tile.texW || y < 0 || y >= tile.texH) continue;

                float dx = (x - cx) / (float)rX;
                float dy = (y - cy) / (float)rY;
                float dist = dx * dx + dy * dy;

                if (dist <= 1f)
                {
                    float noise = Mathf.PerlinNoise(x * 0.3f, y * 0.3f);
                    if (noise > 0.3f || dist < 0.5f)
                        tile.pixels[y * tile.texW + x] = Color.clear;
                }
            }
        }

        tile.tex.SetPixels(tile.pixels);
        tile.tex.Apply();
    }

    void SpawnDebris(Vector2 worldPoint, TileData tile, Vector2 local)
    {
        Color debrisColor = GetPixelColorAt(tile, local);

        for (int i = 0; i < debrisCount; i++)
        {
            var go = new GameObject("Debris");
            go.transform.position = worldPoint + (Vector2)Random.insideUnitCircle * 0.15f;

            var sr = go.AddComponent<SpriteRenderer>();
            sr.sprite = LightSpriteGenerator.WhitePixel;
            sr.color = debrisColor;
            sr.sortingOrder = sortingOrder + 5;
            go.transform.localScale = Vector3.one * debrisSize;

            var rb = go.AddComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Dynamic;
            rb.gravityScale = 1.5f;
            rb.AddForce(Random.insideUnitCircle * debrisSpeed, ForceMode2D.Impulse);

            var script = go.AddComponent<WallDebris>();
            script.lifetime = debrisLifetime;
        }
    }

    Color GetPixelColorAt(TileData tile, Vector2 local)
    {
        float relX = (local.x - tile.localX) / tile.localW;
        float relY = (local.y - tile.localY) / tile.localH;

        int px = Mathf.Clamp(Mathf.RoundToInt(relX * tile.texW), 0, tile.texW - 1);
        int py = Mathf.Clamp(Mathf.RoundToInt(relY * tile.texH), 0, tile.texH - 1);

        Color c = tile.pixels[py * tile.texW + px];
        if (c.a < 0.1f) return Color.gray;

        return c;
    }
}