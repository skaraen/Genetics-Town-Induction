using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Add this to a UI element to make it a drop target for DraggableItems
/// </summary>
public class SlotHandler : MonoBehaviour, IDropHandler, IPointerClickHandler
{
    [Tooltip("If provided, spawns a copy of respawnItemOnDragOut if respawnItemOnDragOut is dragged out of this slot onto a different slot")]
    [SerializeField] private DraggableItem respawnItemOnDragOut;
    [SerializeField] private bool acceptItems = true;
    [Tooltip("If true, items dropped into this slot will smoothly lerp to center position")]
    [SerializeField] public bool lerpItemsToCenter = true;
    
    public void OnDrop(PointerEventData eventData)
    {
        Debug.Log($"[SlotHandler] OnDrop called on {gameObject.name}");
        if(!acceptItems) return;
        if (transform.childCount == 0)
        {
            GameObject dropped = eventData.pointerDrag;
            DraggableItem dItem = dropped.GetComponent<DraggableItem>();
            Debug.Log($"[SlotHandler] Setting parentAfterDrag to {transform.name}");
            dItem.parentAfterDrag = transform;
        }
    }

    /// <summary>
    /// Called when a draggable item that was originally in this slot is dragged out
    /// </summary>
    public void OnItemEndDrag()
    {
        // if the child has changed, respawn a new one
        if(respawnItemOnDragOut != null && respawnItemOnDragOut.transform.parent != transform)
        {
            Instantiate(respawnItemOnDragOut, transform);
        }
    }

    /// <summary>
    /// Debug method to check if raycasts are reaching this slot
    /// </summary>
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"item clicked: {gameObject.name}, children: {transform.childCount}");
    }
}
