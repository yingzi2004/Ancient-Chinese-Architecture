using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueManager : MonoBehaviour
{
    public static DialogueManager Instance { get; private set; }

    [Header("玩家引用")]
    public PlayerController playerController;

    [Header("UI引用")]
    public GameObject dialoguePanel;

    public Text npcNameText;
    public TMP_Text npcNameTextTMP;

    public Text dialogueText;
    public TMP_Text dialogueTextTMP;

    public Image portraitImage;

    [Header("说话人区分")]
    public string playerName = "我";

    public bool parseSpeakerPrefix = true;

    public Color npcDialogueColor = Color.white;

    public Color playerDialogueColor = new Color(0.8f, 0.95f, 1f, 1f);

    public bool forcePlayerName = true;

    public bool replaceNpcTagWithCurrentNpcName = true;

    public bool replacePlayerTagWithPlayerName = true;

    public int maxSpeakerPrefixLength = 8;

    public string speakerSeparators = ":：";

    public string defaultNpcName = "NPC";

    public Color npcNameColor = Color.white;

    public Color playerNameColor = Color.white;

    public bool tintNameColor = false;

    public Transform optionsContainer;

    public GameObject optionButtonPrefab;

    [Header("设置")]
    public float typingSpeed = 0.05f;

    public float autoCloseDelay = 3f;

    public bool showContinuePrompt = true;

    public Text continuePromptText;

    [Header("按键设置")]
    public KeyCode advanceKey = KeyCode.L;

    private bool isDialogueActive = false;
    private Coroutine typingCoroutine;
    private Coroutine autoCloseCoroutine;

    // 手动对话模式状态
    private string[] currentDialogueSequence;
    private int currentDialogueIndex = 0;
    private bool isTyping = false;
    private bool waitingForContinue = false;

    // 当前上下文
    private string currentNpcName;
    private string currentPreparedText;
    private bool currentPreparedIsPlayer;
    private string currentPreparedSpeaker;

    private Sprite defaultPortrait; 
    private Sprite[] currentExpressions; 
    
    private int currentPortraitRevealIndex = 0; 
    private int currentNameRevealIndex = 0; // 当前序列的名字隐藏阈值
    private System.Action currentDialogueCallback; // 当前对话结束的回调

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

        // 自动查找UI元素
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
            if (!showContinuePrompt)
            {
                continuePromptText.text = string.Empty;
            }
        }

        // 默认开启说话人前缀解析
        parseSpeakerPrefix = true;
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

    private void SkipTyping()
    {
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        if ((dialogueText != null || dialogueTextTMP != null) && currentDialogueSequence != null && currentDialogueIndex < currentDialogueSequence.Length)
        {
            // 使用已准备好的文本
            if (string.IsNullOrEmpty(currentPreparedText))
            {
                PrepareLine(currentDialogueSequence[currentDialogueIndex]);
            }

            if (dialogueText != null) dialogueText.text = currentPreparedText;
            if (dialogueTextTMP != null) dialogueTextTMP.text = currentPreparedText;
            isTyping = false;

            Debug.Log("DialogueManager: 跳过打字效果，立即显示全部文本");

            // 显示继续提示
            if (showContinuePrompt && continuePromptText != null)
            {
                continuePromptText.gameObject.SetActive(true);
            }

            waitingForContinue = true;
        }
    }

    private void AutoFindUIElements()
    {
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
        if (npcNameText == null && npcNameTextTMP == null && dialoguePanel != null)
        {
            Transform nameTransform = dialoguePanel.transform.Find("NPCName");
            if (nameTransform == null) nameTransform = dialoguePanel.transform.Find("GuideNameText");

            if (nameTransform == null)
            {
                foreach (Text t in dialoguePanel.GetComponentsInChildren<Text>(true))
                {
                    if (t.name.Contains("NPCName") || t.name.Contains("GuideNameText") || t.name.Contains("NameText"))
                    {
                        npcNameText = t;
                        break;
                    }
                }
                foreach (TMP_Text t in dialoguePanel.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.name.Contains("NPCName") || t.name.Contains("GuideNameText") || t.name.Contains("NameText"))
                    {
                        npcNameTextTMP = t;
                        break;
                    }
                }
            }

            if (nameTransform != null)
            {
                if (npcNameText == null) npcNameText = nameTransform.GetComponent<Text>();
                if (npcNameTextTMP == null) npcNameTextTMP = nameTransform.GetComponent<TMP_Text>();
            }

            if (npcNameText != null || npcNameTextTMP != null)
            {
                Debug.Log("DialogueManager: 自动找到NPCName文本");
            }
        }

        if (dialogueText == null && dialogueTextTMP == null && dialoguePanel != null)
        {
            Transform textTransform = dialoguePanel.transform.Find("DialogueText");
            
            if (textTransform == null)
            {
                foreach (Text t in dialoguePanel.GetComponentsInChildren<Text>(true))
                {
                    if (t.name.Contains("DialogueText") || t.name.Contains("ContentText"))
                    {
                        dialogueText = t;
                        break;
                    }
                }
                foreach (TMP_Text t in dialoguePanel.GetComponentsInChildren<TMP_Text>(true))
                {
                    if (t.name.Contains("DialogueText") || t.name.Contains("ContentText"))
                    {
                        dialogueTextTMP = t;
                        break;
                    }
                }
            }

            if (textTransform != null)
            {
                if (dialogueText == null) dialogueText = textTransform.GetComponent<Text>();
                if (dialogueTextTMP == null) dialogueTextTMP = textTransform.GetComponent<TMP_Text>();
            }

            if (dialogueText != null || dialogueTextTMP != null)
            {
                Debug.Log("DialogueManager: 自动找到DialogueText");
            }
        }

        if (optionsContainer == null)
        {
            Transform containerTransform = dialoguePanel.transform.Find("OptionsContainer");
            if (containerTransform != null)
            {
                optionsContainer = containerTransform;
                Debug.Log("DialogueManager: 自动找到OptionsContainer");
            }
        }

        // 查找选项按钮预制体
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
        if (continuePromptText == null && dialoguePanel != null)
        {
            Transform promptTransform = dialoguePanel.transform.Find("ContinuePrompt");
            if (promptTransform == null) promptTransform = dialoguePanel.transform.Find("ContinuePromptText");
            
            // 尝试深度查找
            if (promptTransform == null)
            {
                foreach (Text t in dialoguePanel.GetComponentsInChildren<Text>(true))
                {
                    if (t.name.Contains("ContinuePrompt") || t.text.Contains("按L键继续") || t.text.Contains("按 L 键继续"))
                    {
                        continuePromptText = t;
                        break;
                    }
                }
            }

            if (promptTransform != null && continuePromptText == null)
            {
                continuePromptText = promptTransform.GetComponent<Text>();
            }

            if (continuePromptText != null)
            {
                Debug.Log("DialogueManager: 自动找到ContinuePrompt文本");
                if (!showContinuePrompt)
                {
                    continuePromptText.text = string.Empty;
                    continuePromptText.gameObject.SetActive(false);
                }
            }
        }
        
        // 暴力清理场景中不该带 "按 L 键继续" 的 NPC_NameText，以防残留
        if (npcNameText != null && npcNameText.text.Contains("按L键继续"))
        {
            npcNameText.text = npcNameText.text.Replace("按L键继续", "").Replace("按 L 键继续", "").Trim();
        }
        if (npcNameTextTMP != null && npcNameTextTMP.text.Contains("按L键继续"))
        {
            npcNameTextTMP.text = npcNameTextTMP.text.Replace("按L键继续", "").Replace("按 L 键继续", "").Trim();
        }
    }

    public void StartDialogue(string npcName, string greeting, DialogueOption[] options, System.Action onComplete = null)
    {
        currentDialogueCallback = onComplete;
        Debug.Log($"DialogueManager: 开始对话 - NPC: {npcName}");

        if (!string.IsNullOrEmpty(npcName))
        {
            npcName = npcName.Replace("按L键继续", "").Replace("按 L 键继续", "").Replace("...", "").Trim();
        }

        currentNpcName = string.IsNullOrEmpty(npcName) ? defaultNpcName : npcName;

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

        // 默认先显示NPC名
        if (npcNameText != null) npcNameText.text = currentNpcName;
        if (npcNameTextTMP != null) npcNameTextTMP.text = currentNpcName;


        // 显示欢迎语
        ShowDialogue(greeting, options);
    }

    public void StartAutoDialogue(string npcName, string[] dialogueSequence, Sprite defaultPortrait = null, Sprite[] expressionPortraits = null, System.Action onComplete = null, int portraitRevealIndex = 0, int nameRevealIndex = 0)
    {
        currentDialogueCallback = onComplete;
        this.currentPortraitRevealIndex = portraitRevealIndex;
        this.currentNameRevealIndex = nameRevealIndex;

        Debug.Log($"DialogueManager: 开始自动对话序列 - NPC: {npcName}, 对话数量: {dialogueSequence?.Length ?? 0}");

        // 如果未绑定 portraitImage，尝试找一找
        if (portraitImage == null && dialoguePanel != null)
        {
            Transform p = dialoguePanel.transform.Find("PortraitImage");
            if (p != null) portraitImage = p.GetComponent<Image>();
        }

        // 记录立绘配置
        this.defaultPortrait = defaultPortrait;
        this.currentExpressions = expressionPortraits;

        if (portraitImage != null)
        {
            if (defaultPortrait != null)
            {
                portraitImage.sprite = defaultPortrait;
                portraitImage.gameObject.SetActive(true);
            }
            else
            {

            }
        }

        if (!string.IsNullOrEmpty(npcName))
        {
            npcName = npcName.Replace("按L键继续", "").Replace("按 L 键继续", "").Replace("...", "").Trim();
        }

        currentNpcName = string.IsNullOrEmpty(npcName) ? defaultNpcName : npcName;

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

        if (npcNameText != null)
        {
            npcNameText.text = npcName;
            Debug.Log($"DialogueManager: 设置NPC名称: {npcName}");
        }
        else
        {
            Debug.LogError("DialogueManager: npcNameText 为空！");
        }

        StartCoroutine(PlayDialogueSequence(dialogueSequence));
    }

    private IEnumerator PlayDialogueSequence(string[] dialogueSequence)
    {
        if (dialogueSequence == null || dialogueSequence.Length == 0)
        {
            Debug.LogWarning("DialogueManager: 对话序列为空！");
            EndDialogue();
            yield break;
        }

        currentDialogueSequence = dialogueSequence;
        currentDialogueIndex = 0;

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }

        ShowCurrentSentence();
    }

    private void ShowCurrentSentence()
    {
        if (currentDialogueSequence == null || currentDialogueIndex >= currentDialogueSequence.Length)
        {
            Debug.Log("DialogueManager: 对话序列播放完毕");
            autoCloseCoroutine = StartCoroutine(AutoCloseDialogue());
            return;
        }

        Debug.Log($"DialogueManager: 播放第 {currentDialogueIndex + 1}/{currentDialogueSequence.Length} 句对话");

        if (portraitImage != null)
        {
            if (currentDialogueIndex < currentPortraitRevealIndex)
            {
                portraitImage.gameObject.SetActive(false);
            }
            else
            {
                if (currentExpressions != null && currentDialogueIndex < currentExpressions.Length && currentExpressions[currentDialogueIndex] != null)
                {
                    portraitImage.sprite = currentExpressions[currentDialogueIndex];
                }
                else if (defaultPortrait != null)
                {
                    portraitImage.sprite = defaultPortrait;
                }

                if (portraitImage.sprite != null) 
                {
                    portraitImage.preserveAspect = true;
                    portraitImage.gameObject.SetActive(true);
                }
            }
        }

        // 清除旧的选项按钮
        ClearOptions();

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

        PrepareLine(currentDialogueSequence[currentDialogueIndex]);

        // 出场延后：如果在设定轮次之前，则名字不显示
        if (currentDialogueIndex < currentNameRevealIndex)
        {
            if (npcNameText != null) npcNameText.text = "";
            if (npcNameTextTMP != null) npcNameTextTMP.text = "";
        }

        typingCoroutine = StartCoroutine(TypeTextWithContinue(currentPreparedText));
    }

    private IEnumerator TypeTextWithContinue(string text)
    {
        isTyping = true;

        if (dialogueText != null) dialogueText.text = "";
        if (dialogueTextTMP != null) dialogueTextTMP.text = "";

        // 逐字显示文本
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            
            // 跳过富文本标签，一次性显示标签本身避免打出来的字符乱码
            if (c == '<')
            {
                int closingIndex = text.IndexOf('>', i);
                if (closingIndex != -1)
                {
                    string tag = text.Substring(i, closingIndex - i + 1);
                    if (dialogueText != null) dialogueText.text += tag;
                    if (dialogueTextTMP != null) dialogueTextTMP.text += tag;
                    i = closingIndex;
                    continue; // 标签内容不消耗打字时间
                }
            }

            if (dialogueText != null) dialogueText.text += c;
            if (dialogueTextTMP != null) dialogueTextTMP.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        Debug.Log("DialogueManager: 打字机效果完成，等待玩家按 L 键");

        // 显示继续提示
        if (showContinuePrompt && continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(true);
        }

        // 等待玩家按 L 键
        waitingForContinue = true;
    }

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

    private IEnumerator TypeTextSingle(string text)
    {
        Debug.Log($"DialogueManager: TypeTextSingle 开始，文本长度: {text?.Length ?? 0}");

        if (dialogueText != null) dialogueText.text = "";
        if (dialogueTextTMP != null) dialogueTextTMP.text = "";

        // 逐字显示文本
        foreach (char c in text)
        {
            if (dialogueText != null) dialogueText.text += c;
            if (dialogueTextTMP != null) dialogueTextTMP.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        Debug.Log("DialogueManager: 单句打字机效果完成");
    }

    private void PrepareLine(string raw)
    {
        // 清空上一次缓存
        currentPreparedText = string.Empty;
        currentPreparedSpeaker = string.Empty;
        currentPreparedIsPlayer = false;

        string npc = !string.IsNullOrEmpty(currentNpcName) ? currentNpcName : defaultNpcName;
        string input = raw ?? string.Empty;

        string speaker;
        string content;
        bool isPlayer;

        if (parseSpeakerPrefix && TryParseSpeakerPrefix(input, npc, out speaker, out content, out isPlayer))
        {
        }
        else
        {
            speaker = npc;
            content = input;
            isPlayer = false;
        }

        if (forcePlayerName && isPlayer)
        {
            speaker = playerName;
        }

        currentPreparedSpeaker = speaker;
        currentPreparedText = content;
        currentPreparedIsPlayer = isPlayer;

        if (npcNameText != null)
        {
            npcNameText.text = speaker;
            if (tintNameColor) npcNameText.color = isPlayer ? playerNameColor : npcNameColor;
        }
        if (npcNameTextTMP != null)
        {
            npcNameTextTMP.text = speaker;
            if (tintNameColor) npcNameTextTMP.color = isPlayer ? playerNameColor : npcNameColor;
        }

        if (dialogueText != null)
        {
            dialogueText.color = isPlayer ? playerDialogueColor : npcDialogueColor;
        }
        if (dialogueTextTMP != null)
        {
            dialogueTextTMP.color = isPlayer ? playerDialogueColor : npcDialogueColor;
        }
    }

    private bool TryParseSpeakerPrefix(string raw, string npcName, out string speaker, out string content, out bool isPlayer)
    {
        speaker = npcName;
        content = raw;
        isPlayer = false;

        if (string.IsNullOrEmpty(raw))
        {
            return false;
        }

        string s = raw.TrimStart();
        int sepIndex = -1;
        for (int i = 0; i < s.Length; i++)
        {
            string separators = string.IsNullOrEmpty(speakerSeparators) ? ":：﹕∶" : speakerSeparators;
            if (separators.IndexOf(s[i]) >= 0)
            {
                sepIndex = i;
                break;
            }

            if (i >= maxSpeakerPrefixLength)
            {
                return false;
            }
        }

        if (sepIndex <= 0)
        {
            return false;
        }

        string label = s.Substring(0, sepIndex).Trim();
        if (string.IsNullOrEmpty(label) || label.Length > maxSpeakerPrefixLength)
        {
            return false;
        }

        string rest = s.Substring(sepIndex + 1).TrimStart();

        // 规范化一些常见标签
        if (replacePlayerTagWithPlayerName && (label == "玩家" || label.Equals("player", System.StringComparison.OrdinalIgnoreCase) || label.Equals("me", System.StringComparison.OrdinalIgnoreCase) || label == "我"))
        {
            speaker = playerName;
            isPlayer = true;
        }
        else if (replaceNpcTagWithCurrentNpcName && (label == "NPC" || label.Equals("npc", System.StringComparison.OrdinalIgnoreCase)))
        {
            speaker = npcName;
            isPlayer = false;
        }
        else
        {
            speaker = label;
            if (label == "我" || label == playerName)
            {
                isPlayer = true;
            }
        }

        content = rest;
        return true;
    }

    private void ShowDialogue(string text, DialogueOption[] options)
    {
        Debug.Log($"DialogueManager: ShowDialogue 被调用，文本: '{text}'，选项数量: {(options != null ? options.Length : 0)}");

        PrepareLine(text);

        if (autoCloseCoroutine != null)
        {
            StopCoroutine(autoCloseCoroutine);
        }

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(currentPreparedText, options));

        Debug.Log("DialogueManager: 打字机协程已启动");
    }

    private IEnumerator TypeText(string text, DialogueOption[] options)
    {
        Debug.Log($"DialogueManager: TypeText 开始，文本长度: {text?.Length ?? 0}，选项数量: {(options != null ? options.Length : 0)}");

        if (dialogueText != null) dialogueText.text = "";
        if (dialogueTextTMP != null) dialogueTextTMP.text = "";
        int charCount = 0;

        // 逐字显示文本
        foreach (char c in text)
        {
            if (dialogueText != null) dialogueText.text += c;
            if (dialogueTextTMP != null) dialogueTextTMP.text += c;
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

    private void ShowOptions(DialogueOption[] options)
    {
        Debug.Log($"DialogueManager: ShowOptions 被调用，选项数量: {(options != null ? options.Length : 0)}");

        // 清除旧的选项按钮
        ClearOptions();

        if (optionsContainer != null)
        {
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

    private void CreateOptionButton(DialogueOption option)
    {
        if (optionButtonPrefab == null)
        {
            Debug.LogError("DialogueManager: optionButtonPrefab 为空，尝试从Resources加载");


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

        Text buttonText = null;

        Transform textTransform = buttonObj.transform.Find("Text");
        if (textTransform != null)
        {
            buttonText = textTransform.GetComponent<Text>();
        }

        if (buttonText == null)
        {
            buttonText = buttonObj.GetComponentInChildren<Text>(false); // false表示不包括非活动对象
        }

        if (buttonText == null)
        {
            buttonText = buttonObj.GetComponentInChildren<Text>(true);
        }

        if (buttonText != null)
        {
            buttonText.text = option.optionText;
            Debug.Log($"DialogueManager: 选项按钮文字已设置为: {option.optionText}");

            if (!buttonText.gameObject.activeSelf)
            {
                buttonText.gameObject.SetActive(true);
                Debug.Log("DialogueManager: Text对象已激活");
            }

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

    private void OnOptionSelected(DialogueOption option)
    {
        Debug.Log($"DialogueManager: 选中选项 - {option.optionText}");

        // 清除选项
        ClearOptions();

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
                ShowDialogue(option.responseText, null); 
            }
        }
        else
        {

            EndDialogue();
        }
    }

    private IEnumerator AutoCloseDialogue()
    {
        yield return new WaitForSeconds(autoCloseDelay);
        EndDialogue();
    }

    public void EndDialogue()
    {

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


        UnlockPlayerMovement();


        currentDialogueSequence = null;
        currentDialogueIndex = 0;


        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }


        if (continuePromptText != null)
        {
            continuePromptText.gameObject.SetActive(false);
        }

        // 清除选项
        ClearOptions();

        if (currentDialogueCallback != null)
        {
            var cb = currentDialogueCallback;
            currentDialogueCallback = null;
            cb?.Invoke();
        }
    }

    public bool IsDialogueActive => isDialogueActive;

    private void ClearOptions()
    {
        if (optionsContainer != null)
        {
            foreach (Transform child in optionsContainer)
            {
                Destroy(child.gameObject);
            }
        }
    }

    private void SetDialogueText(string text, bool append = false)
    {
        if (append)
        {
            if (dialogueText != null) dialogueText.text += text;
            if (dialogueTextTMP != null) dialogueTextTMP.text += text;
        }
        else
        {
            if (dialogueText != null) dialogueText.text = text;
            if (dialogueTextTMP != null) dialogueTextTMP.text = text;
        }
    }

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
