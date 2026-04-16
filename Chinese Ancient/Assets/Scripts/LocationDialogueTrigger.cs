using UnityEngine;

/// <summary>
/// 基于位置的对话触发器
/// 玩家进入触发区域时显示相应的对话内容
/// 用于引导玩家在场景中探索
/// </summary>
public class LocationDialogueTrigger : MonoBehaviour
{
    [Header("触发设置")]
    [Tooltip("是否只触发一次")]
    [SerializeField] private bool triggerOnce = true;

    [Tooltip("触发延迟时间（秒）")]
    [SerializeField] private float triggerDelay = 0.3f;

    [Tooltip("玩家离开后是否自动隐藏对话")]
    [SerializeField] private bool autoHideOnExit = false;

    [Tooltip("自动隐藏延迟（秒）")]
    [SerializeField] private float autoHideDelay = 5f;

    [Header("对话内容")]
    [SerializeField] private string guideName = "导游";
    [TextArea(3, 10)]
    [SerializeField] private string[] dialogueLines;

    [Header("调试信息")]
    [SerializeField] private string locationDescription = "位置描述";

    [Header("可视化设置")]
    [SerializeField] private bool showGizmo = true;
    [SerializeField] private Color gizmoColor = Color.cyan;
    [SerializeField] private GizmoType gizmoType = GizmoType.WireBox;

    private bool hasTriggered = false;
    private bool isPlayerInTrigger = false;
    private GuideDialogueUI guideUI;
    private Coroutine autoHideCoroutine;

    private void Start()
    {
        // 查找GuideDialogueUI
        guideUI = FindObjectOfType<GuideDialogueUI>();
        if (guideUI == null)
        {
            Debug.LogWarning($"LocationDialogueTrigger ({locationDescription}): 未找到GuideDialogueUI！");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        if (triggerOnce && hasTriggered)
            return;

        isPlayerInTrigger = true;
        Debug.Log($"LocationDialogueTrigger: 玩家进入触发区域 - {locationDescription}");

        // 延迟触发对话
        Invoke(nameof(TriggerDialogue), triggerDelay);
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Player"))
            return;

        isPlayerInTrigger = false;
        CancelInvoke(nameof(TriggerDialogue));

        // 如果设置了离开自动隐藏
        if (autoHideOnExit && guideUI != null)
        {
            if (autoHideCoroutine != null)
            {
                StopCoroutine(autoHideCoroutine);
            }
            autoHideCoroutine = StartCoroutine(AutoHideDialogue());
        }
    }

    /// <summary>
    /// 触发对话
    /// </summary>
    private void TriggerDialogue()
    {
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
    /// 绘制触发区域可视化
    /// </summary>
    private void OnDrawGizmos()
    {
        if (!showGizmo)
            return;

        Gizmos.color = hasTriggered ? Color.gray : gizmoColor;

        Collider col = GetComponent<Collider>();
        if (col != null && col.isTrigger)
        {
            if (col is BoxCollider box)
            {
                Vector3 size = transform.lossyScale;
                if (box != null)
                {
                    size = Vector3.Scale(box.size, transform.lossyScale);
                }
                Gizmos.matrix = transform.localToWorldMatrix;
                if (gizmoType == GizmoType.WireBox)
                    Gizmos.DrawWireCube(box != null ? box.center : Vector3.zero, box != null ? box.size : Vector3.one);
                else
                    Gizmos.DrawCube(box != null ? box.center : Vector3.zero, box != null ? box.size : Vector3.one);
            }
            else if (col is SphereCollider sphere)
            {
                float radius = sphere.radius * Mathf.Max(transform.lossyScale.x, transform.lossyScale.y, transform.lossyScale.z);
                Gizmos.DrawWireSphere(transform.position + sphere.center, radius);
            }
        }
        else
        {
            // 如果没有Trigger Collider，显示默认盒子
            Gizmos.DrawWireCube(transform.position, transform.localScale);
        }
    }

    private void OnDestroy()
    {
        CancelInvoke();
        if (autoHideCoroutine != null)
        {
            StopCoroutine(autoHideCoroutine);
        }
    }

    public enum GizmoType
    {
        WireBox,
        SolidBox
    }
}
