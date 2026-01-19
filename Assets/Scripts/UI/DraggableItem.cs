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
    [HideInInspector] public Transform parentAfterDrag;
    private Transform cachedParentBeforeDrag;
    [HideInInspector] public char compType;
    [HideInInspector] public bool lerpToParent = false; // flag triggered by slots to lerp back to parent on end drag
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
        cachedParentBeforeDrag = transform.parent;
        parentAfterDrag = transform.parent;
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
        transform.SetParent(parentAfterDrag);
        if(transform.parent.GetComponent<SlotHandler>().lerpItemsToCenter)
        {
            // Lerp to center of new parent, whether it's a new slot or the original slot
            transform.DOLocalMove(Vector3.zero, 0.3f).SetEase(Ease.OutQuad);
            lerpToParent = false; // slot handler sets lerpParentToTrue if it needs to lerp on the latest drop
        }
        if(cachedParentBeforeDrag != parentAfterDrag)
        {
            // notify the old slot that an item has left it
            SlotHandler oldSlot = cachedParentBeforeDrag.GetComponent<SlotHandler>();
            if(oldSlot != null)
            {
                oldSlot.ItemRemovedFromSlot();
            }
        }
        canvasGroup.blocksRaycasts = true;
    }
}
