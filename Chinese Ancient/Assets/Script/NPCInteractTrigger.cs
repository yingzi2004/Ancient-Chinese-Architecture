using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.Events;

[System.Serializable]
public class DialogNode
{
    [Tooltip("当进行到此节点时，NPC会按顺序说出以下的一句对话或多句对话")]
    [TextArea(2, 4)]
    public string[] npcLines;

    [Tooltip("这几句话说完后，抛给玩家的可选分支选项（如果不配，这个阶段对话就直接结束）")]
    public List<DialogChoice> choices = new List<DialogChoice>();
}

[System.Serializable]
public class DialogChoice
{
    [Tooltip("选项按钮面板上看到的文字")]
    public string optionText;

    [Tooltip("玩家点击此选项后，NPC接下来要进行的剧情对话（层层套用")]
    public DialogNode nextNode;
}

/// <summary>
/// NPC互动对话触发?- 玩家靠近后按键触发，支持选项分支
/// </summary>
public class NPCInteractTrigger : MonoBehaviour
{
    [Header("NPC设置")]
    [Tooltip("NPC名称")]
    public string npcName = "神秘村民";

    [Header("对话树（剧情节点与各分支")]
    [Tooltip("整个对话从这里开始（支持多句聊天，聊完弹出分支）")]
    public DialogNode rootNode = new DialogNode()
    {
        npcLines = new string[] {
            "哎呀，你可算来了",
            "那座破庙有动静，你要不要过去看看"
        },
        choices = new List<DialogChoice>()
        {
            new DialogChoice
            {
                optionText = "好，我这就去",
                nextNode = new DialogNode()
                {
                    npcLines = new string[] { "千万小心点！" },
                    choices = new List<DialogChoice>()
                }
            },
            new DialogChoice
            {
                optionText = "不去，我很忙",
                nextNode = new DialogNode()
                {
                    npcLines = new string[] { "现在的年轻人?.." },
                    choices = new List<DialogChoice>()
                }
            }
        }
    };

    [Header("触发设置")]
    [Tooltip("玩家 Transform，不设置则按 Tag 查找")]
    public Transform player;

    [Tooltip("玩家 Tag（备用自动查找）")]
    public string playerTag = "Player";

    [Tooltip("触发距离（米")]
    public float triggerDistance = 3f;

    [Tooltip("按键触发对话")]
    public KeyCode interactKey = KeyCode.L;

    [Tooltip("NPC的Animator（用于控制动画）")]
    public Animator npcAnimator;

    [Tooltip("使用 Bool 参数切换动画（否则使?Trigger")]
    public bool useBoolParameter = false;

    [Tooltip("触发对话时的动画触发器名称（useBoolParameter=false时使用）")]
    public string waveAnimationTrigger = "Wave";

    [Tooltip("挥手?Bool 参数名称（useBoolParameter=true时使用）")]
    public string waveAnimationBool = "Wave";

    [Tooltip("使用 Bool 时，按键触发后多久自动把 Bool 复位?false（秒）。用于避免一直满足条件导致反复切")]
    public float waveBoolAutoResetSeconds = 0.15f;

    [Tooltip("是否只触发一")]
    public bool triggerOnce = true;

    [Tooltip("触发后离开指定距离才能再次触发")]
    public float exitDistanceOffset = 1f;

    [Header("独立UI设置")]
    [Tooltip("必须要赋值：你自己做的NPC专门对话框界面面")]
    public GameObject customDialoguePanel;
    [Tooltip("必须要赋值：显示NPC名字的文本组")]
    public Text customNameText;
    [Tooltip("必须要赋值：显示对话内容的文本组")]
    public Text customContentText;
    [Tooltip("在这里把你做好的这几个选项按钮拖拽填进来！(例如填写2，然后拖2个Button进去)")]
    public List<Button> customOptionButtons = new List<Button>();

    [Header("调试设置")]
    [Tooltip("是否显示调试日志")]
    public bool showDebugLogs = true;

    [Tooltip("调试日志间隔（秒")]
    public float debugLogInterval = 1f;

    [Header("提示设置")]
    [Tooltip("是否显示提示UI")]
    public bool showPromptUI = true;

    [Tooltip("提示面板（可选，自动查找")]
    public GameObject promptPanel;

    [Header("事件设置")]
    [Tooltip("对话结束后是否恢复NPC原本的位置和朝向（如果要让NPC转身走开，请取消勾选）")]
    public bool restoreTransformOnEnd = true;

    [Tooltip("对话结束时触发的事件（可用于让NPC离开等）")]
    public UnityEvent onDialogueEnd;

    private bool hasTriggeredOnce = false;
    private bool isInsideRange = false;
    private float exitDistance;
    private float debugLogTimer = 0f;
    private Coroutine waveResetCoroutine;
    private bool dialogueStarted = false;
    
    // 存储对话前NPC的原始位置与朝向，以便对话结束后恢复
    

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private bool positionSaved = false;
    private bool originalRootMotion = false;
    private bool originalIsKinematic = false;


    private void Start()
    {
            Debug.Log($"<color=green>[NPC]</color> Trigger");

        exitDistance = triggerDistance + exitDistanceOffset;

        if (npcAnimator == null)
        {
            npcAnimator = GetComponentInChildren<Animator>();
        }

        if (player == null)
        {
            FindPlayer();
        }

        if (showPromptUI)
        {
            FindPromptPanel();
        }

        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        if (customDialoguePanel != null)
        {
            customDialoguePanel.SetActive(false); // 还没触发就隐藏面板！
        }

            Debug.Log($"<color=green>[NPC]</color> Trigger");
    }

    private void FindPlayer()
    {
        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null)
            {
                player = found.transform;
                return;
            }
        }

        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
            return;
        }

        CharacterController characterController = FindObjectOfType<CharacterController>();
        if (characterController != null)
        {
            player = characterController.transform;
            return;
        }

        if (Camera.main != null)
        {
            player = Camera.main.transform;
            return;
        }

        Debug.LogError($"<color=red>[NPC互动触发器]</color> 未找到玩家对象！");
    }

    private void FindPromptPanel()
    {
        if (promptPanel == null)
        {
            Transform promptTransform = transform.Find("PromptPanel");
            if (promptTransform != null)
            {
                promptPanel = promptTransform.gameObject;
            }
        }
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool withinRange = distance <= triggerDistance;

        if (showDebugLogs)
        {
            debugLogTimer += Time.deltaTime;
            if (debugLogTimer >= debugLogInterval)
            {
                debugLogTimer = 0f;
                Debug.Log($"<color=cyan>[NPC]</color> NPC: {npcName}, dist: {distance:F2}");
            }
        }

        // 玩家进入范围
        if (withinRange && !isInsideRange)
        {
            isInsideRange = true;
            if (showPromptUI && promptPanel != null && (!triggerOnce || !hasTriggeredOnce))
            {
                promptPanel.SetActive(true);
            }
        }

        // 在范围内按键交互
        if (Input.GetKeyDown(interactKey))
        {
            if (dialogueStarted)
            {
                // UI内的逐句判定已经在Coroutine里执行，不用单独
            }
            else if (isInsideRange)
            {
                if (!triggerOnce || !hasTriggeredOnce)
                {
                    if (showDebugLogs) Debug.Log($"<color=green>[NPC互动触发器]</color> 开始对话！");
                    
                    // 在转身面向玩家之前，先记录原始位置与朝向
                    
                    CaptureTransform();
                FacePlayer();
                    PlayWaveAnimation();
                    TriggerDialogue();

                    hasTriggeredOnce = true;

                    if (showPromptUI && promptPanel != null)
                    {
                        promptPanel.SetActive(false);
                    }
                }
            }
        }

        // 玩家离开范围
        else if (!withinRange && isInsideRange)
        {
            if (distance >= exitDistance)
            {
                isInsideRange = false;

                if (dialogueStarted)
                {
                    EndDialogue(true);
                    StopAllCoroutines();
                }

                if (showPromptUI && promptPanel != null)
                {
                    promptPanel.SetActive(false);
                }
            }
        }
    }

    private void FacePlayer()
    {
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                transform.rotation = Quaternion.LookRotation(direction);
            }
        }
    }

    private void PlayWaveAnimation()
    {
        if (npcAnimator == null) return;

        if (useBoolParameter)
        {
            if (!string.IsNullOrEmpty(waveAnimationBool))
            {
                npcAnimator.SetBool(waveAnimationBool, true);
                if (waveBoolAutoResetSeconds > 0f)
                {
                    if (waveResetCoroutine != null) StopCoroutine(waveResetCoroutine);
                    waveResetCoroutine = StartCoroutine(ResetWaveBoolAfterDelay(waveBoolAutoResetSeconds));
                }
            }
        }
        else
        {
            if (!string.IsNullOrEmpty(waveAnimationTrigger))
            {
                npcAnimator.ResetTrigger(waveAnimationTrigger);
                npcAnimator.SetTrigger(waveAnimationTrigger);
            }
        }
    }

    private IEnumerator ResetWaveBoolAfterDelay(float seconds)
    {
        yield return new WaitForSeconds(seconds);
        if (npcAnimator != null && !string.IsNullOrEmpty(waveAnimationBool))
        {
            npcAnimator.SetBool(waveAnimationBool, false);
        }
        waveResetCoroutine = null;
    }

    private void TriggerDialogue()
    {
        if (!dialogueStarted)
        {
            dialogueStarted = true;

            // 锁定玩家视角和移?(针对 Move.cs 中的 PlayerController)
            if (player != null)
            {
                PlayerController pc = player.GetComponent<PlayerController>();
                if (pc != null) pc.isInspecting = true;
            }

            StartCoroutine(PlayInteractTree(rootNode));
        }
    }

    /// <summary>
    /// 自定义播放引擎，独立驱动 DialogueManager 的面板，以实现原神般的连贯多段对话与动态选择
    /// </summary>
    private IEnumerator PlayInteractTree(DialogNode node)
    {
        if (node == null || node.npcLines == null || node.npcLines.Length == 0) 
        {
            EndDialogue();
            yield break;
        }

        // 用我们刚刚独立生成（或连好的）的组件体系！它?DialogueManager 再也没有一毛钱关系
        var panel = customDialoguePanel;
        var npcText = customNameText;
        var contentText = customContentText;
        var optionBtns = customOptionButtons;

        if (panel == null || contentText == null)
        {
            EndDialogue();
            yield break;
        }

        // 设置环境，清理旧按键
        panel.SetActive(true);
        if (optionBtns != null)
        {
            // 隐藏所有配好的选项按钮
            for (int i = 0; i < optionBtns.Count; i++)
            {
                if (optionBtns[i] != null) optionBtns[i].gameObject.SetActive(false);
            }
        }
        if (npcText != null) npcText.text = npcName;

        // 2. 依次按顺序播?npcLines 里的句子（按鼠标?L 继续
        for (int i = 0; i < node.npcLines.Length; i++)
        {
            contentText.text = "";
            string currentSentence = node.npcLines[i];

            // ==== 检测名字：原神风格?名字：台? ====
            if (npcText != null)
            {
                int colonIndex = currentSentence.IndexOf("："); // 中文冒号
                if (colonIndex == -1) colonIndex = currentSentence.IndexOf(":"); // 英文半角冒号

                if (colonIndex > 0)
                {
                    // 找到了冒号，把冒号前面的截取出来当名
                    npcText.text = currentSentence.Substring(0, colonIndex);
                    // 冒号后面的当台词
                    currentSentence = currentSentence.Substring(colonIndex + 1);
                }
                else
                {
                    // 没找着冒号，默认名字归
                    npcText.text = npcName;
                }
            }

            // 逐字简易打字机
            bool skipTyping = false;
            for (int ch = 0; ch < currentSentence.Length; ch++)
            {
                contentText.text += currentSentence[ch];

                // 用timer模拟打字速度，同时更精确捕获跳过输入
                float timer = 0f;
                // 打字速度 0.03秒一个字
                while (timer < 0.03f)
                {
                    timer += Time.deltaTime;
                    if (Input.GetKeyDown(interactKey))
                    {
                        skipTyping = true;
                        break;
                    }
                    yield return null;
                }

                if (skipTyping)
                {
                    contentText.text = currentSentence;
                    break;
                }
            }

            // 等待一帧，防止上面按下跳过时马上触发了下一句的检
            yield return null;
            // 等待玩家按下 L 继续或者结束本
            bool advanced = false;
            yield return null; 
            while (!advanced) // 在这里等待玩家按?L
            {
                if (Input.GetKeyDown(interactKey) || Input.GetMouseButtonDown(0)) advanced = true;
                yield return null;
            }
        }

        // 3. 这段话全部说完，检查有没有分支选项
        if (node.choices != null && node.choices.Count > 0)
        {
            int selectedIndex = -1;
            int highlightedIndex = 0; // 当前选中的选项索引
            List<Image> spawnedBtnImages = new List<Image>();
            List<Component> spawnedBtnTexts = new List<Component>();

            // 绑定您在这个列表里手动配好的按钮
            for (int i = 0; i < node.choices.Count; i++)
            {
                int captureIndex = i;
                if (optionBtns != null && i < optionBtns.Count && optionBtns[i] != null)
                {
                    // 获取您在Inspector中拖进槽位里的那几个按钮之一
                    Button btn = optionBtns[i];
                    btn.gameObject.SetActive(true); // 保证这个选项按钮显示出来
                    
                    Image img = btn.GetComponent<Image>();
                    if (img != null) spawnedBtnImages.Add(img);

                    // 兼容旧版 Text 或新?TextMeshPro
                    Text legacyText = btn.GetComponentInChildren<Text>();
                    TMPro.TextMeshProUGUI tmpText = btn.GetComponentInChildren<TMPro.TextMeshProUGUI>();

                    if (legacyText != null) 
                    {
                        legacyText.text = node.choices[i].optionText;
                        spawnedBtnTexts.Add(legacyText);
                    }
                    else if (tmpText != null)
                    {
                        tmpText.text = node.choices[i].optionText;
                        spawnedBtnTexts.Add(tmpText);
                    }

                    btn.onClick.RemoveAllListeners(); // 先清一下之前的事件，防止多次绑
                    btn.onClick.AddListener(() => {
                        selectedIndex = captureIndex;
                    });
                }
                else
                {
                    Debug.LogWarning($"<color=yellow>[NPC]</color> mismatch {node.choices.Count}");
}
            }

            // 等待玩家用滚轮切换或者F键确认（原来的鼠标点击依然兜底保留）
            while (selectedIndex == -1)
            {
                // 获取鼠标滚轮输入
                float scroll = Input.GetAxis("Mouse ScrollWheel");
                if (scroll > 0f) // 向上
                {
                    highlightedIndex--;
                    if (highlightedIndex < 0) highlightedIndex = node.choices.Count - 1;
                }
                else if (scroll < 0f) // 向下
                {
                    highlightedIndex++;
                    if (highlightedIndex >= node.choices.Count) highlightedIndex = 0;
                }

                // 更新高亮变色与缩放效
                for (int i = 0; i < spawnedBtnImages.Count; i++)
                {
                    if (spawnedBtnImages[i] != null)
                    {
                        if (i == highlightedIndex)
                        {
                            // 选中时：按钮变金黄明亮，稍微放大，字体加粗变
                            spawnedBtnImages[i].color = new Color(1f, 0.85f, 0.4f, 1f); 
                            spawnedBtnImages[i].transform.localScale = new Vector3(1.05f, 1.05f, 1f);
                            if (i < spawnedBtnTexts.Count && spawnedBtnTexts[i] != null)
                            {
                                if (spawnedBtnTexts[i] is Text lt) { legacyUpdateStyle(lt, true); }
                                else if (spawnedBtnTexts[i] is TMPro.TextMeshProUGUI tt) { tmpUpdateStyle(tt, true); }
                            }
                        }
                        else
                        {
                            // 未选中时：半透明暗态，正常大小，字体白
                            spawnedBtnImages[i].color = new Color(1f, 1f, 1f, 0.4f);
                            spawnedBtnImages[i].transform.localScale = Vector3.one;
                            if (i < spawnedBtnTexts.Count && spawnedBtnTexts[i] != null)
                            {
                                if (spawnedBtnTexts[i] is Text lt) { legacyUpdateStyle(lt, false); }
                                else if (spawnedBtnTexts[i] is TMPro.TextMeshProUGUI tt) { tmpUpdateStyle(tt, false); }
                            }
                        }
                    }
                }

                // 按下键盘 F 键确认选择当前高亮
                if (Input.GetKeyDown(interactKey) || Input.GetKeyDown(KeyCode.F))
                {
                    selectedIndex = highlightedIndex;
                }

                yield return null;
            }

            // 选完后清空选项
            if (optionBtns != null)
            {
                foreach (Button btn in optionBtns)
                {
                    if (btn != null) btn.gameObject.SetActive(false);
                }
            }

            // 递归进入下一
            yield return StartCoroutine(PlayInteractTree(node.choices[selectedIndex].nextNode));
        }
        else
        {
            EndDialogue();
        }
    }




    private void CaptureTransform()
    {
        if (!positionSaved)
        {
            originalPosition = transform.position;
            originalRotation = transform.rotation;
            
            if (npcAnimator != null)
            {
                originalRootMotion = npcAnimator.applyRootMotion;
                npcAnimator.applyRootMotion = false;
            }
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                originalIsKinematic = rb.isKinematic;
                rb.isKinematic = true;
            }

            positionSaved = true;
        }
    }

    private void RestoreTransform()
    {
        if (positionSaved)
        {
            UnityEngine.AI.NavMeshAgent agent = GetComponent<UnityEngine.AI.NavMeshAgent>();
            if (agent != null) agent.enabled = false;

            transform.position = originalPosition;
            transform.rotation = originalRotation;

            if (agent != null) agent.enabled = true;

            if (npcAnimator != null)
            {
                npcAnimator.applyRootMotion = originalRootMotion;
            }
            Rigidbody rb = GetComponent<Rigidbody>();
            if (rb != null)
            {
                rb.isKinematic = originalIsKinematic;
            }

            positionSaved = false;
        }
    }

    private void EndDialogue(bool skipEvent = false)
    {
        dialogueStarted = false;
        if (customDialoguePanel != null)
        {
            customDialoguePanel.SetActive(false);
        }

        // 解除玩家视角锁定
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.isInspecting = false;
        }

        // 把NPC变回他原本的朝向/位置（避免一直盯着玩家
        if (restoreTransformOnEnd)
        {
            RestoreTransform();
        }

        // 触发对话结束事件
        if (!skipEvent)
        {
            onDialogueEnd?.Invoke();
        }
    }

    private void legacyUpdateStyle(Text t, bool highlight)
    {
        t.color = highlight ? Color.black : Color.white;
        t.fontStyle = highlight ? FontStyle.Bold : FontStyle.Normal;
    }

    private void tmpUpdateStyle(TMPro.TextMeshProUGUI t, bool highlight)
    {
        t.color = highlight ? Color.black : Color.white;
        t.fontStyle = highlight ? TMPro.FontStyles.Bold : TMPro.FontStyles.Normal;
    }

    public void ManualTrigger()
    {
        if (dialogueStarted)
        {
            EndDialogue(true);
            StopAllCoroutines();
        }

        
        CaptureTransform();
        FacePlayer();
        PlayWaveAnimation();
        TriggerDialogue();
    }

    /// <summary>
    /// 提供外部脚本调用临时插入某段新对话（不修改原本的 rootNode
    /// </summary>
    public void StartSpecificDialogue(DialogNode customNode, bool replaceRoot = true)
    {
        // 如果正在和NPC对话中又触发了给荷花，那就先强行中断旧对
        if (dialogueStarted) 
        {
            EndDialogue(true);
            StopAllCoroutines(); // 停掉正在打字的旧对话
        }

        if (replaceRoot)
        {
            rootNode = customNode; // 永久替换之后聊天的内容
        }

        dialogueStarted = true;

        
        CaptureTransform();
        FacePlayer();
        PlayWaveAnimation();

        // 锁定玩家视角和移?(针对 Move.cs 中的 PlayerController)
        if (player != null)
        {
            PlayerController pc = player.GetComponent<PlayerController>();
            if (pc != null) pc.isInspecting = true;
        }

        StartCoroutine(PlayInteractTree(customNode));
    }

    public void ResetTrigger()
    {
        hasTriggeredOnce = false;
        isInsideRange = false;
        dialogueStarted = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);

        float extDist = triggerDistance + exitDistanceOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, extDist);
    }
}
