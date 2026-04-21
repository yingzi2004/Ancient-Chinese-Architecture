using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 玩家拾取系统 - 挂载到玩家对象上
/// 管理玩家拾取到的物品
/// </summary>
public class PlayerPickup : MonoBehaviour
{
    public static PlayerPickup Instance { get; private set; }

    [Header("拾取设置")]
    [Tooltip("拾取范围（米）")]
    public float pickupRange = 3f;

    [Tooltip("拾取键位")]
    public KeyCode pickupKey = KeyCode.E;

    [Header("手持物品显示")]
    [Tooltip("手持物品的父物体（手持位置）")]
    public Transform handTransform;

    [Tooltip("手持物品的显示位置偏移")]
    public Vector3 holdPositionOffset = new Vector3(0.5f, 0, 0.5f);

    [Tooltip("手持物品的旋转")]
    public Vector3 holdRotationOffset = new Vector3(0, 90, 0);

    // 当前持有的物品列表
    private List<JadePendant> pickedUpPendants = new List<JadePendant>();

    // 当前手持的玉佩
    private JadePendant currentHeldPendant = null;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    void Update()
    {
        HandleInput();
    }

    /// <summary>
    /// 处理输入
    /// </summary>
    private void HandleInput()
    {
        // 拾取键 - 由玉佩脚本处理，这里主要用于其他拾取逻辑

        // 丢弃键（可选）
        if (Input.GetKeyDown(KeyCode.G) && currentHeldPendant != null)
        {
            DropCurrentPendant();
        }
    }

    /// <summary>
    /// 添加玉佩到背包
    /// </summary>
    public void AddPendantToInventory(JadePendant pendant)
    {
        if (pendant != null && !pickedUpPendants.Contains(pendant))
        {
            pickedUpPendants.Add(pendant);
            Debug.Log($"已将玉佩 {pendant.pendantId} 添加到背包。当前共有 {pickedUpPendants.Count} 个玉佩。");

            // 显示UI提示（可选）
            ShowPickupMessage($"拾取了玉佩！({pickedUpPendants.Count}/1)");
        }
    }

    /// <summary>
    /// 移除玉佩从背包
    /// </summary>
    public void RemovePendantFromInventory(JadePendant pendant)
    {
        if (pickedUpPendants.Contains(pendant))
        {
            pickedUpPendants.Remove(pendant);
            Debug.Log($"从背包中移除了玉佩 {pendant.pendantId}。剩余 {pickedUpPendants.Count} 个玉佩。");
        }
    }

    /// <summary>
    /// 获取当前拾取的玉佩数量
    /// </summary>
    public int GetPendantCount()
    {
        return pickedUpPendants.Count;
    }

    /// <summary>
    /// 检查是否拥有玉佩
    /// </summary>
    public bool HasPendant()
    {
        return pickedUpPendants.Count > 0;
    }

    /// <summary>
    /// 拿出一个玉佩交给NPC
    /// </summary>
    public JadePendant GetPendantForNPC()
    {
        if (pickedUpPendants.Count > 0)
        {
            JadePendant pendant = pickedUpPendants[0];
            pickedUpPendants.RemoveAt(0);
            return pendant;
        }
        return null;
    }

    /// <summary>
    /// 丢弃当前手持的玉佩
    /// </summary>
    private void DropCurrentPendant()
    {
        // 丢弃逻辑（可选实现）
        Debug.Log("丢弃玉佩功能暂未实现");
    }

    /// <summary>
    /// 显示拾取消息
    /// </summary>
    private void ShowPickupMessage(string message)
    {
        Debug.Log(message);
        // 可以在这里添加UI提示
    }

    /// <summary>
    /// 在场景视图中显示拾取范围（编辑器可视化）
    /// </summary>
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
