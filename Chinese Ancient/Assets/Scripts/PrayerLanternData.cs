using System;
using UnityEngine;

/// <summary>
/// 祈福灯数据类
/// 存储单个祈福灯的信息
/// </summary>
[Serializable]
public class PrayerLanternData
{
    public string wishText;           // 祈福内容
    public string playerName;         // 祈福人名称
    public System.DateTime createTime; // 创建时间
    public Vector3 position;          // 位置
    public Color lanternColor;        // 灯笼颜色

    public PrayerLanternData(string wish, string player, Color color)
    {
        wishText = wish;
        playerName = player;
        createTime = System.DateTime.Now;
        lanternColor = color;
        position = Vector3.zero;
    }

    /// <summary>
    /// 获取祈福内容的简短版本（用于显示）
    /// </summary>
    public string GetShortWish(int maxLength = 20)
    {
        if (string.IsNullOrEmpty(wishText)) return "";

        if (wishText.Length <= maxLength)
            return wishText;

        return wishText.Substring(0, maxLength) + "...";
    }

    /// <summary>
    /// 获取时间字符串
    /// </summary>
    public string GetTimeString()
    {
        return createTime.ToString("yyyy-MM-dd HH:mm");
    }
}

/// <summary>
/// 祈福灯数据列表（用于序列化保存）
/// </summary>
[Serializable]
public class PrayerLanternDataList
{
    public PrayerLanternData[] lanterns;

    public PrayerLanternDataList(System.Collections.Generic.List<PrayerLanternData> list)
    {
        lanterns = list.ToArray();
    }
}
