using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ScrollItem : MonoBehaviour
{
    const string COMP_SET = "ATGC";
    private Dictionary<string, GameObject> prefabMap;
    public float shiftDuration = 0.3f; 
    private Coroutine shiftCoroutine;
    private float rowShift = 0f;
    bool isLoaded = false;

    void LoadPrefabs()
    {
        prefabMap = new Dictionary<string, GameObject>();

        foreach (char c in COMP_SET)
        {
            prefabMap[c + "_L"] = Resources.Load<GameObject>("Prefabs/" + c + "_L");
        }
    }

    public void ClearSequence()
    {
        foreach (Transform child in transform)
        {
            Destroy(child.gameObject);
        }
    }

    public void SetSequence(string sequence)
    {
        if(!isLoaded) {
            LoadPrefabs();
            isLoaded = true;
        }
        
        ClearSequence();
        foreach (char c in sequence)
            Instantiate(prefabMap[c + "_L"], transform);
        UpdateGridHeight();
    }

    public void StartShift(int direction)
    {
        if (rowShift == 0) {
            rowShift = GetRowShiftAmount();
            Debug.Log($"Row shift amount {rowShift}");
        }

        if (shiftCoroutine != null)
            StopCoroutine(shiftCoroutine);

        shiftCoroutine = StartCoroutine(SmoothShift(direction * rowShift));
    }

    private IEnumerator SmoothShift(float deltaY)
    {
        Vector3 startPos = transform.localPosition;
        Vector3 targetPos = startPos + new Vector3(0, deltaY, 0);
        float elapsed = 0f;

        while (elapsed < shiftDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / shiftDuration);
            t = Mathf.SmoothStep(0f, 1f, t); // smooth easing
            transform.localPosition = Vector3.Lerp(startPos, targetPos, t);
            yield return null;
        }

        transform.localPosition = targetPos;
    }

    private float GetRowShiftAmount()
    {
        GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
        if (grid != null)
        {
            float rowHeight = grid.cellSize.y;
            float spacingY = grid.spacing.y;

            Debug.Log($"Row Height {rowHeight}");
            Debug.Log($"Row Spacing {spacingY}");

            return rowHeight + spacingY;
        }

        if (transform.childCount > 0)
        {
            RectTransform firstChild = transform.GetChild(0).GetComponent<RectTransform>();
            return firstChild.rect.height;
        }

        return 50f;
    }

    public void UpdateGridHeight()
    {
        GridLayoutGroup grid = GetComponent<GridLayoutGroup>();
        RectTransform rect = GetComponent<RectTransform>();

        int childCount = transform.childCount;
        int columns = grid.constraintCount;

        int rows = Mathf.CeilToInt((float)childCount / columns);

        float cellHeight = grid.cellSize.y;
        float spacingY = grid.spacing.y;
        float padding = grid.padding.top + grid.padding.bottom;

        float height = rows * cellHeight +
                    (rows - 1) * spacingY +
                    padding;

        rect.SetSizeWithCurrentAnchors(RectTransform.Axis.Vertical, height);
    }

    public void ResetPosition()
    {
        RectTransform rect = GetComponent<RectTransform>();
        rect.anchoredPosition = new Vector2(0f, -450f);
    }
}
