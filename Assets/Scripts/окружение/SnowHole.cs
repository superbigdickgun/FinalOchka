using UnityEngine;

public class SnowHole : MonoBehaviour
{
    public float lifetime = 5f;
    public System.Action onDestroyCallback;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        if (timer >= lifetime)
        {
            onDestroyCallback?.Invoke();
            Destroy(gameObject);
        }
    }
}