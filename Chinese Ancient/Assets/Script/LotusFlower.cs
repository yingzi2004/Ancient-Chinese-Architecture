using UnityEngine;

public class LotusFlower : MonoBehaviour
{
    [Header("角色与NPC设置")]
    [Tooltip("玩家的 Transform，如果不填会自动寻找名为 Player 的物体")]
    public Transform player;
    [Tooltip("玩家的手部节点（绑定在摄像机或角色模型上，用于握住荷花）")]
    public Transform playerHand;
    [Tooltip("目标 NPC 的 Transform")]
    public Transform npc;

    [Header("距离与按键设置")]
    [Tooltip("摘取荷花的判断距离")]
    public float pickDistance = 3f;
    [Tooltip("触发摘取动作的按键")]
    public KeyCode pickKey = KeyCode.R;

    [Tooltip("靠近 NPC 交付的判断距离")]
    public float giveDistance = 4f;
    [Tooltip("交付给 NPC 的按键")]
    public KeyCode giveKey = KeyCode.F;

    [Header("完成任务后的对话（必须在这个脚本配好新的对话）")]
    [Tooltip("当荷花交给NPC时，立刻触发这组新对话，并会永久替换掉NPC默认的旧对话")]
    public DialogNode afterGiveDialogNode;

    // 状态标记
    private bool isPicked = false;
    private bool isGiven = false;

    private void Start()
    {
        if (player == null)
        {
            GameObject pObj = GameObject.FindGameObjectWithTag("Player");
            if (pObj != null) 
            {
                player = pObj.transform;
            }
            else
            {
                // 尝试其他方法寻找玩家
                PlayerController pc = FindObjectOfType<PlayerController>();
                if (pc != null) player = pc.transform;
                else if (Camera.main != null) player = Camera.main.transform;
            }
        }
    }

    private void Update()
    {
        if (player == null) return;

        // 状态 1：还没摘取荷花，此时判断玩家和荷花的距离
        if (!isPicked)
        {
            if (Input.GetKeyDown(pickKey))
            {
                float distToPlayer = Vector3.Distance(transform.position, player.position);
                if (distToPlayer <= pickDistance)
                {
                    PickLotus();
                }
                else
                {
                    Debug.Log($"<color=yellow>[采摘交互]</color> 距离荷花太远啦，当前距离: {distToPlayer:F2}米，要求距离内: {pickDistance}米。由于你乘船位置比较高，可以在荷花物体面板里把 Pick Distance 调大！");
                }
            }
        }
        // 状态 2：已经摘下来在手上了，但是还没给 NPC
        else if (isPicked && !isGiven)
        {
            if (npc != null)
            {
                // 判断玩家和 NPC 的距离
                float distToNPC = Vector3.Distance(player.position, npc.position);
                if (distToNPC <= giveDistance)
                {
                    // 取消原本可能触发对话选项的 F 键冲突：可以在这里进行判断，或者直接按 F 交付
                    if (Input.GetKeyDown(giveKey))
                    {
                        GiveLotusToNPC();
                    }
                }
            }
        }
    }

    private void PickLotus()
    {
        isPicked = true;
        
        // 挂载到玩家手上
        if (playerHand != null)
        {
            transform.SetParent(playerHand);
            // 将荷花的局部坐标和旋转清零，使其准确贴合在“手”的位置
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            // 如果没指定特定的手部节点，就直接挂在玩家身上，并稍微往前偏移
            transform.SetParent(player);
            transform.localPosition = new Vector3(0.5f, -0.5f, 1f); 
            transform.localRotation = Quaternion.identity;
        }

        Debug.Log("<color=cyan>[荷花任务]</color> 已摘取荷花！请返回寻找NPC。");
    }

    private void GiveLotusToNPC()
    {
        isGiven = true;
        
        Debug.Log("<color=cyan>[荷花任务]</color> 荷花已交付给NPC，触发对话并消失！");

        // 触发 NPC 身上的对话代码
        if (npc != null)
        {
            NPCInteractTrigger trigger1 = npc.GetComponent<NPCInteractTrigger>();
            NPCDialogueTrigger trigger2 = npc.GetComponent<NPCDialogueTrigger>();

            if (trigger1 != null)
            {
                Debug.Log("<color=yellow>[荷花任务]</color> 成功获取到 NPCInteractTrigger，准备播放对话...");
                if (afterGiveDialogNode != null && afterGiveDialogNode.npcLines != null && afterGiveDialogNode.npcLines.Length > 0)
                {
                    trigger1.StartSpecificDialogue(afterGiveDialogNode);
                }
                else
                {
                    Debug.LogWarning("<color=red>[荷花任务]</color> 你没有配置荷花的对话，弹默认对话！");
                    trigger1.ManualTrigger();
                }
            }
            else if (trigger2 != null)
            {
                Debug.Log("<color=yellow>[荷花任务]</color> 目标是老版的 NPCDialogueTrigger，正在呼叫它的对话！");
                if (DialogueManager.Instance != null)
                {
                    DialogueManager.Instance.StartAutoDialogue(trigger2.npcName, new string[]{ "谢谢你把摘下的荷花带过来给我！"});
                }
            }
            else
            {
                Debug.LogError("<color=red>[荷花任务]</color> 找不到任何NPC的对话脚本（NPCInteractTrigger 或 NPCDialogueTrigger）！请检查荷花的 Npc 槽位是否拖错了人！");
            }
        }

        // 荷花交付后直接消失
        gameObject.SetActive(false);
    }

    private void OnDrawGizmosSelected()
    {
        // 在编辑器里画两个圈方便你调试距离
        if (!isPicked)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireSphere(transform.position, pickDistance);
        }
        else if (npc != null && !isGiven)
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(npc.position, giveDistance);
        }
    }
}
