using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 场景级点击控制器（基于指定的三个槽位）：
//1) 在 Inspector 中指定 Slot1、Slot2 和 SpawnPrefab（Slot3）。
//2) 当用户先点击 Slot1，然后在超时时间内点击 Slot2 时，会在摄像机前生成 SpawnPrefab或激活场景中的对象。
// 将此脚本挂到场景中的一个空对象（例如 GameManager）。
public class LeichaController : MonoBehaviour
{
    [Header("指定三个槽位（在 Inspector 中拖入对应物体）")]
    [Tooltip("先点击的物体（Slot1）")]
    public GameObject slot1;

    [Tooltip("后点击的物体（Slot2）")]
    public GameObject slot2;

    [Tooltip("满足 Slot1->Slot2 顺序后生成或激活的对象（如果这是场景中的对象且初始为隐藏，则会被 SetActive(true)）")]
    public GameObject spawnPrefab;

    [Tooltip("生成物体距离摄像机的距离（仅当 spawnPrefab 为预制体实例化时有效，单位：米）")]
    public float spawnDistance =1.2f;

    [Tooltip("生成物体相对于摄像机的额外偏移（仅当实例化时有效）")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("点击 Slot1 后，在该秒数内点击 Slot2 才有效（秒）")]
    public float selectionTimeout =5f;

    [Tooltip("使用的射线检测层，默认所有层")]
    public LayerMask clickableLayers = ~0;

    bool slot1Selected = false;
    float slot1SelectTime =0f;

    void Update()
    {
        // 超时清理 Slot1 状态
        if (slot1Selected && Time.time - slot1SelectTime > selectionTimeout)
        {
            slot1Selected = false;
            Debug.Log("Slot1 selection timed out.");
        }

        if (Input.GetMouseButtonDown(0))
        {
            Camera cam = Camera.main;
            if (cam == null) return;

            Ray ray = cam.ScreenPointToRay(Input.mousePosition);
            RaycastHit hit;

            if (Physics.Raycast(ray, out hit,100f, clickableLayers))
            {
                GameObject hitObj = hit.collider.gameObject;
                Debug.Log($"Raycast hit: {hitObj.name}");

                // 如果当前没有选中 Slot1，检查是否点中了 slot1 或其子对象
                if (!slot1Selected)
                {
                    if (IsMatch(hitObj, slot1))
                    {
                        slot1Selected = true;
                        slot1SelectTime = Time.time;
                        Debug.Log("Slot1 selected: " + slot1.name);
                    }
                    else
                    {
                        // 点击了其他物体，忽略
                        Debug.Log("Clicked other object while waiting for Slot1: " + hitObj.name);
                    }
                }
                else
                {
                    // 已选中 Slot1，检查是否点击了 Slot2 或其子对象
                    if (IsMatch(hitObj, slot2))
                    {
                        //触发生成或激活
                        Debug.Log("Slot2 clicked after Slot1. Triggering spawn/activate.");
                        SpawnOrActivate();
                        slot1Selected = false;
                    }
                    else
                    {
                        // 点击了非 Slot2，重置选择（或可改为保持 Slot1 状态）
                        slot1Selected = false;
                        Debug.Log("Slot1 selection cancelled by clicking other object: " + hitObj.name);
                    }
                }
            }
            else
            {
                Debug.Log("Raycast did not hit any clickable object.");
            }
        }
    }

    bool IsMatch(GameObject hitObj, GameObject target)
    {
        if (target == null || hitObj == null) return false;
        if (hitObj == target) return true;
        // If hit object is a child of target
        if (hitObj.transform.IsChildOf(target.transform)) return true;
        // If target is a child of hitObj (in case you assigned a child in inspector)
        if (target.transform.IsChildOf(hitObj.transform)) return true;
        return false;
    }

    void SpawnOrActivate()
    {
        if (spawnPrefab == null)
        {
            Debug.LogWarning("spawnPrefab 未设置，无法生成或激活。");
            return;
        }

        Debug.Log($"SpawnOrActivate called. spawnPrefab reference: {spawnPrefab.name}, scene valid: {spawnPrefab.scene.IsValid()}, activeInHierarchy: {spawnPrefab.activeInHierarchy}");

        // 如果 spawnPrefab 指向场景中的对象（已放好位置和角度），则激活它
        if (spawnPrefab.scene.IsValid())
        {
            if (!spawnPrefab.activeInHierarchy)
            {
                spawnPrefab.SetActive(true);
                Debug.Log("Activated scene object: " + spawnPrefab.name);
            }
            else
            {
                Debug.Log("Scene object already active: " + spawnPrefab.name);
            }

            return;
        }

        // 否则假定是预制体资源 -> 在摄像机前实例化
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 spawnPos = cam.transform.position + cam.transform.forward * spawnDistance + cam.transform.TransformDirection(spawnOffset);
        Quaternion spawnRot = Quaternion.LookRotation(cam.transform.forward, Vector3.up);

        Instantiate(spawnPrefab, spawnPos, spawnRot);
        Debug.Log("Instantiated prefab in front of camera: " + spawnPrefab.name);
    }
}
