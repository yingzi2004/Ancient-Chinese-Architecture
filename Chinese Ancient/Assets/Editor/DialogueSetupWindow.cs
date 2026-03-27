using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

/// <summary>
/// 对话系统自动设置窗口
/// 一键创建所有需要的DialogueManager和UI元素
/// </summary>
public class DialogueSetupWindow : EditorWindow
{
    [MenuItem("工具/对话系统设置")]
    public static void ShowWindow()
    {
        var window = GetWindow<DialogueSetupWindow>("对话系统设置");
        window.minSize = new Vector2(400, 300);
    }

    private void OnGUI()
    {
        try
        {
            EditorGUILayout.BeginVertical();
            {
                GUILayout.Label("对话系统自动设置工具", EditorStyles.boldLabel);
                GUILayout.Space(10);

                EditorGUILayout.HelpBox(
                    "点击下面的按钮将自动创建：\n" +
                    "• DialogueManager对象\n" +
                    "• Canvas和对话UI面板\n" +
                    "• NPC名称、对话内容、选项容器\n" +
                    "• 选项按钮预制体\n" +
                    "• 所有UI引用自动连接",
                    MessageType.Info
                );

                GUILayout.Space(20);

                if (GUILayout.Button("一键创建对话系统", GUILayout.Height(50)))
                {
                    CreateDialogueSystem();
                }

                GUILayout.Space(20);

                if (GUILayout.Button("删除现有的对话系统", GUILayout.Height(30)))
                {
                    if (EditorUtility.DisplayDialog("确认删除",
                        "确定要删除现有的DialogueManager和对话UI吗？此操作不可撤销！", "确定", "取消"))
                    {
                        DeleteDialogueSystem();
                    }
                }

                GUILayout.FlexibleSpace();
            }
            EditorGUILayout.EndVertical();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[对话系统设置] 界面渲染错误: {e.Message}\n{e.StackTrace}");
        }
    }

    private void CreateDialogueSystem()
    {
        try
        {
            Debug.Log("<color=green>[对话系统设置]</color> 开始创建对话系统...");

            // 检查是否已存在
            DialogueManager existingManager = FindObjectOfType<DialogueManager>();
            if (existingManager != null)
            {
                if (EditorUtility.DisplayDialog("警告",
                    "场景中已经存在DialogueManager！\n是否要删除现有的并重新创建？", "重新创建", "取消"))
                {
                    DeleteDialogueSystem();
                }
                else
                {
                    return;
                }
            }

            // 1. 创建DialogueManager
            GameObject dialogueManagerObj = new GameObject("DialogueManager");
            DialogueManager dialogueManager = dialogueManagerObj.AddComponent<DialogueManager>();

            // 2. 创建Canvas
            Canvas canvas = CreateCanvas();

            // 3. 创建DialoguePanel
            GameObject dialoguePanel = CreateDialoguePanel(canvas.transform);

            if (dialoguePanel == null)
            {
                Debug.LogError("<color=red>[对话系统设置]</color> 创建DialoguePanel失败！");
                return;
            }

            // 4. 创建选项按钮预制体（先创建，这样才能连接引用）
            CreateOptionButtonPrefab();

            // 5. 立即连接引用
            ConnectUIReferences(dialogueManager, dialoguePanel);

            // 6. 验证引用是否连接成功
            EditorApplication.delayCall += () =>
            {
                VerifyReferences(dialogueManager);

                Debug.Log("<color=green>[对话系统设置]</color> 对话系统创建完成！");

                EditorUtility.DisplayDialog("完成", "对话系统创建成功！\n所有UI已自动设置完成。", "确定");

                // 选中DialogueManager
                if (dialogueManagerObj != null)
                {
                    Selection.activeGameObject = dialogueManagerObj;
                }
            };
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[对话系统设置]</color> 创建失败: {e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("错误", $"创建对话系统时出错：\n{e.Message}", "确定");
        }
    }

    private Canvas CreateCanvas()
    {
        try
        {
            // 查找是否已有Canvas
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            Canvas canvas = null;

            foreach (Canvas c in canvases)
            {
                if (c.renderMode == RenderMode.ScreenSpaceOverlay)
                {
                    canvas = c;
                    Debug.Log("<color=green>[对话系统设置]</color> 找到现有Canvas");
                    break;
                }
            }

            // 如果没有则创建新的
            if (canvas == null)
            {
                GameObject canvasObj = new GameObject("Canvas");
                canvas = canvasObj.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvasObj.AddComponent<CanvasScaler>();
                canvasObj.AddComponent<GraphicRaycaster>();

                // 配置Canvas Scaler
                CanvasScaler scaler = canvasObj.GetComponent<CanvasScaler>();
                scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
                scaler.referenceResolution = new Vector2(1920, 1080);

                Debug.Log("<color=green>[对话系统设置]</color> 创建新Canvas");

                // 创建EventSystem
                if (UnityEngine.EventSystems.EventSystem.current == null)
                {
                    GameObject eventSystemObj = new GameObject("EventSystem");
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                    eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
                    Debug.Log("<color=green>[对话系统设置]</color> 创建EventSystem");
                }
            }

            return canvas;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[对话系统设置] 创建Canvas失败: {e.Message}");
            return null;
        }
    }

    private GameObject CreateDialoguePanel(Transform parent)
    {
        GameObject panel = null;

        try
        {
            Debug.Log("<color=cyan>[对话系统设置]</color> 开始创建DialoguePanel...");

            // 创建DialoguePanel
            panel = new GameObject("DialoguePanel");
            panel.transform.SetParent(parent, false);

            Debug.Log("<color=green>[对话系统设置]</color> DialoguePanel对象已创建");

            // 设置面板RectTransform
            RectTransform rectTransform = panel.AddComponent<RectTransform>();
            rectTransform.anchorMin = new Vector2(0.5f, 0);
            rectTransform.anchorMax = new Vector2(0.5f, 0);
            rectTransform.pivot = new Vector2(0.5f, 1);
            rectTransform.anchoredPosition = new Vector2(0, 50);
            rectTransform.sizeDelta = new Vector2(600, 280);

            Debug.Log("<color=green>[对话系统设置]</color> RectTransform已添加");

            // 添加半透明黑色背景
            Image image = panel.AddComponent<Image>();
            image.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

            Debug.Log("<color=green>[对话系统设置]</color> 背景Image已添加");

            // ========== NPC名称文本（顶部） ==========
            GameObject npcNameObj = new GameObject("NPCName");
            npcNameObj.transform.SetParent(panel.transform, false);

            RectTransform npcNameRect = npcNameObj.AddComponent<RectTransform>();
            npcNameRect.anchorMin = new Vector2(0, 1);
            npcNameRect.anchorMax = new Vector2(1, 1);
            npcNameRect.pivot = new Vector2(0.5f, 1);
            npcNameRect.anchoredPosition = new Vector2(0, -10);
            npcNameRect.sizeDelta = new Vector2(-40, 40);

            Text npcNameText = npcNameObj.AddComponent<Text>();
            npcNameText.text = "NPC名称";
            npcNameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            npcNameText.fontSize = 22;
            npcNameText.alignment = TextAnchor.MiddleCenter;
            npcNameText.color = new Color(1f, 0.85f, 0.2f);
            npcNameText.fontStyle = FontStyle.Bold;

            Debug.Log("<color=green>[对话系统设置]</color> NPCName已创建");

            // ========== 对话内容文本（中间） ==========
            GameObject dialogueTextObj = new GameObject("DialogueText");
            dialogueTextObj.transform.SetParent(panel.transform, false);

            RectTransform dialogueTextRect = dialogueTextObj.AddComponent<RectTransform>();
            dialogueTextRect.anchorMin = new Vector2(0, 0.35f);
            dialogueTextRect.anchorMax = new Vector2(1, 0.75f);
            dialogueTextRect.sizeDelta = new Vector2(-40, 0);
            dialogueTextRect.pivot = new Vector2(0.5f, 0.5f);

            Text dialogueText = dialogueTextObj.AddComponent<Text>();
            dialogueText.text = "对话内容将在这里显示...";
            dialogueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            dialogueText.fontSize = 18;
            dialogueText.alignment = TextAnchor.UpperLeft;
            dialogueText.color = Color.white;
            dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
            dialogueText.verticalOverflow = VerticalWrapMode.Truncate;

            Debug.Log("<color=green>[对话系统设置]</color> DialogueText已创建");

            // ========== 选项容器（底部） ==========
            GameObject optionsContainer = new GameObject("OptionsContainer");
            optionsContainer.transform.SetParent(panel.transform, false);

            RectTransform optionsRect = optionsContainer.AddComponent<RectTransform>();
            optionsRect.anchorMin = new Vector2(0.1f, 0.05f);
            optionsRect.anchorMax = new Vector2(0.9f, 0.32f);
            optionsRect.sizeDelta = new Vector2(0, 0);
            optionsRect.pivot = new Vector2(0.5f, 0);

            // 添加Vertical Layout Group
            VerticalLayoutGroup layoutGroup = optionsContainer.AddComponent<VerticalLayoutGroup>();
            layoutGroup.spacing = 8;
            layoutGroup.childAlignment = TextAnchor.MiddleCenter;
            layoutGroup.childControlWidth = true;
            layoutGroup.childControlHeight = true;
            layoutGroup.childForceExpandWidth = true;
            layoutGroup.childForceExpandHeight = false;

            ContentSizeFitter sizeFitter = optionsContainer.AddComponent<ContentSizeFitter>();
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Debug.Log("<color=green>[对话系统设置]</color> OptionsContainer已创建");

            // 注册Undo操作（在最后执行）
            Undo.RegisterCreatedObjectUndo(panel, "Create DialoguePanel");
            Undo.RegisterCreatedObjectUndo(npcNameObj, "Create NPCName");
            Undo.RegisterCreatedObjectUndo(dialogueTextObj, "Create DialogueText");
            Undo.RegisterCreatedObjectUndo(optionsContainer, "Create OptionsContainer");

            Debug.Log("<color=green>[对话系统设置]</color> 创建对话面板完成");
            return panel;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"<color=red>[对话系统设置]</color> 创建对话面板失败: {e.Message}\n{e.StackTrace}");
            if (panel != null)
            {
                DestroyImmediate(panel);
            }
            return null;
        }
    }

    private void ConnectUIReferences(DialogueManager dialogueManager, GameObject dialoguePanel)
    {
        try
        {
            if (dialogueManager == null || dialoguePanel == null)
            {
                Debug.LogError("<color=red>[对话系统设置]</color> 连接引用失败：对象为空");
                return;
            }

            // 查找UI元素
            Transform npcNameTransform = dialoguePanel.transform.Find("NPCName");
            Transform dialogueTextTransform = dialoguePanel.transform.Find("DialogueText");
            Transform optionsContainerTransform = dialoguePanel.transform.Find("OptionsContainer");

            Debug.Log($"<color=cyan>[对话系统设置]</color> 查找UI元素：");
            Debug.Log($"  - NPCName: {(npcNameTransform != null ? "✓" : "✗")}");
            Debug.Log($"  - DialogueText: {(dialogueTextTransform != null ? "✓" : "✗")}");
            Debug.Log($"  - OptionsContainer: {(optionsContainerTransform != null ? "✓" : "✗")}");

            if (npcNameTransform == null)
            {
                Debug.LogError("<color=red>[对话系统设置]</color> 找不到NPCName");
                return;
            }
            if (dialogueTextTransform == null)
            {
                Debug.LogError("<color=red>[对话系统设置]</color> 找不到DialogueText");
                return;
            }
            if (optionsContainerTransform == null)
            {
                Debug.LogError("<color=red>[对话系统设置]</color> 找不到OptionsContainer");
                Debug.LogError($"<color=red>[对话系统设置]</color> DialoguePanel的子对象列表：");
                foreach (Transform child in dialoguePanel.transform)
                {
                    Debug.LogError($"  - {child.name}");
                }
                return;
            }

            // 使用SerializedObject来设置引用
            SerializedObject serializedObject = new SerializedObject(dialogueManager);

            // DialoguePanel
            SerializedProperty dialoguePanelProp = serializedObject.FindProperty("dialoguePanel");
            dialoguePanelProp.objectReferenceValue = dialoguePanel;

            // NPCName
            SerializedProperty npcNameProp = serializedObject.FindProperty("npcNameText");
            npcNameProp.objectReferenceValue = npcNameTransform.GetComponent<Text>();

            // DialogueText
            SerializedProperty dialogueTextProp = serializedObject.FindProperty("dialogueText");
            dialogueTextProp.objectReferenceValue = dialogueTextTransform.GetComponent<Text>();

            // OptionsContainer
            SerializedProperty optionsContainerProp = serializedObject.FindProperty("optionsContainer");
            optionsContainerProp.objectReferenceValue = optionsContainerTransform;

            // OptionButtonPrefab - 从Resources加载
            string prefabPath = "Assets/Resources/Prefabs/OptionButton.prefab";
            GameObject optionButtonPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
            if (optionButtonPrefab != null)
            {
                SerializedProperty optionButtonPrefabProp = serializedObject.FindProperty("optionButtonPrefab");
                optionButtonPrefabProp.objectReferenceValue = optionButtonPrefab;
                Debug.Log("<color=green>[对话系统设置]</color> 已连接OptionButton预制体");
            }
            else
            {
                Debug.LogWarning("<color=yellow>[对话系统设置]</color> 未找到OptionButton预制体，将使用Resources.Load自动加载");
            }

            // 应用修改
            serializedObject.ApplyModifiedProperties();

            // 标记场景为已修改，确保Unity保存引用
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            Debug.Log("<color=green>[对话系统设置]</color> UI引用已连接");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[对话系统设置] 连接UI引用失败: {e.Message}");
        }
    }

    private void VerifyReferences(DialogueManager dialogueManager)
    {
        Debug.Log("<color=cyan>[对话系统设置]</color> ===== 开始验证UI引用 =====");

        bool allGood = true;

        if (dialogueManager.dialoguePanel == null)
        {
            Debug.LogError("<color=red>[对话系统设置]</color> ❌ dialoguePanel 为空！");
            allGood = false;
        }
        else
        {
            Debug.Log("<color=green>[对话系统设置]</color> ✅ dialoguePanel 已连接");
        }

        if (dialogueManager.npcNameText == null)
        {
            Debug.LogError("<color=red>[对话系统设置]</color> ❌ npcNameText 为空！");
            allGood = false;
        }
        else
        {
            Debug.Log("<color=green>[对话系统设置]</color> ✅ npcNameText 已连接");
        }

        if (dialogueManager.dialogueText == null)
        {
            Debug.LogError("<color=red>[对话系统设置]</color> ❌ dialogueText 为空！");
            allGood = false;
        }
        else
        {
            Debug.Log("<color=green>[对话系统设置]</color> ✅ dialogueText 已连接");
        }

        if (dialogueManager.optionsContainer == null)
        {
            Debug.LogError("<color=red>[对话系统设置]</color> ❌ optionsContainer 为空！");
            allGood = false;
        }
        else
        {
            Debug.Log("<color=green>[对话系统设置]</color> ✅ optionsContainer 已连接");
        }

        if (dialogueManager.optionButtonPrefab == null)
        {
            Debug.LogWarning("<color=yellow>[对话系统设置]</color> ⚠️ optionButtonPrefab 为空（将使用Resources.Load自动加载）");
        }
        else
        {
            Debug.Log("<color=green>[对话系统设置]</color> ✅ optionButtonPrefab 已连接");
        }

        Debug.Log("<color=cyan>[对话系统设置]</color> ===== 验证完成 =====");

        if (!allGood)
        {
            Debug.LogWarning("<color=yellow>[对话系统设置]</color> 部分引用连接失败，请手动连接！");
        }
    }

    private void CreateOptionButtonPrefab()
    {
        try
        {
            // 确保Resources/Prefabs文件夹存在
            string prefabPath = "Assets/Resources/Prefabs";
            if (!Directory.Exists(prefabPath))
            {
                Directory.CreateDirectory(prefabPath);
                Debug.Log("<color=green>[对话系统设置]</color> 创建文件夹: " + prefabPath);
            }

            // 创建临时按钮对象
            GameObject buttonObj = new GameObject("OptionButton");
            Undo.RegisterCreatedObjectUndo(buttonObj, "Create OptionButton");

            RectTransform buttonRect = buttonObj.AddComponent<RectTransform>();
            buttonRect.sizeDelta = new Vector2(350, 35);

            buttonObj.AddComponent<CanvasRenderer>();

            Image buttonImage = buttonObj.AddComponent<Image>();
            buttonImage.color = new Color(0.3f, 0.5f, 0.8f, 0.9f);

            Button button = buttonObj.AddComponent<Button>();

            // 添加按钮状态颜色
            ColorBlock colors = button.colors;
            colors.normalColor = new Color(0.3f, 0.5f, 0.8f, 0.9f);
            colors.highlightedColor = new Color(0.4f, 0.6f, 0.9f, 1f);
            colors.pressedColor = new Color(0.2f, 0.4f, 0.7f, 1f);
            button.colors = colors;

            // 创建按钮文本
            GameObject textObj = new GameObject("Text");
            Undo.RegisterCreatedObjectUndo(textObj, "Create ButtonText");
            textObj.transform.SetParent(buttonObj.transform, false);

            RectTransform textRect = textObj.AddComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.sizeDelta = Vector2.zero;
            textRect.offsetMin = new Vector2(10, 0);
            textRect.offsetMax = new Vector2(-10, 0);

            Text text = textObj.AddComponent<Text>();
            text.text = "选项";
            text.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
            text.fontSize = 16;
            text.color = Color.white;
            text.alignment = TextAnchor.MiddleCenter;
            text.fontStyle = FontStyle.Bold;

            // 保存为预制体
            string finalPath = prefabPath + "/OptionButton.prefab";

            // 如果已存在则删除
            if (AssetDatabase.LoadAssetAtPath<GameObject>(finalPath) != null)
            {
                AssetDatabase.DeleteAsset(finalPath);
            }

            PrefabUtility.SaveAsPrefabAsset(buttonObj, finalPath);

            // 删除临时对象
            DestroyImmediate(buttonObj);

            Debug.Log("<color=green>[对话系统设置]</color> 创建选项按钮预制体: " + finalPath);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[对话系统设置] 创建按钮预制体失败: {e.Message}");
        }
    }

    private void DeleteDialogueSystem()
    {
        try
        {
            // 删除DialogueManager
            DialogueManager[] dialogueManagers = FindObjectsOfType<DialogueManager>();
            foreach (var dm in dialogueManagers)
            {
                Undo.DestroyObjectImmediate(dm.gameObject);
            }

            // 删除对话UI
            Canvas[] canvases = FindObjectsOfType<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                Transform dialoguePanel = canvas.transform.Find("DialoguePanel");
                if (dialoguePanel != null)
                {
                    Undo.DestroyObjectImmediate(dialoguePanel.gameObject);
                }
            }

            // 删除预制体
            string prefabPath = "Assets/Resources/Prefabs/OptionButton.prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath) != null)
            {
                AssetDatabase.DeleteAsset(prefabPath);
            }

            Debug.Log("<color=yellow>[对话系统设置]</color> 已删除现有的对话系统");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"[对话系统设置] 删除对话系统失败: {e.Message}");
        }
    }
}
