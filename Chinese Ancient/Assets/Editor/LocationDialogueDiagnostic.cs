using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;

/// <summary>
/// 导航对话触发器诊断工具
/// 用于诊断LocationDialogueTrigger和GuideDialogueUI的配置问题
/// </summary>
public class LocationDialogueDiagnostic : EditorWindow
{
    [MenuItem("工具/导航对话诊断")]
    public static void ShowWindow()
    {
        GetWindow<LocationDialogueDiagnostic>("导航对话诊断");
    }

    private Vector2 scrollPosition;

    private void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("导航对话系统诊断工具", EditorStyles.boldLabel);
        GUILayout.Space(10);

        EditorGUILayout.HelpBox(
            "如果进入触发区域后对话框没有出现，请使用下方工具进行诊断。\n\n" +
            "常见问题：\n" +
            "1. GuideDialogueUI未正确配置\n" +
            "2. 玩家Tag不是'Player'\n" +
            "3. 触发器Collider未勾选Is Trigger\n" +
            "4. 对话内容为空",
            MessageType.Info);

        GUILayout.Space(10);

        // 诊断按钮
        if (GUILayout.Button("开始诊断", GUILayout.Height(35)))
        {
            DiagnoseLocationDialogue();
        }

        GUILayout.Space(5);

        // 自动修复按钮
        if (GUILayout.Button("尝试自动修复常见问题", GUILayout.Height(35)))
        {
            AutoFixCommonIssues();
        }

        GUILayout.Space(5);

        // 测试按钮
        if (GUILayout.Button("测试对话显示（模拟触发）", GUILayout.Height(35)))
        {
            TestDialogueDisplay();
        }

        GUILayout.Space(15);

        // 显示诊断信息区域
        ShowDiagnosticInfo();

        EditorGUILayout.EndScrollView();
    }

    private void DiagnoseLocationDialogue()
    {
        Debug.Log("<color=cyan>========== 开始诊断导航对话系统 ==========</color>");

        // 1. 查找GuideDialogueUI
        GuideDialogueUI guideUI = FindObjectOfType<GuideDialogueUI>();
        if (guideUI == null)
        {
            Debug.LogError("<color=red>❌ 未找到GuideDialogueUI！场景中必须有GuideDialogueUI实例！</color>");
            EditorUtility.DisplayDialog("诊断结果", "未找到GuideDialogueUI！\n\n请确保场景中有GuideDialogueUI对象。", "确定");
            return;
        }
        Debug.Log("<color=green>✓ 找到GuideDialogueUI</color>");

        // 2. 检查GuideDialogueUI的UI组件
        CheckGuideDialogueUIComponents(guideUI);

        // 3. 查找所有LocationDialogueTrigger
        LocationDialogueTrigger[] triggers = FindObjectsOfType<LocationDialogueTrigger>();
        if (triggers == null || triggers.Length == 0)
        {
            Debug.LogWarning("<color=yellow>⚠️ 未找到任何LocationDialogueTrigger！</color>");
            EditorUtility.DisplayDialog("诊断结果", "未找到任何LocationDialogueTrigger！\n\n请在场景中添加LocationDialogueTrigger组件。", "确定");
            return;
        }

        Debug.Log($"<color=green>✓ 找到 {triggers.Length} 个LocationDialogueTrigger</color>");

        // 4. 检查每个触发器
        foreach (LocationDialogueTrigger trigger in triggers)
        {
            CheckLocationDialogueTrigger(trigger);
        }

        // 5. 检查玩家对象
        CheckPlayerObject();

        Debug.Log("<color=cyan>========== 诊断完成 ==========</color>");
        EditorUtility.DisplayDialog("诊断完成", "诊断已完成！\n\n请查看Console窗口获取详细信息。", "确定");
    }

    private void CheckGuideDialogueUIComponents(GuideDialogueUI guideUI)
    {
        Debug.Log("<color=cyan>--- 检查GuideDialogueUI组件 ---</color>");

        // 使用反射获取私有字段
        var dialoguePanelField = typeof(GuideDialogueUI).GetField("dialoguePanel",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (dialoguePanelField != null)
        {
            GameObject dialoguePanel = dialoguePanelField.GetValue(guideUI) as GameObject;
            if (dialoguePanel == null)
            {
                Debug.LogError("<color=red>❌ GuideDialogueUI的dialoguePanel为空！</color>");
            }
            else
            {
                Debug.Log("<color=green>✓ dialoguePanel已连接</color>");
            }
        }

        // 检查TextMeshPro和Legacy Text组件
        var guideNameTextTMPField = typeof(GuideDialogueUI).GetField("guideNameTextTMP",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var guideNameTextLegacyField = typeof(GuideDialogueUI).GetField("guideNameTextLegacy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool hasNameText = false;
        if (guideNameTextTMPField != null)
        {
            var tmp = guideNameTextTMPField.GetValue(guideUI);
            if (tmp != null) hasNameText = true;
        }
        if (guideNameTextLegacyField != null)
        {
            var legacy = guideNameTextLegacyField.GetValue(guideUI);
            if (legacy != null) hasNameText = true;
        }

        if (!hasNameText)
        {
            Debug.LogError("<color=red>❌ GuideDialogueUI的guideNameText为空！</color>");
        }
        else
        {
            Debug.Log("<color=green>✓ guideNameText已连接</color>");
        }

        var dialogueTextTMPField = typeof(GuideDialogueUI).GetField("dialogueTextTMP",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var dialogueTextLegacyField = typeof(GuideDialogueUI).GetField("dialogueTextLegacy",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        bool hasDialogueText = false;
        if (dialogueTextTMPField != null)
        {
            var tmp = dialogueTextTMPField.GetValue(guideUI);
            if (tmp != null) hasDialogueText = true;
        }
        if (dialogueTextLegacyField != null)
        {
            var legacy = dialogueTextLegacyField.GetValue(guideUI);
            if (legacy != null) hasDialogueText = true;
        }

        if (!hasDialogueText)
        {
            Debug.LogError("<color=red>❌ GuideDialogueUI的dialogueText为空！</color>");
        }
        else
        {
            Debug.Log("<color=green>✓ dialogueText已连接</color>");
        }
    }

    private void CheckLocationDialogueTrigger(LocationDialogueTrigger trigger)
    {
        Debug.Log($"<color=cyan>--- 检查触发器: {trigger.name} ---</color>");

        // 使用反射获取私有字段
        var dialogueLinesField = typeof(LocationDialogueTrigger).GetField("dialogueLines",
            System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

        if (dialogueLinesField != null)
        {
            string[] dialogueLines = dialogueLinesField.GetValue(trigger) as string[];
            if (dialogueLines == null || dialogueLines.Length == 0)
            {
                Debug.LogError($"<color=red>❌ {trigger.name} 的对话内容为空！</color>");
            }
            else
            {
                Debug.Log($"<color=green>✓ {trigger.name} 有 {dialogueLines.Length} 句对话</color>");
            }
        }

        // 检查Collider
        Collider col = trigger.GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"<color=red>❌ {trigger.name} 没有Collider组件！</color>");
        }
        else if (!col.isTrigger)
        {
            Debug.LogError($"<color=red>❌ {trigger.name} 的Collider未勾选Is Trigger！</color>");
        }
        else
        {
            Debug.Log($"<color=green>✓ {trigger.name} 的Trigger Collider配置正确</color>");
        }
    }

    private void CheckPlayerObject()
    {
        Debug.Log("<color=cyan>--- 检查玩家对象 ---</color>");

        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0)
        {
            Debug.LogError("<color=red>❌ 场景中没有Tag为'Player'的对象！</color>");
            Debug.LogError("<color=red>请确保玩家对象（通常是XR Origin或Player）的Tag设置为'Player'！</color>");
        }
        else
        {
            Debug.Log($"<color=green>✓ 找到 {players.Length} 个Tag为'Player'的对象</color>");
            foreach (GameObject player in players)
            {
                Debug.Log($"  - {player.name}");

                // 检查是否有Rigidbody
                Rigidbody rb = player.GetComponent<Rigidbody>();
                if (rb == null)
                {
                    Debug.LogWarning($"    ⚠️ {player.name} 没有Rigidbody，可能无法触发Trigger事件");
                }

                // 检查是否有Collider
                Collider col = player.GetComponent<Collider>();
                if (col == null)
                {
                    Debug.LogWarning($"    ⚠️ {player.name} 没有Collider，无法触发Trigger事件");
                }
            }
        }
    }

    private void AutoFixCommonIssues()
    {
        Debug.Log("<color=cyan>========== 开始自动修复 ==========</color>");

        bool fixedAny = false;

        // 1. 尝试修复GuideDialogueUI组件
        GuideDialogueUI guideUI = FindObjectOfType<GuideDialogueUI>();
        if (guideUI != null)
        {
            // 查找可能的UI组件
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                // 查找GuideDialoguePanel
                Transform panel = canvas.transform.Find("GuideDialoguePanel");
                if (panel == null) panel = canvas.transform.Find("DialoguePanel");
                if (panel == null) panel = canvas.transform.Find("GuidePanel");

                if (panel != null)
                {
                    Debug.Log($"<color=cyan>找到可能的对话面板: {panel.name}</color>");
                    // 这里可以添加自动绑定逻辑
                    // 但由于GuideDialogueUI使用Serialized属性，需要使用SerializedObject来设置
                }
            }
        }

        // 2. 检查并修复Trigger的Is Trigger设置
        LocationDialogueTrigger[] triggers = FindObjectsOfType<LocationDialogueTrigger>();
        foreach (LocationDialogueTrigger trigger in triggers)
        {
            Collider col = trigger.GetComponent<Collider>();
            if (col != null && !col.isTrigger)
            {
                col.isTrigger = true;
                Debug.Log($"<color=green>✓ 已为 {trigger.name} 的Collider勾选Is Trigger</color>");
                fixedAny = true;
            }
        }

        // 3. 检查玩家对象
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        if (players == null || players.Length == 0)
        {
            // 尝试查找可能的玩家对象
            GameObject[] possiblePlayers = GameObject.FindObjectsOfType<GameObject>();
            foreach (GameObject obj in possiblePlayers)
            {
                if (obj.name.ToLower().Contains("player") ||
                    obj.name.ToLower().Contains("xr") ||
                    obj.name.ToLower().Contains("origin") ||
                    obj.name.ToLower().Contains("camera"))
                {
                    Debug.LogWarning($"<color=yellow>发现可能的玩家对象: {obj.name}，请手动将其Tag设置为'Player'</color>");
                }
            }
        }

        // 标记场景已修改
        if (fixedAny)
        {
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());
            Debug.Log("<color=green>========== 修复完成！请保存场景 ==========</color>");
            EditorUtility.DisplayDialog("修复完成", "已自动修复部分问题！\n\n请保存场景（Ctrl+S）后重新运行。", "确定");
        }
        else
        {
            Debug.Log("<color=yellow>========== 没有发现可自动修复的问题 ==========</color>");
            EditorUtility.DisplayDialog("提示", "没有发现可自动修复的问题。\n\n请查看Console中的诊断信息，手动修复相关问题。", "确定");
        }
    }

    private void TestDialogueDisplay()
    {
        GuideDialogueUI guideUI = FindObjectOfType<GuideDialogueUI>();
        if (guideUI == null)
        {
            EditorUtility.DisplayDialog("错误", "未找到GuideDialogueUI！\n请先确保场景中有GuideDialogueUI对象。", "确定");
            return;
        }

        Debug.Log("<color=cyan>========== 测试对话显示 ==========</color>");

        // 创建测试对话内容
        string[] testDialogue = new string[]
        {
            "这是测试对话第一句。",
            "这是测试对话第二句。",
            "测试完成！"
        };

        // 在编辑器模式下，我们只能记录日志，不能真正运行
        Debug.Log("<color=yellow>请在运行模式下测试！</color>");
        Debug.Log("测试步骤：");
        Debug.Log("1. 点击Unity的Play按钮");
        Debug.Log("2. 在Hierarchy中选择一个LocationDialogueTrigger");
        Debug.Log("3. 右键点击组件，选择'手动触发对话'");

        EditorUtility.DisplayDialog("测试提示",
            "请在运行模式下测试！\n\n步骤：\n" +
            "1. 点击Unity的Play按钮进入运行模式\n" +
            "2. 在Hierarchy中选择一个LocationDialogueTrigger\n" +
            "3. 右键点击LocationDialogueTrigger组件\n" +
            "4. 选择'手动触发对话'进行测试",
            "确定");
    }

    private void ShowDiagnosticInfo()
    {
        GUILayout.Label("快速检查清单", EditorStyles.boldLabel);
        GUILayout.Space(5);

        using (new EditorGUILayout.VerticalScope(EditorStyles.helpBox))
        {
            GUILayout.Label("请检查以下项目：", EditorStyles.boldLabel);
            GUILayout.Space(5);

            bool allGood = true;

            // 检查GuideDialogueUI
            GuideDialogueUI guideUI = FindObjectOfType<GuideDialogueUI>();
            ShowCheckItem("场景中有GuideDialogueUI对象", guideUI != null, ref allGood);

            // 检查触发器
            LocationDialogueTrigger[] triggers = FindObjectsOfType<LocationDialogueTrigger>();
            ShowCheckItem("场景中有LocationDialogueTrigger", triggers != null && triggers.Length > 0, ref allGood);

            // 检查玩家
            GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
            ShowCheckItem("场景中有Tag为'Player'的对象", players != null && players.Length > 0, ref allGood);

            GUILayout.Space(10);

            if (allGood)
            {
                EditorGUILayout.HelpBox("所有基本检查都通过了！\n\n如果对话框仍然不显示，请运行诊断工具查看详细信息。", MessageType.Info);
            }
            else
            {
                EditorGUILayout.HelpBox("发现未完成的项目！\n\n请完成上述检查后再试。", MessageType.Warning);
            }
        }
    }

    private void ShowCheckItem(string text, bool passed, ref bool allGood)
    {
        GUILayout.BeginHorizontal();
        GUILayout.Label(passed ? "✓" : "✗", GUILayout.Width(20));
        GUILayout.Label(text);
        GUILayout.EndHorizontal();

        if (!passed) allGood = false;
    }
}
