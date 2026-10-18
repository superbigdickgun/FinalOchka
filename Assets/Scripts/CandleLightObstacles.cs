using UnityEngine;

public class CandleLightObstacles : MonoBehaviour
{
    [Header("=== СВЕТ ===")]
    public float radius = 3f;
    public float softness = 1f;
    public float intensity = 0.9f;

    [Header("=== МЕРЦАНИЕ ===")]
    public bool flicker = false;
    public float flickerAmount = 0.1f;
    public float flickerSpeed = 8f;

    [Header("=== ПРЕПЯТСТВИЯ ===")]
    public LayerMask blockerLayers = ~0;

    [Header("=== ТОЧНОСТЬ ===")]
    [Tooltip("Количество лучей. Больше = точнее. 16 = грубо, 64 = точно.")]
    public int rayCount = 32;

    // Static буфер — сюда пишет каждая свеча, читает DarknessOverlay
    public static int MaxCandles = 10;
    public static float[,] distancesBuffer = new float[MaxCandles, 64];
    public static Vector2[] positionsBuffer = new Vector2[MaxCandles];
    public static float[] radiiBuffer = new float[MaxCandles];
    public static float[] softnessBuffer = new float[MaxCandles];
    public static float[] intensityBuffer = new float[MaxCandles];
    public static int rayCountBuffer = 32;
    public static int activeCandles = 0;

    private int myIndex = -1;

    void OnEnable()
    {
        if (activeCandles < MaxCandles)
        {
            myIndex = activeCandles;
            activeCandles++;
        }
    }

    void OnDisable()
    {
        // Упрощённо: сбрасываем
        activeCandles = 0;
    }

    void LateUpdate()
    {
        if (myIndex < 0) return;

        // Позиция
        positionsBuffer[myIndex] = transform.position;

        // Мерцание
        float r = radius;
        if (flicker)
            r += Mathf.PerlinNoise(Time.time * flickerSpeed, myIndex) * flickerAmount * radius;

        radiiBuffer[myIndex] = r;
        softnessBuffer[myIndex] = softness;
        intensityBuffer[myIndex] = intensity;
        rayCountBuffer = rayCount;

        // Raycast в N направлений
        for (int i = 0; i < rayCount; i++)
        {
            float a = (i / (float)rayCount) * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));

            RaycastHit2D hit = Physics2D.Raycast(transform.position, dir, r, blockerLayers);
            float dist = hit.collider != null ? hit.distance : r;

            distancesBuffer[myIndex, i] = dist;
        }
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, radius);

        if (!Application.isPlaying) return;

        Gizmos.color = Color.red;
        for (int i = 0; i < rayCount; i++)
        {
            float a = (i / (float)rayCount) * Mathf.PI * 2f;
            Vector2 dir = new Vector2(Mathf.Cos(a), Mathf.Sin(a));
            Gizmos.DrawRay(transform.position, (Vector3)(dir * distancesBuffer[myIndex, i]));
        }
    }
}