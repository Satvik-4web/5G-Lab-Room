using UnityEngine;

public class PlayerInteraction : MonoBehaviour
{
    [Header("Interaction Settings")]
    [Tooltip("The camera used for raycasting. Defaults to Main Camera if left empty.")]
    [SerializeField] private Camera playerCamera;
    
    [Tooltip("How close the player needs to be to see the popup.")]
    [SerializeField] private float interactionDistance = 5f;
    
    [Tooltip("Layer mask to filter out what the player can interact with.")]
    [SerializeField] private LayerMask interactableLayer;

    private InteractableLabel currentTarget;

    private void Start()
    {
        if (playerCamera == null)
        {
            playerCamera = Camera.main;
        }
    }

    private void Update()
    {
        CheckForInteractable();
    }

    private void CheckForInteractable()
    {
        if (playerCamera == null) return;

        // Cast a ray from the center of the camera
        Ray ray = new Ray(playerCamera.transform.position, playerCamera.transform.forward);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit, interactionDistance, interactableLayer))
        {
            InteractableLabel interactable = hit.collider.GetComponent<InteractableLabel>();
            
            if (interactable != null)
            {
                if (currentTarget != interactable)
                {
                    currentTarget = interactable;
                    if (PopupUIManager.Instance != null)
                    {
                        PopupUIManager.Instance.ShowPopup(currentTarget.objectName, currentTarget.objectDescription, currentTarget.transform);
                    }
                }
                return; // Valid interactable found, exit the function early
            }
        }

        // If we hit nothing, or we hit something without an InteractableLabel
        if (currentTarget != null)
        {
            currentTarget = null;
            if (PopupUIManager.Instance != null)
            {
                PopupUIManager.Instance.HidePopup();
            }
        }
    }
}
