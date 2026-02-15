using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.Events;

public class BookPuzzleManager : MonoBehaviour
{
    [Header("Right Page Text")]
    [SerializeField] private TextMeshProUGUI descriptionLabel;
    [TextArea]
    [SerializeField] private string idleMessage = "点击開始遊戲後拖動窗戶感受蘇州園林的節奏";
    [TextArea]
    [SerializeField] private string solvedMessage = "窗是園林與自然之間的框景，透過鏤空窗欞，遠山、流水與白牆構成一幅動態水墨。";

    [Header("Pieces")]
    [SerializeField] private BookDragItem[] dragItems;

    [Header("Feedback")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip solvedClip;

    [Header("Auto Flip")]
    [SerializeField] private float solvedFlipDelay = 0.6f;
    [SerializeField] private float flipDuration = 0.7f;
    [SerializeField] private UnityEvent onPuzzleSolved;

    private BookGameController controller;
    private BookPro book;
    private bool isSolved;

    public void Initialize(BookGameController owner, BookPro linkedBook)
    {
        controller = owner;
        book = linkedBook;
    }

    public void PreparePuzzle()
    {
        isSolved = false;

        if (descriptionLabel)
            descriptionLabel.text = idleMessage;

        if (dragItems == null)
            return;

        for (int i = 0; i < dragItems.Length; i++)
            dragItems[i]?.ResetPiece();
    }

    public void BeginPuzzle()
    {
        PreparePuzzle();
    }

    public void HandleCorrectPlacement()
    {
        if (isSolved)
            return;

        isSolved = true;

        if (descriptionLabel)
            descriptionLabel.text = solvedMessage;

        audioSource?.PlayOneShot(solvedClip);
        onPuzzleSolved?.Invoke();

        StartCoroutine(AutoFlipNextPage());
    }

    private IEnumerator AutoFlipNextPage()
    {
        yield return new WaitForSeconds(solvedFlipDelay);

        if (book)
        {
            PageFlipper.FlipPage(book, flipDuration, FlipMode.RightToLeft, () =>
            {
                controller?.OnPuzzleFinished();
            });
        }
        else
        {
            controller?.OnPuzzleFinished();
        }
    }
}
