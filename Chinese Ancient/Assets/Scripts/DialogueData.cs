using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DialogueOption
{
    public string optionText; // 选项显示的文本
    public string responseText; // 选择该选项后NPC的回复

    [Header("后续选项（可选）")]
    public DialogueOption[] followUpOptions;

    public DialogueOption()
    {
        followUpOptions = null;
    }
}

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
