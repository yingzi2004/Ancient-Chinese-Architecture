using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// 触发器诊断脚本
/// 用于快速检测为什么触发器不工作
/// </summary>
public class TriggerDiagnostics : MonoBehaviour
{
    [Header("诊断信息")]
    [SerializeField] private string objectName;

    void Start()
    {
        objectName = gameObject.name;
        Debug.Log($"========== 开始诊断 {objectName} ==========");

        // 检查1：是否有Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogError($"❌ {objectName}: 没有Collider组件！请添加Box Collider并勾选Is Trigger。");
            return;
        }
        else
        {
            Debug.Log($"✅ {objectName}: 有Collider组件");
        }

        // 检查2：Collider是否是Trigger
        if (!col.isTrigger)
        {
            Debug.LogError($"❌ {objectName}: Collider的Is Trigger未勾选！");
        }
        else
        {
            Debug.Log($"✅ {objectName}: Is Trigger已勾选");
        }

        // 检查3：Box Collider大小
        if (col is BoxCollider box)
        {
            Debug.Log($"✅ {objectName}: Box Collider Size = {box.size}, Center = {box.center}");
        }

        // 检查4：是否有触发脚本
        var triggerScript = GetComponent<LocationDialogueTrigger_Auto>();
        if (triggerScript == null)
        {
            Debug.LogError($"❌ {objectName}: 没有LocationDialogueTrigger_Auto脚本！");
        }
        else
        {
            Debug.Log($"✅ {objectName}: 有LocationDialogueTrigger_Auto脚本");
        }

        // 检查5：查找DialogueManager
        DialogueManager dm = FindObjectOfType<DialogueManager>();
        if (dm == null)
        {
            Debug.LogWarning($"⚠️ {objectName}: 场景中没有DialogueManager！");
        }
        else
        {
            Debug.Log($"✅ 找到DialogueManager");

            // 检查UI引用
            if (dm.dialoguePanel == null)
                Debug.LogError($"❌ DialogueManager: dialoguePanel为空！");
            else
                Debug.Log($"✅ DialogueManager: dialoguePanel已设置");

            if (dm.dialogueText == null)
                Debug.LogError($"❌ DialogueManager: dialogueText为空！");
            else
                Debug.Log($"✅ DialogueManager: dialogueText已设置");

            if (dm.npcNameText == null)
                Debug.LogError($"❌ DialogueManager: npcNameText为空！");
            else
                Debug.Log($"✅ DialogueManager: npcNameText已设置");
        }

        // 检查6：查找玩家
        GameObject player = GameObject.FindWithTag("Player");
        if (player == null)
        {
            Debug.LogError($"❌ 场景中没有Tag为'Player'的物体！");
        }
        else
        {
            Debug.Log($"✅ 找到玩家: {player.name}");

            // 检查玩家是否有Collider
            Collider playerCol = player.GetComponent<Collider>();
            if (playerCol == null)
            {
                Debug.LogWarning($"⚠️ 玩家没有Collider，可能无法触发！");
            }

            // 检查玩家是否有Rigidbody
            Rigidbody rb = player.GetComponent<Rigidbody>();
            if (rb == null)
            {
                Debug.LogWarning($"⚠️ 玩家没有Rigidbody，可能无法触发Trigger！");
            }
        }

        Debug.Log($"========== 诊断完成 {objectName} ==========");
        Debug.Log($"如果所有检查都通过但还是不工作，请检查：");
        Debug.Log($"1. 触发器的位置是否在玩家移动路径上");
        Debug.Log($"2. 触发器的大小是否足够大（建议Size: 5,3,5）");
        Debug.Log($"3. 运行游戏时，玩家是否真的进入了触发区域");
    }

    void OnDrawGizmos()
    {
        // 绘制触发区域
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null && col.isTrigger)
        {
            Gizmos.color = Color.yellow;
            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.DrawWireCube(col.center, col.size);

            // 绘制中心点
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(transform.position + col.center, 0.2f);
        }
    }

    [ContextMenu("重新诊断")]
    public void DiagnoseAgain()
    {
        Start();
    }
}
