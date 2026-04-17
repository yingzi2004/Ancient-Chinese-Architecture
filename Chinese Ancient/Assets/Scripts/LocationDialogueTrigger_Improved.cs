using UnityEngine;

/// <summary>
/// 改进版基于位置的对话触发器
/// 玩家进入触发区域时显示对话，离开时自动关闭
/// </summary>
public class LocationDialogueTrigger_Improved : MonoBehaviour
{
    [Header("触发设置")]
    [Tooltip("是否只触发一次")]
    [SerializeField] private bool triggerOnce = false;

    [Tooltip("触发延迟时间（秒）")]
    [SerializeField] private float triggerDelay = 0.3f;

    [Tooltip("玩家离开后是否自动隐藏对话")]
    [SerializeField] private bool autoHideOnExit = true;

    [Tooltip("自动隐藏延迟（秒），玩家离开后多久关闭")]
    [SerializeField] private float autoHideDelay = 1f;

    [Tooltip("离开距离偏移（玩家需要离开多远才触发关闭）")]
    [SerializeField] private float exitDistanceOffset = 2f;

    [Header("对话内容")]
    [SerializeField] private string guideName = "导游";
    [TextArea(3, 10)]
    [SerializeField] private string[] dialogueLines;

    [Header("调试信息")]
    [SerializeField] private string locationDescription = "位置描述";

    [Header("玩家检测")]
    [Tooltip("玩家 Transform，不设置则按 Tag 查找")]
    [SerializeField] private Transform player;

    [Tooltip("玩家 Tag（备用自动查找）")]
    [SerializeField] private string playerTag = "Player";

    [Tooltip("触发距离（用于检测玩家是否离开）")]
    [SerializeField] private float detectionDistance = 5f;

    [Header("可视化设置")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = Color.cyan;

    private bool hasTriggered = false;
    private bool isPlayerInTrigger = false;
    private GuideDialogueUI guideUI;
    private Coroutine autoHideCoroutine;
    private PlayerController playerController;

    private void Start()
    {
        // 查找GuideDialogueUI
        guideUI = FindObjectOfType<GuideDialogueUI>();
        if (guideUI == null)
        {
            Debug.LogWarning($"LocationDialogueTrigger ({locationDescription}): 未找到GuideDialogueUI！");
        }

        // 查找玩家控制器
        playerController = FindObjectOfType<PlayerController>();

        // 查找玩家
        if (player == null)
        {
            FindPlayer();
        }
    }

    /// <summary>
    /// 查找玩家对象
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
                Debug.Log($"LocationDialogueTrigger: 通过Tag '{playerTag}' 找到玩家: {player.name}");
                return;
            }
        }

        // 方法2: 通过PlayerController组件查找
        if (playerController != null)
        {
            player = playerController.transform;
            Debug.Log($"LocationDialogueTrigger: 通过PlayerController找到玩家: {player.name}");
            return;
        }

        // 方法3: 通过CharacterController组件查找
        CharacterController characterController = FindObjectOfType<CharacterController>();
        if (characterController != null)
        {
            player = characterController.transform;
            Debug.Log($"LocationDialogueTrigger: 通过CharacterController找到玩家: {player.name}");
            return;
        }

        Debug.LogWarning($"LocationDialogueTrigger: 未找到玩家对象！");
    }

    private void Update()
    {
        if (player == null) return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool insideRange = distance <= detectionDistance;

        // 玩家进入范围
        if (insideRange && !isPlayerInTrigger)
        {
            if (!triggerOnce || !hasTriggered)
            {
                isPlayerInTrigger = true;
                Debug.Log($"LocationDialogueTrigger: 玩家进入触发区域 - {locationDescription}, 距离: {distance:F2}米");

                // 延迟触发对话
                Invoke(nameof(TriggerDialogue), triggerDelay);
            }
        }

        // 玩家离开范围
        if (!insideRange && isPlayerInTrigger)
        {
            float exitDistance = detectionDistance + exitDistanceOffset;
            if (distance >= exitDistance)
            {
                isPlayerInTrigger = false;
                CancelInvoke(nameof(TriggerDialogue));

                Debug.Log($"LocationDialogueTrigger: 玩家离开触发区域 - {locationDescription}, 距离: {distance:F2}米");

                // 如果设置了离开自动隐藏
                if (autoHideOnExit)
                {
                    if (autoHideCoroutine != null)
                    {
                        StopCoroutine(autoHideCoroutine);
                    }
                    autoHideCoroutine = StartCoroutine(AutoHideDialogue());
                }
            }
        }
    }

    /// <summary>
    /// 触发对话
    /// </summary>
    private void TriggerDialogue()
    {
        if (!isPlayerInTrigger)
        {
            Debug.Log($"LocationDialogueTrigger: 玩家已离开范围，取消触发 - {locationDescription}");
            return;
        }

        if (guideUI == null)
        {
            Debug.LogError($"LocationDialogueTrigger ({locationDescription}): GuideDialogueUI为空！");
            return;
        }

        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning($"LocationDialogueTrigger ({locationDescription}): 对话内容为空！");
            return;
        }

        // 开始对话
        guideUI.StartGuideDialogue(guideName, null, dialogueLines);
        hasTriggered = true;

        Debug.Log($"LocationDialogueTrigger: 触发对话 - {locationDescription}");
    }

    /// <summary>
    /// 自动隐藏对话
    /// </summary>
    private System.Collections.IEnumerator AutoHideDialogue()
    {
        Debug.Log($"LocationDialogueTrigger: {autoHideDelay}秒后自动隐藏对话 - {locationDescription}");
        yield return new WaitForSeconds(autoHideDelay);

        if (guideUI != null && !isPlayerInTrigger)
        {
            guideUI.EndDialogue();
            Debug.Log($"LocationDialogueTrigger: 自动隐藏对话 - {locationDescription}");
        }
    }

    /// <summary>
    /// 重置触发器（用于测试）
    /// </summary>
    [ContextMenu("重置触发器")]
    public void ResetTrigger()
    {
        hasTriggered = false;
        isPlayerInTrigger = false;
        Debug.Log($"LocationDialogueTrigger: 触发器已重置 - {locationDescription}");
    }

    /// <summary>
    /// 手动触发对话（用于测试）
    /// </summary>
    [ContextMenu("手动触发对话")]
    public void ManualTrigger()
    {
        hasTriggered = false;
        TriggerDialogue();
    }

    /// <summary>
    /// 手动关闭对话（用于测试）
    /// </summary>
    [ContextMenu("手动关闭对话")]
    public void ManualCloseDialogue()
    {
        if (guideUI != null)
        {
            guideUI.EndDialogue();
            Debug.Log($"LocationDialogueTrigger: 手动关闭对话 - {locationDescription}");
        }
    }

    /// <summary>
    /// 绘制触发区域可视化
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        // 检测范围（青色 - 进入触发）
        Gizmos.color = hasTriggered ? Color.gray : gizmoColor;
        Gizmos.DrawWireSphere(transform.position, detectionDistance);

        // 离开范围（红色 - 离开自动关闭）
        float exitDistance = detectionDistance + exitDistanceOffset;
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.5f); // 半透明橙色
        Gizmos.DrawWireSphere(transform.position, exitDistance);
    }

    private void OnDestroy()
    {
        CancelInvoke();
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
    }
}
