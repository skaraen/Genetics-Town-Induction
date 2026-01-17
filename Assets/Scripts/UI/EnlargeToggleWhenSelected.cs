using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// Drop onto a component with a UI toggle to make it enlarge when selected
/// </summary>
[RequireComponent(typeof(UnityEngine.UI.Toggle))]
public class EnlargeToggleWhenSelected : MonoBehaviour
{
    private Toggle toggle;
    private Vector3 initialScale;

    [SerializeField] private float scaleIncrease = 1.2f;
    [SerializeField] private float animationDuration = 0.3f;

    void Start()
    {
        toggle = GetComponent<Toggle>();
        initialScale = transform.localScale;

        toggle.onValueChanged.AddListener(OnToggleValueChanged);
    }

    void OnDestroy()
    {
        if (toggle != null)
            toggle.onValueChanged.RemoveListener(OnToggleValueChanged);
        
        transform.DOKill();
    }

    /// <summary>
    /// Enlarge the component when selected, shrink it back when deselected
    /// <param name="isOn">Whether the toggle is on or off</param>
    /// </summary>
    private void OnToggleValueChanged(bool isOn)
    {
        // Cancel any ongoing animations on this transform
        transform.DOKill();

        // Animate to the target scale with bounce effect
        Vector3 targetScale = isOn ? initialScale * scaleIncrease : initialScale;
        transform.DOScale(targetScale, animationDuration)
            .SetEase(Ease.OutBack);
    }
}
