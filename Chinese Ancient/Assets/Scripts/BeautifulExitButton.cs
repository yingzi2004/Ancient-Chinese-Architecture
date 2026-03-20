using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using System.Collections;
using DG.Tweening;  // 使用DOTween

/// <summary>
/// 美化的纸牌游戏退出按钮：带有动画效果、音效和视觉反馈
/// 使用方法：
/// 1. 将此脚本挂载到退出按钮上
/// 2. 可选：添加AudioSource组件配置点击音效
/// </summary>
public class BeautifulExitButton : MonoBehaviour
{
    [Header("场景设置")]
    [Tooltip("要返回的场景名称")]
    public string targetSceneName = "京 Exhibition";

    [Header("按钮组件")]
    public Button exitButton;
    public Text buttonText;
    public Image buttonImage;
    public Image iconImage;

    [Header("颜色配置")]
    public Color normalColor = new Color(1f, 0.42f, 0.42f);      // 红色 #FF6B6B
    public Color highlightColor = new Color(1f, 0.53f, 0.53f);    // 浅红色 #FF8888
    public Color pressedColor = new Color(0.8f, 0.33f, 0.33f);    // 深红色 #CC5555
    public Color textColor = Color.white;

    [Header("动画效果")]
    [Tooltip("是否启用悬停放大动画")]
    public bool enableHoverAnimation = true;
    [Tooltip("悬停时的放大比例")]
    [Range(0.8f, 1.5f)]
    public float hoverScale = 1.1f;
    [Tooltip("动画过渡速度")]
    [Range(0.1f, 5f)]
    public float animationSpeed = 3f;

    [Header("按钮点击效果")]
    [Tooltip("点击时是否缩小一下")]
    public bool enableClickEffect = true;
    [Tooltip("点击时的缩小比例")]
    [Range(0.5f, 1f)]
    public float clickScale = 0.9f;

    [Header("音效")]
    [Tooltip("点击音效（可选）")]
    public AudioClip clickSound;
    private AudioSource audioSource;

    [Header("脉冲动画（吸引注意力）")]
    [Tooltip("是否启用脉冲呼吸效果")]
    public bool enablePulseAnimation = true;
    [Tooltip("脉冲速度")]
    [Range(0.5f, 3f)]
    public float pulseSpeed = 1.5f;
    [Tooltip("脉冲幅度")]
    [Range(0.05f, 0.3f)]
    public float pulseAmount = 0.1f;

    [Header("阴影效果")]
    [Tooltip("是否添加阴影")]
    public bool enableShadow = true;
    public Color shadowColor = new Color(0f, 0f, 0f, 0.5f);
    public Vector2 shadowOffset = new Vector2(3, -3);

    private RectTransform rectTransform;
    private Vector3 originalScale;
    private Sequence pulseSequence;
    private Outline outline;
    private Shadow shadow;

    void Start()
    {
        // 初始化组件引用
        InitializeComponents();

        // 设置原始缩放
        originalScale = rectTransform.localScale;

        // 应用样式
        ApplyButtonStyle();

        // 绑定事件
        exitButton.onClick.AddListener(OnButtonClick);

        // 添加事件监听器
        AddEventListeners();

        // 启动脉冲动画
        if (enablePulseAnimation)
        {
            StartPulseAnimation();
        }
    }

    void InitializeComponents()
    {
        // 获取或添加组件
        if (exitButton == null)
            exitButton = GetComponent<Button>();
        if (buttonImage == null)
            buttonImage = GetComponent<Image>();
        if (rectTransform == null)
            rectTransform = GetComponent<RectTransform>();

        // 获取Text组件（可能在子对象中）
        if (buttonText == null)
            buttonText = GetComponentInChildren<Text>();

        // 初始化AudioSource
        if (clickSound != null && audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
            audioSource.playOnAwake = false;
            audioSource.clip = clickSound;
        }

        // 添加阴影组件
        if (enableShadow)
        {
            AddShadowEffect();
        }
    }

    void ApplyButtonStyle()
    {
        // 设置颜色块
        ColorBlock colorBlock = exitButton.colors;
        colorBlock.normalColor = normalColor;
        colorBlock.highlightedColor = highlightColor;
        colorBlock.pressedColor = pressedColor;
        colorBlock.selectedColor = normalColor;
        colorBlock.disabledColor = new Color(0.5f, 0.5f, 0.5f, 0.5f);
        colorBlock.colorMultiplier = 1f;
        exitButton.colors = colorBlock;

        // 设置文本颜色
        if (buttonText != null)
        {
            buttonText.color = textColor;
        }
    }

    void AddShadowEffect()
    {
        // 添加或获取Shadow组件
        shadow = GetComponent<Shadow>();
        if (shadow == null)
        {
            shadow = gameObject.AddComponent<Shadow>();
        }
        shadow.effectColor = shadowColor;
        shadow.effectDistance = shadowOffset;
    }

    void AddEventListeners()
    {
        // 添加指针进入事件
        exitButton.transition = Selectable.Transition.ColorTint;

        // 使用EventTrigger添加悬停效果
        EventTrigger eventTrigger = gameObject.GetComponent<EventTrigger>();
        if (eventTrigger == null)
        {
            eventTrigger = gameObject.AddComponent<EventTrigger>();
        }

        // 清除旧的事件
        eventTrigger.triggers.Clear();

        // 添加指针进入事件
        EventTrigger.Entry enterEntry = new EventTrigger.Entry();
        enterEntry.eventID = EventTriggerType.PointerEnter;
        enterEntry.callback.AddListener((data) => OnPointerEnter());
        eventTrigger.triggers.Add(enterEntry);

        // 添加指针退出事件
        EventTrigger.Entry exitEntry = new EventTrigger.Entry();
        exitEntry.eventID = EventTriggerType.PointerExit;
        exitEntry.callback.AddListener((data) => OnPointerExit());
        eventTrigger.triggers.Add(exitEntry);
    }

    void OnPointerEnter()
    {
        if (enableHoverAnimation)
        {
            StopPulseAnimation();
            // 杀死所有DOTween动画
            rectTransform.DOKill();
            rectTransform.DOScale(originalScale * hoverScale, 1f / animationSpeed).SetEase(Ease.OutBack);
        }
    }

    void OnPointerExit()
    {
        if (enableHoverAnimation)
        {
            rectTransform.DOScale(originalScale, 1f / animationSpeed).SetEase(Ease.OutBack);
            if (enablePulseAnimation)
            {
                Invoke("StartPulseAnimation", 1f / animationSpeed);
            }
        }
    }

    void OnButtonClick()
    {
        // 播放音效
        if (audioSource != null && clickSound != null)
        {
            audioSource.Play();
        }

        // 点击效果
        if (enableClickEffect)
        {
            StartCoroutine(ClickAnimation());
        }
        else
        {
            LoadScene();
        }
    }

    IEnumerator ClickAnimation()
    {
        // 停止所有动画
        StopPulseAnimation();
        rectTransform.DOKill();

        // 缩小
        rectTransform.DOScale(originalScale * clickScale, 0.1f).SetEase(Ease.InQuad);

        yield return new WaitForSeconds(0.1f);

        // 恢复并加载场景
        rectTransform.DOScale(originalScale, 0.1f).SetEase(Ease.OutQuad);

        yield return new WaitForSeconds(0.1f);

        LoadScene();
    }

    void LoadScene()
    {
        Debug.Log($"返回场景: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }

    void StartPulseAnimation()
    {
        StopPulseAnimation();

        // 使用DOTween创建脉冲动画
        pulseSequence = DOTween.Sequence();
        pulseSequence.Append(rectTransform.DOScale(originalScale * (1f + pulseAmount), 1f / pulseSpeed).SetEase(Ease.InOutSine));
        pulseSequence.Append(rectTransform.DOScale(originalScale, 1f / pulseSpeed).SetEase(Ease.InOutSine));
        pulseSequence.SetLoops(-1, LoopType.Restart);
    }

    void StopPulseAnimation()
    {
        if (pulseSequence != null)
        {
            pulseSequence.Kill();
            pulseSequence = null;
        }
        // 恢复原始大小
        rectTransform.DOKill();
        rectTransform.localScale = originalScale;
    }

    void OnDestroy()
    {
        // 清理DOTween动画
        rectTransform.DOKill();
        StopPulseAnimation();

        // 清理协程
        StopAllCoroutines();

        // 清理事件监听
        if (exitButton != null)
        {
            exitButton.onClick.RemoveListener(OnButtonClick);
        }
    }
}
