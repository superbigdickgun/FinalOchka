using UnityEngine;

public class LightSource2D : MonoBehaviour
{
    [Header("=== СВЕТ ===")]
    [Tooltip("Радиус света в юнитах.")]
    public float radius = 3f;

    [Tooltip("Мягкость края (в юнитах). Больше = плавнее.")]
    public float softness = 1f;

    [Tooltip("Яркость. 1 = полностью снимает тьму, 0.5 = наполовину.")]
    [Range(0f, 1f)] public float intensity = 0.9f;

    [Header("=== ЦВЕТ ===")]
    public Color lightColor = new Color(1f, 0.85f, 0.55f);

    [Header("=== МЕРЦАНИЕ ===")]
    public bool flicker = false;
    public float flickerAmount = 0.1f;
    public float flickerSpeed = 8f;
}