using UnityEngine;

[CreateAssetMenu(fileName = "AIChatSettings", menuName = "AI/Qwen Chat Settings")]
public class AIChatSettings : ScriptableObject
{
    [Header("接口设置")]
    [Tooltip("千问兼容接口地址，默认使用百炼兼容 Chat Completions 接口")]
    public string endpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";

    [Tooltip("API Key。建议留空并从环境变量 AI_QWEN_API_KEY 读取")]
    public string apiKey = "";

    [Tooltip("模型名，例如 qwen-plus / qwen-max / qwen-turbo")]
    public string model = "qwen-plus";

    [Header("AI 人设")]
    [TextArea(3, 10)]
    [Tooltip("System Prompt，用于定义 AI 角色与回答风格")]
    public string systemPrompt = "你是一个友好的古建筑导览助手，回答简洁、准确，优先结合中国古建筑背景。";

    [Header("生成参数")]
    [Range(0f, 2f)]
    public float temperature = 0.7f;

    [Min(1)]
    public int maxTokens = 512;

    [Min(1)]
    [Tooltip("HTTP 超时时间（秒）")]
    public int requestTimeoutSeconds = 60;
}