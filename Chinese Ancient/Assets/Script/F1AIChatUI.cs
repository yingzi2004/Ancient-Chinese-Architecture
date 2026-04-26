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
        StartCoroutine(DelayRestoreState());
    }
    private IEnumerator DelayRestoreState()
    {
        yield return null; 

        playerController = FindFirstObjectByType<PlayerController>();
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
        if (sendButton != null) sendButton.onClick.AddListener(OnSendClicked);
        if (clearButton != null) clearButton.onClick.AddListener(OnClearClicked);
        if (voiceButton != null) voiceButton.onClick.AddListener(OnVoiceToggleClicked);
        if (closeButton != null) closeButton.onClick.AddListener(CloseWindow);
    }
    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleWindow();
        }
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
        if (chatClient.IsRequesting) return;
        string message = inputField.text == null ? string.Empty : inputField.text.Trim();
        if (string.IsNullOrWhiteSpace(message)) return;
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
            finalReply = finalReply.Replace(match.Value, "").Trim();
            //处理天气相关指令
            if (cmd.StartsWith("Weather_"))
            {
                string weatherType = cmd.Substring(8);
                Debug.Log("AI触发切换天气：" + weatherType);
                if (UniStormSystem.Instance != null && UniStormSystem.Instance.AllWeatherTypes != null)
                {
                    // 按名称匹配天气类型，而不是按索引
                    foreach (WeatherType w in UniStormSystem.Instance.AllWeatherTypes)
                    {
                        if (w != null && !string.IsNullOrEmpty(w.WeatherTypeName))
                        {
                            if ((weatherType.Contains("Clear") || weatherType.Contains("晴")) && w.WeatherTypeName.Contains("Clear"))
                            {
                                UniStormSystem.Instance.ChangeWeather(w);
                                Debug.Log("成功切换到晴天：" + w.WeatherTypeName);
                                break;
                            }
                            else if ((weatherType.Contains("Rain") || weatherType.Contains("雨")) && w.WeatherTypeName.Contains("Rain"))
                            {
                                UniStormSystem.Instance.ChangeWeather(w);
                                Debug.Log("成功切换到雨天：" + w.WeatherTypeName);
                                break;
                            }
                            else if ((weatherType.Contains("Snow") || weatherType.Contains("雪")) && w.WeatherTypeName.Contains("Snow"))
                            {
                                UniStormSystem.Instance.ChangeWeather(w);
                                Debug.Log("成功切换到雪天：" + w.WeatherTypeName);
                                break;
                            }
                        }
                    }
                }
            }
            //处理传送相关指令
            else if (cmd.StartsWith("Teleport_"))
            {
                string location = cmd.Substring(9);
                Debug.Log("AI准备传送到：" + location);

                // 按照场馆名称对应关系映射到正确的场景名称
                string targetSceneName = "";
                if (location.Contains("土楼") || location.Contains("福建") || location.Contains("闽派场馆") || location.Contains("闽场馆"))
                {
                    targetSceneName = "Min Exhibition";
                }
                else if (location.Contains("苏州") || location.Contains("园林") || location.Contains("拙政园") || location.Contains("苏派场馆") || location.Contains("苏场馆"))
                {
                    targetSceneName = "Su Exhibition";
                }
                else if (location.Contains("晋商") || location.Contains("山西") || location.Contains("窑洞") || location.Contains("晋派场馆") || location.Contains("晋场馆"))
                {
                    targetSceneName = "晋Exhibition";
                }
                else if (location.Contains("天坛") || location.Contains("京派") || location.Contains("北京") || location.Contains("故宫") || location.Contains("京派场馆") || location.Contains("京场馆"))
                {
                    targetSceneName = "京 Exhibition";
                }
                else
                {
                    targetSceneName = "Scene_" + location;
                }

                Debug.Log("AI执行传送到场景：" + targetSceneName);

                PopupMapController mapController = FindFirstObjectByType<PopupMapController>();
                bool isUnlocked = true; 

                if (isUnlocked && !string.IsNullOrEmpty(targetSceneName))
                {
                    //跟AI说完话传送走，要保证游戏不在暂停状态或者死锁
                    Time.timeScale = 1f;
                    Cursor.visible = true;
                    Cursor.lockState = CursorLockMode.None;
                    //必须等待当前一帧(可能是UI或者网络事件帧)彻底执行完，
                    // 让旧的 EventSystem 收尾结束，然后再销毁旧场景
                    StartCoroutine(DeferredLoadScene(targetSceneName));
                }
            }
        }
        return finalReply;
    }
    private IEnumerator DeferredLoadScene(string targetScene)
    {
        yield return null; 
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
            chatHistoryText.text = string.Join("\n", lines);
            StartCoroutine(ScrollToBottom());
        }
    }
    private IEnumerator ScrollToBottom()
    {
        yield return new WaitForEndOfFrame();
        if (scrollRect != null && scrollRect.gameObject != null && scrollRect.gameObject.activeInHierarchy)
        {
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
    public void ToggleWindow()
    {
        isOpen = !isOpen;
        if (chatPanel != null)
        {
            chatPanel.SetActive(isOpen);
            if (isOpen)
            {
                ForceTopUIClickable();
            }
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
        if (chatPanel != null)
        {
            chatPanel.SetActive(isOpen);
            if (isOpen)
            {
                ForceTopUIClickable();
            }
        }
        if (!isOpen && ttsClient != null) ttsClient.Stop();
        UpdateCursorState();
    }
    private void ForceTopUIClickable()
    {
        if (chatPanel == null) return;
        chatPanel.transform.SetAsLastSibling();
        Canvas c = chatPanel.GetComponent<Canvas>();
        if (c == null)
            c = chatPanel.AddComponent<Canvas>();
        c.overrideSorting = true;
        c.sortingOrder = 30000;
        if (chatPanel.GetComponent<UnityEngine.UI.GraphicRaycaster>() == null)
            chatPanel.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        if (UnityEngine.EventSystems.EventSystem.current == null)
        {
            Debug.LogWarning("[AI UI] 检测到场景缺少 EventSystem！已自动为您创建以修复 UI 无法输入的问题。");
            GameObject eventSystemObj = new GameObject("EventSystem");
            eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
            eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
        }
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
    //为了兼容以前的AIChatUIController的空方法 
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
