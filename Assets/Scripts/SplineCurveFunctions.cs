using UnityEngine;

public class SplineCurveFunctions
{
    // Transforming spline curve into a texture that can be sampled for values.
    public static Texture2D ArrayToTexture(float[] curveArray, int resolution = 256)
    {
        var curveTexture = new Texture2D(resolution, 1, TextureFormat.RFloat, false, true);
        for (int i = 0; i < resolution; i++)
        {
            // float time = i / (float)(resolution - 1);
            // float value = curve.Evaluate(time);
            curveTexture.SetPixel(i, 0, new Color(curveArray[i], 0, 0, 0));
        }
        curveTexture.Apply();
        return curveTexture;
    }
    
    public static float[] CurveToArray(AnimationCurve curve, int resolution = 256)
    {
        float[] curveArray = new float[resolution];
        for (int i = 0; i < resolution; i++) 
        {
            float time = i / (float)(resolution - 1);
            float value = curve.Evaluate(time);
            curveArray[i] = value;
        }
        return curveArray;
    }
}
