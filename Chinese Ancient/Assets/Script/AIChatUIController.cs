using UnityEngine;
public class AIChatUIController : MonoBehaviour
{
    [Header("依赖")]
    [SerializeField] private F1AIChatUI chatUI;
    private void Start()
    {
        if (chatUI == null)
        {
            chatUI = FindFirstObjectByType<F1AIChatUI>();
        }
    }
    public void SetAncientChineseTheme()
    {
        if (chatUI == null) return;
        // 古色古香的配色
        chatUI.SetBackgroundColor(new Color(0.18f, 0.16f, 0.14f, 0.95f));  // 深棕色
        chatUI.SetContentBackgroundColor(new Color(0.12f, 0.11f, 0.09f, 0.85f));  // 更深的棕色
        chatUI.SetTextColor(new Color(0.95f, 0.9f, 0.8f, 1f));  // 米黄色文字
        chatUI.SetFontSize(20);
        // 按钮：古铜色调
        chatUI.SetButtonColors(
            new Color(0.35f, 0.28f, 0.2f, 1f),   // 正常
            new Color(0.45f, 0.38f, 0.28f, 1f), // 悬停
            new Color(0.55f, 0.45f, 0.3f, 1f)   // 点击
        );
        chatUI.SetButtonTextColor(new Color(0.95f, 0.9f, 0.8f, 1f));
        // 输入框
        chatUI.SetInputBackgroundColor(new Color(0.1f, 0.09f, 0.07f, 0.9f));
        chatUI.SetInputTextColor(new Color(0.95f, 0.9f, 0.8f, 1f));
        // 标题
        chatUI.SetWindowTitle("古建筑导览助手");
        chatUI.SetTitleTextColor(new Color(0.95f, 0.85f, 0.65f, 1f));
    }
    public void SetModernTechTheme()
    {
        if (chatUI == null) return;
        chatUI.SetBackgroundColor(new Color(0.1f, 0.15f, 0.2f, 0.95f));  // 深蓝灰色
        chatUI.SetContentBackgroundColor(new Color(0.05f, 0.08f, 0.12f, 0.9f));  // 更深的蓝黑色
        chatUI.SetTextColor(new Color(0.4f, 0.8f, 1f, 1f));  // 青蓝色文字
        chatUI.SetFontSize(18);
        // 按钮：科技蓝
        chatUI.SetButtonColors(
            new Color(0.15f, 0.3f, 0.5f, 1f),   // 正常
            new Color(0.2f, 0.4f, 0.6f, 1f),   // 悬停
            new Color(0.1f, 0.5f, 0.8f, 1f)    // 点击
        );
        chatUI.SetButtonTextColor(Color.white);
        // 输入框
        chatUI.SetInputBackgroundColor(new Color(0.08f, 0.12f, 0.18f, 0.95f));
        chatUI.SetInputTextColor(new Color(0.7f, 0.9f, 1f, 1f));
        // 标题
        chatUI.SetWindowTitle("AI Assistant");
        chatUI.SetTitleTextColor(new Color(0.4f, 0.8f, 1f, 1f));
    }
    public void SetSakuraTheme()
    {
        if (chatUI == null) return;
        // 不改背景图，只调整其他元素颜色以配合
        chatUI.SetContentBackgroundColor(new Color(0.1f, 0.1f, 0.15f, 0.7f));  // 半透明深色
        chatUI.SetTextColor(Color.white);
        chatUI.SetFontSize(18);
        // 按钮：粉色调
        chatUI.SetButtonColors(
            new Color(0.7f, 0.4f, 0.5f, 0.9f),   // 正常 - 暗粉
            new Color(0.85f, 0.5f, 0.6f, 0.95f), // 悬停 - 亮粉
            new Color(0.9f, 0.6f, 0.7f, 1f)      // 点击 - 更亮
        );
        chatUI.SetButtonTextColor(Color.white);
        // 输入框
        chatUI.SetInputBackgroundColor(new Color(0.15f, 0.1f, 0.12f, 0.85f));
        chatUI.SetInputTextColor(Color.white);
        // 标题
        chatUI.SetWindowTitle("AI 对话");
        chatUI.SetTitleTextColor(new Color(1f, 0.9f, 0.95f, 1f));
    }
    public void ResetToDefault()
    {
        if (chatUI == null) return;
        chatUI.SetWindowRect(30f, 30f, 620f, 520f);
        chatUI.SetBackgroundColor(new Color(0.2f, 0.2f, 0.2f, 0.95f));
        chatUI.SetContentBackgroundColor(new Color(0.1f, 0.1f, 0.1f, 0.8f));
        chatUI.SetTextColor(Color.white);
        chatUI.SetFontSize(18);
        chatUI.SetButtonColors(
            new Color(0.3f, 0.3f, 0.3f, 1f),
            new Color(0.4f, 0.4f, 0.4f, 1f),
            new Color(0.2f, 0.5f, 0.8f, 1f)
        );
        chatUI.SetButtonTextColor(Color.white);
        chatUI.SetInputBackgroundColor(new Color(0.15f, 0.15f, 0.15f, 0.9f));
        chatUI.SetInputTextColor(Color.white);
        chatUI.SetWindowTitle("AI 对话（千问）");
        chatUI.SetTitleTextColor(Color.white);
    }
    public void CenterWindow()
    {
        if (chatUI == null) return;
        Rect rect = chatUI.GetWindowRect();
        float x = (Screen.width - rect.width) / 2f;
        float y = (Screen.height - rect.height) / 2f;
        chatUI.SetWindowPosition(x, y);
    }
    public void SetWindowSize(string size)
    {
        if (chatUI == null) return;
        switch (size.ToLower())
        {
            case "small":
                chatUI.SetWindowSize(400f, 350f);
                break;
            case "medium":
                chatUI.SetWindowSize(620f, 520f);
                break;
            case "large":
                chatUI.SetWindowSize(800f, 600f);
                break;
        }
    }
    public void SetCustomBackground(Texture2D texture)
    {
        if (chatUI == null) return;
        if (texture != null)
        {
            chatUI.SetBackgroundTexture(texture);
        }
        else
        {
            chatUI.ClearBackgroundTexture();
        }
    }
}
