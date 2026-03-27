using UnityEngine;
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

    [Header("对话选项")]
    [Tooltip("对话选项列表")]
    public List<DialogueOption> dialogueOptions = new List<DialogueOption>()
    {
        new DialogueOption
        {
            optionText = "请问祈年殿是用来做什么的？",
            responseText = "祈年殿是明清两代皇帝祭祈谷神的地方，每年孟春时节，皇帝会在这里举行祈谷典礼，祈祷五谷丰登。"
        },
        new DialogueOption
        {
            optionText = "这座建筑有什么特别之处吗？",
            responseText = "祈年殿采用独特的三重檐圆攒尖顶设计，全部用木结构构建，没有使用一根铁钉。殿内的柱子也很有讲究，中间四根龙井柱代表四季，外围十二根代表十二个月。"
        },
        new DialogueOption
        {
            optionText = "谢谢你，小谷！",
            responseText = "不客气！很高兴能为你介绍祈年殿，祝你在天坛玩得开心！"
        }
    };

    [Header("触发设置")]
    [Tooltip("玩家 Transform，不设置则按 Tag 查找")]
    public Transform player;

    [Tooltip("玩家 Tag（备用自动查找）")]
    public string playerTag = "Player";

    [Tooltip("触发距离（米）")]
    public float triggerDistance = 3f;

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

    private void Start()
    {
        Debug.Log($"<color=green>[NPC对话触发器]</color> 初始化开始 - NPC: {npcName}");

        // 计算离开距离（触发距离 + 偏移量）
        exitDistance = triggerDistance + exitDistanceOffset;

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

            // 检查是否可以触发
            if (!triggerOnce || !hasTriggeredOnce)
            {
                Debug.Log($"<color=green>[NPC对话触发器]</color> 准备触发对话... (0.5秒后)");
                // 延迟一小段时间后触发对话，让玩家有时间注意到NPC
                Invoke(nameof(TriggerDialogue), 0.5f);
                hasTriggeredOnce = true;
            }
            else if (triggerOnce && hasTriggeredOnce)
            {
                Debug.Log($"<color=yellow>[NPC对话触发器]</color> 已触发过一次，不再触发（Trigger Once = true）");
            }

            // 显示提示面板
            if (showPromptUI && promptPanel != null)
            {
                promptPanel.SetActive(true);
            }
        }
        // 离开触发范围（需要离开更远一点才能重置）
        else if (!withinRange && isInsideRange)
        {
            if (distance >= exitDistance)
            {
                Debug.Log($"<color=yellow>[NPC对话触发器]</color> 玩家离开范围并重置！距离: {distance:F2}米");
                isInsideRange = false;

                // 隐藏提示面板
                if (showPromptUI && promptPanel != null)
                {
                    promptPanel.SetActive(false);
                }
            }
        }
    }

    /// <summary>
    /// 触发对话
    /// </summary>
    private void TriggerDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            DialogueManager.Instance.StartDialogue(npcName, greetingText, dialogueOptions.ToArray());
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
