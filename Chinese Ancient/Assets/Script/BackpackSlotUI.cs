using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BackpackSlotUI : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text countText;

    private Sprite currentIcon;
    private int currentAmount;
    private Image runtimeItemIconImage;

    public bool IsEmpty => currentIcon == null || currentAmount <= 0;

    private void Awake()
    {
        TryAutoBindReferences();
        RefreshView();
    }

    public void TryAutoBindReferences()
    {
        if (countText == null)
        {
            countText = GetComponentInChildren<TMP_Text>(true);
        }

        EnsureRuntimeIconImage();
    }

    public bool CanStack(Sprite icon, int maxStack)
    {
        return !IsEmpty && currentIcon == icon && currentAmount < maxStack;
    }

    public int PutItem(Sprite icon, int amount, int maxStack)
    {
        if (icon == null || amount <= 0)
        {
            return amount;
        }

        int safeMaxStack = Mathf.Max(1, maxStack);

        if (IsEmpty)
        {
            currentIcon = icon;
            currentAmount = 0;
        }
        else if (currentIcon != icon)
        {
            return amount;
        }

        int canAdd = safeMaxStack - currentAmount;
        if (canAdd <= 0)
        {
            return amount;
        }

        int addCount = Mathf.Min(canAdd, amount);
        currentAmount += addCount;
        RefreshView();

        return amount - addCount;
    }

    public void Clear()
    {
        currentIcon = null;
        currentAmount = 0;
        RefreshView();
    }

    private void RefreshView()
    {
        bool hasItem = !IsEmpty;
        Image displayImage = GetDisplayImage();

        if (displayImage != null)
        {
            displayImage.enabled = hasItem;
            displayImage.sprite = hasItem ? currentIcon : null;
            if (hasItem)
            {
                displayImage.color = Color.white;
                
                // 强制修正由于预制体或者布局组件导致的图片位置偏移
                RectTransform rect = displayImage.GetComponent<RectTransform>();
                if (rect != null)
                {
                    // 设置锚点为完全伸展(撑满父物体)，并带有一点边距
                    rect.anchorMin = new Vector2(0.1f, 0.1f);
                    rect.anchorMax = new Vector2(0.9f, 0.9f);
                    rect.anchoredPosition3D = Vector3.zero;
                    rect.sizeDelta = Vector2.zero;
                    rect.localRotation = Quaternion.identity;
                    rect.localScale = Vector3.one;
                }
            }
        }

        if (countText != null)
        {
            bool showCount = hasItem && currentAmount > 1;
            countText.gameObject.SetActive(showCount);
            countText.text = showCount ? currentAmount.ToString() : string.Empty;
        }
    }

    private void EnsureRuntimeIconImage()
    {
        if (runtimeItemIconImage != null)
        {
            return;
        }

        Transform existing = transform.Find("ItemIconRuntime");
        if (existing != null)
        {
            runtimeItemIconImage = existing.GetComponent<Image>();
        }

        if (runtimeItemIconImage == null)
        {
            GameObject iconObject = new GameObject("ItemIconRuntime", typeof(RectTransform), typeof(Image));
            iconObject.transform.SetParent(transform, false);

            RectTransform iconRect = iconObject.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.15f, 0.15f);
            iconRect.anchorMax = new Vector2(0.85f, 0.85f);
            iconRect.offsetMin = Vector2.zero;
            iconRect.offsetMax = Vector2.zero;
            iconRect.localScale = Vector3.one;
            iconRect.anchoredPosition3D = Vector3.zero;
            iconRect.localPosition = new Vector3(iconRect.localPosition.x, iconRect.localPosition.y, 0);

            runtimeItemIconImage = iconObject.GetComponent<Image>();
            runtimeItemIconImage.raycastTarget = false;
            runtimeItemIconImage.preserveAspect = true;
            runtimeItemIconImage.enabled = false;
        }
    }

    private Image GetDisplayImage()
    {
        if (iconImage != null && iconImage.transform != transform)
        {
            return iconImage;
        }

        if (runtimeItemIconImage == null)
        {
            EnsureRuntimeIconImage();
        }

        return runtimeItemIconImage;
    }
}
