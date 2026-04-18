using UnityEngine;
using UnityEngine.SceneManagement;

public class ShadowSoftener : MonoBehaviour {

    void OnEnable() {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    void OnDisable() {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode) {
        ApplyLightingFix();
    }

    void Start() {
        ApplyLightingFix();
    }

    void ApplyLightingFix() {
        Light[] lights = FindObjectsOfType<Light>();
        foreach(var l in lights) {
            if (l.type == LightType.Directional) {
                l.shadowStrength = 0.2f; // 降低阴影强度
                l.shadows = LightShadows.Soft; // 强制使用软阴影
            }
        }
        
        // 当开启天气系统时，不应锁死高亮的全局环境光，这会导致夜间过亮。
        // 若完全不应用，则删除或注释以下两行即可。
        // 如果仍想单独对夜间压暗，可以引入对UniStormSystem的时间判断，但通常建议交由天气系统自身处理环境光渐变。
        // RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.8f); // 原本这行锁死了高亮
        // DynamicGI.UpdateEnvironment();
    }
}