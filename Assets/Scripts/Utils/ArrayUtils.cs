using System;
using UnityEngine;

public static class ArrayUtils
{
    private static System.Random rng = new System.Random();

    public static void ShuffleRows<T>(T[][] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T[] temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }

    public static void ShuffleArray<T>(T[] array)
    {
        for (int i = array.Length - 1; i > 0; i--)
        {
            int j = rng.Next(i + 1);
            T temp = array[i];
            array[i] = array[j];
            array[j] = temp;
        }
    }
}