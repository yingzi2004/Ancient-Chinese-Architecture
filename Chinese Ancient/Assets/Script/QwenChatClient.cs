using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
public interface IQwenChatService
{
    bool IsRequesting { get; }
    void SetApiKey(string apiKey);
    void SetModel(string modelName);
    void SetSystemPrompt(string prompt);
    void ClearHistory();
    void SendUserMessage(string userMessage, Action<string> onSuccess, Action<string> onError = null);
}
public class QwenChatClient : MonoBehaviour, IQwenChatService
{
    [Header("配置资产")]
    [SerializeField] private AIChatSettings settings;
    [Header("无配置资产时直接填写")]
    [SerializeField] private string endpointDirect = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
    [SerializeField] private string apiKeyDirect = "";
    [SerializeField] private string modelDirect = "qwen-plus";
    [TextArea(2, 8)]
    [SerializeField] private string systemPromptDirect = "你是一个友好的古建筑导览助手，回答简洁、准确，优先结合中国古建筑背景。";
    [SerializeField] private float temperatureDirect = 0.7f;
    [SerializeField] private int maxTokensDirect = 512;
    [SerializeField] private int requestTimeoutSecondsDirect = 60;
    [Header("运行时覆盖（可选）")]
    [SerializeField] private string endpointOverride = "";
    [SerializeField] private string modelOverride = "";
    [TextArea(2, 8)]
    [SerializeField] private string systemPromptOverride = "";
    public bool IsRequesting { get; private set; }
    private readonly List<ChatMessage> history = new List<ChatMessage>();
    private string runtimeApiKey = "";
    [Serializable]
    private class ChatMessage
    {
        public string role;
        public string content;
        public ChatMessage(string role, string content)
        {
            this.role = role;
            this.content = content;
        }
    }
    [Serializable]
    private class ChatCompletionRequest
    {
        public string model;
        public List<ChatMessage> messages;
        public float temperature;
        public int max_tokens;
        public bool stream;
    }
    [Serializable]
    private class ChatCompletionResponse
    {
        public Choice[] choices;
    }
    [Serializable]
    private class Choice
    {
        public ChatMessage message;
    }
    [Serializable]
    private class ErrorResponse
    {
        public ErrorBody error;
    }
    [Serializable]
    private class ErrorBody
    {
        public string message;
        public string code;
    }
    public void SetApiKey(string apiKey)
    {
        runtimeApiKey = apiKey == null ? string.Empty : apiKey.Trim();
    }
    public void SetModel(string modelName)
    {
        modelOverride = modelName == null ? string.Empty : modelName.Trim();
    }
    public void SetSystemPrompt(string prompt)
    {
        systemPromptOverride = prompt == null ? string.Empty : prompt.Trim();
    }
    public void ClearHistory()
    {
        history.Clear();
    }
    public void SendUserMessage(string userMessage, Action<string> onSuccess, Action<string> onError = null)
    {
        if (IsRequesting)
        {
            onError?.Invoke("请求进行中，请稍候。");
            return;
        }
        if (string.IsNullOrWhiteSpace(userMessage))
        {
            onError?.Invoke("输入不能为空。");
            return;
        }
        string endpoint = GetEndpoint();
        string apiKey = GetApiKey();
        string model = GetModel();
        if (string.IsNullOrWhiteSpace(endpoint))
        {
            onError?.Invoke("未配置接口地址。请在 AIChatSettings 或运行时设置 endpoint。 ");
            return;
        }
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            onError?.Invoke("未配置 API Key。请在 AIChatSettings、运行时接口或环境变量 AI_QWEN_API_KEY 中设置。");
            return;
        }
        if (string.IsNullOrWhiteSpace(model))
        {
            onError?.Invoke("未配置模型名。请设置如 qwen-plus。");
            return;
        }
        StartCoroutine(SendRequestRoutine(endpoint, apiKey, model, userMessage, onSuccess, onError));
    }
    private class BypassCertificate : CertificateHandler
    {
        protected override bool ValidateCertificate(byte[] certificateData)
        {
            return true;
        }
    }
    private IEnumerator SendRequestRoutine(string endpoint, string apiKey, string model, string userMessage, Action<string> onSuccess, Action<string> onError)
    {
        IsRequesting = true;
        // ... build json ...
        var messages = BuildMessages(userMessage);
        // Simple JSON construction to avoid complex serialization issues
        string messagesJson = "[";
        for (int i = 0; i < messages.Count; i++)
        {
            messagesJson += $"{{\"role\":\"{messages[i].role}\",\"content\":\"{EscapeJson(messages[i].content)}\"}}";
            if (i < messages.Count - 1) messagesJson += ",";
        }
        messagesJson += "]";
        string requestJson = $"{{\"model\":\"{model}\",\"messages\":{messagesJson},\"temperature\":{GetTemperature()},\"max_tokens\":{GetMaxTokens()},\"stream\":false}}";
        byte[] rawBody = Encoding.UTF8.GetBytes(requestJson);
        using (UnityWebRequest request = new UnityWebRequest(endpoint, UnityWebRequest.kHttpVerbPOST))
        {
            request.certificateHandler = new BypassCertificate();
            request.uploadHandler = new UploadHandlerRaw(rawBody);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");
            request.SetRequestHeader("Authorization", "Bearer " + apiKey);
            request.timeout = GetTimeout();
            yield return request.SendWebRequest();
            bool success = request.result == UnityWebRequest.Result.Success;
            string responseText = request.downloadHandler != null ? request.downloadHandler.text : string.Empty;
            if (!success)
            {
                IsRequesting = false;
                onError?.Invoke(FormatRequestError(request, responseText));
                yield break;
            }
            ChatCompletionResponse response = null;
            try
            {
                response = JsonUtility.FromJson<ChatCompletionResponse>(responseText);
            }
            catch (Exception ex)
            {
                IsRequesting = false;
                onError?.Invoke("响应解析失败：" + ex.Message);
                yield break;
            }
            string answer = ExtractAssistantReply(response);
            if (string.IsNullOrWhiteSpace(answer))
            {
                IsRequesting = false;
                onError?.Invoke("AI 未返回有效内容。原始响应：" + responseText);
                yield break;
            }
            history.Add(new ChatMessage("user", userMessage));
            history.Add(new ChatMessage("assistant", answer));
            IsRequesting = false;
            onSuccess?.Invoke(answer);
        }
    }
    private List<ChatMessage> BuildMessages(string userMessage)
    {
        List<ChatMessage> messages = new List<ChatMessage>();
        string systemPrompt = GetSystemPrompt();
        if (!string.IsNullOrWhiteSpace(systemPrompt))
        {
            messages.Add(new ChatMessage("system", systemPrompt));
        }
        for (int i = 0; i < history.Count; i++)
        {
            messages.Add(history[i]);
        }
        messages.Add(new ChatMessage("user", userMessage));
        return messages;
    }
    private string ExtractAssistantReply(ChatCompletionResponse response)
    {
        if (response == null || response.choices == null || response.choices.Length == 0)
            return string.Empty;
        Choice first = response.choices[0];
        if (first == null || first.message == null)
            return string.Empty;
        return first.message.content == null ? string.Empty : first.message.content.Trim();
    }
    private string FormatRequestError(UnityWebRequest request, string responseText)
    {
        if (!string.IsNullOrWhiteSpace(responseText))
        {
            try
            {
                ErrorResponse errorObj = JsonUtility.FromJson<ErrorResponse>(responseText);
                if (errorObj != null && errorObj.error != null && !string.IsNullOrWhiteSpace(errorObj.error.message))
                {
                    return "请求失败(" + (int)request.responseCode + ")：" + errorObj.error.message;
                }
            }
            catch
            {
            }
        }
        return "请求失败(" + (int)request.responseCode + ")：" + request.error;
    }
    private string GetEndpoint()
    {
        if (!string.IsNullOrWhiteSpace(endpointOverride)) return endpointOverride.Trim();
        if (settings != null && !string.IsNullOrWhiteSpace(settings.endpoint)) return settings.endpoint.Trim();
        if (!string.IsNullOrWhiteSpace(endpointDirect)) return endpointDirect.Trim();
        return string.Empty;
    }
    private string GetApiKey()
    {
        if (!string.IsNullOrWhiteSpace(runtimeApiKey)) return runtimeApiKey.Trim();
        if (settings != null && !string.IsNullOrWhiteSpace(settings.apiKey)) return settings.apiKey.Trim();
        if (!string.IsNullOrWhiteSpace(apiKeyDirect)) return apiKeyDirect.Trim();
        string envValue = Environment.GetEnvironmentVariable("AI_QWEN_API_KEY");
        return string.IsNullOrWhiteSpace(envValue) ? string.Empty : envValue.Trim();
    }
    private string GetModel()
    {
        if (!string.IsNullOrWhiteSpace(modelOverride)) return modelOverride.Trim();
        if (settings != null && !string.IsNullOrWhiteSpace(settings.model)) return settings.model.Trim();
        if (!string.IsNullOrWhiteSpace(modelDirect)) return modelDirect.Trim();
        return string.Empty;
    }
    private string GetSystemPrompt()
    {
        if (!string.IsNullOrWhiteSpace(systemPromptOverride)) return systemPromptOverride.Trim();
        if (settings != null && !string.IsNullOrWhiteSpace(settings.systemPrompt)) return settings.systemPrompt.Trim();
        if (!string.IsNullOrWhiteSpace(systemPromptDirect)) return systemPromptDirect.Trim();
        return string.Empty;
    }
    private float GetTemperature()
    {
        if (settings == null) return Mathf.Clamp(temperatureDirect, 0f, 2f);
        return settings.temperature;
    }
    private int GetMaxTokens()
    {
        if (settings == null) return Mathf.Max(1, maxTokensDirect);
        return Mathf.Max(1, settings.maxTokens);
    }
    private int GetTimeout()
    {
        if (settings == null) return Mathf.Max(1, requestTimeoutSecondsDirect);
        return Mathf.Max(1, settings.requestTimeoutSeconds);
    }
    private string EscapeJson(string s)
    {
        if (s == null) return "";
        return s.Replace("\\", "\\\\").Replace("\"", "\\\"").Replace("\n", "\\n").Replace("\r", "\\r").Replace("\t", "\\t");
    }}
