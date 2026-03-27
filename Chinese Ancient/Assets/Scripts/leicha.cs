using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// ���������������������ָ����������λ����
//1) �� Inspector ��ָ�� Slot1��Slot2 �� SpawnPrefab��Slot3����
//2) ���û��ȵ�� Slot1��Ȼ���ڳ�ʱʱ���ڵ�� Slot2 ʱ�����������ǰ���� SpawnPrefab�򼤻���еĶ���
// ���˽ű��ҵ������е�һ���ն������� GameManager����
public class LeichaController : MonoBehaviour
{
    [Header("ָ��������λ���� Inspector �������Ӧ���壩")]
    [Tooltip("�ȵ�������壨Slot1��")]
    public GameObject slot1;

    [Tooltip("���������壨Slot2��")]
    public GameObject slot2;

    [Tooltip("���� Slot1->Slot2 ˳������ɻ򼤻�Ķ���������ǳ����еĶ����ҳ�ʼΪ���أ���ᱻ SetActive(true)��")]
    public GameObject spawnPrefab;

    [Tooltip("�����������������ľ��루���� spawnPrefab ΪԤ����ʵ����ʱ��Ч����λ���ף�")]
    public float spawnDistance =1.2f;

    [Tooltip("�������������������Ķ���ƫ�ƣ�����ʵ����ʱ��Ч��")]
    public Vector3 spawnOffset = Vector3.zero;

    [Tooltip("��� Slot1 ���ڸ������ڵ�� Slot2 ����Ч���룩")]
    public float selectionTimeout =5f;

    [Tooltip("ʹ�õ����߼��㣬Ĭ�����в�")]
    public LayerMask clickableLayers = ~0;

    bool slot1Selected = false;
    float slot1SelectTime =0f;

    void Update()
    {
        // ��ʱ���� Slot1 ״̬
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

                // �����ǰû��ѡ�� Slot1������Ƿ������ slot1 �����Ӷ���
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
                        // ������������壬����
                        Debug.Log("Clicked other object while waiting for Slot1: " + hitObj.name);
                    }
                }
                else
                {
                    // ��ѡ�� Slot1������Ƿ����� Slot2 �����Ӷ���
                    if (IsMatch(hitObj, slot2))
                    {
                        //�������ɻ򼤻�
                        Debug.Log("Slot2 clicked after Slot1. Triggering spawn/activate.");
                        SpawnOrActivate();
                        slot1Selected = false;
                    }
                    else
                    {
                        // ����˷� Slot2������ѡ�񣨻�ɸ�Ϊ���� Slot1 ״̬��
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
            Debug.LogWarning("spawnPrefab δ���ã��޷����ɻ򼤻");
            return;
        }

        Debug.Log($"SpawnOrActivate called. spawnPrefab reference: {spawnPrefab.name}, scene valid: {spawnPrefab.scene.IsValid()}, activeInHierarchy: {spawnPrefab.activeInHierarchy}");

        // ��� spawnPrefab ָ�򳡾��еĶ����ѷź�λ�úͽǶȣ����򼤻���
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

        // ����ٶ���Ԥ������Դ -> �������ǰʵ����
        Camera cam = Camera.main;
        if (cam == null) return;

        Vector3 spawnPos = cam.transform.position + cam.transform.forward * spawnDistance + cam.transform.TransformDirection(spawnOffset);
        Quaternion spawnRot = Quaternion.LookRotation(cam.transform.forward, Vector3.up);

        Instantiate(spawnPrefab, spawnPos, spawnRot);
        Debug.Log("Instantiated prefab in front of camera: " + spawnPrefab.name);
    }
}
