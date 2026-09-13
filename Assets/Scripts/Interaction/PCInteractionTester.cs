using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using UnityEngine.XR.Interaction.Toolkit.Interactables;

[RequireComponent(typeof(Collider))]
public class PCInteractionTester : MonoBehaviour
{
    private InteractableLabel interactableLabel;
    private AudioSource audioSource;
    private XRSimpleInteractable xrInteractable;
    private bool isHovered = false;
    private bool isZoomed = false;

    private Transform xrOrigin;
    private Vector3 originalOriginPos;
    private Quaternion originalOriginRot;
    
    [SerializeField]
    public float distanceInFront = 0.65f;

    void Start()
    {
        interactableLabel = GetComponent<InteractableLabel>();
        audioSource = GetComponent<AudioSource>();
        xrInteractable = GetComponent<XRSimpleInteractable>();
        
        GameObject originGO = GameObject.Find("XR Origin (VR)");
        if (originGO != null) xrOrigin = originGO.transform;

        if (xrInteractable != null)
        {
            xrInteractable.selectEntered.AddListener(OnXRSelect);
        }
        
        BoxCollider col = GetComponent<BoxCollider>();
        if (col != null)
        {
            Vector3 worldSize = Vector3.Scale(col.size, transform.lossyScale);
            float maxDim = Mathf.Max(Mathf.Abs(worldSize.x), Mathf.Abs(worldSize.y), Mathf.Abs(worldSize.z));
            if (maxDim > 1.0f)
            {
                distanceInFront = maxDim * 1.1f; 
            }
        }
    }

    private void OnXRSelect(SelectEnterEventArgs args)
    {
        TriggerInteraction();
    }

    void OnMouseEnter()
    {
        if (interactableLabel != null && PopupUIManager.Instance != null && !isZoomed)
        {
            PopupUIManager.Instance.ShowPopup(interactableLabel.objectName, interactableLabel.objectDescription, transform);
            isHovered = true;
        }
    }

    void OnMouseExit()
    {
        if (!isZoomed && interactableLabel != null && PopupUIManager.Instance != null)
        {
            PopupUIManager.Instance.HidePopup();
            isHovered = false;
        }
    }

    void OnMouseDown()
    {
        TriggerInteraction();
    }

    void TriggerInteraction()
    {
        if (xrOrigin != null && Camera.main != null)
        {
            if (!isZoomed)
            {
                if (audioSource != null && !audioSource.isPlaying) audioSource.Play();

                if (interactableLabel != null && PopupUIManager.Instance != null)
                {
                    PopupUIManager.Instance.ShowPopup(interactableLabel.objectName, interactableLabel.objectDescription, transform);
                    PopupUIManager.Instance.SetInspectionMode(true); 
                }

                originalOriginPos = xrOrigin.position;
                originalOriginRot = xrOrigin.rotation;
                
                // Calculate zoom straight in from the camera's current viewing angle
                Vector3 objCenter = transform.position;
                BoxCollider col = GetComponent<BoxCollider>();
                if (col != null) objCenter = transform.TransformPoint(col.center); // Use true center of object
                
                Vector3 dirToCamera = (Camera.main.transform.position - objCenter).normalized;
                
                // Stop distanceInFront meters away from the center
                Vector3 desiredCameraPos = objCenter + (dirToCamera * distanceInFront);
                
                // Keep the camera's original height relative to the object center to avoid looking up/down weirdly
                desiredCameraPos.y = objCenter.y + 0.15f; 

                Vector3 cameraLocalOffset = Camera.main.transform.position - xrOrigin.position;
                Vector3 targetOriginPos = desiredCameraPos - cameraLocalOffset;
                
                // Look directly at the object center
                Vector3 lookDirection = objCenter - desiredCameraPos;
                Quaternion desiredCameraRot = Quaternion.LookRotation(lookDirection);
                Quaternion targetOriginRot = desiredCameraRot * Quaternion.Inverse(Camera.main.transform.localRotation);

                StopAllCoroutines();
                StartCoroutine(LerpTransform(xrOrigin, targetOriginPos, targetOriginRot, 1.2f));
                isZoomed = true;
            }
            else
            {
                if (audioSource != null && audioSource.isPlaying) audioSource.Stop();

                if (interactableLabel != null && PopupUIManager.Instance != null)
                {
                    PopupUIManager.Instance.SetInspectionMode(false);
                    PopupUIManager.Instance.HidePopup();
                }

                StopAllCoroutines();
                StartCoroutine(LerpTransform(xrOrigin, originalOriginPos, originalOriginRot, 1.0f));
                isZoomed = false;
            }
        }
    }

    private IEnumerator LerpTransform(Transform target, Vector3 destPos, Quaternion destRot, float duration)
    {
        Vector3 startPos = target.position;
        Quaternion startRot = target.rotation;
        float elapsed = 0;
        
        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); 
            
            target.position = Vector3.Lerp(startPos, destPos, t);
            target.rotation = Quaternion.Slerp(startRot, destRot, t);
            
            elapsed += Time.deltaTime;
            yield return null;
        }
        
        target.position = destPos;
        target.rotation = destRot;
    }
}
