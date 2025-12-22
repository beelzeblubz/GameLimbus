using UnityEngine;

public class InventoryManager : MonoBehaviour
{
    public InventorySlot[] slots;
    public InventorySlotUI[] slotUI;
    public GameObject inventoryPanel;

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.I))
        {
            inventoryPanel.SetActive(!inventoryPanel.activeSelf);
        }
    }


    void UpdateUI()
    {
        for (int i = 0; i < slots.Length; i++)
        {
            slotUI[i].SetSlot(slots[i]);
        }
    }

    public bool AddItem(ItemData newItem)
    {
        // Jika item bisa stack
        if (newItem.stackable)
        {
            foreach (var slot in slots)
            {
                if (!slot.IsEmpty() && slot.item == newItem && slot.amount < newItem.maxStack)
                {
                    slot.amount++;
                    return true;
                }
            }
        }

        // Cari slot kosong
        foreach (var slot in slots)
        {
            if (slot.IsEmpty())
            {
                slot.item = newItem;
                slot.amount = 1;
                return true;
            }
        }

        return false; // Inventory penuh
    }
}

