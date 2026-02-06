using UnityEngine;

public class GardenButton : MonoBehaviour, IInteractable
{
    public enum ButtonType { Next, Previous }
    public ButtonType type;

    public void Interact()
    {
        GardenManager manager = Object.FindFirstObjectByType<GardenManager>();
        if (manager == null) return;

        if (type == ButtonType.Next)
        {
            manager.NextGarden();
        }
        else
        {
            manager.PreviousGarden();
        }
    }
}