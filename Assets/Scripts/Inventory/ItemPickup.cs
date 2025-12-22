using UnityEngine;

public class ItemPickup : MonoBehaviour
{
    public ItemData item;

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            InventoryManager inventory = FindObjectOfType<InventoryManager>();
            if (inventory.AddItem(item))
            {
                Destroy(gameObject);
            }
        }
    }
}

