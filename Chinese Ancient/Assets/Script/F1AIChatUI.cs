using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class F1AIChatUI : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private QwenChatClient chatClient;

    [Header("UI引用 (需在Inspector中拖拽Canvas下的组件)")]
    [SerializeField] private GameObject chatPanel; // 整个聊天界面的根节点
    [SerializeField] private TMP_InputField inputField; // 用户输入框
    [SerializeField] private TextMeshProUGUI chatHistoryText; // 显示聊天记录的文本组件
    [SerializeField] private ScrollRect scrollRect; // 用来保证聊天内容自动滚动的组件
    
    [Header("按钮引用")]
    [SerializeField] private Button sendButton; // 发送按钮
    [SerializeField] private Button clearButton; // 清空按钮
    [SerializeField] private Button voiceButton; // 语音开关按钮
    [SerializeField] private Button closeButton; // 关闭按钮
    [SerializeField] private TextMeshProUGUI voiceButtonText; // 语音开关按钮上的文本

    [Header("按键")]
    [SerializeField] private KeyCode toggleKey = KeyCode.F1;

    [Header("配置")]
    [SerializeField] private bool defaultOpen = false;
    [SerializeField] private bool controlCursor = true;
    [SerializeField] private CursorLockMode lockModeWhenClosed = CursorLockMode.Locked;

    [Header("语音设置")]
    [SerializeField] private bool enableVoice = true;
    [SerializeField] private AliyunTTSClient ttsClient;

    private bool isOpen;
    private readonly List<string> lines = new List<string>();
    private PlayerController playerController;

    private void Awake()
    {
        if (chatClient == null)
            chatClient = FindFirstObjectByType<QwenChatClient>();
        
        if (ttsClient == null)
            ttsClient = FindFirstObjectByType<AliyunTTSClient>();
        
        playerController = FindFirstObjectByType<PlayerController>();
    }

    private void Start()
    {
        isOpen = defaultOpen;
        if (chatPanel != null)
            chatPanel.SetActive(isOpen);
            
        UpdateCursorState();
        UpdateVoiceButtonText();

        if (lines.Count == 0)
        {
            lines.Add("[系统] 按 F1 可打开/关闭 AI 对话。");
            RefreshChatDisplay();
        }

        // 绑定按钮事件
        if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
        if (clearButton != null) clearButton.onClick.AddListener(OnClearClicked);
        if (voiceButton != null) voiceButton.onClick.AddListener(OnVoiceToggleClicked);
        if (closeButton != null) closeButton.onClick.AddListener(CloseWindow);
    }

    private void Update()
    {
        // 监听开关按键
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleWindow();
        }

        // 按回车键发送，并且确保在聊天窗口激活时才生效
        if (isOpen && Input.GetKeyDown(KeyCode.Return))
        {
            // 如果同时按下Shift，允许在输入框中换行而不是发送；如果没有按下，则发送。
            if (!Input.GetKey(KeyCode.LeftShift) && !Input.GetKey(KeyCode.RightShift))
            {
                OnSendClicked();
            }
        }
    }

    private void OnSendClicked()
    {
        if (chatClient == null)
        {
            lines.Add("[错误] 未找到 QwenChatClient，请先挂载该组件。");
            RefreshChatDisplay();
            return;
        }
        
        if (chatClient.IsRequesting) return; // 正在请求中不允许再次发送

        string message = inputField.text == null ? string.Empty : inputField.text.Trim();
        if (string.IsNullOrWhiteSpace(message)) return;

        // 清空输入框并保持焦点（可选）
        if (inputField != null)
        {
            inputField.text = string.Empty;
            inputField.ActivateInputField();
        }

        lines.Add("[我] " + message);
        lines.Add("[AI] ...");
        RefreshChatDisplay();

        chatClient.SendUserMessage(
            message,
            onSuccess: reply =>
            {
                ReplaceLastAiPlaceholder(reply);
                RefreshChatDisplay();
                
                // 语音朗读AI回复
                if (enableVoice && ttsClient != null)
                {
                    ttsClient.Speak(reply);
                }
            },
            onError: err =>
            {
                ReplaceLastAiPlaceholder("[错误] " + err);
                RefreshChatDisplay();
            });
    }

    private void OnClearClicked()
    {
        lines.Clear();
        lines.Add("[系统] 已清空当前面板显示。");
        if (chatClient != null) chatClient.ClearHistory();
        RefreshChatDisplay();
    }

    private void OnVoiceToggleClicked()
    {
        enableVoice = !enableVoice;
        if (!enableVoice && ttsClient != null)
        {
            ttsClient.Stop();
        }
        UpdateVoiceButtonText();
    }

    private void UpdateVoiceButtonText()
    {
        if (voiceButtonText != null)
        {
            voiceButtonText.text = enableVoice ? "🔊 语音开" : "🔇 语音关";
        }
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

    private void RefreshChatDisplay()
    {
        if (chatHistoryText != null)
        {
            // 将所有行拼接并显示
            chatHistoryText.text = string.Join("\n", lines);
            
            // 延迟一帧强制滚动到最底部
            StartCoroutine(ScrollToBottom());
        }
    }

    private IEnumerator ScrollToBottom()
    {
        // 等待UI布局重建完成
        yield return null;
        if (scrollRect != null)
        {
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 0f;
        }
    }

    // ============ 运行时功能接口 ============
    
    public void ToggleWindow()
    {
        isOpen = !isOpen;
        if (chatPanel != null)
        {
            chatPanel.SetActive(isOpen);
        }
        
        if (!isOpen && ttsClient != null) ttsClient.Stop();
        
        UpdateCursorState();
    }

    public void CloseWindow()
    {
        if (!isOpen) return;
        isOpen = false;
        if (chatPanel != null) chatPanel.SetActive(false);
        if (ttsClient != null) ttsClient.Stop();
        UpdateCursorState();
    }

    public void SetWindowOpen(bool open)
    {
        if (isOpen == open) return;
        isOpen = open;
        if (chatPanel != null) chatPanel.SetActive(isOpen);
        if (!isOpen && ttsClient != null) ttsClient.Stop();
        UpdateCursorState();
    }

    public bool IsWindowOpen()
    {
        return isOpen;
    }

    private void UpdateCursorState()
    {
        if (!controlCursor) return;

        if (isOpen)
        {
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            if (playerController != null)
                playerController.isInspecting = true;
        }
        else
        {
            Cursor.visible = false;
            Cursor.lockState = lockModeWhenClosed;
            if (playerController != null)
                playerController.isInspecting = false;
        }
    }

    public void SetApiKey(string apiKey)
    {
        if (chatClient != null) chatClient.SetApiKey(apiKey);
    }

    public void SetPersona(string systemPrompt)
    {
        if (chatClient != null) chatClient.SetSystemPrompt(systemPrompt);
    }

    public void SetModel(string modelName)
    {
        if (chatClient != null) chatClient.SetModel(modelName);
    }

    // ============ 为了兼容以前的 AIChatUIController 的空方法 (避免报错) ============
    public void SetBackgroundColor(Color color) { /* 旧版OnGUI特有，Canvas已弃用 */ }
    public void SetContentBackgroundColor(Color color) { /* 旧版OnGUI特有，Canvas已弃用 */ }
    public void SetTextColor(Color color) { /* 旧版OnGUI特有，使用TMP替代 */ }
    public void SetFontSize(int size) { /* 旧版OnGUI特有，使用TMP替代 */ }
    public void SetButtonColors(Color normal, Color hover, Color active) { /* 旧版OnGUI特有，修改UI Button的Transition即可 */ }
    public void SetButtonTextColor(Color color) { /* 旧版OnGUI特有，使用TMP替代 */ }
    public void SetInputBackgroundColor(Color color) { /* 旧版OnGUI特有，修改InputField背景图的Color即可 */ }
    public void SetInputTextColor(Color color) { /* 旧版OnGUI特有，使用TMP替代 */ }
    public void SetWindowTitle(string title) { /* 窗口标题不需要了 */ }
    public void SetTitleTextColor(Color color) { /* 窗口标题不需要了 */ }
    public void SetWindowRect(float x, float y, float width, float height) { /* 旧版OnGUI特有，修改Canvas面板的RectTransform即可 */ }
    public Rect GetWindowRect() { return new Rect(0,0,0,0); }
    public void SetWindowPosition(float x, float y) { /* 旧版OnGUI特有 */ }
    public void SetWindowSize(float width, float height) { /* 旧版OnGUI特有 */ }
    public void SetBackgroundTexture(Texture2D texture) { /* 旧版OnGUI特有，使用UI Image替代 */ }
    public void ClearBackgroundTexture() { /* 旧版OnGUI特有 */ }

    public void SetVoiceEnabled(bool enabled)
    {
        enableVoice = enabled;
        if (!enabled && ttsClient != null) ttsClient.Stop();
        UpdateVoiceButtonText();
    }

    public bool IsVoiceEnabled()
    {
        return enableVoice;
    }

    public void SetVoice(AliyunTTSClient.VoiceType voiceType)
    {
        if (ttsClient != null) ttsClient.SetVoice(voiceType);
    }

    public void SetVoiceVolume(int volume)
    {
        if (ttsClient != null) ttsClient.SetVolume(volume);
    }

    public void SetVoiceSpeechRate(int rate)
    {
        if (ttsClient != null) ttsClient.SetSpeechRate(rate);
    }

    public void StopVoice()
    {
        if (ttsClient != null) ttsClient.Stop();
    }

    public void SpeakText(string text)
    {
        if (ttsClient != null && enableVoice) ttsClient.Speak(text);
    }
}