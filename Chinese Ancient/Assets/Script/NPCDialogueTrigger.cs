using UnityEngine;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// NPC对话触发器 - 玩家靠近时自动弹出对话
/// </summary>
public class NPCDialogueTrigger : MonoBehaviour
{
    [Header("NPC设置")]
    [Tooltip("NPC名称")]
    public string npcName = "小谷";

    [Tooltip("欢迎语（玩家靠近时说的第一句话）")]
    [TextArea(3, 5)]
    public string greetingText = "你好呀,我是小谷,欢迎来到祈年殿,你有什么想了解的吗?";

    [Header("导游设置（自动对话序列）")]
    [Tooltip("自动对话序列（欢迎语后的对话内容）")]
    [TextArea(3, 5)]
    public string[] autoDialogueSequence = new string[]
    {
        "祈年殿是明清两代皇帝祭天、祈谷的神圣场所，建于明永乐十八年，已有600多年历史了。你看这宏伟的建筑，直径32米，高38米，全部采用木质结构，没用一颗钉子！",
        "最神奇的是，祈年殿完全依靠木构件相互咬合而成，代表了中国古代建筑工艺的巅峰。蓝色的琉璃瓦象征蓝天，整座大殿寓意'天圆地方'的宇宙观。",
        "殿内的28根楠木大柱也各有寓意——中间4根龙井柱代表一年四季，中圈12根象征十二个月，外圈12根对应十二时辰，加起来恰好是28星宿，完美融合了天文历法与建筑美学！"
    };

    [Header("触发设置")]
    [Tooltip("玩家 Transform，不设置则按 Tag 查找")]
    public Transform player;

    [Tooltip("玩家 Tag（备用自动查找）")]
    public string playerTag = "Player";

    [Tooltip("触发距离（米）")]
    public float triggerDistance = 3f;

    [Tooltip("自动触发对话（靠近时自动开始，不需要按键）")]
    public bool autoTriggerOnApproach = true;

    [Tooltip("按键触发对话（仅在autoTriggerOnApproach=false时有效）")]
    public KeyCode interactKey = KeyCode.L;

    [Tooltip("自动触发延迟时间（秒），避免立即触发")]
    public float autoTriggerDelay = 0.5f;

    [Tooltip("NPC的Animator（用于控制动画）")]
    public Animator npcAnimator;

    [Tooltip("使用 Bool 参数切换动画（否则使用 Trigger）")]
    public bool useBoolParameter = false;

    [Tooltip("触发对话时的动画触发器名称（useBoolParameter=false时使用）")]
    public string waveAnimationTrigger = "Wave";

    [Tooltip("挥手的 Bool 参数名称（useBoolParameter=true时使用）")]
    public string waveAnimationBool = "Wave";

    [Tooltip("使用 Bool 时，按键触发后多久自动把 Bool 复位为 false（秒）。用于避免一直满足条件导致反复切换")]
    public float waveBoolAutoResetSeconds = 0.15f;

    [Tooltip("是否只触发一次")]
    public bool triggerOnce = true;

    [Tooltip("触发后离开指定距离才能再次触发")]
    public float exitDistanceOffset = 1f;

    [Header("调试设置")]
    [Tooltip("是否显示调试日志")]
    public bool showDebugLogs = true;

    [Tooltip("调试日志间隔（秒）")]
    public float debugLogInterval = 1f;

    [Header("提示设置")]
    [Tooltip("是否显示提示UI")]
    public bool showPromptUI = true;

    [Tooltip("提示面板（可选，自动查找）")]
    public GameObject promptPanel;

    private bool hasTriggeredOnce = false;
    private bool isInsideRange = false;
    private float exitDistance;
    private float debugLogTimer = 0f;
    private Coroutine waveResetCoroutine;
    private Coroutine autoTriggerCoroutine;
    private bool dialogueStarted = false;

    private void Start()
    {
        Debug.Log($"<color=green>[NPC对话触发器]</color> 初始化开始 - NPC: {npcName}");

        // 计算离开距离（触发距离 + 偏移量）
        exitDistance = triggerDistance + exitDistanceOffset;

        // 如果未绑定Animator，尝试自动获取
        if (npcAnimator == null)
        {
            npcAnimator = GetComponentInChildren<Animator>();
        }

        // 查找玩家
        if (player == null)
        {
            FindPlayer();
        }

        // 查找或隐藏提示面板
        if (showPromptUI)
        {
            FindPromptPanel();
        }

        // 初始隐藏提示面板
        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        // 验证DialogueManager
        ValidateDialogueManager();

        Debug.Log($"<color=green>[NPC对话触发器]</color> 初始化完成 - 玩家: {(player != null ? player.name : "未找到")}, 触发距离: {triggerDistance}米");
    }

    /// <summary>
    /// 查找玩家对象（多种方式）
    /// </summary>
    private void FindPlayer()
    {
        // 方法1: 通过Tag查找
        if (!string.IsNullOrEmpty(playerTag))
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null)
            {
                player = found.transform;
                Debug.Log($"<color=green>[NPC对话触发器]</color> 通过Tag '{playerTag}' 找到玩家: {player.name}");
                return;
            }
        }

        // 方法2: 通过PlayerController组件查找
        PlayerController playerController = FindObjectOfType<PlayerController>();
        if (playerController != null)
        {
            player = playerController.transform;
            Debug.Log($"<color=green>[NPC对话触发器]</color> 通过PlayerController组件找到玩家: {player.name}");
            return;
        }

        // 方法3: 通过CharacterController组件查找
        CharacterController characterController = FindObjectOfType<CharacterController>();
        if (characterController != null)
        {
            player = characterController.transform;
            Debug.Log($"<color=green>[NPC对话触发器]</color> 通过CharacterController组件找到玩家: {player.name}");
            return;
        }

        // 方法4: 找主相机
        if (Camera.main != null)
        {
            player = Camera.main.transform;
            Debug.Log($"<color=yellow>[NPC对话触发器]</color> 警告：使用主相机作为玩家位置（可能不准确）");
            return;
        }

        Debug.LogError($"<color=red>[NPC对话触发器]</color> 错误：未找到玩家对象！请手动设置玩家Transform或确保玩家对象有正确的Tag！");
    }

    /// <summary>
    /// 验证DialogueManager是否存在
    /// </summary>
    private void ValidateDialogueManager()
    {
        if (DialogueManager.Instance == null)
        {
            Debug.LogError($"<color=red>[NPC对话触发器]</color> 错误：DialogueManager实例不存在！请确保场景中有DialogueManager对象！");
        }
        else
        {
            Debug.Log($"<color=green>[NPC对话触发器]</color> DialogueManager实例已找到");
        }
    }

    private void FindPromptPanel()
    {
        // 如果没有手动设置提示面板，尝试查找
        if (promptPanel == null)
        {
            // 尝试在NPC对象下查找
            Transform promptTransform = transform.Find("PromptPanel");
            if (promptTransform != null)
            {
                promptPanel = promptTransform.gameObject;
            }
        }
    }

    private void Update()
    {
        if (player == null)
        {
            Debug.LogWarning("<color=yellow>[NPC对话触发器]</color> 玩家对象为空，跳过检测！");
            return;
        }

        float distance = Vector3.Distance(transform.position, player.position);
        bool withinRange = distance <= triggerDistance;

        // 调试日志
        if (showDebugLogs)
        {
            debugLogTimer += Time.deltaTime;
            if (debugLogTimer >= debugLogInterval)
            {
                debugLogTimer = 0f;
                Debug.Log($"<color=cyan>[NPC对话触发器]</color> NPC: {npcName}, 距离: {distance:F2}米, 触发范围: {triggerDistance}米, 状态: {(withinRange ? "在范围内" : "不在范围内")}, 已触发: {hasTriggeredOnce}");
            }
        }

        // 进入触发范围
        if (withinRange && !isInsideRange)
        {
            Debug.Log($"<color=green>[NPC对话触发器]</color> 玩家进入触发范围！距离: {distance:F2}米");
            isInsideRange = true;

            // 自动触发模式：延迟后自动开始对话
            if (autoTriggerOnApproach)
            {
                if (!triggerOnce || !hasTriggeredOnce)
                {
                    if (autoTriggerCoroutine != null)
                    {
                        StopCoroutine(autoTriggerCoroutine);
                    }
                    autoTriggerCoroutine = StartCoroutine(AutoTriggerAfterDelay());
                }
            }
            else
            {
                // 按键触发模式：显示提示面板
                if (showPromptUI && promptPanel != null)
                {
                    promptPanel.SetActive(true);
                }
            }
        }

        // 按键触发模式（仅在autoTriggerOnApproach=false时有效）
        if (!autoTriggerOnApproach && Input.GetKeyDown(interactKey))
        {
            if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
            {
                // 对话已开启时不重复触发；继续/下一句由 DialogueManager.Update() 处理
            }
            else if (isInsideRange)
            {
                if (!triggerOnce || !hasTriggeredOnce)
                {
                    Debug.Log($"<color=green>[NPC对话触发器]</color> 玩家按下 {interactKey}，开始对话！");

                    // 转向玩家
                    FacePlayer();

                    // 播放挥手动画
                    PlayWaveAnimation();

                    // 触发对话
                    TriggerDialogue();
                    hasTriggeredOnce = true;
                    dialogueStarted = true;
                }
                else if (triggerOnce && hasTriggeredOnce)
                {
                    Debug.Log($"<color=yellow>[NPC对话触发器]</color> 已对话过一次，不再触发");
                }
            }
        }

        // 离开触发范围（需要离开更远一点才能重置）
        else if (!withinRange && isInsideRange)
        {
            if (distance >= exitDistance)
            {
                Debug.Log($"<color=yellow>[NPC对话触发器]</color> 玩家离开范围并重置！距离: {distance:F2}米");
                isInsideRange = false;

                // 取消自动触发协程
                if (autoTriggerCoroutine != null)
                {
                    StopCoroutine(autoTriggerCoroutine);
                    autoTriggerCoroutine = null;
                }

                // 结束正在进行的对话
                if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                {
                    Debug.Log("<color=yellow>[NPC对话触发器]</color> 玩家离开，结束对话");
                    DialogueManager.Instance.EndDialogue();
                    dialogueStarted = false;
                }

                // 隐藏提示面板
                if (showPromptUI && promptPanel != null)
                {
                    promptPanel.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 让NPC转向玩家
    /// </summary>
    private void FacePlayer()
    {
        if (player != null)
        {
            Vector3 direction = (player.position - transform.position).normalized;
            // 忽略Y轴（防止NPC仰头或低头）
            direction.y = 0;
            if (direction != Vector3.zero)
            {
                Quaternion lookRotation = Quaternion.LookRotation(direction);
                // 瞬间转向玩家（如果需要自然转，可以用Slerp或者放到协程里面做平滑旋转）
                transform.rotation = lookRotation;
            }
        }
    }

    private void PlayWaveAnimation()
    {
        if (npcAnimator == null)
        {
            return;
        }

        if (useBoolParameter)
        {
            if (string.IsNullOrEmpty(waveAnimationBool))
            {
                return;
            }

            npcAnimator.SetBool(waveAnimationBool, true);

            if (waveBoolAutoResetSeconds > 0f)
            {
                if (waveResetCoroutine != null)
                {
                    StopCoroutine(waveResetCoroutine);
                }
                waveResetCoroutine = StartCoroutine(ResetWaveBoolAfterDelay(waveBoolAutoResetSeconds));
            }
        }
        else
        {
            if (string.IsNullOrEmpty(waveAnimationTrigger))
            {
                return;
            }

            // 防止连续触发时丢触发
            npcAnimator.ResetTrigger(waveAnimationTrigger);
            npcAnimator.SetTrigger(waveAnimationTrigger);
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

    /// <summary>
    /// 延迟自动触发对话
    /// </summary>
    private IEnumerator AutoTriggerAfterDelay()
    {
        Debug.Log($"<color=cyan>[NPC对话触发器]</color> {autoTriggerDelay}秒后自动触发对话...");

        yield return new WaitForSeconds(autoTriggerDelay);

        // 检查玩家是否还在范围内
        if (isInsideRange && player != null)
        {
            float distance = Vector3.Distance(transform.position, player.position);
            if (distance <= triggerDistance)
            {
                Debug.Log($"<color=green>[NPC对话触发器]</color> 自动触发对话！距离: {distance:F2}米");

                // 转向玩家
                FacePlayer();

                // 播放挥手动画
                PlayWaveAnimation();

                // 触发对话
                TriggerDialogue();
                hasTriggeredOnce = true;
                dialogueStarted = true;
            }
            else
            {
                Debug.Log($"<color=yellow>[NPC对话触发器]</color> 玩家已离开范围，取消自动触发");
            }
        }

        autoTriggerCoroutine = null;
    }

    /// <summary>
    /// 触发对话
    /// </summary>
    private void TriggerDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            // 导游自动对话模式 - 将欢迎语和后续对话合并成一个序列
            string[] fullSequence = new string[autoDialogueSequence.Length + 1];
            fullSequence[0] = greetingText;
            for (int i = 0; i < autoDialogueSequence.Length; i++)
            {
                fullSequence[i + 1] = autoDialogueSequence[i];
            }
            DialogueManager.Instance.StartAutoDialogue(npcName, fullSequence);
        }
        else
        {
            Debug.LogError($"[{nameof(NPCDialogueTrigger)}] DialogueManager 实例不存在！请确保场景中有 DialogueManager。");
        }
    }

    /// <summary>
    /// 手动触发对话（供按钮或其他方式调用）
    /// </summary>
    public void ManualTrigger()
    {
        TriggerDialogue();
    }

    /// <summary>
    /// 重置触发器（允许再次触发）
    /// </summary>
    public void ResetTrigger()
    {
        hasTriggeredOnce = false;
        isInsideRange = false;
        dialogueStarted = false;

        // 取消正在进行的自动触发
        if (autoTriggerCoroutine != null)
        {
            StopCoroutine(autoTriggerCoroutine);
            autoTriggerCoroutine = null;
        }
    }

    /// <summary>
    /// 在编辑器中绘制触发范围
    /// </summary>
    private void OnDrawGizmosSelected()
    {
        // 触发范围（黄色）
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);

        // 离开范围（红色）
        float exitDistance = triggerDistance + exitDistanceOffset;
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, exitDistance);
    }
}
