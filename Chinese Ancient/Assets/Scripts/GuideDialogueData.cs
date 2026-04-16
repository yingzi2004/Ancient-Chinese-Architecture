using UnityEngine;

/// <summary>
/// 导游对话数据
/// 用于存储不同场景的导游对话内容
/// </summary>
[CreateAssetMenu(fileName = "GuideDialogueData", menuName = "Game/导游对话数据")]
public class GuideDialogueData : ScriptableObject
{
    [Header("场景信息")]
    public string sceneName = "Min Exhibition";

    [Header("导游信息")]
    public string guideName = "导游";
    public Sprite portraitSprite;
    [Multiline]
    public string[] dialogueSequence;

    [Header("触发设置")]
    [Tooltip("触发延迟时间（秒）")]
    public float triggerDelay = 1f;

    [Tooltip("是否只触发一次")]
    public bool triggerOnce = true;

    /// <summary>
    /// 示例：Min Exhibition场馆的导游对话
    /// </summary>
    [ContextMenu("生成Min Exhibition示例对话")]
    public void GenerateMinExhibitionExample()
    {
        sceneName = "Min Exhibition";
        guideName = "古建筑讲解员";

        dialogueSequence = new string[]
        {
            "欢迎来到微型古建筑展览馆！",
            "这里展示了中国古代建筑的精髓，从福建土楼到苏州园林，每一座建筑都承载着深厚的历史文化。",
            "我是这里的讲解员，将由我带领您参观这个精彩的展览。",
            "请自由参观，如果需要了解更多信息，随时可以向我提问。",
            "祝您参观愉快！"
        };

        Debug.Log($"已生成 {sceneName} 的示例导游对话");
    }

    /// <summary>
    /// 示例：福建土楼的导游对话
    /// </summary>
    [ContextMenu("生成土楼示例对话")]
    public void GenerateFujianExample()
    {
        sceneName = "Fujian Tulou";
        guideName = "客家文化讲解员";

        dialogueSequence = new string[]
        {
            "欢迎来到福建土楼展区！",
            "土楼是客家文化的象征，这种独特的建筑形式体现了客家人团结互助的精神。",
            "您面前的这座土楼模型，展示了福建土楼典型的圆形结构。",
            "土楼不仅坚固耐用，还具有防御功能，是客家人智慧的结晶。"
        };

        Debug.Log($"已生成 {sceneName} 的示例导游对话");
    }

    /// <summary>
    /// 示例：苏州园林的导游对话
    /// </summary>
    [ContextMenu("生成苏州园林示例对话")]
    public void GenerateSuzhouExample()
    {
        sceneName = "Suzhou Garden";
        guideName = "园林艺术讲解员";

        dialogueSequence = new string[]
        {
            "欢迎来到苏州园林展区！",
            "苏州园林是中国古典园林的代表，以其精巧的布局和深邃的意境闻名于世。",
            "这里的每一处景致都经过精心设计，体现了'虽由人作，宛自天开'的艺术境界。",
            "请慢慢欣赏，感受东方园林艺术的魅力。"
        };

        Debug.Log($"已生成 {sceneName} 的示例导游对话");
    }
}
