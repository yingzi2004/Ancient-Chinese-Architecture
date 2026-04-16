using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

/// <summary>
/// 简化的对话自动设置脚本
/// 独立运行，自动创建所需的UI和对话系统
/// 使用方法：将此脚本添加到场景中的任何GameObject上
/// </summary>
public class SimpleDialogueSetup : MonoBehaviour
{
    [Header("导游信息")]
    [SerializeField] private string guideName = "民俗展览讲解员";

    [Header("对话内容")]
    [TextArea(3, 10)]
    [SerializeField] private string[] dialogueLines = new string[]
    {
        "欢迎来到民俗文化展览馆！",
        "这里展示了中国丰富多彩的民俗文化。",
        "您可以自由参观各个展区。",
        "按任意键或点击继续按钮推进对话。"
    };

    [Header("设置")]
    [SerializeField] private float startDelay = 0.5f;
    [SerializeField] private float typingSpeed = 0.05f;

    // UI组件
    private GameObject canvas;
    private GameObject dialoguePanel;
    private TextMeshProUGUI nameText;
    private TextMeshProUGUI contentText;
    private TextMeshProUGUI continueText;
    private Button continueButton;

    private Coroutine typingCoroutine;
    private int currentLine = 0;
    private bool isTyping = false;

    void Start()
    {
        Debug.Log("[SimpleDialogueSetup] 开始设置对话系统...");
        StartCoroutine(SetupAndStart());
    }

    IEnumerator SetupAndStart()
    {
        // 等待一帧，确保场景加载完成
        yield return null;

        // 创建UI
        CreateUI();

        // 等待延迟
        yield return new WaitForSeconds(startDelay);

        // 开始对话
        StartDialogue();
    }

    void CreateUI()
    {
        Debug.Log("[SimpleDialogueSetup] 创建UI系统...");

        // 查找或创建Canvas
        Canvas existingCanvas = FindObjectOfType<Canvas>();
        if (existingCanvas == null)
        {
            Debug.Log("[SimpleDialogueSetup] 创建新Canvas");
            GameObject canvasObj = new GameObject("DialogueCanvas");
            canvas = canvasObj;
            Canvas canvasComponent = canvasObj.AddComponent<Canvas>();
            canvasComponent.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 添加EventSystem
            if (FindObjectOfType<UnityEngine.EventSystems.EventSystem>() == null)
            {
                GameObject eventSystemObj = new GameObject("EventSystem");
                eventSystemObj.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystemObj.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }
        }
        else
        {
            Debug.Log("[SimpleDialogueSetup] 使用现有Canvas");
            canvas = existingCanvas.gameObject;
        }

        // 创建对话面板
        dialoguePanel = new GameObject("DialoguePanel");
        dialoguePanel.transform.SetParent(canvas.transform, false);

        RectTransform panelRect = dialoguePanel.AddComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0);
        panelRect.anchorMax = new Vector2(0.5f, 0);
        panelRect.pivot = new Vector2(0.5f, 0);
        panelRect.anchoredPosition = new Vector2(0, 100);
        panelRect.sizeDelta = new Vector2(800, 200);

        Image panelImage = dialoguePanel.AddComponent<Image>();
        panelImage.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);

        // 创建名称文本
        GameObject nameObj = CreateTextObject(dialoguePanel.transform, "NameText",
            new Vector2(0, 1), new Vector2(20, -15), new Vector2(300, 40));
        nameText = nameObj.GetComponent<TextMeshProUGUI>();
        nameText.text = guideName;
        nameText.fontSize = 28;
        nameText.color = Color.white;
        nameText.fontStyle = FontStyles.Bold;

        // 创建对话内容文本
        GameObject contentObj = CreateTextObject(dialoguePanel.transform, "ContentText",
            new Vector2(0.5f, 0.5f), new Vector2(0, 20), new Vector2(750, 120));
        contentText = contentObj.GetComponent<TextMeshProUGUI>();
        contentText.text = "";
        contentText.fontSize = 22;
        contentText.color = new Color(0.95f, 0.95f, 0.95f);
        contentText.alignment = TextAlignmentOptions.TopLeft;

        // 创建继续提示
        GameObject continueObj = CreateTextObject(dialoguePanel.transform, "ContinueText",
            new Vector2(1, 0), new Vector2(-20, 15), new Vector2(200, 30));
        continueText = continueObj.GetComponent<TextMeshProUGUI>();
        continueText.text = "按任意键继续...";
        continueText.fontSize = 16;
        continueText.color = new Color(0.6f, 0.6f, 0.6f);
        continueText.alignment = TextAlignmentOptions.Right;
        continueText.gameObject.SetActive(false);

        // 创建继续按钮
        GameObject buttonObj = CreateButtonObject(dialoguePanel.transform, "ContinueButton",
            new Vector2(0.5f, 1), new Vector2(300, -10), new Vector2(140, 35));
        continueButton = buttonObj.GetComponent<Button>();
        continueButton.onClick.AddListener(OnContinue);

        Debug.Log("[SimpleDialogueSetup] UI创建完成");
    }

    GameObject CreateTextObject(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        TextMeshProUGUI text = obj.AddComponent<TextMeshProUGUI>();
        return obj;
    }

    GameObject CreateButtonObject(Transform parent, string name, Vector2 anchor, Vector2 pos, Vector2 size)
    {
        GameObject obj = new GameObject(name);
        obj.transform.SetParent(parent, false);

        RectTransform rect = obj.AddComponent<RectTransform>();
        rect.anchorMin = anchor;
        rect.anchorMax = anchor;
        rect.pivot = anchor;
        rect.anchoredPosition = pos;
        rect.sizeDelta = size;

        Image image = obj.AddComponent<Image>();
        image.color = new Color(0.3f, 0.6f, 0.9f);

        Button button = obj.AddComponent<Button>();

        // 创建按钮文字
        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(obj.transform, false);

        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        TextMeshProUGUI text = textObj.AddComponent<TextMeshProUGUI>();
        text.text = "继续";
        text.fontSize = 18;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.Center;

        return obj;
    }

    void StartDialogue()
    {
        Debug.Log("[SimpleDialogueSetup] 开始对话");
        dialoguePanel.SetActive(true);
        currentLine = 0;
        ShowLine();
    }

    void ShowLine()
    {
        if (currentLine >= dialogueLines.Length)
        {
            EndDialogue();
            return;
        }

        Debug.Log($"[SimpleDialogueSetup] 显示第 {currentLine + 1}/{dialogueLines.Length} 行");

        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        typingCoroutine = StartCoroutine(TypeText(dialogueLines[currentLine]));
    }

    IEnumerator TypeText(string text)
    {
        isTyping = true;
        contentText.text = "";
        continueText.gameObject.SetActive(false);

        foreach (char c in text)
        {
            contentText.text += c;
            yield return new WaitForSeconds(typingSpeed);
        }

        isTyping = false;
        continueText.gameObject.SetActive(true);
    }

    void OnContinue()
    {
        if (isTyping)
        {
            // 立即完成打字
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
            }
            contentText.text = dialogueLines[currentLine];
            isTyping = false;
            continueText.gameObject.SetActive(true);
        }
        else
        {
            // 下一行
            currentLine++;
            ShowLine();
        }
    }

    void Update()
    {
        // 任意键继续（除了鼠标）
        if (Input.anyKeyDown && !Input.GetMouseButton(0) && !Input.GetMouseButton(1))
        {
            OnContinue();
        }
    }

    void EndDialogue()
    {
        Debug.Log("[SimpleDialogueSetup] 对话结束");
        if (dialoguePanel != null)
        {
            dialoguePanel.SetActive(false);
        }
        this.enabled = false;
    }

    /// <summary>
    /// 测试对话
    /// </summary>
    [ContextMenu("测试对话")]
    public void TestDialogue()
    {
        StartDialogue();
    }
}
