using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// 一个极简通用的物品拾取脚本，挂在任何可以通过准星点击的物体上
/// </summary>
public class SimpleItemPickup : MonoBehaviour, IInteractable
{
    [Header("拾取配置")]
    [Tooltip("被拾取后是否立刻在场景中隐藏（或者你可以选择销毁）")]
    public bool hideOnPickup = true;

    [Header("拾取成功后触发的事件（连接任务系统）")]
    [Tooltip("在这里拖入需要通知的NPC或者机关。例如把【点灯大伯】拖进来，调用他的切换剧情方法。")]
    public UnityEvent onPickupEvent;

    // 当玩家的准星对准并按下交互键时，你们的系统会自动调用这个 Interact()
    public void Interact()
    {
        Debug.Log($"[物品拾取] 玩家拾取了: {gameObject.name}");

        // 第一步：触发我们在面板上连好的各种事件（比如大伯的剧情阶段+1）
        if (onPickupEvent != null)
        {
            onPickupEvent.Invoke();
        }

        // 第二步：隐藏火折子模型，假装它进了背包
        if (hideOnPickup)
        {
            gameObject.SetActive(false);
        }
    }
}
