using UnityEngine;

// 挂在任意物体上，通过“准心 + 摄像机中心射线”点击 掉2
public class Drop2RayClick : MonoBehaviour
{
    public Camera clickCamera;     // 玩家视角摄像机（和 DrawPath 里的一样）
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

        // 因为改为了按F定点操作，现在用鼠标点击而不是准心
        Vector3 screenPos = Input.mousePosition;
        Ray ray = clickCamera.ScreenPointToRay(screenPos);
        RaycastHit hit;

        int layerMask = ~0;
        layerMask = 1 << drop2.layer; // 只检测与掉2 同一图层

        if (Physics.Raycast(ray, out hit, 100f, layerMask))
        {
            Debug.Log("Drop2RayClick: Raycast hit " + hit.collider.name);

            // 命中了掉2（或其子物体）
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
