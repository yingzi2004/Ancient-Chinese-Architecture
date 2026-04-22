using UnityEngine;
public class AIVoiceAssistant : MonoBehaviour
{
    [Header("组件引用")]
    [SerializeField] private QwenChatClient chatClient;
    [SerializeField] private AliyunTTSClient ttsClient;
    [SerializeField] private F1AIChatUI chatUI;
    [Header("语音设置")]
    [SerializeField] private bool enableVoice = true;
    [SerializeField] private bool autoSpeak = true;
    [Header("快捷键")]
    [SerializeField] private KeyCode toggleVoiceKey = KeyCode.F2;
    [SerializeField] private KeyCode stopSpeakKey = KeyCode.F3;
    public bool EnableVoice
    {
        get => enableVoice;
        set => enableVoice = value;
    }
    public bool AutoSpeak
    {
        get => autoSpeak;
        set => autoSpeak = value;
    }
    private void Awake()
    {
        // 自动查找组件
        if (chatClient == null)
            chatClient = FindFirstObjectByType<QwenChatClient>();
        if (ttsClient == null)
            ttsClient = FindFirstObjectByType<AliyunTTSClient>();
        if (chatUI == null)
            chatUI = FindFirstObjectByType<F1AIChatUI>();
    }
    private void Update()
    {
        // 切换语音开关
        if (Input.GetKeyDown(toggleVoiceKey))
        {
            enableVoice = !enableVoice;
            Debug.Log($"语音朗读已{(enableVoice ? "开启" : "关闭")}");
        }
        // 停止朗读
        if (Input.GetKeyDown(stopSpeakKey))
        {
            StopSpeaking();
        }
    }
    public void SendMessageWithVoice(string message)
    {
        if (chatClient == null)
        {
            Debug.LogError("未找到 QwenChatClient");
            return;
        }
        chatClient.SendUserMessage(
            message,
            onSuccess: reply =>
            {
                // 如果启用了语音且自动朗读
                if (enableVoice && autoSpeak)
                {
                    SpeakText(reply);
                }
            },
            onError: err =>
            {
                Debug.LogError($"AI请求失败: {err}");
            }
        );
    }
    public void SpeakText(string text)
    {
        if (!enableVoice)
        {
            Debug.Log("语音已关闭");
            return;
        }
        if (ttsClient == null)
        {
            Debug.LogError("未找到 AliyunTTSClient");
            return;
        }
        // 清理文本（移除特殊标记等）
        string cleanText = CleanTextForSpeech(text);
        if (string.IsNullOrWhiteSpace(cleanText))
            return;
        ttsClient.Speak(
            cleanText,
            onComplete: () =>
            {
                Debug.Log("朗读完成");
            },
            onError: err =>
            {
                Debug.LogError($"语音合成失败: {err}");
            }
        );
    }
    public void StopSpeaking()
    {
        if (ttsClient != null)
        {
            ttsClient.Stop();
        }
    }
    public void SetVoice(AliyunTTSClient.VoiceType voiceType)
    {
        if (ttsClient != null)
        {
            ttsClient.SetVoice(voiceType);
        }
    }
    public void SetVolume(int volume)
    {
        if (ttsClient != null)
        {
            ttsClient.SetVolume(volume);
        }
    }
    public void SetSpeechRate(int rate)
    {
        if (ttsClient != null)
        {
            ttsClient.SetSpeechRate(rate);
        }
    }
    public void SetPitchRate(int pitch)
    {
        if (ttsClient != null)
        {
            ttsClient.SetPitchRate(pitch);
        }
    }
    private string CleanTextForSpeech(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
            return string.Empty;
        // 移除常见的标记前缀
        text = text.Replace("[AI]", "").Replace("[我]", "").Replace("[系统]", "").Replace("[错误]", "");
        // 移除多余空白
        text = text.Trim();
        // 限制长度（避免过长的文本）
        if (text.Length > 500)
        {
            text = text.Substring(0, 500) + "...";
        }
        return text;
    }
}
