using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 对话管理器
/// 管理NPC对话的显示和交互
/// </summary>
public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("玩家引用")]
    [Tooltip("玩家控制器（如果为空则自动查找）")]
    public PlayerController playerController;

    [Header("UI引用")]
    [Tooltip("对话面板")]
    public GameObject dialoguePanel;

    [Tooltip("NPC名称文本")]
    public Text npcNameText;

    [Tooltip("对话内容文本")]
    public Text dialogueText;

    [Tooltip("选项按钮容器")]
    public Transform optionsContainer;

    [Tooltip("选项按钮预制体")]
    public GameObject optionButtonPrefab;

    [Header("设置")]
    [Tooltip("打字机效果速度")]
    public float typingSpeed = 0.05f;

    [Tooltip("自动关闭延迟（对话结束后）")]
    public float autoCloseDelay = 3f;

    [Tooltip("继续提示文本")]
    public Text continuePromptText;

    [Header("按键设置")]
    [Tooltip("推进对话的按键")]
    public KeyCode advanceKey = KeyCode.L;

    private bool isDialogueActive = false;
    private Coroutine typingCoroutine;
    private Coroutine autoCloseCoroutine;

    // 手动对话模式状态
    private string[] currentDialogueSequence;
    private int currentDialogueIndex = 0;
    private bool isTyping = false;
    private bool waitingForContinue = false;

    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // 强制设置按键为L键
        advanceKey = KeyCode.L;

        // 查找玩家控制器
        if (playerController == null)
        {
            playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                Debug.Log("DialogueManager: 自动找到PlayerController");
            }
            else
            {
                Debug.LogWarning("DialogueManager: 未找到PlayerController，对话时将无法锁定玩家移动");
            }
        }

        // 自动查找UI元素（如果没有手动赋值）
        AutoFindUIElements();

        // 检查UI元素是否正确加载
        bool allGood = true;
        if (dialoguePanel == null)
        {
            Debug.LogError("DialogueManager: dialoguePanel 为空！");
            allGood = false;
        }
        if (npcNameText == null)
        {
            Debug.LogError("DialogueManager: npcNameText 为空！");
            allGood = false;
        }
        if (dialogueText == null)
        {
            Debug.LogError("DialogueManager: dialogueText 为空！");
            allGood = false;
        }
        if (optionsContainer == null)
        {
            Debug.LogError("DialogueManager: optionsContainer 为空！");
            allGood = false;
        }
        if (optionButtonPrefab == null)
        {
            Debug.LogError("DialogueManager: optionButtonPrefab 为空！请确保预制体在 Assets/Resources/Prefabs/OptionButton");
            allGood = false;
        }

        if (allGood)
        {
            Debug.Log("DialogueManager: 所有UI元素已正确加载！");
        }

        // 初始化时隐藏对话框
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 初始化继续提示文本
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }
    }

    void Update()
    {
        // 检测推进对话按键
        if (isDialogueActive && Input.GetKeyDown(advanceKey))
        {
            if (isTyping)
            {
                // 如果正在打字，立即完成打字
                SkipTyping();
            }
            else if (waitingForContinue)
            {
                // 如果打字完成，推进到下一句
                AdvanceDialogue();
            }
        }
    }

    /// <summary>
    /// 跳过打字效果，立即显示全部文本
    /// </summary>
    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if (dialogueText != null && currentDialogueSequence != null && currentDialogueIndex < currentDialogueSequence.Length)
        {
            dialogueText.text = currentDialogueSequence[currentDialogueIndex];
            isTyping = false;

            Debug.Log("DialogueManager: 跳过打字效果，立即显示全部文本");

            // 显示继续提示
            if (continuePromptText != null)
            {
                continuePromptText.gameObject.SetActive(true);
            }

            waitingForContinue = true;
        }
    }

    /// <summary>
    /// 自动查找UI元素
    /// </summary>
    private void AutoFindUIElements()
    {
        // 查找DialoguePanel
        if (dialoguePanel == null)
        {
            GameObject panel = GameObject.Find("DialoguePanel");
            if (panel != null)
            {
                dialoguePanel = panel;
                Debug.Log("DialogueManager: 自动找到DialoguePanel");
            }
            else
            {
                Debug.LogError("DialogueManager: 未找到DialoguePanel！请确保场景中有名为DialoguePanel的对象。");
                return;
            }
        }

        // 查找NPCName Text
        if (npcNameText == null)
        {
            Transform nameTransform = dialoguePanel.transform.Find("NPCName");
            if (nameTransform != null)
            {
                npcNameText = nameTransform.GetComponent<Text>();
                if (npcNameText != null)
                {
                    Debug.Log("DialogueManager: 自动找到NPCName文本");
                }
            }
        }

        // 查找DialogueText Text
        if (dialogueText == null)
        {
            Transform textTransform = dialoguePanel.transform.Find("DialogueText");
            if (textTransform != null)
            {
                dialogueText = textTransform.GetComponent<Text>();
                if (dialogueText != null)
                {
                    Debug.Log("DialogueManager: 自动找到DialogueText");
                }
            }
        }

        // 查找OptionsContainer
        if (optionsContainer == null)
        {
            Transform containerTransform = dialoguePanel.transform.Find("OptionsContainer");
            if (containerTransform != null)
            {
                optionsContainer = containerTransform;
                Debug.Log("DialogueManager: 自动找到OptionsContainer");
            }
        }

        // 查找选项按钮预制体（从Resources文件夹加载）
        if (optionButtonPrefab == null)
        {
            optionButtonPrefab = Resources.Load<GameObject>("Prefabs/OptionButton");
            if (optionButtonPrefab != null)
            {
                Debug.Log("DialogueManager: 从Resources加载OptionButton预制体");
            }
            else
            {
                Debug.LogWarning("DialogueManager: 未找到OptionsButton预制体。请将预制体放在Assets/Resources/Prefabs/文件夹下，命名为'OptionButton'");
            }
        }

        // 查找继续提示文本
        if (continuePromptText == null)
        {
            Transform promptTransform = dialoguePanel.transform.Find("ContinuePrompt");
            if (promptTransform != null)
            {
                continuePromptText = promptTransform.GetComponent<Text>();
                if (continuePromptText != null)
                {
                    Debug.Log("DialogueManager: 自动找到ContinuePrompt文本");
                    // 设置默认提示文本
                    if (string.IsNullOrEmpty(continuePromptText.text))
                    {
                        continuePromptText.text = "按 L 键继续...";
                    }
                }
            }
        }
    }

    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialogue(string npcName, string greeting, DialogueOption[] options)
    {
        Debug.Log($"DialogueManager: 开始对话 - NPC: {npcName}");

        isDialogueActive = true;

        // 锁定玩家移动
        LockPlayerMovement();

        // 显示对话面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            Debug.Log("DialogueManager: 显示对话面板");
        }
        else
        {
            Debug.LogError("DialogueManager: dialoguePanel 为空，无法显示对话！");
            return;
        }

        // 设置NPC名称
        if (npcNameText != null)
        {
            npcNameText.text = npcName;
            Debug.Log($"DialogueManager: 设置NPC名称: {npcName}");
        }
        else
        {
            Debug.LogError("DialogueManager: npcNameText 为空！");
        }

        // 显示欢迎语
        ShowDialogue(greeting, options);
    }

    /// <summary>
    /// 开始自动对话序列（无选项）
    /// </summary>
    public void StartAutoDialogue(string npcName, string[] dialogueSequence)
    {
        Debug.Log($"DialogueManager: 开始自动对话序列 - NPC: {npcName}, 对话数量: {dialogueSequence?.Length ?? 0}");

        isDialogueActive = true;

        // 锁定玩家移动
        LockPlayerMovement();

        // 显示对话面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(true);
            Debug.Log("DialogueManager: 显示对话面板");
        }
        else
        {
            Debug.LogError("DialogueManager: dialoguePanel 为空，无法显示对话！");
            return;
        }

        // 设置NPC名称
        if (npcNameText != null)
        {
            npcNameText.text = npcName;
            Debug.Log($"DialogueManager: 设置NPC名称: {npcName}");
        }
        else
        {
            Debug.LogError("DialogueManager: npcNameText 为空！");
        }

        // 开始播放对话序列
        StartCoroutine(PlayDialogueSequence(dialogueSequence));
    }

    /// <summary>
    /// 播放对话序列协程
    /// </summary>
    private IEnumerator PlayDialogueSequence(string[] dialogueSequence)
    {
        if (dialogueSequence == null || dialogueSequence.Length == 0)
        {
            Debug.LogWarning("DialogueManager: 对话序列为空！");
            EndDialogue();
            yield break;
        }

        // 保存对话序列
        currentDialogueSequence = dialogueSequence;
        currentDialogueIndex = 0;

        // 清除之前的协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }

        // 显示第一句
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
            Debug.Log("DialogueManager: 对话序列播放完毕");
            autoCloseCoroutine = StartCoroutine(AutoCloseDialogue());
            return;
        }

        Debug.Log($"DialogueManager: 播放第 {currentDialogueIndex + 1}/{currentDialogueSequence.Length} 句对话");

        // 清除旧的选项按钮（如果有）
        if (optionsContainer != null)
        {
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // 隐藏继续提示
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }

        // 开始打字机效果
        waitingForContinue = false;
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        typingCoroutine = StartCoroutine(TypeTextWithContinue(currentDialogueSequence[currentDialogueIndex]));
    }

    /// <summary>
    /// 打字机效果（带继续等待）
    /// </summary>
    private IEnumerator TypeTextWithContinue(string text)
    {
        isTyping = true;

        if (dialogueText == null)
        {
            Debug.LogError("DialogueManager: dialogueText 为空，无法显示打字机效果！");
            yield break;
        }

        dialogueText.text = "";

        // 逐字显示文本
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        Debug.Log("DialogueManager: 打字机效果完成，等待玩家按空格键");

        // 显示继续提示
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(true);
        }

        // 等待玩家按空格键
        waitingForContinue = true;
    }

    /// <summary>
    /// 推进到下一句对话
    /// </summary>
    private void AdvanceDialogue()
    {
        Debug.Log("DialogueManager: 玩家按下L键，推进对话");

        waitingForContinue = false;

        // 隐藏继续提示
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }

        // 移动到下一句
        currentDialogueIndex++;

        // 检查是否已经显示完所有句子
        if (currentDialogueIndex >= currentDialogueSequence.Length)
        {
            // 对话结束，立即关闭对话框
            Debug.Log("DialogueManager: 对话序列播放完毕，立即关闭");
            EndDialogue();
        }
        else
        {
            // 显示下一句
            ShowCurrentSentence();
        }
    }

    /// <summary>
    /// 单句打字机效果
    /// </summary>
    private IEnumerator TypeTextSingle(string text)
    {
        Debug.Log($"DialogueManager: TypeTextSingle 开始，文本长度: {text?.Length ?? 0}");

        if (dialogueText == null)
        {
            Debug.LogError("DialogueManager: dialogueText 为空，无法显示打字机效果！");
            yield break;
        }

        dialogueText.text = "";

        // 逐字显示文本
        foreach (char c in text)
        {
            dialogueText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        Debug.Log("DialogueManager: 单句打字机效果完成");
    }

    /// <summary>
    /// 显示对话内容和选项
    /// </summary>
    private void ShowDialogue(string text, DialogueOption[] options)
    {
        Debug.Log($"DialogueManager: ShowDialogue 被调用，文本: '{text}'，选项数量: {(options != null ? options.Length : 0)}");

        // 清除之前的自动关闭
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }

        // 清除之前的打字机效果
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // 开始新的打字机效果
        typingCoroutine = StartCoroutine(TypeText(text, options));

        Debug.Log("DialogueManager: 打字机协程已启动");
    }

    /// <summary>
    /// 打字机效果协程
    /// </summary>
    private IEnumerator TypeText(string text, DialogueOption[] options)
    {
        Debug.Log($"DialogueManager: TypeText 开始，文本长度: {text?.Length ?? 0}，选项数量: {(options != null ? options.Length : 0)}");

        if (dialogueText == null)
        {
            Debug.LogError("DialogueManager: dialogueText 为空，无法显示打字机效果！");
            yield break;
        }

        dialogueText.text = "";
        int charCount = 0;

        // 逐字显示文本
        foreach (char c in text)
        {
            dialogueText.text += c;
            charCount++;

            // 每10个字符输出一次进度
            if (charCount % 10 == 0)
            {
                Debug.Log($"DialogueManager: 打字进度 {charCount}/{text.Length}");
            }

            yield return new WaitForSeconds(typingSpeed);
        }

        Debug.Log("DialogueManager: 打字机效果完成，准备显示选项");

        // 打字完成后显示选项
        ShowOptions(options);
    }

    /// <summary>
    /// 显示对话选项
    /// </summary>
    private void ShowOptions(DialogueOption[] options)
    {
        Debug.Log($"DialogueManager: ShowOptions 被调用，选项数量: {(options != null ? options.Length : 0)}");

        // 清除旧的选项按钮
        if (optionsContainer != null)
        {
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }

            // 创建新的选项按钮
            if (options != null && options.Length > 0)
            {
                Debug.Log($"DialogueManager: 开始创建 {options.Length} 个选项按钮");

                foreach (DialogueOption option in options)
                {
                    CreateOptionButton(option);
                }
            }
            else
            {
                Debug.Log("DialogueManager: 没有选项，将自动关闭对话框");
                // 如果没有选项，自动关闭对话框
                autoCloseCoroutine = StartCoroutine(AutoCloseDialogue());
            }
        }
        else
        {
            Debug.LogError("DialogueManager: optionsContainer 为空，无法显示选项！");
        }
    }

    /// <summary>
    /// 创建选项按钮
    /// </summary>
    private void CreateOptionButton(DialogueOption option)
    {
        if (optionButtonPrefab == null)
        {
            Debug.LogError("DialogueManager: optionButtonPrefab 为空，尝试从Resources加载");

            // 尝试从Resources加载
            optionButtonPrefab = Resources.Load<GameObject>("Prefabs/OptionButton");
            if (optionButtonPrefab == null)
            {
                Debug.LogError("DialogueManager: 无法从Resources加载OptionButton预制体！");
                return;
            }
            else
            {
                Debug.Log("DialogueManager: 成功从Resources加载OptionButton预制体");
            }
        }

        if (optionsContainer == null)
        {
            Debug.LogError("DialogueManager: optionsContainer 为空，无法创建选项按钮！");
            return;
        }

        Debug.Log($"DialogueManager: 创建选项按钮 - {option.optionText}");

        GameObject buttonObj = Instantiate(optionButtonPrefab, optionsContainer);
        Button button = buttonObj.GetComponent<Button>();

        // 更可靠的Text组件查找方式
        Text buttonText = null;

        // 方法1: 尝试在直接子对象中查找名为"Text"的对象
        Transform textTransform = buttonObj.transform.Find("Text");
        if (textTransform != null)
        {
            buttonText = textTransform.GetComponent<Text>();
        }

        // 方法2: 如果没找到，使用GetComponentInChildren
        if (buttonText == null)
        {
            buttonText = buttonObj.GetComponentInChildren<Text>(false); // false表示不包括非活动对象
        }

        // 方法3: 还是没找到，包括非活动对象
        if (buttonText == null)
        {
            buttonText = buttonObj.GetComponentInChildren<Text>(true);
        }

        if (buttonText != null)
        {
            buttonText.text = option.optionText;
            Debug.Log($"DialogueManager: 选项按钮文字已设置为: {option.optionText}");

            // 确保Text组件是激活的
            if (!buttonText.gameObject.activeSelf)
            {
                buttonText.gameObject.SetActive(true);
                Debug.Log("DialogueManager: Text对象已激活");
            }

            // 确保Text组件启用
            if (!buttonText.enabled)
            {
                buttonText.enabled = true;
                Debug.Log("DialogueManager: Text组件已启用");
            }
        }
        else
        {
            Debug.LogError("DialogueManager: 选项按钮中没有找到Text组件！请确保预制体包含Text组件。尝试查找的路径：buttonObj.transform.Find('Text') 或 GetComponentInChildren<Text>()");
        }

        if (button != null)
        {
            button.onClick.AddListener(() => OnOptionSelected(option));
            Debug.Log("DialogueManager: 选项按钮点击事件已添加");
        }
        else
        {
            Debug.LogWarning("DialogueManager: 选项按钮中没有找到Button组件");
        }
    }

    /// <summary>
    /// 当选择选项时
    /// </summary>
    private void OnOptionSelected(DialogueOption option)
    {
        Debug.Log($"DialogueManager: 选中选项 - {option.optionText}");

        // 清除选项
        if (optionsContainer != null)
        {
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // 显示NPC回复，并传入后续选项
        if (!string.IsNullOrEmpty(option.responseText))
        {
            // 检查是否有后续选项
            if (option.followUpOptions != null && option.followUpOptions.Length > 0)
            {
                Debug.Log($"DialogueManager: 显示回复并显示 {option.followUpOptions.Length} 个后续选项");
                ShowDialogue(option.responseText, option.followUpOptions);
            }
            else
            {
                Debug.Log("DialogueManager: 显示回复，无后续选项，将自动关闭");
                ShowDialogue(option.responseText, null); // null表示没有更多选项
            }
        }
        else
        {
            // 如果没有回复文本，直接关闭对话框
            EndDialogue();
        }
    }

    /// <summary>
    /// 自动关闭对话框协程
    /// </summary>
    private IEnumerator AutoCloseDialogue()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        EndDialogue();
    }

    /// <summary>
    /// 结束对话
    /// </summary>
    public void EndDialogue()
    {
        // 清除协程
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }

        isDialogueActive = false;
        isTyping = false;
        waitingForContinue = false;

        // 解锁玩家移动
        UnlockPlayerMovement();

        // 重置对话序列状态
        currentDialogueSequence = null;
        currentDialogueIndex = 0;

        // 隐藏对话面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }

        // 隐藏继续提示
        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }

        // 清除选项
        if (optionsContainer != null)
        {
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    /// <summary>
    /// 对话是否处于活动状态
    /// </summary>
    public bool IsDialogueActive => isDialogueActive;

    /// <summary>
    /// 锁定玩家移动（对话时使用）
    /// </summary>
    private void LockPlayerMovement()
    {
        if (playerController != null)
        {
            playerController.isInspecting = true;
            Debug.Log("DialogueManager: 已锁定玩家移动");
        }
        else
        {
            Debug.LogWarning("DialogueManager: 无法锁定玩家移动 - playerController为空");
        }
    }

    /// <summary>
    /// 解锁玩家移动（对话结束时使用）
    /// </summary>
    private void UnlockPlayerMovement()
    {
        if (playerController != null)
        {
            playerController.isInspecting = false;
            Debug.Log("DialogueManager: 已解锁玩家移动");
        }
        else
        {
            Debug.LogWarning("DialogueManager: 无法解锁玩家移动 - playerController为空");
        }
    }
}
