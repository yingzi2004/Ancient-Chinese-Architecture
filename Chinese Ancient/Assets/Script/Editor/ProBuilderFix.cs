using UnityEditor;
using UnityEngine;
using System.Linq;

[InitializeOnLoad]
public class FixProBuilderMenu
{
    static FixProBuilderMenu()
    {
        Debug.Log("🔧 ProBuilder 恢复工具已加载，请在顶部菜单栏选择 Tools > ProBuilder > Force Open Window 尝试手动打开。");
    }
    [MenuItem("Window/Fix ProBuilder (强制开启)")]
    private static void ForceReset()
    {
        var assembly = System.AppDomain.CurrentDomain.GetAssemblies().FirstOrDefault(a => a.GetName().Name == "Unity.ProBuilder.Editor");
        if (assembly != null)
        {
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

        bool success = EditorApplication.ExecuteMenuItem("Tools/ProBuilder/ProBuilder Window");
        if (!success)
        {
            Debug.LogError("❌ 无法找到 ProBuilder 所在的程序集或窗口类，请确认 Package Manager 中 ProBuilder 确实已安装并没有报错。");
        }
    }
}