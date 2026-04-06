using UnityEditor;
using UnityEngine;
using System.Linq;

// 这是一个编辑器工具类，用于在运行时修复菜单
[InitializeOnLoad]
public class FixProBuilderMenu
{
    static FixProBuilderMenu()
    {
        Debug.Log("🔧 ProBuilder 恢复工具已加载，请在顶部菜单栏选择 Tools > ProBuilder > Force Open Window 尝试手动打开。");
    }

    // 添加一个手动修复的菜单项，改到 Window（窗口）菜单下避免被拦截
    [MenuItem("Window/Fix ProBuilder (强制开启)")]
    private static void ForceReset()
    {
        // 尝试通过反射查找 ProBuilder Editor Assembly
        var assembly = System.AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Unity.ProBuilder.Editor");
        if (assembly != null)
        {
            // 获取 ProBuilderEditor 窗口类型
            var type = assembly.GetType("UnityEditor.ProBuilder.ProBuilderEditor");
            if (type != null)
            {
                var window = EditorWindow.GetWindow(type);
                window.Show();
                window.Focus();
                Debug.Log("✅ 成功唤醒 ProBuilder 窗口！");
                return;
            }
        }

        // 备用方案：尝试调用原生菜单命令（菜单可能存在但界面被遮挡或隐藏）
        bool success = EditorApplication.ExecuteMenuItem("Tools/ProBuilder/ProBuilder Window");
        if (!success)
        {
            Debug.LogError("❌ 无法找到 ProBuilder 所在的程序集或窗口类，请确认 Package Manager 中 ProBuilder 确实已安装并没有报错。");
        }
    }
}