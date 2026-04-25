using UnityEngine;

public class Drop2RayClick : MonoBehaviour
{
    public Camera clickCamera;     // 玩家视角摄像机
    public GameObject drop2;       // 掉2 物体
    public GameObject object2;     // 2 物体
    public GameObject object3;     // 3 成品

    void Update()
    {
        // 掉2 没激活，或者没有按下左键，就什么都不做
        if (drop2 == null || !drop2.activeInHierarchy) return;
        if (!Input.GetMouseButtonDown(0)) return;

        if (clickCamera == null)
        {
            clickCamera = Camera.main;
            if (clickCamera == null) return;
        }

        Vector3 screenPos = Input.mousePosition;
        Ray ray = clickCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        int layerMask = ~0;
        layerMask = 1 << drop2.layer; // 只检测与掉2 同一图层

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            Debug.Log("Drop2RayClick: Raycast hit " + hit.collider.name);

            // 命中了掉2
            if (hit.collider != null && (hit.collider.gameObject == drop2 || hit.collider.transform.IsChildOf(drop2.transform)))
            {
                var manager = drop2.GetComponent<ObjectManager>();
                if (manager != null)
                {
                    manager.StartFalling();
                }

                if (object2 != null) object2.SetActive(false);
                if (object3 != null) object3.SetActive(true);
            }
        }
    }
}
