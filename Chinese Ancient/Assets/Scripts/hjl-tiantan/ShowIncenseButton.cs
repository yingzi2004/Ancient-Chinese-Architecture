using UnityEngine;

public class ShowIncenseButton : MonoBehaviour
{
    [Header("需要显示的物体")]
    [Tooltip("请将场景中的“香”物体拖拽到这里")]
    public GameObject incenseObject;

    // 按钮点击时调用的方法
    public void ShowIncense()
    {
        if (incenseObject != null)
        {
            // 将物体设置为显示（激活）状态
            incenseObject.SetActive(true);
            Debug.Log("“香”已显示！");
        }
        else
        {
            Debug.LogWarning("未分配需要显示的物体！请在Inspector中将“香”拖拽到Incense Object变量上。");
        }
    }
}
