using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class Scroller : MonoBehaviour
{
    [SerializeField] string sequence;
    [SerializeField] ScrollItem scrollItem;
    [SerializeField] Scroller oppScroller;
    [SerializeField] GameObject matcher;
    [SerializeField] Button upButton;
    [SerializeField] Button downButton;
    [SerializeField] TextMeshProUGUI title;
    [SerializeField] Color correctMatchColor;
    [SerializeField] Color wrongMatchColor;

    const int TARGET_WIDTH = 2;
    const int CODON_LENGTH = 3;

    int currRow, numRows;
    public string targetCodons;
    private bool isMatch;

    void OnEnable()
    {
        targetCodons = "";
        isMatch = false;

        upButton.onClick.AddListener(() => ShiftDown());
        downButton.onClick.AddListener(() => ShiftUp());
    }

    void SetTargetCodons()
    {
        if (numRows >= 2)
            targetCodons = sequence.Substring(currRow * 3, CODON_LENGTH * TARGET_WIDTH);

        CheckMatch();
    }

    void InitParams()
    {
        currRow = 0;
        numRows = sequence.Length / 3;
        SetTargetCodons();
    }

    public string GetTargetCodons()
    {
        return targetCodons;
    }

    public void CheckMatch()
    {
        if (oppScroller == null) return;

        string mine = targetCodons;
        string other = oppScroller.GetTargetCodons();

        bool signal = !string.IsNullOrEmpty(mine) && !string.IsNullOrEmpty(other) && mine == other;

        SignalMatcher(signal);
        oppScroller.SignalMatcher(signal);
    }

    public void SignalMatcher(bool signal)
    {
        if (isMatch == signal) return;

        isMatch = signal;
        matcher.GetComponent<Image>().color = isMatch ? correctMatchColor : wrongMatchColor;
    }

    public void SetGeneSequence(string sequence)
    {
        this.sequence = sequence;
        scrollItem.ResetPosition();

        scrollItem.SetSequence(this.sequence);
        InitParams();
    }

    public void ShiftUp()
    {
        if (currRow >= numRows - TARGET_WIDTH)
            return;

        if (scrollItem.StartShift(1))
        {
            currRow++;
            SetTargetCodons();
        }
    }

    public void ShiftDown()
    {
        if (currRow <= 0)
            return;
        
        if (scrollItem.StartShift(-1))
        {
            currRow--;
            SetTargetCodons();
        }
    }

    public void SetScrollTitle(string name)
    {
        string str = name.EndsWith("s") ? $"{name}' Code" : $"{name}'s Code";
        title.text = str;
    }
}
