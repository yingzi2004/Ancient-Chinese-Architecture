using UnityEngine;
using UnityEngine.UI; 
using TMPro;       
using System.Collections;

public class LevelUnlocker : MonoBehaviour
{
    [Header("解锁配置")]
    public int unlockIndexToLog = 1;

    [Header("UI提示 (可选)")]
    public string hintMessage = "解锁：苏州园林";
    public Text legacyText;
    public TMP_Text tmpText;
    public TextMesh textMesh;
    public float displayDuration = 3f;

    [Header("触发模式")]
    public bool unlockOnEnable = false;

    private void Start()
    {
        // 初始化隐藏提示UI
        if (legacyText != null) legacyText.gameObject.SetActive(false);
        if (tmpText != null) tmpText.gameObject.SetActive(false);
        if (textMesh != null) textMesh.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        if (unlockOnEnable)
        {
            UnlockNextLevel();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            UnlockNextLevel();
        }
    }

    private void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Player"))
        {
            UnlockNextLevel();
        }
    }

    public void UnlockNextLevel()
    {
        PopupMapController[] allMapControllers = Resources.FindObjectsOfTypeAll<PopupMapController>();
        bool foundAny = false;

        foreach (var mapController in allMapControllers)
        {
            if (mapController.gameObject.scene.isLoaded)
            {
                foundAny = true;
                if (unlockIndexToLog >= 0 && unlockIndexToLog < mapController.unlockedArray.Length)
                {
                    mapController.unlockedArray[unlockIndexToLog] = true;
                    mapController.RefreshMapVisuals(unlockIndexToLog);
                }
                else
                {
                    Debug.LogWarning($"[LevelUnlocker] Index {unlockIndexToLog} out of bounds for {mapController.name}");
                }
            }
        }

        if (!foundAny)
        {
            Debug.LogWarning("[LevelUnlocker] PopupMapController not found in scene.");
        }

        // 无论如何，弹出屏幕文字提示
        ShowHintMessage();
    }

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
    
    [ContextMenu("一键清除所有进度 (测试用)")]
    public void ResetAllProgress()
    {
        PlayerPrefs.DeleteKey("UnlockedBuildingIndex");
        Debug.Log("已删除旧的 PlayerPrefs 存档记录！（现在游戏已全面改用面板控制）");
    }
}