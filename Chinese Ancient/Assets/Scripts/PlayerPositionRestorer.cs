using UnityEngine;

public class PlayerPositionRestorer : MonoBehaviour
{
    [Header("玩家设置")]
    public GameObject player;

    [Header("场景设置")]
    public string cardGameSceneName = "2";

    [Header("返回位置偏移设置")]
    public bool enableReturnOffset = true;

    public Vector3 returnPositionOffset = new Vector3(0f, 0f, -5f);

    public bool useFixedReturnPosition = false;

    public Vector3 fixedReturnPosition = new Vector3(0f, 1f, 0f);

    public Vector3 fixedReturnRotation = new Vector3(0f, 0f, 0f);

    void Start()
    {
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

        string previousScene = PlayerPrefs.GetString("PreviousScene", "");

        // 只有从纸牌游戏返回时才恢复位置
        if (!string.IsNullOrEmpty(previousScene) && previousScene == cardGameSceneName)
        {
            Vector3 finalPosition;
            Quaternion finalRotation;

            // 检查是否使用固定返回位置
            if (useFixedReturnPosition)
            {
                // 使用固定位置
                finalPosition = fixedReturnPosition;
                finalRotation = Quaternion.Euler(fixedReturnRotation);
                Debug.Log($"从纸牌游戏返回，使用固定位置: {finalPosition}");
            }
            else if (PlayerPrefs.HasKey("PlayerPosX"))
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

                finalRotation = Quaternion.Euler(rotX, rotY, rotZ);

                // 应用偏移
                if (enableReturnOffset)
                {
                    // 根据玩家的朝向计算偏移
                    finalPosition = savedPosition + finalRotation * returnPositionOffset;
                    Debug.Log($"从纸牌游戏返回，原始位置: {savedPosition}, 应用偏移: {returnPositionOffset}, 最终位置: {finalPosition}");
                }
                else
                {
                    finalPosition = savedPosition;
                    Debug.Log($"从纸牌游戏返回，恢复玩家位置: {finalPosition}");
                }
            }
            else
            {
                Debug.LogWarning("从纸牌游戏返回，但未找到保存的位置信息！");
                return;
            }

            // 恢复玩家位置和旋转
            player.transform.position = finalPosition;
            player.transform.rotation = finalRotation;

            Debug.Log($"从纸牌游戏返回，最终位置: {finalPosition}, 旋转: {finalRotation.eulerAngles}");

            // 清除保存的位置数据，避免下次启动场景时又恢复
            PlayerPrefs.DeleteKey("PlayerPosX");
            PlayerPrefs.DeleteKey("PlayerPosY");
            PlayerPrefs.DeleteKey("PlayerPosZ");
            PlayerPrefs.DeleteKey("PlayerRotX");
            PlayerPrefs.DeleteKey("PlayerRotY");
            PlayerPrefs.DeleteKey("PlayerRotZ");
            PlayerPrefs.DeleteKey("PreviousScene");
        }
        else
        {
            Debug.Log("首次进入场景，不恢复位置");
        }
    }
}
