using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// Add to a UI element to make it draggable. To use:
/// - add dragable item component to the item
/// - Add a slothandler to the place you want to drop it in
/// </summary>
public class DraggableItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [HideInInspector] public Transform parentAfterDrag;
    [HideInInspector] public char compType;
    private Canvas canvas;
    private Image image;

    void Awake()
    {
        canvas = transform.GetComponentInParent<Canvas>().rootCanvas;
        image = transform.GetComponent<Image>();
    }
    public void OnBeginDrag(PointerEventData eventData)
    {
        parentAfterDrag = transform.parent;
        transform.SetParent(transform.root);
        transform.SetAsLastSibling();
        image.raycastTarget = false;
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
        // If dropped in a valid slot, the SlotHandler will have updated parentAfterDrag to the slot
        transform.SetParent(parentAfterDrag);
        image.raycastTarget = true;
    }
}
