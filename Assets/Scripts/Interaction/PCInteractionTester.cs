using UnityEngine;
using UnityEngine.XR.Interaction.Toolkit;
using System.Collections;
using System.Collections.Generic;
using UnityEngine.XR.Interaction.Toolkit.Interactables;
using FiveGLab.VoiceAgent;

[RequireComponent(typeof(Collider))]
public class PCInteractionTester : MonoBehaviour
{
    public static readonly Dictionary<EquipmentId, PCInteractionTester> Instances = new Dictionary<EquipmentId, PCInteractionTester>();
    private static PCInteractionTester currentZoomedInstance = null;
    private static Vector3 defaultOriginPos;
    private static Quaternion defaultOriginRot;
    private static bool hasSavedDefaultOrigin = false;

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

    public EquipmentId AssociatedEquipmentId { get; private set; } = EquipmentId.None;
    public AudioSource EquipmentAudioSource => audioSource;

    void Awake()
    {
        AssociatedEquipmentId = LabContext.ResolveEquipment(gameObject);
        if (AssociatedEquipmentId != EquipmentId.None)
        {
            Instances[AssociatedEquipmentId] = this;
        }
    }

    void OnDestroy()
    {
        if (AssociatedEquipmentId != EquipmentId.None && Instances.ContainsKey(AssociatedEquipmentId))
        {
            Instances.Remove(AssociatedEquipmentId);
        }
    }

    void Start()
    {
        interactableLabel = GetComponent<InteractableLabel>();
        audioSource = GetComponent<AudioSource>();
        xrInteractable = GetComponent<XRSimpleInteractable>();
        
        GameObject originGO = GameObject.Find("XR Origin (VR)");
        if (originGO != null)
        {
            xrOrigin = originGO.transform;
            if (!hasSavedDefaultOrigin)
            {
                defaultOriginPos = xrOrigin.position;
                defaultOriginRot = xrOrigin.rotation;
                hasSavedDefaultOrigin = true;
            }
        }

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
            LabContext.SetHovered(AssociatedEquipmentId);
        }
    }

    void OnMouseExit()
    {
        if (!isZoomed && interactableLabel != null && PopupUIManager.Instance != null)
        {
            PopupUIManager.Instance.HidePopup();
            isHovered = false;
            LabContext.SetHovered(EquipmentId.None);
        }
    }

    void OnMouseDown()
    {
        TriggerInteraction();
    }

    public static void InspectEquipment(EquipmentId id, bool playEquipmentAudio = false)
    {
        if (Instances.TryGetValue(id, out var tester))
        {
            if (currentZoomedInstance != null && currentZoomedInstance != tester)
            {
                currentZoomedInstance.CloseUI();
                currentZoomedInstance.isZoomed = false;
                currentZoomedInstance = null;
            }

            if (!tester.isZoomed)
            {
                tester.TriggerInteraction(playEquipmentAudio);
            }
            else
            {
                tester.ShowPopupUI();
            }
        }
    }

    public static void ExitCurrentInspection()
    {
        if (currentZoomedInstance != null && currentZoomedInstance.isZoomed)
        {
            currentZoomedInstance.ExitInspection();
        }
    }

    public void ShowPopupUI()
    {
        if (interactableLabel != null && PopupUIManager.Instance != null)
        {
            PopupUIManager.Instance.ShowPopup(interactableLabel.objectName, interactableLabel.objectDescription, transform);
            PopupUIManager.Instance.SetInspectionMode(true);
        }
    }

    public void CloseUI()
    {
        if (audioSource != null && audioSource.isPlaying) audioSource.Stop();
        if (interactableLabel != null && PopupUIManager.Instance != null)
        {
            PopupUIManager.Instance.SetInspectionMode(false);
            PopupUIManager.Instance.HidePopup();
        }
    }

    public void EnterInspection(bool playAudio = false)
    {
        if (!isZoomed) TriggerInteraction(playAudio);
    }

    public void ExitInspection()
    {
        if (isZoomed)
        {
            CloseUI();
            if (currentZoomedInstance == this) currentZoomedInstance = null;
            LabContext.SetInspected(EquipmentId.None);

            StopAllCoroutines();
            if (xrOrigin != null)
            {
                Vector3 returnPos = hasSavedDefaultOrigin ? defaultOriginPos : originalOriginPos;
                Quaternion returnRot = hasSavedDefaultOrigin ? defaultOriginRot : originalOriginRot;
                StartCoroutine(LerpTransform(xrOrigin, returnPos, returnRot, 1.0f));
            }
            isZoomed = false;
        }
    }

    public void TriggerInteraction()
    {
        TriggerInteraction(false);
    }

    public void TriggerInteraction(bool allowAudio)
    {
        if (xrOrigin != null && Camera.main != null)
        {
            if (!isZoomed)
            {
                bool skipWav = false;
                if (VoiceAgentManager.Instance != null && VoiceAgentManager.Instance.Config != null)
                {
                    skipWav = VoiceAgentManager.Instance.Config.useAgentNarrationInsteadOfWav;
                }

                // Check if Aria is currently speaking or in Guided Tour mode
                bool ariaSpeaking = false;
                if (VoiceAgentManager.Instance != null)
                {
                    ariaSpeaking = (VoiceAgentManager.Instance.CurrentState == AgentState.Speaking ||
                                    VoiceAgentManager.Instance.CurrentState == AgentState.Touring);
                }

                // Play equipment pre-recorded audio ONLY if Aria is not speaking and audio is allowed
                if (allowAudio && !skipWav && !ariaSpeaking && audioSource != null && !audioSource.isPlaying)
                {
                    audioSource.Play();
                }

                ShowPopupUI();

                currentZoomedInstance = this;
                LabContext.SetInspected(AssociatedEquipmentId);

                originalOriginPos = xrOrigin.position;
                originalOriginRot = xrOrigin.rotation;
                
                // Calculate zoom straight in from the camera's current viewing angle
                Vector3 objCenter = transform.position;
                BoxCollider col = GetComponent<BoxCollider>();
                if (col != null) objCenter = transform.TransformPoint(col.center);
                
                Vector3 dirToCamera = (Camera.main.transform.position - objCenter).normalized;
                Vector3 desiredCameraPos = objCenter + (dirToCamera * distanceInFront);
                desiredCameraPos.y = objCenter.y + 0.15f; 

                Vector3 cameraLocalOffset = Camera.main.transform.position - xrOrigin.position;
                Vector3 targetOriginPos = desiredCameraPos - cameraLocalOffset;
                
                Vector3 lookDirection = objCenter - desiredCameraPos;
                Quaternion desiredCameraRot = Quaternion.LookRotation(lookDirection);
                Quaternion targetOriginRot = desiredCameraRot * Quaternion.Inverse(Camera.main.transform.localRotation);

                StopAllCoroutines();
                StartCoroutine(LerpTransform(xrOrigin, targetOriginPos, targetOriginRot, 1.2f));
                isZoomed = true;
            }
            else
            {
                ExitInspection();
            }
        }
    }

    public void Highlight(float duration)
    {
        StartCoroutine(PulseEmissive(duration));
    }

    private IEnumerator PulseEmissive(float duration)
    {
        var renderers = GetComponentsInChildren<Renderer>();
        if (renderers == null || renderers.Length == 0) yield break;

        var mpb = new MaterialPropertyBlock();
        Color highlightCol = new Color(0f, 0.9f, 1f, 1f); // Neon Cyan

        float elapsed = 0;
        while (elapsed < duration)
        {
            float t = Mathf.PingPong(elapsed * 3f, 1f);
            Color glow = highlightCol * (t * 2.2f);
            for (int i = 0; i < renderers.Length; i++)
            {
                if (renderers[i] != null)
                {
                    renderers[i].GetPropertyBlock(mpb);
                    mpb.SetColor("_EmissionColor", glow);
                    renderers[i].SetPropertyBlock(mpb);
                }
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < renderers.Length; i++)
        {
            if (renderers[i] != null)
            {
                renderers[i].GetPropertyBlock(mpb);
                mpb.Clear();
                renderers[i].SetPropertyBlock(mpb);
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
