using UnityEngine;
using UnityEngine.Events;

public class NPCWalkAfterDialogue : MonoBehaviour
{
    public Animator animator;
    
    public Transform[] waypoints;

    public bool[] stopAtWaypoints;

    public bool[] teleportToNextWaypoint;
    
    public float walkSpeed = 2f;
    
    public float turnSpeed = 5f;

    public string walkAnimBoolParams = "Walk";

    [Header("逐步停点的对话配置")]
    public NPCInteractTrigger interactTrigger;

    public DialogNode[] stopDialogues;

    public UnityEvent onAllWaypointsReached;

    private bool isWalking = false;
    private int currentWaypointIndex = 0;

    // 这个方法将绑定到NPCInteractTrigger的OnDialogueEnd事件上
    public void StartWalkingAway()
    {
        if (waypoints == null || waypoints.Length == 0) return;

        // 如果已经所有的点都走完了，就不再走了
        if (currentWaypointIndex >= waypoints.Length) return;

        // 对话刚结束，此时我们要离开刚才停留的点（currentWaypointIndex - 1），准备前往下一个目的地。
        // 检查离开该点时是否被配置为“消失瞬移”过去？（正合您意：聊完天再消失）
        int fromIndex = currentWaypointIndex - 1;
        if (fromIndex >= 0 && teleportToNextWaypoint != null && fromIndex < teleportToNextWaypoint.Length)
        {
            if (teleportToNextWaypoint[fromIndex])
            {
                transform.position = waypoints[currentWaypointIndex].position;
                transform.rotation = waypoints[currentWaypointIndex].rotation;
            }
        }

        Transform currentTarget = waypoints[currentWaypointIndex];
        if (currentTarget == null) return;

        isWalking = true;
        
        // 播放走路动画（如果刚刚已经瞬移了，就不必播动作了，Update下一帧会平稳接收）
        if (animator != null && !string.IsNullOrEmpty(walkAnimBoolParams))
        {
            bool alreadyThere = Vector3.Distance(transform.position, currentTarget.position) < 0.1f;
            if (!alreadyThere)
            {
                animator.SetBool(walkAnimBoolParams, true);
            }
        }
    }

    private void Update()
    {
        if (!isWalking || waypoints == null || waypoints.Length == 0) return;

        if (currentWaypointIndex >= waypoints.Length) return;

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
            // 记录下刚到达的是几号点
            int reachedIndex = currentWaypointIndex;

            // 判断当前这个点需不需要停下来？
            bool shouldStop = false;
            if (stopAtWaypoints != null && reachedIndex < stopAtWaypoints.Length)
            {
                shouldStop = stopAtWaypoints[reachedIndex];
            }
            
            // 最后一个路点强制使其停下
            if (reachedIndex == waypoints.Length - 1)
            {
                shouldStop = true;
            }

            if (shouldStop)
            {
                // ========== 情况A：停下来说话 ==========
                isWalking = false;
                if (animator != null && !string.IsNullOrEmpty(walkAnimBoolParams))
                {
                    animator.SetBool(walkAnimBoolParams, false);
                }

                // 替换并重置对话，等着玩家来按L跟你聊
                if (interactTrigger != null && stopDialogues != null && reachedIndex < stopDialogues.Length)
                {
                    DialogNode node = stopDialogues[reachedIndex];
                    if (node != null && node.npcLines != null && node.npcLines.Length > 0)
                    {
                        interactTrigger.rootNode = node; 
                        interactTrigger.ResetTrigger();  
                    }
                }

                // 指向下一个路点，原地挂机，不瞬移。
                // 瞬移会被推迟到：玩家【对话结束】触发了 StartWalkingAway() 的时候执行！真正在你眼皮底下消失。
                currentWaypointIndex++;

                if (currentWaypointIndex >= waypoints.Length)
                {
                    onAllWaypointsReached?.Invoke();
                }
            }
            else
            {
                // ========== 情况B：只是一个单纯拐弯点（不对话） ==========
                currentWaypointIndex++; // 直接指向下一个路点
                
                // 如果这是个拐弯点，但紧接着您配置了“从这个点去下个点要瞬移”：
                if (currentWaypointIndex < waypoints.Length && teleportToNextWaypoint != null && reachedIndex < teleportToNextWaypoint.Length)
                {
                    if (teleportToNextWaypoint[reachedIndex])
                    {
                        transform.position = waypoints[currentWaypointIndex].position;
                        transform.rotation = waypoints[currentWaypointIndex].rotation;
                    }
                }
                // 没配瞬移就无缝过度，NPC顺滑地接着往下走路...
            }
        }
    }
}
