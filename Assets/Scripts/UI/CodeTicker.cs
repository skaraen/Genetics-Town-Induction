using System.Collections.Generic;
using System.Text;
using UnityEngine;

public class CodeTicker : MonoBehaviour
{
    [SerializeField] float tickSpeed;
    [SerializeField] GameObject tickerItemPrefab;

    List<VerticalTickItem> vtItems;
    float pixelsPerSecond;
    RectTransform rt;
    float height;
    float width;
    System.Random rng;

    readonly char[] binaryChars  = { '0', '1' };
    readonly char[] geneticChars = { 'A', 'C', 'T', 'G' };

    void Start()
    {
        rt = transform.parent.GetComponent<RectTransform>();
        height = rt.rect.height;
        width = rt.rect.width;
        rng = new System.Random();

        vtItems = new List<VerticalTickItem>();
        pixelsPerSecond = height / tickSpeed;
    }

    public void SpawnStrands()
    {
        int prefabWidth = 30;
        int numItems = (int) width / prefabWidth;

        Debug.Log($"Number of items {numItems}");

        for (int i = 0; i < numItems; i++)
        {
            GameObject go = Instantiate(tickerItemPrefab, transform.parent);
            VerticalTickItem vt = go.GetComponent<VerticalTickItem>();

            float x = i * prefabWidth;
            float y = rng.Next(0, (int)height);

            vt.Init(height, pixelsPerSecond, (i % 2 == 0) ? 1 : -1, x, y);
            vt.UpdateText(RandomSequence(binaryChars, 9));

            vtItems.Add(vt);
        }
    }

    string RandomSequence(char[] alphabet, int length)
    {
        StringBuilder sb = new StringBuilder(length);
        for (int i = 0; i < length; i++)
        {
            sb.Append(alphabet[rng.Next(alphabet.Length)]);
        }
        return sb.ToString();
    }

    public void UpdateStrands()
    {
        foreach (VerticalTickItem vt in vtItems)
        {
            vt.FlickerUpdateText(RandomSequence(geneticChars, 9));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
