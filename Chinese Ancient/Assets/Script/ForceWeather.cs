using UnityEngine;
using UniStorm;
using System.Collections;
public class ForceWeather : MonoBehaviour
{
    [Header("雾气设置 (强制开启全局雾气)")]
    public bool forceCustomFog = true;
    [Range(0f, 0.12f)]
    public float fogDensity = 0.015f;
    public Color fogColor = new Color(0.6f, 0.6f, 0.6f, 1f);
    private void Update()
    {
        // 强制覆盖 UniStorm 的雾气设置，营造烟雾朦胧的江南效果，不下雨
        if (forceCustomFog)
        {
            RenderSettings.fog = true;
            RenderSettings.fogMode = FogMode.ExponentialSquared;
            RenderSettings.fogDensity = fogDensity;
            RenderSettings.fogColor = fogColor;
        }
    }
}
