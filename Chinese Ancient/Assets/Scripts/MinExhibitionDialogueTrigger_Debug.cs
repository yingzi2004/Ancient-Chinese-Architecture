using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections;

/// <summary>
/// Min Exhibition 导航式对话触发器 - 调试版本
/// 用于排查问题
/// </summary>
public class MinExhibitionDialogueTrigger_Debug : MonoBehaviour
{
    [Header("调试设置")]
    [SerializeField] private bool enableDebugLog = true;
    [SerializeField] private bool forceTrigger = false; // 强制触发，忽略场景名称检查

    [Header("对话设置")]
    [SerializeField] private float triggerDelay = 0.5f;
    [SerializeField] private bool triggerOnce = true;

    [Header("导游信息")]
    [SerializeField] private string guideName = "民俗展览讲解员";

    [Header("欢迎对话内容")]
    [TextArea(3, 10)]
    [SerializeField] private string[] welcomeDialogue = new string[]
    {
        "欢迎来到民俗文化展览馆！",
        "这里展示了中国丰富多彩的民俗文化，包括传统建筑、生活用具、文化习俗等。",
        "您可以自由参观各个展区，了解不同地区的民俗特色。",
        "按 L 键可以推进对话，祝您参观愉快！"
    };

    private GuideDialogueUI guideUI;
    private bool hasTriggered = false;

    void Start()
    {
        DebugLog("=== MinExhibitionDialogueTrigger_Debug 启动 ===");

        // 显示当前场景信息
        Scene currentScene = SceneManager.GetActiveScene();
        DebugLog($"当前场景: {currentScene.name} (路径: {currentScene.path})");

        // 查找GuideDialogueUI
        FindGuideDialogueUI();

        // 延迟触发对话
        DebugLog($"将在 {triggerDelay} 秒后触发对话...");
        Invoke(nameof(TriggerDialogue), triggerDelay);
    }

    void FindGuideDialogueUI()
    {
        DebugLog("开始查找 GuideDialogueUI...");

        // 查找所有GuideDialogueUI
        GuideDialogueUI[] allGuideUIs = FindObjectsOfType<GuideDialogueUI>();
        DebugLog($"找到 {allGuideUIs.Length} 个 GuideDialogueUI 组件");

        if (allGuideUIs.Length == 0)
        {
            Debug.LogError("❌ 场景中没有 GuideDialogueUI！");
            Debug.LogError("请尝试以下方法之一：");
            Debug.LogError("1. 添加 GuideDialogueAutoSetup 组件到场景");
            Debug.LogError("2. 检查场景中是否有 Canvas");
            return;
        }

        // 使用第一个找到的
        guideUI = allGuideUIs[0];
        DebugLog($"✓ 找到 GuideDialogueUI: {guideUI.name}");
    }

    void TriggerDialogue()
    {
        DebugLog("=== 尝试触发对话 ===");

        if (guideUI == null)
        {
            Debug.LogError("❌ guideUI 为 null，无法触发对话");
            FindGuideDialogueUI();

            if (guideUI == null)
            {
                Debug.LogError("❌ 仍然找不到 guideUI，放弃触发");
                return;
            }
        }

        if (triggerOnce && hasTriggered)
        {
            DebugLog("已触发过，跳过（除非 forceTrigger = true）");
            if (!forceTrigger) return;
        }

        if (welcomeDialogue == null || welcomeDialogue.Length == 0)
        {
            Debug.LogError("❌ 对话内容为空");
            return;
        }

        // 检查GuideDialogueUI状态
        DebugLog($"GuideDialogueUI.IsDialogueActive: {guideUI.IsDialogueActive}");

        // 开始对话
        try
        {
            guideUI.StartGuideDialogue(guideName, null, welcomeDialogue);
            hasTriggered = true;
            DebugLog($"✓ 对话已触发 - 导游: {guideName}, 对话数: {welcomeDialogue.Length}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"❌ 触发对话时出错: {e.Message}");
            Debug.LogError($"堆栈跟踪: {e.StackTrace}");
        }
    }

    void DebugLog(string message)
    {
        if (enableDebugLog)
        {
            Debug.Log($"[对话触发器] {message}");
        }
    }

    /// <summary>
    /// 手动触发对话（可在运行时从Inspector调用）
    /// </summary>
    [ContextMenu("手动触发对话")]
    public void ManualTrigger()
    {
        DebugLog("手动触发对话");
        hasTriggered = false;
        FindGuideDialogueUI();
        TriggerDialogue();
    }

    /// <summary>
    /// 测试UI系统
    /// </summary>
    [ContextMenu("测试UI系统")]
    public void TestUISystem()
    {
        DebugLog("=== 测试UI系统 ===");

        // 查找Canvas
        Canvas[] canvases = FindObjectsOfType<Canvas>();
        DebugLog($"Canvas数量: {canvases.Length}");

        // 查找GuideDialogueUI
        GuideDialogueUI[] guideUIs = FindObjectsOfType<GuideDialogueUI>();
        DebugLog($"GuideDialogueUI数量: {guideUIs.Length}");

        // 查找GuideDialogueAutoSetup
        GuideDialogueAutoSetup[] autoSetups = FindObjectsOfType<GuideDialogueAutoSetup>();
        DebugLog($"GuideDialogueAutoSetup数量: {autoSetups.Length}");

        // 查找所有GameObject
        GameObject[] allObjects = FindObjectsOfType<GameObject>();
        int dialoguePanelCount = 0;
        foreach (var obj in allObjects)
        {
            if (obj.name.Contains("Dialogue") || obj.name.Contains("Guide"))
            {
                dialoguePanelCount++;
                DebugLog($"找到对话相关对象: {obj.name}");
            }
        }
        DebugLog($"对话相关对象总数: {dialoguePanelCount}");
    }

    void OnDestroy()
    {
        CancelInvoke();
    }
}
