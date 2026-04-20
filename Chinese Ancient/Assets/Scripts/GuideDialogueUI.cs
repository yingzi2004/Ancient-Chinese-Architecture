using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 导游对话UI控制器
/// 在屏幕下方显示带立绘的导游对话
/// 支持TextMeshPro和Legacy UI Text
/// </summary>
public class GuideDialogueUI : MonoBehaviour
{
    public static GuideDialogueUI Instance { get; private set; }

    [Header("UI引用")]
    [SerializeField] private GameObject dialoguePanel;
    [SerializeField] private Image portraitImage; // 立绘图片

    // TextMeshPro组件
    [SerializeField] private TextMeshProUGUI guideNameTextTMP;
    [SerializeField] private TextMeshProUGUI dialogueTextTMP;
    [SerializeField] private TextMeshProUGUI continuePromptTextTMP;

    // Legacy UI Text组件
    [SerializeField] private Text guideNameTextLegacy;
    [SerializeField] private Text dialogueTextLegacy;
    [SerializeField] private Text continuePromptTextLegacy;

    [SerializeField] private Button continueButton;

    [Header("设置")]
    [SerializeField] private float typingSpeed = 0.05f;
    [SerializeField] private KeyCode advanceKey = KeyCode.L;

    private bool isDialogueActive = false;
    private bool isTyping = false;
    private Coroutine typingCoroutine;
    private string[] currentDialogueSequence;
    private int currentDialogueIndex = 0;
    private PlayerController playerController;
    private Sprite defaultPortrait; // 保存默认立绘
    private Sprite[] currentExpressions; // 当前对话序列对应的表情差分
    private System.Action currentDialogueCallback;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject); // 可选：跨场景保持
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 查找玩家控制器
        playerController = FindObjectOfType<PlayerController>();

        // 初始化时隐藏对话框
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 绑定继续按钮
        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }
    }

    void Update()
    {
        if (isDialogueActive && Input.GetKeyDown(advanceKey))
        {
            AdvanceDialogue();
        }
    }

    /// <summary>
    /// 开始导游对话
    /// </summary>
    public void StartGuideDialogue(string guideName, Sprite portraitSprite, string[] dialogueSequence, Sprite[] expressionPortraits = null, System.Action onComplete = null)
    {
        currentDialogueCallback = onComplete;
        if (dialogueSequence == null || dialogueSequence.Length == 0)
        {
            Debug.LogWarning("GuideDialogueUI: 对话序列为空！");
            return;
        }

        Debug.Log($"GuideDialogueUI: 开始导游对话 - 导游: {guideName}, 对话数量: {dialogueSequence.Length}");

        isDialogueActive = true;

        // 锁定玩家移动
        LockPlayerMovement();

        // 显示对话面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
        }

        // 设置导游名称
        SetGuideNameText(guideName);

        // 保存默认立绘和表情差分
        defaultPortrait = portraitSprite;
        currentExpressions = expressionPortraits;

        // 设置立绘图片
        if (portraitImage != null && portraitSprite != null)
        {
            portraitImage.sprite = portraitSprite;
            portraitImage.gameObject.SetActive(true);
        }
        else if (portraitImage != null)
        {
            portraitImage.gameObject.SetActive(false);
        }

        // 保存对话序列
        currentDialogueSequence = dialogueSequence;
        currentDialogueIndex = 0;

        // 显示第一句对话
        ShowCurrentSentence();
    }

    /// <summary>
    /// 显示当前句子
    /// </summary>
    private void ShowCurrentSentence()
    {
        if (currentDialogueSequence == null || currentDialogueIndex >= currentDialogueSequence.Length)
        {
            // 所有句子已显示完毕
            Debug.Log("GuideDialogueUI: 对话序列播放完毕");
            EndDialogue();
            return;
        }

        Debug.Log($"GuideDialogueUI: 播放第 {currentDialogueIndex + 1}/{currentDialogueSequence.Length} 句对话");

        // 尝试切换表情差分
        if (portraitImage != null)
        {
            if (currentExpressions != null && currentDialogueIndex < currentExpressions.Length && currentExpressions[currentDialogueIndex] != null)
            {
                portraitImage.sprite = currentExpressions[currentDialogueIndex];
                portraitImage.gameObject.SetActive(true);
            }
            else if (defaultPortrait != null)
            {
                portraitImage.sprite = defaultPortrait;
                portraitImage.gameObject.SetActive(true);
            }
        }

        // 隐藏继续提示
        SetContinuePromptActive(false);

        // 开始打字机效果
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeText(currentDialogueSequence[currentDialogueIndex]));
    }

    /// <summary>
    /// 打字机效果
    /// </summary>
    private IEnumerator TypeText(string text)
    {
        isTyping = true;

        if (!HasDialogueTextComponent())
        {
            Debug.LogError("GuideDialogueUI: dialogueText 为空！");
            yield break;
        }

        SetDialogueText("");

        // 逐字显示文本（支持富文本标签）
        string currentText = "";
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '<')
            {
                int closingIndex = text.IndexOf('>', i);
                if (closingIndex != -1)
                {
                    string tag = text.Substring(i, closingIndex - i + 1);
                    currentText += tag;
                    SetDialogueText(currentText);
                    i = closingIndex;
                    continue; // 标签内容不消耗打字时间
                }
            }

            currentText += c;
            SetDialogueText(currentText);
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        Debug.Log("GuideDialogueUI: 打字机效果完成");

        // 显示继续提示
        SetContinuePromptActive(true);
        SetContinuePromptText($"按 {advanceKey} 键继续...");
    }

    /// <summary>
    /// 推进到下一句对话
    /// </summary>
    private void AdvanceDialogue()
    {
        if (!isDialogueActive) return;

        if (isTyping)
        {
            // 如果正在打字，立即完成打字
            SkipTyping();
        }
        else
        {
            // 移动到下一句
            currentDialogueIndex++;

            // 检查是否已经显示完所有句子
            if (currentDialogueIndex >= currentDialogueSequence.Length)
            {
                // 对话结束
                EndDialogue();
            }
            else
            {
                // 显示下一句
                ShowCurrentSentence();
            }
        }
    }

    /// <summary>
    /// 跳过打字效果
    /// </summary>
    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (HasDialogueTextComponent() && currentDialogueSequence != null && currentDialogueIndex < currentDialogueSequence.Length)
        {
            SetDialogueText(currentDialogueSequence[currentDialogueIndex]);
            isTyping = false;

            // 显示继续提示
            SetContinuePromptActive(true);
            SetContinuePromptText($"按 {advanceKey} 键继续...");
        }
    }

    /// <summary>
    /// 继续按钮点击事件
    /// </summary>
    private void OnContinueClicked()
    {
        AdvanceDialogue();
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    public void EndDialogue()
    {
        Debug.Log("GuideDialogueUI: 结束对话");

        isDialogueActive = false;
        isTyping = false;

        // 清除协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // 解锁玩家移动
        UnlockPlayerMovement();

        // 隐藏对话面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 隐藏继续提示
        SetContinuePromptActive(false);

        // 重置对话序列状态
        currentDialogueSequence = null;
        currentDialogueIndex = 0;

        if (currentDialogueCallback != null)
        {
            var cb = currentDialogueCallback;
            currentDialogueCallback = null;
            cb?.Invoke();
        }
    }

    /// <summary>
    /// 锁定玩家移动
    /// </summary>
    private void LockPlayerMovement()
    {
        if (playerController != null)
        {
            playerController.isInspecting = true;
            Debug.Log("GuideDialogueUI: 已锁定玩家移动");
        }
    }

    /// <summary>
    /// 解锁玩家移动
    /// </summary>
    private void UnlockPlayerMovement()
    {
        if (playerController != null)
        {
            playerController.isInspecting = false;
            Debug.Log("GuideDialogueUI: 已解锁玩家移动");
        }
    }

    /// <summary>
    /// 对话是否处于活动状态
    /// </summary>
    public bool IsDialogueActive => isDialogueActive;

    /// <summary>
    /// 设置UI组件（用于运行时动态绑定）- TextMeshPro版本
    /// </summary>
    public void SetUIComponents(GameObject panel, Image portrait, TextMeshProUGUI nameText,
        TextMeshProUGUI dialogue, TextMeshProUGUI prompt, Button button)
    {
        dialoguePanel = panel;
        portraitImage = portrait;
        guideNameTextTMP = nameText;
        dialogueTextTMP = dialogue;
        continuePromptTextTMP = prompt;
        continueButton = button;

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        Debug.Log("GuideDialogueUI: UI组件已绑定 (TextMeshPro)");
    }

    /// <summary>
    /// 设置UI组件（用于运行时动态绑定）- Legacy Text版本
    /// </summary>
    public void SetUIComponents(GameObject panel, Image portrait, Text nameText,
        Text dialogue, Text prompt, Button button)
    {
        dialoguePanel = panel;
        portraitImage = portrait;
        guideNameTextLegacy = nameText;
        dialogueTextLegacy = dialogue;
        continuePromptTextLegacy = prompt;
        continueButton = button;

        if (continueButton != null)
        {
            continueButton.onClick.AddListener(OnContinueClicked);
        }

        Debug.Log("GuideDialogueUI: UI组件已绑定 (Legacy Text)");
    }

    /// <summary>
    /// 设置导游名称文本
    /// </summary>
    private void SetGuideNameText(string name)
    {
        if (guideNameTextTMP != null)
        {
            guideNameTextTMP.text = name;
        }
        else if (guideNameTextLegacy != null)
        {
            guideNameTextLegacy.text = name;
        }
    }

    /// <summary>
    /// 获取对话文本组件（支持两种类型）
    /// </summary>
    private string GetDialogueText()
    {
        if (dialogueTextTMP != null) return dialogueTextTMP.text;
        if (dialogueTextLegacy != null) return dialogueTextLegacy.text;
        return "";
    }

    /// <summary>
    /// 设置对话文本
    /// </summary>
    private void SetDialogueText(string text)
    {
        if (dialogueTextTMP != null) dialogueTextTMP.text = text;
        if (dialogueTextLegacy != null) dialogueTextLegacy.text = text;
    }

    /// <summary>
    /// 显示/隐藏继续提示
    /// </summary>
    private void SetContinuePromptActive(bool active)
    {
        if (continuePromptTextTMP != null) continuePromptTextTMP.gameObject.SetActive(active);
        if (continuePromptTextLegacy != null) continuePromptTextLegacy.gameObject.SetActive(active);
    }

    /// <summary>
    /// 设置继续提示文本
    /// </summary>
    private void SetContinuePromptText(string text)
    {
        if (continuePromptTextTMP != null) continuePromptTextTMP.text = text;
        if (continuePromptTextLegacy != null) continuePromptTextLegacy.text = text;
    }

    /// <summary>
    /// 检查是否有对话文本组件
    /// </summary>
    private bool HasDialogueTextComponent()
    {
        return dialogueTextTMP != null || dialogueTextLegacy != null;
    }
}
