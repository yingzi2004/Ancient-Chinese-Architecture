using UnityEngine;
using UnityEngine.EventSystems;

public class ButtonClickTest : MonoBehaviour, IPointerClickHandler, IPointerDownHandler, IPointerUpHandler
{
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("========== OnPointerClick 被触发！ ==========");
        Debug.Log("点击位置: " + eventData.position);
        Debug.Log("点击对象: " + gameObject.name);
    }

    public void OnPointerDown(PointerEventData eventData)
    {
        Debug.Log("OnPointerDown - 鼠标按下");
    }

    public void OnPointerUp(PointerEventData eventData)
    {
        Debug.Log("OnPointerUp - 鼠标抬起");
    }
}
