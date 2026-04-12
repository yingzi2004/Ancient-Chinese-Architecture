using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// 祈福灯UI控制器
/// 处理祈福界面的显示和交互
/// </summary>
public class PrayerLanternUI : MonoBehaviour
{
    public static PrayerLanternUI Instance { get; private set; }

    [Header("UI面板引用")]
    [Tooltip("主面板")]
    public GameObject mainPanel;

    [Tooltip("输入面板")]
    public GameObject inputPanel;

    [Tooltip("颜色选择面板")]
    public GameObject colorPanel;

    [Header("输入UI元素")]
    [Tooltip("祈福内容输入框")]
    public TMP_InputField wishInputField;

    [Tooltip("姓名输入框")]
    public TMP_InputField nameInputField;

    [Tooltip("释放按钮")]
    public Button releaseButton;

    [Tooltip("取消按钮")]
    public Button cancelButton;

    [Header("快捷祝福按钮")]
    [Tooltip("快捷祝福按钮数组")]
    public Button[] quickWishButtons;

    [Tooltip("快捷祝福内容")]
    public string[] quickWishes = new string[]
    {
        "国泰民安",
        "身体健康",
        "家庭幸福",
        "心想事成",
        "学业进步",
        "工作顺利",
        "财源广进",
        "万事如意"
    };

    [Header("交互触发")]
    [Tooltip("交互触发器（如NPC或物体）")]
    public GameObject interactTrigger;

    [Tooltip("交互距离")]
    public float interactDistance = 5f;

    [Tooltip("提示文本")]
    public Text promptText;

    [Header("设置")]
    [Tooltip("祈福内容最小长度")]
    public int minWishLength = 2;

    [Tooltip("祈福内容最大长度")]
    public int maxWishLength = 50;

    [Tooltip("默认祈福人名称")]
    public string defaultPlayerName = "祈福者";

    // 私有变量
    private bool isUIActive = false;
    private PlayerController playerController;
    private Canvas canvas;

    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        canvas = GetComponent<Canvas>();
    }

    void Start()
    {
        // 查找玩家控制器
        playerController = FindObjectOfType<PlayerController>();

        // 初始化UI
        InitializeUI();

        // 初始隐藏UI
        HideUI();

        // 设置快捷祝福按钮
        SetupQuickWishButtons();
    }

    void Update()
    {
        // 检测玩家是否在交互范围内
        CheckPlayerDistance();

        // 按ESC关闭UI
        if (isUIActive && Input.GetKeyDown(KeyCode.Escape))
        {
            HideUI();
        }
    }

    /// <summary>
    /// 初始化UI
    /// </summary>
    private void InitializeUI()
    {
        // 设置释放按钮事件
        if (releaseButton != null)
        {
            releaseButton.onClick.AddListener(OnReleaseButtonClicked);
        }

        // 设置取消按钮事件
        if (cancelButton != null)
        {
            cancelButton.onClick.AddListener(HideUI);
        }

        // 设置输入框字符限制
        if (wishInputField != null)
        {
            wishInputField.characterLimit = maxWishLength;
        }

        // 设置默认名称
        if (nameInputField != null && string.IsNullOrEmpty(nameInputField.text))
        {
            nameInputField.text = defaultPlayerName;
        }
    }

    /// <summary>
    /// 设置快捷祝福按钮
    /// </summary>
    private void SetupQuickWishButtons()
    {
        if (quickWishButtons != null && quickWishButtons.Length > 0)
        {
            for (int i = 0; i < quickWishButtons.Length && i < quickWishes.Length; i++)
            {
                int index = i; // 保存索引
                if (quickWishButtons[i] != null)
                {
                    // 设置按钮文本
                    Text buttonText = quickWishButtons[i].GetComponentInChildren<Text>();
                    if (buttonText != null)
                    {
                        buttonText.text = quickWishes[i];
                    }

                    // 设置点击事件
                    quickWishButtons[i].onClick.AddListener(() => OnQuickWishButtonClicked(index));
                }
            }
        }
    }

    /// <summary>
    /// 检测玩家距离
    /// </summary>
    private void CheckPlayerDistance()
    {
        if (interactTrigger == null || Camera.main == null) return;

        float distance = Vector3.Distance(interactTrigger.transform.position, Camera.main.transform.position);

        // 显示/隐藏提示文本
        if (promptText != null)
        {
            promptText.gameObject.SetActive(distance <= interactDistance && !isUIActive);
        }

        // 检测交互按键（如F键或E键）
        if (distance <= interactDistance && !isUIActive && Input.GetKeyDown(KeyCode.F))
        {
            ShowUI();
        }
    }

    /// <summary>
    /// 显示UI
    /// </summary>
    public void ShowUI()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(true);
            isUIActive = true;

            // 锁定玩家移动
            LockPlayerMovement();

            // 显示光标
            ShowCursor();

            // 聚焦到输入框
            if (wishInputField != null)
            {
                wishInputField.Select();
                wishInputField.ActivateInputField();
            }
        }
    }

    /// <summary>
    /// 隐藏UI
    /// </summary>
    public void HideUI()
    {
        if (mainPanel != null)
        {
            mainPanel.SetActive(false);
            isUIActive = false;

            // 解锁玩家移动
            UnlockPlayerMovement();

            // 隐藏光标
            HideCursor();
        }
    }

    /// <summary>
    /// 释放按钮点击事件
    /// </summary>
    private void OnReleaseButtonClicked()
    {
        // 验证输入
        if (!ValidateInput())
        {
            return;
        }

        // 获取祈福内容
        string wishText = wishInputField.text.Trim();
        string playerName = nameInputField.text.Trim();

        if (string.IsNullOrEmpty(playerName))
        {
            playerName = defaultPlayerName;
        }

        // 添加祈福
        if (PrayerLanternManager.Instance != null)
        {
            PrayerLanternManager.Instance.AddPrayer(wishText, playerName);

            // 清空输入框
            wishInputField.text = "";

            Debug.Log($"祈福成功: {wishText}");

            // 隐藏UI
            HideUI();
        }
        else
        {
            Debug.LogError("PrayerLanternManager实例不存在！");
        }
    }

    /// <summary>
    /// 快捷祝福按钮点击事件
    /// </summary>
    private void OnQuickWishButtonClicked(int index)
    {
        if (index >= 0 && index < quickWishes.Length)
        {
            if (wishInputField != null)
            {
                wishInputField.text = quickWishes[index];
            }
        }
    }

    /// <summary>
    /// 验证输入
    /// </summary>
    private bool ValidateInput()
    {
        if (wishInputField == null) return false;

        string wishText = wishInputField.text.Trim();

        // 检查长度
        if (wishText.Length < minWishLength)
        {
            Debug.Log($"祈福内容太短，至少需要{minWishLength}个字");
            return false;
        }

        if (wishText.Length > maxWishLength)
        {
            Debug.Log($"祈福内容太长，最多{maxWishLength}个字");
            return false;
        }

        return true;
    }

    /// <summary>
    /// 锁定玩家移动
    /// </summary>
    private void LockPlayerMovement()
    {
        if (playerController != null)
        {
            playerController.isInspecting = true;
        }
    }

    /// <summary>
    /// 解锁玩家移动
    /// </summary>
    private void UnlockPlayerMovement()
    {
        if (playerController != null)
        {
            playerController.isInspecting = false;
        }
    }

    /// <summary>
    /// 显示光标
    /// </summary>
    private void ShowCursor()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// 隐藏光标
    /// </summary>
    private void HideCursor()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// 从其他脚本调用的公共方法
    /// </summary>
    public void ToggleUI()
    {
        if (isUIActive)
        {
            HideUI();
        }
        else
        {
            ShowUI();
        }
    }
}
