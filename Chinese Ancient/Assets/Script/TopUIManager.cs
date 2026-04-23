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
        playerController = FindObjectOfType<PlayerController>();
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
                        bool isOpening = !binding.uiPanel.activeSelf;
                        binding.uiPanel.SetActive(isOpening);
                        Debug.Log($"[{binding.uiName}] 状态切换为：{isOpening}");
                        Cursor.visible = isOpening;
                        Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;
                        if (playerController != null)
                        {
                            playerController.isInspecting = isOpening;
                        }
                        // 隐藏/恢复 其他顶部UI元素
                        if (otherUIToHide != null)
                        {
                            foreach (var ui in otherUIToHide)
                            {
                                if (ui != null)
                                {
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
