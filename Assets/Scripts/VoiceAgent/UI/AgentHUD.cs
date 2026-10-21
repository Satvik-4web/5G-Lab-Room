using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace FiveGLab.VoiceAgent
{
    public class AgentHUD : MonoBehaviour
    {
        [Header("Positioning")]
        [SerializeField] private float forwardDistance = 1.15f;
        [SerializeField] private float verticalOffset = -0.35f;
        [SerializeField] private float smoothFollowSpeed = 6f;

        [Header("UI References (Auto-created if null)")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Image micIconImage;
        [SerializeField] private Image levelMeterBar;

        private MicrophoneCapture micCapture;
        private Coroutine subtitleFadeRoutine;

        private void Awake()
        {
            EnsureUIStructure();
        }

        private void Start()
        {
            if (VoiceAgentManager.Instance != null)
            {
                micCapture = VoiceAgentManager.Instance.GetComponentInChildren<MicrophoneCapture>();
            }
            UpdateStateVisuals(AgentState.Idle);
        }

        public void UpdateStateVisuals(AgentState state)
        {
            if (statusText == null) return;

            switch (state)
            {
                case AgentState.Idle:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#888888>| Hold [V] or Controller Grip to speak</color>";
                    if (micIconImage != null) micIconImage.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                    if (levelMeterBar != null) levelMeterBar.fillAmount = 0f;
                    break;

                case AgentState.Listening:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#4CAF50>| Listening...</color>";
                    if (micIconImage != null) micIconImage.color = new Color(0.3f, 0.9f, 0.4f, 1f);
                    break;

                case AgentState.Transcribing:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#FFCA28>| Processing speech...</color>";
                    if (micIconImage != null) micIconImage.color = new Color(1f, 0.8f, 0.2f, 1f);
                    if (levelMeterBar != null) levelMeterBar.fillAmount = 0f;
                    break;

                case AgentState.Thinking:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#8AB4F8>| Thinking...</color>";
                    if (micIconImage != null) micIconImage.color = new Color(0.54f, 0.71f, 0.97f, 1f);
                    break;

                case AgentState.Speaking:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#FFFFFF>| Speaking</color>";
                    if (micIconImage != null) micIconImage.color = new Color(0.54f, 0.71f, 0.97f, 1f);
                    break;

                case AgentState.Touring:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#E0E0E0>| 5G Guided Tour Active</color>";
                    if (micIconImage != null) micIconImage.color = new Color(0.54f, 0.71f, 0.97f, 1f);
                    break;
            }
        }

        public void ShowSubtitle(string text)
        {
            if (subtitleText == null) return;

            subtitleText.text = text;
            if (subtitleFadeRoutine != null) StopCoroutine(subtitleFadeRoutine);
            subtitleFadeRoutine = StartCoroutine(ClearSubtitleAfterDelay(6f));
        }

        private IEnumerator ClearSubtitleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (subtitleText != null) subtitleText.text = "";
        }

        private void LateUpdate()
        {
            // Soft camera follow (slerp)
            if (Camera.main != null && hudRoot != null)
            {
                Transform cam = Camera.main.transform;
                Vector3 targetPos = cam.position + cam.forward * forwardDistance + cam.up * verticalOffset;
                Quaternion targetRot = Quaternion.LookRotation(targetPos - cam.position);

                hudRoot.transform.position = Vector3.Lerp(hudRoot.transform.position, targetPos, Time.deltaTime * smoothFollowSpeed);
                hudRoot.transform.rotation = Quaternion.Slerp(hudRoot.transform.rotation, targetRot, Time.deltaTime * smoothFollowSpeed);
            }

            // Animate mic level meter during Listening
            if (levelMeterBar != null && micCapture != null && micCapture.IsRecording)
            {
                float rms = Mathf.Clamp01(micCapture.CurrentRms * 12f);
                levelMeterBar.fillAmount = Mathf.Lerp(levelMeterBar.fillAmount, rms, Time.deltaTime * 15f);
            }
            else if (levelMeterBar != null && levelMeterBar.fillAmount > 0.01f)
            {
                levelMeterBar.fillAmount = Mathf.Lerp(levelMeterBar.fillAmount, 0f, Time.deltaTime * 10f);
            }
        }

        private void EnsureUIStructure()
        {
            if (canvas == null) canvas = GetComponentInChildren<Canvas>(true);
            if (canvas == null)
            {
                var canvasGO = new GameObject("AgentHUD_Canvas");
                canvasGO.transform.SetParent(transform, false);
                canvas = canvasGO.AddComponent<Canvas>();
                canvas.renderMode = RenderMode.WorldSpace;
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 10f;
                canvasGO.AddComponent<GraphicRaycaster>();
            }

            if (hudRoot == null)
            {
                hudRoot = new GameObject("HUD_Root");
                hudRoot.transform.SetParent(canvas.transform, false);
                var rect = hudRoot.AddComponent<RectTransform>();
                rect.sizeDelta = new Vector2(500, 110);
                rect.localScale = Vector3.one * 0.0016f; // Perfect comfortable size in VR

                // Background Panel (#1A1A1A at 85% opacity)
                var bgGO = new GameObject("Background");
                bgGO.transform.SetParent(hudRoot.transform, false);
                backgroundImage = bgGO.AddComponent<Image>();
                backgroundImage.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
                var bgRect = bgGO.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                // Level Meter Bar
                var meterGO = new GameObject("LevelMeter");
                meterGO.transform.SetParent(hudRoot.transform, false);
                levelMeterBar = meterGO.AddComponent<Image>();
                levelMeterBar.type = Image.Type.Filled;
                levelMeterBar.fillMethod = Image.FillMethod.Horizontal;
                levelMeterBar.color = new Color(0.54f, 0.71f, 0.97f, 0.9f);
                var meterRect = meterGO.GetComponent<RectTransform>();
                meterRect.anchorMin = new Vector2(0, 0);
                meterRect.anchorMax = new Vector2(1, 0);
                meterRect.pivot = new Vector2(0.5f, 0);
                meterRect.sizeDelta = new Vector2(0, 3f);
                meterRect.anchoredPosition = Vector2.zero;

                // Status text
                var statusGO = new GameObject("StatusText");
                statusGO.transform.SetParent(hudRoot.transform, false);
                statusText = statusGO.AddComponent<TextMeshProUGUI>();
                statusText.fontSize = 17;
                statusText.alignment = TextAlignmentOptions.MidlineLeft;
                statusText.text = "ARIA";
                var statusRect = statusGO.GetComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0, 0.65f);
                statusRect.anchorMax = new Vector2(1, 0.95f);
                statusRect.offsetMin = new Vector2(16, 0);
                statusRect.offsetMax = new Vector2(-16, 0);

                // Subtitle text
                var subGO = new GameObject("SubtitleText");
                subGO.transform.SetParent(hudRoot.transform, false);
                subtitleText = subGO.AddComponent<TextMeshProUGUI>();
                subtitleText.fontSize = 15;
                subtitleText.color = new Color(0.88f, 0.88f, 0.88f, 1f); // Off-white #E0E0E0
                subtitleText.enableWordWrapping = true;
                subtitleText.alignment = TextAlignmentOptions.TopLeft;
                subtitleText.text = "";
                var subRect = subGO.GetComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0, 0.08f);
                subRect.anchorMax = new Vector2(1, 0.65f);
                subRect.offsetMin = new Vector2(16, 0);
                subRect.offsetMax = new Vector2(-16, 0);
            }
        }
    }
}
