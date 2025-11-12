using UnityEngine;
using System.Collections.Generic;

public static class JsonUtilityWrapper
{
    [System.Serializable]
    private class Wrapper<T>
    {
        public List<T> Items;
    }

    public static List<T> FromJsonArray<T>(string json)
    {
        string newJson = "{ \"Items\": " + json + "}";
        return JsonUtility.FromJson<Wrapper<T>>(newJson).Items;
    }
}