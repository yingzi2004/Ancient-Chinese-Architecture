using UnityEngine;
public class LotusFlower : MonoBehaviour
{
    [Header("角色与NPC设置")]
    public Transform player;
    public Transform playerHand;
    public Transform npc;
    [Header("距离设置")]
    public float pickDistance = 3f;
    public float giveDistance = 4f;
    [Header("完成任务后的对话（必须在这个脚本配好新的对话）")]
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
                PlayerController pc = FindObjectOfType<PlayerController>();
                if (pc != null) player = pc.transform;
                else if (Camera.main != null) player = Camera.main.transform;
            }
        }
    }
    private void Update()
    {
        if (player == null || Camera.main == null) return;
        //还没摘取荷花，此时判断玩家和荷花的距离
        if (!isPicked)
        {
            if (Input.GetMouseButtonDown(0))
            {
                float distToPlayer = Vector3.Distance(transform.position, player.position);
                if (distToPlayer <= pickDistance)
                {
                    // 使用 RaycastAll 以防被玩家自身碰撞体或水面挡住射线，延长射线距离（100米）
                    Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
                    RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
                    bool hitTarget = false;
                    foreach (var hit in hits)
                    {
                        if (hit.transform == transform || hit.transform.IsChildOf(transform))
                        {
                            hitTarget = true;
                            break;
                        }
                    }
                    if (hitTarget)
                    {
                        PickLotus();
                    }
                    else
                    {
                        Debug.LogWarning($"<color=yellow>[交互提示]</color> 距离已满足，但是准心没对准！\n如果已对准但还是不行，请检查【荷花物体】上是否添加了碰撞体组件（如 BoxCollider）！射线必须有Collider才能点到。");
                    }
                }
                else
                {
                    // 这里原本太远时不处理交互，为了让你知道点击生效了，打印一下
                    // Debug.Log($"距离荷花太远，当前: {distToPlayer}");
                }
            }
        }
        else if (isPicked && !isGiven)
        {
            if (npc != null)
            {
                float distToNPC = Vector3.Distance(player.position, npc.position);
                if (distToNPC <= giveDistance)
                {
                    if (Input.GetMouseButtonDown(0))
                    {
                        Ray ray = Camera.main.ScreenPointToRay(new Vector3(Screen.width / 2f, Screen.height / 2f, 0));
                        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);
                        bool hitTarget = false;
                        foreach (var hit in hits)
                        {
                            if (hit.transform == npc || hit.transform.IsChildOf(npc))
                            {
                                hitTarget = true;
                                break;
                            }
                        }
                        if (hitTarget)
                        {
                            GiveLotusToNPC();
                        }
                        else
                        {
                            Debug.LogWarning($"<color=yellow>[交互提示]</color> 距离已满足，但是准心没对准NPC！\n如果已对准但还是不行，请检查【NPC物体】上是否添加了碰撞体组件（如 CapsuleCollider）！");
                        }
                    }
                }
            }
        }
    }
    // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
    private void PickLotus()
    {
        isPicked = true;
        // 挂载到玩家手上
        if (playerHand != null)
        {
            transform.SetParent(playerHand);
            transform.localPosition = Vector3.zero;
            transform.localRotation = Quaternion.identity;
        }
        else
        {
            transform.SetParent(player);
            transform.localPosition = new Vector3(0.5f, -0.5f, 1f);
            transform.localRotation = Quaternion.identity;
        }
        Debug.Log("<color=cyan>[荷花任务]</color> 已摘取荷花！请返回寻找NPC。");
    }
    // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
    private void GiveLotusToNPC()
    {
        isGiven = true;
        Debug.Log("<color=cyan>[荷花任务]</color> 荷花已交付给NPC，触发对话并消失！");
        if (npc != null)
        {
            NPCInteractTrigger trigger1 = npc.GetComponent<NPCInteractTrigger>();
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
            else
            {
                Debug.LogError("<color=red>[荷花任务]</color> 找不到NPC的对话脚本（NPCInteractTrigger）！请检查荷花的 Npc 槽位是否拖错了人！");
            }
        }
        gameObject.SetActive(false);
    }
    private void OnDrawGizmosSelected()
    {
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
