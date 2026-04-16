using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 导游对话自动设置脚本 - 使用Legacy UI Text（支持中文）
/// 将此脚本添加到Min Exhibition场景的任何GameObject上
/// </summary>
public class GuideDialogueAutoSetup_Legacy : MonoBehaviour
{
    [Header("导游信息")]
    [SerializeField] private string guideName = "古建筑讲解员";
    [SerializeField] private bool useDefaultPortrait = true;

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

    void Start()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        if (currentScene.name != "Min Exhibition")
        {
            return;
        }

        Canvas canvas = FindOrCreateCanvas();
        CreateGuideDialogueUI(canvas);
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

            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        return canvas;
    }

    void CreateGuideDialogueUI(Canvas canvas)
    {
        // 主面板 - 中下方，小尺寸
        GameObject panelObj = new GameObject("GuideDialoguePanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 80);
        panelRect.sizeDelta = new Vector2(800, 180);

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        guideUI = panelObj.AddComponent<GuideDialogueUI>();

        // 立绘 - 左侧，小尺寸
        GameObject portraitObj = CreateUIElement("PortraitImage", panelObj.transform,
            new Vector2(0, 0.5f), new Vector2(15, 0), new Vector2(120, 160));
        Image portraitImage = portraitObj.AddComponent<Image>();

        if (useDefaultPortrait)
        {
            #if UNITY_EDITOR
            Sprite portraitSprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>("Assets/UIdesign/AIChat/立绘/1-1.png");
            if (portraitSprite != null)
            {
                portraitImage.sprite = portraitSprite;
            }
            else
            {
                portraitImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            }
            #endif
        }
        portraitImage.preserveAspect = true;

        // 导游名称 - 使用Legacy Text
        GameObject guideNameObj = CreateUIElement("GuideNameText", panelObj.transform,
            new Vector2(0, 1), new Vector2(150, -10), new Vector2(400, 35));
        Text guideNameText = guideNameObj.AddComponent<Text>();
        guideNameText.text = guideName;
        guideNameText.font = Resources.GetBuiltinResource<Font>("Arial.ttf"); // 使用内置字体
        guideNameText.fontSize = 24;
        guideNameText.color = Color.white;
        guideNameText.fontStyle = FontStyle.Bold;
        guideNameText.alignment = TextAnchor.MiddleLeft;

        // 对话内容 - 使用Legacy Text
        GameObject dialogueObj = CreateUIElement("DialogueText", panelObj.transform,
            new Vector2(0.5f, 0.5f), new Vector2(60, 0), new Vector2(550, 100));
        Text dialogueText = dialogueObj.AddComponent<Text>();
        dialogueText.text = "对话内容将在这里显示...";
        dialogueText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        dialogueText.fontSize = 20;
        dialogueText.color = new Color(0.95f, 0.95f, 0.95f);
        dialogueText.alignment = TextAnchor.UpperLeft;
        dialogueText.horizontalOverflow = HorizontalWrapMode.Wrap;
        dialogueText.verticalOverflow = VerticalWrapMode.Overflow;

        // 继续提示 - 使用Legacy Text
        GameObject continuePromptObj = CreateUIElement("ContinuePromptText", panelObj.transform,
            new Vector2(1, 0), new Vector2(-20, 10), new Vector2(300, 30));
        Text continuePromptText = continuePromptObj.AddComponent<Text>();
        continuePromptText.text = "按 L 键继续";
        continuePromptText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        continuePromptText.fontSize = 16;
        continuePromptText.color = new Color(0.7f, 0.7f, 0.7f);
        continuePromptText.alignment = TextAnchor.MiddleRight;
        continuePromptText.gameObject.SetActive(false);

        // 继续按钮
        GameObject continueButtonObj = CreateUIElement("ContinueButton", panelObj.transform,
            new Vector2(0.5f, 1), new Vector2(320, -8), new Vector2(120, 28));
        Image continueButtonImage = continueButtonObj.AddComponent<Image>();
        continueButtonImage.color = new Color(0.3f, 0.5f, 0.8f);
        Button continueButton = continueButtonObj.AddComponent<Button>();

        // 按钮文字 - 使用Legacy Text
        GameObject buttonTextObj = new GameObject("Text");
        buttonTextObj.transform.SetParent(continueButtonObj.transform, false);
        RectTransform buttonTextRect = buttonTextObj.AddComponent<RectTransform>();
        buttonTextRect.anchorMin = Vector2.zero;
        buttonTextRect.anchorMax = Vector2.one;
        buttonTextRect.sizeDelta = Vector2.zero;

        Text buttonText = buttonTextObj.AddComponent<Text>();
        buttonText.text = "继续";
        buttonText.font = Resources.GetBuiltinResource<Font>("Arial.ttf");
        buttonText.fontSize = 16;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;

        Debug.Log("GuideDialogueAutoSetup_Legacy: 导游对话UI创建完成（使用Legacy Text）");
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

    void TriggerWelcomeDialogue()
    {
        if (guideUI != null && welcomeDialogue != null && welcomeDialogue.Length > 0)
        {
            guideUI.StartGuideDialogue(guideName, null, welcomeDialogue);
        }
    }

    [ContextMenu("测试对话")]
    public void TestDialogue()
    {
        if (guideUI != null)
        {
            guideUI.StartGuideDialogue(guideName, null, welcomeDialogue);
        }
    }
}
