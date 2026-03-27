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

    private bool isDialogueActive = false;
    private Coroutine typingCoroutine;
    private Coroutine autoCloseCoroutine;

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
    }

    /// <summary>
    /// 开始对话
    /// </summary>
    public void StartDialogue(string npcName, string greeting, DialogueOption[] options)
    {
        Debug.Log($"DialogueManager: 开始对话 - NPC: {npcName}");

        isDialogueActive = true;

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
        Text buttonText = buttonObj.GetComponentInChildren<Text>();

        if (buttonText != null)
        {
            buttonText.text = option.optionText;
            Debug.Log($"DialogueManager: 选项按钮文字已设置为: {option.optionText}");
        }
        else
        {
            Debug.LogWarning("DialogueManager: 选项按钮中没有找到Text组件");
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
        // 清除选项
        if (optionsContainer != null)
        {
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }

        // 显示NPC回复
        if (!string.IsNullOrEmpty(option.responseText))
        {
            ShowDialogue(option.responseText, null); // null表示没有更多选项
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

        // 隐藏对话面板
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
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
}
