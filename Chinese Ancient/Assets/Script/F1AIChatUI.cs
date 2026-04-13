using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using UniStorm;

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

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 还是换回实用至上的“延迟一帧”大法
        // 因为必须等新场景里的 PlayerController 走完它的 Start() 锁死鼠标后，我们再出手把它抢回来！
        StartCoroutine(DelayRestoreState());
    }

    private IEnumerator DelayRestoreState()
    {
        yield return null; // 延迟一帧，这是制胜关键
        
        // 找回玩家
        playerController = FindFirstObjectByType<PlayerController>();
        
        // 恢复鼠标控制权
        if (isOpen)
        {
            UpdateCursorState();
            if (inputField != null) 
            {
                inputField.text = string.Empty;
                inputField.ActivateInputField();
            }
        }
    }

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
        lines.Add("[小微] ...");
        RefreshChatDisplay();

        chatClient.SendUserMessage(
            message,
            onSuccess: reply =>
            {
                // 拦截并处理隐藏指令
                reply = ProcessAICommands(reply);

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

    private string ProcessAICommands(string rawReply)
    {
        if (string.IsNullOrEmpty(rawReply)) return rawReply;
        
        string finalReply = rawReply;
        string pattern = @"\[CMD:([^\]]+)\]";
        MatchCollection matches = Regex.Matches(rawReply, pattern);

        foreach (Match match in matches)
        {
            string cmd = match.Groups[1].Value.Trim();
            // 把这个暗号从对话文本里删掉，免得让玩家看到
            finalReply = finalReply.Replace(match.Value, "").Trim();

            // 1. 处理天气相关指令
            if (cmd.StartsWith("Weather_"))
            {
                string weatherType = cmd.Substring(8);
                Debug.Log("AI触发切换天气：" + weatherType);
                
                if (UniStormSystem.Instance != null && UniStormSystem.Instance.AllWeatherTypes != null)
                {
                    int weatherIndex = -1;
                    if (weatherType.Contains("Clear") || weatherType.Contains("晴")) weatherIndex = 0; // 假设0是晴
                    else if (weatherType.Contains("Rain") || weatherType.Contains("雨")) weatherIndex = 1; // 假设1是雨
                    else if (weatherType.Contains("Snow") || weatherType.Contains("雪")) weatherIndex = 2; // 假设2是雪
                    
                    if (weatherIndex >= 0 && weatherIndex < UniStormSystem.Instance.AllWeatherTypes.Count)
                    {
                        UniStormSystem.Instance.ChangeWeather(UniStormSystem.Instance.AllWeatherTypes[weatherIndex]);
                    }
                }
            }
            // 2. 处理传送相关指令
            else if (cmd.StartsWith("Teleport_"))
            {
                string location = cmd.Substring(9);
                Debug.Log("AI准备传送到：" + location);
                
                bool isUnlocked = false;
                int buildingIndex = -1;

                // 根据地名判断应该是哪个场馆 (0=土楼, 1=苏州, 2=京派, 3=晋商)
                if (location.Contains("土楼") || location.Contains("福建")) buildingIndex = 0;
                else if (location.Contains("苏州") || location.Contains("园林")) buildingIndex = 1;
                else if (location.Contains("京派") || location.Contains("北京") || location.Contains("天坛") || location.Contains("故宫")) buildingIndex = 2;
                else if (location.Contains("晋商") || location.Contains("山西") || location.Contains("窑洞")) buildingIndex = 3;

                string targetSceneName = "";
                PopupMapController mapController = FindFirstObjectByType<PopupMapController>();
                
                if (mapController != null && buildingIndex >= 0 && buildingIndex < mapController.unlockedArray.Length)
                {
                    isUnlocked = mapController.unlockedArray[buildingIndex];
                    if (buildingIndex < mapController.buildings.Count)
                    {
                        targetSceneName = mapController.buildings[buildingIndex].targetSceneName;
                    }
                }
                else
                {
                    // 如果地图控制器没找到，默认算已解锁（或者根据你的游戏逻辑默认不给传）
                    isUnlocked = true; 
                    targetSceneName = "Scene_" + location; // 降级处理
                }

                if (isUnlocked)
                {
                    Debug.Log("AI执行传送到场景：" + targetSceneName);
                    if (!string.IsNullOrEmpty(targetSceneName))
                    {
                        // 【传送修复】跟AI说完话传送走，要保证游戏不在暂停状态或者死锁
                        Time.timeScale = 1f;
                        Cursor.visible = true;
                        Cursor.lockState = CursorLockMode.None;

                        // 【致命 Bug 修复】：必须等待当前一帧(可能是UI或者网络事件帧)彻底执行完，
                        // 让旧的 EventSystem 收尾结束，然后再销毁旧场景！
                        // 不然会导致新场景的 UI 事件被永久卡死而“点不动”。
                        StartCoroutine(DeferredLoadScene(targetSceneName));
                    }
                }
                else
                {
                    // 如果没解锁，我们直接篡改 AI 的回答
                    finalReply = $"哎呀，[{location}] 还没有解锁哦！需要先通关前面的场景才能去那里呢~";
                }
            }
        }
        return finalReply;
    }

    private IEnumerator DeferredLoadScene(string targetScene)
    {
        yield return null; // 核心：等这帧里所有后续代码跟UI输入流安全收发完毕！
        SceneManager.LoadScene(targetScene);
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
            if (lines[i] == "[小微] ...")
            {
                lines[i] = "[小微] " + content;
                return;
            }
        }
        lines.Add("[小微] " + content);
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
        // 关键修复：TextMeshPro在跨场景或频繁更新时，有时自身的重绘没跟上Canvas的生命周期
        // 等待UI布局完全重建，必须通过等待帧的末尾，而不是随便一帧
        yield return new WaitForEndOfFrame();
        
        if (scrollRect != null && scrollRect.gameObject != null && scrollRect.gameObject.activeInHierarchy)
        {
            // 在强制更新前稍作等待防御，并捕获因TMP内部资源被意外销毁产生的报错
            try
            {
                Canvas.ForceUpdateCanvases();
                scrollRect.verticalNormalizedPosition = 0f;
            }
            catch (System.Exception e)
            {
                Debug.LogWarning("[聊天UI] 拦截到一个因为切场景引起的Canvas/TMP底层刷新冲突，已安全跳过报错: " + e.Message);
            }
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