using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

/// <summary>
/// 导游对话自动设置脚本
/// 将此脚本添加到Min Exhibition场景的任何GameObject上
/// 运行时会自动创建导游对话UI和触发器
/// </summary>
public class GuideDialogueAutoSetup : MonoBehaviour
{
    [Header("导游信息")]
    [SerializeField] private string guideName = "古建筑讲解员";
    [SerializeField] private bool useDefaultPortrait = true;
    [SerializeField] private Sprite customPortrait; // 自定义立绘图片

    [Header("对话内容")]
    [TextArea(3, 10)]
    [SerializeField] private string[] welcomeDialogue = new string[]
    {
        "欢迎来到微型古建筑展览馆！",
        "这里展示了中国古代建筑的精髓，从福建土楼到苏州园林，每一座建筑都承载着深厚的历史文化。",
        "我是这里的讲解员，将由我带领您参观这个精彩的展览。",
        "请自由参观，如果需要了解更多信息，随时可以向我提问。",
        "祝您参观愉快！"
    };

    [Header("设置")]
    [SerializeField] private float triggerDelay = 1f;
    [SerializeField] private bool triggerOnce = true;

    private GuideDialogueUI guideUI;
    private Sprite loadedPortraitSprite; // 存储已加载的立绘图片

    void Start()
    {
        // 检查当前场景
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name != "Min Exhibition")
        {
            Debug.Log($"GuideDialogueAutoSetup: 当前场景是 '{currentScene.name}'，不是 Min Exhibition，跳过自动创建");
            return;
        }

        Debug.Log("GuideDialogueAutoSetup: 开始在 Min Exhibition 场景中创建导游对话系统");

        // 查找或创建Canvas
        Canvas canvas = FindOrCreateCanvas();

        // 创建导游对话UI
        CreateGuideDialogueUI(canvas);

        // 延迟触发对话
        Invoke(nameof(TriggerWelcomeDialogue), triggerDelay);
    }

    Canvas FindOrCreateCanvas()
    {
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 添加EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("GuideDialogueAutoSetup: 创建了新的Canvas");
        }

        return canvas;
    }

    void CreateGuideDialogueUI(Canvas canvas)
    {
        // 创建主面板 - 缩小并放在中下方
        GameObject panelObj = new GameObject("GuideDialoguePanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0); // 从底部中心开始
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 80); // 距离底部80像素
        panelRect.sizeDelta = new Vector2(800, 180); // 缩小到800x180

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // 添加GuideDialogueUI脚本
        guideUI = panelObj.AddComponent<GuideDialogueUI>();

        // 创建立绘图片
        GameObject portraitObj = CreateUIElement("PortraitImage", panelObj.transform,
            new Vector2(0, 0.5f), new Vector2(20, 0), new Vector2(280, 400)); // 立绘尺寸280x400
        Image portraitImage = portraitObj.AddComponent<Image>();

        // 确定要使用的立绘图片
        Sprite portraitSprite = null;
        if (customPortrait != null)
        {
            portraitSprite = customPortrait;
        }
        else if (useDefaultPortrait)
        {
            // 尝试加载立绘图片
            portraitSprite = Resources.Load<Sprite>("UIdesign/AIChat/立绘/1-1");
            if (portraitSprite == null)
            {
                // 如果Resources文件夹没有，尝试直接从Assets加载
                #if UNITY_EDITOR
                portraitSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIdesign/AIChat/立绘/1-1.png");
                #endif
            }
        }

        // 设置立绘图片
        if (portraitSprite != null)
        {
            portraitImage.sprite = portraitSprite;
            loadedPortraitSprite = portraitSprite; // 保存以便在对话中使用
            Debug.Log("GuideDialogueAutoSetup: 成功加载立绘图片");
        }
        else
        {
            Debug.LogWarning("GuideDialogueAutoSetup: 未找到立绘图片，使用默认颜色");
            portraitImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
        }
        portraitImage.preserveAspect = true;

        // 创建导游名称 - 调整位置和大小
        GameObject guideNameObj = CreateUIElement("GuideNameText", panelObj.transform,
            new Vector2(0, 1), new Vector2(150, -10), new Vector2(400, 35));
        TextMeshProUGUI guideNameText = guideNameObj.AddComponent<TextMeshProUGUI>();
        guideNameText.text = guideName;
        guideNameText.fontSize = 24;
        guideNameText.color = Color.white;
        guideNameText.fontStyle = FontStyles.Bold;
        guideNameText.alignment = TextAlignmentOptions.Left;
        // 尝试设置支持中文的字体
        SetChineseFont(guideNameText);

        // 创建对话内容 - 调整位置和大小
        GameObject dialogueObj = CreateUIElement("DialogueText", panelObj.transform,
            new Vector2(0.5f, 0.5f), new Vector2(60, 0), new Vector2(550, 100)); // 缩小并居中
        TextMeshProUGUI dialogueText = dialogueObj.AddComponent<TextMeshProUGUI>();
        dialogueText.text = "对话内容将在这里显示...";
        dialogueText.fontSize = 20; // 减小字体
        dialogueText.color = new Color(0.95f, 0.95f, 0.95f);
        dialogueText.alignment = TextAlignmentOptions.TopLeft;
        // 尝试设置支持中文的字体
        SetChineseFont(dialogueText);

        // 创建继续提示 - 调整位置
        GameObject continuePromptObj = CreateUIElement("ContinuePromptText", panelObj.transform,
            new Vector2(1, 0), new Vector2(-20, 10), new Vector2(300, 30));
        TextMeshProUGUI continuePromptText = continuePromptObj.AddComponent<TextMeshProUGUI>();
        continuePromptText.text = "按 L 键继续";
        continuePromptText.fontSize = 16;
        continuePromptText.color = new Color(0.7f, 0.7f, 0.7f);
        continuePromptText.alignment = TextAlignmentOptions.Right;
        continuePromptText.gameObject.SetActive(false);
        SetChineseFont(continuePromptText);

        // 创建继续按钮 - 缩小
        GameObject continueButtonObj = CreateUIElement("ContinueButton", panelObj.transform,
            new Vector2(0.5f, 1), new Vector2(320, -8), new Vector2(120, 28)); // 右上角
        Image continueButtonImage = continueButtonObj.AddComponent<Image>();
        continueButtonImage.color = new Color(0.3f, 0.5f, 0.8f);
        Button continueButton = continueButtonObj.AddComponent<Button>();

        // 按钮文字
        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(continueButtonObj.transform, false);
        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI buttonText = buttonTextObj.AddComponent<TextMeshProUGUI>();
        buttonText.text = "继续";
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAlignmentOptions.Center;
        SetChineseFont(buttonText);

        // 绑定所有UI组件到GuideDialogueUI脚本
        guideUI.SetUIComponents(panelObj, portraitImage, guideNameText, dialogueText, continuePromptText, continueButton);

        Debug.Log("GuideDialogueAutoSetup: 导游对话UI创建完成");
    }

    GameObject CreateUIElement(string name, Transform parent, Vector2 anchor, Vector2 position, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        return obj;
    }

    /// <summary>
    /// 设置支持中文的字体
    /// </summary>
    void SetChineseFont(TextMeshProUGUI textComponent)
    {
        // 尝试查找项目中已配置的中文字体资源
        #if UNITY_EDITOR
        // 常见的中文字体资源路径
        string[] fontPaths = new string[]
        {
            "Assets/TextMesh Pro/Resources/TMP Settings.asset", // TMP默认设置
            "Assets/TextMesh Pro/Font Resources", // TMP字体资源文件夹
        };

        // 获取TMP设置
        if (TMPro.TMP_Settings.defaultFontAsset != null)
        {
            textComponent.font = TMPro.TMP_Settings.defaultFontAsset;
            Debug.Log("GuideDialogueAutoSetup: 使用TMP默认字体");
        }

        // 尝试加载常见的中文字体资源
        string[] chineseFontNames = new string[]
        {
            "LiberationSans SDF", // TMP默认字体
            "NotoSansCJK-Regular SDF",
            "SourceHanSansCN-Regular SDF",
            "MSYH SDF", // 微软雅黑
        };

        foreach (string fontName in chineseFontNames)
        {
            TMPro.TMP_FontAsset fontAsset = Resources.Load<TMPro.TMP_FontAsset>("Font Resources/" + fontName);
            if (fontAsset != null)
            {
                textComponent.font = fontAsset;
                Debug.Log($"GuideDialogueAutoSetup: 使用字体 {fontName}");
                break;
            }
        }
        #endif
    }

    void TriggerWelcomeDialogue()
    {
        if (guideUI != null && welcomeDialogue != null && welcomeDialogue.Length > 0)
        {
            guideUI.StartGuideDialogue(guideName, loadedPortraitSprite, welcomeDialogue);
            Debug.Log("GuideDialogueAutoSetup: 触发欢迎对话");
        }
    }

    /// <summary>
    /// 测试对话（可在运行时通过Inspector按钮调用）
    /// </summary>
    [ContextMenu("测试对话")]
    public void TestDialogue()
    {
        if (guideUI != null)
        {
            guideUI.StartGuideDialogue(guideName, loadedPortraitSprite, welcomeDialogue);
        }
    }
}
