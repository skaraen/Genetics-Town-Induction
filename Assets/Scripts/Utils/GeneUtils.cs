using System;
using System.Collections.Generic;
using UnityEngine;

public static class GeneUtils
{
    static readonly Dictionary<char, string> hexMap = new Dictionary<char, string>()
    {
        { 'A', "#ff914d" },
        { 'C', "#0197b2" },
        { 'G', "#00bf63" },
        { 'T', "#b4b4b4" }
    };

    public static string GetHexCode(char c)
    {
        if (hexMap.TryGetValue(c, out string hexCode))
            return hexCode;
        else
            return "#FFFFFF";
    }
}