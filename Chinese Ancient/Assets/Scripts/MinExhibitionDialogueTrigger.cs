using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Min Exhibition 展馆导航式对话自动触发器
/// 玩家进入场景后自动显示欢迎对话
/// </summary>
public class MinExhibitionDialogueTrigger : MonoBehaviour
{
    [Header("对话设置")]
    [Tooltip("对话延迟时间（秒）- 玩家进入场景后多久开始对话")]
    [SerializeField] private float triggerDelay = 0.5f;

    [Tooltip("是否只触发一次")]
    [SerializeField] private bool triggerOnce = true;

    [Header("导游信息")]
    [SerializeField] private string guideName = "民俗展览讲解员";

    [Header("立绘设置")]
    [Tooltip("导游默认立绘图片（将显示在对话框左上方）")]
    [SerializeField] private Sprite guidePortrait;

    [Tooltip("针对每句对话的不同表情立绘，可使角色更灵动。数量建议与对话内容一致，若对应位置为空则使用默认立绘。")]
    [SerializeField] private Sprite[] expressionPortraits;

    [Header("欢迎对话内容")]
    [TextArea(3, 10)]
    [SerializeField] private string[] welcomeDialogue = new string[]
    {
        "欢迎来到民俗文化展览馆！",
        "这里展示了中国丰富多彩的民俗文化，包括传统建筑、生活用具、文化习俗等。",
        "您可以自由参观各个展区，了解不同地区的民俗特色。",
        "按 L 键可以推进对话，祝您参观愉快！"
    };

    [Header("高亮设置")]
    [Tooltip("需要高亮显示的关键字（如：F键、M键）")]
    [SerializeField] private string[] highlightKeywords;
    [Tooltip("关键字高亮颜色")]
    [SerializeField] private Color highlightColor = new Color(1f, 0.84f, 0f); // 默认金色

    [Header("结尾特效")]
    [Tooltip("勾选后，对话结束会自动播放屏幕模糊、黑屏、相机震动特效（不自动退出游戏）")]
    public bool playEndVFX = false;

    [Header("事件设置")]
    [Tooltip("对话结束时触发的事件")]
    public UnityEngine.Events.UnityEvent onDialogueEnd = new UnityEngine.Events.UnityEvent();

    private GuideDialogueUI guideUI;
    private bool hasTriggered = false;
    private bool isInitialized = false;

    void Start()
    {
        // 检查当前场景
        Scene currentScene = SceneManager.GetActiveScene();
        string sceneName = currentScene.name;

        // 支持多种可能的场景名称
        bool isCorrectScene = sceneName.Contains("Min") ||
                               sceneName.Contains("min") ||
                               sceneName.Contains("Exhibition") ||
                               sceneName.Contains("民俗");

        if (!isCorrectScene)
        {
            Debug.Log($"MinExhibitionDialogueTrigger: 当前场景是 '{sceneName}'，不是民俗展馆场景，跳过触发");
            return;
        }

        Debug.Log($"MinExhibitionDialogueTrigger: 在场景 '{sceneName}' 中初始化导航对话");

        // 查找GuideDialogueUI
        InitializeGuideUI();

        // 延迟触发对话
        Invoke(nameof(TriggerDialogue), triggerDelay);
    }

    /// <summary>
    /// 初始化导游UI系统
    /// </summary>
    void InitializeGuideUI()
    {
        // 方法1: 查找现有的GuideDialogueUI
        guideUI = FindObjectOfType<GuideDialogueUI>();

        if (guideUI != null)
        {
            Debug.Log("MinExhibitionDialogueTrigger: 找到现有的GuideDialogueUI");
            isInitialized = true;
            return;
        }

        // 方法2: 如果没有找到，尝试自动创建
        Debug.LogWarning("MinExhibitionDialogueTrigger: 未找到GuideDialogueUI，尝试使用GuideDialogueAutoSetup");

        GuideDialogueAutoSetup autoSetup = FindObjectOfType<GuideDialogueAutoSetup>();
        if (autoSetup != null)
        {
            Debug.Log("MinExhibitionDialogueTrigger: 找到GuideDialogueAutoSetup");
            // AutoSetup会自己创建UI，我们需要等待它创建完成
            Invoke(nameof(WaitForAutoSetup), 0.5f);
        }
        else
        {
            Debug.LogError("MinExhibitionDialogueTrigger: 场景中既没有GuideDialogueUI也没有GuideDialogueAutoSetup！");
            Debug.LogError("请确保场景中有以下之一：");
            Debug.LogError("1. GuideDialogueUI 组件");
            Debug.LogError("2. GuideDialogueAutoSetup 组件");
        }
    }

    /// <summary>
    /// 等待AutoSetup完成
    /// </summary>
    void WaitForAutoSetup()
    {
        guideUI = FindObjectOfType<GuideDialogueUI>();
        if (guideUI != null)
        {
            Debug.Log("MinExhibitionDialogueTrigger: AutoSetup创建完成，GuideDialogueUI已找到");
            isInitialized = true;
        }
        else
        {
            Debug.LogError("MinExhibitionDialogueTrigger: AutoSetup未创建GuideDialogueUI");
        }
    }

    /// <summary>
    /// 触发对话
    /// </summary>
    void TriggerDialogue()
    {
        if (!isInitialized)
        {
            Debug.LogWarning("MinExhibitionDialogueTrigger: UI未初始化，无法触发对话");
            return;
        }

        if (triggerOnce && hasTriggered)
        {
            Debug.Log("MinExhibitionDialogueTrigger: 已触发过，跳过");
            return;
        }

        if (guideUI == null)
        {
            Debug.LogError("MinExhibitionDialogueTrigger: guideUI为空，无法触发对话");
            return;
        }

        if (welcomeDialogue == null || welcomeDialogue.Length == 0)
        {
            Debug.LogWarning("MinExhibitionDialogueTrigger: 对话内容为空");
            return;
        }

        // 处理关键字高亮
        string[] processedLines = new string[welcomeDialogue.Length];
        string colorHex = ColorUtility.ToHtmlStringRGB(highlightColor);

        for (int i = 0; i < welcomeDialogue.Length; i++)
        {
            string line = welcomeDialogue[i];
            if (highlightKeywords != null && highlightKeywords.Length > 0)
            {
                foreach (string kw in highlightKeywords)
                {
                    if (!string.IsNullOrEmpty(kw) && line.Contains(kw))
                    {
                        line = line.Replace(kw, $"<color=#{colorHex}>{kw}</color>");
                    }
                }
            }
            processedLines[i] = line;
        }

        // 开始对话
        guideUI.StartGuideDialogue(guideName, guidePortrait, processedLines, expressionPortraits, () => {
            Debug.Log($"[{gameObject.name}] 对话结束了！");
            
            if (playEndVFX && EndSequenceVFX.Instance != null)
            {
                Debug.Log($"[{gameObject.name}] 准备播放黑屏震动模糊特效...");
                EndSequenceVFX.Instance.Play(); // 只播特效不退出游戏
            }

            if (onDialogueEnd != null)
            {
                onDialogueEnd.Invoke();
            }
        });
        hasTriggered = true;

        Debug.Log($"MinExhibitionDialogueTrigger: 触发欢迎对话 - 导游: {guideName}, 对话数: {welcomeDialogue.Length}");
    }

    /// <summary>
    /// 手动触发对话（可用于测试或其他事件触发）
    /// </summary>
    [ContextMenu("手动触发对话")]
    public void ManualTrigger()
    {
        hasTriggered = false; // 重置触发状态
        TriggerDialogue();
    }

    /// <summary>
    /// 设置新的对话内容
    /// </summary>
    public void SetDialogue(string[] newDialogue)
    {
        welcomeDialogue = newDialogue;
        Debug.Log("MinExhibitionDialogueTrigger: 对话内容已更新");
    }

    /// <summary>
    /// 设置导游名称
    /// </summary>
    public void SetGuideName(string newName)
    {
        guideName = newName;
        Debug.Log($"MinExhibitionDialogueTrigger: 导游名称已更新为: {newName}");
    }

    void OnDestroy()
    {
        // 清理定时器
        CancelInvoke();
    }
}
