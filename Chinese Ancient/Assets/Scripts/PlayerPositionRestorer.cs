using UnityEngine;

/// <summary>
/// 玩家位置恢复器：从纸牌游戏返回时恢复玩家的位置和旋转
/// 使用方法：将此脚本挂载到场景中的任意GameObject上（建议挂载到玩家或场景管理器）
/// </summary>
public class PlayerPositionRestorer : MonoBehaviour
{
    [Header("玩家设置")]
    [Tooltip("玩家对象（如果为空则自动查找PlayerController）")]
    public GameObject player;

    [Header("场景设置")]
    [Tooltip("纸牌游戏场景名称，只有从这个场景返回时才恢复位置")]
    public string cardGameSceneName = "2";

    void Start()
    {
        // 查找玩家对象
        if (player == null)
        {
            PlayerController playerController = FindObjectOfType<PlayerController>();
            if (playerController != null)
            {
                player = playerController.gameObject;
            }
        }

        if (player == null)
        {
            Debug.LogWarning("PlayerPositionRestorer: 未找到玩家对象！");
            return;
        }

        // 检查是否从纸牌游戏场景返回
        string previousScene = PlayerPrefs.GetString("PreviousScene", "");

        // 只有从纸牌游戏返回时才恢复位置
        if (!string.IsNullOrEmpty(previousScene) && previousScene == cardGameSceneName)
        {
            // 检查是否有保存的位置信息
            if (PlayerPrefs.HasKey("PlayerPosX"))
            {
                // 读取保存的位置
                float posX = PlayerPrefs.GetFloat("PlayerPosX");
                float posY = PlayerPrefs.GetFloat("PlayerPosY");
                float posZ = PlayerPrefs.GetFloat("PlayerPosZ");

                Vector3 savedPosition = new Vector3(posX, posY, posZ);

                // 读取保存的旋转
                float rotX = PlayerPrefs.GetFloat("PlayerRotX");
                float rotY = PlayerPrefs.GetFloat("PlayerRotY");
                float rotZ = PlayerPrefs.GetFloat("PlayerRotZ");

                Quaternion savedRotation = Quaternion.Euler(rotX, rotY, rotZ);

                // 恢复玩家位置和旋转
                player.transform.position = savedPosition;
                player.transform.rotation = savedRotation;

                Debug.Log($"从纸牌游戏返回，恢复玩家位置: {savedPosition}, 旋转: {savedRotation.eulerAngles}");

                // 清除保存的位置数据，避免下次启动场景时又恢复
                PlayerPrefs.DeleteKey("PlayerPosX");
                PlayerPrefs.DeleteKey("PlayerPosY");
                PlayerPrefs.DeleteKey("PlayerPosZ");
                PlayerPrefs.DeleteKey("PlayerRotX");
                PlayerPrefs.DeleteKey("PlayerRotY");
                PlayerPrefs.DeleteKey("PlayerRotZ");
                PlayerPrefs.DeleteKey("PreviousScene");
            }
        }
        else
        {
            Debug.Log("首次进入场景，不恢复位置");
        }
    }
}
