#if UNITY_EDITOR
using UnityEngine;
using UnityEngine.UI;
using UnityEditor;

/// <summary>
/// 编辑器脚本：一键创建对话UI系统
/// 在Unity编辑器菜单中执行：Tools → Create Dialogue UI
/// </summary>
public class CreateDialogueUIEditor
{
    [MenuItem("Tools/Create Dialogue UI %#&d")] // Ctrl+Alt+Shift+D
    public static void CreateDialogueUI()
    {
        Debug.Log("开始创建对话UI系统...");

        // 1. 创建或查找Canvas
        Canvas canvas = Object.FindFirstObjectByType<Canvas>();
        if (canvas == null)
        {
            GameObject canvasObj = new GameObject("Canvas");
            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();
            Debug.Log("✓ 创建Canvas");
        }
        else
        {
            Debug.Log("✓ 找到现有Canvas");
        }

        // 2. 创建DialoguePanel
        GameObject panelObj = new GameObject("DialoguePanel");
        panelObj.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = panelObj.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.2f, 0.1f);
        panelRect.anchorMax = new Vector2(0.8f, 0.3f);
        panelRect.sizeDelta = Vector2.zero;

        Image panelImage = panelObj.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
        Debug.Log("✓ 创建DialoguePanel");

        // 3. 创建NPCName
        GameObject nameObj = CreateTextElement(panelObj.transform, "NPCName", "导游",
            new Vector2(0, 1), new Vector2(-230, -80), new Vector2(200, 30), 24);
        Debug.Log("✓ 创建NPCName");

        // 4. 创建DialogueText
        GameObject dialogueObj = CreateTextElement(panelObj.transform, "DialogueText", "对话内容将在这里显示...",
            new Vector2(0.5f, 0.5f), new Vector2(0, 0), new Vector2(550, 120), 18);
        Text dialogueText = dialogueObj.GetComponent<Text>();
        dialogueText.alignment = TextAnchor.UpperLeft;
        Debug.Log("✓ 创建DialogueText");

        // 5. 创建ContinuePrompt
        GameObject continueObj = CreateTextElement(panelObj.transform, "ContinuePrompt", "按 L 键继续...",
            new Vector2(1, 0), new Vector2(-20, 10), new Vector2(300, 30), 16);
        Text continueText = continueObj.GetComponent<Text>();
        continueText.alignment = TextAnchor.MiddleRight;
        continueText.color = new Color(0.7f, 0.7f, 0.7f);
        continueObj.SetActive(false);
        Debug.Log("✓ 创建ContinuePrompt");

        // 6. 创建OptionsContainer
        GameObject optionsObj = new GameObject("OptionsContainer");
        optionsObj.transform.SetParent(panelObj.transform, false);
        RectTransform optionsRect = optionsObj.AddComponent<RectTransform>();
        optionsRect.anchorMin = new Vector2(0, 0);
        optionsRect.anchorMax = new Vector2(1, 0);
        optionsRect.anchoredPosition = new Vector2(0, 10);
        optionsRect.sizeDelta = new Vector2(-40, 100);
        Debug.Log("✓ 创建OptionsContainer");

        // 7. 创建DialogueManager
        GameObject dmObj = new GameObject("DialogueManager");
        DialogueManager dm = dmObj.AddComponent<DialogueManager>();

        // 设置引用
        dm.dialoguePanel = panelObj;
        dm.npcNameText = nameObj.GetComponent<Text>();
        dm.dialogueText = dialogueText;
        dm.continuePromptText = continueText;
        dm.optionsContainer = optionsObj.transform;

        // 尝试查找OptionButton预制体
        dm.optionButtonPrefab = Resources.Load<GameObject>("Prefabs/OptionButton");

        Debug.Log("✓ 创建DialogueManager并连接所有引用");

        // 8. 创建示例触发器
        GameObject triggerObj = new GameObject("示例触发器_入口");
        triggerObj.transform.position = new Vector3(0, 1, 0);

        BoxCollider col = triggerObj.AddComponent<BoxCollider>();
        col.isTrigger = true;
        col.size = new Vector3(5, 3, 5);

        LocationDialogueTrigger_Auto trigger = triggerObj.AddComponent<LocationDialogueTrigger_Auto>();
        Debug.Log("✓ 创建示例触发器");

        Debug.Log("<color=green>✓✓✓ 对话UI系统创建完成！</color>");
        Debug.Log("现在可以运行游戏测试了。将触发器移动到玩家经过的位置即可。");

        // 选中新创建的对象
        Selection.activeGameObject = panelObj;
    }

    private static GameObject CreateTextElement(Transform parent, string name, string text,
        Vector2 anchor, Vector2 position, Vector2 size, int fontSize)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = position;
        rect.sizeDelta = size;

        Text textComponent = obj.AddComponent<Text>();
        textComponent.text = text;
        textComponent.fontSize = fontSize;
        textComponent.color = Color.white;
        textComponent.alignment = TextAnchor.MiddleLeft;
        textComponent.resizeTextForBestFit = true;
        textComponent.resizeTextMinSize = 10;
        textComponent.resizeTextMaxSize = fontSize;

        return obj;
    }
}
#endif
