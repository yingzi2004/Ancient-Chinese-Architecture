using UnityEngine;
[CreateAssetMenu(fileName = "AIChatSettings", menuName = "AI/Qwen Chat Settings")]
public class AIChatSettings : ScriptableObject
{
    [Header("接口设置")]
    public string endpoint = "https://dashscope.aliyuncs.com/compatible-mode/v1/chat/completions";
    public string apiKey = "";
    public string model = "qwen-plus";
    [Header("AI 人设")]
    [TextArea(3, 10)]
    public string systemPrompt = "你是一个友好的古建筑导览助手，回答简洁、准确，优先结合中国古建筑背景。";
    [Header("生成参数")]
    [Range(0f, 2f)]
    public float temperature = 0.7f;
    [Min(1)]
    public int maxTokens = 512;
    [Min(1)]
    public int requestTimeoutSeconds = 60;
}
