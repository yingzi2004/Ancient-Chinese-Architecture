using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 对话系统诊断工具 - 检查UI配置
/// </summary>
public class DialogueDiagnostic : EditorWindow
{
    [MenuItem("工具/对话系统诊断")]
    public static void ShowWindow()
    {
        GetWindow<DialogueDiagnostic>("对话系统诊断");
    }

    private void OnGUI()
    {
        GUILayout.Label("对话系统诊断工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        if (GUILayout.Button("诊断对话系统", GUILayout.Height(40)))
        {
            DiagnoseDialogueSystem();
        }

        GUILayout.Space(10);

        if (GUILayout.Button("自动修复文字显示问题"))
        {
            AutoFixTextIssues();
        }
    }

    private void DiagnoseDialogueSystem()
    {
        Debug.Log("<color=cyan>========== 开始诊断对话系统 ==========</color>");

        // 1. 查找DialogueManager
        DialogueManager manager = FindObjectOfType<DialogueManager>();
        if (manager == null)
        {
            Debug.LogError("<color=red>❌ 未找到DialogueManager！</color>");
            return;
        }
        Debug.Log("<color=green>✓ 找到DialogueManager</color>");

        // 2. 检查DialoguePanel
        if (manager.dialoguePanel == null)
        {
            Debug.LogError("<color=red>❌ dialoguePanel 为空！</color>");
        }
        else
        {
            Debug.Log("<color=green>✓ dialoguePanel 已连接</color>");

            // 检查panel下的子对象
            Debug.Log($"<color=cyan>DialoguePanel 包含 {manager.dialoguePanel.transform.childCount} 个子对象：</color>");
            foreach (Transform child in manager.dialoguePanel.transform)
            {
                Debug.Log($"  - {child.name} (Active: {child.gameObject.activeSelf})");

                // 检查是否有Text组件
                Text textComponent = child.GetComponent<Text>();
                if (textComponent != null)
                {
                    Debug.Log($"    ✓ 有Text组件，文字: '{textComponent.text}'");
                    Debug.Log($"    - 字体: {(textComponent.font != null ? textComponent.font.name : "NULL")}");
                    Debug.Log($"    - 字号: {textComponent.fontSize}");
                    Debug.Log($"    - 颜色: {textComponent.color}");
                    Debug.Log($"    - 位置: {textComponent.rectTransform.anchoredPosition}");
                    Debug.Log($"    - 大小: {textComponent.rectTransform.sizeDelta}");
                }
            }
        }

        // 3. 检查npcNameText
        if (manager.npcNameText == null)
        {
            Debug.LogError("<color=red>❌ npcNameText 为空！</color>");
        }
        else
        {
            Debug.Log("<color=green>✓ npcNameText 已连接</color>");
            Debug.Log($"  - 当前文字: '{manager.npcNameText.text}'");
        }

        // 4. 检查dialogueText
        if (manager.dialogueText == null)
        {
            Debug.LogError("<color=red>❌ dialogueText 为空！</color>");
        }
        else
        {
            Debug.Log("<color=green>✓ dialogueText 已连接</color>");
            Debug.Log($"  - 当前文字: '{manager.dialogueText.text}'");
        }

        // 5. 检查optionsContainer
        if (manager.optionsContainer == null)
        {
            Debug.LogError("<color=red>❌ optionsContainer 为空！</color>");
        }
        else
        {
            Debug.Log("<color=green>✓ optionsContainer 已连接</color>");
        }

        // 6. 检查optionButtonPrefab
        if (manager.optionButtonPrefab == null)
        {
            Debug.LogWarning("<color=yellow>⚠️ optionButtonPrefab 为空（将使用Resources.Load加载）</color>");
        }
        else
        {
            Debug.Log("<color=green>✓ optionButtonPrefab 已连接</color>");
        }

        Debug.Log("<color=cyan>========== 诊断完成 ==========</color>");
    }

    private void AutoFixTextIssues()
    {
        Debug.Log("<color=cyan>========== 开始自动修复文字显示问题 ==========</color>");

        DialogueManager manager = FindObjectOfType<DialogueManager>();
        if (manager == null)
        {
            Debug.LogError("<color=red>❌ 未找到DialogueManager！</color>");
            EditorUtility.DisplayDialog("错误", "未找到DialogueManager！", "确定");
            return;
        }

        bool fixedAny = false;

        // 修复 npcNameText
        if (manager.npcNameText == null && manager.dialoguePanel != null)
        {
            Transform npcNameTransform = manager.dialoguePanel.transform.Find("NPCName");
            if (npcNameTransform != null)
            {
                Text text = npcNameTransform.GetComponent<Text>();
                if (text != null)
                {
                    // 使用SerializedObject来设置引用
                    SerializedObject serializedObject = new SerializedObject(manager);
                    SerializedProperty property = serializedObject.FindProperty("npcNameText");
                    property.objectReferenceValue = text;
                    serializedObject.ApplyModifiedProperties();

                    Debug.Log("<color=green>✓ 已修复 npcNameText</color>");
                    fixedAny = true;
                }
            }
        }

        // 修复 dialogueText
        if (manager.dialogueText == null && manager.dialoguePanel != null)
        {
            Transform dialogueTextTransform = manager.dialoguePanel.transform.Find("DialogueText");
            if (dialogueTextTransform != null)
            {
                Text text = dialogueTextTransform.GetComponent<Text>();
                if (text != null)
                {
                    // 使用SerializedObject来设置引用
                    SerializedObject serializedObject = new SerializedObject(manager);
                    SerializedProperty property = serializedObject.FindProperty("dialogueText");
                    property.objectReferenceValue = text;
                    serializedObject.ApplyModifiedProperties();

                    Debug.Log("<color=green>✓ 已修复 dialogueText</color>");
                    fixedAny = true;
                }
            }
        }

        // 修复 optionsContainer
        if (manager.optionsContainer == null && manager.dialoguePanel != null)
        {
            Transform optionsTransform = manager.dialoguePanel.transform.Find("OptionsContainer");
            if (optionsTransform != null)
            {
                // 使用SerializedObject来设置引用
                SerializedObject serializedObject = new SerializedObject(manager);
                SerializedProperty property = serializedObject.FindProperty("optionsContainer");
                property.objectReferenceValue = optionsTransform;
                serializedObject.ApplyModifiedProperties();

                Debug.Log("<color=green>✓ 已修复 optionsContainer</color>");
                fixedAny = true;
            }
        }

        // 标记场景已修改
        if (fixedAny)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>========== 修复完成！请保存场景 ==========</color>");
            EditorUtility.DisplayDialog("完成", "已自动修复文字显示问题！\n请保存场景（Ctrl+S）后重新运行。", "确定");
        }
        else
        {
            Debug.Log("<color=yellow>========== 没有发现需要修复的问题 ==========</color>");
            EditorUtility.DisplayDialog("提示", "没有发现需要修复的问题。\n\n如果文字仍然不显示，请检查：\n1. Text组件的字体是否正确\n2. Text组件是否被其他UI遮挡\n3. 文字颜色是否与背景色相同", "确定");
        }
    }
}
