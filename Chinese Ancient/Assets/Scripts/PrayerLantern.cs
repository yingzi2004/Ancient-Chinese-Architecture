using System.Collections;
using UnityEngine;

/// <summary>
/// 单个祈福灯脚本
/// 控制祈福灯的升空、飘动和交互
/// </summary>
public class PrayerLantern : MonoBehaviour
{
    [Header("祈福灯数据")]
    public PrayerLanternData data;

    [Header("升空设置")]
    [Tooltip("升空速度")]
    public float riseSpeed = 2f;

    [Tooltip("最大高度（升空到此高度后消失）")]
    public float maxHeight = 100f;

    [Tooltip("水平飘动范围")]
    public float wanderRange = 5f;

    [Tooltip("飘动速度")]
    public float wanderSpeed = 0.5f;

    [Header("视觉设置")]
    [Tooltip("灯笼光源")]
    public Light lanternLight;

    [Tooltip("火焰粒子效果")]
    public ParticleSystem fireParticle;

    [Tooltip("灯笼材质")]
    public Renderer lanternRenderer;

    [Header("交互设置")]
    [Tooltip("可否点击查看详情")]
    public bool canClickToView = true;

    [Tooltip("交互距离")]
    public float interactDistance = 10f;

    // 私有变量
    private Vector3 startPosition;
    private float riseStartTime;
    private bool isRising = false;
    private Vector3 wanderOffset;
    private Canvas infoCanvas; // 显示祈福内容的Canvas

    void Start()
    {
        startPosition = transform.position;

        // 初始化视觉
        InitializeVisuals();

        // 创建信息Canvas
        CreateInfoCanvas();
    }

    /// <summary>
    /// 初始化视觉效果
    /// </summary>
    private void InitializeVisuals()
    {
        // 设置光源颜色
        if (lanternLight != null)
        {
            lanternLight.color = data?.lanternColor ?? Color.yellow;
        }

        // 设置材质颜色（如果有）
        if (lanternRenderer != null && data != null)
        {
            // 尝试修改材质颜色
            if (lanternRenderer.material.HasProperty("_Color"))
            {
                lanternRenderer.material.SetColor("_Color", data.lanternColor);
            }
        }
    }

    /// <summary>
    /// 创建显示祈福信息的Canvas
    /// </summary>
    private void CreateInfoCanvas()
    {
        // 创建Canvas对象
        GameObject canvasObj = new GameObject("InfoCanvas");
        canvasObj.transform.SetParent(transform);
        canvasObj.transform.localPosition = Vector3.zero;

        infoCanvas = canvasObj.AddComponent<Canvas>();
        infoCanvas.renderMode = RenderMode.WorldSpace;
        infoCanvas.worldCamera = Camera.main;

        RectTransform canvasRect = canvasObj.GetComponent<RectTransform>();
        canvasRect.sizeDelta = new Vector2(3f, 2f);
        canvasRect.localPosition = new Vector3(0, 0.5f, 0);

        // 初始隐藏
        canvasObj.SetActive(false);
    }

    /// <summary>
    /// 开始升空
    /// </summary>
    public void StartRising()
    {
        if (!isRising)
        {
            isRising = true;
            riseStartTime = Time.time;
            StartCoroutine(RiseAndWander());
        }
    }

    /// <summary>
    /// 升空和飘动协程
    /// </summary>
    private IEnumerator RiseAndWander()
    {
        while (isRising && transform.position.y < maxHeight)
        {
            // 向上移动
            Vector3 riseMovement = Vector3.up * riseSpeed * Time.deltaTime;

            // 水平飘动（使用正弦波创建平滑的运动）
            float time = Time.time - riseStartTime;
            float wanderX = Mathf.Sin(time * wanderSpeed) * wanderRange * 0.5f;
            float wanderZ = Mathf.Cos(time * wanderSpeed * 0.7f) * wanderRange * 0.5f;
            Vector3 wanderMovement = new Vector3(wanderX, 0, wanderZ) * Time.deltaTime;

            // 应用移动
            transform.position += riseMovement + wanderMovement;

            // 缓慢旋转
            transform.rotation = Quaternion.Euler(0, time * 10f, 0);

            yield return null;
        }

        // 到达最大高度后消失
        FadeOutAndDestroy();
    }

    /// <summary>
    /// 淡出并销毁
    /// </summary>
    private void FadeOutAndDestroy()
    {
        StartCoroutine(FadeOutCoroutine());
    }

    private IEnumerator FadeOutCoroutine()
    {
        float fadeDuration = 2f;
        float elapsedTime = 0f;

        // 获取初始透明度
        float initialAlpha = 1f;

        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            float alpha = Mathf.Lerp(initialAlpha, 0f, elapsedTime / fadeDuration);

            // 淡出光源
            if (lanternLight != null)
            {
                lanternLight.intensity = alpha;
            }

            // 淡出材质
            if (lanternRenderer != null)
            {
                Color color = lanternRenderer.material.color;
                color.a = alpha;
                lanternRenderer.material.color = color;
            }

            yield return null;
        }

        Destroy(gameObject);
    }

    /// <summary>
    /// 显示祈福内容（当玩家点击时）
    /// </summary>
    public void ShowWishInfo()
    {
        if (!canClickToView || data == null) return;

        // 检查距离
        if (Vector3.Distance(transform.position, Camera.main.transform.position) > interactDistance)
        {
            Debug.Log("太远了，无法查看祈福内容");
            return;
        }

        // 显示Canvas内容
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(true);

            // 这里可以添加文本显示
            // 实际实现需要在Canvas上添加Text组件
        }

        // 通知管理器显示详情
        if (PrayerLanternManager.Instance != null)
        {
            PrayerLanternManager.Instance.ShowLanternDetail(data);
        }

        Debug.Log($"祈福内容: {data.wishText}\n祈福人: {data.playerName}\n时间: {data.GetTimeString()}");
    }

    /// <summary>
    /// 隐藏祈福内容
    /// </summary>
    public void HideWishInfo()
    {
        if (infoCanvas != null)
        {
            infoCanvas.gameObject.SetActive(false);
        }
    }

    void OnMouseOver()
    {
        // 鼠标悬停时高亮
        if (lanternRenderer != null)
        {
            lanternRenderer.material.SetFloat("_Outline", 1f);
        }
    }

    void OnMouseExit()
    {
        // 鼠标离开时取消高亮
        if (lanternRenderer != null)
        {
            lanternRenderer.material.SetFloat("_Outline", 0f);
        }
    }

    void OnMouseDown()
    {
        if (canClickToView)
        {
            ShowWishInfo();
        }
    }

    void OnDestroy()
    {
        // 清理
        if (infoCanvas != null)
        {
            Destroy(infoCanvas.gameObject);
        }
    }

    /// <summary>
    /// 设置祈福灯数据
    /// </summary>
    public void SetData(PrayerLanternData newData)
    {
        data = newData;
        InitializeVisuals();
    }
}
