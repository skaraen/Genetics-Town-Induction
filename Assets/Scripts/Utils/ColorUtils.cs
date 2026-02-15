using System;
using UnityEngine;

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

}