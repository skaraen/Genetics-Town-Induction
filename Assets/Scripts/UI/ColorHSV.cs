using UnityEngine;

public class ColorHSV
{
    public float h;
    public float s;
    public float v;

    public ColorHSV()
    {
        h = 0.0f;
        s = 0.0f;
        v = 0.0f;
    }

    public ColorHSV(float h, float s, float v)
    {
        this.h = h;
        this.s = s;
        this.v = v;
    }
}