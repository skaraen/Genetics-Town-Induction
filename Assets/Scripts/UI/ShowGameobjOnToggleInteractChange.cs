using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Shows or hides specific game objects based on the interactable state of the attached Toggle component.
/// </summary>
[RequireComponent(typeof(Toggle))]
public class ShowGameobjOnToggleInteractChange : MonoBehaviour
{
    [Tooltip("List of gameobjects to enable/disable based on the toggle's interactable state.")]
    [SerializeField] private List<GameObject> gameobjcsToShowHide;
    [Tooltip("The gameobjects will show if the toggle state = interactableStateToShowGameobjects")]
    [SerializeField] private bool interactableStateToShowGameobjects = true;
    
    private Toggle toggle;
    
    void Start()
    {
        toggle = GetComponent<Toggle>();

        // Subscribe to value changed event as a trigger to check state
        // Note: Interactable changes don't have their own event, but often accompany value changes
        toggle.onValueChanged.AddListener(OnToggleValueChanged);

        // Initialize the gameobject states
        UpdateGameobjectVisibility();
    }

    void OnToggleValueChanged(bool isOn)
    {
        UpdateGameobjectVisibility();
    }
    
    private void UpdateGameobjectVisibility()
    {
        bool shouldShow = toggle.interactable == interactableStateToShowGameobjects;
        
        foreach(var gameObj in gameobjcsToShowHide)
        {
            if(gameObj != null)
            {
                gameObj.SetActive(shouldShow);
            }
        }
    }
}
