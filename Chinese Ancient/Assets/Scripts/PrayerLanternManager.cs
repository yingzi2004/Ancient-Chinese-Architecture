using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 祈福灯管理器
/// 管理场景中所有祈福灯的生成、保存和显示
/// </summary>
public class PrayerLanternManager : MonoBehaviour
{
    public static PrayerLanternManager Instance { get; private set; }

    [Header("祈福灯预制体")]
    [Tooltip("祈福灯预制体")]
    public GameObject lanternPrefab;

    [Header("生成设置")]
    [Tooltip("生成点（祈福灯从这里起飞）")]
    public Transform spawnPoint;

    [Tooltip("同时最大显示的祈福灯数量")]
    public int maxLanterns = 50;

    [Tooltip("自动生成的间隔时间")]
    public float autoSpawnInterval = 5f;

    [Tooltip("是否自动生成祈福灯")]
    public bool autoSpawn = true;

    [Header("颜色选项")]
    [Tooltip("可选的灯笼颜色")]
    public Color[] lanternColors = new Color[]
    {
        new Color(1f, 0.8f, 0.4f),  // 金黄色
        new Color(1f, 0.6f, 0.2f),  // 橙色
        new Color(1f, 0.9f, 0.6f),  // 淡黄色
        new Color(1f, 0.4f, 0.4f),  // 红色
        new Color(0.8f, 0.6f, 1f),  // 淡紫色
    };

    [Header("UI引用")]
    [Tooltip("详情面板（显示祈福内容）")]
    public GameObject detailPanel;

    [Tooltip("祈福内容文本")]
    public Text wishText;

    [Tooltip("祈福人文本")]
    public Text playerNameText;

    [Tooltip("时间文本")]
    public Text timeText;

    [Header("保存设置")]
    [Tooltip("保存文件名")]
    private string saveFileName = "prayer_lanterns.json";

    [Tooltip("最多保存多少条祈福记录")]
    public int maxSavedLanterns = 100;

    // 私有变量
    private List<PrayerLanternData> allPrayers = new List<PrayerLanternData>();
    private List<PrayerLantern> activeLanterns = new List<PrayerLantern>();
    private Coroutine autoSpawnCoroutine;

    void Awake()
    {
        // 单例模式
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
            return;
        }

        // 查找生成点
        if (spawnPoint == null)
        {
            // 尝试找名为"LanternSpawnPoint"的对象
            GameObject spawnObj = GameObject.Find("LanternSpawnPoint");
            if (spawnObj != null)
            {
                spawnPoint = spawnObj.transform;
            }
            else
            {
                // 如果找不到，使用当前位置
                spawnPoint = transform;
                Debug.LogWarning("PrayerLanternManager: 未找到LanternSpawnPoint，使用当前位置作为生成点");
            }
        }

        // 查找UI
        FindUIElements();

        // 加载已保存的祈福
        LoadPrayers();
    }

    void Start()
    {
        // 开始自动生成
        if (autoSpawn)
        {
            StartAutoSpawn();
        }

        // 隐藏详情面板
        HideDetailPanel();
    }

    void Update()
    {
        // 按ESC隐藏详情面板
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            HideDetailPanel();
        }

        // 清理已销毁的灯笼
        activeLanterns.RemoveAll(l => l == null);
    }

    /// <summary>
    /// 查找UI元素
    /// </summary>
    private void FindUIElements()
    {
        if (detailPanel == null)
        {
            GameObject panel = GameObject.Find("LanternDetailPanel");
            if (panel != null)
            {
                detailPanel = panel;
            }
        }

        if (detailPanel != null)
        {
            if (wishText == null)
            {
                Transform textTransform = detailPanel.transform.Find("WishText");
                if (textTransform != null)
                {
                    wishText = textTransform.GetComponent<Text>();
                }
            }

            if (playerNameText == null)
            {
                Transform nameTransform = detailPanel.transform.Find("PlayerNameText");
                if (nameTransform != null)
                {
                    playerNameText = nameTransform.GetComponent<Text>();
                }
            }

            if (timeText == null)
            {
                Transform timeTransform = detailPanel.transform.Find("TimeText");
                if (timeTransform != null)
                {
                    timeText = timeTransform.GetComponent<Text>();
                }
            }
        }
    }

    /// <summary>
    /// 开始自动生成祈福灯
    /// </summary>
    public void StartAutoSpawn()
    {
        if (autoSpawnCoroutine != null)
        {
            StopCoroutine(autoSpawnCoroutine);
        }
        autoSpawnCoroutine = StartCoroutine(AutoSpawnRoutine());
    }

    /// <summary>
    /// 停止自动生成
    /// </summary>
    public void StopAutoSpawn()
    {
        if (autoSpawnCoroutine != null)
        {
            StopCoroutine(autoSpawnCoroutine);
            autoSpawnCoroutine = null;
        }
    }

    /// <summary>
    /// 自动生成协程
    /// </summary>
    private IEnumerator AutoSpawnRoutine()
    {
        while (autoSpawn)
        {
            yield return new WaitForSeconds(autoSpawnInterval);

            // 从保存的祈福中随机选择一个生成
            if (allPrayers.Count > 0)
            {
                PrayerLanternData randomPrayer = allPrayers[Random.Range(0, allPrayers.Count)];
                SpawnLantern(randomPrayer);
            }
        }
    }

    /// <summary>
    /// 生成一个新的祈福灯
    /// </summary>
    public void SpawnLantern(PrayerLanternData data)
    {
        // 检查是否超过最大数量
        if (activeLanterns.Count >= maxLanterns)
        {
            Debug.Log("已达到最大祈福灯数量");
            return;
        }

        // 实例化预制体
        if (lanternPrefab != null)
        {
            GameObject lanternObj = Instantiate(lanternPrefab, spawnPoint.position, Quaternion.identity);
            lanternObj.transform.SetParent(transform);

            // 获取PrayerLantern组件
            PrayerLantern lantern = lanternObj.GetComponent<PrayerLantern>();
            if (lantern != null)
            {
                lantern.SetData(data);
                lantern.StartRising();
                activeLanterns.Add(lantern);
            }
            else
            {
                Debug.LogError("祈福灯预制体上没有PrayerLantern组件！");
                Destroy(lanternObj);
            }
        }
        else
        {
            Debug.LogError("未设置祈福灯预制体！");
        }
    }

    /// <summary>
    /// 添加新的祈福
    /// </summary>
    public void AddPrayer(string wishText, string playerName = "游客")
    {
        // 随机选择颜色
        Color randomColor = lanternColors[Random.Range(0, lanternColors.Length)];

        // 创建祈福数据
        PrayerLanternData newPrayer = new PrayerLanternData(wishText, playerName, randomColor);

        // 添加到列表
        allPrayers.Add(newPrayer);

        // 限制保存数量
        if (allPrayers.Count > maxSavedLanterns)
        {
            allPrayers.RemoveAt(0);
        }

        // 保存到文件
        SavePrayers();

        // 立即生成这个祈福灯
        SpawnLantern(newPrayer);

        Debug.Log($"祈福成功: {wishText}");
    }

    /// <summary>
    /// 显示祈福详情
    /// </summary>
    public void ShowLanternDetail(PrayerLanternData data)
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(true);

            if (wishText != null)
            {
                wishText.text = data.wishText;
            }

            if (playerNameText != null)
            {
                playerNameText.text = $"祈福人: {data.playerName}";
            }

            if (timeText != null)
            {
                timeText.text = data.GetTimeString();
            }
        }
    }

    /// <summary>
    /// 隐藏详情面板
    /// </summary>
    public void HideDetailPanel()
    {
        if (detailPanel != null)
        {
            detailPanel.SetActive(false);
        }
    }

    /// <summary>
    /// 保存祈福到文件
    /// </summary>
    private void SavePrayers()
    {
        string json = JsonUtility.ToJson(new PrayerLanternDataList(allPrayers), true);
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);
        System.IO.File.WriteAllText(filePath, json);
        Debug.Log($"祈福已保存到: {filePath}");
    }

    /// <summary>
    /// 从文件加载祈福
    /// </summary>
    private void LoadPrayers()
    {
        string filePath = System.IO.Path.Combine(Application.persistentDataPath, saveFileName);

        if (System.IO.File.Exists(filePath))
        {
            try
            {
                string json = System.IO.File.ReadAllText(filePath);
                PrayerLanternDataList list = JsonUtility.FromJson<PrayerLanternDataList>(json);

                if (list != null && list.lanterns != null)
                {
                    allPrayers = list.lanterns.ToList();
                    Debug.Log($"已加载 {allPrayers.Count} 条祈福记录");
                }
            }
            catch (System.Exception e)
            {
                Debug.LogError($"加载祈福失败: {e.Message}");
            }
        }
        else
        {
            Debug.Log("未找到祈福保存文件，使用默认祈福");

            // 添加一些默认祈福
            AddDefaultPrayers();
        }
    }

    /// <summary>
    /// 添加默认祈福（用于首次运行）
    /// </summary>
    private void AddDefaultPrayers()
    {
        allPrayers.Add(new PrayerLanternData("国泰民安，风调雨顺", "古人", lanternColors[0]));
        allPrayers.Add(new PrayerLanternData("家人平安健康", "祈福者", lanternColors[1]));
        allPrayers.Add(new PrayerLanternData("学业进步，金榜题名", "学子", lanternColors[2]));
        allPrayers.Add(new PrayerLanternData("五谷丰登，年年有余", "农夫", lanternColors[3]));
        allPrayers.Add(new PrayerLanternData("世界和平", "善信", lanternColors[4]));
    }

    /// <summary>
    /// 清除所有祈福数据
    /// </summary>
    public void ClearAllPrayers()
    {
        allPrayers.Clear();
        SavePrayers();
        Debug.Log("已清除所有祈福数据");
    }

    /// <summary>
    /// 获取祈福数量
    /// </summary>
    public int GetPrayerCount()
    {
        return allPrayers.Count;
    }

    void OnDestroy()
    {
        // 停止自动生成
        StopAutoSpawn();
    }
}
