using System.Collections.Generic;
using UnityEngine;

public class DrawPath : MonoBehaviour
{
    [Header("UI & Render")]
    public LineRenderer lineRenderer;
    
    [Header("Objects")]
    public Collider fanweiCollider; // 拖动判定范围（改成3D的Collider）
    public GameObject object1;        // 1物体
    public GameObject objectFanwei;   // fanwei物体
    public GameObject objectDrop1;    // 掉1物体
    public GameObject object2;        // 2物体

    private List<Vector3> points = new List<Vector3>();

    void Start()
    {
        if (lineRenderer == null)
            lineRenderer = GetComponent<LineRenderer>();
        
        // 初始化设置线条端点数目为0
        lineRenderer.positionCount = 0;
        
        // 初始状态下隐藏掉落物体
        if (objectDrop1 != null)
            objectDrop1.SetActive(false);
    }

    void Update()
    {
        // 鼠标按下，开始绘制，清空之前的轨迹
        if (Input.GetMouseButtonDown(0))
        {
            points.Clear();
            lineRenderer.positionCount = 0;
        }
        // 按住鼠标拖动时记录路径并画线
        else if (Input.GetMouseButton(0))
        {
            // 通过射线检测准心（或是鼠标）所指的空间位置
            Ray ray = Camera.main.ScreenPointToRay(new Vector2(Screen.width / 2f, Screen.height / 2f)); // 如果你要鼠标位置，改成 Input.mousePosition
            RaycastHit hit;
            
            // 使用射线碰触到物体的位置。如果在3D中，你需要确保背景或者画布上有碰撞体（比如BoxCollider）
            if (Physics.Raycast(ray, out hit, 100f))
            {
                Vector3 worldPos = hit.point;

                // 为了防止线条与物体表面重叠闪烁(Z-Fighting)，让线条稍微往前一点点
                worldPos -= ray.direction * 0.05f;

                // 距离大于0.05才添加点，避免点太密集
                if (points.Count == 0 || Vector3.Distance(points[points.Count - 1], worldPos) > 0.05f)
                {
                    points.Add(worldPos);
                    lineRenderer.positionCount = points.Count;
                    lineRenderer.SetPosition(points.Count - 1, worldPos);
                }
            }
        }
        // 鼠标抬起时，判断是否成功
        else if (Input.GetMouseButtonUp(0))
        {
            if (CheckDrawSuccess())
            {
                // 成功掩藏 1 和 fanwei
                if (object1 != null) object1.SetActive(false);
                if (objectFanwei != null) objectFanwei.SetActive(false);
                
                // 让"掉1"显示并开始掉落
                if (objectDrop1 != null)
                {
                    objectDrop1.SetActive(true);
                    ObjectManager manager = objectDrop1.GetComponent<ObjectManager>();
                    if (manager != null)
                    {
                        manager.StartFalling();
                    }
                }
            }
            else
            {
                // 失败则清除线条重新画
                points.Clear();
                lineRenderer.positionCount = 0;
            }
        }
    }

    // 路径判定逻辑：检测画的点是否在 fanwei 范围内
    bool CheckDrawSuccess()
    {
        // 如果点太少说明没滑，判断为失败
        if (points.Count < 5) return false;
        
        if (fanweiCollider != null)
        {
            int insideCount = 0;
            foreach (var p in points)
            {
                // 用ClosestPoint判断该点是否在碰撞体内或者极其靠近碰撞体
                Vector3 closest = fanweiCollider.ClosestPoint(p);
                if (Vector3.Distance(closest, p) < 0.2f)
                {
                    insideCount++;
                }
            }
            // 如果绘制的轨迹中，有80%以上的点都在fanwei区域内，可算成功
            float matchRate = (float)insideCount / points.Count;
            return matchRate > 0.8f;
        }

        // 如果没有指定碰撞体，默认就认为成功（方便测试）
        return true; 
    }
}
