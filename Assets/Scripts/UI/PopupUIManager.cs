using UnityEngine;
using TMPro;
using System.Collections;

public class PopupUIManager : MonoBehaviour
{
    public static PopupUIManager Instance { get; private set; }

    [Header("UI References")]
    [SerializeField] private GameObject popupPanel;
    [SerializeField] private TextMeshProUGUI nameText;
    [SerializeField] private TextMeshProUGUI descriptionText;

    private Coroutine animationCoroutine;
    
    private Vector3 worldOriginalPos;
    private Quaternion worldOriginalRot;
    
    public bool isInspecting = false;
    private float targetScaleFactor = 1.0f;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
        
        if (popupPanel != null)
        {
            popupPanel.SetActive(false);
            popupPanel.transform.localScale = Vector3.zero;
        }
    }

    public void ShowPopup(string objName, string objDescription, Transform targetObj)
    {
        if (popupPanel != null)
        {
            popupPanel.SetActive(true);
            
            if (targetObj != null && Camera.main != null && !isInspecting)
            {
                popupPanel.transform.position = targetObj.position + Vector3.up * 0.4f;
                popupPanel.transform.rotation = Quaternion.LookRotation(popupPanel.transform.position - Camera.main.transform.position);
                
                worldOriginalPos = popupPanel.transform.position;
                worldOriginalRot = popupPanel.transform.rotation;
                targetScaleFactor = 1.0f;
            }

            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateScale(Vector3.one * targetScaleFactor, 0.25f));
        }

        if (nameText != null) nameText.text = objName;
        if (descriptionText != null) descriptionText.text = objDescription;
    }

    public void HidePopup()
    {
        if (isInspecting) return; // Prevent hiding when zoomed in!

        if (popupPanel != null && popupPanel.activeSelf)
        {
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateScale(Vector3.zero, 0.2f, true));
        }
    }
    
    public void SetInspectionMode(bool isInspection)
    {
        if (popupPanel == null || Camera.main == null) return;
        this.isInspecting = isInspection;
        
        if (isInspection)
        {
            targetScaleFactor = 0.25f; // Scale it down by quarter when bringing it very close
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateScale(Vector3.one * targetScaleFactor, 0.3f));
        }
        else
        {
            targetScaleFactor = 1.0f;
            if (animationCoroutine != null) StopCoroutine(animationCoroutine);
            animationCoroutine = StartCoroutine(AnimateScale(Vector3.one * targetScaleFactor, 0.3f));
        }
    }

    private void LateUpdate()
    {
        if (isInspecting && popupPanel != null && popupPanel.activeSelf && Camera.main != null)
        {
            Transform camTr = Camera.main.transform;
            // Bring it EXTREMELY close: 0.15m forward, 0.08m right. Avoids any massive rack clipping!
            Vector3 targetPos = camTr.position + camTr.forward * 0.15f + camTr.right * 0.08f - camTr.up * 0.03f;
            Quaternion targetRot = Quaternion.LookRotation(popupPanel.transform.position - camTr.position);
            
            popupPanel.transform.position = Vector3.Lerp(popupPanel.transform.position, targetPos, Time.deltaTime * 8f);
            popupPanel.transform.rotation = Quaternion.Slerp(popupPanel.transform.rotation, targetRot, Time.deltaTime * 8f);
        }
        else if (!isInspecting && popupPanel != null && popupPanel.activeSelf)
        {
            popupPanel.transform.position = Vector3.Lerp(popupPanel.transform.position, worldOriginalPos, Time.deltaTime * 8f);
            popupPanel.transform.rotation = Quaternion.Slerp(popupPanel.transform.rotation, worldOriginalRot, Time.deltaTime * 8f);
        }
    }

    private IEnumerator AnimateScale(Vector3 targetScale, float duration, bool hideOnComplete = false)
    {
        Vector3 startScale = popupPanel.transform.localScale;
        float elapsed = 0;

        while (elapsed < duration)
        {
            float t = elapsed / duration;
            t = t * t * (3f - 2f * t); 
            popupPanel.transform.localScale = Vector3.Lerp(startScale, targetScale, t);
            elapsed += Time.deltaTime;
            yield return null;
        }

        popupPanel.transform.localScale = targetScale;
        if (hideOnComplete) popupPanel.SetActive(false);
    }
}
