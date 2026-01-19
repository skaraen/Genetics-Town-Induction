using System.Collections.Generic;
using UnityEditor.EditorTools;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;

/// <summary>
/// Add this to a UI element to make it a drop target for DraggableItems
/// </summary>
public class SlotHandler : MonoBehaviour, IDropHandler
{
    [Tooltip("If provided, spawns a copy of respawnItemOnDragOut if respawnItemOnDragOut is dragged out of this slot onto a different slot")]
    [SerializeField] private GameObject respawnItemOnDragOut;
    [SerializeField] private bool acceptItems = true;
    [Tooltip("If true, dropping an item into this slot will destroy an existing item in the slot. Will NOT work with respawnItemOnDragOut")]
    [SerializeField] private bool replaceItems = false;
    [Tooltip("If true, items dropped into this slot will smoothly lerp to center position")]
    [SerializeField] public bool lerpItemsToCenter = true;
    [Tooltip("If true, items that are dropped outside of any slot will be destroyed")]
    [SerializeField] public bool destroyItemOnFailedDrop = false;
    [Tooltip("Called whenever an item is dropped into or removed from this slot")]
    [SerializeField] public List<UnityEvent> callOnStateChange;
    public void OnDrop(PointerEventData eventData)
    {
        if(!acceptItems) return;
        if(replaceItems && transform.childCount > 0){
            Destroy(transform.GetChild(0).gameObject);
        }
        if (transform.childCount == 0 || replaceItems)
        {
            GameObject dropped = eventData.pointerDrag;
            DraggableItem dItem = dropped.GetComponent<DraggableItem>();
            dItem.slotAfterDrag = this;
        }
    }

    /// <summary>
    /// Called when a draggable item that was originally in this slot is dragged out
    /// </summary>
    public void ItemRemovedFromSlot()
    {
        callOnStateChange.ForEach(e => e.Invoke());
        // if the child has changed, optionally respawn a new one
        if(respawnItemOnDragOut != null && respawnItemOnDragOut.transform.parent != transform)
        {
            GameObject newItem = Instantiate(respawnItemOnDragOut, transform);
            respawnItemOnDragOut = newItem;
            CanvasGroup group = newItem.GetComponentInChildren<CanvasGroup>();
            // make sure raycast blocking is re-enabled on the new item
            if (group != null)
            {
                group.blocksRaycasts = true;
            }
            // spawn it in the center, if specified
            if(lerpItemsToCenter)
            {
                newItem.transform.localPosition = Vector3.zero;
            }
        }
    }
}
