using UnityEngine;

public class MouseUnlocker : MonoBehaviour
{
    [Header("设置")]
    public bool unlockOnStart = true;

    public bool forceKeepVisible = true;

    void Start()
    {
        if (unlockOnStart)
        {
            UnlockMouse();
        }
    }

    void Update()
    {
        // 强制保持鼠标解锁和可见状态
        if (forceKeepVisible)
        {
            EnsureMouseUnlocked();
        }
    }

    void LateUpdate()
    {
        // 在LateUpdate中再次确保，防止其他脚本在Update中锁定鼠标
        if (forceKeepVisible)
        {
            EnsureMouseUnlocked();
        }
    }

    void EnsureMouseUnlocked()
    {
        // 强制保持鼠标解锁状态
        if (Cursor.lockState != CursorLockMode.None)
        {
            Cursor.lockState = CursorLockMode.None;
        }

        // 强制保持鼠标可见
        if (!Cursor.visible)
        {
            Cursor.visible = true;
        }
    }

    public void UnlockMouse()
    {
        // 解锁并显示鼠标
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        Debug.Log("鼠标已解锁并显示");
    }
}
