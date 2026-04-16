using UnityEngine;

public class DrawLotsController : MonoBehaviour
{
    [Header("需要显示和隐藏的物品列表")]
    public GameObject[] toggleObjects;

    // 记录当前的显示状态，假设一开始是隐藏的
    private bool isShowing = false;

    private void Start()
    {
        // 游戏开始时，确保所有物品的显示状态与 isShowing 一致 (即隐藏)
        UpdateObjectsState();
    }

    // 将此方法绑定到按钮的 OnClick 事件上
    public void ToggleObjects()
    {
        isShowing = !isShowing; // 切换状态：如果显示状态为 true 就变成 false，反之亦然
        UpdateObjectsState();
    }

    private void UpdateObjectsState()
    {
        foreach (GameObject obj in toggleObjects)
        {
            if (obj != null)
            {
                obj.SetActive(isShowing);
            }
        }
    }
}
