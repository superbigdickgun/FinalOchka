using UnityEngine;

public class CrowBloodMover : MonoBehaviour
{
    public Vector2 velocity;
    public float lifetime = 1.5f;

    private float timer = 0f;

    void Update()
    {
        timer += Time.deltaTime;
        transform.position += (Vector3)velocity * Time.deltaTime;

        if (timer >= lifetime)
            Destroy(gameObject);
    }
}