using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using DG.Tweening;

/// <summary>
/// 挂在每个建筑图标上，处理点击事件和悬停动画
/// 自动关联 ScrollMapController
/// </summary>
[RequireComponent(typeof(Image))]
public class MapBuildingButton : MonoBehaviour, IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
{
    [Header("配置")]
    [Tooltip("该建筑在 ScrollMapController.buildings 列表中的索引")]
    public int buildingIndex;

    [Tooltip("悬停放大比例")]
    public float hoverScale = 1.15f;

    [Tooltip("悬停动画时长")]
    public float hoverDuration = 0.2f;

    [Header("提示文字（可选）")]
    [Tooltip("建筑名称提示 UI（鼠标悬停显示）")]
    public CanvasGroup tooltipGroup;

    private ScrollMapController controller;
    private Vector3 originalScale;
    private RectTransform rectTransform;

    private void Start()
    {
        controller = FindObjectOfType<ScrollMapController>();
        rectTransform = GetComponent<RectTransform>();
        originalScale = Vector3.one; // 动画完成后为 1

        if (tooltipGroup != null)
        {
            tooltipGroup.alpha = 0f;
            tooltipGroup.gameObject.SetActive(true);
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (controller == null) return;

        // 点击缩放反馈
        rectTransform.DOKill();
        rectTransform.DOPunchScale(Vector3.one * 0.1f, 0.2f, 6, 0.5f)
            .OnComplete(() =>
            {
                controller.OnBuildingClicked(buildingIndex);
            });
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        // 悬停放大
        rectTransform.DOKill();
        rectTransform.DOScale(originalScale * hoverScale, hoverDuration)
            .SetEase(Ease.OutBack);

        // 显示提示
        if (tooltipGroup != null)
        {
            tooltipGroup.DOKill();
            tooltipGroup.DOFade(1f, 0.2f);
        }
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        // 恢复原大小
        rectTransform.DOKill();
        rectTransform.DOScale(originalScale, hoverDuration)
            .SetEase(Ease.OutQuad);

        // 隐藏提示
        if (tooltipGroup != null)
        {
            tooltipGroup.DOKill();
            tooltipGroup.DOFade(0f, 0.15f);
        }
    }

    private void OnDestroy()
    {
        rectTransform.DOKill();
        if (tooltipGroup != null) tooltipGroup.DOKill();
    }
}
