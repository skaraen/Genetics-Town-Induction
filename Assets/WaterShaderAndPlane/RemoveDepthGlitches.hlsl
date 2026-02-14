void RemoveDepthGlitches_float(float depthValue, out float result)
{
    result = depthValue < 0 ? 1 : depthValue;
}