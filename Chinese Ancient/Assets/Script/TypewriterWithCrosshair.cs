using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using System.Collections.Generic;

public class TypewriterTrigger : MonoBehaviour
{
    [Header("--- UI 组件 ---")]
    public TextMeshProUGUI displayText;    
    public GameObject introPanel;          
    public Button closeButton;             

    [Header("--- 准星交互设置 ---")]
    public Color hoverColor = Color.yellow; 
    private Color originalColor;           
    private bool isHovering = false;

    [Header("--- 触发设置 ---")]
    private bool hasBeenTriggered = false; // 确保只能触发一次

    [Header("--- 文本内容 ---")]
    [TextArea(3, 10)]
    public string[] paragraphs = new string[] { "这是固定在世界空间的文字。", "只会触发一次。" };

    [Header("--- 打字与自动关闭设置 ---")]
    public float typingSpeed = 0.05f;
    public float displayDuration = 2.5f;   
    public float autoCloseDelay = 3.0f;    

    private Coroutine typewriterCoroutine;

    private void Start()
    {
        // 初始关闭，但不移动它的坐标
        if (introPanel != null) introPanel.SetActive(false); 
        
        if (closeButton != null) 
        {
            closeButton.gameObject.SetActive(true);
            originalColor = closeButton.image.color;
        }

        // 核心修复：防止空物体下坠导致无法触发
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true; 
            rb.useGravity = false;
        }

        // --- 修复：确保文本框能容纳超长文本 ---
        if (displayText != null)
        {
            // 允许文字溢出边界显示，防止因为框太小而被截断
            displayText.overflowMode = TextOverflowModes.Overflow;
            // 开启自动换行
            displayText.enableWordWrapping = true;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        // 如果已经触发过，或者进来的不是玩家，直接返回
        if (hasBeenTriggered || !other.CompareTag("Player")) return;

        Debug.Log("[触发] 玩家进入，面板在原位启动。");
        hasBeenTriggered = true; // 锁定状态，以后再进来也不会触发了

        if (introPanel != null)
        {
            introPanel.SetActive(true);
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(PlaySequenceAndAutoClose());
        }
    }

    private void Update()
    {
        // 只有面板显示时，才处理准星检测
        if (introPanel != null && introPanel.activeSelf)
        {
            CheckCrosshairHover();

            if (isHovering && Input.GetMouseButtonDown(0))
            {
                CloseUI();
            }
        }
    }

    private void CheckCrosshairHover()
    {
        if (closeButton == null) return;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = new Vector2(Screen.width / 2f, Screen.height / 2f);

        List<RaycastResult> results = new List<RaycastResult>();
        if (EventSystem.current != null)
            EventSystem.current.RaycastAll(eventData, results);

        bool found = false;
        foreach (RaycastResult result in results)
        {
            if (result.gameObject == closeButton.gameObject || result.gameObject.transform.IsChildOf(closeButton.transform))
            {
                found = true;
                break;
            }
        }

        if (found && !isHovering)
        {
            isHovering = true;
            closeButton.image.color = hoverColor;
        }
        else if (!found && isHovering)
        {
            isHovering = false;
            closeButton.image.color = originalColor;
        }
    }

    IEnumerator PlaySequenceAndAutoClose()
    {
        foreach (string text in paragraphs)
        {
            if (!introPanel.activeSelf) yield break;
            
            // 1. 先设置完整文本
            displayText.text = text;
            
            // 2. 关键步骤：先让它全部显示，以便 TMP 计算正确的排版和字符数量
            displayText.maxVisibleCharacters = 99999;
            displayText.ForceMeshUpdate(true); 

            // 3. 获取真实的字符数量（包含空格、换行等所有占位符，但不包含富文本标签字符）
            int totalVisibleCharacters = displayText.textInfo.characterCount;

            // 4. 重置为0，准备开始打字
            displayText.maxVisibleCharacters = 0;

            for (int j = 0; j <= totalVisibleCharacters; j++)
            {
                displayText.maxVisibleCharacters = j;
                yield return new WaitForSeconds(typingSpeed);
            }
            
            // 5. 确保完全显示（防止 characterCount 计数偏差）
            displayText.maxVisibleCharacters = 99999;

            yield return new WaitForSeconds(displayDuration);
        }
        
        yield return new WaitForSeconds(autoCloseDelay);
        if (introPanel.activeSelf) CloseUI();
    }

    public void CloseUI()
    {
        if (introPanel != null) introPanel.SetActive(false);
        isHovering = false;
        if (closeButton != null) closeButton.image.color = originalColor;
    }
}