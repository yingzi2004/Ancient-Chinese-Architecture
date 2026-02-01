//using System.Diagnostics;
using UnityEngine;
using UnityEngine.Video;

[RequireComponent(typeof(VideoPlayer))]
public class WallVideoTrigger : MonoBehaviour
{
    private VideoPlayer videoPlayer;
    public string playerTag = "Player";

    void Start()
    {
        // 自动获取视频组件
        videoPlayer = GetComponent<VideoPlayer>();
        // 初始状态停止播放
        if (videoPlayer != null)
            videoPlayer.Stop();
    }

    // 进入触发区域时播放
    void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (videoPlayer != null && !videoPlayer.isPlaying)
            {
                videoPlayer.Play();
                Debug.Log("靠近墙面，视频开始播放");
            }
        }
    }

    // 离开触发区域时暂停/停止
    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            if (videoPlayer != null && videoPlayer.isPlaying)
            {
                // 这里用Stop()会回到开头，用Pause()会暂停在当前帧
                videoPlayer.Stop();
                Debug.Log("离开墙面，视频停止播放");
            }
        }
    }
}