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
        FiveGLab.VoiceAgent.LabContext.SetHovered(FiveGLab.VoiceAgent.LabContext.ResolveEquipment(gameObject));
        if (PopupUIManager.Instance != null)
        {
            PopupUIManager.Instance.ShowPopup(objectName, objectDescription, transform);
        }
    }

    private void OnHoverExit(HoverExitEventArgs args)
    {
        FiveGLab.VoiceAgent.LabContext.SetHovered(FiveGLab.VoiceAgent.EquipmentId.None);
        if (PopupUIManager.Instance != null)
        {
            PopupUIManager.Instance.HidePopup();
        }
    }
}
