using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

public class SwitchToggle : MonoBehaviour
{
    [SerializeField] RectTransform uiRectTransform;
    Vector2 defaultPosition;
    Toggle toggle;
    Image backgroundImage;
    Sprite toggleOnSprite;
    Sprite toggleOffSprite;
    public UnityEvent<bool> onToggleChanged;

    void Awake()
    {
        defaultPosition = uiRectTransform.anchoredPosition;
        backgroundImage = uiRectTransform.parent.GetComponent<Image>();
        toggleOnSprite = Resources.Load<Sprite>("Images/toggle-on");
        toggleOffSprite = Resources.Load<Sprite>("Images/toggle-off");

        toggle = GetComponent<Toggle>();
        toggle.onValueChanged.AddListener(OnSwitch);
        if (toggle.isOn)
        {
            OnSwitch(true);
        }
    }

    void OnSwitch(bool isOn)
    {
        uiRectTransform.anchoredPosition = isOn ? defaultPosition : defaultPosition * -1;
        backgroundImage.sprite = isOn ? toggleOnSprite : toggleOffSprite;

        onToggleChanged?.Invoke(isOn);
    }
}