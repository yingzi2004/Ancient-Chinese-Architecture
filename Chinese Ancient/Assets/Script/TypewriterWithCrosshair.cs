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
    private bool hasBeenTriggered = false;
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
        if (introPanel != null) introPanel.SetActive(false);
        if (closeButton != null)
        {
            closeButton.gameObject.SetActive(true);
            originalColor = closeButton.image.color;
        }
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.useGravity = false;
        }

        if (displayText != null)
        {
            displayText.overflowMode = TextOverflowModes.Overflow;
            // 开启自动换行
            displayText.enableWordWrapping = true;
        }
    }
    private void OnTriggerEnter(Collider other)
    {
        if (hasBeenTriggered || !other.CompareTag("Player")) return;
        Debug.Log("[触发] 玩家进入，面板在原位启动。");
        hasBeenTriggered = true;
        if (introPanel != null)
        {
            introPanel.SetActive(true);
            if (typewriterCoroutine != null) StopCoroutine(typewriterCoroutine);
            typewriterCoroutine = StartCoroutine(PlaySequenceAndAutoClose());
        }
    }
    private void Update()
    {
        if (introPanel != null && introPanel.activeSelf)
        {
            CheckCrosshairHover();
            if (isHovering && Input.GetMouseButtonDown(0))
            {
                CloseUI();
            }
        }
    }
    // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
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
            displayText.text = text;
            displayText.maxVisibleCharacters = 99999;
            displayText.ForceMeshUpdate(true);
            int totalVisibleCharacters = displayText.textInfo.characterCount;
            displayText.maxVisibleCharacters = 0;
            for (int j = 0; j <= totalVisibleCharacters; j++)
            {
                displayText.maxVisibleCharacters = j;
                yield return new WaitForSeconds(typingSpeed);
            }
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
