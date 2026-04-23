using UnityEngine;
using UnityEngine.Events;

public class SimpleItemPickup : MonoBehaviour, IInteractable
{
    [Header("拾取配置")]
    public bool hideOnPickup = true;

    [Header("拾取成功后触发的事件（连接任务系统）")]
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