using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

/// <summary>
/// 问答答案按钮 - 处理单个答案选项的显示和交互
/// </summary>
public class QuizButton : MonoBehaviour
{
    [Header("--- 组件引用 ---")]
    public Button button;                   // 按钮组件
    public TextMeshProUGUI buttonText;      // 按钮文本
    public Image backgroundImage;           // 按钮背景图片

    [Header("--- 颜色设置 ---")]
    public Color defaultColor = Color.white;
    public Color hoverColor = new Color(0.9f, 0.9f, 1f);
    public Color correctColor = Color.green;
    public Color wrongColor = Color.red;

    private int answerIndex;
    private QuizManager quizManager;
    private Color originalColor;

    private void Awake()
    {
        // 自动获取组件
        if (button == null)
            button = GetComponent<Button>();
        if (backgroundImage == null)
            backgroundImage = GetComponent<Image>();
        if (buttonText == null)
            buttonText = GetComponentInChildren<TextMeshProUGUI>();

        // 保存原始颜色
        if (backgroundImage != null)
            originalColor = backgroundImage.color;

        // 绑定点击事件
        if (button != null)
            button.onClick.AddListener(OnClick);
    }

    private void OnDestroy()
    {
        // 清理事件绑定
        if (button != null)
            button.onClick.RemoveListener(OnClick);
    }

    /// <summary>
    /// 初始化按钮
    /// </summary>
    public void Initialize(string optionText, int index, QuizManager manager)
    {
        answerIndex = index;
        quizManager = manager;

        if (buttonText != null)
            buttonText.text = $"{(char)('A' + index)}. {optionText}";

        ResetColor();
    }

    /// <summary>
    /// 设置按钮是否可交互
    /// </summary>
    public void SetInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    /// <summary>
    /// 设置按钮颜色
    /// </summary>
    public void SetColor(Color color)
    {
        if (backgroundImage != null)
            backgroundImage.color = color;
    }

    /// <summary>
    /// 重置按钮颜色为默认值
    /// </summary>
    public void ResetColor()
    {
        SetColor(originalColor);
    }

    private void OnClick()
    {
        if (quizManager != null)
        {
            quizManager.SelectAnswer(answerIndex);
        }
    }

    // 以下是为了在 Inspector 中方便设置的辅助方法
    private void OnValidate()
    {
        // 在编辑器中实时更新颜色预览
        if (backgroundImage != null)
            originalColor = backgroundImage.color;
    }
}
