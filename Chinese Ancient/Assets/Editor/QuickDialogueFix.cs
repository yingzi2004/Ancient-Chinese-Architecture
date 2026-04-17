using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.Linq;

/// <summary>
/// 快速修复导航对话问题工具
/// </summary>
public class QuickDialogueFix : EditorWindow
{
    [MenuItem("工具/快速修复导航对话")]
    public static void ShowWindow()
    {
        GetWindow<QuickDialogueFix>("快速修复导航对话");
    }

    private float initializationDelay = 2f;
    private bool autoHideOnExit = true;
    private float autoHideDelay = 1f;
    private bool triggerOnce = false;

    private void OnGUI()
    {
        GUILayout.Label("快速修复导航对话", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "问题：刚进入场景就有对话弹出\n\n" +
            "解决：添加初始化延迟，让玩家有时间移动",
            MessageType.Warning);

        GUILayout.Space(10);

        GUILayout.Label("修复设置", EditorStyles.boldLabel);

        initializationDelay = EditorGUILayout.Slider("初始化延迟（秒）", initializationDelay, 0f, 10f);
        EditorGUILayout.HelpBox("场景开始后多久开始检测玩家（避免出生点触发）", MessageType.None);

        GUILayout.Space(5);

        autoHideOnExit = EditorGUILayout.Toggle("启用离开自动关闭", autoHideOnExit);
        EditorGUILayout.HelpBox("玩家离开后自动关闭对话", MessageType.None);

        if (autoHideOnExit)
        {
            EditorGUI.indentLevel++;
            autoHideDelay = EditorGUILayout.Slider("关闭延迟（秒）", autoHideDelay, 0.1f, 5f);
            EditorGUI.indentLevel--;
        }

        GUILayout.Space(5);

        triggerOnce = EditorGUILayout.Toggle("只触发一次", triggerOnce);
        EditorGUILayout.HelpBox("关闭后可以多次触发对话", MessageType.None);

        GUILayout.Space(15);

        // 显示当前场景中的触发器
        ShowCurrentTriggers();

        GUILayout.Space(10);

        // 修复按钮
        using (new EditorGUILayout.HorizontalScope())
        {
            if (GUILayout.Button("应用到所有触发器", GUILayout.Height(35)))
            {
                ApplyFix();
            }

            if (GUILayout.Button("重置为默认值", GUILayout.Height(35)))
            {
                ResetToDefaults();
            }
        }

        GUILayout.Space(10);

        // 测试按钮
        if (GUILayout.Button("测试当前设置（打印配置）", GUILayout.Height(30)))
        {
            TestCurrentSettings();
        }
    }

    private void ShowCurrentTriggers()
    {
        GUILayout.Label("当前场景中的触发器", EditorStyles.boldLabel);

        LocationDialogueTrigger[] triggers = FindObjectsOfType<LocationDialogueTrigger>();

        if (triggers == null || triggers.Length == 0)
        {
            EditorGUILayout.HelpBox("场景中没有LocationDialogueTrigger", MessageType.Info);
            return;
        }

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label($"找到 {triggers.Length} 个触发器:");

            foreach (LocationDialogueTrigger trigger in triggers.OrderBy(t => t.name))
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("•", GUILayout.Width(15));
                GUILayout.Label(trigger.name, GUILayout.Width(200));

                // 显示当前状态
                var initDelayField = typeof(LocationDialogueTrigger).GetField("initializationDelay",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                var autoHideField = typeof(LocationDialogueTrigger).GetField("autoHideOnExit",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (initDelayField != null)
                {
                    float delay = (float)initDelayField.GetValue(trigger);
                    if (delay <= 0)
                    {
                        GUILayout.Label("<color=red>⚠️ 无初始化延迟</color>", GUI.skin.label);
                    }
                    else
                    {
                        GUILayout.Label($"<color=green>✓ 延迟 {delay}秒</color>", GUI.skin.label);
                    }
                }

                if (autoHideField != null)
                {
                    bool autoHide = (bool)autoHideField.GetValue(trigger);
                    if (!autoHide)
                    {
                        GUILayout.Label("<color=yellow>⚠️ 不会自动关闭</color>", GUI.skin.label);
                    }
                }

                GUILayout.EndHorizontal();
            }
        }
    }

    private void ApplyFix()
    {
        Debug.Log("<color=cyan>========== 开始修复导航对话触发器 ==========</color>");

        LocationDialogueTrigger[] triggers = FindObjectsOfType<LocationDialogueTrigger>();

        if (triggers == null || triggers.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "场景中没有找到LocationDialogueTrigger！", "确定");
            return;
        }

        int fixedCount = 0;

        foreach (LocationDialogueTrigger trigger in triggers)
        {
            // 使用反射修改私有字段
            var initDelayField = typeof(LocationDialogueTrigger).GetField("initializationDelay",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var autoHideField = typeof(LocationDialogueTrigger).GetField("autoHideOnExit",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var hideDelayField = typeof(LocationDialogueTrigger).GetField("autoHideDelay",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var triggerOnceField = typeof(LocationDialogueTrigger).GetField("triggerOnce",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            // 使用SerializedObject来修改值（会保存到场景）
            SerializedObject serializedObject = new SerializedObject(trigger);

            if (initDelayField != null)
            {
                var property = serializedObject.FindProperty("initializationDelay");
                if (property != null)
                {
                    property.floatValue = initializationDelay;
                }
                else
                {
                    initDelayField.SetValue(trigger, initializationDelay);
                }
            }

            if (autoHideField != null)
            {
                var property = serializedObject.FindProperty("autoHideOnExit");
                if (property != null)
                {
                    property.boolValue = autoHideOnExit;
                }
                else
                {
                    autoHideField.SetValue(trigger, autoHideOnExit);
                }
            }

            if (hideDelayField != null)
            {
                var property = serializedObject.FindProperty("autoHideDelay");
                if (property != null)
                {
                    property.floatValue = autoHideDelay;
                }
                else
                {
                    hideDelayField.SetValue(trigger, autoHideDelay);
                }
            }

            if (triggerOnceField != null)
            {
                var property = serializedObject.FindProperty("triggerOnce");
                if (property != null)
                {
                    property.boolValue = triggerOnce;
                }
                else
                {
                    triggerOnceField.SetValue(trigger, triggerOnce);
                }
            }

            serializedObject.ApplyModifiedProperties();

            fixedCount++;
            Debug.Log($"<color=green>✓ 已修复: {trigger.name}</color>");
            Debug.Log($"  - 初始化延迟: {initializationDelay}秒");
            Debug.Log($"  - 自动关闭: {autoHideOnExit}");
            Debug.Log($"  - 关闭延迟: {autoHideDelay}秒");
            Debug.Log($"  - 只触发一次: {triggerOnce}");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("修复完成",
            $"已修复 {fixedCount} 个触发器！\n\n" +
            $"应用设置：\n" +
            $"• 初始化延迟: {initializationDelay}秒\n" +
            $"• 离开自动关闭: {(autoHideOnExit ? "开启" : "关闭")}\n" +
            $"• 关闭延迟: {autoHideDelay}秒\n" +
            $"• 只触发一次: {(triggerOnce ? "是" : "否")}\n\n" +
            $"请保存场景（Ctrl+S）后重新运行。",
            "确定");

        Debug.Log("<color=cyan>========== 修复完成 ==========</color>");
    }

    private void ResetToDefaults()
    {
        initializationDelay = 2f;
        autoHideOnExit = true;
        autoHideDelay = 1f;
        triggerOnce = false;

        Debug.Log("<color=cyan>已重置为默认值</color>");
    }

    private void TestCurrentSettings()
    {
        Debug.Log("<color=cyan>========== 当前设置 ==========</color>");
        Debug.Log($"初始化延迟: {initializationDelay}秒");
        Debug.Log($"离开自动关闭: {autoHideOnExit}");
        Debug.Log($"关闭延迟: {autoHideDelay}秒");
        Debug.Log($"只触发一次: {triggerOnce}");
        Debug.Log("<color=cyan>==============================</color>");
    }
}
