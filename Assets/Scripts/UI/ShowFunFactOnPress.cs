using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;

/// <summary>
/// On press: waits a short delay so the sprite-swap is visible, fades the button out,
/// then reveals the target panel with a DOTween bounce-in animation.
/// Attach to a Button; wire onClick to Show() in the inspector.
/// </summary>
[RequireComponent(typeof(Button))]
[RequireComponent(typeof(CanvasGroup))]
public class ShowFunFactOnPress : MonoBehaviour
{
    [SerializeField] private GameObject panel;
    [SerializeField] private float delay = 0.3f;
    [SerializeField] private float fadeOutDuration = 0.15f;
    [SerializeField] private float bounceInDuration = 0.4f;

    /// <summary>
    /// Disables interaction immediately, waits for the sprite-swap animation to register,
    /// fades the button out, then bounces the fun fact panel in from scale zero.
    /// </summary>
    public void Show()
    {
        GetComponent<Button>().interactable = false;
        CanvasGroup cg = GetComponent<CanvasGroup>();

        DOTween.Sequence()
            .AppendInterval(delay)
            .Append(cg.DOFade(0f, fadeOutDuration))
            .AppendCallback(() =>
            {
                panel.SetActive(true);
                panel.transform.localScale = Vector3.zero;
            })
            .Append(panel.transform.DOScale(Vector3.one, bounceInDuration).SetEase(Ease.OutBack));
    }
}
