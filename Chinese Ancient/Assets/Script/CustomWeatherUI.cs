using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement; // 新增场景管理
using UnityEngine.EventSystems; // 新增：处理EventSystem
using UniStorm; // 必须引入 UniStorm 才能切换天气

public class CustomWeatherUI : MonoBehaviour
{
    [Header("")]
    public KeyCode toggleKey = KeyCode.T; // 热键 T
    public GameObject weatherPanel;       // 把你做的天气面板拖进来

    private PlayerController playerController;
    private Canvas weatherCanvas;
    private CanvasGroup canvasGroup;

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // 过图时重新抓取新场景的玩家引用
        playerController = FindFirstObjectByType<PlayerController>();
        Debug.Log($"[天气UI] 场景切换后重新获取PlayerController: {(playerController != null ? "成功" : "失败")}");

        // 重新初始化UI系统以适应新场景
        if (weatherPanel != null)
        {
            InitializeUISystem();
            Debug.Log("[天气UI] 场景切换后重新初始化UI系统");

            // 场景切换后强制确保Canvas Sort Order最高 ← 传送问题的关键！
            if (weatherCanvas != null)
            {
                weatherCanvas.sortingOrder = 999;
                Debug.Log($"[天气UI] 【场景传送后】确保Canvas Sort Order = 999");
            }
        }

        // 如果天气系统这时候还在开着，保证切场景鼠标控制权正常
        if (weatherPanel != null && weatherPanel.activeSelf)
        {
            Debug.Log("[天气UI] 天气面板在切换时处于打开状态，正在修复UI交互权限...");
            Cursor.visible = true;
            Cursor.lockState = CursorLockMode.None;
            
            // 确保CanvasGroup允许交互
            if (canvasGroup != null)
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
            }
            
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
        // 先找到玩家，以免被后面漏掉
        playerController = FindFirstObjectByType<PlayerController>();

        // 游戏一开始，隐藏你的天气面板
        if (weatherPanel != null) 
        {
            weatherPanel.SetActive(false);
            
            // 提前初始化Canvas和EventSystem
            InitializeUISystem();
        }
        
        // 关键修复：如果在 OnSceneLoaded 时由 activeSelf=true 错误地把玩家锁死了，这里一定要强行解锁
        if (playerController != null)
        {
            playerController.isInspecting = false;
            playerController.SetCursorState(true);
        }
    }

    private void InitializeUISystem()
    {
        // 确保场景中有EventSystem
        if (EventSystem.current == null)
        {
            GameObject eventSystemObj = new GameObject("EventSystem");
            EventSystem es = eventSystemObj.AddComponent<EventSystem>();
            eventSystemObj.AddComponent<StandaloneInputModule>();
            Debug.Log("[天气UI] 自动创建了EventSystem");
        }

        // 获取Canvas并检查必要组件
        weatherCanvas = weatherPanel.GetComponent<Canvas>();
        if (weatherCanvas == null)
        {
            weatherCanvas = weatherPanel.GetComponentInParent<Canvas>();
        }

        if (weatherCanvas != null)
        {
            // ======== 关键：确保Canvas Sort Order足够高 ========
            // 这样即使在传送后，天气UI也不会被其他UI遮挡
            weatherCanvas.sortingOrder = 999;  // 最高优先级
            Debug.Log($"[天气UI] Canvas Sort Order 设置为: {weatherCanvas.sortingOrder}");

            // 检查Canvas RenderMode
            Debug.Log($"[天气UI] Canvas RenderMode: {weatherCanvas.renderMode}");

            // 确保Canvas有GraphicRaycaster
            GraphicRaycaster raycaster = weatherCanvas.GetComponent<GraphicRaycaster>();
            if (raycaster == null)
            {
                weatherCanvas.gameObject.AddComponent<GraphicRaycaster>();
                Debug.Log("[天气UI] Canvas缺少GraphicRaycaster，已自动添加");
            }

            // 获取或创建CanvasGroup
            canvasGroup = weatherPanel.GetComponent<CanvasGroup>();
            if (canvasGroup == null)
            {
                canvasGroup = weatherPanel.AddComponent<CanvasGroup>();
                Debug.Log("[天气UI] 添加了CanvasGroup用于UI事件管理");
            }

            // 确保CanvasGroup允许交互
            canvasGroup.blocksRaycasts = true;
            canvasGroup.interactable = true;

            // 确保weatherPanel本身处于活跃状态（如果现在处于激活）
            Debug.Log($"[天气UI] weatherPanel 激活状态: {weatherPanel.activeSelf}, Canvas激活状态: {weatherCanvas.gameObject.activeSelf}");

            Debug.Log("[天气UI] UI系统初始化完成");
        }
        else
        {
            Debug.LogWarning("[天气UI] 警告：无法找到与天气面板相关的Canvas！");
        }
    }

    void Update()
    {
        // 监听热键呼出/关闭面板
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleWeatherPanel();
        }

        // 持续检查UI是否打开，如果打开但PlayerController丢失，主动重新获取
        if (weatherPanel != null && weatherPanel.activeSelf)
        {
            // 如果PlayerController丢失，重新获取
            if (playerController == null)
            {
                playerController = FindFirstObjectByType<PlayerController>();
                if (playerController != null)
                {
                    playerController.isInspecting = true;
                    Debug.Log("[天气UI] 自动恢复PlayerController并设置isInspecting=true");
                }
            }

            // 如果CanvasGroup丢失或配置错误，修复它
            if (canvasGroup == null)
            {
                canvasGroup = weatherPanel.GetComponent<CanvasGroup>();
                if (canvasGroup == null)
                {
                    canvasGroup = weatherPanel.AddComponent<CanvasGroup>();
                    Debug.Log("[天气UI] 重新添加CanvasGroup");
                }
            }

            // 确保CanvasGroup始终允许交互（关键！）
            if (canvasGroup != null && (!canvasGroup.blocksRaycasts || !canvasGroup.interactable))
            {
                canvasGroup.blocksRaycasts = true;
                canvasGroup.interactable = true;
                Debug.LogWarning("[天气UI] 警告：CanvasGroup配置错误，已自动修复");
            }

            // 传送后的关键检查：确保Canvas Sort Order足够高 ← 解决传送问题
            if (weatherCanvas != null && weatherCanvas.sortingOrder != 999)
            {
                weatherCanvas.sortingOrder = 999;
                Debug.LogWarning($"[天气UI] 警告：Canvas Sort Order不对，已改为999");
            }

            // 确保weatherPanel和Canvas都是活跃的
            if (weatherCanvas != null && !weatherCanvas.gameObject.activeSelf)
            {
                Debug.LogWarning("[天气UI] 警告：Canvas被意外禁用");
            }
        }
    }

    public void ToggleWeatherPanel()
    {
        if (weatherPanel == null) return;

        bool isOpening = !weatherPanel.activeSelf;
        weatherPanel.SetActive(isOpening);
        
        // 每次开关都强制重新获取当前的PlayerController（防止场景切换后引用失效）
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController == null)
        {
            Debug.LogWarning("[天气UI] 无法找到PlayerController！UI和游戏逻辑交互会失效");
        }

        // 如果你平时游戏里是锁定隐藏鼠标的，打开面板时记得解锁并显示鼠标
        Cursor.visible = isOpening;
        Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;

        // 告诉玩家控制器：停下移动和转视角，把鼠标让给UI！
        if (playerController != null)
        {
            playerController.isInspecting = isOpening;
            if (!isOpening)
            {
                playerController.SetCursorState(true); // 恢复视角的锁定状态，保证可以转视角
            }
            Debug.Log($"[天气UI] ToggleWeatherPanel -> isInspecting={isOpening}");
        }

        // 确保CanvasGroup允许交互 ← 这是关键！
        if (canvasGroup != null)
        {
            canvasGroup.blocksRaycasts = isOpening;
            canvasGroup.interactable = isOpening;
            Debug.Log($"[天气UI] CanvasGroup -> blocksRaycasts={isOpening}, interactable={isOpening}");
        }

        // 打开UI时强制设置Canvas Sort Order为最高 ← 解决传送后点不了的问题
        if (isOpening && weatherCanvas != null)
        {
            weatherCanvas.sortingOrder = 999;
            Debug.Log($"[天气UI] 打开面板 -> Canvas Sort Order 强制设置为 999");
        }
    }

    public void ChangeWeatherByIndex(int index)
    {
        if (UniStormSystem.Instance == null)
        {
            Debug.LogError("场景中找不到 UniStormSystem 实例！");
            return;
        }

        var allWeathers = UniStormSystem.Instance.AllWeatherTypes;
        if (index >= 0 && index < allWeathers.Count)
        {
            // 通过获取到的天气类型，让 UniStorm 切换
            WeatherType targetWeather = allWeathers[index];
            UniStormSystem.Instance.ChangeWeather(targetWeather);
            Debug.Log($"【天气系统】成功切换天气至: {targetWeather.WeatherTypeName}");
        }
        else
        {
            Debug.LogWarning("传入的天气序号越界了，请检查按钮绑定的数字！");
        }
    }

    public void ChangeWeatherByType(WeatherType targetWeather)
    {
        if (UniStormSystem.Instance == null)
        {
            Debug.LogError("场景中找不到 UniStormSystem 实例！");
            return;
        }

        if (targetWeather != null)
        {
            UniStormSystem.Instance.ChangeWeather(targetWeather);
            Debug.Log($"【天气系统】成功切换天气至: {targetWeather.WeatherTypeName}");
        }
    }
}
