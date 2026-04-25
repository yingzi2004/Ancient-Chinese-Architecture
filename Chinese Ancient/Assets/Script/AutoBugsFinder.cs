using UnityEngine;
using TMPro;

public class AutoBugsFinder : MonoBehaviour
{
    void Start()
    {
        Debug.Log("<color=green><b>====== 侦探已上线！正在全图扫描寻找病灶 ======</b></color>");

        Rigidbody[] rbs = FindObjectsOfType<Rigidbody>(true);
        bool foundPhysicsBug = false;
        foreach (var rb in rbs)
        {
            if (!rb.isKinematic)
            {
                Debug.LogError($"🚨【可疑的物理刚体！】👉 物体: <b>{rb.gameObject.name}</b>。它受物理引擎控制（未开启 isKinematic），极有可能就是乱飞的元凶！", rb.gameObject);
                foundPhysicsBug = true;
            }
        }
        
        if (!foundPhysicsBug) Debug.Log("<color=yellow>没有揪出明显的物理刚体问题，飞升可能来自于动画脚本或父节点。</color>");

        bool foundTMPBug = false;
        TextMeshPro[] tmps3d = FindObjectsOfType<TextMeshPro>(true);
        foreach (var tmp in tmps3d)
        {
            if (tmp.fontSharedMaterial == null)
            {
                Debug.LogError($"🚨【抓到 UI 报错元凶！】👉 3D文字: <b>{tmp.gameObject.name}</b>。它的材质丢失了！请重新给它选一下 Font Asset。", tmp.gameObject);
                foundTMPBug = true;
            }
            MeshRenderer mr = tmp.GetComponent<MeshRenderer>();
            if (mr != null && mr.sharedMaterial != null && mr.sharedMaterial.name.Contains("pb_"))
            {
                Debug.LogError($"🚨【抓到被 ProBuilder 误伤的文字！】👉 3D文字: <b>{tmp.gameObject.name}</b>。你不小心把墙面的材质刷到它头上了！", tmp.gameObject);
                foundTMPBug = true;
            }
        }

        if (!foundTMPBug) Debug.Log("<color=yellow>目前文字材质正常，说明是被代码删掉引起的。</color>");

        Debug.Log("<color=green><b>====== 扫描结束，请检查以上红字报错 ======</b></color>");
    }
}
