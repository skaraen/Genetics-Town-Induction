using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

public class ComponentHandler : MonoBehaviour
{
    const int NUM_COMPONENTS = 12;
    const string BUG_SEQ = "ACTGCT";
    const string set = "ATGC";
    public GameObject LContainer;
    public GameObject RContainer;
    public GameObject CompContainer;

    private Dictionary<string, GameObject> prefabMap;
    private Dictionary<char, char> compMap = new Dictionary<char, char>
    {
        { 'A', 'T' },
        { 'T', 'A' },
        { 'C', 'G' },
        { 'G', 'C' }
    };
    private string ASO;
    private

    void OnEnable()
    {
        prefabMap = new Dictionary<string, GameObject>();
        LoadPrefabs();
        PopulateLContainer();
        PopulateCompContainer();
    }

    void LoadPrefabs()
    {
        foreach (char c in set)
        {
            prefabMap[c + "_L"] = Resources.Load<GameObject>("Prefabs/" + c + "_L");
            prefabMap[c + "_R"] = Resources.Load<GameObject>("Prefabs/" + c + "_R");
        }
    }

    void PopulateLContainer()
    {
        foreach (char c in BUG_SEQ)
        {
            GameObject component = Instantiate(prefabMap[c + "_L"], LContainer.transform);
        }
    }

    void PopulateCompContainer()
    {
        char[] compTypes = new char[NUM_COMPONENTS];

        for (int i = 0; i < set.Length; i++)
        {
            int k = i * (NUM_COMPONENTS / set.Length);
            for (int j = 0; j < NUM_COMPONENTS / set.Length; j++)
            {
                compTypes[k + j] = set[i];
            }
        }

        ArrayUtils.ShuffleArray(compTypes);
        for (int i = 0; i < compTypes.Length; i++)
        {
            GameObject component = Instantiate(prefabMap[compTypes[i] + "_R"], CompContainer.transform.GetChild(i));
            component.GetComponent<DraggableItem>().compType = compTypes[i];
        }
    }

    public bool IsASOValid()
    {
        ASO = "";
        for (int i = 0; i < RContainer.transform.childCount; i++)
        {
            if (RContainer.transform.GetChild(i).childCount == 0)
                return false;
            ASO += RContainer.transform.GetChild(i).GetChild(0).GetComponent<DraggableItem>().compType;
        }
        return true;
    }

    public bool IsASOCorrect()
    {
        if (ASO.Length != BUG_SEQ.Length)
            return false;
        for (int i = 0; i < ASO.Length; i++)
        {
            if (compMap[ASO[i]] != BUG_SEQ[i])
            {
                Debug.Log($"Failed at {i}, ASO: {ASO}, Bug: {BUG_SEQ}");
                return false;   
            }
        }
        return true;
    }
}
