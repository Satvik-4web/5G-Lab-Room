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
        private string activeUserQuery = "";

        private void Awake()
        {
            EnsureUIStructure();
        }

        private void Start()
        {
            if (VoiceAgentManager.Instance != null)
            {
                micCapture = VoiceAgentManager.Instance.GetComponentInChildren<MicrophoneCapture>();
                if (micCapture != null)
                {
                    micCapture.OnDeviceChanged += (dev) => UpdateStateVisuals(VoiceAgentManager.Instance.CurrentState);
                }
            }
            UpdateStateVisuals(AgentState.Idle);
        }

        public void UpdateStateVisuals(AgentState state)
        {
            if (statusText == null) return;

            string micName = micCapture != null ? micCapture.GetFriendlyDeviceName() : "Mic";

            switch (state)
            {
                case AgentState.Idle:
                    statusText.text = $"<color=#8AB4F8><b>ARIA</b></color> <color=#CCCCCC>| Hold [V] to Talk | [M] {micName} | [1] Tour | [T] Ask</color>";
                    if (micIconImage != null) micIconImage.color = new Color(0.6f, 0.6f, 0.6f, 0.7f);
                    if (levelMeterBar != null)
                    {
                        levelMeterBar.color = new Color(0.54f, 0.71f, 0.97f, 0.6f);
                        levelMeterBar.fillAmount = 0f;
                    }
                    break;

                case AgentState.Listening:
                    statusText.text = $"<color=#8AB4F8><b>ARIA</b></color> <color=#00E676><b>| RECORDING ({micName})... Speak now! [V] Stop</b></color>";
                    if (micIconImage != null) micIconImage.color = new Color(0f, 0.95f, 0.45f, 1f);
                    if (levelMeterBar != null) levelMeterBar.color = new Color(0f, 0.95f, 0.45f, 1f);
                    break;

                case AgentState.Transcribing:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#FFCA28><b>| Processing speech...</b></color>";
                    if (micIconImage != null) micIconImage.color = new Color(1f, 0.8f, 0.2f, 1f);
                    if (levelMeterBar != null) levelMeterBar.fillAmount = 0f;
                    break;

                case AgentState.Thinking:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#8AB4F8>| Consulting 5G knowledge base...</color>";
                    if (micIconImage != null) micIconImage.color = new Color(0.54f, 0.71f, 0.97f, 1f);
                    break;

                case AgentState.Speaking:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#FFFFFF>| Speaking (Aria)</color>";
                    if (micIconImage != null) micIconImage.color = new Color(0.54f, 0.71f, 0.97f, 1f);
                    break;

                case AgentState.Touring:
                    statusText.text = "<color=#8AB4F8><b>ARIA</b></color> <color=#8AB4F8>| 5G Interactive Guided Tour</color>";
                    if (micIconImage != null) micIconImage.color = new Color(0.54f, 0.71f, 0.97f, 1f);
                    break;
            }
        }

        public void SetUserQuery(string query)
        {
            activeUserQuery = query;
            if (subtitleText != null)
            {
                subtitleText.text = $"<color=#8AB4F8><b>You:</b> \"{query}\"</color>\n<color=#B0BEC5><i>Aria is thinking...</i></color>";
            }
            if (subtitleFadeRoutine != null) StopCoroutine(subtitleFadeRoutine);
            subtitleFadeRoutine = StartCoroutine(ClearSubtitleAfterDelay(15f));
        }

        public void ShowSubtitle(string text)
        {
            if (subtitleText == null) return;

            if (!string.IsNullOrEmpty(activeUserQuery))
            {
                subtitleText.text = $"<color=#8AB4F8><b>You:</b> \"{activeUserQuery}\"</color>\n<color=#FFFFFF><b>Aria:</b> {text}</color>";
            }
            else
            {
                subtitleText.text = text;
            }

            if (subtitleFadeRoutine != null) StopCoroutine(subtitleFadeRoutine);
            subtitleFadeRoutine = StartCoroutine(ClearSubtitleAfterDelay(15f));
        }

        public void ClearDialogue()
        {
            activeUserQuery = "";
            if (subtitleText != null) subtitleText.text = "";
        }

        private IEnumerator ClearSubtitleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            activeUserQuery = "";
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
                float rms = Mathf.Clamp01(micCapture.CurrentRms * 20f);
                if (micCapture.CurrentRms < 0.0004f)
                {
                    // No voice detected yet (silence)
                    levelMeterBar.color = new Color(0.95f, 0.35f, 0.25f, 0.8f); // Red alert
                    levelMeterBar.fillAmount = 0.04f;
                }
                else
                {
                    // Voice detected!
                    levelMeterBar.color = new Color(0f, 0.95f, 0.45f, 1f); // Bright Green
                    levelMeterBar.fillAmount = Mathf.Lerp(levelMeterBar.fillAmount, Mathf.Max(0.12f, rms), Time.deltaTime * 20f);
                }
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
                rect.sizeDelta = new Vector2(640, 140);
                rect.localScale = Vector3.one * 0.0016f;

                // Background Panel (#1A1A1A at 90% opacity)
                var bgGO = new GameObject("Background");
                bgGO.transform.SetParent(hudRoot.transform, false);
                backgroundImage = bgGO.AddComponent<Image>();
                backgroundImage.color = new Color(0.08f, 0.08f, 0.08f, 0.90f);
                var bgRect = bgGO.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                // Level Meter Bar (Height 5px, reactive)
                var meterGO = new GameObject("LevelMeter");
                meterGO.transform.SetParent(hudRoot.transform, false);
                levelMeterBar = meterGO.AddComponent<Image>();
                levelMeterBar.type = Image.Type.Filled;
                levelMeterBar.fillMethod = Image.FillMethod.Horizontal;
                levelMeterBar.color = new Color(0f, 0.95f, 0.45f, 1f);
                var meterRect = meterGO.GetComponent<RectTransform>();
                meterRect.anchorMin = new Vector2(0, 0);
                meterRect.anchorMax = new Vector2(1, 0);
                meterRect.pivot = new Vector2(0.5f, 0);
                meterRect.sizeDelta = new Vector2(0, 5f);
                meterRect.anchoredPosition = Vector2.zero;

                // Status text
                var statusGO = new GameObject("StatusText");
                statusGO.transform.SetParent(hudRoot.transform, false);
                statusText = statusGO.AddComponent<TextMeshProUGUI>();
                statusText.fontSize = 15;
                statusText.alignment = TextAlignmentOptions.MidlineLeft;
                statusText.text = "ARIA";
                var statusRect = statusGO.GetComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0, 0.72f);
                statusRect.anchorMax = new Vector2(1, 0.96f);
                statusRect.offsetMin = new Vector2(16, 0);
                statusRect.offsetMax = new Vector2(-16, 0);

                // Subtitle text (Supports multi-line user question + Aria reply)
                var subGO = new GameObject("SubtitleText");
                subGO.transform.SetParent(hudRoot.transform, false);
                subtitleText = subGO.AddComponent<TextMeshProUGUI>();
                subtitleText.fontSize = 14;
                subtitleText.color = new Color(0.94f, 0.94f, 0.94f, 1f); // Off-white #F0F0F0
                subtitleText.textWrappingMode = TextWrappingModes.Normal;
                subtitleText.alignment = TextAlignmentOptions.TopLeft;
                subtitleText.text = "";
                var subRect = subGO.GetComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0, 0.06f);
                subRect.anchorMax = new Vector2(1, 0.72f);
                subRect.offsetMin = new Vector2(16, 0);
                subRect.offsetMax = new Vector2(-16, 0);
            }
        }
    }
}
