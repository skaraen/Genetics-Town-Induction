using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// Component handler needs to be renamed
/// Every object in unity is a component and does some kind of "handling" with it anyway
/// </summary>
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

    /// <summary>
    /// Instantiates the left-strand nucleotide prefabs into LContainer, one per character in BUG_SEQ.
    /// </summary>
    void PopulateLContainer()
    {
        foreach (char c in BUG_SEQ)
        {
            GameObject component = Instantiate(prefabMap[c + "_L"], LContainer.transform);
        }
    }

    /// <summary>
    /// Spawns shuffled nucleotides into CompContainer with boid swimming behavior.
    /// Expects CompContainer to already have a SlotHandler and no GridLayoutGroup in the scene.
    /// </summary>
    void PopulateCompContainer()
    {
        SlotHandler poolSlot = CompContainer.GetComponent<SlotHandler>();

        // Build shuffled array of nucleotide types (3 of each)
        char[] compTypes = new char[NUM_COMPONENTS];
        for (int i = 0; i < set.Length; i++)
        {
            int k = i * (NUM_COMPONENTS / set.Length);
            for (int j = 0; j < NUM_COMPONENTS / set.Length; j++)
                compTypes[k + j] = set[i];
        }
        ArrayUtils.ShuffleArray(compTypes);

        // Instantiate nucleotides directly under CompContainer with boid behavior
        RectTransform containerRect = CompContainer.GetComponent<RectTransform>();
        Rect bounds = containerRect.rect;
        float margin = 100f;

        for (int i = 0; i < compTypes.Length; i++)
        {
            GameObject component = Instantiate(prefabMap[compTypes[i] + "_R"], CompContainer.transform);

            RectTransform rt = component.GetComponent<RectTransform>();
            // Spawn boids in random position inside parent rect
            rt.anchoredPosition = new Vector2(
                Random.Range(bounds.xMin + margin, bounds.xMax - margin),
                Random.Range(bounds.yMin + margin, bounds.yMax - margin)
            );

            DraggableItem dItem = component.GetComponent<DraggableItem>();
            dItem.compType = compTypes[i];
            dItem.slotAfterDrag = poolSlot;

            BoidController boid = component.AddComponent<BoidController>();
            boid.Initialize(containerRect);
        }
    }

    public bool IsASOValid()
    {
        // This function is pretty badly coupled
        // Despite its name "IsASOValid", it also sets the ASO string
        // so (this is not obvious externaly at all) you need to call IsASOValid before isASOCorrect
        // furthermore isASOValid and IsASOCorrect have names that sound the exact same and don't explain their differences
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
