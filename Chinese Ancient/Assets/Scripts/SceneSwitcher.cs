using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneSwitcher : MonoBehaviour
{
    [Header("目标场景设置")]
    public string targetSceneName = "2";

    [Header("玩家设置")]
    public GameObject player;

    private bool playerInZone = false;

    void Start()
    {
        // 如果没有设置玩家对象，自动查找
        if (player == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.gameObject;
            }
        }

        // 检查是否有Trigger Collider
        Collider col = GetComponent<Collider>();
        if (col == null)
        {
            Debug.LogWarning("SceneSwitcher: 未找到Collider！请添加Box Collider并勾选Is Trigger。");
        }
        else if (!col.isTrigger)
        {
            Debug.LogWarning("SceneSwitcher: Collider未勾选Is Trigger！请勾选Is Trigger。");
        }
    }

    void Update()
    {
        if (playerInZone)
        {
            return;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            Debug.Log("玩家进入区域，准备跳转...");
            playerInZone = true;
            SwitchScene();
        }
    }

    void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInZone = false;
        }
    }

    bool IsPlayer(Collider col)
    {
        if (col.GetComponent<PlayerController>() != null)
            return true;
        if (col.GetComponentInParent<PlayerController>() != null)
            return true;
        return false;
    }

    void SwitchScene()
    {
        // 保存当前场景名称
        string currentScene = SceneManager.GetActiveScene().name;
        PlayerPrefs.SetString("PreviousScene", currentScene);
        Debug.Log($"保存来源场景: {currentScene}");

        // 保存玩家位置
        if (player != null)
        {
            Vector3 pos = player.transform.position;
            PlayerPrefs.SetFloat("PlayerPosX", pos.x);
            PlayerPrefs.SetFloat("PlayerPosY", pos.y);
            PlayerPrefs.SetFloat("PlayerPosZ", pos.z);

            // 保存玩家旋转
            Vector3 rot = player.transform.rotation.eulerAngles;
            PlayerPrefs.SetFloat("PlayerRotX", rot.x);
            PlayerPrefs.SetFloat("PlayerRotY", rot.y);
            PlayerPrefs.SetFloat("PlayerRotZ", rot.z);

            PlayerPrefs.Save();
            Debug.Log($"保存玩家位置: {pos}");
        }
        else
        {
            Debug.LogWarning("SceneSwitcher: 未找到玩家对象，无法保存位置！");
        }

        // 跳转到目标场景
        Debug.Log($"跳转到场景: {targetSceneName}");
        SceneManager.LoadScene(targetSceneName);
    }
}
