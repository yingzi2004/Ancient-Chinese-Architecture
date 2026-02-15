using UnityEngine;
using UnityEngine.EventSystems;

[RequireComponent(typeof(RectTransform))]
public class BookDragItem : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler
{
    [SerializeField] private string itemId = "window";
    [SerializeField] private Canvas canvas;
    [SerializeField] private float dragScale = 1.05f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip grabClip;
    [SerializeField] private AudioClip snapClip;
    [SerializeField] private AudioClip resetClip;

    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private Vector2 startAnchoredPosition;
    private Transform startParent;
    private bool snapped;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();

        if (!canvas)
            canvas = GetComponentInParent<Canvas>();
    }

    private void Start()
    {
        startParent = rectTransform.parent;
        startAnchoredPosition = rectTransform.anchoredPosition;
    }

    public string ItemId => itemId;

    public void ResetPiece()
    {
        snapped = false;
        rectTransform.SetParent(startParent);
        rectTransform.anchoredPosition = startAnchoredPosition;
        rectTransform.localScale = Vector3.one;

        if (canvasGroup)
        {
            canvasGroup.blocksRaycasts = true;
            canvasGroup.alpha = 1f;
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (snapped)
            return;

        if (audioSource && grabClip)
            audioSource.PlayOneShot(grabClip);

        if (canvasGroup)
            canvasGroup.blocksRaycasts = false;

        rectTransform.SetAsLastSibling();
        rectTransform.localScale = Vector3.one * dragScale;
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (snapped || canvas == null)
            return;

        rectTransform.anchoredPosition += eventData.delta / canvas.scaleFactor;
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (canvasGroup)
            canvasGroup.blocksRaycasts = true;

        rectTransform.localScale = Vector3.one;

        if (snapped)
            return;

        rectTransform.anchoredPosition = startAnchoredPosition;

        if (audioSource && resetClip)
            audioSource.PlayOneShot(resetClip);
    }

    public void SnapTo(BookDropSlot slot)
    {
        snapped = true;
        rectTransform.SetParent(slot.transform);
        rectTransform.anchoredPosition = Vector2.zero;
        rectTransform.localScale = Vector3.one;

        if (audioSource && snapClip)
            audioSource.PlayOneShot(snapClip);
    }

    public void RejectDrop()
    {
        if (audioSource && resetClip)
            audioSource.PlayOneShot(resetClip);
    }
}
