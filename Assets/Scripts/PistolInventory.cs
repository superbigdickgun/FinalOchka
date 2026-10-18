using UnityEngine;

public class PistolInventory : MonoBehaviour
{
    [Header("=== СОСТОЯНИЕ ===")]
    public bool hasPistol = false;

    [Header("=== ССЫЛКИ ===")]
    public Pistol pistol;

    [Tooltip("Объект руки с оружием (ArmGun). Включается при подборе.")]
    public GameObject armGun;

    void Awake()
    {
        if (pistol == null)
            pistol = GetComponentInChildren<Pistol>(true);

        // При старте рука с оружием ВЫКЛЮЧЕНА
        if (armGun != null) armGun.SetActive(false);
    }

    public void Pickup()
    {
        hasPistol = true;
        if (armGun != null) armGun.SetActive(true);
        if (pistol != null) pistol.Equip();
        Debug.Log("[PistolInv] Пистолет подобран");
    }

    public void Drop()
    {
        hasPistol = false;
        if (armGun != null) armGun.SetActive(false);
        if (pistol != null) pistol.Unequip();
        Debug.Log("[PistolInv] Пистолет выброшен");
    }
}