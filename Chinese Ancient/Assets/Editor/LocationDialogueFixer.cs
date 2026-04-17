using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 修复导航对话触发器工具
/// </summary>
public class LocationDialogueFixer : EditorWindow
{
    [MenuItem("工具/修复导航对话触发器")]
    public static void ShowWindow()
    {
        GetWindow<LocationDialogueFixer>("修复导航对话");
    }

    private void OnGUI()
    {
        GUILayout.Label("导航对话触发器修复工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "问题：导航对话进入后自动出现，无法消失\n\n" +
            "原因：\n" +
            "1. autoHideOnExit 可能设置为 false\n" +
            "2. 对话需要按L键手动关闭\n" +
            "3. 没有检测玩家离开",
            MessageType.Warning);

        GUILayout.Space(10);

        // 修复选项
        GUILayout.Label("修复选项", EditorStyles.boldLabel);

        bool autoHide = EditorGUILayout.Toggle("启用离开自动关闭", true);
        float hideDelay = EditorGUILayout.Slider("关闭延迟（秒）", 1f, 0f, 5f);
        float exitOffset = EditorGUILayout.Slider("离开距离偏移", 2f, 0.5f, 10f);

        GUILayout.Space(10);

        if (GUILayout.Button("修复所有LocationDialogueTrigger", GUILayout.Height(40)))
        {
            FixAllTriggers(autoHide, hideDelay, exitOffset);
        }

        GUILayout.Space(5);

        if (GUILayout.Button("替换为改进版触发器", GUILayout.Height(40)))
        {
            ReplaceWithImprovedVersion();
        }

        GUILayout.Space(10);

        // 显示当前场景中的触发器信息
        ShowCurrentTriggersInfo();
    }

    private void FixAllTriggers(bool autoHide, float hideDelay, float exitOffset)
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
            var autoHideField = typeof(LocationDialogueTrigger).GetField("autoHideOnExit",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var hideDelayField = typeof(LocationDialogueTrigger).GetField("autoHideDelay",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            var exitOffsetField = typeof(LocationDialogueTrigger).GetField("exitDistanceOffset",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (autoHideField != null)
            {
                autoHideField.SetValue(trigger, autoHide);
            }

            if (hideDelayField != null)
            {
                hideDelayField.SetValue(trigger, hideDelay);
            }

            if (exitOffsetField != null)
            {
                // 原来的脚本可能没有这个字段，忽略
            }

            fixedCount++;
            Debug.Log($"<color=green>✓ 已修复: {trigger.name}</color>");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("修复完成",
            $"已修复 {fixedCount} 个触发器！\n\n" +
            $"设置：\n" +
            $"- 离开自动关闭: {autoHide}\n" +
            $"- 关闭延迟: {hideDelay}秒\n\n" +
            $"请保存场景（Ctrl+S）后重新运行。",
            "确定");

        Debug.Log("<color=cyan>========== 修复完成 ==========</color>");
    }

    private void ReplaceWithImprovedVersion()
    {
        Debug.Log("<color=cyan>========== 替换为改进版触发器 ==========</color>");

        LocationDialogueTrigger[] oldTriggers = FindObjectsOfType<LocationDialogueTrigger>();

        if (oldTriggers == null || oldTriggers.Length == 0)
        {
            EditorUtility.DisplayDialog("提示", "场景中没有找到LocationDialogueTrigger！", "确定");
            return;
        }

        if (!EditorUtility.DisplayDialog("确认",
            $"将替换 {oldTriggers.Length} 个触发器为改进版。\n\n" +
            "改进版功能：\n" +
            "✓ 玩家离开自动关闭对话\n" +
            "✓ 基于距离检测（更可靠）\n" +
            "✓ 支持可视化调试\n\n" +
            "是否继续？",
            "确定", "取消"))
        {
            return;
        }

        int replacedCount = 0;

        foreach (LocationDialogueTrigger oldTrigger in oldTriggers)
        {
            GameObject obj = oldTrigger.gameObject;

            // 获取旧触发器的配置
            SerializedObject serializedObj = new SerializedObject(oldTrigger);

            string guideName = serializedObj.FindProperty("guideName").stringValue;
            string locationDesc = serializedObj.FindProperty("locationDescription").stringValue;
            bool triggerOnce = serializedObj.FindProperty("triggerOnce").boolValue;
            float triggerDelay = serializedObj.FindProperty("triggerDelay").floatValue;

            // 获取对话内容
            SerializedProperty dialogueLinesProp = serializedObj.FindProperty("dialogueLines");
            string[] dialogueLines = new string[dialogueLinesProp.arraySize];
            for (int i = 0; i < dialogueLinesProp.arraySize; i++)
            {
                dialogueLines[i] = dialogueLinesProp.GetArrayElementAtIndex(i).stringValue;
            }

            // 移除旧组件
            DestroyImmediate(oldTrigger);

            // 添加新组件
            LocationDialogueTrigger_Improved newTrigger = obj.AddComponent<LocationDialogueTrigger_Improved>();

            // 复制配置
            var newSerializedObj = new SerializedObject(newTrigger);
            newSerializedObj.FindProperty("guideName").stringValue = guideName;
            newSerializedObj.FindProperty("locationDescription").stringValue = locationDesc;
            newSerializedObj.FindProperty("triggerOnce").boolValue = triggerOnce;
            newSerializedObj.FindProperty("triggerDelay").floatValue = triggerDelay;

            // 设置对话内容
            newSerializedObj.FindProperty("dialogueLines").arraySize = dialogueLines.Length;
            SerializedProperty newDialogueLinesProp = newSerializedObj.FindProperty("dialogueLines");
            for (int i = 0; i < dialogueLines.Length; i++)
            {
                newDialogueLinesProp.GetArrayElementAtIndex(i).stringValue = dialogueLines[i];
            }

            newSerializedObj.ApplyModifiedProperties();

            replacedCount++;
            Debug.Log($"<color=green>✓ 已替换: {obj.name}</color>");
        }

        EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

        EditorUtility.DisplayDialog("替换完成",
            $"已替换 {replacedCount} 个触发器！\n\n" +
            "改进版默认设置：\n" +
            "✓ 离开自动关闭: 开启\n" +
            "✓ 关闭延迟: 1秒\n" +
            "✓ 离开距离偏移: 2米\n\n" +
            "请保存场景（Ctrl+S）后重新运行。",
            "确定");

        Debug.Log("<color=cyan>========== 替换完成 ==========</color>");
    }

    private void ShowCurrentTriggersInfo()
    {
        GUILayout.Space(10);
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

            foreach (LocationDialogueTrigger trigger in triggers)
            {
                GUILayout.BeginHorizontal();
                GUILayout.Label("•", GUILayout.Width(15));
                GUILayout.Label(trigger.name);

                // 检查autoHideOnExit设置
                var autoHideField = typeof(LocationDialogueTrigger).GetField("autoHideOnExit",
                    System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

                if (autoHideField != null)
                {
                    bool autoHide = (bool)autoHideField.GetValue(trigger);
                    if (!autoHide)
                    {
                        GUILayout.Label("<color=red>⚠️ 未启用自动关闭</color>", GUI.skin.label);
                    }
                    else
                    {
                        GUILayout.Label("<color=green>✓ 自动关闭已启用</color>", GUI.skin.label);
                    }
                }

                GUILayout.EndHorizontal();
            }
        }
    }
}
