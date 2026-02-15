using UnityEngine;

[RequireComponent(typeof(Collider))]
public class BookStartInteractable : MonoBehaviour, IInteractable
{
    [Header("References")]
    [SerializeField] private BookGameController controller;
    [SerializeField] private GameObject visualRoot;

    [Header("Feedback")]
    [SerializeField] private Animator pressAnimator;
    [SerializeField] private string pressTrigger = "Pressed";
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip pressClip;
    [SerializeField] private bool disableAfterPress = true;

    private Collider cachedCollider;

    private void Awake()
    {
        cachedCollider = GetComponent<Collider>();
        if (!visualRoot)
            visualRoot = gameObject;
    }

    private void Reset()
    {
        if (!controller)
            controller = GetComponentInParent<BookGameController>();

        Collider col = GetComponent<Collider>();
        if (col)
        {
            col.isTrigger = true;
            cachedCollider = col;
        }
    }

    public void Interact()
    {
        if (!controller || !controller.IsOpen)
            return;

        bool started = controller.TryRequestStartGame();
        if (!started)
            return;

        if (pressAnimator && !string.IsNullOrEmpty(pressTrigger))
            pressAnimator.SetTrigger(pressTrigger);

        if (audioSource && pressClip)
            audioSource.PlayOneShot(pressClip);

        if (disableAfterPress)
        {
            if (visualRoot)
                visualRoot.SetActive(false);

            if (cachedCollider)
                cachedCollider.enabled = false;
        }
    }
}
