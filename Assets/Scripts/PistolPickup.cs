using UnityEngine;

public class PistolPickup : MonoBehaviour
{
    public string promptText = "Подобрать пистолет";
    private bool playerInRange;
    private PistolInventory playerInv;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = true;
            playerInv = other.GetComponent<PistolInventory>();
            if (playerInv == null) playerInv = other.GetComponentInParent<PistolInventory>();
            if (UI_Controller.Instance != null) UI_Controller.Instance.ShowPrompt(promptText);
        }
    }

    void OnTriggerExit2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            playerInRange = false;
            if (UI_Controller.Instance != null) UI_Controller.Instance.HidePrompt();
        }
    }

    void Update()
    {
        if (playerInRange && Input.GetKeyDown(KeyCode.E))
        {
            if (playerInv != null && !playerInv.hasPistol)
            {
                playerInv.Pickup();
                if (UI_Controller.Instance != null) UI_Controller.Instance.HidePrompt();
                Destroy(gameObject);
            }
        }
    }
}