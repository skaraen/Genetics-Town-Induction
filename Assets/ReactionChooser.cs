using UnityEngine;

public class ReactionChooser : MonoBehaviour
{
    GameObject[] children;

    void GetChildren()
    {
        children = new GameObject[transform.childCount];
        for (int i = 0; i < transform.childCount; i++)
        {
            children[i] = transform.GetChild(i).gameObject;
        }
    }

    public void SetReaction(string reaction)
    {
        if (children == null)
            GetChildren();

        foreach (GameObject obj in children)
        {
            if (obj.name == reaction || obj.name == "outside")
                obj.SetActive(true);
            else
                obj.SetActive(false);
        }
    }
}
