using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

/// <summary>
/// 纸牌游戏退出按钮：点击后返回到京Exhibition场景
/// 使用方法：
/// 1. 将此脚本挂载到退出按钮的GameObject上
/// 2. 在Unity中创建Canvas和Button
/// 3. 配置按钮位置在屏幕右上方
/// </summary>
public class CardGameExitButton : MonoBehaviour
{
    [Header("场景设置")]
    [Tooltip("要返回的场景名称")]
    public string targetSceneName = "京 Exhibition";

    [Header("按钮设置")]
    [Tooltip("退出按钮的Button组件")]
    public Button exitButton;

    [Header("提示信息（可选）")]
    [Tooltip("是否在控制台打印调试信息")]
    public bool debugMode = true;

    void Start()
    {
        // 如果没有手动指定按钮，自动获取当前GameObject上的Button组件
        if (exitButton == null)
        {
            exitButton = GetComponent<Button>();

            if (exitButton == null)
            {
                Debug.LogError("CardGameExitButton: 未找到Button组件！请在Inspector中拖入Button或确保此脚本挂载在Button上。");
                return;
            }
        }

        // 绑定点击事件
        exitButton.onClick.AddListener(OnExitButtonClick);

        if (debugMode)
        {
            Debug.Log($"退出按钮已初始化，目标场景: {targetSceneName}");
        }
    }

    /// <summary>
    /// 退出按钮点击事件处理
    /// </summary>
    void OnExitButtonClick()
    {
        if (debugMode)
        {
            Debug.Log($"玩家点击退出按钮，准备返回场景: {targetSceneName}");
        }

        // 加载目标场景
        SceneManager.LoadScene(targetSceneName);
    }

    void OnDestroy()
    {
        // 清理事件监听，防止内存泄漏
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnExitButtonClick);
        }
    }
}
