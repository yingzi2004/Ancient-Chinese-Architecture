#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 香炉烟雾效果编辑器助手
/// 提供快速设置和预设配置
/// </summary>
public class IncenseSmokeEditor : EditorWindow
{
    private GameObject selectedIncense;
    private IncenseSmokeEffect smokeEffect;

    // 预设配置
    private enum PresetType
    {
        轻烟袅袅,    // 淡雅的香烟
        浓烟密布,    // 浓重的烟雾
        檀香,        // 檀香专用
        线香,        // 线香效果
        柱香         // 柱香效果
    }

    private PresetType currentPreset = PresetType.轻烟袅袅;

    [MenuItem("工具/香炉烟雾效果配置器")]
    public static void ShowWindow()
    {
        GetWindow<IncenseSmokeEditor>("香炉烟雾配置");
    }

    private void OnGUI()
    {
        GUILayout.Label("🌫️ 香炉烟雾效果配置器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 选择香炉对象
        EditorGUILayout.BeginHorizontal();
        if (GUILayout.Button("选择场景中的香炉", GUILayout.Height(30)))
        {
            Selection.activeObject = selectedIncense;
        }

        if (GUILayout.Button("从选择获取", GUILayout.Width(100)))
        {
            if (Selection.activeGameObject != null)
            {
                selectedIncense = Selection.activeGameObject;
                smokeEffect = selectedIncense.GetComponent<IncenseSmokeEffect>();
            }
        }
        EditorGUILayout.EndHorizontal();

        EditorGUILayout.Space();

        // 显示当前选择的对象
        if (selectedIncense != null)
        {
            EditorGUILayout.HelpBox($"当前对象: {selectedIncense.name}", MessageType.Info);
        }
        else
        {
            EditorGUILayout.HelpBox("请在Hierarchy中选择香炉对象", MessageType.Warning);
        }

        EditorGUILayout.Space();

        // 快速添加烟雾效果
        if (selectedIncense != null && smokeEffect == null)
        {
            EditorGUILayout.BeginVertical(EditorStyles.helpBox);
            GUILayout.Label("检测到该对象没有烟雾效果", EditorStyles.boldLabel);
            if (GUILayout.Button("➕ 添加烟雾效果", GUILayout.Height(35)))
            {
                AddSmokeEffect();
            }
            EditorGUILayout.EndVertical();
            EditorGUILayout.Space();
        }

        // 预设选择
        if (smokeEffect != null)
        {
            GUILayout.Label("📋 快速预设", EditorStyles.boldLabel);
            currentPreset = (PresetType)EditorGUILayout.EnumPopup("选择预设:", currentPreset);

            if (GUILayout.Button("应用预设", GUILayout.Height(30)))
            {
                ApplyPreset(currentPreset);
            }

            EditorGUILayout.Space();
            EditorGUILayout.HelpBox("预设说明:\n• 轻烟袅袅 - 淡雅的香烟，适合小香炉\n• 浓烟密布 - 浓重烟雾，适合大香炉\n• 檀香 - 檀香专用效果\n• 线香 - 线香烟雾效果\n• 柱香 - 柱香烟雾效果", MessageType.Info);

            EditorGUILayout.Space();

            // 快速调整
            GUILayout.Label("🎛️ 快速调整", EditorStyles.boldLabel);

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("增强烟雾"))
            {
                smokeEffect.SetSmokeIntensity(Mathf.Clamp01(GetCurrentIntensity() + 0.2f));
            }
            if (GUILayout.Button("减弱烟雾"))
            {
                smokeEffect.SetSmokeIntensity(Mathf.Clamp01(GetCurrentIntensity() - 0.2f));
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("播放"))
            {
                smokeEffect.PlaySmoke();
            }
            if (GUILayout.Button("停止"))
            {
                smokeEffect.StopSmoke();
            }
            if (GUILayout.Button("暂停"))
            {
                smokeEffect.PauseSmoke();
            }
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 颜色预设
            GUILayout.Label("🎨 烟雾颜色预设", EditorStyles.boldLabel);
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("白烟")) smokeEffect.SetSmokeColor(new Color(0.8f, 0.8f, 0.8f, 0.3f));
            if (GUILayout.Button("青烟")) smokeEffect.SetSmokeColor(new Color(0.6f, 0.7f, 0.8f, 0.3f));
            if (GUILayout.Button("黄烟")) smokeEffect.SetSmokeColor(new Color(0.9f, 0.8f, 0.6f, 0.3f));
            EditorGUILayout.EndHorizontal();

            EditorGUILayout.Space();

            // 风力控制
            GUILayout.Label("💨 风力控制", EditorStyles.boldLabel);
            bool windEnabled = EditorGUILayout.Toggle("启用风力", smokeEffect.enableWind);
            if (windEnabled != smokeEffect.enableWind)
            {
                smokeEffect.SetWindEnabled(windEnabled);
            }
        }

        EditorGUILayout.Space();

        // 批量添加
        GUILayout.Label("🔧 批量操作", EditorStyles.boldLabel);
        if (GUILayout.Button("为所有选中对象添加烟雾效果"))
        {
            foreach (var obj in Selection.gameObjects)
            {
                if (obj.GetComponent<IncenseSmokeEffect>() == null)
                {
                    obj.AddComponent<IncenseSmokeEffect>();
                    Debug.Log($"[批量添加] 已为 {obj.name} 添加烟雾效果");
                }
            }
        }

        EditorGUILayout.Space();
        EditorGUILayout.HelpBox("提示:\n1. 选中香炉对象后点击'从选择获取'\n2. 选择合适的预设点击'应用预设'\n3. 可以通过快速调整按钮微调效果", MessageType.Info);
    }

    private void AddSmokeEffect()
    {
        if (selectedIncense != null)
        {
            // 添加粒子系统
            var ps = selectedIncense.GetComponent<ParticleSystem>();
            if (ps == null)
            {
                ps = selectedIncense.AddComponent<ParticleSystem>();
            }

            // 添加烟雾脚本
            smokeEffect = selectedIncense.GetComponent<IncenseSmokeEffect>();
            if (smokeEffect == null)
            {
                smokeEffect = selectedIncense.AddComponent<IncenseSmokeEffect>();
            }

            Debug.Log($"[添加成功] 已为 {selectedIncense.name} 添加烟雾效果");

            // 刷新选择
            EditorUtility.SetDirty(selectedIncense);
        }
    }

    private void ApplyPreset(PresetType preset)
    {
        if (smokeEffect == null) return;

        switch (preset)
        {
            case PresetType.轻烟袅袅:
                smokeEffect.smokeColor = new Color(0.8f, 0.8f, 0.8f, 0.3f);
                smokeEffect.maxParticles = 150;
                smokeEffect.riseSpeed = 0.2f; // 非常慢
                smokeEffect.spreadRange = 0.3f;
                smokeEffect.minSize = 0.3f;
                smokeEffect.maxSize = 1.5f;
                smokeEffect.lifetime = 10f; // 较长生命周期
                smokeEffect.emissionRate = 25;
                smokeEffect.enableWind = false;
                break;

            case PresetType.浓烟密布:
                smokeEffect.smokeColor = new Color(0.7f, 0.7f, 0.7f, 0.4f);
                smokeEffect.maxParticles = 400;
                smokeEffect.riseSpeed = 0.5f; // 慢速
                smokeEffect.spreadRange = 0.8f;
                smokeEffect.minSize = 0.5f;
                smokeEffect.maxSize = 2.5f;
                smokeEffect.lifetime = 12f; // 长生命周期
                smokeEffect.emissionRate = 60;
                smokeEffect.enableWind = false;
                break;

            case PresetType.檀香:
                smokeEffect.smokeColor = new Color(0.9f, 0.85f, 0.7f, 0.35f); // 微黄色
                smokeEffect.maxParticles = 200;
                smokeEffect.riseSpeed = 0.3f; // 缓慢上升
                smokeEffect.spreadRange = 0.4f;
                smokeEffect.minSize = 0.3f;
                smokeEffect.maxSize = 1.8f;
                smokeEffect.lifetime = 10f;
                smokeEffect.emissionRate = 30;
                smokeEffect.enableWind = false;
                break;

            case PresetType.线香:
                smokeEffect.smokeColor = new Color(0.75f, 0.75f, 0.75f, 0.3f);
                smokeEffect.maxParticles = 100;
                smokeEffect.riseSpeed = 0.15f; // 极慢
                smokeEffect.spreadRange = 0.2f;
                smokeEffect.minSize = 0.15f;
                smokeEffect.maxSize = 1.0f;
                smokeEffect.lifetime = 12f; // 很长生命周期
                smokeEffect.emissionRate = 15;
                smokeEffect.enableWind = false;
                break;

            case PresetType.柱香:
                smokeEffect.smokeColor = new Color(0.8f, 0.8f, 0.75f, 0.35f);
                smokeEffect.maxParticles = 300;
                smokeEffect.riseSpeed = 0.4f; // 慢速
                smokeEffect.spreadRange = 0.6f;
                smokeEffect.minSize = 0.4f;
                smokeEffect.maxSize = 2.2f;
                smokeEffect.lifetime = 10f;
                smokeEffect.emissionRate = 50;
                smokeEffect.enableWind = false;
                break;
        }

        // 重新配置粒子系统
        smokeEffect.SendMessage("ConfigureParticleSystem");

        // 标记为已修改
        EditorUtility.SetDirty(smokeEffect);

        Debug.Log($"[应用预设] 已应用 '{preset}' 预设到 {selectedIncense.name}");
    }

    private float GetCurrentIntensity()
    {
        // 估算当前强度
        if (smokeEffect != null)
        {
            return (float)smokeEffect.emissionRate / 30f; // 30是默认值
        }
        return 0.5f;
    }

    private void OnSelectionChange()
    {
        if (Selection.activeGameObject != null)
        {
            var smoke = Selection.activeGameObject.GetComponent<IncenseSmokeEffect>();
            if (smoke != null)
            {
                selectedIncense = Selection.activeGameObject;
                smokeEffect = smoke;
                Repaint();
            }
        }
    }
}
#endif
