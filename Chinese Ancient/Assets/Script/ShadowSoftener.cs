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
        RenderSettings.ambientLight = new Color(0.8f, 0.8f, 0.8f); // 提亮全局环境光，减少死黑
        DynamicGI.UpdateEnvironment(); // 强制刷新环境光照（非常重要，修复传送后变暗的问题）
    }
}