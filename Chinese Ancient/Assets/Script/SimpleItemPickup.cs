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
        if (hideOnPickup)
        {

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

        if (onPickupEvent != null)
        {
            onPickupEvent.Invoke();
        }
    }
}
