using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

public class PuzzleSlotDrop : MonoBehaviour, IDropHandler
{
    private int slotID;
    private PuzzleInventory inventory;
    
    public void Initialize(int id, PuzzleInventory inv)
    {
        slotID = id;
        inventory = inv;
    }
    
    public void OnDrop(PointerEventData eventData)
    {
        // Method ini akan dipanggil otomatis oleh Unity Event System
        // Tapi kita handle drop di DraggablePuzzlePiece menggunakan raycast
    }
    
    public void OnPieceDropped(int pieceID, GameObject pieceObj)
    {
        if (inventory != null)
        {
            inventory.OnPieceDroppedOnSlot(slotID, pieceID, pieceObj);
        }
    }
}