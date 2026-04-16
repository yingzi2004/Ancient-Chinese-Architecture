using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// 场景导游触发器
/// 当玩家进入特定场景时自动触发导游对话
/// </summary>
public class SceneGuideTrigger : MonoBehaviour
{
    [Header("场景设置")]
    [SerializeField] private string targetSceneName = "Min Exhibition";

    [Header("导游信息 - 方式1：直接配置")]
    [SerializeField] private string guideName = "导游";
    [SerializeField] private Sprite portraitSprite;
    [TextArea]
    [SerializeField] private string[] dialogueSequence;

    [Header("导游信息 - 方式2：使用数据文件")]
    [SerializeField] private GuideDialogueData dialogueData;

    [Header("触发设置")]
    [SerializeField] private float triggerDelay = 1f; // 延迟触发时间，给玩家一点时间适应场景
    [SerializeField] private bool triggerOnce = true; // 是否只触发一次

    private bool hasTriggered = false;
    private bool isInTargetScene = false;

    void Start()
    {
        // 检查当前场景
        CheckScene();
    }

    void Update()
    {
        // 检查场景切换
        Scene currentScene = SceneManager.GetActiveScene();
        bool currentlyInTarget = currentScene.name == targetSceneName;

        if (currentlyInTarget && !isInTargetScene)
        {
            // 刚进入目标场景
            isInTargetScene = true;
            OnSceneEnter();
        }
        else if (!currentlyInTarget && isInTargetScene)
        {
            // 离开目标场景
            isInTargetScene = false;
        }
    }

    private void CheckScene()
    {
        Scene currentScene = SceneManager.GetActiveScene();
        isInTargetScene = currentScene.name == targetSceneName;

        if (isInTargetScene)
        {
            OnSceneEnter();
        }
    }

    private void OnSceneEnter()
    {
        if (triggerOnce && hasTriggered)
        {
            Debug.Log($"SceneGuideTrigger: 场景 '{targetSceneName}' 已触发过，跳过");
            return;
        }

        Debug.Log($"SceneGuideTrigger: 进入场景 '{targetSceneName}'，准备触发导游对话");

        // 延迟触发
        Invoke(nameof(TriggerGuideDialogue), triggerDelay);
    }

    private void TriggerGuideDialogue()
    {
        if (hasTriggered && triggerOnce) return;

        if (GuideDialogueUI.Instance != null)
        {
            // 优先使用数据文件，否则使用直接配置的数据
            string name = dialogueData != null ? dialogueData.guideName : guideName;
            Sprite sprite = dialogueData != null ? dialogueData.portraitSprite : portraitSprite;
            string[] dialogue = dialogueData != null ? dialogueData.dialogueSequence : dialogueSequence;

            if (dialogue == null || dialogue.Length == 0)
            {
                Debug.LogWarning("SceneGuideTrigger: 对话内容为空，无法触发导游对话");
                return;
            }

            GuideDialogueUI.Instance.StartGuideDialogue(name, sprite, dialogue);
            hasTriggered = true;
            Debug.Log("SceneGuideTrigger: 导游对话已触发");
        }
        else
        {
            Debug.LogError("SceneGuideTrigger: 未找到 GuideDialogueUI 实例！请确保场景中有导游对话UI。");
        }
    }

    void OnDisable()
    {
        CancelInvoke(nameof(TriggerGuideDialogue));
    }

    /// <summary>
    /// 重置触发状态（用于测试）
    /// </summary>
    [ContextMenu("重置触发状态")]
    public void ResetTrigger()
    {
        hasTriggered = false;
        Debug.Log("SceneGuideTrigger: 触发状态已重置");
    }

    /// <summary>
    /// 手动触发导游对话（用于测试）
    /// </summary>
    [ContextMenu("手动触发")]
    public void ManualTrigger()
    {
        TriggerGuideDialogue();
    }
}
