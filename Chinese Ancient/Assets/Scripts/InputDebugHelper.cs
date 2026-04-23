using UnityEngine;

public class InputDebugHelper : MonoBehaviour
{
    [Header("显示设置")]
    [Tooltip("是否在屏幕上显示输入调试信息")]
    public bool showDebugInfo = true;

    [Tooltip("显示在屏幕上的位置")]
    public Vector2 screenPosition = new Vector2(10, 150);

    private string lastKeyPressed = "";
    private string eKeyStatus = "未按下";

    void Update()
    {
        // 检测E键
        if (Input.GetKeyDown(KeyCode.E))
        {
            eKeyStatus = $"<color=yellow>E键按下! (时间: {Time.time:F2})</color>";
            Debug.Log($"<color=yellow>[InputDebug] E键被按下</color>");
        }

        // 检测任意按键
        if (Input.anyKeyDown)
        {
            foreach (KeyCode key in System.Enum.GetValues(typeof(KeyCode)))
            {
                if (Input.GetKeyDown(key))
                {
                    lastKeyPressed = $"最后按键: {key} (时间: {Time.time:F2})";
                    break;
                }
            }
        }
    }

    void OnGUI()
    {
        if (!showDebugInfo) return;

        // 背景框
        float boxWidth = 400;
        float boxHeight = 120;
        float boxX = screenPosition.x;
        float boxY = screenPosition.y;

        GUIStyle boxStyle = new GUIStyle(GUI.skin.box);
        boxStyle.normal.background = MakeTexture(2, 2, new Color(0, 0, 0, 0.8f));
        GUI.Box(new Rect(boxX, boxY, boxWidth, boxHeight), "", boxStyle);

        // 标题样式
        GUIStyle titleStyle = new GUIStyle();
        titleStyle.fontSize = 16;
        titleStyle.fontStyle = FontStyle.Bold;
        titleStyle.normal.textColor = Color.cyan;

        // 内容样式
        GUIStyle contentStyle = new GUIStyle();
        contentStyle.fontSize = 14;
        contentStyle.normal.textColor = Color.white;
        contentStyle.richText = true;

        // 显示信息
        GUI.Label(new Rect(boxX + 10, boxY + 10, boxWidth - 20, 25),
            "=== 输入调试信息 ===", titleStyle);

        GUI.Label(new Rect(boxX + 10, boxY + 40, boxWidth - 20, 25),
            eKeyStatus, contentStyle);

        GUI.Label(new Rect(boxX + 10, boxY + 65, boxWidth - 20, 25),
            lastKeyPressed, contentStyle);

        GUI.Label(new Rect(boxX + 10, boxY + 90, boxWidth - 20, 25),
            $"<color=gray>提示: 按E键测试输入是否正常</color>", contentStyle);
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
}
