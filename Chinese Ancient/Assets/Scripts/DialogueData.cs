using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 对话选项数据类
/// </summary>
[Serializable]
public class DialogueOption
{
    public string optionText; // 选项显示的文本
    public string responseText; // 选择该选项后NPC的回复
}

/// <summary>
/// 对话数据类
/// </summary>
[Serializable]
public class DialogueData
{
    public string npcName; // NPC名称
    public string greetingText; // 第一句欢迎语
    public List<DialogueOption> dialogueOptions; // 对话选项列表

    public DialogueData()
    {
        dialogueOptions = new List<DialogueOption>();
    }
}
