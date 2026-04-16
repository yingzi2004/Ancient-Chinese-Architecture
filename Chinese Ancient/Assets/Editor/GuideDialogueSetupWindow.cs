using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.SceneManagement;
using TMPro;
using System.IO;

/// <summary>
/// 导游对话UI自动生成工具
/// 在Unity编辑器中自动创建导游对话面板
/// </summary>
public class GuideDialogueSetupWindow : EditorWindow
{
    private Sprite portraitSprite;
    private string portraitPath = "Assets/UIdesign/AIChat/立绘/1-1.png";
    private Vector2 scrollPosition;

    [MenuItem("Tools/导游对话系统/创建导游对话面板")]
    public static void ShowWindow()
    {
        var window = GetWindow<GuideDialogueSetupWindow>("导游对话面板生成器");
        window.minSize = new Vector2(400, 500);
    }

    void OnGUI()
    {
        scrollPosition = EditorGUILayout.BeginScrollView(scrollPosition);

        GUILayout.Label("导游对话UI面板生成器", EditorStyles.boldLabel);
        EditorGUILayout.Space();

        // 立绘图片设置
        GUILayout.Label("步骤1: 选择立绘图片", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "立绘图片将显示在对话框左上角",
            MessageType.Info
        );

        portraitPath = EditorGUILayout.TextField("图片路径", portraitPath);

        if (GUILayout.Button("加载立绘图片", GUILayout.Height(30)))
        {
            LoadPortraitImage();
        }

        if (portraitSprite != null)
        {
            EditorGUILayout.ObjectField("当前立绘", portraitSprite, typeof(Sprite), false);
        }
        else
        {
            EditorGUILayout.HelpBox("未加载立绘图片（可选）", MessageType.Warning);
        }

        EditorGUILayout.Space();

        // 创建按钮
        GUI.backgroundColor = new Color(0.4f, 0.8f, 0.4f);
        if (GUILayout.Button("步骤2: 创建导游对话面板", GUILayout.Height(50)))
        {
            CreateGuideDialoguePanel();
        }
        GUI.backgroundColor = Color.white;

        EditorGUILayout.Space();

        // 使用说明
        GUILayout.Label("使用说明", EditorStyles.boldLabel);
        EditorGUILayout.HelpBox(
            "创建后，请在场景中添加SceneGuideTrigger组件来自动触发对话。\n\n" +
            "或者直接调用代码：\n" +
            "GuideDialogueUI.Instance.StartGuideDialogue(\n" +
            "    \"导游名称\",\n" +
            "    立绘Sprite,\n" +
            "    new string[] { \"欢迎!\", \"这是导游对话\" }\n" +
            ");",
            MessageType.None
        );

        EditorGUILayout.EndScrollView();
    }

    void LoadPortraitImage()
    {
        portraitSprite = AssetDatabase.LoadAssetAtPath<Sprite>(portraitPath);
        if (portraitSprite == null)
        {
            Debug.LogError($"无法加载图片：{portraitPath}\n请确保路径正确且图片类型为Sprite (2D and UI)。");
            EditorUtility.DisplayDialog("加载失败", $"无法加载图片：\n{portraitPath}\n\n请检查路径是否正确，并确保图片的Texture Type设置为'Sprite (2D and UI)'", "确定");
        }
        else
        {
            Debug.Log($"成功加载立绘图片：{portraitSprite.name}");
        }
    }

    void CreateGuideDialoguePanel()
    {
        try
        {
            // 查找或创建Canvas
            Canvas canvas = FindOrCreateCanvas();

            // 创建主面板
            GameObject panelObj = CreateMainPanel(canvas);

            // 创建UI元素
            Image portraitImage = CreatePortraitImage(panelObj);
            TextMeshProUGUI guideNameText = CreateGuideNameText(panelObj);
            TextMeshProUGUI dialogueText = CreateDialogueText(panelObj);
            TextMeshProUGUI continuePromptText = CreateContinuePromptText(panelObj);
            Button continueButton = CreateContinueButton(panelObj);

            // 添加脚本并绑定
            GuideDialogueUI guideUI = panelObj.GetComponent<GuideDialogueUI>();
            if (guideUI == null)
            {
                guideUI = panelObj.AddComponent<GuideDialogueUI>();
            }

            // 绑定引用
            BindUIReferences(guideUI, panelObj, portraitImage, guideNameText, dialogueText, continuePromptText, continueButton);

            // 保存场景
            EditorSceneManager.MarkSceneDirty(EditorSceneManager.GetActiveScene());

            // 选择新创建的对象
            Selection.activeGameObject = panelObj;

            Debug.Log("导游对话面板创建成功！");
            EditorUtility.DisplayDialog("创建成功", "导游对话面板已创建！\n\n面板已自动选中，您可以在Inspector中查看和调整设置。\n\n下一步：\n1. 在场景中创建空对象\n2. 添加SceneGuideTrigger组件\n3. 配置对话内容", "确定");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"创建导游对话面板时出错：{e.Message}\n{e.StackTrace}");
            EditorUtility.DisplayDialog("创建失败", $"创建过程中出现错误：\n{e.Message}", "确定");
        }
    }

    Canvas FindOrCreateCanvas()
    {
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            if (!EditorUtility.DisplayDialog("未找到Canvas", "场景中没有Canvas，是否创建一个新的Canvas？", "创建", "取消"))
            {
                throw new System.OperationCanceledException("用户取消了创建Canvas");
            }

            canvas = CreateNewCanvas();
        }

        return canvas;
    }

    Canvas CreateNewCanvas()
    {
        GameObject canvasObj = new GameObject("Canvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<CanvasScaler>();
        canvasObj.AddComponent<GraphicRaycaster>();

        // 添加EventSystem
        if (Object.FindFirstObjectByType<UnityEngine.EventSystems.EventSystem>() == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }

        return canvas;
    }

    GameObject CreateMainPanel(Canvas canvas)
    {
        GameObject panelObj = new GameObject("GuideDialoguePanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0, 0);
        panelRect.anchorMax = new Vector2(1, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 50);
        panelRect.sizeDelta = new Vector2(-100, 300);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.9f);

        return panelObj;
    }

    Image CreatePortraitImage(GameObject parent)
    {
        GameObject portraitObj = CreateUIObject("PortraitImage", parent.transform);
        RectTransform portraitRect = portraitObj.GetComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0, 1);
        portraitRect.anchorMax = new Vector2(0, 1);
        portraitRect.pivot = new Vector2(0, 1);
        portraitRect.anchoredPosition = new Vector2(100, -100);
        portraitRect.sizeDelta = new Vector2(200, 250);

        Image portraitImage = portraitObj.AddComponent<Image>();
        if (portraitSprite != null)
        {
            portraitImage.sprite = portraitSprite;
        }
        portraitImage.preserveAspect = true;

        return portraitImage;
    }

    TextMeshProUGUI CreateGuideNameText(GameObject parent)
    {
        GameObject guideNameObj = CreateUIObject("GuideNameText", parent.transform);
        RectTransform guideNameRect = guideNameObj.GetComponent<RectTransform>();
        guideNameRect.anchorMin = new Vector2(0, 1);
        guideNameRect.anchorMax = new Vector2(0, 1);
        guideNameRect.pivot = new Vector2(0, 1);
        guideNameRect.anchoredPosition = new Vector2(320, -50);
        guideNameRect.sizeDelta = new Vector2(400, 50);

        TextMeshProUGUI guideNameText = guideNameObj.AddComponent<TextMeshProUGUI>();
        guideNameText.text = "导游";
        guideNameText.fontSize = 36;
        guideNameText.color = Color.white;
        guideNameText.fontStyle = FontStyles.Bold;
        guideNameText.alignment = TextAlignmentOptions.Left;

        return guideNameText;
    }

    TextMeshProUGUI CreateDialogueText(GameObject parent)
    {
        GameObject dialogueObj = CreateUIObject("DialogueText", parent.transform);
        RectTransform dialogueRect = dialogueObj.GetComponent<RectTransform>();
        dialogueRect.anchorMin = new Vector2(0, 0);
        dialogueRect.anchorMax = new Vector2(1, 1);
        dialogueRect.pivot = new Vector2(0.5f, 0.5f);
        dialogueRect.anchoredPosition = new Vector2(100, 0);
        dialogueRect.sizeDelta = new Vector2(-500, -200);

        TextMeshProUGUI dialogueText = dialogueObj.AddComponent<TextMeshProUGUI>();
        dialogueText.text = "对话内容将在这里显示...";
        dialogueText.fontSize = 28;
        dialogueText.color = new Color(0.95f, 0.95f, 0.95f);
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        // 文本换行默认开启，无需额外设置

        return dialogueText;
    }

    TextMeshProUGUI CreateContinuePromptText(GameObject parent)
    {
        GameObject continuePromptObj = CreateUIObject("ContinuePromptText", parent.transform);
        RectTransform continuePromptRect = continuePromptObj.GetComponent<RectTransform>();
        continuePromptRect.anchorMin = new Vector2(1, 0);
        continuePromptRect.anchorMax = new Vector2(1, 0);
        continuePromptRect.pivot = new Vector2(1, 0);
        continuePromptRect.anchoredPosition = new Vector2(-50, 50);
        continuePromptRect.sizeDelta = new Vector2(400, 50);

        TextMeshProUGUI continuePromptText = continuePromptObj.AddComponent<TextMeshProUGUI>();
        continuePromptText.text = "按 L 键或点击按钮继续...";
        continuePromptText.fontSize = 24;
        continuePromptText.color = new Color(0.7f, 0.7f, 0.7f);
        continuePromptText.alignment = TextAlignmentOptions.Right;
        continuePromptText.gameObject.SetActive(false);

        return continuePromptText;
    }

    Button CreateContinueButton(GameObject parent)
    {
        GameObject continueButtonObj = CreateUIObject("ContinueButton", parent.transform);
        RectTransform continueButtonRect = continueButtonObj.GetComponent<RectTransform>();
        continueButtonRect.anchorMin = new Vector2(0.5f, 0);
        continueButtonRect.anchorMax = new Vector2(0.5f, 0);
        continueButtonRect.pivot = new Vector2(0.5f, 0);
        continueButtonRect.anchoredPosition = new Vector2(0, 30);
        continueButtonRect.sizeDelta = new Vector2(200, 50);

        Image continueButtonImage = continueButtonObj.AddComponent<Image>();
        continueButtonImage.color = new Color(0.3f, 0.5f, 0.8f);

        Button continueButton = continueButtonObj.AddComponent<Button>();

        // 按钮文字
        GameObject buttonTextObj = CreateUIObject("Text", continueButtonObj.transform);
        RectTransform buttonTextRect = buttonTextObj.GetComponent<RectTransform>();
        buttonTextRect.anchorMin = new Vector2(0, 0);
        buttonTextRect.anchorMax = new Vector2(1, 1);
        buttonTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "继续";
        buttonText.fontSize = 24;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;

        return continueButton;
    }

    void BindUIReferences(GuideDialogueUI guideUI, GameObject panelObj, Image portraitImage,
        TextMeshProUGUI guideNameText, TextMeshProUGUI dialogueText,
        TextMeshProUGUI continuePromptText, Button continueButton)
    {
        SerializedObject serializedObject = new SerializedObject(guideUI);

        SerializedProperty portraitProp = serializedObject.FindProperty("portraitImage");
        portraitProp.objectReferenceValue = portraitImage;

        SerializedProperty guideNameProp = serializedObject.FindProperty("guideNameText");
        guideNameProp.objectReferenceValue = guideNameText;

        SerializedProperty dialogueProp = serializedObject.FindProperty("dialogueText");
        dialogueProp.objectReferenceValue = dialogueText;

        SerializedProperty continuePromptProp = serializedObject.FindProperty("continuePromptText");
        continuePromptProp.objectReferenceValue = continuePromptText;

        SerializedProperty continueButtonProp = serializedObject.FindProperty("continueButton");
        continueButtonProp.objectReferenceValue = continueButton;

        SerializedProperty panelProp = serializedObject.FindProperty("dialoguePanel");
        panelProp.objectReferenceValue = panelObj;

        serializedObject.ApplyModifiedProperties();
    }

    GameObject CreateUIObject(string name, Transform parent)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);
        obj.AddComponent<RectTransform>();
        return obj;
    }
}
