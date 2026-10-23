using UnityEngine;

public class WhaleFloorPuddle : MonoBehaviour
{
    public float lifetime = 10f;
    public System.Action onDestroyCallback;

    private float timer = 0f;

    void Update()
    {
        if (lifetime <= 0f) return; // Если 0 — не исчезает

        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            onDestroyCallback?.Invoke();
            Destroy(gameObject);
        }
    }
}