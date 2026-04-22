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

        // 第一步：隐藏火折子模型，假装它进了背包（必须先隐藏，防止后续事件报错打断或者导致物理残留）
        if (hideOnPickup)
        {
            // 不直接使用 SetActive(false)，因为如果后续连接的事件里有需要在该物体上等待的协程，或者别的脚本还需要引用它就会被打断
            // 最佳实践：关闭它的网格渲染和贴图发光，并关闭碰撞体
            Renderer[] renderers = GetComponentsInChildren<Renderer>();
            foreach (Renderer r in renderers)
            {
                r.enabled = false;
            }

            Collider[] colliders = GetComponentsInChildren<Collider>();
            foreach (Collider c in colliders)
            {
                c.enabled = false;
            }
            
            // 额外处理可能挂载的灯光和火星粒子系统特效，防止视觉上还有火光
            Light[] lights = GetComponentsInChildren<Light>();
            foreach (Light l in lights) l.enabled = false;
            
            ParticleSystem[] particles = GetComponentsInChildren<ParticleSystem>();
            foreach (ParticleSystem p in particles) p.Stop();

            // 如果挂了我们之前新加的高亮脚本，也要立刻清理掉
            ItemGlowHighlight glow = GetComponent<ItemGlowHighlight>();
            if (glow != null) glow.DisableGlow();
        }

        // 第二步：触发我们在面板上连好的各种事件（比如大伯的剧情阶段+1）
        if (onPickupEvent != null)
        {
            onPickupEvent.Invoke();
        }
    }
}
