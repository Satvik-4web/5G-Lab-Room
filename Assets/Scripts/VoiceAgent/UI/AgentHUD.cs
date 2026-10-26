using System.Collections;
using System.Collections.Generic;
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
        [SerializeField] private float smoothFollowSpeed = 7f;

        [Header("UI References (Auto-created if null)")]
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject hudRoot;
        [SerializeField] private Image backgroundImage;
        [SerializeField] private Image topAccentLine;
        [SerializeField] private TextMeshProUGUI statusText;
        [SerializeField] private TextMeshProUGUI subtitleText;
        [SerializeField] private Image micIconImage;
        [SerializeField] private Image levelMeterBar;

        [Header("Waveform Visualizer")]
        [SerializeField] private RectTransform[] waveBars;

        private MicrophoneCapture micCapture;
        private AgentSpeaker speaker;
        private AudioSource speakerAudio;
        private AgentAvatarOrb avatarOrb;

        private Coroutine subtitleFadeRoutine;
        private string activeUserQuery = "";
        private Vector3 followVelocity;

        private float[] audioSamples = new float[128];
        private float smoothedVoiceAmp = 0f;

        private void Awake()
        {
            EnsureUIStructure();
        }

        private void Start()
        {
            if (VoiceAgentManager.Instance != null)
            {
                micCapture = VoiceAgentManager.Instance.GetComponentInChildren<MicrophoneCapture>();
                speaker = VoiceAgentManager.Instance.GetComponentInChildren<AgentSpeaker>();
                if (speaker != null)
                {
                    speakerAudio = speaker.GetComponent<AudioSource>();
                }

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
                    statusText.text = $"<color=#00E5FF><b>● ARIA</b></color> <color=#94A3B8>| 5G AI Tutor</color> <color=#475569>•</color> <color=#CBD5E1>Hold [V] to Talk</color> <color=#475569>•</color> <color=#94A3B8>[M] {micName}</color> <color=#475569>•</color> <color=#94A3B8>[1] Tour</color>";
                    if (topAccentLine != null) topAccentLine.color = new Color(0f, 0.9f, 1f, 0.8f);
                    if (levelMeterBar != null)
                    {
                        levelMeterBar.color = new Color(0f, 0.9f, 1f, 0.4f);
                        levelMeterBar.fillAmount = 0.02f;
                    }
                    break;

                case AgentState.Listening:
                    statusText.text = $"<color=#00E676><b>● RECORDING</b></color> <color=#00E676><b>| Listening on {micName}... Speak now! [V] Stop</b></color>";
                    if (topAccentLine != null) topAccentLine.color = new Color(0f, 1f, 0.45f, 1f);
                    if (levelMeterBar != null) levelMeterBar.color = new Color(0f, 1f, 0.45f, 1f);
                    break;

                case AgentState.Transcribing:
                    statusText.text = "<color=#FFD54F><b>● PROCESSING</b></color> <color=#FFD54F><b>| Transcribing your speech...</b></color>";
                    if (topAccentLine != null) topAccentLine.color = new Color(1f, 0.83f, 0.31f, 1f);
                    if (levelMeterBar != null) levelMeterBar.fillAmount = 0.05f;
                    break;

                case AgentState.Thinking:
                    statusText.text = "<color=#FFD54F><b>● THINKING</b></color> <color=#FFD54F>| Consulting 5G Knowledge Brain...</color>";
                    if (topAccentLine != null) topAccentLine.color = new Color(1f, 0.83f, 0.31f, 1f);
                    break;

                case AgentState.Speaking:
                    statusText.text = "<color=#40C4FF><b>● SPEAKING</b></color> <color=#E2E8F0>| Aria (5G Voice Tutor)</color>";
                    if (topAccentLine != null) topAccentLine.color = new Color(0.25f, 0.77f, 1f, 1f);
                    if (levelMeterBar != null) levelMeterBar.color = new Color(0.25f, 0.77f, 1f, 0.8f);
                    break;

                case AgentState.Touring:
                    statusText.text = "<color=#00E5FF><b>● GUIDED TOUR</b></color> <color=#94A3B8>| 5G Interactive Lab Walkthrough</color>";
                    if (topAccentLine != null) topAccentLine.color = new Color(0f, 0.9f, 1f, 1f);
                    break;
            }
        }

        public void SetUserQuery(string query)
        {
            activeUserQuery = query;
            if (subtitleText != null)
            {
                subtitleText.text = $"<color=#8AB4F8><b>YOU:</b></color> <color=#FFFFFF>\"{query}\"</color>\n<color=#B0BEC5><i>Aria is thinking...</i></color>";
            }
            if (subtitleFadeRoutine != null) StopCoroutine(subtitleFadeRoutine);
            subtitleFadeRoutine = StartCoroutine(ClearSubtitleAfterDelay(16f));
        }

        public void ShowSubtitle(string text)
        {
            if (subtitleText == null) return;

            if (!string.IsNullOrEmpty(activeUserQuery))
            {
                subtitleText.text = $"<color=#8AB4F8><b>YOU:</b></color> <color=#CBD5E1>\"{activeUserQuery}\"</color>\n<color=#00E5FF><b>ARIA:</b></color> <color=#FFFFFF>{text}</color>";
            }
            else
            {
                subtitleText.text = $"<color=#00E5FF><b>ARIA:</b></color> <color=#FFFFFF>{text}</color>";
            }

            if (subtitleFadeRoutine != null) StopCoroutine(subtitleFadeRoutine);
            subtitleFadeRoutine = StartCoroutine(ClearSubtitleAfterDelay(16f));
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
            // Buttery-smooth lazy camera follow with SmoothDamp
            if (Camera.main != null && hudRoot != null)
            {
                Transform cam = Camera.main.transform;
                Vector3 targetPos = cam.position + cam.forward * forwardDistance + cam.up * verticalOffset;
                Quaternion targetRot = Quaternion.LookRotation(targetPos - cam.position);

                hudRoot.transform.position = Vector3.SmoothDamp(hudRoot.transform.position, targetPos, ref followVelocity, 0.10f);
                hudRoot.transform.rotation = Quaternion.Slerp(hudRoot.transform.rotation, targetRot, Time.deltaTime * smoothFollowSpeed);
            }

            AgentState state = VoiceAgentManager.Instance != null ? VoiceAgentManager.Instance.CurrentState : AgentState.Idle;

            // 1. Reactive audio RMS calculation
            float voiceAmp = 0f;
            if (state == AgentState.Listening && micCapture != null && micCapture.IsRecording)
            {
                voiceAmp = Mathf.Clamp01(micCapture.CurrentRms * 15f);
            }
            else if (state == AgentState.Speaking && speakerAudio != null && speakerAudio.isPlaying)
            {
                speakerAudio.GetOutputData(audioSamples, 0);
                float sum = 0f;
                for (int i = 0; i < audioSamples.Length; i++) sum += audioSamples[i] * audioSamples[i];
                voiceAmp = Mathf.Clamp01(Mathf.Sqrt(sum / audioSamples.Length) * 7f);
            }

            smoothedVoiceAmp = Mathf.Lerp(smoothedVoiceAmp, voiceAmp, Time.deltaTime * 20f);

            // 2. Animate Level Meter Bar
            if (levelMeterBar != null)
            {
                if (state == AgentState.Listening)
                {
                    if (micCapture != null && micCapture.CurrentRms < 0.0004f)
                    {
                        levelMeterBar.color = new Color(0.95f, 0.35f, 0.25f, 0.8f); // Red alert
                        levelMeterBar.fillAmount = 0.04f;
                    }
                    else
                    {
                        levelMeterBar.color = new Color(0f, 0.95f, 0.45f, 1f); // Bright Green
                        levelMeterBar.fillAmount = Mathf.Lerp(levelMeterBar.fillAmount, Mathf.Max(0.10f, smoothedVoiceAmp), Time.deltaTime * 20f);
                    }
                }
                else if (state == AgentState.Speaking)
                {
                    levelMeterBar.fillAmount = Mathf.Lerp(levelMeterBar.fillAmount, Mathf.Max(0.08f, smoothedVoiceAmp), Time.deltaTime * 18f);
                }
                else
                {
                    levelMeterBar.fillAmount = Mathf.Lerp(levelMeterBar.fillAmount, 0.02f, Time.deltaTime * 8f);
                }
            }

            // 3. Animate Header Waveform Equalizer Bars
            AnimateWaveformBars(state, smoothedVoiceAmp);
        }

        private void AnimateWaveformBars(AgentState state, float amp)
        {
            if (waveBars == null || waveBars.Length == 0) return;

            float time = Time.time;
            for (int i = 0; i < waveBars.Length; i++)
            {
                if (waveBars[i] == null) continue;

                float barHeight = 4f;

                if (state == AgentState.Speaking)
                {
                    // Dancing speech waveform
                    float wave = Mathf.Abs(Mathf.Sin(time * 12f + i * 0.9f));
                    barHeight = Mathf.Clamp(4f + (wave * 18f * Mathf.Max(0.25f, amp)), 4f, 26f);
                }
                else if (state == AgentState.Listening)
                {
                    // Microphone response waveform
                    float wave = Mathf.Abs(Mathf.Sin(time * 10f + i * 1.1f));
                    barHeight = Mathf.Clamp(4f + (wave * 20f * amp), 4f, 26f);
                }
                else if (state == AgentState.Thinking || state == AgentState.Transcribing)
                {
                    // Traveling thinking ripple
                    float wave = Mathf.Abs(Mathf.Sin(time * 6f + i * 0.6f));
                    barHeight = 4f + wave * 14f;
                }
                else
                {
                    // Gentle ambient idle wave
                    float wave = Mathf.Abs(Mathf.Sin(time * 2f + i * 0.5f));
                    barHeight = 4f + wave * 5f;
                }

                waveBars[i].sizeDelta = new Vector2(waveBars[i].sizeDelta.x, barHeight);
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
                rect.sizeDelta = new Vector2(650, 150);
                rect.localScale = Vector3.one * 0.0016f;

                // 1. Sleek Glassmorphism Background (#0B0F19 at 92% opacity)
                var bgGO = new GameObject("Background");
                bgGO.transform.SetParent(hudRoot.transform, false);
                backgroundImage = bgGO.AddComponent<Image>();
                backgroundImage.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
                var bgRect = bgGO.GetComponent<RectTransform>();
                bgRect.anchorMin = Vector2.zero;
                bgRect.anchorMax = Vector2.one;
                bgRect.offsetMin = Vector2.zero;
                bgRect.offsetMax = Vector2.zero;

                // 2. Cybernetic Glowing Top Accent Line (Height 2.5px)
                var topGO = new GameObject("TopAccentLine");
                topGO.transform.SetParent(hudRoot.transform, false);
                topAccentLine = topGO.AddComponent<Image>();
                topAccentLine.color = new Color(0f, 0.9f, 1f, 0.9f);
                var topRect = topGO.GetComponent<RectTransform>();
                topRect.anchorMin = new Vector2(0, 1);
                topRect.anchorMax = new Vector2(1, 1);
                topRect.pivot = new Vector2(0.5f, 1);
                topRect.sizeDelta = new Vector2(0, 2.5f);
                topRect.anchoredPosition = Vector2.zero;

                // 3. Bottom Reactive Volume Meter Bar (Height 4px)
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
                meterRect.sizeDelta = new Vector2(0, 4f);
                meterRect.anchoredPosition = Vector2.zero;

                // 4. Status text header
                var statusGO = new GameObject("StatusText");
                statusGO.transform.SetParent(hudRoot.transform, false);
                statusText = statusGO.AddComponent<TextMeshProUGUI>();
                statusText.fontSize = 14;
                statusText.alignment = TextAlignmentOptions.MidlineLeft;
                statusText.text = "ARIA";
                var statusRect = statusGO.GetComponent<RectTransform>();
                statusRect.anchorMin = new Vector2(0, 0.74f);
                statusRect.anchorMax = new Vector2(0.80f, 0.98f);
                statusRect.offsetMin = new Vector2(18, 0);
                statusRect.offsetMax = Vector2.zero;

                // 5. Animated 7-Bar Audio Waveform Equalizer (Top-Right)
                var waveRoot = new GameObject("WaveformEqualizer");
                waveRoot.transform.SetParent(hudRoot.transform, false);
                var waveRootRect = waveRoot.AddComponent<RectTransform>();
                waveRootRect.anchorMin = new Vector2(0.82f, 0.76f);
                waveRootRect.anchorMax = new Vector2(0.98f, 0.96f);
                waveRootRect.offsetMin = Vector2.zero;
                waveRootRect.offsetMax = Vector2.zero;

                waveBars = new RectTransform[7];
                for (int b = 0; b < 7; b++)
                {
                    var barGO = new GameObject($"WaveBar_{b}");
                    barGO.transform.SetParent(waveRoot.transform, false);
                    var barImg = barGO.AddComponent<Image>();
                    barImg.color = new Color(0f, 0.9f, 1f, 0.85f);
                    var barRect = barGO.GetComponent<RectTransform>();
                    barRect.anchorMin = new Vector2((float)b / 7f, 0.5f);
                    barRect.anchorMax = new Vector2((float)(b + 1) / 7f, 0.5f);
                    barRect.pivot = new Vector2(0.5f, 0.5f);
                    barRect.sizeDelta = new Vector2(4f, 6f);
                    waveBars[b] = barRect;
                }

                // 6. Subtitle dialogue text
                var subGO = new GameObject("SubtitleText");
                subGO.transform.SetParent(hudRoot.transform, false);
                subtitleText = subGO.AddComponent<TextMeshProUGUI>();
                subtitleText.fontSize = 14;
                subtitleText.color = new Color(0.95f, 0.95f, 0.95f, 1f);
                subtitleText.textWrappingMode = TextWrappingModes.Normal;
                subtitleText.alignment = TextAlignmentOptions.TopLeft;
                subtitleText.text = "";
                var subRect = subGO.GetComponent<RectTransform>();
                subRect.anchorMin = new Vector2(0, 0.06f);
                subRect.anchorMax = new Vector2(1, 0.74f);
                subRect.offsetMin = new Vector2(18, 0);
                subRect.offsetMax = new Vector2(-18, 0);
            }

            // 7. Spawn 3D Holographic AI Avatar Orb floating above HUD
            if (avatarOrb == null) avatarOrb = GetComponentInChildren<AgentAvatarOrb>();
            if (avatarOrb == null)
            {
                var orbGO = new GameObject("Aria_AvatarOrb");
                orbGO.transform.SetParent(hudRoot.transform, false);
                // Float gracefully directly above the center of the HUD panel
                orbGO.transform.localPosition = new Vector3(0, 110f, 10f);
                avatarOrb = orbGO.AddComponent<AgentAvatarOrb>();
            }
        }
    }
}
