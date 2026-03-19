using UnityEngine;
using UnityEngine.UI; // 支持旧版 Text
using TMPro;          // 支持 TextMeshPro
using System.Collections;

/// <summary>
/// 场景解锁器
/// 挂载到任何你想触发解锁的物体（按钮、触发器、或代码直接调用）上即可解锁下一关
/// </summary>
public class LevelUnlocker : MonoBehaviour
{
    [Header("要解锁的下一关是第几个？")]
    [Tooltip("例如：通关了序号0(土楼)，这里填1，就会解锁序号1(苏州)")]
    public int unlockIndexToLog = 1;

    [Header("【可选功能】解锁时的屏幕文字提示")]
    [Tooltip("你想显示的具体文字，例如：解锁苏州园林")]
    public string hintMessage = "解锁：苏州园林";
    [Tooltip("如果是旧版 Text 组件，拖到这里（可选）")]
    public Text legacyText;
    [Tooltip("如果是 TextMeshPro 组件，拖到这里（可选）")]
    public TMP_Text tmpText;
    [Tooltip("如果是 3D TextMesh组件，拖到这里（可选）")]
    public TextMesh textMesh;
    [Tooltip("提示文字在屏幕上显示几秒后消失？")]
    public float displayDuration = 3f;

    [Header("【触发模式】")]
    [Tooltip("勾选：一出来就解锁；取消勾选：必须通过其他脚本（如ProximityTrigger）主动调用")]
    public bool unlockOnEnable = false;

    /// <summary>
    /// 最无脑的解锁方式：只要这个加了该脚本的物体被激活(Active=True)，就瞬间解锁！
    /// </summary>
    private void Start()
    {
        // 游戏一开始，强制把所有提示文字隐藏起来！
        // 确保就算你在场景里勾选着显示文字，一按Play也会被代码自动藏起来。
        if (legacyText != null) legacyText.gameObject.SetActive(false);
        if (tmpText != null) tmpText.gameObject.SetActive(false);
        if (textMesh != null) textMesh.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (unlockOnEnable)
        {
            Debug.Log($"<color=orange>[LevelUnlocker - 调试]</color> 脚本被激活（且开启了【一加载就解锁】）！物体名字：{gameObject.name}");
            UnlockNextLevel();
        }
    }

    /// <summary>
    /// 当带有 "Player" 标签的物体进入触发器时，自动触发解锁
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        Debug.Log($"<color=yellow>[LevelUnlocker - 触发测试]</color> 有物体进入了触发器！该物体的名字是：{other.name}，它的Tag是：{other.tag}");
        
        if (other.CompareTag("Player"))
        {
            Debug.Log($"<color=green>[LevelUnlocker - 触发]</color> 玩家正确碰撞到了解锁区域！");
            UnlockNextLevel();
        }
        else
        {
            Debug.LogWarning($"<color=red>碰撞失败说明：</color> 撞到解锁区域的物体名叫 '{other.name}'，但它的标签(Tag)不是 'Player' 而是 '{other.tag}'，因此无法解锁。请设置Tag为Player！");
        }
    }

    /// <summary>
    /// 防止你忘记勾选 Is Trigger 导致的普通的物理碰撞
    /// </summary>
    private void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"<color=yellow>[LevelUnlocker - 物理碰撞测试]</color> 发生了实体碰撞！该物体的名字是：{collision.gameObject.name}，它的Tag是：{collision.gameObject.tag}。(建议把解锁物体的Collider勾选上 Is Trigger)");
        
        if (collision.gameObject.CompareTag("Player"))
        {
            Debug.Log($"<color=green>[LevelUnlocker - 触发]</color> 玩家物理碰撞到了解锁区域！");
            UnlockNextLevel();
        }
    }

    /// <summary>
    /// 核心解锁方法（可供其他代码调用）
    /// </summary>
    public void UnlockNextLevel()
    {
        // 获取当前的解锁进度
        int currentLevel = PlayerPrefs.GetInt("UnlockedBuildingIndex", 0);
        
        Debug.Log($"<color=orange>[LevelUnlocker - 调试]</color> 正在检查解锁条件。当前身上挂着的解锁目标序号是：{unlockIndexToLog}，玩家目前的真实进度是：{currentLevel}");

        // 如果要解锁的关卡比当前的大，就更新覆盖
        if (unlockIndexToLog > currentLevel)
        {
            PlayerPrefs.SetInt("UnlockedBuildingIndex", unlockIndexToLog);
            PlayerPrefs.Save(); // 强制存盘，防止中途退出丢失
            Debug.Log($"<color=green>【进度更新】恭喜！条件满足！已成功把进度推到了第 {unlockIndexToLog} 个场景！</color>");

            // 弹出屏幕文字提示
            ShowHintMessage();
        }
        else
        {
            Debug.Log($"<color=yellow>[LevelUnlocker - 调试]</color> 拦截：玩家当前的进度（{currentLevel}）已经 >= 目标进度（{unlockIndexToLog}）了。无需重复解锁，但为了测试，依然弹出文字！");
            
            // 【如果你希望玩家重玩这一关时依然弹出文字，就把下面这行注释取消】
            ShowHintMessage(); 
        }
    }

    /// <summary>
    /// 显示解锁文字提示
    /// </summary>
    private void ShowHintMessage()
    {
        bool hasShown = false;
        Debug.Log($"<color=orange>[LevelUnlocker - 调试]</color> 准备弹出文字，预设文本为：{hintMessage}");

        // 如果拖了旧版Text
        if (legacyText != null)
        {
            legacyText.text = hintMessage;
            legacyText.gameObject.SetActive(true);
            hasShown = true;
            Debug.Log("<color=cyan> -> 成功启用了 Legacy Text</color>");
        }

        // 如果拖了TextMeshPro
        if (tmpText != null)
        {
            tmpText.text = hintMessage;
            tmpText.gameObject.SetActive(true);
            hasShown = true;
            Debug.Log("<color=cyan> -> 成功启用了 TMP Text</color>");
        }

        // 如果拖了老的 3D TextMesh
        if (textMesh != null)
        {
            textMesh.text = hintMessage;
            textMesh.gameObject.SetActive(true);
            hasShown = true;
            Debug.Log("<color=cyan> -> 成功启用了 3D TextMesh</color>");
        }

        if (!hasShown)
        {
            Debug.LogWarning("<color=red>[LevelUnlocker - 警告]</color> 触发了解锁，但你没有在面板里拖入任何一种文字组件，所以无法显示屏幕提示。");
        }

        // 启动延迟关闭
        if (hasShown)
        {
            if (gameObject.activeInHierarchy)
            {
                Debug.Log($"<color=orange>[LevelUnlocker - 调试]</color> 文字显现成功，开启 {displayDuration} 秒后的自动隐藏计时器...");
                StartCoroutine(HideHintCoroutine());
            }
            else
            {
                Debug.LogError("<color=red>[LevelUnlocker - 错误]</color> 这个存有代码的物体被立刻判定为未激活(可能被上一层代码瞬间关闭了)，无法执行延时隐藏的协程！");
            }
        }
    }

    private IEnumerator HideHintCoroutine()
    {
        // 等待设定的秒数
        yield return new WaitForSeconds(displayDuration);

        Debug.Log($"<color=orange>[LevelUnlocker - 调试]</color> {displayDuration} 秒时间到，正在尝试隐藏文字...");
        
        // 自动隐藏文字
        if (legacyText != null) legacyText.gameObject.SetActive(false);
        if (tmpText != null) tmpText.gameObject.SetActive(false);
        if (textMesh != null) textMesh.gameObject.SetActive(false);
        
        Debug.Log("<color=green>自动隐藏文字完成。</color>");
    }
    
    /// <summary>
    /// 用于测试：清除所有的解锁进度（回到只解锁土楼状态）
    /// </summary>
    [ContextMenu("一键清除所有进度 (测试用)")]
    public void ResetAllProgress()
    {
        PlayerPrefs.SetInt("UnlockedBuildingIndex", 0);
        PlayerPrefs.Save();
        Debug.Log("【进度重置】已退回最初状态，当前只有土楼可交互。");
    }
}