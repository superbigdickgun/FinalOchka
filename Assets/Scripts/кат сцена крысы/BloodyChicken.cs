using UnityEngine;

public class BloodyChicken : MonoBehaviour
{
    public BloodFXController bloodFX;
    public float breathSpeed = 3f;
    public float breathAmount = 0.05f;
    public float pulseIntervalMin = 0.1f;
    public float pulseIntervalMax = 0.3f;

    private Vector3 baseScale;
    private float pulseTimer = 0f;
    private bool isFountainActive = false;

    void Start() 
    { 
        baseScale = transform.localScale; 
    }

    public void StartFountain() { isFountainActive = true; }
    public void StopFountain() { isFountainActive = false; }

    void Update()
    {
        float breath = Mathf.Sin(Time.time * breathSpeed) * breathAmount;
        transform.localScale = baseScale + new Vector3(breath, breath * 0.5f, 0f);

        if (!isFountainActive) return;

        pulseTimer -= Time.deltaTime;
        if (pulseTimer <= 0f)
        {
            pulseTimer = Random.Range(pulseIntervalMin, pulseIntervalMax);
            TriggerSplatter();
        }
    }

    public void TriggerSplatter()
    {
        if (bloodFX != null) bloodFX.SpawnSplatter(transform.position);
    }
}