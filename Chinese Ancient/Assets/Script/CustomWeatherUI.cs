using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using UniStorm;
public class CustomWeatherUI : MonoBehaviour
{
    [Header("输入设置")]
    public KeyCode toggleKey = KeyCode.T;
    public GameObject weatherPanel;       
    private PlayerController playerController;
    private Canvas weatherCanvas;
    private CanvasGroup canvasGroup;
    // AI辅助生成：Kimi K2.6, 2026-04-21
    private bool wasPanelOpenLastFrame = false;
    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }
    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    // AI辅助生成：Kimi K2.6, 2026-04-21
    // 补充生命周期清理，防止对象销毁后事件残留
    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // AI辅助生成：Kimi K2.6, 2026-04-21
        // 防御性检查：weatherPanel 可能因场景切换被销毁
        if (weatherPanel == null)
        {
            Debug.LogWarning("[天气UI] weatherPanel 为空，跳过场景加载初始化");
            return;
        }
        // 过图时重新抓取新场景的玩家引用
        RefreshPlayerReference();
        // 重新初始化UI系统以适应新场景
        InitializeUISystem();
        Debug.Log("[天气UI] 场景切换后重新初始化UI系统");
        // 场景切换后强制确保Canvas Sort Order最高
        EnsureCanvasSortOrder();
        // 如果天气面板这时候还在开着，保证切场景鼠标控制权正常
        if (weatherPanel.activeSelf)
        {
            Debug.Log("[天气UI] 天气面板在切换时处于打开状态，正在修复UI交互权限...");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            SetCanvasGroupInteractable(true);
            if (playerController != null)
            {
                playerController.isInspecting = true;
                Debug.Log("[天气UI] 已设置isInspecting=true");
            }
            else
            {
                Debug.LogWarning("[天气UI] 警告：无法找到新场景的PlayerController！");
            }
        }
    }
    void Start()
    {
        // AI辅助生成：Kimi K2.6, 2026-04-21
        // 防御性检查：面板未赋值时直接返回，避免后续 NullReference
        if (weatherPanel == null)
        {
            Debug.LogError("[天气UI] weatherPanel 未赋值！请在Inspector中绑定天气面板。");
            enabled = false;
            return;
        }
        RefreshPlayerReference();
        // 游戏一开始，隐藏天气面板
        weatherPanel.SetActive(false);
        InitializeUISystem();
        // 关键修复：如果在 OnSceneLoaded 时由 activeSelf=true 错误地把玩家锁死了，这里强行解锁
        if (playerController != null)
        {
            playerController.isInspecting = false;
            playerController.SetCursorState(true);
        }
    }
    private void RefreshPlayerReference()
    {
        playerController = FindFirstObjectByType<PlayerController>();
        Debug.Log($"[天气UI] 重新获取PlayerController: {(playerController != null ? "成功" : "失败")}");
    }
    private void InitializeUISystem()
    {
        // AI辅助生成：Kimi K2.6, 2026-04-21
        // 防御性检查：weatherPanel 可能为空（如被误删）
        if (weatherPanel == null) return;
        EnsureEventSystem();
        ResolveCanvas();
        EnsureGraphicRaycaster();
        ResolveCanvasGroup();
        EnsureCanvasSortOrder();
        Debug.Log("[天气UI] UI系统初始化完成");
    }
    private void EnsureEventSystem()
    {
        if (EventSystem.current != null) return;
        GameObject eventSystemObj = new GameObject("EventSystem");
        eventSystemObj.AddComponent<EventSystem>();
        eventSystemObj.AddComponent<StandaloneInputModule>();
        Debug.Log("[天气UI] 自动创建了EventSystem");
    }
    // AI辅助生成：Kimi K2.6, 2026-04-21
    // 提取 Canvas 解析逻辑，统一处理自身和父级查找
    private void ResolveCanvas()
    {
        weatherCanvas = weatherPanel.GetComponent<Canvas>();
        if (weatherCanvas == null)
        {
            weatherCanvas = weatherPanel.GetComponentInParent<Canvas>();
        }
        if (weatherCanvas != null)
        {
            Debug.Log($"[天气UI] Canvas RenderMode: {weatherCanvas.renderMode}");
        }
        else
        {
            Debug.LogWarning("[天气UI] 警告：无法找到与天气面板相关的Canvas！");
        }
    }
    private void EnsureGraphicRaycaster()
    {
        if (weatherCanvas == null) return;
        GraphicRaycaster raycaster = weatherCanvas.GetComponent<GraphicRaycaster>();
        if (raycaster == null)
        {
            weatherCanvas.gameObject.AddComponent<GraphicRaycaster>();
            Debug.Log("[天气UI] Canvas缺少GraphicRaycaster，已自动添加");
        }
    }
    // AI辅助生成：Kimi K2.6, 2026-04-21
    // 提取 CanvasGroup 解析逻辑
    private void ResolveCanvasGroup()
    {
        if (weatherPanel == null) return;
        canvasGroup = weatherPanel.GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = weatherPanel.AddComponent<CanvasGroup>();
            Debug.Log("[天气UI] 添加了CanvasGroup用于UI事件管理");
        }
        SetCanvasGroupInteractable(true);
    }
    // AI辅助生成：Kimi K2.6, 2026-04-21
    // 提取 Sort Order 设置，统一入口
    private void EnsureCanvasSortOrder()
    {
        if (weatherCanvas == null) return;
        const int targetSortOrder = 999;
        if (weatherCanvas.sortingOrder != targetSortOrder)
        {
            weatherCanvas.sortingOrder = targetSortOrder;
        }
        Debug.Log($"[天气UI] Canvas Sort Order 确保为: {weatherCanvas.sortingOrder}");
    }
    // AI辅助生成：Kimi K2.6, 2026-04-21
    // 提取 CanvasGroup 交互状态设置
    private void SetCanvasGroupInteractable(bool interactable)
    {
        if (canvasGroup == null) return;
        canvasGroup.blocksRaycasts = interactable;
        canvasGroup.interactable = interactable;
    }
    void Update()
    {
        // 监听热键呼出/关闭面板
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleWeatherPanel();
        }
        // AI辅助生成：Kimi K2.6, 2026-04-21
        // 仅在面板状态变化时执行修复，避免每帧重复检测
        bool isPanelOpen = weatherPanel != null && weatherPanel.activeSelf;
        if (isPanelOpen && !wasPanelOpenLastFrame)
        {
            // 面板刚打开，执行一次性修复
            OnPanelOpened();
        }
        wasPanelOpenLastFrame = isPanelOpen;
    }
    // AI辅助生成：Kimi K2.6, 2026-04-21
    // 提取面板打开时的修复逻辑，替代 Update 中每帧检测
    private void OnPanelOpened()
    {
        // 如果PlayerController丢失，重新获取
        if (playerController == null)
        {
            RefreshPlayerReference();
            if (playerController != null)
            {
                playerController.isInspecting = true;
                Debug.Log("[天气UI] 自动恢复PlayerController并设置isInspecting=true");
            }
        }
        // 确保CanvasGroup和Sort Order正确
        if (canvasGroup == null)
        {
            ResolveCanvasGroup();
        }
        SetCanvasGroupInteractable(true);
        EnsureCanvasSortOrder();
    }
    public void ToggleWeatherPanel()
    {
        if (weatherPanel == null)
        {
            Debug.LogWarning("[天气UI] weatherPanel 为空，无法切换面板");
            return;
        }
        bool isOpening = !weatherPanel.activeSelf;
        weatherPanel.SetActive(isOpening);
        // AI辅助生成：Kimi K2.6, 2026-04-21
        // 统一使用 RefreshPlayerReference 替代内联 Find
        RefreshPlayerReference();
        // 鼠标控制
        Cursor.visible = isOpening;
        Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;
        // 玩家控制器状态同步
        if (playerController != null)
        {
            playerController.isInspecting = isOpening;
            if (!isOpening)
            {
                playerController.SetCursorState(true);
            }
            Debug.Log($"[天气UI] ToggleWeatherPanel -> isInspecting={isOpening}");
        }
        // CanvasGroup 交互同步
        SetCanvasGroupInteractable(isOpening);
        Debug.Log($"[天气UI] CanvasGroup -> blocksRaycasts={isOpening}, interactable={isOpening}");
        // 打开时强制设置Canvas Sort Order为最高
        if (isOpening)
        {
            EnsureCanvasSortOrder();
            Debug.Log($"[天气UI] 打开面板 -> Canvas Sort Order 强制设置为 999");
        }
        wasPanelOpenLastFrame = isOpening;
    }
    public void ChangeWeatherByIndex(int index)
    {
        // AI辅助生成：Kimi K2.6, 2026-04-21
        // 防御性检查：UniStorm 实例可能未初始化
        if (UniStormSystem.Instance == null)
        {
            Debug.LogError("[天气系统] 场景中找不到 UniStormSystem 实例！");
            return;
        }
        var allWeathers = UniStormSystem.Instance.AllWeatherTypes;
        if (allWeathers == null)
        {
            Debug.LogError("[天气系统] UniStormSystem.AllWeatherTypes 为空！");
            return;
        }
        if (index >= 0 && index < allWeathers.Count)
        {
            WeatherType targetWeather = allWeathers[index];
            if (targetWeather != null)
            {
                UniStormSystem.Instance.ChangeWeather(targetWeather);
                Debug.Log($"[天气系统] 成功切换天气至: {targetWeather.WeatherTypeName}");
            }
            else
            {
                Debug.LogWarning($"[天气系统] 索引 {index} 对应的天气类型为空！");
            }
        }
        else
        {
            Debug.LogWarning($"[天气系统] 传入的天气序号 {index} 越界（总数：{allWeathers.Count}），请检查按钮绑定的数字！");
        }
    }
    public void ChangeWeatherByType(WeatherType targetWeather)
    {
        if (targetWeather == null)
        {
            Debug.LogWarning("[天气系统] 传入的 WeatherType 为空，跳过切换");
            return;
        }
        if (UniStormSystem.Instance == null)
        {
            Debug.LogError("[天气系统] 场景中找不到 UniStormSystem 实例！");
            return;
        }
        UniStormSystem.Instance.ChangeWeather(targetWeather);
        Debug.Log($"[天气系统] 成功切换天气至: {targetWeather.WeatherTypeName}");
    }
}
