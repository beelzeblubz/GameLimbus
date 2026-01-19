using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public class DraggablePuzzlePiece : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    private int pieceID;
    private PuzzleInventory inventory;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 originalPosition;
    private Transform originalParent;
    private Canvas canvas;
    private bool isLocked = false;
    
    public void Initialize(int id, PuzzleInventory inv)
    {
        pieceID = id;
        inventory = inv;
        rectTransform = GetComponent<RectTransform>();
        
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }
        
        canvas = GetComponentInParent<Canvas>();
    }
    
    public int GetPieceID()
    {
        return pieceID;
    }
    
    public void SetLocked(bool locked)
    {
        isLocked = locked;
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = !locked;
        }
    }
    
    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 0.6f;
            canvasGroup.blocksRaycasts = false;
        }
        
        originalPosition = rectTransform.anchoredPosition;
        originalParent = transform.parent;
        
        transform.SetParent(canvas.transform);
        transform.SetAsLastSibling();
        
        if (inventory != null)
        {
            inventory.OnPieceDragged(pieceID, gameObject);
        }
    }
    
    public void OnDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        
        if (rectTransform != null && canvas != null)
        {
            Vector2 pos;
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvas.transform as RectTransform,
                eventData.position,
                canvas.worldCamera,
                out pos
            );
            
            rectTransform.anchoredPosition = pos;
        }
    }
    
    public void OnEndDrag(PointerEventData eventData)
    {
        if (isLocked) return;
        
        if (canvasGroup != null)
        {
            canvasGroup.alpha = 1f;
            canvasGroup.blocksRaycasts = true;
        }
        
        bool droppedOnSlot = false;
        
        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);
        
        foreach (RaycastResult result in results)
        {
            PuzzleSlotDrop slot = result.gameObject.GetComponent<PuzzleSlotDrop>();
            if (slot != null)
            {
                slot.OnPieceDropped(pieceID, gameObject);
                droppedOnSlot = true;
                break;
            }
        }
        
        if (!droppedOnSlot)
        {
            transform.SetParent(originalParent);
            rectTransform.anchoredPosition = originalPosition;
        }
        
        if (inventory != null)
        {
            inventory.OnPieceDragEnd(pieceID, gameObject);
        }
    }
}