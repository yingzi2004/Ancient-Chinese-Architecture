using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public class BackpackInitialItem
{
    public string itemName = "新物品";
    public Sprite icon;
    [Min(1)] public int amount = 1;
    [Min(1)] public int maxStack = 99;
}

public class BackpackUIController : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject backpackRoot;
    [SerializeField] private Transform slotContainer;
    [SerializeField] private GameObject slotPrefab;

    [Header("Input")]
    [SerializeField] private KeyCode toggleKey = KeyCode.B;

    [Header("Slot Settings")]
    [SerializeField] private int slotCount = 20;
    [SerializeField] private bool buildSlotsOnStart = true;

    [Header("Initial Items")]
    [SerializeField] private bool addInitialItemsOnStart = false;
    [SerializeField] private BackpackInitialItem[] initialItems;

    [Header("Behavior")]
    [SerializeField] private bool hideBackpackOnStart = true;
    [SerializeField] private bool pauseGameWhenOpen = false;
    [SerializeField] private bool unlockCursorWhenOpen = true;

    private bool isOpen;
    private bool useCanvasGroupMode;
    private CanvasGroup backpackCanvasGroup;
    private PlayerController playerController;
    private readonly List<BackpackSlotUI> slotUIs = new List<BackpackSlotUI>();

    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();

        if (backpackRoot == null)
        {
            backpackRoot = gameObject;
        }

        useCanvasGroupMode = backpackRoot == gameObject;

        if (useCanvasGroupMode)
        {
            backpackCanvasGroup = backpackRoot.GetComponent<CanvasGroup>();
            if (backpackCanvasGroup == null)
            {
                backpackCanvasGroup = backpackRoot.AddComponent<CanvasGroup>();
            }
        }

        if (buildSlotsOnStart)
        {
            BuildSlots();
        }
        else
        {
            CacheExistingSlots(true, true);
        }

        if (addInitialItemsOnStart)
        {
            AddAllInitialItems();
        }

        if (hideBackpackOnStart && backpackRoot != null)
        {
            SetBackpackOpen(false);
        }
        else if (backpackRoot != null)
        {
            if (useCanvasGroupMode && backpackCanvasGroup != null)
            {
                isOpen = backpackCanvasGroup.alpha > 0.001f && backpackCanvasGroup.blocksRaycasts;
            }
            else
            {
                isOpen = backpackRoot.activeSelf;
            }
        }
    }

    private void Update()
    {
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleBackpack();
        }
    }

    public void ToggleBackpack()
    {
        SetBackpackOpen(!isOpen);
    }

    public void SetBackpackOpen(bool open)
    {
        isOpen = open;

        if (backpackRoot != null)
        {
            if (useCanvasGroupMode && backpackCanvasGroup != null)
            {
                backpackCanvasGroup.alpha = isOpen ? 1f : 0f;
                backpackCanvasGroup.interactable = isOpen;
                backpackCanvasGroup.blocksRaycasts = isOpen;
            }
            else
            {
                backpackRoot.SetActive(isOpen);
            }
        }

        if (unlockCursorWhenOpen)
        {
            Cursor.visible = isOpen;
            Cursor.lockState = isOpen ? CursorLockMode.None : CursorLockMode.Locked;
        }

        if (playerController != null)
        {
            playerController.isInspecting = isOpen;
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = isOpen ? 0f : 1f;
        }
    }

    private void OnDisable()
    {
        if (playerController != null)
        {
            playerController.isInspecting = false;
        }

        if (pauseGameWhenOpen)
        {
            Time.timeScale = 1f;
        }
    }

    [ContextMenu("Build Slots")]
    public void BuildSlots()
    {
        if (slotContainer == null)
        {
            Debug.LogWarning("[BackpackUIController] 未绑定 Slot Container，无法创建格子。");
            return;
        }

        if (slotPrefab == null)
        {
            Debug.LogWarning("[BackpackUIController] 未绑定 Slot Prefab，改为使用 Slot Container 现有子物体作为格子。", this);
            CacheExistingSlots(true, true);
            return;
        }

        if (slotPrefab == slotContainer.gameObject)
        {
            Debug.LogWarning("[BackpackUIController] Slot Prefab 当前绑定成了 Slot Container，将使用第一个子物体作为模板自动补齐格子。建议把 Slot Prefab 改成单独的格子预制体。", this);
            BuildSlotsFromFirstChildTemplate();
            return;
        }

        ClearSlots();
        slotUIs.Clear();

        for (int i = 0; i < slotCount; i++)
        {
            GameObject slotObject = Instantiate(slotPrefab, slotContainer);
            
            // 确保克隆出来的格子缩放为 1，解决在某些 Canvas 环境下缩放变成 0 的问题
            slotObject.transform.localScale = Vector3.one;
            
            // 禁用克隆物体上所有 GridLayoutGroup 组件，防止干扰格子显示
            // (因为 SlotContainer 本身已经有 GridLayoutGroup 了，子物体不应该再有)
            GridLayoutGroup[] gridLayouts = slotObject.GetComponents<GridLayoutGroup>();
            foreach (GridLayoutGroup gl in gridLayouts)
            {
                gl.enabled = false;
            }

            BackpackSlotUI slotUI = EnsureSlotComponent(slotObject, true);
            slotUIs.Add(slotUI);
        }

        if (slotUIs.Count == 0)
        {
            Debug.LogWarning("[BackpackUIController] 没有可用格子，无法放入物品。", this);
        }
    }

    [ContextMenu("Clear Slots")]
    public void ClearSlots()
    {
        slotUIs.Clear();

        if (slotContainer == null)
        {
            return;
        }

        for (int i = slotContainer.childCount - 1; i >= 0; i--)
        {
            Destroy(slotContainer.GetChild(i).gameObject);
        }
    }

    [ContextMenu("Add First Initial Item")]
    public void AddFirstInitialItem()
    {
        if (initialItems == null || initialItems.Length == 0)
        {
            Debug.LogWarning("[BackpackUIController] initialItems 为空，无法添加第一个物品。");
            return;
        }

        BackpackInitialItem item = initialItems[0];
        if (item == null || item.icon == null)
        {
            Debug.LogWarning("[BackpackUIController] 第一个物品未配置图标。请先给 icon 赋值。");
            return;
        }

        bool success = AddItem(item.icon, item.amount, item.maxStack);
        Debug.Log($"[BackpackUIController] AddFirstInitialItem 结果: {(success ? "成功" : "失败")}", this);
    }

    [ContextMenu("Add All Initial Items")]
    public void AddAllInitialItems()
    {
        if (initialItems == null || initialItems.Length == 0)
        {
            return;
        }

        int successCount = 0;
        int tryCount = 0;

        for (int i = 0; i < initialItems.Length; i++)
        {
            BackpackInitialItem item = initialItems[i];
            if (item == null || item.icon == null || item.amount <= 0)
            {
                continue;
            }

            tryCount++;
            if (AddItem(item.icon, item.amount, item.maxStack))
            {
                successCount++;
            }
        }

        Debug.Log($"[BackpackUIController] 初始物品放入完成: 成功 {successCount}/{tryCount}", this);
    }

    public bool AddItem(Sprite icon, int amount = 1, int maxStack = 99)
    {
        if (icon == null || amount <= 0)
        {
            return false;
        }

        if (!EnsureSlotCacheReady())
        {
            return false;
        }

        int safeMaxStack = Mathf.Max(1, maxStack);
        int remaining = amount;

        for (int i = 0; i < slotUIs.Count && remaining > 0; i++)
        {
            if (slotUIs[i].CanStack(icon, safeMaxStack))
            {
                remaining = slotUIs[i].PutItem(icon, remaining, safeMaxStack);
            }
        }

        for (int i = 0; i < slotUIs.Count && remaining > 0; i++)
        {
            if (slotUIs[i].IsEmpty)
            {
                remaining = slotUIs[i].PutItem(icon, remaining, safeMaxStack);
            }
        }

        if (remaining > 0)
        {
            Debug.LogWarning($"[BackpackUIController] 背包空间不足，仍有 {remaining} 个物品未放入。");
        }

        return remaining == 0;
    }

    private bool EnsureSlotCacheReady()
    {
        if (slotUIs.Count > 0)
        {
            return true;
        }

        CacheExistingSlots(true, false);

        if (slotUIs.Count == 0)
        {
            Debug.LogWarning("[BackpackUIController] 没有可用格子。请检查 Slot Container 下的格子是否挂了 BackpackSlotUI。");
            return false;
        }

        return true;
    }

    private void CacheExistingSlots()
    {
        CacheExistingSlots(true, true);
    }

    private void BuildSlotsFromFirstChildTemplate()
    {
        if (slotContainer.childCount == 0)
        {
            Debug.LogWarning("[BackpackUIController] Slot Container 下没有任何子物体，无法根据模板生成格子。", this);
            return;
        }

        GameObject template = slotContainer.GetChild(0).gameObject;

        for (int i = slotContainer.childCount; i < slotCount; i++)
        {
            Instantiate(template, slotContainer);
        }

        for (int i = slotContainer.childCount - 1; i >= slotCount; i--)
        {
            Destroy(slotContainer.GetChild(i).gameObject);
        }

        CacheExistingSlots(true, true);
        Debug.Log($"[BackpackUIController] 已根据模板生成/整理格子数量到: {slotCount}", this);
    }

    private void CacheExistingSlots(bool addMissingComponent, bool clearSlotData)
    {
        slotUIs.Clear();

        if (slotContainer == null)
        {
            return;
        }

        for (int i = 0; i < slotContainer.childCount; i++)
        {
            GameObject slotObject = slotContainer.GetChild(i).gameObject;
            BackpackSlotUI slotUI = slotObject.GetComponent<BackpackSlotUI>();
            if (slotUI == null && addMissingComponent)
            {
                slotUI = slotObject.AddComponent<BackpackSlotUI>();
            }

            if (slotUI != null)
            {
                slotUI.TryAutoBindReferences();
                if (clearSlotData)
                {
                    slotUI.Clear();
                }
                slotUIs.Add(slotUI);
            }
        }

        Debug.Log($"[BackpackUIController] 已识别格子数量: {slotUIs.Count}", this);

        if (slotUIs.Count == 0)
        {
            Debug.LogWarning("[BackpackUIController] 在 Slot Container 下没有找到可用格子。请确认至少有一个格子子物体。", this);
        }
    }

    private BackpackSlotUI EnsureSlotComponent(GameObject slotObject, bool clearSlotData)
    {
        BackpackSlotUI slotUI = slotObject.GetComponent<BackpackSlotUI>();
        if (slotUI == null)
        {
            slotUI = slotObject.AddComponent<BackpackSlotUI>();
        }

        slotUI.TryAutoBindReferences();
        if (clearSlotData)
        {
            slotUI.Clear();
        }

        return slotUI;
    }
}
