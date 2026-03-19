using UnityEngine;

public class ShadowSoftener : MonoBehaviour {
    void Start() {
        Light[] lights = FindObjectsOfType<Light>();
        foreach(var l in lights) {
            if (l.type == LightType.Directional) {
                l.shadowStrength = 0.4f; // 降低阴影强度
            }
        }
        RenderSettings.ambientLight = new Color(0.6f, 0.6f, 0.6f); // 提亮全局环境光，减少死黑
    }
}