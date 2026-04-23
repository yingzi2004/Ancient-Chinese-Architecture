using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;
[RequireComponent(typeof(Rigidbody))]
public class SceneTeleporter : MonoBehaviour
{
    [Header("传送设置")]
    [SerializeField] private string targetSceneName;
    [SerializeField] private bool usePreload = false;
    [Header("常驻标记 UI（World Space Canvas，标识传送点位置）")]
    [SerializeField] private Transform markerRoot;
    [SerializeField] private CanvasGroup markerGroup;
    [SerializeField] private TextMeshProUGUI markerText;
    [SerializeField] private float markerHeightOffset = 2.5f;
    [SerializeField] private float markerFadeSpeed = 4f;
    [Header("确认弹窗 UI")]
    [SerializeField] private CanvasGroup confirmPanel;
    [SerializeField] private TextMeshProUGUI confirmText;
    [Header("提示文案")]
    [SerializeField] private string markerLabel = "传送点";
    [SerializeField] private string confirmMessage = "确认传送到 {0} ？\n\n按 F 确认　　按 Esc 取消";
    [SerializeField] private string loadingMessage = "正在加载...";
    private PlayerController playerController;
    private Transform playerCamera;
    private bool playerInZone = false;
    private bool isConfirming = false;
    private bool isLoading = false;
    private float confirmTargetAlpha = 0f;
    private AsyncOperation asyncLoad = null;
    // ───────────────────── 生命周期 ─────────────────────
    private void Start()
    {
        playerController = FindObjectOfType<PlayerController>();
        playerCamera = Camera.main != null ? Camera.main.transform : null;
        // Rigidbody Kinematic
        Rigidbody rb = GetComponent<Rigidbody>();
        if (rb != null) { rb.isKinematic = true; rb.useGravity = false; }
        // 常驻标记从一开始就显示
        if (markerText != null) markerText.text = markerLabel;
        if (markerGroup != null) { markerGroup.alpha = 1f; markerGroup.gameObject.SetActive(true); }
        // 确认弹窗初始隐藏
        if (confirmPanel != null) { confirmPanel.alpha = 0f; confirmPanel.blocksRaycasts = false; confirmPanel.gameObject.SetActive(true); }
    }
    private void Update()
    {
        if (isConfirming)
        {
            HandleConfirm();
        }
        else
        {
            if (playerInZone && Input.GetKeyDown(KeyCode.F))
                ShowConfirm();
        }
        // 常驻标记：踏入淡出，离开淡入
        float markerTarget = (playerInZone || isConfirming) ? 0f : 1f;
        FadeGroup(markerGroup, markerTarget, markerFadeSpeed);
        // 确认弹窗淡入淡出
        FadeGroup(confirmPanel, confirmTargetAlpha, 6f);
        // 常驻标记 Billboard（面向摄像机）
        UpdateMarkerBillboard();
    }
    // ───────────────────── Billboard ─────────────────────
    private void UpdateMarkerBillboard()
    {
        if (markerRoot == null) return;
        // 固定在传送点上方
        markerRoot.position = transform.position + Vector3.up * markerHeightOffset;
        // 面向摄像机
        if (playerCamera != null)
        {
            Vector3 dir = playerCamera.position - markerRoot.position;
            dir.y = 0f;
            if (dir.sqrMagnitude > 0.001f)
                markerRoot.rotation = Quaternion.LookRotation(dir);
        }
    }
    // ───────────────────── 碰撞触发 ─────────────────────
    private void OnTriggerEnter(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInZone = true;
            // 玩家踏入时开始预加载场景
            if (usePreload)
                PreloadScene();
        }
    }
    private void OnTriggerExit(Collider other)
    {
        if (IsPlayer(other))
        {
            playerInZone = false;
            if (isConfirming) HideConfirm();
        }
    }
    private bool IsPlayer(Collider col)
    {
        return col.GetComponent<PlayerController>() != null ||
               col.GetComponentInParent<PlayerController>() != null;
    }
    // ───────────────────── 确认弹窗 ─────────────────────
    private void ShowConfirm()
    {
        isConfirming = true;
        confirmTargetAlpha = 1f;
        if (confirmText != null)
            confirmText.text = string.Format(confirmMessage, targetSceneName);
    }
    private void HideConfirm()
    {
        isConfirming = false;
        confirmTargetAlpha = 0f;
    }
    private void HandleConfirm()
    {
        if (Input.GetKeyDown(KeyCode.F))
            DoTeleport();
        else if (Input.GetKeyDown(KeyCode.Escape))
            HideConfirm();
    }
    // ───────────────────── 传送 ─────────────────────
    private void PreloadScene()
    {
        if (asyncLoad != null || isLoading) return;
        if (string.IsNullOrEmpty(targetSceneName)) return;
        StartCoroutine(PreloadCoroutine());
    }
    private IEnumerator PreloadCoroutine()
    {
        // 延迟几帧再启动加载，避免进入触发区瞬间卡顿
        yield return null;
        yield return null;
        // 降低后台加载优先级，减少帧率波动
        var oldPriority = Application.backgroundLoadingPriority;
        Application.backgroundLoadingPriority = ThreadPriority.Low;
        asyncLoad = SceneManager.LoadSceneAsync(targetSceneName);
        if (asyncLoad != null)
        {
            asyncLoad.allowSceneActivation = false;
            // 等待加载至 90%（allowSceneActivation=false 时最多到 0.9）
            while (asyncLoad.progress < 0.9f)
                yield return null;
        }
        Application.backgroundLoadingPriority = oldPriority;
    }
    private void DoTeleport()
    {
        if (string.IsNullOrEmpty(targetSceneName))
        {
            Debug.LogWarning("[SceneTeleporter] 未设置目标场景名称！");
            HideConfirm();
            return;
        }
        isLoading = true;
        // 显示加载提示
        if (confirmText != null)
            confirmText.text = loadingMessage;
        if (usePreload && asyncLoad != null)
        {
            // 预加载已完成或进行中，直接激活
            asyncLoad.allowSceneActivation = true;
        }
        else
        {
            // 默认：直接单场景切换，保证光照/后处理按目标场景完整初始化
            SceneManager.LoadScene(targetSceneName, LoadSceneMode.Single);
        }
    }
    // ───────────────────── 工具方法 ─────────────────────
    private void FadeGroup(CanvasGroup group, float target, float speed)
    {
        if (group == null) return;
        group.alpha = Mathf.MoveTowards(group.alpha, target, Time.deltaTime * speed);
        group.blocksRaycasts = group.alpha > 0.01f;
    }
}
