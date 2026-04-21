using UnityEngine;

/// <summary>
/// 简单的3D文字提示 - 使用TextMesh显示在物体上方
/// 作为OnGUI不工作时的备选方案
/// </summary>
[ExecuteInEditMode]
public class SimplePromptTest : MonoBehaviour
{
    [Header("提示设置")]
    [Tooltip("提示文字")]
    public string promptText = "按E拾取";

    [Tooltip("文字偏移位置")]
    public Vector3 offset = Vector3.up * 2f;

    [Tooltip("文字颜色")]
    public Color textColor = Color.yellow;

    [Tooltip("字体大小")]
    public float fontSize = 1f;

    [Tooltip("是否始终显示")]
    public bool alwaysShow = false;

    private TextMesh textMesh;
    private GameObject textObject;
    private JadePendant jadePendant;
    private float checkInterval = 0.1f; // 每0.1秒检查一次
    private float nextCheckTime;

    void Start()
    {
        // 获取JadePendant组件
        jadePendant = GetComponent<JadePendant>();

        // 创建文字对象
        CreateTextObject();
    }

    void CreateTextObject()
    {
        // 如果已存在，先销毁
        if (textObject != null)
        {
            DestroyImmediate(textObject);
        }

        // 创建新的文字对象
        textObject = new GameObject("PromptText");
        textObject.transform.SetParent(transform);
        textObject.transform.localPosition = offset;

        // 添加TextMesh组件
        textMesh = textObject.AddComponent<TextMesh>();
        textMesh.text = promptText;
        textMesh.fontSize = 64; // TextMesh的字体大小需要很大
        textMesh.color = textColor;
        textMesh.alignment = TextAlignment.Center;
        textMesh.anchor = TextAnchor.MiddleCenter;

        // 设置字体大小
        textMesh.characterSize = fontSize;

        // 让文字始终朝向摄像机
        textObject.AddComponent<FaceCamera>();

        // 初始隐藏
        textObject.SetActive(false);
    }

    void Update()
    {
        // 定期检查是否应该显示提示
        if (Time.time >= nextCheckTime)
        {
            nextCheckTime = Time.time + checkInterval;
            UpdateVisibility();
        }
    }

    void UpdateVisibility()
    {
        if (textObject == null) return;

        bool shouldShow = false;

        if (alwaysShow)
        {
            shouldShow = true;
        }
        else if (jadePendant != null)
        {
            // 通过反射获取canBePickedUp的值
            var field = typeof(JadePendant).GetField("canBePickedUp",
                System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);

            if (field != null)
            {
                bool canPickup = (bool)field.GetValue(jadePendant);
                shouldShow = canPickup;
            }
        }

        // 更新文字
        if (textMesh != null)
        {
            textMesh.text = shouldShow ? promptText : "";
        }

        textObject.SetActive(shouldShow && textMesh != null);
    }

    void OnDestroy()
    {
        if (textObject != null)
        {
            DestroyImmediate(textObject);
        }
    }

    /// <summary>
    /// 在Scene视图中绘制提示范围
    /// </summary>
    void OnDrawGizmos()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position + offset, 0.3f);
    }
}

/// <summary>
/// 让物体始终朝向摄像机
/// </summary>
public class FaceCamera : MonoBehaviour
{
    void Update()
    {
        if (Camera.main != null)
        {
            transform.LookAt(Camera.main.transform);
            // 翻转180度，让文字正面朝向摄像机
            transform.Rotate(0, 180, 0);
        }
    }
}
