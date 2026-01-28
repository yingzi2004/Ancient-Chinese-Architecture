using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class TypewriterSimple : MonoBehaviour
{
    [Header("--- UI 组件 ---")]
    public TextMeshProUGUI displayText;    
    public GameObject introPanel;          
    public CanvasGroup crosshairGroup;     
    public Button closeButton;             // 拖入你的关闭按钮

    [Header("--- 文本内容 ---")]
    [TextArea(3, 10)]
    public string[] paragraphs = new string[] { "欢迎来到苏州古典园林。", "在这里你可以自由走动。" };

    [Header("--- 打字设置 ---")]
    public float typingSpeed = 0.05f;
    public float displayDuration = 2.5f;

    private void Start()
    {
        // 1. 初始化状态
        if (introPanel != null) introPanel.SetActive(true);
        if (closeButton != null) closeButton.gameObject.SetActive(false); // 播放时先隐藏按钮

        // 2. 玩家控制：为了能点到按钮，初始状态需要显示鼠标
        UnlockCursor(true);

        if (crosshairGroup != null) crosshairGroup.alpha = 1f;

        if (displayText != null)
        {
            displayText.color = new Color(displayText.color.r, displayText.color.g, displayText.color.b, 1f);
            StartCoroutine(PlaySequence());
        }

        // 3. 自动绑定按钮点击事件（防止你在 Inspector 里填错）
        if (closeButton != null)
        {
            closeButton.onClick.RemoveAllListeners();
            closeButton.onClick.AddListener(CloseUI);
        }
    }

    IEnumerator PlaySequence()
    {
        if (paragraphs == null || paragraphs.Length == 0) yield break;

        for (int i = 0; i < paragraphs.Length; i++)
        {
            displayText.text = ""; 
            string targetText = paragraphs[i];

            for (int j = 0; j <= targetText.Length; j++)
            {
                displayText.text = targetText.Substring(0, j);
                displayText.ForceMeshUpdate(); 
                yield return new WaitForSeconds(typingSpeed);
            }

            yield return new WaitForSeconds(displayDuration);

            if (i < paragraphs.Length - 1)
            {
                // 段落切换动画
                float t = 0;
                while (t < 1f)
                {
                    t += Time.deltaTime * 2f;
                    Color tempColor = displayText.color;
                    tempColor.a = 1 - t;
                    displayText.color = tempColor;
                    yield return null;
                }
            }
        }

        // 全部播完后，显示关闭按钮
        if (closeButton != null) closeButton.gameObject.SetActive(true);
    }

    // --- 核心方法：关闭 UI 并恢复游戏状态 ---
    public void CloseUI()
    {
        // 隐藏面板
        if (introPanel != null) introPanel.SetActive(false);

        // 重新锁定鼠标，让玩家可以旋转视角走动
        UnlockCursor(false);

        Debug.Log("UI 已关闭，玩家已解锁视角。");
    }

    private void UnlockCursor(bool show)
    {
        if (show)
        {
            Cursor.lockState = CursorLockMode.None; // 不锁定，允许点击按钮
            Cursor.visible = true;
        }
        else
        {
            Cursor.lockState = CursorLockMode.Locked; // 锁定，用于第一人称操作
            Cursor.visible = false;
        }
    }
}