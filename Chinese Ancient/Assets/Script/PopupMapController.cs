using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using DG.Tweening;
using System.Collections.Generic;
public class PopupMapController : MonoBehaviour
{
    [System.Serializable]
    public class MapBuilding
    {
        public string buildingName = "新建筑";
        public RectTransform buildingRect;
        public string targetSceneName;
        [Header("黑白差分图(可选)")]
        public Sprite bwSprite;
        // 运行时缓存动态生成的黑白图层的CanvasGroup对象
        [HideInInspector]
        public CanvasGroup runtimeBWGroup;
    }
    [Header("══ 建筑列表 ══")]
    public List<MapBuilding> buildings = new List<MapBuilding>();
    [Header("══ ★ 手动解锁控制阵列 (取代之前的存档) ★ ══")]
    public bool[] unlockedArray = new bool[] { true, false, false, false };
    [Header("══ 呼吸(心脏鼓动)效果设置 ══")]
    public float pulseScale = 1.05f;
    public float pulseDuration = 0.6f;
    [Header("══ 解锁设置 ══")]
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
    // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
    public void RefreshMapVisuals(int overridePulseIndex = -1)
    {
        Debug.Log($"<color=cyan>【场馆地图加载】完全抛弃存档模式！正在读取本场景面板中配置的 unlockedArray 解锁阵列...</color>");
        // 算出数组里哪个是应该心跳鼓动的
        int pulsingIndex = -1;
        if (overridePulseIndex != -1)
        {
            pulsingIndex = overridePulseIndex;
        }
        else
        {
            // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
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
            bool isUnlocked = false;
            if (i < unlockedArray.Length)
            {
                isUnlocked = unlockedArray[i];
            }
            b.buildingRect.DOKill();
            b.buildingRect.localScale = Vector3.one;
            Image img = b.buildingRect.GetComponent<Image>();
            Button btn = b.buildingRect.GetComponent<Button>();
            if (btn == null)
            {
                btn = b.buildingRect.gameObject.AddComponent<Button>();
                btn.transition = Selectable.Transition.None;
            }
            btn.onClick.RemoveAllListeners();
            if (!isUnlocked)
            {
                btn.interactable = false; 
                if (b.runtimeBWGroup != null)
                {
                    b.runtimeBWGroup.alpha = 1f;
                    if (img != null) img.color = new Color(1, 1, 1, 0f);
                }
                else if (img != null)
                {
                    img.color = lockedBuildingColor;
                }
                continue;
            }
            else
            {
                btn.interactable = true; 
                if (b.runtimeBWGroup != null)
                {
                    b.runtimeBWGroup.alpha = 0f;
                }
                if (img != null) img.color = Color.white;
            }
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
        Time.timeScale = 1f;
        Cursor.visible = true;
        Cursor.lockState = CursorLockMode.None;
        StartCoroutine(DeferredLoadScene(sceneName.Trim()));
    }
    private System.Collections.IEnumerator DeferredLoadScene(string targetScene)
    {
        yield return null; // 核心：等这一帧走完，EventSystem 闭环
        SceneManager.LoadScene(targetScene);
    }
    private void EnsureBWOverlay(MapBuilding b)
    {
        if (b.bwSprite == null || b.runtimeBWGroup != null) return;
        GameObject bwObj = new GameObject("BW_Overlay");
        RectTransform rect = bwObj.AddComponent<RectTransform>();
        rect.SetParent(b.buildingRect, false);
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
        rect.localScale = Vector3.one;
        Image bwImg = bwObj.AddComponent<Image>();
        bwImg.sprite = b.bwSprite;
        bwImg.raycastTarget = false; 
        Image parentImg = b.buildingRect.GetComponent<Image>();
        if (parentImg != null)
        {
            bwImg.preserveAspect = parentImg.preserveAspect;
            bwImg.type = parentImg.type;
        }
        b.runtimeBWGroup = bwObj.AddComponent<CanvasGroup>();
        b.runtimeBWGroup.alpha = 1f; 
        b.runtimeBWGroup.interactable = false;
        b.runtimeBWGroup.blocksRaycasts = false;
        if (parentImg != null) parentImg.color = new Color(1, 1, 1, 0f);
    }
}
