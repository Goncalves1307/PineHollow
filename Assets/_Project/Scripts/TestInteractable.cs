using UnityEngine;

public class TestInteractable : MonoBehaviour, IInteractable
{
    public void Interact()
    {
        Debug.Log("Interagiste com o objeto!");
    }

    public string GetInteractionText()
    {
        return "Interagir";
    }
}