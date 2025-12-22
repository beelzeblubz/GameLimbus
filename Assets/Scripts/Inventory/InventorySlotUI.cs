using UnityEngine;
using UnityEngine.UI;

public class InventorySlotUI : MonoBehaviour
{
    public Image icon;
    public Text amountText;
    InventorySlot slot;

    public void SetSlot(InventorySlot newSlot)
    {
        slot = newSlot;

        if (slot.IsEmpty())
        {
            icon.enabled = false;
            amountText.text = "";
        }
        else
        {
            icon.enabled = true;
            icon.sprite = slot.item.icon;
            amountText.text = slot.amount > 1 ? slot.amount.ToString() : "";
        }
    }
}
