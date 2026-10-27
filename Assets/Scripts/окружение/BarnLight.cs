using UnityEngine;

public class BarnLight : MonoBehaviour
{
    [Header("=== ГЕНЕРАТОР ===")]
    public Generator generator;

    private bool lastState = false;

    void Start()
    {
        // На старте принудительно выключаем свет
        gameObject.SetActive(false);
        if (generator == null) generator = FindObjectOfType<Generator>();
    }

    void Update()
    {
        if (generator == null) return;

        // Если генератор включен - включаем свет, иначе выключаем
        bool power = generator.isOn;

        if (power != lastState)
        {
            lastState = power;
            gameObject.SetActive(power);
        }
    }
}