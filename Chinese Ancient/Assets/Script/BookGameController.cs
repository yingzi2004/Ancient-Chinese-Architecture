using System.Collections;
using UnityEngine;
using TMPro;
public class BookGameController : MonoBehaviour
{
    [Header("Book")]
    [SerializeField] private BookPro book;
    [Header("阅读位")]
    [SerializeField] private Transform readingAnchor;
    [SerializeField] private bool useAutoReadingSpot = true;
    [SerializeField] private float autoReadDistance = 1.2f;
    [SerializeField] private float autoReadHeightOffset = 0f;
    [SerializeField] private float activateDistance = 2.5f;
    [Header("提示文本")]
    [SerializeField] private CanvasGroup hintGroup;
    [SerializeField] private float hintFadeSpeed = 6f;
    [Header("阅读中按键提示布局")]
    [SerializeField] private TextMeshProUGUI leftKeyHintText;
    [SerializeField] private TextMeshProUGUI rightKeyHintText;
    [SerializeField] private TextMeshProUGUI exitKeyHintText;
    [SerializeField] private string nearHint = "按 F 开始阅读";
    [SerializeField] private string leftReadHint = "A 上一页";
    [SerializeField] private string rightReadHint = "D 下一页";
    [SerializeField] private string exitReadHint = "F 退出阅读";
    [SerializeField] private string firstPageHint = "已经是第一页";
    [SerializeField] private string lastPageHint = "已经是最后一页";
    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pageFlipClip;
    [Header("Settings")]
    [SerializeField] private float flipDuration = 0.5f;
    private PlayerController playerController;
    private Transform playerTransform;
    private Transform playerCamera;
    private bool isFlipping = false;
    private bool isReading = false;
    private bool hintVisible = false;
    private float tempHintEndTime = -1f;
    private string tempReadingCenterHint;
    private Vector3 savedPlayerPosition;
    private Quaternion savedPlayerRotation;
    private Quaternion savedCameraLocalRotation;
    private void Awake()
    {
        if (!book)
            book = GetComponentInChildren<BookPro>(true);
        playerController = FindFirstObjectByType<PlayerController>();
        if (playerController != null)
        {
            playerTransform = playerController.transform;
            playerCamera = playerController.cameraTransform;
        }
    }
    private void Start()
    {
        if (book)
            book.interactable = false;
        if (hintGroup != null)
        {
            hintGroup.gameObject.SetActive(true);
            hintGroup.alpha = 0f;
            hintGroup.interactable = false;
            hintGroup.blocksRaycasts = false;
        }
        SetReadingHintsVisible(false);
        HideHint();
    }
    private void Update()
    {
        UpdateHintFade();
        if (playerTransform == null)
            return;
        if (!isReading)
        {
            HandleIdleState();
            return;
        }
        HandleReadingState();
    }
    private void HandleIdleState()
    {
        Transform refTransform = book != null ? book.transform : transform;
        Vector3 playerPos = playerTransform.position;
        Vector3 bookPos = refTransform.position;
        playerPos.y = 0f;
        bookPos.y = 0f;
        float distance = Vector3.Distance(playerPos, bookPos);
        bool inRange = distance <= activateDistance;
        SetHint(nearHint);
        if (inRange && Input.GetKeyDown(KeyCode.F))
        {
            EnterReadingMode();
        }
    }
    private void HandleReadingState()
    {
        RefreshReadingHints();
        if (Input.GetKeyDown(KeyCode.F) || Input.GetKeyDown(KeyCode.Escape))
        {
            ExitReadingMode();
            return;
        }
        if (Input.GetKeyDown(KeyCode.D))
        {
            FlipNext();
        }
        if (Input.GetKeyDown(KeyCode.A))
        {
            FlipPrevious();
        }
    }
    private void EnterReadingMode()
    {
        if (playerTransform == null)
            return;
        savedPlayerPosition = playerTransform.position;
        savedPlayerRotation = playerTransform.rotation;
        if (playerCamera != null)
            savedCameraLocalRotation = playerCamera.localRotation;
        Vector3 targetPosition;
        Quaternion targetRotation;
        GetReadingPose(out targetPosition, out targetRotation);
        playerTransform.SetPositionAndRotation(targetPosition, targetRotation);
        if (playerCamera != null)
            playerCamera.localRotation = Quaternion.identity;
        if (playerController != null)
            playerController.isInspecting = true;
        isReading = true;
        HideHint();
        tempHintEndTime = -1f;
        tempReadingCenterHint = string.Empty;
        SetReadingHintsVisible(true);
        RefreshReadingHints();
    }
    private void ExitReadingMode()
    {
        if (playerTransform != null)
        {
            playerTransform.SetPositionAndRotation(savedPlayerPosition, savedPlayerRotation);
        }
        if (playerCamera != null)
            playerCamera.localRotation = savedCameraLocalRotation;
        if (playerController != null)
            playerController.isInspecting = false;
        isReading = false;
        tempHintEndTime = -1f;
        tempReadingCenterHint = string.Empty;
        SetReadingHintsVisible(false);
        HideHint();
    }
    public void FlipNext()
    {
        if (!book || isFlipping) return;
        if (book.CurrentPaper > book.EndFlippingPaper)
        {
            ShowTempHint(lastPageHint);
            return;
        }
        StartCoroutine(FlipRoutine(FlipMode.RightToLeft));
    }
    public void FlipPrevious()
    {
        if (!book || isFlipping) return;
        if (book.CurrentPaper <= book.StartFlippingPaper)
        {
            ShowTempHint(firstPageHint);
            return;
        }
        StartCoroutine(FlipRoutine(FlipMode.LeftToRight));
    }
    // AI辅助生成：DeepSeek-R1-0528, 2026-04-23
    private IEnumerator FlipRoutine(FlipMode mode)
    {
        isFlipping = true;
        if (audioSource && pageFlipClip)
            audioSource.PlayOneShot(pageFlipClip);
        PageFlipper.FlipPage(book, flipDuration, mode, () =>
        {
            isFlipping = false;
        });
        yield return null;
    }
    private void SetHint(string text)
    {
        if (exitKeyHintText != null)
            exitKeyHintText.text = text;
        hintVisible = !string.IsNullOrEmpty(text);
        if (hintGroup == null && exitKeyHintText != null)
            exitKeyHintText.gameObject.SetActive(hintVisible);
        if (hintGroup != null)
            hintGroup.gameObject.SetActive(true);
    }
    private void HideHint()
    {
        hintVisible = false;
        if (hintGroup == null && exitKeyHintText != null)
            exitKeyHintText.gameObject.SetActive(false);
    }
    private void UpdateHintFade()
    {
        if (hintGroup == null)
            return;
        float target = hintVisible ? 1f : 0f;
        hintGroup.alpha = Mathf.MoveTowards(hintGroup.alpha, target, hintFadeSpeed * Time.deltaTime);
    }
    private void ShowTempHint(string tip)
    {
        if (!isReading)
            return;
        tempHintEndTime = Time.time + 0.8f;
        tempReadingCenterHint = tip;
        RefreshReadingHints();
    }
    private void OnDisable()
    {
        if (isReading)
            ExitReadingMode();
    }
    private void SetReadingHintsVisible(bool visible)
    {
        if (leftKeyHintText != null) leftKeyHintText.gameObject.SetActive(visible);
        if (rightKeyHintText != null) rightKeyHintText.gameObject.SetActive(visible);
        if (exitKeyHintText != null) exitKeyHintText.gameObject.SetActive(visible);
    }
    private void RefreshReadingHints()
    {
        if (!isReading)
            return;
        if (leftKeyHintText != null) leftKeyHintText.text = leftReadHint;
        if (rightKeyHintText != null) rightKeyHintText.text = rightReadHint;
        if (exitKeyHintText != null)
        {
            if (Time.time < tempHintEndTime && !string.IsNullOrEmpty(tempReadingCenterHint))
                exitKeyHintText.text = tempReadingCenterHint;
            else
                exitKeyHintText.text = exitReadHint;
        }
    }
    private void GetReadingPose(out Vector3 targetPosition, out Quaternion targetRotation)
    {
        if (!useAutoReadingSpot && readingAnchor != null)
        {
            targetPosition = readingAnchor.position;
            targetRotation = readingAnchor.rotation;
            return;
        }
        Transform refTransform = book != null ? book.transform : transform;
        Vector3 forward = refTransform.forward;
        forward.y = 0f;
        if (forward.sqrMagnitude < 0.0001f)
            forward = Vector3.forward;
        forward.Normalize();
        targetPosition = refTransform.position - forward * autoReadDistance;
        targetPosition.y += autoReadHeightOffset;
        Vector3 lookDirection = refTransform.position - targetPosition;
        lookDirection.y = 0f;
        if (lookDirection.sqrMagnitude < 0.0001f)
            lookDirection = forward;
        targetRotation = Quaternion.LookRotation(lookDirection.normalized, Vector3.up);
    }
    private void OnDrawGizmosSelected()
    {
        if (!useAutoReadingSpot || readingAnchor != null)
            return;
        Vector3 pos;
        Quaternion rot;
        GetReadingPose(out pos, out rot);
        Gizmos.color = Color.cyan;
        Gizmos.DrawSphere(pos, 0.08f);
        Gizmos.DrawLine(pos, pos + rot * Vector3.forward * 0.5f);
    }
}
