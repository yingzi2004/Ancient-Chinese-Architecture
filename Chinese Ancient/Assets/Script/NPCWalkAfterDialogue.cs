using UnityEngine;
using UnityEngine.Events;

public class NPCWalkAfterDialogue : MonoBehaviour
{
    [Tooltip("NPC的Animator")]
    public Animator animator;
    
    [Tooltip("指定NPC要顺次走过的目标点数组（按顺序创建空物体填入，最后一个是终点）")]
    public Transform[] waypoints;
    
    [Tooltip("移动速度")]
    public float walkSpeed = 2f;
    
    [Tooltip("转身速度")]
    public float turnSpeed = 5f;

    [Tooltip("控制走路动画的Bool参数名")]
    public string walkAnimBoolParams = "Walk";

    [Header("到达终点后的对话与事件")]
    [Tooltip("是否在到达终点后自动触发一段新对话？")]
    public bool triggerDialogueOnReach = true;

    [Tooltip("NPCInteractTrigger组件（就是NPC身上的那个对话脚本，拖进来）")]
    public NPCInteractTrigger interactTrigger;

    [Tooltip("到达终点后NPC要说的新对话内容")]
    public DialogNode destinationDialogue;

    [Tooltip("到达终点后触发的其他事件（可选）")]
    public UnityEvent onDestinationReached;

    private bool isWalking = false;
    private int currentWaypointIndex = 0;

    // 这个方法将绑定到NPCInteractTrigger的OnDialogueEnd事件上
    public void StartWalkingAway()
    {
        if (waypoints == null || waypoints.Length == 0)
        {
            Debug.LogWarning("未指定目标点数组，无法离开！");
            return;
        }

        Transform finalWaypoint = waypoints[waypoints.Length - 1];
        if (finalWaypoint == null) return;

        // 【关键修复】：如果NPC已经到达或非常接近终点，就不要再执行走路和后续对话了！
        // 这可以防止“对话结束 -> 触发走 -> 发现已到终点 -> 再次触发对话 -> 对话结束...”的无限死循环。
        if (Vector3.Distance(transform.position, finalWaypoint.position) < 0.1f)
        {
            return;
        }

        isWalking = true;
        
        // 播放走路动画
        if (animator != null && !string.IsNullOrEmpty(walkAnimBoolParams))
        {
            animator.SetBool(walkAnimBoolParams, true);
        }
    }

    private void Update()
    {
        if (!isWalking || waypoints == null || waypoints.Length == 0) return;

        Transform currentTarget = waypoints[currentWaypointIndex];
        if (currentTarget == null) return;

        // 1. 移动到当前目标点
        transform.position = Vector3.MoveTowards(transform.position, currentTarget.position, walkSpeed * Time.deltaTime);

        // 2. 平滑转身看向当前目标点
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        direction.y = 0; // 保持身体水平
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }

        // 3. 到达当前目标点后
        if (Vector3.Distance(transform.position, currentTarget.position) < 0.1f)
        {
            // 如果还有下一个路点，就切换到下一个点接着走（实现拐弯）
            if (currentWaypointIndex < waypoints.Length - 1)
            {
                currentWaypointIndex++;
            }
            else
            {
                // 已经是最后一个点（终点），彻底停下
                isWalking = false;
                if (animator != null && !string.IsNullOrEmpty(walkAnimBoolParams))
                {
                    animator.SetBool(walkAnimBoolParams, false);
                }

                // 到达目标点后：重新看向玩家并触发最终对话
                if (triggerDialogueOnReach && interactTrigger != null)
                {
                    interactTrigger.StartSpecificDialogue(destinationDialogue);
                }

                onDestinationReached?.Invoke();
            }
        }
    }
}
