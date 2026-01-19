using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// Add to a UI element to make it draggable. To use:
/// - add dragable item component to the item
/// - Add a slothandler to the place you want to drop it in
/// </summary>
[RequireComponent(typeof(Image))]
[RequireComponent(typeof(CanvasGroup))]
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    // Static registry of all DraggableItems in the scene
    // by turning of all draggable item raycasts when one is dragged,
    // this allows you to drag over other draggable items that are in their own slots
    // and have their slots register the drop, for slot replacement,
    // rather than those draggable items blocking the raycast to the slot below them
    private static List<DraggableItem> allItems = new List<DraggableItem>();
    
    [HideInInspector] public SlotHandler slotAfterDrag; // the (possibly new) slot to place this item in after dragging
    private SlotHandler cachedSlotBeforeDrag; // the slot the item was in before the drag began
    
    [Tooltip("Character type identifier for this draggable item (e.g., DNA nucleotide letter A, T, G, or C)")]
    public char compType;
    private Canvas canvas;
    // make a canvas group for this draggable item so that we can disable raycast blocking while dragging
    // otherwise the draggableItem may block the slothandler onEndDrag from being called    
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvas = transform.GetComponentInParent<Canvas>().rootCanvas;
        canvasGroup = GetComponent<CanvasGroup>();
        
        // Register this item in the global list
        allItems.Add(this);
    }
    
    void OnDestroy()
    {
        // Unregister when destroyed
        allItems.Remove(this);
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        cachedSlotBeforeDrag = transform.parent.GetComponent<SlotHandler>();
        slotAfterDrag = cachedSlotBeforeDrag;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        
        // Disable raycast blocking for ALL draggable items when any item starts dragging
        foreach (var item in allItems)
        {
            if (item != null && item.canvasGroup != null)
            {
                item.canvasGroup.blocksRaycasts = false;
            }
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        Vector3 worldPos;
        if (RectTransformUtility.ScreenPointToWorldPointInRectangle(
            transform as RectTransform, eventData.position, canvas.worldCamera, out worldPos))
        {
            transform.position = worldPos;
        }
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        transform.SetParent(slotAfterDrag.transform);
        // if the item was returned to its original slot, optionally destroy it
        if(cachedSlotBeforeDrag.transform == transform.parent){
            if(slotAfterDrag.destroyItemOnFailedDrop)
            {
                Destroy(gameObject);
                return;
            }
        }

        if(slotAfterDrag.lerpItemsToCenter)
        {
            // Lerp to center of new parent, whether it's a new slot or the original slot
            transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutQuad);
        }
        if(cachedSlotBeforeDrag != slotAfterDrag)
        {
            // notify the old slot that an item has left it
            cachedSlotBeforeDrag.ItemRemovedFromSlot();
        }
        
        // Re-enable raycast blocking for ALL draggable items when drag ends
        foreach (var item in allItems)
        {
            if (item != null && item.canvasGroup != null)
            {
                item.canvasGroup.blocksRaycasts = true;
            }
        }
    }
}
