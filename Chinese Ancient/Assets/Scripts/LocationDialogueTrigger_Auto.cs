using UnityEngine;
using UnityEngine.UI;

public class LocationDialogueTrigger_Auto : MonoBehaviour
{
    [Header("触发设置")]
    [SerializeField] private bool triggerOnce = true;
    
    [Tooltip("跨场景全局单次触发（推荐开场引导使用）：勾选后，即使玩家在场景间来回切转，整个游戏进程中也绝对只会触发一次。依靠下方的 Location Name 区分，请确保本场景内它的命名是唯一的。")]
    [SerializeField] private bool triggerOncePerGameSession = false;

    [SerializeField] private float triggerDelay = 0.3f;

    [Header("对话内容")]
    [SerializeField] private string npcDisplayName = "按L键继续  小微"; // NPC显示名称
    [SerializeField] private Sprite portraitSprite; // 默认立绘图片

    [SerializeField] private Sprite[] expressionPortraits;

    [TextArea(3, 10)]
    [SerializeField] private string[] dialogueLines = new string[]
    {
        "欢迎来到这个展区！",
        "这里有精彩的展品等待您的探索。",
        "按 L 键继续..."
    };

    [Header("高亮设置")]
    [SerializeField] private string[] highlightKeywords;
    [SerializeField] private Color highlightColor = new Color(1f, 0.84f, 0f); // 默认金色

    [Header("调试信息")]
    [SerializeField] private string locationName = "未命名位置";

    [Header("沉浸式表现设定 (可选)")]
    [SerializeField] private int portraitRevealIndex = 0;

    [SerializeField] private int nameRevealIndex = 0;

    [Header("重复与组控制")]
    [SerializeField] private bool globalPreventSameDialogue = false;

    [Header("结尾特效")]
    public bool playEndVFX = false;

    [SerializeField] private string sharedGroupID = "";

    [Header("前置条件 (可选)")]
    [Tooltip("是否需要前置事件（如交还火折子、修好灯笼）完成后，玩家进入此处才能听小微说话？")]
    public bool requireExternalCondition = false;

    [Tooltip("前置条件是否已满足（可在外界交互成功后调用 SetConditionMet 开启）")]
    public bool isConditionMet = false;

    [Header("事件设置")]
    public UnityEngine.Events.UnityEvent onDialogueEnd = new UnityEngine.Events.UnityEvent();

    // 静态内存（整个游戏运行期间共享）
    private static System.Collections.Generic.HashSet<string> playedDialogueHashes = new System.Collections.Generic.HashSet<string>();
    private static System.Collections.Generic.HashSet<string> triggeredGroupIDs = new System.Collections.Generic.HashSet<string>();
    private static System.Collections.Generic.HashSet<string> globalTriggeredLocationIDs = new System.Collections.Generic.HashSet<string>();

    private string GetDialogueHash()
    {
        if (dialogueLines == null || dialogueLines.Length == 0) return "";
        return string.Join("|", dialogueLines);
    }

    private bool hasTriggered = false;
    private bool isPlayerInTrigger = false;
    private DialogueManager dialogueManager;

    private void Start()
    {
        // 自动查找或创建DialogueManager
        SetupDialogueManager();
    }

    private void SetupDialogueManager()
    {
        // 尝试查找现有的DialogueManager
        dialogueManager = FindObjectOfType<DialogueManager>();

        if (dialogueManager == null)
        {
            Debug.LogWarning($"[{locationName}] 未找到DialogueManager，尝试自动创建...");

            // 先检查是否已经有DialoguePanel
            GameObject existingPanel = GameObject.Find("DialoguePanel");
            if (existingPanel != null)
            {
                Debug.LogWarning($"[{locationName}] 发现已存在的DialoguePanel，将被清理");
                DestroyImmediate(existingPanel);
            }

            // 创建DialogueManager GameObject
            GameObject dmObj = new GameObject("DialogueManager_AutoCreated");
            DontDestroyOnLoad(dmObj); // 防止被意外销毁
            dialogueManager = dmObj.AddComponent<DialogueManager>();

            // 查找或创建UI
            SetupDialogueUI(dmObj);
        }
        else
        {
            Debug.Log($"[{locationName}] 找到现有的DialogueManager");

            // 确保UI组件都正确设置
            if (dialogueManager.dialoguePanel == null)
            {
                Debug.LogWarning($"[{locationName}] DialogueManager存在但UI未设置，重新创建UI");
                SetupDialogueUI(dialogueManager.gameObject);
            }
        }

        // 检查UI是否正确配置
        CheckUIConfiguration();
    }

    private void SetupDialogueUI(GameObject dmObj)
    {
        Debug.Log($"[{locationName}] 开始创建对话UI...");

        // 查找或创建Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas_AutoCreated");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log($"[{locationName}] 创建了Canvas");
        }

        // 创建DialoguePanel
        GameObject panelObj = new GameObject("DialoguePanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.1f); // 扩大面板以容纳立绘
        panelRect.anchorMax = new Vector2(0.8f, 0.35f);
        panelRect.sizeDelta = Vector2.zero;

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // 创建立绘图片 - 显示在左上方
        GameObject portraitObj = CreatePortraitImage(panelObj.transform);

        // 创建NPC名称 - 显示在右下角，包含继续提示
        GameObject nameObj = CreateTextElement(panelObj.transform, "NPCName", npcDisplayName,
            new Vector2(1, 0), new Vector2(-10, 10), new Vector2(280, 30), 18);
        Text nameText = nameObj.GetComponent<Text>();
        nameText.alignment = TextAnchor.MiddleRight;
        nameText.color = new Color(0.8f, 0.8f, 0.8f, 1f); // 浅灰色

        // 创建对话内容 - 显示在立绘右侧
        GameObject dialogueObj = CreateTextElement(panelObj.transform, "DialogueText", "对话内容...",
            new Vector2(0.5f, 0.5f), new Vector2(60, 0), new Vector2(450, 100), 18);

        // 创建继续提示 - 已合并到NPC名称中，但仍保留用于DialogueManager
        GameObject continueObj = CreateTextElement(panelObj.transform, "ContinuePrompt", "",
            new Vector2(0.5f, 0), new Vector2(0, 5), new Vector2(300, 30), 16);
        continueObj.SetActive(false); // 隐藏，因为已经在NPC名称中显示

        // 创建OptionsContainer
        GameObject optionsObj = new GameObject("OptionsContainer");
        optionsObj.transform.SetParent(panelObj.transform, false);
        RectTransform optionsRect = optionsObj.AddComponent<RectTransform>();
        optionsRect.anchorMin = new Vector2(0, 0);
        optionsRect.anchorMax = new Vector2(1, 0);
        optionsRect.sizeDelta = new Vector2(-20, 100);

        // 设置DialogueManager的引用
        dialogueManager.dialoguePanel = panelObj;
        dialogueManager.portraitImage = portraitObj.GetComponent<Image>();
        dialogueManager.npcNameText = nameObj.GetComponent<Text>();
        dialogueManager.dialogueText = dialogueObj.GetComponent<Text>();
        dialogueManager.continuePromptText = continueObj.GetComponent<Text>();
        dialogueManager.optionsContainer = optionsObj.transform;

        // 尝试加载OptionButton预制体
        dialogueManager.optionButtonPrefab = Resources.Load<GameObject>("Prefabs/OptionButton");

        Debug.Log($"[{locationName}] 对话UI创建完成");
    }

    private GameObject CreateTextElement(Transform parent, string name, string text,
        Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text textComponent = obj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleLeft;

        return obj;
    }

    private GameObject CreatePortraitImage(Transform parent)
    {
        // 创建立绘图片对象
        GameObject portraitObj = new GameObject("PortraitImage");
        portraitObj.transform.SetParent(parent, false);

        // 设置RectTransform - 位置在左上方
        RectTransform portraitRect = portraitObj.AddComponent<RectTransform>();
        portraitRect.anchorMin = new Vector2(0, 1); // 左上角锚点
        portraitRect.anchorMax = new Vector2(0, 1);
        portraitRect.pivot = new Vector2(0, 1);
        portraitRect.anchoredPosition = new Vector2(20, -20); // 距离左上角20像素
        portraitRect.sizeDelta = new Vector2(150, 200); // 图片大小

        // 添加Image组件
        Image portraitImage = portraitObj.AddComponent<Image>();

        // 优先使用Inspector中配置的图片
        Sprite spriteToUse = portraitSprite;

        // 如果Inspector中没有配置，尝试自动加载
        if (spriteToUse == null)
        {
            spriteToUse = LoadPortraitSprite();
        }

        if (spriteToUse != null)
        {
            portraitImage.sprite = spriteToUse;
            portraitImage.preserveAspect = true; // 保持图片比例适配，防止变形
            Debug.Log($"[{locationName}] 成功加载立绘图片");
        }
        else
        {
            // 如果加载失败，使用占位颜色
            portraitImage.color = new Color(0.3f, 0.3f, 0.3f, 0.5f);
            Debug.LogWarning($"[{locationName}] 未找到立绘图片，使用占位颜色");
        }

        return portraitObj;
    }

    private Sprite LoadPortraitSprite()
    {
        // 使用AssetDatabase加载（仅编辑器）
        #if UNITY_EDITOR
        string[] possiblePaths = new string[]
        {
            "Assets/UIdesign/AIChat/立绘/1-1.png",
            "Assets/UIdesign/AIChat/立绘/1-1.jpg",
            "Assets/UIdesign/AIChat/立绘/1-1.jpeg"
        };

        foreach (string path in possiblePaths)
        {
            Sprite sprite = UnityEditor.AssetDatabase.LoadAssetAtPath<Sprite>(path);
            if (sprite != null)
            {
                Debug.Log($"[{locationName}] 从路径加载立绘成功: {path}");
                return sprite;
            }
        }
        #endif

        Debug.LogWarning($"[{locationName}] 无法加载立绘图片，尝试的路径: Assets/UIdesign/AIChat/立绘/1-1");
        return null;
    }

    private void CheckUIConfiguration()
    {
        if (dialogueManager == null)
        {
            Debug.LogError($"[{locationName}] DialogueManager 为空！");
            return;
        }

        bool hasErrors = false;

        if (dialogueManager.dialoguePanel == null)
        {
            Debug.LogError($"[{locationName}] dialoguePanel 未设置！");
            hasErrors = true;
        }

        if (dialogueManager.dialogueText == null)
        {
            Debug.LogError($"[{locationName}] dialogueText 未设置！");
            hasErrors = true;
        }

        if (dialogueManager.npcNameText == null)
        {
            Debug.LogError($"[{locationName}] npcNameText 未设置！");
            hasErrors = true;
        }

        if (!hasErrors)
        {
            Debug.Log($"[{locationName}] 所有UI组件配置正确！");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        Debug.Log($"[{locationName}] 检测到碰撞，对象: {other.name}");

        // 0. 外部前置任务检查（例如交出火折子亮灯笼后才能触发）
        if (requireExternalCondition && !isConditionMet)
        {
            Debug.Log($"[{locationName}] 前置任务暂未满足，不弹对话，跳过");
            return;
        }

        // 1. 本地单次触发检查
        if (triggerOnce && hasTriggered)
        {
            Debug.Log($"[{locationName}] 已触发过，跳过");
            return;
        }

        // 1.5 跨场景全局单次触发检查（依靠 场景名+LocationName 锁定唯一身份）
        string globalId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + locationName;
        if (triggerOncePerGameSession && globalTriggeredLocationIDs.Contains(globalId))
        {
            Debug.Log($"[{locationName}] 跨场景单次控制生效：玩家之前已从本场景触发过该引导对白，切回场景不重复播放，跳过");
            return;
        }

        // 2. 组级触发检查 (不同位置的同组触发器实现"一方触发，全组静默")
        if (!string.IsNullOrEmpty(sharedGroupID) && triggeredGroupIDs.Contains(sharedGroupID))
        {
            Debug.Log($"[{locationName}] 同组 '{sharedGroupID}' 的其他触发器已触发过，跳过");
            return;
        }

        // 3. 全局文案去重检查 (只要说过这句话，就在全游戏里不再触发同一句话)
        if (globalPreventSameDialogue && playedDialogueHashes.Contains(GetDialogueHash()))
        {
            Debug.Log($"[{locationName}] 相同的文案早已在别处播放过，防重复去重生效，跳过");
            // 直接标记已触发，避免玩家卡在这里频繁查重
            hasTriggered = true;
            return;
        }

        isPlayerInTrigger = true;

        // 延迟触发对话
        Invoke(nameof(TriggerDialogue), triggerDelay);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInTrigger = false;
        CancelInvoke(nameof(TriggerDialogue));
    }

    private void TriggerDialogue()
    {
        if (dialogueManager == null)
        {
            Debug.LogError($"[{locationName}] DialogueManager 为空，无法触发对话！");
            return;
        }

        if (dialogueManager.dialoguePanel == null)
        {
            Debug.LogError($"[{locationName}] dialoguePanel 为空，无法触发对话！");
            return;
        }

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning($"[{locationName}] 对话内容为空！");
            return;
        }

        // 处理关键字高亮
        string[] processedLines = new string[dialogueLines.Length];
        string colorHex = ColorUtility.ToHtmlStringRGB(highlightColor);

        for (int i = 0; i < dialogueLines.Length; i++)
        {
            string line = dialogueLines[i];
            if (highlightKeywords != null && highlightKeywords.Length > 0)
            {
                foreach (string kw in highlightKeywords)
                {
                    if (!string.IsNullOrEmpty(kw) && line.Contains(kw))
                    {
                        line = line.Replace(kw, $"<color=#{colorHex}>{kw}</color>");
                    }
                }
            }
            processedLines[i] = line;
        }

        // 开始对话 - 使用配置的NPC显示名称和处理后的富文本台词，并传递延后显示的索引
        dialogueManager.StartAutoDialogue(npcDisplayName, processedLines, portraitSprite, expressionPortraits, () => {
            Debug.Log($"[{locationName}] 对话结束回调触发！正在调用 onDialogueEnd ({onDialogueEnd?.GetPersistentEventCount() ?? 0} 个监听器)");

            if (playEndVFX && EndSequenceVFX.Instance != null)
            {
                Debug.Log($"[{locationName}] 正在播放黑屏震动模糊特效...");
                EndSequenceVFX.Instance.Play();
            }

            if (onDialogueEnd != null)
            {
                onDialogueEnd.Invoke();
            }
        }, portraitRevealIndex, nameRevealIndex);
        hasTriggered = true;

        // 存入跨场景唯一凭证
        if (triggerOncePerGameSession)
        {
            string globalId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + locationName;
            globalTriggeredLocationIDs.Add(globalId);
        }

        // 记入系统的组级防漏和全局去重字典
        if (!string.IsNullOrEmpty(sharedGroupID) && triggerOnce)
        {
            triggeredGroupIDs.Add(sharedGroupID);
        }

        if (globalPreventSameDialogue)
        {
            playedDialogueHashes.Add(GetDialogueHash());
        }

        Debug.Log($"[{locationName}] 触发对话成功！对话数量: {dialogueLines.Length}");
    }

    private void OnDrawGizmos()
    {
        Gizmos.color = hasTriggered ? Color.gray : Color.cyan;

        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);
        }
        else
        {
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }

    [ContextMenu("重置触发器(含全局静态记录)")]
    public void ResetTrigger()
    {
        hasTriggered = false;

        string globalId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name + "_" + locationName;
        globalTriggeredLocationIDs.Remove(globalId);

        if (!string.IsNullOrEmpty(sharedGroupID))
        {
            triggeredGroupIDs.Remove(sharedGroupID);
        }

        if (globalPreventSameDialogue)
        {
            playedDialogueHashes.Remove(GetDialogueHash());
        }

        Debug.Log($"[{locationName}] 触发器及其关联的全局记录已重置");
    }

    [ContextMenu("手动触发对话")]
    public void ManualTrigger()
    {
        SetupDialogueManager();
        hasTriggered = false;
        TriggerDialogue();
    }

    /// <summary>
    /// 被外部事件调用：标记任务已完成，并且如果玩家正身处触发器范围内，自动立刻弹对话开始！
    /// </summary>
    public void SetConditionMet()
    {
        if (requireExternalCondition && !isConditionMet)
        {
            isConditionMet = true;
            Debug.Log($"[{locationName}] 前置任务已满足，触发器状态：{isConditionMet}。允许和小微对话了！");

            // （可选）体贴功能：如果玩家交火折子的时候刚好就站在这段对话触发器圈子里，就让他直接触发对白
            if (isPlayerInTrigger && !hasTriggered)
            {
                Invoke(nameof(TriggerDialogue), triggerDelay);
            }
        }
    }
}
