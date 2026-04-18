using UnityEngine;

public class LanternLighter : MonoBehaviour
{
    [Header("灯笼组配置")]
    [Tooltip("存放所有大院红灯笼的父节点 GameObject")]
    public Transform lanternsParent;

    [Tooltip("灯笼亮起时替换的发光材质 (带有 Emission 属性)")]
    public Material litMaterial;

    [Tooltip("原本的普通材质 (可选，用于开发测试还原用)")]
    public Material unlitMaterial;

    [Header("火折子拾取逻辑")]
    [Tooltip("火折子模型是否在拾取后隐藏或销毁")]
    public bool destroyOnPickup = true;
    
    // 是否已经被点亮，防止重复触发
    private bool isLit = false;

    private void OnTriggerEnter(Collider other)
    {
        // 测试拾取功能：如果碰到的是玩家（确保你的第一人称胶囊体或相机上的 Tag 设为了 "Player"）
        if (!isLit && other.CompareTag("Player"))
        {
            TurnOnAllLanterns();

            // 拾取后发光火折子消失（如果是提交给NPC才发生，将来注释这部分即可）
            if (destroyOnPickup)
            {
                gameObject.SetActive(false); // 或者用 Destroy(gameObject)
            }
        }
    }

    /// <summary>
    /// 被触发或者被 NPC 任务系统调用时，执行全体灯笼材质替换
    /// </summary>
    public void TurnOnAllLanterns()
    {
        if (lanternsParent == null || litMaterial == null) return;

        // 核心：遍历父节点下所有的子灯笼物体
        foreach (Transform lantern in lanternsParent)
        {
            MeshRenderer renderer = lantern.GetComponent<MeshRenderer>();
            
            // 确保找到了 MeshRenderer 组件
            if (renderer != null)
            {
                // 1. 无性能消耗的极致做法：直接把材质换成挂载发光贴图/Emission 的材质
                // 注意：使用 sharedMaterial 而不是 material，这样会复用同一个材质实例，极大地节省开销（Draw Call）
                renderer.sharedMaterial = litMaterial;

                /* 
                 * 【进阶提示 - 局部打光】
                 * 如果你发现满院子灯笼变亮了，但周围的墙壁没有被照亮，
                 * 可以在场景中每隔 2-3 个灯笼的位置，在这个父节点下单独挂几个空的、很小的 Point Light。
                 * 这个脚本会在下面自动找到它们并打开，性能上比每个灯笼都挂光要好几十倍。
                 */
                Light[] partialLights = lantern.GetComponentsInChildren<Light>(true);
                foreach (Light light in partialLights)
                {
                    light.enabled = true;
                }
            }
        }

        Debug.Log("大院掌灯完成：所有灯笼已成功替换为自发光材质！");
        isLit = true; 
    }
}
