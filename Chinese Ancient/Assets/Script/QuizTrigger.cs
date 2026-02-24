using UnityEngine;
using TMPro;

/// <summary>
/// 问答游戏触发器 - 当玩家靠近时显示提示并触发问答
/// </summary>
public class QuizTrigger : MonoBehaviour
{
    [Header("--- 触发器设置 ---")]
    public GameObject triggerZone;              // 触发区域（可以是带 Collider 的物体）
    public LayerMask playerLayer;               // 玩家图层

    [Header("--- 提示 UI ---")]
    public GameObject promptPanel;              // 提示面板
    public TextMeshProUGUI promptText;          // 提示文字
    public string promptMessage = "按 [E] 键开始京派建筑知识问答";

    [Header("--- 提示音效 ---")]
    public AudioSource audioSource;
    public AudioClip promptSound;               // 提示音效

    [Header("--- 管理器引用 ---")]
    public QuizManager quizManager;             // 问答管理器

    private bool isPlayerInRange = false;
    private bool hasShownPrompt = false;        // 是否已经显示过提示

    private void Start()
    {
        // 初始化 Audio Source
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        if (audioSource == null)
            audioSource = gameObject.AddComponent<AudioSource>();

        // 隐藏提示面板
        if (promptPanel != null)
            promptPanel.SetActive(false);

        // 查找 QuizManager（如果没有手动指定）
        if (quizManager == null)
        {
            quizManager = FindObjectOfType<QuizManager>();
            if (quizManager == null)
            {
                Debug.LogWarning("[QuizTrigger] 未找到 QuizManager，请手动指定或确保场景中有 QuizManager！");
            }
        }

        // 验证触发器设置
        ValidateTriggerZone();
    }

    private void ValidateTriggerZone()
    {
        if (triggerZone == null)
        {
            // 如果没有指定触发区域，使用当前物体的 Collider
            Collider col = GetComponent<Collider>();
            if (col == null)
            {
                // 如果也没有 Collider，自动添加一个 BoxCollider
                BoxCollider boxCol = gameObject.AddComponent<BoxCollider>();
                boxCol.isTrigger = true;
                Debug.LogWarning("[QuizTrigger] 未找到触发区域，已自动添加 BoxCollider！");
            }
        }
    }

    private void Update()
    {
        // 检查玩家是否在范围内
        CheckPlayerInRange();

        // 显示/隐藏提示面板
        UpdatePromptDisplay();

        // 检测输入
        if (isPlayerInRange && Input.GetKeyDown(KeyCode.E))
        {
            OnInteract();
        }
    }

    /// <summary>
    /// 检查玩家是否在触发范围内
    /// </summary>
    private void CheckPlayerInRange()
    {
        Collider[] hitColliders;

        // 使用指定的触发区域或当前物体的 Collider
        Collider triggerCol = (triggerZone != null) ? triggerZone.GetComponent<Collider>() : GetComponent<Collider>();

        if (triggerCol != null)
        {
            // 检测触发区域内的碰撞体
            hitColliders = Physics.OverlapBox(
                triggerCol.bounds.center,
                triggerCol.bounds.extents,
                transform.rotation,
                playerLayer
            );

            isPlayerInRange = hitColliders.Length > 0;
        }
        else
        {
            // 使用距离检测作为备选方案
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                float distance = Vector3.Distance(transform.position, player.transform.position);
                isPlayerInRange = distance <= 3f; // 默认3米范围
            }
            else
            {
                isPlayerInRange = false;
            }
        }
    }

    /// <summary>
    /// 更新提示面板显示
    /// </summary>
    private void UpdatePromptDisplay()
    {
        // 如果问答正在进行，不显示提示
        if (quizManager != null && quizManager.IsQuizActive())
        {
            if (promptPanel != null)
                promptPanel.SetActive(false);
            return;
        }

        // 显示/隐藏提示
        if (promptPanel != null)
        {
            if (isPlayerInRange)
            {
                if (!hasShownPrompt)
                {
                    PlaySound(promptSound);
                    hasShownPrompt = true;
                }

                promptPanel.SetActive(true);
                if (promptText != null)
                    promptText.text = promptMessage;
            }
            else
            {
                promptPanel.SetActive(false);
                hasShownPrompt = false;
            }
        }
    }

    /// <summary>
    /// 玩家交互
    /// </summary>
    private void OnInteract()
    {
        if (quizManager != null)
        {
            quizManager.StartQuiz();
        }
        else
        {
            Debug.LogError("[QuizTrigger] QuizManager 未指定，无法启动问答游戏！");
        }
    }

    private void PlaySound(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }

    /// <summary>
    /// 在场景视图中绘制触发区域 Gizmo
    /// </summary>
    private void OnDrawGizmos()
    {
        // 绘制触发区域
        Collider triggerCol = (triggerZone != null) ? triggerZone.GetComponent<Collider>() : GetComponent<Collider>();

        if (triggerCol != null)
        {
            Gizmos.color = new Color(0f, 1f, 0f, 0.3f); // 半透明绿色
            Gizmos.DrawCube(triggerCol.bounds.center, triggerCol.bounds.size);
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(triggerCol.bounds.center, triggerCol.bounds.size);
        }
    }
}
