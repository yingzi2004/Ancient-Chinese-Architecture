using UnityEngine;
using System.Collections.Generic;

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


    private void HandleInput()
    {
        // 拾取键 - 由玉佩脚本处理，这里主要用于其他拾取逻辑
        if (Input.GetKeyDown(KeyCode.G) && currentHeldPendant != null)
        {
            DropCurrentPendant();
        }
    }


    public void AddPendantToInventory(JadePendant pendant)
    {
        if (pendant != null && !pickedUpPendants.Contains(pendant))
        {
            pickedUpPendants.Add(pendant);
            Debug.Log($"已将玉佩 {pendant.pendantId} 添加到背包。当前共有 {pickedUpPendants.Count} 个玉佩。");

            ShowPickupMessage($"拾取了玉佩！({pickedUpPendants.Count}/1)");
        }
    }


    public void RemovePendantFromInventory(JadePendant pendant)
    {
        if (pickedUpPendants.Contains(pendant))
        {
            pickedUpPendants.Remove(pendant);
            Debug.Log($"从背包中移除了玉佩 {pendant.pendantId}。剩余 {pickedUpPendants.Count} 个玉佩。");
        }
    }


    public int GetPendantCount()
    {
        return pickedUpPendants.Count;
    }

 
    public bool HasPendant()
    {
        return pickedUpPendants.Count > 0;
    }


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


    private void DropCurrentPendant()
    {
        // 丢弃逻辑
        Debug.Log("丢弃玉佩功能暂未实现");
    }


    private void ShowPickupMessage(string message)
    {
        Debug.Log(message);
        // 可以在这里添加UI提示
    }


    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, pickupRange);
    }
}
