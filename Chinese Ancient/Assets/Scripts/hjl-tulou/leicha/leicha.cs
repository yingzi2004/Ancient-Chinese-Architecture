// AI辅助生成：DeepSeek-R1-0528, 2026-04-23 (优化点：超时取消逻辑位置调整)
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// 控制擂茶交互流程：按顺序点击两个槽位触发生成/激活对象
// 1) 在 Inspector 中指定 Slot1、Slot2、SpawnPrefab
// 2) 用户先点击 Slot1，然后在超时时间内点击 Slot2，会在相机前方生成 SpawnPrefab（或激活场景内对象）
// 建议将此脚本挂载到场景中的一个空对象上（例如 GameManager）
public class LeichaController : MonoBehaviour
{
    [Header("指定对象与位置（在 Inspector 中设置对应对象）")]
    [Tooltip("先点击的物体（Slot1）")]
    public GameObject slot1;

    [Tooltip("后点击的物体（Slot2）")]
    public GameObject slot2;

    [Tooltip("按照 Slot1->Slot2 顺序点击后生成或激活的对象。若它是场景内对象且处于隐藏，会被 SetActive(true)")]
    public GameObject spawnPrefab;

    [Tooltip("生成对象距离相机的距离（当 spawnPrefab 为预制件实例化时生效，单位：米）")]
    public float spawnDistance = 1.2f;

    [Tooltip("生成对象相对于相机的局部偏移（当实例化时生效）")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("点击 Slot1 后，在该时间内点击 Slot2 才有效（秒）")]
    public float selectionTimeout = 5f;

    [Tooltip("射线检测的层（LayerMask），默认所有层")]
    public LayerMask clickableLayers = ~0;

    bool slot1Selected = false;
    float slot1SelectTime =0f;

    void Update()
    {
        // AI辅助生成：DeepSeek-R1-0528, 2026-04-23 (优化点：超时取消逻辑位置调整)
        // 超时取消 Slot1 状态
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

                // 若当前未选中 Slot1：检查是否点击了 slot1 或其子对象
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
                    // 已选中 Slot1：检查是否点击了 Slot2 或其子对象
                    if (IsMatch(hitObj, slot2))
                    {
                        // 触发生成或激活
                        Debug.Log("Slot2 clicked after Slot1. Triggering spawn/activate.");
                        SpawnOrActivate();
                        slot1Selected = false;
                    }
                    else
                    {
                        // 点击了非 Slot2：取消选择（恢复为未选中 Slot1 状态）
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

        // 如果 spawnPrefab 指向场景内对象（已放好位置和角度），则直接激活
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

        // 否则视为预制件资源 -> 在相机前方实例化
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 spawnPos = cam.transform.position + cam.transform.forward * spawnDistance + cam.transform.TransformDirection(spawnOffset);
        Quaternion spawnRot = Quaternion.LookRotation(cam.transform.forward, Vector3.up);

        Instantiate(spawnPrefab, spawnPos, spawnRot);
        Debug.Log("Instantiated prefab in front of camera: " + spawnPrefab.name);
    }
}
