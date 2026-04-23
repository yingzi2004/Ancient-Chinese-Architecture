using UnityEngine;
using UnityEditor;
using TMPro;

public class FindCulpritsTool : EditorWindow
{
    [MenuItem("Tools/一键找出飞升与报错的元凶")]
    public static void FindTheBugs()
    {
        Debug.Log("<color=green><b>====== 正在全图扫描寻找病灶，绝对不会改变你的原本材质 ======</b></color>");

        // 1. 揪出“飞升”的老鼠屎 (检查刚体)
        Rigidbody[] rbs = FindObjectsOfType<Rigidbody>(true);
        bool foundPhysicsBug = false;
        foreach (var rb in rbs)
        {
            // 只要没勾选运动学（受重力/弹力影响），就给我全部列出来！
            if (!rb.isKinematic)
            {
                Debug.LogError($"🚨【可疑的物理刚体！】👉 物体: <b>{rb.gameObject.name}</b>受物理引擎控制（未开启 isKinematic）。看看是不是它在乱飞！", rb.gameObject);
                foundPhysicsBug = true;
            }
        }
        if (!foundPhysicsBug) Debug.Log("<color=yellow>没有揪出明显的物理刚体问题，飞升可能来自于动画脚本或父节点。</color>");


        // 2. 揪出“Material 被销毁”的报错元凶
        bool foundTMPBug = false;
        
        // 扫 3D TMP
        TextMeshPro[] tmps3d = FindObjectsOfType<TextMeshPro>(true);
        foreach (var tmp in tmps3d)
        {
            if (tmp.fontSharedMaterial == null)
            {
                Debug.LogError($"🚨【抓到 UI 报错元凶！】👉 3D文字: <b>{tmp.gameObject.name}</b>。<br>它的材质 (Material) 已经丢失或彻底损坏，一旦 UI 刷新它就会触发 Material destroyed 报错！<br>请点击这条报错，重新给它选一下 Font Asset。", tmp.gameObject);
                foundTMPBug = true;
            }
            // 误贴 ProBuilder 材质的情况
            MeshRenderer mr = tmp.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.name.Contains("pb_"))
            {
                Debug.LogError($"🚨【抓到被 ProBuilder 误伤的文字！】👉 3D文字: <b>{tmp.gameObject.name}</b>。<br>你不小心把墙面的 ProBuilder 材质刷到这个文字头上了！", tmp.gameObject);
                foundTMPBug = true;
            }
        }

        // 扫 UI TMP
        TextMeshProUGUI[] tmpsUI = FindObjectsOfType<TextMeshProUGUI>(true);
        foreach (var tmp in tmpsUI)
        {
            if (tmp.fontSharedMaterial == null)
            {
                Debug.LogError($"🚨【抓到 UI 报错元凶！】👉 UI文字: <b>{tmp.gameObject.name}</b> 的材质丢失了！请检查它的 Inspector。", tmp.gameObject);
                foundTMPBug = true;
            }
        }

        if (!foundTMPBug) Debug.Log("<color=yellow>场景中现有的 TMP 文字目前看起来材质均未出现明显丢失。如果运行后依然报 Material 错，说明是在代码中动态被删掉的。</color>");

        Debug.Log("<color=green><b>====== 扫描结束，请检查以上报错（点击控制台红字即可定位场景物体） ======</b></color>");
    }
}
