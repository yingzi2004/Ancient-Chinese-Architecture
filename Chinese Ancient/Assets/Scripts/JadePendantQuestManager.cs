using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玉佩任务管理器 - 管理整个玉佩拾取任务的流程
/// 挂载到场景中的空GameObject上
/// </summary>
public class JadePendantQuestManager : MonoBehaviour
{
    public static JadePendantQuestManager Instance { get; private set; }

    [Header("任务设置")]
    [Tooltip("任务名称")]
    public string questName = "寻找玉佩";

    [Tooltip("任务描述")]
    [TextArea(3, 5)]
    public string questDescription = "老爷爷的玉佩丢了，帮他找回来吧！";

    [Tooltip("场景中所有玉佩的总数")]
    public int totalPendantsInScene = 1;

    [Header("UI显示")]
    [Tooltip("是否显示任务UI")]
    public bool showQuestUI = true;

    [Tooltip("任务开始时的提示文本")]
    public string questStartMessage = "任务开始：寻找丢失的玉佩";

    [Tooltip("拾取玉佩的提示文本")]
    public string pickupMessage = "找到了玉佩！把它还给老爷爷吧！";

    [Tooltip("任务完成的提示文本")]
    public string questCompleteMessage = "任务完成！玉佩已归还给老爷爷";

    [Header("奖励设置")]
    [Tooltip("完成任务后的奖励分数")]
    public int rewardScore = 100;

    [Tooltip("是否在完成后自动加载下一个场景")]
    public bool loadNextSceneOnComplete = false;

    [Tooltip("下一个场景的名称")]
    public string nextSceneName = "";

    // 任务状态
    private bool questStarted = false;
    private bool questCompleted = false;
    private int pendantsCollected = 0;

    // 所有玉佩引用
    private List<JadePendant> allPendants = new List<JadePendant>();

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
        // 查找场景中所有玉佩
        FindAllPendants();

        // 开始任务
        StartQuest();

        // 显示任务UI
        if (showQuestUI)
        {
            ShowQuestUI();
        }
    }

    /// <summary>
    /// 查找场景中所有玉佩
    /// </summary>
    private void FindAllPendants()
    {
        JadePendant[] foundPendants = FindObjectsOfType<JadePendant>();
        allPendants = new List<JadePendant>(foundPendants);
        totalPendantsInScene = allPendants.Count;

        Debug.Log($"找到 {totalPendantsInScene} 个玉佩");
    }

    /// <summary>
    /// 开始任务
    /// </summary>
    public void StartQuest()
    {
        if (questStarted) return;

        questStarted = true;
        Debug.Log($"[{questName}] {questStartMessage}");
        Debug.Log(questDescription);

        // 这里可以添加任务开始的特效或音效
    }

    /// <summary>
    /// 当玉佩被拾取时调用
    /// </summary>
    public void OnJadePendantPickedUp(JadePendant pendant)
    {
        if (questCompleted) return;

        pendantsCollected++;

        Debug.Log(pickupMessage);
        Debug.Log($"进度: {pendantsCollected}/{totalPendantsInScene}");

        // 检查是否所有玉佩都被拾取（本例只有1个）
        if (pendantsCollected >= totalPendantsInScene)
        {
            Debug.Log("<color=yellow>所有玉佩已找到！现在去找老爷爷吧！</color>");
        }
    }

    /// <summary>
    /// 当任务完成时调用（玉佩归还给老爷爷）
    /// </summary>
    public void OnQuestCompleted()
    {
        if (questCompleted) return;

        questCompleted = true;

        Debug.Log($"<color=green>====================</color>");
        Debug.Log($"<color=green>【任务完成】{questName}</color>");
        Debug.Log($"<color=green>{questCompleteMessage}</color>");
        Debug.Log($"<color=green>获得奖励分数: {rewardScore}</color>");
        Debug.Log($"<color=green>====================</color>");

        // 隐藏任务UI
        HideQuestUI();

        // 保存完成状态
        PlayerPrefs.SetInt($"{questName}_Completed", 1);
        PlayerPrefs.Save();

        // 可选：加载下一个场景
        if (loadNextSceneOnComplete && !string.IsNullOrEmpty(nextSceneName))
        {
            StartCoroutine(LoadNextSceneAfterDelay());
        }
    }

    /// <summary>
    /// 延迟加载下一个场景
    /// </summary>
    private System.Collections.IEnumerator LoadNextSceneAfterDelay()
    {
        yield return new WaitForSeconds(2f);
        UnityEngine.SceneManagement.SceneManager.LoadScene(nextSceneName);
    }

    /// <summary>
    /// 显示任务UI
    /// </summary>
    private void ShowQuestUI()
    {
        Debug.Log($"任务UI显示: {questName}");
        // 这里可以集成UI系统显示任务面板
    }

    /// <summary>
    /// 更新任务UI
    /// </summary>
    private void UpdateQuestUI()
    {
        Debug.Log($"任务进度更新: {pendantsCollected}/{totalPendantsInScene}");
        // 这里可以更新UI上的进度显示
    }

    /// <summary>
    /// 隐藏任务UI
    /// </summary>
    private void HideQuestUI()
    {
        Debug.Log("任务UI隐藏");
        // 这里可以隐藏任务UI面板
    }

    /// <summary>
    /// 获取任务进度
    /// </summary>
    public float GetQuestProgress()
    {
        return totalPendantsInScene > 0 ? (float)pendantsCollected / totalPendantsInScene : 0f;
    }

    /// <summary>
    /// 检查任务是否完成
    /// </summary>
    public bool IsQuestCompleted()
    {
        return questCompleted;
    }

    void OnGUI()
    {
        if (showQuestUI && !questCompleted)
        {
            // 显示任务进度
            GUIStyle style = new GUIStyle();
            style.fontSize = 18;
            style.normal.textColor = Color.white;
            style.fontStyle = FontStyle.Bold;

            string questText = $"任务: {questName}\n" +
                             $"进度: {pendantsCollected}/{totalPendantsInScene}";

            GUI.Label(new Rect(10, 10, 300, 60), questText, style);
        }
    }
}
