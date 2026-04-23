using UnityEngine;


public class InteractionPromptTester : MonoBehaviour
{
    [Header("测试设置")]
    [Tooltip("测试提示文字")]
    public string testPrompt = "按 [E] 拾取";

    [Tooltip("提示文字颜色")]
    public Color textColor = Color.yellow;

    [Tooltip("是否始终显示测试提示")]
    public bool alwaysShow = true;

    [Tooltip("显示位置偏移")]
    public Vector3 offset = Vector3.up * 2f;

    void OnGUI()
    {
        if (!alwaysShow) return;

        Camera cam = Camera.main;
        if (cam == null) return;

        // 获取物体在屏幕上的位置
        Vector3 screenPos = cam.WorldToScreenPoint(transform.position + offset);

        // 检查是否在摄像机前方
        if (screenPos.z < 0)
        {
            return;
        }

        // 转换Y轴
        float displayY = Screen.height - screenPos.y;

        // 提示框尺寸
        float boxWidth = 200;
        float boxHeight = 50;
        float boxX = screenPos.x - boxWidth / 2;
        float boxY = displayY - boxHeight / 2;

        // 确保在屏幕内
        boxX = Mathf.Clamp(boxX, 10, Screen.width - boxWidth - 10);
        boxY = Mathf.Clamp(boxY, 10, Screen.height - boxHeight - 10);

        // 绘制背景
        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.8f));
        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "", boxStyle);

        // 绘制文字
        GUIStyle textStyle = new GUIStyle();
        textStyle.fontSize = 20;
        textStyle.fontStyle = FontStyle.Bold;
        textStyle.normal.textColor = textColor;
        textStyle.alignment = TextAnchor.MiddleCenter;

        GUI.Label(new Rect(boxX, boxY, boxWidth, boxHeight), testPrompt, textStyle);
    }

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

    void OnDrawGizmos()
    {
        // 在Scene视图中绘制图标，方便找到这个物体
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position + offset, 0.5f);
    }
}
