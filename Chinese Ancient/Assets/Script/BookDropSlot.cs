using UnityEngine;
using UnityEngine.EventSystems;

public class BookDropSlot : MonoBehaviour, IDropHandler, IPointerEnterHandler, IPointerExitHandler
{
    [SerializeField] private string acceptsItemId = "window";
    [SerializeField] private BookPuzzleManager puzzleManager;
    [SerializeField] private float highlightScale = 1.05f;
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip hoverClip;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
    }

    public void OnDrop(PointerEventData eventData)
    {
        if (eventData.pointerDrag == null)
            return;

        BookDragItem dragItem = eventData.pointerDrag.GetComponent<BookDragItem>();

        if (dragItem == null)
            return;

        if (dragItem.ItemId == acceptsItemId)
        {
            dragItem.SnapTo(this);
            puzzleManager?.HandleCorrectPlacement();
        }
        else
        {
            dragItem.RejectDrop();
        }
    }

    public void OnPointerEnter(PointerEventData eventData)
    {
        if (audioSource && hoverClip)
            audioSource.PlayOneShot(hoverClip);

        rectTransform.localScale = Vector3.one * highlightScale;
    }

    public void OnPointerExit(PointerEventData eventData)
    {
        rectTransform.localScale = Vector3.one;
    }
}
