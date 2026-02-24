using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 京派建筑知识问答游戏 - 自动设置版
/// 只需将此脚本挂载到场景中的空GameObject上，运行时会自动创建所有UI
/// </summary>
public class QuizGameAutoSetup : MonoBehaviour
{
    [Header("--- 自动创建开关 ---")]
    [Tooltip("勾选此项会在Start时自动创建所有UI")]
    public bool autoCreateOnStart = true;

    [Header("--- 题目数据 ---")]
    public QuestionData[] questions;

    [Header("--- 样式设置 ---")]
    public Color panelBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.95f);
    public Color buttonNormalColor = new Color(0.2f, 0.4f, 0.8f);
    public Color buttonHoverColor = new Color(0.3f, 0.6f, 1f);
    public Color correctColor = new Color(0.2f, 0.8f, 0.2f);
    public Color wrongColor = new Color(0.9f, 0.2f, 0.2f);
    public Color textColor = Color.white;

    [Header("--- 游戏设置 ---")]
    public float feedbackDuration = 3f;

    // 内部组件引用
    private QuizManager quizManager;
    private QuizTrigger quizTrigger;

    private void Start()
    {
        if (autoCreateOnStart)
        {
            // 使用默认题目（如果没有手动设置）
            if (questions == null || questions.Length == 0)
            {
                questions = JingQuizData.GetDefaultQuestions();
            }

            CreateQuizSystem();
        }
    }

    /// <summary>
    /// 创建完整的问答系统
    /// </summary>
    private void CreateQuizSystem()
    {
        Debug.Log("[QuizGameAutoSetup] 开始创建问答游戏系统...");

        // 1. 查找或创建Canvas
        Canvas canvas = FindOrCreateCanvas();

        // 2. 创建QuizManager和UI
        CreateQuizManager(canvas);

        // 3. 创建触发器
        CreateQuizTrigger();

        Debug.Log("[QuizGameAutoSetup] 问答游戏系统创建完成！");
    }

    /// <summary>
    /// 查找或创建Canvas
    /// </summary>
    private Canvas FindOrCreateCanvas()
    {
        // 先查找现有Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        Canvas canvas = null;

        foreach (Canvas c in canvases)
        {
            if (c.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                canvas = c;
                break;
            }
        }

        // 如果没有找到，创建新的
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Quiz Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 添加EventSystem（如果不存在）
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }

        return canvas;
    }

    /// <summary>
    /// 创建QuizManager和所有UI
    /// </summary>
    private void CreateQuizManager(Canvas canvas)
    {
        // 创建QuizManager对象
        GameObject managerObj = new GameObject("QuizManager");
        managerObj.transform.SetParent(canvas.transform);
        managerObj.transform.localPosition = Vector3.zero;

        quizManager = managerObj.AddComponent<QuizManager>();
        quizManager.questions = questions;

        // 添加AudioSource
        AudioSource audioSource = managerObj.AddComponent<AudioSource>();
        quizManager.audioSource = audioSource;

        // 创建主面板
        GameObject quizPanel = CreatePanel(canvas.transform, "QuizPanel", panelBackgroundColor);
        quizPanel.SetActive(false);
        quizManager.quizPanel = quizPanel;

        // 创建标题
        GameObject titleObj = CreateText(quizPanel.transform, "Title", "京派建筑知识问答", 48, TextAlignmentOptions.Center);
        SetUIPosition(titleObj, new Vector2(0, 400), new Vector2(800, 80));

        // 题目文本
        GameObject questionTextObj = CreateText(quizPanel.transform, "QuestionText", "", 32, TextAlignmentOptions.Center);
        SetUIPosition(questionTextObj, new Vector2(0, 280), new Vector2(700, 100));
        quizManager.questionText = questionTextObj.GetComponent<TextMeshProUGUI>();

        // 分数和进度
        GameObject scoreTextObj = CreateText(quizPanel.transform, "ScoreText", "得分: 0", 24, TextAlignmentOptions.Left);
        SetUIPosition(scoreTextObj, new Vector2(-350, 350), new Vector2(200, 50));
        quizManager.scoreText = scoreTextObj.GetComponent<TextMeshProUGUI>();

        GameObject progressTextObj = CreateText(quizPanel.transform, "ProgressText", "1 / 8", 24, TextAlignmentOptions.Right);
        SetUIPosition(progressTextObj, new Vector2(350, 350), new Vector2(200, 50));
        quizManager.progressText = progressTextObj.GetComponent<TextMeshProUGUI>();

        // 创建答案按钮
        GameObject buttonContainer = new GameObject("ButtonContainer");
        buttonContainer.transform.SetParent(quizPanel.transform, false);
        RectTransform containerRect = buttonContainer.AddComponent<RectTransform>();
        containerRect.anchorMin = new Vector2(0.5f, 0.5f);
        containerRect.anchorMax = new Vector2(0.5f, 0.5f);
        containerRect.anchoredPosition = new Vector2(0, -50);
        containerRect.sizeDelta = new Vector2(600, 400);

        quizManager.answerButtons = new QuizButton[4];
        string[] optionLabels = { "A", "B", "C", "D" };

        for (int i = 0; i < 4; i++)
        {
            GameObject buttonObj = CreateButton(containerRect.transform, $"Button{optionLabels[i]}", buttonNormalColor, textColor);
            RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
            buttonRect.anchoredPosition = new Vector2(0, 120 - i * 90);
            buttonRect.sizeDelta = new Vector2(500, 70);

            QuizButton quizButton = buttonObj.GetComponent<QuizButton>();
            quizButton.button = buttonObj.GetComponent<Button>();
            quizButton.backgroundImage = buttonObj.GetComponent<Image>();
            quizButton.buttonText = buttonObj.GetComponentInChildren<TextMeshProUGUI>();
            quizButton.defaultColor = buttonNormalColor;
            quizButton.correctColor = correctColor;
            quizButton.wrongColor = wrongColor;

            quizManager.answerButtons[i] = quizButton;
        }

        // 创建反馈面板
        GameObject feedbackPanel = CreatePanel(canvas.transform, "FeedbackPanel", new Color(0.1f, 0.1f, 0.1f, 0.98f));
        feedbackPanel.SetActive(false);
        quizManager.feedbackPanel = feedbackPanel;

        GameObject feedbackTitleObj = CreateText(feedbackPanel.transform, "FeedbackTitle", "", 48, TextAlignmentOptions.Center);
        SetUIPosition(feedbackTitleObj, new Vector2(0, 150), new Vector2(600, 80));
        quizManager.feedbackText = feedbackTitleObj.GetComponent<TextMeshProUGUI>();

        GameObject explanationObj = CreateText(feedbackPanel.transform, "ExplanationText", "", 24, TextAlignmentOptions.Center);
        SetUIPosition(explanationObj, new Vector2(0, -50), new Vector2(700, 200));
        quizManager.explanationText = explanationObj.GetComponent<TextMeshProUGUI>();

        GameObject continueObj = CreateText(feedbackPanel.transform, "ContinueText", "即将进入下一题...", 20, TextAlignmentOptions.Center);
        SetUIPosition(continueObj, new Vector2(0, -200), new Vector2(400, 50));
        continueObj.GetComponent<TextMeshProUGUI>().color = new Color(0.7f, 0.7f, 0.7f);
        quizManager.continueText = continueObj.GetComponent<TextMeshProUGUI>();

        // 创建结果面板
        GameObject resultPanel = CreatePanel(canvas.transform, "ResultPanel", new Color(0.1f, 0.1f, 0.1f, 0.98f));
        resultPanel.SetActive(false);
        quizManager.resultPanel = resultPanel;

        GameObject resultTitleObj = CreateText(resultPanel.transform, "ResultTitle", "完成！", 56, TextAlignmentOptions.Center);
        SetUIPosition(resultTitleObj, new Vector2(0, 200), new Vector2(600, 100));
        quizManager.resultTitleText = resultTitleObj.GetComponent<TextMeshProUGUI>();

        GameObject resultScoreObj = CreateText(resultPanel.transform, "ResultScore", "", 36, TextAlignmentOptions.Center);
        SetUIPosition(resultScoreObj, new Vector2(0, 50), new Vector2(600, 80));
        quizManager.resultScoreText = resultScoreObj.GetComponent<TextMeshProUGUI>();

        GameObject resultMessageObj = CreateText(resultPanel.transform, "ResultMessage", "", 24, TextAlignmentOptions.Center);
        SetUIPosition(resultMessageObj, new Vector2(0, -80), new Vector2(700, 150));
        quizManager.resultMessageText = resultMessageObj.GetComponent<TextMeshProUGUI>();

        // 重玩按钮
        GameObject restartButtonObj = CreateButton(resultPanel.transform, "RestartButton", new Color(0.2f, 0.6f, 0.3f), Color.white);
        SetUIPosition(restartButtonObj, new Vector2(0, -250), new Vector2(200, 60));
        restartButtonObj.GetComponentInChildren<TextMeshProUGUI>().text = "重新开始";
        restartButtonObj.GetComponent<Button>().onClick.AddListener(() => quizManager.RestartQuiz());

        // 关闭按钮
        GameObject closeButtonObj = CreateButton(resultPanel.transform, "CloseButton", new Color(0.6f, 0.2f, 0.2f), Color.white);
        SetUIPosition(closeButtonObj, new Vector2(250, -250), new Vector2(150, 60));
        closeButtonObj.GetComponentInChildren<TextMeshProUGUI>().text = "关闭";
        closeButtonObj.GetComponent<Button>().onClick.AddListener(() => quizManager.CloseQuiz());

        // 设置游戏参数
        quizManager.feedbackDuration = feedbackDuration;
        quizManager.correctColor = correctColor;
        quizManager.wrongColor = wrongColor;
        quizManager.defaultColor = buttonNormalColor;
    }

    /// <summary>
    /// 创建触发器
    /// </summary>
    private void CreateQuizTrigger()
    {
        // 创建触发器对象（在Cube位置附近）
        GameObject triggerObj = new GameObject("QuizTrigger");
        triggerObj.transform.position = new Vector3(0, 1, 0); // 默认位置，可以手动调整

        // 添加Box Collider作为触发区域
        BoxCollider collider = triggerObj.AddComponent<BoxCollider>();
        collider.size = new Vector3(3, 2, 3);
        collider.isTrigger = true;

        // 添加QuizTrigger脚本
        quizTrigger = triggerObj.AddComponent<QuizTrigger>();
        quizTrigger.quizManager = quizManager;
        quizTrigger.playerLayer = 1; // Default layer，可以根据实际调整

        // 创建提示UI
        Canvas canvas = quizManager.quizPanel.GetComponentInParent<Canvas>();
        GameObject promptPanel = CreatePanel(canvas.transform, "PromptPanel", new Color(0.2f, 0.2f, 0.2f, 0.9f));
        promptPanel.SetActive(false);
        quizTrigger.promptPanel = promptPanel;

        GameObject promptTextObj = CreateText(promptPanel.transform, "PromptText", "按 [E] 键开始京派建筑知识问答", 20, TextAlignmentOptions.Center);
        SetUIPosition(promptTextObj, Vector2.zero, new Vector2(400, 60));
        quizTrigger.promptText = promptTextObj.GetComponent<TextMeshProUGUI>();
    }

    // ==================== 辅助方法 ====================

    private GameObject CreatePanel(Transform parent, string name, Color backgroundColor)
    {
        GameObject panelObj = new GameObject(name);
        panelObj.transform.SetParent(parent, false);

        RectTransform rect = panelObj.AddComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        Image image = panelObj.AddComponent<Image>();
        image.color = backgroundColor;

        return panelObj;
    }

    private GameObject CreateText(Transform parent, string name, string content, int fontSize, TextAlignmentOptions alignment)
    {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);

        RectTransform rect = textObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = content;
        text.fontSize = fontSize;
        text.alignment = alignment;
        text.color = textColor;

        return textObj;
    }

    private GameObject CreateButton(Transform parent, string name, Color backgroundColor, Color textColor)
    {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);

        RectTransform rect = buttonObj.AddComponent<RectTransform>();
        rect.sizeDelta = new Vector2(200, 50);

        Image image = buttonObj.AddComponent<Image>();
        image.color = backgroundColor;

        Button button = buttonObj.AddComponent<Button>();

        // 创建按钮文本
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        textRect.offsetMin = new Vector2(10, 5);
        textRect.offsetMax = new Vector2(-10, -5);

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "选项";
        text.fontSize = 24;
        text.alignment = TextAlignmentOptions.Center;
        text.color = textColor;

        // 添加QuizButton组件
        QuizButton quizButton = buttonObj.AddComponent<QuizButton>();

        // 添加颜色过渡效果
        ColorBlock colors = button.colors;
        colors.normalColor = backgroundColor;
        colors.highlightedColor = new Color(backgroundColor.r + 0.2f, backgroundColor.g + 0.2f, backgroundColor.b + 0.2f);
        colors.pressedColor = new Color(backgroundColor.r - 0.1f, backgroundColor.g - 0.1f, backgroundColor.b - 0.1f);
        colors.selectedColor = backgroundColor;
        colors.disabledColor = new Color(0.5f, 0.5f, 0.5f);
        colors.colorMultiplier = 1;
        button.colors = colors;

        return buttonObj;
    }

    private void SetUIPosition(GameObject obj, Vector2 anchoredPosition, Vector2 sizeDelta)
    {
        RectTransform rect = obj.GetComponent<RectTransform>();
        if (rect != null)
        {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = sizeDelta;
        }
    }
}
