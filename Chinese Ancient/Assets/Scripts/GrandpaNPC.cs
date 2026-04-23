// AI辅助生成：DeepSeek-R1-0528, 2026-04-23
using UnityEngine;


public class GrandpaNPC : MonoBehaviour
{
    public static GrandpaNPC Instance { get; private set; }

    [Header("交互设置")]
    [Tooltip("玩家对象（如果不设置，将通过Player标签查找）")]
    public Transform playerTransform;

    [Tooltip("交互范围（米）")]
    public float interactionRange = 3f;

    [Tooltip("交互键位")]
    public KeyCode interactionKey = KeyCode.E;

    [Tooltip("是否支持鼠标点击交互")]
    public bool allowClickInteraction = true;

    [Header("对话内容")]
    [Tooltip("老爷爷的名字")]
    public string npcName = "老爷爷";

    [Tooltip("未拾取玉佩时的对话")]
    [TextArea(3, 5)]
    public string dialogueNoPendant = "小伙子，我的玉佩丢了，你能帮我找回来吗？";

    [Tooltip("归还玉佩后的感谢对话序列（多行对话）")]
    [TextArea(5, 10)]
    public string[] gratitudeDialogueSequence = new string[]
    {
        "噢！这不是我的玉佩吗？",                          
        "太感谢你了，小伙子！",                            
        "这玉佩是我祖上传下来的，对我意义重大。",          
        "您太客气了，这是我应该做的。",                     
        "你真是个好孩子，你的善良我永远不会忘记！"          
    };

    [Tooltip("已完成任务后的对话")]
    [TextArea(3, 5)]
    public string dialogueQuestComplete = "这玉佩对我很重要，再次感谢你！";

    [Header("奖励设置")]
    [Tooltip("完成任务后给予的奖励")]
    public string rewardText = "获得奖励：老爷爷的祝福！";

    [Tooltip("完成时的特效Prefab（可选）")]
    public GameObject completeEffectPrefab;

    private bool questCompleted = false;
    private bool isPlayerInRange = false;
    private JadePendantQuestManager questManager;
    private Collider npcCollider;

    // 对话触发控制
    private bool hasTriggeredNoPendantDialogue = false;
    private bool hasTriggeredGratitudeDialogue = false;
    private bool isDialoguePlaying = false;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Start()
    {
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    playerTransform = mainCam.transform;
                }
            }
        }

        questManager = FindObjectOfType<JadePendantQuestManager>();
        // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
        npcCollider = GetComponent<Collider>();
        if (npcCollider == null)
        {
            SphereCollider collider = gameObject.AddComponent<SphereCollider>();
            collider.isTrigger = true;
            collider.radius = 1f;
            npcCollider = collider;
        }
    }

    void Update()
    {
        // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
        isPlayerInRange = CheckPlayerInRange();

        if (DialogueManager.Instance != null)
        {
            isDialoguePlaying = DialogueManager.Instance.IsDialogueActive;
        }

        if (isPlayerInRange && Input.GetKeyDown(interactionKey))
        {
            if (isDialoguePlaying)
            {
                Debug.Log("对话正在进行中，请等待对话结束");
                return;
            }

            Interact();
        }
    }


    private bool CheckPlayerInRange()
    {
        if (playerTransform == null) return false;

        float distance = Vector3.Distance(transform.position, playerTransform.position);
        return distance <= interactionRange;
    }

    void OnMouseDown()
    {
        if (!allowClickInteraction) return;

        // 如果对话正在播放，不允许触发新的交互
        if (isDialoguePlaying)
        {
            Debug.Log("对话正在进行中，请等待对话结束");
            return;
        }

        if (CheckPlayerInRange())
        {
            Interact();
        }
        else
        {
            Debug.Log("距离老爷爷太远了，无法交互");
        }
    }


    private void Interact()
    {
        if (questCompleted)
        {
            // 任务已完成，只显示一次完成对话
            if (!hasTriggeredNoPendantDialogue)
            {
                ShowDialogue(dialogueQuestComplete);
                hasTriggeredNoPendantDialogue = true;
            }
            else
            {
                Debug.Log("任务已完成，不再重复对话");
            }
            return;
        }

        // 检查玩家是否有玉佩
        PlayerPickup player = PlayerPickup.Instance;
        if (player != null && player.HasPendant())
        {
            // 玩家有玉佩，完成任务
            CompleteQuest();
        }
        else
        {
            if (!hasTriggeredNoPendantDialogue)
            {
                ShowDialogue(dialogueNoPendant);
                hasTriggeredNoPendantDialogue = true;
                Debug.Log("首次触发'未拾取玉佩'对话");
            }
            else
            {
                Debug.Log("已经触发过'未拾取玉佩'对话，请去寻找玉佩吧！");
            }
        }
    }

    private void CompleteQuest()
    {
        Debug.Log("任务完成：归还玉佩给老爷爷！");

        // 从玩家背包中移除玉佩
        PlayerPickup player = PlayerPickup.Instance;
        if (player != null)
        {
            player.GetPendantForNPC();
            Debug.Log("已从玩家背包移除玉佩");
        }
        else
        {
            Debug.LogWarning("未找到PlayerPickup实例！");
        }

        // 播放完成特效
        if (completeEffectPrefab != null)
        {
            Instantiate(completeEffectPrefab, transform.position + Vector3.up, Quaternion.identity);
        }

        // 通知任务管理器
        if (questManager != null)
        {
            questManager.OnQuestCompleted();
        }

        // 显示奖励
        ShowReward();

        // 触发感谢对话（只触发一次）
        if (!hasTriggeredGratitudeDialogue)
        {
            StartGratitudeDialogue();
            hasTriggeredGratitudeDialogue = true;
        }

        questCompleted = true;
    }


    private void StartGratitudeDialogue()
    {
        if (hasTriggeredGratitudeDialogue)
        {
            Debug.Log("感谢对话已经触发过了，不再重复");
            return;
        }

        if (DialogueManager.Instance != null && gratitudeDialogueSequence != null && gratitudeDialogueSequence.Length > 0)
        {
            Debug.Log($"<color=yellow>启动老爷爷感谢对话，共 {gratitudeDialogueSequence.Length} 句</color>");
            isDialoguePlaying = true;

            // 为对话添加说话人前缀：奇数句=老爷爷，偶数句=我
            string[] dialogueWithSpeakers = new string[gratitudeDialogueSequence.Length];
            for (int i = 0; i < gratitudeDialogueSequence.Length; i++)
            {
                // 奇数句（1,3,5...索引0,2,4...）是老爷爷说话
                if (i % 2 == 0)
                {
                    dialogueWithSpeakers[i] = $"{npcName}：{gratitudeDialogueSequence[i]}";
                }
                // 偶数句（2,4,6...索引1,3,5...）是玩家说话
                else
                {
                    dialogueWithSpeakers[i] = $"我：{gratitudeDialogueSequence[i]}";
                }
            }

            DialogueManager.Instance.StartAutoDialogue(
                npcName,
                dialogueWithSpeakers,
                onComplete: () =>
                {
                    Debug.Log("感谢对话播放完毕");
                    isDialoguePlaying = false;
                }
            );
        }
        else
        {
            // 如果没有对话系统或对话序列为空，使用简单模式
            string fallbackMessage = "太感谢你了！你真是个好孩子！";

            if (DialogueManager.Instance == null)
            {
                Debug.LogWarning("未找到DialogueManager，使用Debug输出");
                Debug.Log($"{npcName}: {fallbackMessage}");
            }
            else if (gratitudeDialogueSequence == null || gratitudeDialogueSequence.Length == 0)
            {
                Debug.LogWarning("感谢对话序列为空，使用默认对话");
                ShowDialogue(fallbackMessage);
            }

            isDialoguePlaying = false;
        }
    }


    private void ShowDialogue(string dialogue)
    {
        // 防止重复触发
        if (isDialoguePlaying)
        {
            Debug.LogWarning("对话正在进行中，忽略新的对话请求");
            return;
        }

        Debug.Log($"老爷爷: {dialogue}");

        // 如果需要，也可以启动简单的单句对话
        if (DialogueManager.Instance != null)
        {
            isDialoguePlaying = true;

            DialogueManager.Instance.StartAutoDialogue(
                npcName,
                new string[] { dialogue },
                onComplete: () =>
                {
                    isDialoguePlaying = false;
                }
            );
        }
    }

    private void ShowReward()
    {
        Debug.Log($"获得奖励: {rewardText}");
        // 这里可以添加奖励逻辑，比如：
        // - 增加分数
        // - 解锁新内容
        // - 给予物品
        // - 播放音效
    }

    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, interactionRange);
    }

    void OnGUI()
    {
        // 显示交互提示
        if (isPlayerInRange && !isDialoguePlaying)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // 获取物体在屏幕上的位置
            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 2.5f);

            // 检查是否在摄像机前方
            if (screenPos.z < 0)
            {
                return;
            }

            // OnGUI的Y轴是倒置的，需要转换
            float displayY = Screen.height - screenPos.y;

            // 提示框尺寸
            float boxWidth = 280;
            float boxHeight = 80;
            float boxX = screenPos.x - boxWidth / 2;
            float boxY = displayY - boxHeight / 2;

            // 确保提示框在屏幕内
            boxX = Mathf.Clamp(boxX, 10, Screen.width - boxWidth - 10);
            boxY = Mathf.Clamp(boxY, 10, Screen.height - boxHeight - 10);

            // 确定提示文字和颜色
            string mainText = "";
            string subText = "";
            Color borderColor = Color.white;
            Color textColor = Color.white;

            bool hasPendant = PlayerPickup.Instance != null && PlayerPickup.Instance.HasPendant();

            if (questCompleted)
            {
                mainText = "按 [E] 对话";
                subText = "与老爷爷交谈";
                borderColor = new Color(0.5f, 0.8f, 1f); // 蓝色
                textColor = Color.white;
            }
            else if (hasPendant)
            {
                mainText = "按 [E] 归还玉佩";
                subText = "将玉佩还给老爷爷";
                borderColor = new Color(1f, 0.8f, 0f); // 金色
                textColor = new Color(1f, 0.95f, 0.3f); // 金黄色文字
            }
            else
            {
                mainText = "按 [E] 对话";
                subText = "与老爷爷交谈";
                borderColor = new Color(0.8f, 0.8f, 0.8f); // 灰色
                textColor = Color.white;
            }

            // 绘制半透明背景框
            GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
            boxStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.85f));
            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "", boxStyle);

            // 绘制彩色边框
            GUI.DrawTexture(new Rect(boxX, boxY, boxWidth, 5), MakeTexture(2, 2, borderColor));
            GUI.DrawTexture(new Rect(boxX, boxY + boxHeight - 5, boxWidth, 5), MakeTexture(2, 2, borderColor));

            // 主提示文字样式
            GUIStyle mainTextStyle = new GUIStyle();
            mainTextStyle.fontSize = 26;
            mainTextStyle.fontStyle = FontStyle.Bold;
            mainTextStyle.normal.textColor = textColor;
            mainTextStyle.alignment = TextAnchor.MiddleCenter;

            // 副提示文字样式
            GUIStyle subTextStyle = new GUIStyle();
            subTextStyle.fontSize = 18;
            subTextStyle.fontStyle = FontStyle.Normal;
            subTextStyle.normal.textColor = new Color(0.9f, 0.9f, 0.9f);
            subTextStyle.alignment = TextAnchor.MiddleCenter;

            // 绘制文字
            GUI.Label(new Rect(boxX, boxY + 10, boxWidth, 40), mainText, mainTextStyle);
            GUI.Label(new Rect(boxX, boxY + 45, boxWidth, 30), subText, subTextStyle);

            // 调试信息（按F2查看）
            if (Input.GetKeyDown(KeyCode.F2))
            {
                Debug.Log($"老爷爷提示 - 屏幕坐标: ({screenPos.x:F0}, {displayY:F0}), 框位置: ({boxX:F0}, {boxY:F0}), 在范围内: {isPlayerInRange}");
            }
        }
    }


    private Texture2D MakeTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
