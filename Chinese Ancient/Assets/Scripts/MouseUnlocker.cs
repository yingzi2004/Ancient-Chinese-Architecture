using UnityEngine;

/// <summary>
/// 鼠标解锁器：在场景加载时解锁鼠标并保持光标始终可见
/// 使用方法：将此脚本挂载到需要鼠标交互的场景中（如2场景的任意物体上）
/// </summary>
public class MouseUnlocker : MonoBehaviour
{
    [Header("设置")]
    [Tooltip("是否在场景加载时自动解锁鼠标")]
    public bool unlockOnStart = true;

    [Tooltip("是否强制保持鼠标可见（防止被其他脚本锁定）")]
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
