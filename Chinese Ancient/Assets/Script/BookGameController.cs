using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;

/// <summary>
/// 书本翻页控制器 - 适用于 World Space Canvas 中的 BookPro
/// 玩家用准星对准书本上的"上一页/下一页"按钮，点击即可翻页
/// 不需要"打开/关闭"步骤，书本始终在场景中可交互
/// </summary>
public class BookGameController : MonoBehaviour
{
    [Header("Book 引用")]
    [SerializeField] private BookPro book;

    [Header("翻页按钮 (放在书本 World Space Canvas 上的 UI Button)")]
    [Tooltip("下一页按钮 - 放在书本右侧")]
    public Button nextPageButton;
    [Tooltip("上一页按钮 - 放在书本左侧")]
    public Button previousPageButton;

    [Header("准星悬停反馈")]
    [Tooltip("准星悬停时按钮高亮颜色")]
    public Color hoverColor = Color.yellow;

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pageFlipClip;

    [Header("Settings")]
    [SerializeField] private float flipDuration = 0.5f;

    private bool isFlipping = false;

    // --- 准星交互内部状态 ---
    private Button currentHoveredButton = null;
    private Color hoveredOriginalColor;

    private void Awake()
    {
        if (!book)
            book = GetComponentInChildren<BookPro>(true);
    }

    private void Start()
    {
        // 禁用 BookPro 自带的鼠标拖拽交互，完全使用准星 + 按钮
        if (book)
            book.interactable = false;

        UpdateButtonsState();
    }

    // ========================
    //  准星交互核心 (每帧检测)
    // ========================

    private void Update()
    {
        // 1. 准星悬停检测：从屏幕正中心发出 UI 射线
        Button hitButton = GetButtonUnderCrosshair();

        // 2. 更新悬停高亮状态
        UpdateHoverState(hitButton);

        // 3. 准星点击：左键按下时，如果悬停在某个按钮上就执行对应操作
        if (Input.GetMouseButtonDown(0) && currentHoveredButton != null)
        {
            if (currentHoveredButton == nextPageButton)
                FlipNext();
            else if (currentHoveredButton == previousPageButton)
                FlipPrevious();
        }

        // 4. 按 F 键翻到下一页
        if (Input.GetKeyDown(KeyCode.F))
        {
            FlipNext();
        }
    }

    /// <summary>
    /// 用 EventSystem 从屏幕正中心（准星位置）做 UI Raycast，
    /// 检测准星下方是否有我们的翻页按钮。
    /// 对于 World Space Canvas，需要确保 Canvas 上有 GraphicRaycaster，
    /// 并且 Canvas 的 Event Camera 设置为主摄像机。
    /// </summary>
    private Button GetButtonUnderCrosshair()
    {
        if (EventSystem.current == null) return null;

        PointerEventData eventData = new PointerEventData(EventSystem.current);
        eventData.position = new Vector2(Screen.width / 2f, Screen.height / 2f);

        List<RaycastResult> results = new List<RaycastResult>();
        EventSystem.current.RaycastAll(eventData, results);

        foreach (RaycastResult result in results)
        {
            Button btn = FindMatchingButton(result.gameObject);
            if (btn != null) return btn;
        }
        return null;
    }

    /// <summary>
    /// 判断 UI Raycast 命中的 GameObject 是否属于我们的翻页按钮之一
    /// </summary>
    private Button FindMatchingButton(GameObject hitObject)
    {
        Button[] buttons = { nextPageButton, previousPageButton };
        foreach (Button btn in buttons)
        {
            if (btn == null) continue;
            if (hitObject == btn.gameObject || hitObject.transform.IsChildOf(btn.transform))
                return btn;
        }
        return null;
    }

    /// <summary>
    /// 更新准星悬停高亮效果：进入时变色，离开时恢复
    /// </summary>
    private void UpdateHoverState(Button newHovered)
    {
        if (newHovered == currentHoveredButton) return;

        // 离开上一个按钮 → 恢复原色
        if (currentHoveredButton != null && currentHoveredButton.image != null)
        {
            currentHoveredButton.image.color = hoveredOriginalColor;
        }

        // 进入新按钮 → 记录原色 & 高亮
        currentHoveredButton = newHovered;
        if (currentHoveredButton != null && currentHoveredButton.image != null)
        {
            hoveredOriginalColor = currentHoveredButton.image.color;
            currentHoveredButton.image.color = hoverColor;
        }
    }

    // ========================
    //  翻页逻辑
    // ========================

    public void FlipNext()
    {
        if (!book || isFlipping) return;
        if (book.CurrentPaper > book.EndFlippingPaper) return;

        Debug.Log("<color=green>[书本]</color> 翻到下一页");
        StartCoroutine(FlipRoutine(FlipMode.RightToLeft));
    }

    public void FlipPrevious()
    {
        if (!book || isFlipping) return;
        if (book.CurrentPaper <= book.StartFlippingPaper) return;

        Debug.Log("<color=green>[书本]</color> 翻到上一页");
        StartCoroutine(FlipRoutine(FlipMode.LeftToRight));
    }

    private IEnumerator FlipRoutine(FlipMode mode)
    {
        isFlipping = true;

        if (audioSource && pageFlipClip)
            audioSource.PlayOneShot(pageFlipClip);

        PageFlipper.FlipPage(book, flipDuration, mode, () =>
        {
            isFlipping = false;
            UpdateButtonsState();
        });

        yield return null;
    }

    /// <summary>
    /// 到了首/尾页时禁用对应按钮，防止无效翻页
    /// </summary>
    private void UpdateButtonsState()
    {
        if (!book) return;

        if (nextPageButton)
            nextPageButton.interactable = (book.CurrentPaper <= book.EndFlippingPaper);

        if (previousPageButton)
            previousPageButton.interactable = (book.CurrentPaper > book.StartFlippingPaper);
    }
}
