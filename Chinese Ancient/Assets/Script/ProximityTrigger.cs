using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 近距离触发系统 - 当玩家靠近时触发事件
/// 可用于：视频播放、音效播放、特效显示、对话触发等
/// </summary>
public class ProximityTrigger : MonoBehaviour
{
    [Header("玩家设置")]
    [Tooltip("玩家 Transform，不设置则按 Tag 查找")] 
    public Transform player;
    [Tooltip("玩家 Tag（备用自动查找）")] 
    public string playerTag = "Player";

    [Header("触发设置")]
    [Tooltip("触发距离（米）")] 
    public float triggerDistance = 4f;
    [Tooltip("是否只触发一次")]
    public bool triggerOnce = false;

    [Header("事件回调")]
    [Tooltip("进入范围时调用")]
    public UnityEvent onEnterRange;
    [Tooltip("离开范围时调用")]
    public UnityEvent onExitRange;

    private bool isInsideRange = false;
    private bool hasTriggeredOnce = false;

    private void Start()
    {
        // 自动查找玩家
        if (player == null && !string.IsNullOrEmpty(playerTag))
        {
            GameObject found = GameObject.FindGameObjectWithTag(playerTag);
            if (found != null)
            {
                player = found.transform;
            }
        }
    }

    private void Update()
    {
        if (player == null)
            return;

        float distance = Vector3.Distance(transform.position, player.position);
        bool withinRange = distance <= triggerDistance;

        // 进入范围
        if (withinRange && !isInsideRange)
        {
            isInsideRange = true;

            if (!triggerOnce || !hasTriggeredOnce)
            {
                onEnterRange?.Invoke();
                hasTriggeredOnce = true;
                Debug.Log($"<color=green>[近距离触发]</color> 玩家进入范围: {gameObject.name}");
            }
        }
        // 离开范围
        else if (!withinRange && isInsideRange)
        {
            isInsideRange = false;

            if (!triggerOnce || !hasTriggeredOnce)
            {
                onExitRange?.Invoke();
                Debug.Log($"<color=yellow>[近距离触发]</color> 玩家离开范围: {gameObject.name}");
            }
        }
    }

    // 重置触发状态（用于 triggerOnce = true 时）
    public void ResetTrigger()
    {
        hasTriggeredOnce = false;
        isInsideRange = false;
    }

    // 在 Scene 里可视化触发范围
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, triggerDistance);
    }
}
