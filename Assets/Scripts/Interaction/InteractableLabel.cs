using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;

[RequireComponent(typeof(Collider))]
[RequireComponent(typeof(UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable))]
public class InteractableLabel : MonoBehaviour
{
    [Header("Label Info")]
    [Tooltip("The name of the component that will appear in the UI.")]
    public string objectName = "Component Name";
    
    [Tooltip("The description or use of the component.")]
    [TextArea(3, 5)]
    public string objectDescription = "Component Description goes here.";

    private UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable interactable;

    private void Awake()
    {
        interactable = GetComponent<UnityEngine.XR.Interaction.Toolkit.Interactables.XRSimpleInteractable>();
        if (interactable != null)
        {
            interactable.hoverEntered.AddListener(OnHoverEnter);
            interactable.hoverExited.AddListener(OnHoverExit);
        }
    }

    private void OnHoverEnter(HoverEnterEventArgs args)
    {
        if (PopupUIManager.Instance != null)
        {
            PopupUIManager.Instance.ShowPopup(objectName, objectDescription, transform);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        // Don't hide if we are currently inspecting
        // We handle hiding in PCInteractionTester now for inspection mode, 
        // but for normal hover, we should hide it.
        // To be safe and decoupled, we check if the UI is in inspection mode.
        // But since we can't easily check, we'll let PCInteractionTester handle hide when needed,
        // or just let it hide if it's not being inspected.
        // Actually, PCInteractionTester will hide it. Let's just call HidePopup here.
        if (PopupUIManager.Instance != null)
        {
            PopupUIManager.Instance.HidePopup();
        }
    }
}
