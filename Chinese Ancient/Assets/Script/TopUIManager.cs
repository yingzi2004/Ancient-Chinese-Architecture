using UnityEngine;
using UnityEngine.SceneManagement;
[System.Serializable]
public class UIKeyBinding
{
    public string uiName = "地图（在此绑定预制体）";
    public KeyCode hotKey = KeyCode.M;
    public GameObject uiPanel;
}
public class TopUIManager : MonoBehaviour
{
    [Header("")]
    public UIKeyBinding[] uiBindings = new UIKeyBinding[]
    {
        new UIKeyBinding { uiName = "地图", hotKey = KeyCode.M }
    };
    [Header("")]
    public GameObject[] otherUIToHide;
    private PlayerController playerController;
    void Start()
    {
        // 寻找场景中的玩家控制器，用来控制视角和移动
        playerController = FindObjectOfType<PlayerController>();
        // 游戏开始时，自动将列表里绑定了物体的那些UI给藏起来
        if (uiBindings != null)
        {
            foreach (var binding in uiBindings)
            {
                if (binding.uiPanel != null)
                {
                    binding.uiPanel.SetActive(false);
                }
            }
        }
    }
    void Update()
    {
        if (uiBindings != null)
        {
            foreach (var binding in uiBindings)
            {
                if (Input.GetKeyDown(binding.hotKey))
                {
                    if (binding.uiPanel != null)
                    {
                        // 切换显示状态（关变开，开变关）
                        bool isOpening = !binding.uiPanel.activeSelf;
                        binding.uiPanel.SetActive(isOpening);
                        Debug.Log($"[{binding.uiName}] 状态切换为：{isOpening}");
                        // 处理鼠标显示与隐藏
                        Cursor.visible = isOpening;
                        Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;
                        // 固定玩家视角 & 停止移动
                        if (playerController != null)
                        {
                            // 利用玩家控制器里写好的 isInspecting 来锁死它的操作
                            playerController.isInspecting = isOpening;
                        }
                        // 隐藏/恢复 其他顶部UI元素
                        if (otherUIToHide != null)
                        {
                            foreach (var ui in otherUIToHide)
                            {
                                if (ui != null)
                                {
                                    // 地图打开(isOpening=true)时，其他UI隐藏(!isOpening)
                                    ui.SetActive(!isOpening);
                                }
                            }
                        }
                    }
                }
            }
        }
    }
}
