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

    [Header("自动对话序列")]
    [Tooltip("是否使用自动对话模式（不需要玩家选择）")]
    public bool useAutoDialogue = true;

    [Tooltip("自动对话序列（欢迎语后的对话内容）")]
    [TextArea(3, 5)]
    public string[] autoDialogueSequence = new string[]
    {
        "祈年殿是明清两代皇帝祭天、祈谷的神圣场所，建于明永乐十八年，已有600多年历史了。你看这宏伟的建筑，直径32米，高38米，全部采用木质结构，没用一颗钉子！",
        "最神奇的是，祈年殿完全依靠木构件相互咬合而成，代表了中国古代建筑工艺的巅峰。蓝色的琉璃瓦象征蓝天，整座大殿寓意'天圆地方'的宇宙观。"
    };

    [Header("对话选项（仅当useAutoDialogue=false时使用）")]
    [Tooltip("对话选项列表")]
    public List<DialogueOption> dialogueOptions = new List<DialogueOption>()
    {
        new DialogueOption
        {
            optionText = "请问祈年殿是用来做什么的？",
            responseText = "祈年殿是明清两代皇帝祭祈谷神的地方，每年孟春时节，皇帝会在这里举行祈谷典礼，祈祷五谷丰登。",
            followUpOptions = new DialogueOption[]
            {
                new DialogueOption
                {
                    optionText = "听起来很有历史感，它是什么时候建造的？",
                    responseText = "祈年殿始建于明永乐十八年（1420年），原本叫大祀殿。不过我们现在看到的是光绪二十二年（1896年）重建后的样子，因为原殿在光绪十五年被雷火焚毁了。",
                    followUpOptions = new DialogueOption[]
                    {
                        new DialogueOption
                        {
                            optionText = "哇，那它还能保存这么完好真是太不容易了！",
                            responseText = "是啊！这座殿见证了中国近600年的历史变迁。每一次修缮都保留了原有的建筑风格和工艺，真不愧是中国古代建筑的杰作！"
                        },
                        new DialogueOption
                        {
                            optionText = "我还想了解其他方面。",
                            responseText = "当然！你想了解什么呢？"
                        }
                    }
                },
                new DialogueOption
                {
                    optionText = "那皇帝每次都亲自来吗？",
                    responseText = "是的！皇帝非常重视这个仪式。每年的正月上辛日，皇帝都会亲临或派遣王公大臣代为行礼，可见对农业的重视程度。"
                }
            }
        },
        new DialogueOption
        {
            optionText = "这座建筑有什么特别之处吗？",
            responseText = "祈年殿采用独特的三重檐圆攒尖顶设计，全部用木结构构建，没有使用一根铁钉。",
            followUpOptions = new DialogueOption[]
            {
                new DialogueOption
                {
                    optionText = "不用钉子？那它是怎么连接的？",
                    responseText = "这就要说到中国古代的榫卯工艺了！所有木构件都通过榫头和卯眼相互咬合，环环相扣。不仅坚固，还能抗震呢！",
                    followUpOptions = new DialogueOption[]
                    {
                        new DialogueOption
                        {
                            optionText = "太神奇了！那殿内的柱子也有讲究吧？",
                            responseText = "没错！你观察得很仔细。殿内共有28根楠木大柱，中间4根龙井柱代表一年四季，中圈12根代表十二个月，外圈12根代表十二时辰。加起来正好是28星宿！"
                        },
                        new DialogueOption
                        {
                            optionText = "古人的智慧真是令人佩服！",
                            responseText = "是啊！祈年殿不仅是建筑奇迹，更承载着深厚的文化内涵。每一处设计都体现着古人对天地、时间和自然的理解。"
                        }
                    }
                },
                new DialogueOption
                {
                    optionText = "蓝色的瓦片也很漂亮。",
                    responseText = "你说得对！这些蓝色的琉璃瓦象征蓝天，配合圆形的殿顶，寓意'天圆地方'的宇宙观。整座大殿就像一座连接天地的神圣建筑！"
                }
            }
        },
        new DialogueOption
        {
            optionText = "这里有什么有趣的传说或故事吗？",
            responseText = "当然有！相传祈年殿的设计与古代的'明堂'有关，是皇帝与天地沟通的神圣场所。",
            followUpOptions = new DialogueOption[]
            {
                new DialogueOption
                {
                    optionText = "能给我讲讲更具体的故事吗？",
                    responseText = "据说光绪年间的一场大火后，慈禧太后下令重建。工匠们凭借记忆和图纸，仅用四年就重建完成，而且完全保持了原貌。这些建筑技艺的传承，本身就是一段传奇！"
                },
                new DialogueOption
                {
                    optionText = "真有意思！还有什么特别的意义吗？",
                    responseText = "祈年殿的'三重檐'也很有寓意。上层象征天，中层象征人，下层象征地，体现了中国古代'天人合一'的哲学思想。"
                }
            }
        },
        new DialogueOption
        {
            optionText = "谢谢你，小谷！你的讲解真棒！",
            responseText = "不客气！很高兴能为你介绍祈年殿。这里还有很多值得探索的地方，比如皇穹宇、圜丘坛等等。祝你在天坛玩得开心！"
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

                // 结束正在进行的对话
                if (DialogueManager.Instance != null && DialogueManager.Instance.IsDialogueActive)
                {
                    Debug.Log("<color=yellow>[NPC对话触发器]</color> 玩家离开，结束对话");
                    DialogueManager.Instance.EndDialogue();
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
    /// 触发对话
    /// </summary>
    private void TriggerDialogue()
    {
        if (DialogueManager.Instance != null)
        {
            if (useAutoDialogue)
            {
                // 使用自动对话模式
                // 将欢迎语和后续对话合并成一个序列
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
                // 使用选项对话模式
                DialogueManager.Instance.StartDialogue(npcName, greetingText, dialogueOptions.ToArray());
            }
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
