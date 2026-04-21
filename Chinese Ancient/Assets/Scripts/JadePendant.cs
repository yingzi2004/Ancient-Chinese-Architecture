using UnityEngine;

/// <summary>
/// 玉佩物品脚本 - 挂载到玉佩3D模型上
/// </summary>
public class JadePendant : MonoBehaviour
{
    [Header("玉佩设置")]
    [Tooltip("玉佩唯一ID")]
    public string pendantId = "JadePendant_001";

    [Tooltip("是否可以拾取")]
    public bool canBePickedUp = true;

    [Tooltip("拾取提示文本")]
    public string pickupPrompt = "按 E 键拾取玉佩";

    [Header("玩家设置")]
    [Tooltip("玩家对象（如果不设置，将通过Player标签查找）")]
    public Transform playerTransform;

    [Tooltip("拾取范围（米）")]
    public float pickupRange = 3f;

    [Header("拾取方式")]
    [Tooltip("是否支持鼠标点击拾取")]
    public bool allowClickPickup = true;

    [Tooltip("是否支持键盘E键拾取")]
    public bool allowKeyboardPickup = true;

    [Header("视觉效果")]
    [Tooltip("拾取时的高亮颜色")]
    public Color highlightColor = Color.cyan;

    [Tooltip("是否使用发光效果")]
    public bool useGlowEffect = true;

    [Tooltip("显示调试信息")]
    public bool showDebugInfo = false;

    private Material originalMaterial;
    private Renderer rend;
    private bool isHighlighted = false;
    private JadePendantQuestManager questManager;
    private Collider col;

    void Start()
    {
        rend = GetComponent<Renderer>();
        col = GetComponent<Collider>();

        if (rend != null)
        {
            originalMaterial = rend.material;
        }

        // 如果没有设置玩家引用，尝试通过标签查找
        if (playerTransform == null)
        {
            GameObject player = GameObject.FindGameObjectWithTag("Player");
            if (player != null)
            {
                playerTransform = player.transform;
            }
            else
            {
                // 尝试查找Main Camera
                Camera mainCam = Camera.main;
                if (mainCam != null)
                {
                    playerTransform = mainCam.transform;
                }
                else
                {
                    Debug.LogWarning($"[{gameObject.name}] 无法找到玩家对象！请手动设置playerTransform或确保玩家对象有Player标签");
                }
            }
        }

        // 检查碰撞体
        if (col == null)
        {
            Debug.LogWarning($"[{gameObject.name}] 没有找到Collider组件，已自动添加BoxCollider");
            col = gameObject.AddComponent<BoxCollider>();
            col.isTrigger = true;
        }

        // 查找任务管理器
        questManager = FindObjectOfType<JadePendantQuestManager>();

        // 自动添加发光效果组件
        if (useGlowEffect && GetComponent<ItemGlowHighlight>() == null)
        {
            gameObject.AddComponent<ItemGlowHighlight>();
        }

    }

    void Update()
    {
        if (!canBePickedUp) return;

        // 检查玩家是否在拾取范围内
        bool inRange = IsPlayerInRange();

        if (inRange)
        {
            HighlightObject();

            // 键盘拾取 - 添加调试信息
            if (allowKeyboardPickup)
            {
                if (Input.GetKeyDown(KeyCode.E))
                {
                    Debug.Log($"<color=yellow>[E键检测] 按下E键，正在拾取玉佩: {pendantId}</color>");
                    Pickup();
                }
            }
        }
        else
        {
            RemoveHighlight();
        }
    }

    /// <summary>
    /// 鼠标点击拾取
    /// </summary>
    void OnMouseDown()
    {
        if (!canBePickedUp || !allowClickPickup) return;


        // 检查距离
        if (IsPlayerInRange())
        {
            Pickup();
        }
        else
        {
            Debug.LogWarning($"[{gameObject.name}] 距离太远，无法拾取！当前距离: {GetDistanceToPlayer():F2}米，需要: {pickupRange}米以内");
        }
    }

    /// <summary>
    /// 拾取玉佩
    /// </summary>
    public void Pickup()
    {
        if (!canBePickedUp)
        {
            return;
        }

        Debug.Log($"拾取了玉佩: {pendantId}");

        // 通知任务管理器
        if (questManager != null)
        {
            questManager.OnJadePendantPickedUp(this);
        }

        // 通知玩家拾取系统
        if (PlayerPickup.Instance != null)
        {
            PlayerPickup.Instance.AddPendantToInventory(this);
        }

        // 这里可以添加拾取音效
        // AudioSource.PlayClipAtPoint(pickupSound, transform.position);

        // 禁用碰撞体和渲染
        if (col != null) col.enabled = false;
        if (rend != null) rend.enabled = false;

        canBePickedUp = false;
        this.enabled = false;
    }

    /// <summary>
    /// 检查玩家是否在拾取范围内
    /// </summary>
    private bool IsPlayerInRange()
    {
        if (playerTransform == null)
        {
            return false;
        }

        float distance = GetDistanceToPlayer();
        return distance <= pickupRange;
    }

    /// <summary>
    /// 获取到玩家的距离
    /// </summary>
    private float GetDistanceToPlayer()
    {
        if (playerTransform == null) return float.MaxValue;
        return Vector3.Distance(transform.position, playerTransform.position);
    }

    /// <summary>
    /// 高亮显示物体
    /// </summary>
    private void HighlightObject()
    {
        if (isHighlighted) return;

        if (rend != null && rend.material != null)
        {
            rend.material.EnableKeyword("_EMISSION");
            rend.material.SetColor("_EmissionColor", highlightColor * 0.5f);
            isHighlighted = true;
        }
    }

    /// <summary>
    /// 移除高亮效果
    /// </summary>
    private void RemoveHighlight()
    {
        if (!isHighlighted) return;

        if (rend != null && rend.material != null)
        {
            rend.material.SetColor("_EmissionColor", Color.black);
            isHighlighted = false;
        }
    }

    void OnDestroy()
    {
        if (originalMaterial != null)
        {
            Destroy(originalMaterial);
        }
    }

    /// <summary>
    /// 在Scene视图中绘制拾取范围
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);

        // 绘制到玩家的连线
        if (playerTransform != null)
        {
            Gizmos.color = IsPlayerInRange() ? Color.green : Color.red;
            Gizmos.DrawLine(transform.position, playerTransform.position);
        }
    }

    void OnGUI()
    {
        // 显示拾取提示
        if (canBePickedUp && IsPlayerInRange() && rend != null && rend.enabled)
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            // 获取物体在屏幕上的位置
            Vector3 screenPos = cam.WorldToScreenPoint(transform.position + Vector3.up * 1.5f);

            // 检查是否在摄像机前方
            if (screenPos.z < 0) return;

            // OnGUI的Y轴是倒置的，需要转换
            float displayY = Screen.height - screenPos.y;

            // 提示框尺寸
            float boxWidth = 300;
            float boxHeight = 100;
            float boxX = screenPos.x - boxWidth / 2;
            float boxY = displayY - boxHeight / 2;

            // 确保提示框在屏幕内
            boxX = Mathf.Clamp(boxX, 10, Screen.width - boxWidth - 10);
            boxY = Mathf.Clamp(boxY, 10, Screen.height - boxHeight - 10);

            // 绘制半透明背景
            GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "");

            // 绘制金色边框
            Color goldColor = new Color(1f, 0.8f, 0f);
            GUI.DrawTexture(new Rect(boxX, boxY, boxWidth, 6), MakeTexture(2, 2, goldColor));
            GUI.DrawTexture(new Rect(boxX, boxY + boxHeight - 6, boxWidth, 6), MakeTexture(2, 2, goldColor));

            // 主提示文字样式
            GUIStyle textStyle = new GUIStyle();
            textStyle.fontSize = 32;
            textStyle.fontStyle = FontStyle.Bold;
            textStyle.normal.textColor = goldColor;
            textStyle.alignment = TextAnchor.MiddleCenter;

            // 副提示文字样式
            GUIStyle subTextStyle = new GUIStyle();
            subTextStyle.fontSize = 18;
            subTextStyle.fontStyle = FontStyle.Bold;
            subTextStyle.normal.textColor = Color.white;
            subTextStyle.alignment = TextAnchor.MiddleCenter;

            // 主提示文字
            string mainText = "按 [E] 拾取玉佩";
            string subText = allowClickPickup ? "或点击鼠标拾取" : "";

            // 绘制文字
            GUI.Label(new Rect(boxX, boxY + 15, boxWidth, 50), mainText, textStyle);

            if (!string.IsNullOrEmpty(subText))
            {
                GUI.Label(new Rect(boxX, boxY + 60, boxWidth, 30), subText, subTextStyle);
            }
        }
    }

    /// <summary>
    /// 创建纯色纹理
    /// </summary>
    private Texture2D MakeTexture(int width, int height, Color col)
    {
        Color[] pix = new Color[width * height];
        for (int i = 0; i < pix.Length; i++)
            pix[i] = col;

        Texture2D result = new Texture2D(width, height);
        result.SetPixels(pix);
        result.Apply();
        return result;
    }
}
