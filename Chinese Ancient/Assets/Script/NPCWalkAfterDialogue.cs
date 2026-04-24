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
    public void StartWalkingAway()
    {
        if (waypoints == null || waypoints.Length == 0) return;
        if (currentWaypointIndex >= waypoints.Length) return;
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
        transform.position = Vector3.MoveTowards(transform.position, currentTarget.position, walkSpeed * Time.deltaTime);
        Vector3 direction = (currentTarget.position - transform.position).normalized;
        direction.y = 0; 
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, turnSpeed * Time.deltaTime);
        }
        if (Vector3.Distance(transform.position, currentTarget.position) < 0.1f)
        {
            int reachedIndex = currentWaypointIndex;
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
                isWalking = false;
                if (animator != null && !string.IsNullOrEmpty(walkAnimBoolParams))
                {
                    animator.SetBool(walkAnimBoolParams, false);
                }
                // 替换并重置对话，按L聊
                if (interactTrigger != null && stopDialogues != null && reachedIndex < stopDialogues.Length)
                {
                    DialogNode node = stopDialogues[reachedIndex];
                    if (node != null && node.npcLines != null && node.npcLines.Length > 0)
                    {
                        interactTrigger.rootNode = node;
                        interactTrigger.ResetTrigger();
                    }
                }
                currentWaypointIndex++;
                if (currentWaypointIndex >= waypoints.Length)
                {
                    onAllWaypointsReached?.Invoke();
                }
            }
            else
            {
                currentWaypointIndex++; 

                if (currentWaypointIndex < waypoints.Length && teleportToNextWaypoint != null && reachedIndex < teleportToNextWaypoint.Length)
                {
                    if (teleportToNextWaypoint[reachedIndex])
                    {
                        transform.position = waypoints[currentWaypointIndex].position;
                        transform.rotation = waypoints[currentWaypointIndex].rotation;
                    }
                }
            }
        }
    }
}
