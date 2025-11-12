using System;
using Unity.Mathematics;
using UnityEngine;
using System.Globalization;

public static class ColorUtils
{
    private static System.Random rng = new System.Random();

    public static ColorHSV GetHSVFromRGB(Color color)
    {
        float h, s, v;
        Color.RGBToHSV(color, out h, out s, out v);
        return new ColorHSV(h, s, v);
    }

    public static Color GetRGBFromHSV(ColorHSV hsv)
    {
        return Color.HSVToRGB(hsv.h, hsv.s, hsv.v, true);
    }

    public static ColorHSV GetHSVFromHex(string hex)
    {
        if (hex.StartsWith("#"))
            hex = hex.Substring(1);

        if (hex.Length != 6)
            throw new ArgumentException("Hex string must be 6 characters long (RRGGBB)");

        int val = int.Parse(hex, NumberStyles.HexNumber);
        byte r = (byte)((val >> 16) & 0xFF);
        byte g = (byte)((val >> 8) & 0xFF);
        byte b = (byte)(val & 0xFF);
        return GetHSVFromRGB(new Color32(r, g, b, 255));
    }
}