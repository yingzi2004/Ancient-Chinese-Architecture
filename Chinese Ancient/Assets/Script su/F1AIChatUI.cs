using System.Collections.Generic;
using UnityEngine;

public class F1AIChatUI : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private QwenChatClient chatClient;

    [Header("按键")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Header("UI 设置")]
    [SerializeField] private bool defaultOpen = false;
    [SerializeField] private Rect windowRect = new Rect(30f, 30f, 620f, 520f);
    [SerializeField] private int fontSize = 18;
    [SerializeField] private Color textColor = Color.white;
    [SerializeField] private string windowTitle = "AI 对话（千问）";

    [Header("背景设置")]
    [Tooltip("窗口背景图片（可选，留空则使用纯色）")]
    [SerializeField] private Texture2D backgroundTexture;
    [Tooltip("背景颜色（无背景图时生效）")]
    [SerializeField] private Color backgroundColor = new Color(0.2f, 0.2f, 0.2f, 0.95f);
    [Tooltip("内容区域背景颜色")]
    [SerializeField] private Color contentBackgroundColor = new Color(0.1f, 0.1f, 0.1f, 0.8f);

    [Header("标题栏样式")]
    [SerializeField] private Color titleBarColor = new Color(0.15f, 0.15f, 0.15f, 1f);
    [SerializeField] private Color titleTextColor = Color.white;
    [SerializeField] private int titleFontSize = 16;

    [Header("按钮样式")]
    [SerializeField] private Color buttonNormalColor = new Color(0.3f, 0.3f, 0.3f, 1f);
    [SerializeField] private Color buttonHoverColor = new Color(0.4f, 0.4f, 0.4f, 1f);
    [SerializeField] private Color buttonActiveColor = new Color(0.2f, 0.5f, 0.8f, 1f);
    [SerializeField] private Color buttonTextColor = Color.white;

    [Header("输入框样式")]
    [SerializeField] private Color inputBackgroundColor = new Color(0.15f, 0.15f, 0.15f, 0.9f);
    [SerializeField] private Color inputTextColor = Color.white;

    [Header("光标控制")]
    [SerializeField] private bool controlCursor = true;
    [SerializeField] private CursorLockMode lockModeWhenClosed = CursorLockMode.Locked;

    [Header("语音设置")]
    [SerializeField] private bool enableVoice = true;
    [SerializeField] private AliyunTTSClient ttsClient;

    private bool isOpen;
    private string userInput = string.Empty;
    private Vector2 scrollPosition;
    private readonly List<string> lines = new List<string>();
    private GUIStyle windowStyle;
    private GUIStyle backgroundStyle;
    private GUIStyle buttonStyle;
    private GUIStyle inputStyle;
    private GUIStyle labelStyle;
    private GUIStyle titleStyle;
    private bool stylesInitialized = false;
    private PlayerController playerController;

    private void Awake()
    {
        if (chatClient == null)
        {
            chatClient = FindFirstObjectByType<QwenChatClient>();
        }
        
        if (ttsClient == null)
        {
            ttsClient = FindFirstObjectByType<AliyunTTSClient>();
        }
        
        // 查找玩家控制器，用于暂停视角控制
        playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Start()
    {
        isOpen = defaultOpen;
        UpdateCursorState();

        if (lines.Count == 0)
        {
            lines.Add("[系统] 按 F1 可打开/关闭 AI 对话。");
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            isOpen = !isOpen;
            if (!isOpen && ttsClient != null)
            {
                ttsClient.Stop();
            }
            UpdateCursorState();
        }
    }

    private void OnGUI()
    {
        if (!isOpen)
            return;

        if (!stylesInitialized)
        {
            InitializeStyles();
            stylesInitialized = true;
        }

        windowRect = GUI.Window(GetInstanceID(), windowRect, DrawWindow, windowTitle, windowStyle);
    }

    private void InitializeStyles()
    {
        // 窗口样式
        windowStyle = new GUIStyle(GUI.skin.window);
        windowStyle.fontSize = titleFontSize;
        windowStyle.normal.textColor = titleTextColor;
        windowStyle.active.textColor = titleTextColor;
        windowStyle.focused.textColor = titleTextColor;
        windowStyle.hover.textColor = titleTextColor;
        
        Texture2D windowBg;
        if (backgroundTexture != null)
        {
            windowBg = backgroundTexture;
        }
        else
        {
            windowBg = MakeTex(1, 1, backgroundColor);
        }

        // 设置所有状态的背景，防止点击/聚焦时背景消失
        windowStyle.normal.background = windowBg;
        windowStyle.active.background = windowBg;
        windowStyle.focused.background = windowBg;
        windowStyle.hover.background = windowBg;
        windowStyle.onNormal.background = windowBg;
        windowStyle.onActive.background = windowBg;
        windowStyle.onFocused.background = windowBg;
        windowStyle.onHover.background = windowBg;

        // 内容区域样式
        Texture2D contentBg = MakeTex(2, 2, contentBackgroundColor);
        backgroundStyle = new GUIStyle(GUI.skin.box);
        SetAllStateBackgrounds(backgroundStyle, contentBg);

        // 按钮样式
        buttonStyle = new GUIStyle(GUI.skin.button);
        buttonStyle.fontSize = fontSize;
        buttonStyle.normal.textColor = buttonTextColor;
        buttonStyle.hover.textColor = buttonTextColor;
        buttonStyle.active.textColor = buttonTextColor;
        buttonStyle.focused.textColor = buttonTextColor;
        
        Texture2D btnNormal = MakeTex(2, 2, buttonNormalColor);
        Texture2D btnHover = MakeTex(2, 2, buttonHoverColor);
        Texture2D btnActive = MakeTex(2, 2, buttonActiveColor);
        
        buttonStyle.normal.background = btnNormal;
        buttonStyle.hover.background = btnHover;
        buttonStyle.active.background = btnActive;
        buttonStyle.focused.background = btnNormal;
        buttonStyle.onNormal.background = btnNormal;
        buttonStyle.onHover.background = btnHover;
        buttonStyle.onActive.background = btnActive;
        buttonStyle.onFocused.background = btnNormal;

        // 输入框样式
        inputStyle = new GUIStyle(GUI.skin.textArea);
        inputStyle.fontSize = fontSize;
        inputStyle.normal.textColor = inputTextColor;
        inputStyle.focused.textColor = inputTextColor;
        inputStyle.active.textColor = inputTextColor;
        inputStyle.hover.textColor = inputTextColor;
        
        Texture2D inputBg = MakeTex(2, 2, inputBackgroundColor);
        SetAllStateBackgrounds(inputStyle, inputBg);

        // 文字标签样式
        labelStyle = new GUIStyle(GUI.skin.label);
        labelStyle.fontSize = fontSize;
        labelStyle.normal.textColor = textColor;
        labelStyle.wordWrap = true;

        // 标题样式
        titleStyle = new GUIStyle(GUI.skin.label);
        titleStyle.fontSize = titleFontSize;
        titleStyle.normal.textColor = titleTextColor;
        titleStyle.alignment = TextAnchor.MiddleCenter;
    }

    private void SetAllStateBackgrounds(GUIStyle style, Texture2D bg)
    {
        style.normal.background = bg;
        style.active.background = bg;
        style.focused.background = bg;
        style.hover.background = bg;
        style.onNormal.background = bg;
        style.onActive.background = bg;
        style.onFocused.background = bg;
        style.onHover.background = bg;
    }

    private Texture2D MakeTex(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;
        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }

    private void DrawWindow(int windowId)
    {
        float margin = 12f;
        float top = 30f;
        float contentWidth = windowRect.width - margin * 2f;
        float historyHeight = windowRect.height - 170f;

        Rect historyRect = new Rect(margin, top, contentWidth, historyHeight);
        Rect inputRect = new Rect(margin, top + historyHeight + 8f, contentWidth, 56f);

        // 绘制内容区域
        GUILayout.BeginArea(historyRect, backgroundStyle);
        scrollPosition = GUILayout.BeginScrollView(scrollPosition, GUILayout.ExpandWidth(true), GUILayout.ExpandHeight(true));
        for (int i = 0; i < lines.Count; i++)
        {
            GUILayout.Label(lines[i], labelStyle, GUILayout.ExpandWidth(true));
        }
        GUILayout.EndScrollView();
        GUILayout.EndArea();

        // 绘制输入框
        GUI.SetNextControlName("AIInputField");
        userInput = GUI.TextArea(inputRect, userInput, inputStyle);

        // 绘制按钮
        float buttonTop = inputRect.yMax + 8f;
        Rect sendBtnRect = new Rect(margin, buttonTop, 100f, 34f);
        Rect clearBtnRect = new Rect(margin + 110f, buttonTop, 100f, 34f);
        Rect voiceBtnRect = new Rect(margin + 220f, buttonTop, 100f, 34f);
        Rect closeBtnRect = new Rect(windowRect.width - margin - 100f, buttonTop, 100f, 34f);

        bool canSend = chatClient != null && !chatClient.IsRequesting;
        GUI.enabled = canSend;
        if (GUI.Button(sendBtnRect, "发送", buttonStyle) || HandleEnterSend())
        {
            SendCurrentInput();
        }
        GUI.enabled = true;

        if (GUI.Button(clearBtnRect, "清空对话", buttonStyle))
        {
            lines.Clear();
            lines.Add("[系统] 已清空当前面板显示。");
            if (chatClient != null) chatClient.ClearHistory();
            ScrollToBottom();
        }

        // 语音开关按钮
        string voiceBtnText = enableVoice ? "🔊 语音开" : "🔇 语音关";
        if (GUI.Button(voiceBtnRect, voiceBtnText, buttonStyle))
        {
            enableVoice = !enableVoice;
            if (!enableVoice && ttsClient != null)
            {
                ttsClient.Stop();
            }
        }

        if (GUI.Button(closeBtnRect, "关闭(F1)", buttonStyle))
        {
            isOpen = false;
            if (ttsClient != null) ttsClient.Stop();
            UpdateCursorState();
        }

        if (chatClient != null && chatClient.IsRequesting)
        {
            GUI.Label(new Rect(margin + 265f, buttonTop + 6f, 250f, 24f), "AI 正在思考...", labelStyle);
        }

        GUI.DragWindow(new Rect(0f, 0f, windowRect.width, 26f));
    }

    // ============ 公开的运行时UI控制接口 ============

    /// <summary>
    /// 设置窗口大小和位置
    /// </summary>
    public void SetWindowRect(float x, float y, float width, float height)
    {
        windowRect = new Rect(x, y, width, height);
    }

    /// <summary>
    /// 设置窗口大小
    /// </summary>
    public void SetWindowSize(float width, float height)
    {
        windowRect.width = width;
        windowRect.height = height;
    }

    /// <summary>
    /// 设置窗口位置
    /// </summary>
    public void SetWindowPosition(float x, float y)
    {
        windowRect.x = x;
        windowRect.y = y;
    }

    /// <summary>
    /// 设置字体大小
    /// </summary>
    public void SetFontSize(int size)
    {
        fontSize = Mathf.Max(8, size);
    }

    /// <summary>
    /// 设置文字颜色
    /// </summary>
    public void SetTextColor(Color color)
    {
        textColor = color;
    }

    /// <summary>
    /// 设置背景颜色（纯色模式）
    /// </summary>
    public void SetBackgroundColor(Color color)
    {
        backgroundColor = color;
        RefreshStyles();
    }

    /// <summary>
    /// 设置内容区域背景颜色
    /// </summary>
    public void SetContentBackgroundColor(Color color)
    {
        contentBackgroundColor = color;
        RefreshStyles();
    }

    /// <summary>
    /// 设置背景纹理
    /// </summary>
    public void SetBackgroundTexture(Texture2D texture)
    {
        backgroundTexture = texture;
        RefreshStyles();
    }

    /// <summary>
    /// 清除背景纹理，使用纯色背景
    /// </summary>
    public void ClearBackgroundTexture()
    {
        backgroundTexture = null;
        RefreshStyles();
    }

    /// <summary>
    /// 强制刷新UI样式
    /// </summary>
    public void RefreshStyles()
    {
        stylesInitialized = false;
    }

    /// <summary>
    /// 打开/关闭对话窗口
    /// </summary>
    public void ToggleWindow()
    {
        isOpen = !isOpen;
        if (!isOpen && ttsClient != null) ttsClient.Stop();
        UpdateCursorState();
    }

    /// <summary>
    /// 设置窗口开关状态
    /// </summary>
    public void SetWindowOpen(bool open)
    {
        isOpen = open;
        if (!isOpen && ttsClient != null) ttsClient.Stop();
        UpdateCursorState();
    }

    /// <summary>
    /// 获取当前窗口是否打开
    /// </summary>
    public bool IsWindowOpen()
    {
        return isOpen;
    }

    /// <summary>
    /// 获取当前窗口矩形
    /// </summary>
    public Rect GetWindowRect()
    {
        return windowRect;
    }

    // ============ 按钮样式接口 ============

    /// <summary>
    /// 设置按钮颜色（正常、悬停、点击）
    /// </summary>
    public void SetButtonColors(Color normal, Color hover, Color active)
    {
        buttonNormalColor = normal;
        buttonHoverColor = hover;
        buttonActiveColor = active;
        RefreshStyles();
    }

    /// <summary>
    /// 设置按钮文字颜色
    /// </summary>
    public void SetButtonTextColor(Color color)
    {
        buttonTextColor = color;
        RefreshStyles();
    }

    // ============ 输入框样式接口 ============

    /// <summary>
    /// 设置输入框背景颜色
    /// </summary>
    public void SetInputBackgroundColor(Color color)
    {
        inputBackgroundColor = color;
        RefreshStyles();
    }

    /// <summary>
    /// 设置输入框文字颜色
    /// </summary>
    public void SetInputTextColor(Color color)
    {
        inputTextColor = color;
        RefreshStyles();
    }

    // ============ 标题栏样式接口 ============

    /// <summary>
    /// 设置窗口标题
    /// </summary>
    public void SetWindowTitle(string title)
    {
        windowTitle = title;
    }

    /// <summary>
    /// 设置标题栏颜色
    /// </summary>
    public void SetTitleBarColor(Color color)
    {
        titleBarColor = color;
        RefreshStyles();
    }

    /// <summary>
    /// 设置标题文字颜色
    /// </summary>
    public void SetTitleTextColor(Color color)
    {
        titleTextColor = color;
        RefreshStyles();
    }

    /// <summary>
    /// 设置标题字体大小
    /// </summary>
    public void SetTitleFontSize(int size)
    {
        titleFontSize = Mathf.Max(8, size);
        RefreshStyles();
    }

    private bool HandleEnterSend()
    {
        Event current = Event.current;
        if (current == null) return false;

        if (current.type == EventType.KeyDown && current.keyCode == KeyCode.Return && !current.shift)
        {
            current.Use();
            return true;
        }

        return false;
    }

    private void SendCurrentInput()
    {
        if (chatClient == null)
        {
            lines.Add("[错误] 未找到 QwenChatClient，请先挂载该组件。");
            ScrollToBottom();
            return;
        }

        string message = userInput == null ? string.Empty : userInput.Trim();
        if (string.IsNullOrWhiteSpace(message)) return;

        userInput = string.Empty;
        lines.Add("[我] " + message);
        lines.Add("[AI] ...");
        ScrollToBottom();

        chatClient.SendUserMessage(
            message,
            onSuccess: reply =>
            {
                ReplaceLastAiPlaceholder(reply);
                ScrollToBottom();
                
                // 语音朗读AI回复
                if (enableVoice && ttsClient != null)
                {
                    ttsClient.Speak(reply);
                }
            },
            onError: err =>
            {
                ReplaceLastAiPlaceholder("[错误] " + err);
                ScrollToBottom();
            });
    }

    private void ReplaceLastAiPlaceholder(string content)
    {
        for (int i = lines.Count - 1; i >= 0; i--)
        {
            if (lines[i] == "[AI] ...")
            {
                lines[i] = "[AI] " + content;
                return;
            }
        }

        lines.Add("[AI] " + content);
    }

    private void ScrollToBottom()
    {
        scrollPosition = new Vector2(0f, 100000f);
    }

    private void UpdateCursorState()
    {
        if (!controlCursor)
            return;

        if (isOpen)
        {
            // 显示鼠标，解锁光标
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // 暂停玩家视角控制
            if (playerController != null)
            {
                playerController.isInspecting = true;
            }
        }
        else
        {
            // 隐藏鼠标，锁定光标
            Cursor.visible = false;
            Cursor.lockState = lockModeWhenClosed;
            
            // 恢复玩家视角控制
            if (playerController != null)
            {
                playerController.isInspecting = false;
            }
        }
    }

    public void SetApiKey(string apiKey)
    {
        if (chatClient == null) return;
        chatClient.SetApiKey(apiKey);
    }

    public void SetPersona(string systemPrompt)
    {
        if (chatClient == null) return;
        chatClient.SetSystemPrompt(systemPrompt);
    }

    public void SetModel(string modelName)
    {
        if (chatClient == null) return;
        chatClient.SetModel(modelName);
    }

    // ============ 语音控制接口 ============

    /// <summary>
    /// 设置是否启用语音朗读
    /// </summary>
    public void SetVoiceEnabled(bool enabled)
    {
        enableVoice = enabled;
        if (!enabled && ttsClient != null)
        {
            ttsClient.Stop();
        }
    }

    /// <summary>
    /// 获取语音是否启用
    /// </summary>
    public bool IsVoiceEnabled()
    {
        return enableVoice;
    }

    /// <summary>
    /// 设置语音音色
    /// </summary>
    public void SetVoice(AliyunTTSClient.VoiceType voiceType)
    {
        if (ttsClient != null)
        {
            ttsClient.SetVoice(voiceType);
        }
    }

    /// <summary>
    /// 设置语音音量
    /// </summary>
    public void SetVoiceVolume(int volume)
    {
        if (ttsClient != null)
        {
            ttsClient.SetVolume(volume);
        }
    }

    /// <summary>
    /// 设置语音语速
    /// </summary>
    public void SetVoiceSpeechRate(int rate)
    {
        if (ttsClient != null)
        {
            ttsClient.SetSpeechRate(rate);
        }
    }

    /// <summary>
    /// 停止当前语音播放
    /// </summary>
    public void StopVoice()
    {
        if (ttsClient != null)
        {
            ttsClient.Stop();
        }
    }

    /// <summary>
    /// 朗读指定文本
    /// </summary>
    public void SpeakText(string text)
    {
        if (ttsClient != null && enableVoice)
        {
            ttsClient.Speak(text);
        }
    }
}