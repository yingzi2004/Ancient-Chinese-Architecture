using UnityEngine;
using UniStorm;

public class CustomNightLighting : MonoBehaviour
{
    [Header("夜间全局设置")]
    [Tooltip("夜间主环境光压暗倍率 (越小越暗)")]
    [Range(0f, 1f)]
    public float nightAmbientMultiplier = 0.2f;

    [Tooltip("夜间月光光源强度压暗倍率 (越小越暗)")]
    [Range(0f, 1f)]
    public float moonLightingMultiplier = 0.5f;

    [Tooltip("开启以强制重写夜间天空盒/环境光颜色")]
    public bool overrideNightColor = false;
    public Color customNightAmbientColor = new Color(0.1f, 0.12f, 0.15f);

    void LateUpdate()
    {
        if (UniStormSystem.Instance == null) return;

        // 如果获取到时间处于夜间（UniStorm 默认夜间为晚上19点到早上6点前后）
        if (UniStormSystem.Instance.CurrentTimeOfDay == UniStormSystem.CurrentTimeOfDayEnum.Night)
        {
            // 因为 UniStorm 会在自己的 Update() 里覆盖 RenderSettings 的值，
            // 所以我们需要在 LateUpdate() 里再次对它进行“二次压暗”修正。

            if (overrideNightColor)
            {
                // 直接强制使用纯色扁平光照，忽略 UniStorm 的环境天光
                RenderSettings.ambientMode = UnityEngine.Rendering.AmbientMode.Flat;
                RenderSettings.ambientLight = customNightAmbientColor;
                RenderSettings.ambientIntensity = nightAmbientMultiplier;
            }
            else
            {
                // 否则直接衰减 UniStorm 配置的高亮
                RenderSettings.ambientIntensity *= nightAmbientMultiplier;
                RenderSettings.ambientSkyColor *= nightAmbientMultiplier;
                RenderSettings.ambientEquatorColor *= nightAmbientMultiplier;
                RenderSettings.ambientGroundColor *= nightAmbientMultiplier;
            }

            // 对月亮（主定向光）亮度进行压暗
            if (UniStormSystem.Instance.m_MoonLight != null)
            {
                UniStormSystem.Instance.m_MoonLight.intensity *= moonLightingMultiplier;
            }
        }
    }
}
