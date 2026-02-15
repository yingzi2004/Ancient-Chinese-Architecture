using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class BookGameController : MonoBehaviour, IInteractable
{
    [Header("Book UI")]
    [SerializeField] private CanvasGroup bookCanvas;
    [SerializeField] private BookPro book;
    [SerializeField] private float canvasFadeDuration = 0.25f;
    [SerializeField] private bool useCanvasOverlay = false;

    [Header("Start Page")]
    [SerializeField] private Button startButton;
    [SerializeField] private Animator startButtonAnimator;
    [SerializeField] private string startButtonTrigger = "Pressed";

    [Header("Audio")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip openClip;
    [SerializeField] private AudioClip startClip;

    [Header("Mini Game")]
    [SerializeField] private BookPuzzleManager puzzleManager;
    [SerializeField] private float flipDuration = 0.6f;
    [SerializeField] private float puzzleFlipDelay = 0.2f;
    [SerializeField] private int startPageIndex = 0;
    [SerializeField] private int puzzlePageIndex = 1;

    private bool isOpen;
    private bool startRequested;
    private Coroutine fadeRoutine;

    private void Awake()
    {
        if (!book)
            book = GetComponentInChildren<BookPro>(true);

        if (useCanvasOverlay)
            HideInstant();

        if (startButton)
            startButton.onClick.AddListener(RequestStartGame);

        if (puzzleManager)
            puzzleManager.Initialize(this, book);
    }

    private void OnDestroy()
    {
        if (startButton)
            startButton.onClick.RemoveListener(RequestStartGame);
    }

    public void Interact()
    {
        if (isOpen)
            return;

        OpenBook();
    }

    public bool IsOpen => isOpen;

    private void OpenBook()
    {
        isOpen = true;
        ToggleCanvas(true);
        ResetStartPrompt();

        if (book)
        {
            SetCurrentPage(startPageIndex);
            book.interactable = true;
        }

        puzzleManager?.PreparePuzzle();
        if (audioSource && openClip)
            audioSource.PlayOneShot(openClip);
    }

    public void CloseBook()
    {
        if (!isOpen)
            return;

        isOpen = false;
        ToggleCanvas(false);
        ResetStartPrompt();
    }

    private void ToggleCanvas(bool show)
    {
        if (!useCanvasOverlay || !bookCanvas)
            return;

        if (fadeRoutine != null)
            StopCoroutine(fadeRoutine);

        fadeRoutine = StartCoroutine(FadeCanvas(show ? 1f : 0f));
        bookCanvas.blocksRaycasts = show;
        bookCanvas.interactable = show;
    }

    private IEnumerator FadeCanvas(float target)
    {
        float start = bookCanvas.alpha;
        float elapsed = 0f;

        while (elapsed < canvasFadeDuration)
        {
            elapsed += Time.deltaTime;
            bookCanvas.alpha = Mathf.Lerp(start, target, elapsed / canvasFadeDuration);
            yield return null;
        }

        bookCanvas.alpha = target;
    }

    private void HideInstant()
    {
        if (!bookCanvas || !useCanvasOverlay)
            return;

        bookCanvas.alpha = 0f;
        bookCanvas.blocksRaycasts = false;
        bookCanvas.interactable = false;
    }

    public void RequestStartGame()
    {
        TryRequestStartGame();
    }

    public bool TryRequestStartGame()
    {
        if (!TryStartGame())
            return false;

        if (startButtonAnimator && !string.IsNullOrEmpty(startButtonTrigger))
            startButtonAnimator.SetTrigger(startButtonTrigger);

        if (audioSource && startClip)
            audioSource.PlayOneShot(startClip);

        SetStartButtonVisible(false);

        return true;
    }

    private bool TryStartGame()
    {
        if (!isOpen || startRequested)
            return false;

        startRequested = true;
        if (startButton)
            startButton.interactable = false;

        StartCoroutine(FlipToPuzzle());
        return true;
    }

    private IEnumerator FlipToPuzzle()
    {
        if (puzzleFlipDelay > 0f)
            yield return new WaitForSeconds(puzzleFlipDelay);

        yield return FlipToPage(puzzlePageIndex);

        puzzleManager?.BeginPuzzle();
    }

    public void OnPuzzleFinished()
    {
        StartCoroutine(CloseAfterDelay(0.5f));
    }

    private IEnumerator CloseAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        CloseBook();
    }

    private void ResetStartPrompt()
    {
        startRequested = false;
        SetStartButtonVisible(true);
    }

    private IEnumerator FlipToPage(int targetPaperIndex)
    {
        if (!book)
            yield break;

        int clampedTarget = Mathf.Clamp(targetPaperIndex, book.StartFlippingPaper, book.EndFlippingPaper + 1);
        int steps = clampedTarget - book.CurrentPaper;
        if (steps == 0)
            yield break;

        FlipMode mode = steps > 0 ? FlipMode.RightToLeft : FlipMode.LeftToRight;
        steps = Mathf.Abs(steps);

        for (int i = 0; i < steps; i++)
        {
            bool finished = false;
            PageFlipper.FlipPage(book, flipDuration, mode, () => finished = true);
            while (!finished)
                yield return null;
        }
    }

    private void SetCurrentPage(int pageIndex)
    {
        if (!book)
            return;

        int clamped = Mathf.Clamp(pageIndex, book.StartFlippingPaper, book.EndFlippingPaper + 1);
        book.CurrentPaper = clamped;
    }

    private void SetStartButtonVisible(bool visible)
    {
        if (!startButton)
            return;

        if (startButton.gameObject.activeSelf != visible)
            startButton.gameObject.SetActive(visible);

        if (visible)
            startButton.interactable = true;
    }
}
