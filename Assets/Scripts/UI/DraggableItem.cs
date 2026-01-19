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
    [HideInInspector] public SlotHandler slotAfterDrag; // the (possibly new) slot to place this item in after dragging
    private SlotHandler cachedSlotBeforeDrag; // the slot the item was in before the drag began
    [HideInInspector] public char compType;
    private Canvas canvas;
    // make a canvas group for this draggable item so that we can disable raycast blocking while dragging
    // otherwise the draggableItem may block the slothandler onEndDrag from being called    
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvas = transform.GetComponentInParent<Canvas>().rootCanvas;
        canvasGroup = GetComponent<CanvasGroup>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        cachedSlotBeforeDrag = transform.parent.GetComponent<SlotHandler>();
        slotAfterDrag = cachedSlotBeforeDrag;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        canvasGroup.blocksRaycasts = false;
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
        canvasGroup.blocksRaycasts = true;
    }
}
