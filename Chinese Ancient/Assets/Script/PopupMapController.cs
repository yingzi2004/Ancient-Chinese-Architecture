using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections.Generic;

/// <summary>
/// 快捷键调出的弹窗地图控制器 - 支持进度解锁、点击跳转与心脏鼓动呼吸效果
/// </summary>
public class PopupMapController : MonoBehaviour
{
    [System.Serializable]
    public class MapBuilding
    {
        public string buildingName = "新建筑";
        public RectTransform buildingRect;
        public string targetSceneName;

        [Header("黑白差分图(可选)")]
        [Tooltip("【如果有黑白图，拖到这里。未解锁时会显示它】")]
        public Sprite bwSprite;

        // 运行时缓存动态生成的黑白图层的CanvasGroup对象
        [HideInInspector]
        public CanvasGroup runtimeBWGroup;
    }

    [Header("══ 建筑列表 ══")]
    public List<MapBuilding> buildings = new List<MapBuilding>();

    [Header("══ ★ 手动解锁控制阵列 (取代之前的存档) ★ ══")]
    [Tooltip("这几个勾选框代表对应的建筑是否亮起。0=土楼, 1=苏州, 2=京派, 3=晋商。\n你可以在不同场馆单独修改这个预制体，打勾谁谁就亮！\n当玩家碰到接触体时，代码也会自动给这里打上勾~")]
    public bool[] unlockedArray = new bool[] { true, false, false, false };

    [Header("══ 呼吸(心脏鼓动)效果设置 ══")]
    [Tooltip("放大的最大倍数（1.05表示放大5%）")]
    public float pulseScale = 1.05f;
    [Tooltip("鼓动一次的速度（秒）")]
    public float pulseDuration = 0.6f;

    [Header("══ 解锁设置 ══")]
    [Tooltip("未解锁建筑变灰的颜色（可点击自由调整）")]
    public Color lockedBuildingColor = new Color(0.6f, 0.6f, 0.6f, 1f);

    [ContextMenu("★★★一键清空记录 (测试用)★★★")]
    public void ForceResetSave()
    {
        for (int i = 1; i < unlockedArray.Length; i++)
        {
            unlockedArray[i] = false;
        }
        unlockedArray[0] = true; // 永远保留土楼

        Debug.Log("【存档已重置】已清除掉所有打勾状态，只保留了第0项（土楼）！");
    }

    private void Update()
    {
        // 增加一个无敌的测试快捷键：在游戏里直接按 F12 键清档
        if (Input.GetKeyDown(KeyCode.F12))
        {
            ForceResetSave();
            Debug.LogWarning("<color=red>【强制清档】你按下了 F12 键！所有通关记录已被重置为主面板设置，重新按 M 查看。</color>");
        }
    }

    private void OnEnable()
    {
        RefreshMapVisuals();
    }

    /// <summary>
    /// 手动刷新地图表现，供别的脚本修改 unlockedArray 后立刻调用！
    /// </summary>
    public void RefreshMapVisuals(int overridePulseIndex = -1)
    {
        Debug.Log($"<color=cyan>【场馆地图加载】完全抛弃存档模式！正在读取本场景面板中配置的 unlockedArray 解锁阵列...</color>");

        // 算出数组里哪个是应该心跳鼓动的
        int pulsingIndex = -1;
        
        // 如果有外部强制指定（比如刚碰了接触体，那被碰的那个必须跳！）
        if (overridePulseIndex != -1)
        {
            pulsingIndex = overridePulseIndex;
        }
        else
        {
            // 如果没有人指定，那就默认找数字最大的那一个打勾的作为当前进度让他跳
            for (int j = 0; j < unlockedArray.Length; j++)
            {
                if (unlockedArray[j]) pulsingIndex = j;
            }
        }

        for (int i = 0; i < buildings.Count; i++)
        {
            var b = buildings[i];
            if (b.buildingRect == null) continue;

            EnsureBWOverlay(b);

            // 直接按我们面板里的数组打勾情况做主！
            bool isUnlocked = false;
            if (i < unlockedArray.Length)
            {
                isUnlocked = unlockedArray[i];
            }

            // 清理残留动画
            b.buildingRect.DOKill(); 
            b.buildingRect.localScale = Vector3.one;

            Image img = b.buildingRect.GetComponent<Image>();
            
            // 提前获取或加上外挂 Button
            Button btn = b.buildingRect.GetComponent<Button>();
            if (btn == null)
            {
                btn = b.buildingRect.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None; 
            }
            // 清理所有旧事件，防止重复触发
            btn.onClick.RemoveAllListeners();

            if (!isUnlocked)
            {
                // 【未解锁状态】
                btn.interactable = false; // ★核心修复：强行锁死未解锁的建筑，绝对不允许点击跳转！

                if (b.runtimeBWGroup != null)
                {
                    // 有真实黑白切图时，直接显示黑白层，底图隐身
                    b.runtimeBWGroup.alpha = 1f;
                    if (img != null) img.color = new Color(1, 1, 1, 0f);
                }
                else if (img != null)
                {
                    // 未解锁且没配图：用规定的灰色染色
                    img.color = lockedBuildingColor; 
                }
                continue; 
            }
            else
            {
                // 【已解锁状态】
                btn.interactable = true; // ★允许点击跳转
                
                if (b.runtimeBWGroup != null)
                {
                    // 隐藏黑白层
                    b.runtimeBWGroup.alpha = 0f;
                }
                // 已解锁：恢复底图正常颜色
                if (img != null) img.color = Color.white;
            }

            // --- 只有当前最新解锁的那一个建筑（或者是全部通关时的最后一个），才会展现心脏鼓动效果！ ---
            if (i == pulsingIndex)
            {
                b.buildingRect.DOScale(Vector3.one * pulseScale, pulseDuration)
                    .SetEase(Ease.InOutSine)
                    .SetLoops(-1, LoopType.Yoyo) 
                    .SetUpdate(true); 
            }

            // 绑定点击事件
            string sceneToLoad = b.targetSceneName; 
            btn.onClick.AddListener(() => 
            {
                OnBuildingClicked(sceneToLoad);
            });
        }
    }

    private void OnDisable()
    {
        foreach (var b in buildings)
        {
            if (b.buildingRect != null)
            {
                b.buildingRect.DOKill();
                b.buildingRect.localScale = Vector3.one;
            }
        }
    }

    private void OnBuildingClicked(string sceneName)
    {
        if (string.IsNullOrWhiteSpace(sceneName)) return;
        Debug.Log($"【地图】正在前往场景: {sceneName}");
        SceneManager.LoadScene(sceneName.Trim());
    }

    /// <summary>
    /// 动态生成真实的黑白图片涂层
    /// </summary>
    private void EnsureBWOverlay(MapBuilding b)
    {
        if (b.bwSprite == null || b.runtimeBWGroup != null) return;

        GameObject bwObj = new GameObject("BW_Overlay");
        RectTransform rect = bwObj.AddComponent<RectTransform>();
        rect.SetParent(b.buildingRect, false);
        
        // 自动铺满父物体的全部空间
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;

        Image bwImg = bwObj.AddComponent<Image>();
        bwImg.sprite = b.bwSprite;
        bwImg.raycastTarget = false; // 不要拦截鼠标点击！

        Image parentImg = b.buildingRect.GetComponent<Image>();
        if (parentImg != null)
        {
            bwImg.preserveAspect = parentImg.preserveAspect;
            bwImg.type = parentImg.type;
        }

        b.runtimeBWGroup = bwObj.AddComponent<CanvasGroup>();
        b.runtimeBWGroup.alpha = 1f; // ★ 初始化时，我们强行让黑白图层立刻变得可见
        b.runtimeBWGroup.interactable = false;
        b.runtimeBWGroup.blocksRaycasts = false;
        
        // ★ 初始化时，立刻把底图的颜色抽空，防止它抢在 OnEnable 判断前闪现一瞬间的彩色
        if (parentImg != null) parentImg.color = new Color(1, 1, 1, 0f);
    }
}