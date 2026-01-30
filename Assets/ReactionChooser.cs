using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Manages Gregor's reactions by enabling/disabling child gameobjects
/// with reaction names.
/// </summary>
public class ReactionChooser : MonoBehaviour
{
    [SerializeField] public List<GameObject> reactionsToAlwaysShow;
    GameObject[] _children;
    GameObject[] Children
    {
        get
        {
            if (_children == null){
                _children = new GameObject[transform.childCount];
                for (int i = 0; i < transform.childCount; i++)
                {
                    _children[i] = transform.GetChild(i).gameObject;
                }
            }
            return _children;
        }
    }


    /// <summary>
    /// Set gregor's reaction by enabling Gregor's child gameobject with name "reaction"
    /// and disabling all other reaction child gameobjects.
    /// </summary>
    /// <param name="reaction"></param>
    public void SetReaction(string reaction)
    {
        foreach (GameObject obj in Children)
        {
            if (obj.name == reaction || reactionsToAlwaysShow.Contains(obj))
                obj.SetActive(true);
            else
                obj.SetActive(false);
        }
    }
}
