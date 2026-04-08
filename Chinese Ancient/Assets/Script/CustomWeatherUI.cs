using UnityEngine;
using UnityEngine.UI;
using UniStorm; // 必须引入 UniStorm 才能切换天气

public class CustomWeatherUI : MonoBehaviour
{
    [Header("【自定义面板设置】")]
    public KeyCode toggleKey = KeyCode.T; // 热键 T
    public GameObject weatherPanel;       // 把你做的天气面板拖进来

    private PlayerController playerController;

    void Start()
    {
        // 游戏一开始，隐藏你的天气面板
        if (weatherPanel != null) weatherPanel.SetActive(false);
        playerController = FindObjectOfType<PlayerController>();
    }

    void Update()
    {
        // 监听热键呼出/关闭面板
        if (Input.GetKeyDown(toggleKey))
        {
            ToggleWeatherPanel();
        }
    }

    public void ToggleWeatherPanel()
    {
        if (weatherPanel == null) return;

        bool isOpening = !weatherPanel.activeSelf;
        weatherPanel.SetActive(isOpening);

        // 如果你平时游戏里是锁定隐藏鼠标的，打开面板时记得解锁并显示鼠标
        Cursor.visible = isOpening;
        Cursor.lockState = isOpening ? CursorLockMode.None : CursorLockMode.Locked;

        // 告诉玩家控制器：停下移动和转视角，把鼠标让给UI！
        if (playerController != null)
        {
            playerController.isInspecting = isOpening;
        }
    }

    /// <summary>
    /// 给 Button 点击事件用的方法（根据序号换天气）
    /// 0=晴天，1=雨天... 排序看目标 UniStormSystem 组件里的 All Weather Types
    /// </summary>
    public void ChangeWeatherByIndex(int index)
    {
        if (UniStormSystem.Instance == null)
        {
            Debug.LogError("场景中找不到 UniStormSystem 实例！");
            return;
        }

        var allWeathers = UniStormSystem.Instance.AllWeatherTypes;
        if (index >= 0 && index < allWeathers.Count)
        {
            // 通过获取到的天气类型，让 UniStorm 切换
            WeatherType targetWeather = allWeathers[index];
            UniStormSystem.Instance.ChangeWeather(targetWeather);
            Debug.Log($"【天气系统】成功切换天气至: {targetWeather.WeatherTypeName}");
        }
        else
        {
            Debug.LogWarning("传入的天气序号越界了，请检查按钮绑定的数字！");
        }
    }

    /// <summary>
    /// 让设计师能在 Inspector 面板自由拖拽 WeatherType 来控制按钮
    /// </summary>
    public void ChangeWeatherByType(WeatherType targetWeather)
    {
        if (UniStormSystem.Instance == null)
        {
            Debug.LogError("场景中找不到 UniStormSystem 实例！");
            return;
        }

        if (targetWeather != null)
        {
            UniStormSystem.Instance.ChangeWeather(targetWeather);
            Debug.Log($"【天气系统】成功切换天气至: {targetWeather.WeatherTypeName}");
        }
    }
}
