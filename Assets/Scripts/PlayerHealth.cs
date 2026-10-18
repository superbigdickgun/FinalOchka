using UnityEngine;

public class PlayerHealth : MonoBehaviour
{
    public float maxHP = 100f;
    public float currentHP = 100f;

    void Awake() { currentHP = maxHP; }

    public void TakeDamage(float amount)
    {
        currentHP -= amount;
        Debug.Log("[Player] HP: " + currentHP + " (-" + amount + ")");
        if (currentHP <= 0) Debug.Log("[Player] Умер");
    }
}