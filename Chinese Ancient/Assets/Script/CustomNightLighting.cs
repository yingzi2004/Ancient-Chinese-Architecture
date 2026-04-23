using UnityEngine;
using UniStorm;
public class CustomNightLighting : MonoBehaviour
{
    [Header("夜间全局设置")]
    [Range(0f, 1f)]
    public float nightAmbientMultiplier = 0.2f;
    [Range(0f, 1f)]
    public float moonLightingMultiplier = 0.5f;
    public bool overrideNightColor = false;
    public Color customNightAmbientColor = new Color(0.1f, 0.12f, 0.15f);
    void LateUpdate()
    {
        if (UniStormSystem.Instance == null) return;
        // 如果获取到时间处于夜间（UniStorm 默认夜间为晚上19点到早上6点前后）
        if (UniStormSystem.Instance.CurrentTimeOfDay == UniStormSystem.CurrentTimeOfDayEnum.Night)
        {
            if (overrideNightColor)
            {
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = customNightAmbientColor;
                RenderSettings.ambientIntensity = nightAmbientMultiplier;
            }
            else
            {
                RenderSettings.ambientIntensity *= nightAmbientMultiplier;
                RenderSettings.ambientSkyColor *= nightAmbientMultiplier;
                RenderSettings.ambientEquatorColor *= nightAmbientMultiplier;
                RenderSettings.ambientGroundColor *= nightAmbientMultiplier;
            }
            if (UniStormSystem.Instance.m_MoonLight != null)
            {
                UniStormSystem.Instance.m_MoonLight.intensity *= moonLightingMultiplier;
            }
        }
    }
}
