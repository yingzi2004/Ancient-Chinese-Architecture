using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 交互式祈福灯
/// 场景中放置一个静态灯笼，玩家靠近点击后放飞
/// </summary>
public class InteractivePrayerLantern : MonoBehaviour
{
    [Header("引用")]
    [Tooltip("灯笼预制体（如果没有设置，会从Manager获取）")]
    public GameObject lanternPrefab;

    [Tooltip("玩家Transform")]
    public Transform player;

    [Header("设置")]
    [Tooltip("交互距离")]
    public float interactDistance = 5f;

    [Tooltip("提示文本")]
    public string promptMessage = "按 F 键放飞祈福灯";

    [Tooltip("提示UI（可选）")]
    public GameObject promptUI;

    [Header("祈福设置")]
    [Tooltip("祈福内容（留空则随机）")]
    public string wishText = "";

    [Tooltip("祈福人名称（留空则显示'祈福者'）")]
    public string playerName = "祈福者";

    // 私有变量
    private GameObject currentLantern;
    private bool isReleased = false;
    private bool playerInRange = false;
    private GameObject promptPanel;

    void Start()
    {
        // 查找玩家
        if (player == null)
        {
            GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
            if (playerObj == null)
            {
                PlayerController controller = FindObjectOfType<PlayerController>();
                if (controller != null)
                {
                    playerObj = controller.gameObject;
                }
            }
            if (playerObj != null)
            {
                player = playerObj.transform;
            }
        }

        // 获取预制体
        if (lanternPrefab == null)
        {
            PrayerLanternManager manager = FindObjectOfType<PrayerLanternManager>();
            if (manager != null && manager.lanternPrefab != null)
            {
                lanternPrefab = manager.lanternPrefab;
            }
        }

        // 创建静态灯笼
        CreateStaticLantern();

        // 隐藏提示UI
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }
    }

    void Update()
    {
        if (isReleased || player == null || currentLantern == null) return;

        // 检测玩家距离
        float distance = Vector3.Distance(transform.position, player.position);
        playerInRange = distance <= interactDistance;

        // 调试信息（每秒输出一次）
        if (Time.frameCount % 60 == 0)
        {
            Debug.Log($"[InteractivePrayerLantern] 距离: {distance:F2}m, 范围: {interactDistance}m, 在范围内: {playerInRange}");
        }

        // 显示/隐藏提示
        if (promptUI != null)
        {
            promptUI.SetActive(playerInRange);
        }
        else
        {
            // 如果没有设置UI，尝试自动创建
            if (promptPanel == null)
            {
                CreatePromptPanel();
            }
            if (promptPanel != null)
            {
                promptPanel.SetActive(playerInRange);

                // 调试信息
                if (playerInRange && Time.frameCount % 60 == 0)
                {
                    Debug.Log($"[InteractivePrayerLantern] 显示提示面板");
                }
            }
            else
            {
                if (Time.frameCount % 60 == 0)
                {
                    Debug.LogWarning($"[InteractivePrayerLantern] promptPanel为null，无法显示提示！");
                }
            }
        }

        // 检测交互按键
        if (playerInRange && Input.GetKeyDown(KeyCode.F))
        {
            ReleaseLantern();
        }
    }

    /// <summary>
    /// 创建静态灯笼
    /// </summary>
    void CreateStaticLantern()
    {
        if (lanternPrefab != null)
        {
            // 实例化预制体
            currentLantern = Instantiate(lanternPrefab, transform.position, transform.rotation);
            currentLantern.transform.SetParent(transform);

            // 移除火焰粒子效果
            RemoveFireEffect();

            // 确保不自动升空
            PrayerLantern lantern = currentLantern.GetComponent<PrayerLantern>();
            if (lantern != null)
            {
                // 设置祈福数据
                SetLanternData(lantern);
            }

            Debug.Log("静态祈福灯已创建");
        }
        else
        {
            Debug.LogError("InteractivePrayerLantern: 未设置灯笼预制体！");
        }
    }

    /// <summary>
    /// 移除火焰粒子效果
    /// </summary>
    void RemoveFireEffect()
    {
        if (currentLantern == null) return;

        // 查找所有粒子系统
        ParticleSystem[] particles = currentLantern.GetComponentsInChildren<ParticleSystem>();
        foreach (ParticleSystem ps in particles)
        {
            // 停止并禁用
            if (ps != null)
            {
                ps.Stop();
                GameObject psObj = ps.gameObject;
                if (psObj != null)
                {
                    psObj.SetActive(false);
                }
            }
        }

        Debug.Log("已移除火焰粒子效果");
    }

    /// <summary>
    /// 设置灯笼数据
    /// </summary>
    void SetLanternData(PrayerLantern lantern)
    {
        // 如果没有指定祈福内容，使用默认
        string wish = string.IsNullOrEmpty(wishText) ? "国泰民安，万事如意" : wishText;
        string name = string.IsNullOrEmpty(playerName) ? "祈福者" : playerName;

        // 随机颜色
        Color randomColor = new Color(
            Random.Range(0.8f, 1f),
            Random.Range(0.6f, 0.9f),
            Random.Range(0.4f, 0.7f)
        );

        PrayerLanternData data = new PrayerLanternData(wish, name, randomColor);
        lantern.SetData(data);
    }

    /// <summary>
    /// 放飞祈福灯
    /// </summary>
    void ReleaseLantern()
    {
        if (isReleased || currentLantern == null) return;

        isReleased = true;

        // 隐藏提示
        if (promptUI != null)
        {
            promptUI.SetActive(false);
        }

        if (promptPanel != null)
        {
            promptPanel.SetActive(false);
        }

        // 从父对象分离
        currentLantern.transform.SetParent(null);

        // 开始升空
        PrayerLantern lantern = currentLantern.GetComponent<PrayerLantern>();
        if (lantern != null)
        {
            lantern.StartRising();
        }

        Debug.Log("祈福灯已放飞！");

        // 可选：延迟后生成新的静态灯笼
        // Invoke("CreateStaticLantern", 10f);
    }

    /// <summary>
    /// 在Scene视图绘制交互范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, interactDistance);
    }

    /// <summary>
    /// 创建提示面板
    /// </summary>
    void CreatePromptPanel()
    {
        // 查找Canvas
        Canvas canvas = FindObjectOfType<Canvas>();
        if (canvas == null)
        {
            // 创建新的Canvas
            GameObject canvasObj = new GameObject("PromptCanvas");
            canvasObj.transform.SetParent(null);
            canvasObj.layer = LayerMask.NameToLayer("UI");

            canvas = canvasObj.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100; // 确保在最前面
            canvasObj.AddComponent<CanvasScaler>();
            canvasObj.AddComponent<GraphicRaycaster>();

            // 创建EventSystem
            if (UnityEngine.EventSystems.EventSystem.current == null)
            {
                GameObject eventSystem = new GameObject("EventSystem");
                eventSystem.AddComponent<UnityEngine.EventSystems.EventSystem>();
                eventSystem.AddComponent<UnityEngine.EventSystems.StandaloneInputModule>();
            }

            Debug.Log("已创建新的Canvas");
        }

        // 创建面板
        GameObject panelObj = new GameObject("PromptPanel");
        panelObj.transform.SetParent(canvas.transform, false);
        panelObj.layer = LayerMask.NameToLayer("UI");

        // 添加Image组件作为背景
        UnityEngine.UI.Image panelImage = panelObj.AddComponent<UnityEngine.UI.Image>();
        panelImage.color = new Color(0f, 0f, 0f, 0.7f);

        // 设置RectTransform - 放在屏幕底部中央
        RectTransform panelRect = panelObj.GetComponent<RectTransform>();
        panelRect.anchorMin = new Vector2(0.5f, 0f); // 底部中央
        panelRect.anchorMax = new Vector2(0.5f, 0f);
        panelRect.pivot = new Vector2(0.5f, 0f);
        panelRect.anchoredPosition = new Vector2(0f, 100f); // 距离底部100像素
        panelRect.sizeDelta = new Vector2(400f, 60f);

        // 创建文本
        GameObject textObj = new GameObject("PromptText");
        textObj.transform.SetParent(panelObj.transform, false);
        textObj.layer = LayerMask.NameToLayer("UI");

        UnityEngine.UI.Text text = textObj.AddComponent<UnityEngine.UI.Text>();
        text.text = promptMessage;
        text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        text.fontSize = 28;
        text.alignment = TextAnchor.MiddleCenter;
        text.color = Color.white;

        // 设置文本RectTransform
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        // 添加Outline效果让文字更清晰
        UnityEngine.UI.Outline outline = textObj.AddComponent<UnityEngine.UI.Outline>();
        outline.effectColor = Color.black;
        outline.effectDistance = new Vector2(2f, -2f);

        // 保存引用
        promptPanel = panelObj;

        // 初始隐藏
        panelObj.SetActive(false);

        Debug.Log($"提示面板已创建 - 消息: {promptMessage}");
    }

    void OnMouseDown()
    {
        // 也支持鼠标点击
        if (playerInRange && !isReleased)
        {
            ReleaseLantern();
        }
    }
}
