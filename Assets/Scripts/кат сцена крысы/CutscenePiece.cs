using UnityEngine;

public class CutscenePiece : MonoBehaviour
{
    public Sprite bloodTrailSprite;
    public float spawnInterval = 0.1f;
    private float timer = 0f;

    void Update()
    {
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = spawnInterval;
            if (bloodTrailSprite != null)
            {
                var trail = new GameObject("BloodTrail");
                trail.transform.position = transform.position;
                var sr = trail.AddComponent<SpriteRenderer>();
                sr.sprite = bloodTrailSprite;
                sr.color = new Color(0.5f, 0f, 0f, 0.5f);
                sr.sortingOrder = 0;
                Destroy(trail, 2f);
            }
        }
    }
}