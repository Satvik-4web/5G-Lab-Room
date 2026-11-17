using System.Collections;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.InputSystem;
using TMPro;

namespace FiveGLab.VoiceAgent
{
    public class AgentHUD : MonoBehaviour
    {
        public static AgentHUD Instance { get; private set; }

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

        [Header("Next Button UI")]
        [SerializeField] private GameObject nextButtonRoot;
        [SerializeField] private Button nextButton;
        [SerializeField] private Image nextButtonBg;
        [SerializeField] private Outline nextButtonOutline;
        [SerializeField] private TextMeshProUGUI nextButtonText;

        [Header("Waveform Visualizer")]
        [SerializeField] private RectTransform[] waveBars;

        private MicrophoneCapture micCapture;
        private AgentSpeaker speaker;
        private AudioSource speakerAudio;
        private AgentAvatarOrb avatarOrb;

        private Coroutine subtitleFadeRoutine;
        private Coroutine timedSubtitleRoutine;
        private string activeUserQuery = "";
        private Vector3 followVelocity;
        private bool isCinemaSubtitleMode = false;
        private System.Action onNextClickAction;
        private bool isNextButtonActive = false;

        public bool IsCinemaSubtitleMode => isCinemaSubtitleMode;
        public bool IsNextButtonActive => isNextButtonActive;

        private float[] audioSamples = new float[128];
        private float smoothedVoiceAmp = 0f;

        private void Awake()
        {
            if (Instance == null) Instance = this;
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
            if (timedSubtitleRoutine != null) { StopCoroutine(timedSubtitleRoutine); timedSubtitleRoutine = null; }
            if (subtitleText != null)
            {
                subtitleText.text = $"<color=#8AB4F8><b>YOU:</b></color> <color=#FFFFFF>\"{query}\"</color>\n<color=#B0BEC5><i>Aria is thinking...</i></color>";
            }
            if (subtitleFadeRoutine != null) StopCoroutine(subtitleFadeRoutine);
            subtitleFadeRoutine = StartCoroutine(ClearSubtitleAfterDelay(16f));
        }

        public void SetCinemaSubtitleMode(bool enabled)
        {
            isCinemaSubtitleMode = enabled;
            if (avatarOrb != null) avatarOrb.gameObject.SetActive(!enabled);
            if (topAccentLine != null) topAccentLine.enabled = !enabled;
            if (levelMeterBar != null) levelMeterBar.enabled = !enabled;
            if (statusText != null) statusText.enabled = !enabled;

            if (waveBars != null)
            {
                for (int i = 0; i < waveBars.Length; i++)
                {
                    if (waveBars[i] != null) waveBars[i].gameObject.SetActive(!enabled);
                }
            }

            if (hudRoot != null)
            {
                var rect = hudRoot.GetComponent<RectTransform>();
                if (rect != null)
                {
                    if (enabled)
                    {
                        rect.sizeDelta = new Vector2(880, 88);
                        if (backgroundImage != null)
                        {
                            backgroundImage.color = new Color(0.02f, 0.04f, 0.07f, 0.92f);
                        }
                        if (topAccentLine != null)
                        {
                            topAccentLine.enabled = true;
                            topAccentLine.color = new Color(0f, 0.90f, 1f, 0.85f);
                            var aRect = topAccentLine.GetComponent<RectTransform>();
                            if (aRect != null)
                            {
                                aRect.anchorMin = new Vector2(0.04f, 1f);
                                aRect.anchorMax = new Vector2(0.96f, 1f);
                                aRect.pivot = new Vector2(0.5f, 1f);
                                aRect.sizeDelta = new Vector2(0, 3f);
                                aRect.anchoredPosition = new Vector2(0, 0);
                            }
                        }
                        if (subtitleText != null)
                        {
                            subtitleText.fontSize = 24;
                            subtitleText.fontStyle = FontStyles.Bold;
                            subtitleText.alignment = TextAlignmentOptions.Center;
                            subtitleText.lineSpacing = 10f;
                            var sRect = subtitleText.GetComponent<RectTransform>();
                            if (sRect != null)
                            {
                                sRect.anchorMin = Vector2.zero;
                                sRect.anchorMax = Vector2.one;
                                sRect.offsetMin = new Vector2(24, 6);
                                sRect.offsetMax = new Vector2(-24, -6);
                            }
                        }
                    }
                    else
                    {
                        rect.sizeDelta = new Vector2(650, 150);
                        if (backgroundImage != null) backgroundImage.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
                        if (subtitleText != null)
                        {
                            subtitleText.fontSize = 14;
                            subtitleText.fontStyle = FontStyles.Normal;
                            subtitleText.alignment = TextAlignmentOptions.TopLeft;
                            subtitleText.lineSpacing = 0f;
                            var sRect = subtitleText.GetComponent<RectTransform>();
                            if (sRect != null)
                            {
                                sRect.anchorMin = new Vector2(0, 0.06f);
                                sRect.anchorMax = new Vector2(1, 0.74f);
                                sRect.offsetMin = new Vector2(18, 0);
                                sRect.offsetMax = new Vector2(-18, 0);
                            }
                        }
                    }
                }
            }
        }

        public void PlayTimedSubtitles(string text, float duration)
        {
            if (subtitleText == null || string.IsNullOrWhiteSpace(text)) return;

            if (subtitleFadeRoutine != null) { StopCoroutine(subtitleFadeRoutine); subtitleFadeRoutine = null; }
            if (timedSubtitleRoutine != null) { StopCoroutine(timedSubtitleRoutine); timedSubtitleRoutine = null; }

            timedSubtitleRoutine = StartCoroutine(TimedSubtitlesRoutine(text, duration));
        }

        private IEnumerator TimedSubtitlesRoutine(string text, float totalDuration)
        {
            var chunks = SplitIntoSubtitleChunks(text);
            if (chunks.Count == 0) yield break;

            int totalWords = 0;
            foreach (var c in chunks) totalWords += Mathf.Max(1, c.Split(' ').Length);

            float safeDuration = totalDuration > 1f ? totalDuration : Mathf.Max(3f, totalWords * 0.35f);

            for (int i = 0; i < chunks.Count; i++)
            {
                string chunk = chunks[i];
                int chunkWords = Mathf.Max(1, chunk.Split(' ').Length);
                float chunkFraction = chunkWords / (float)totalWords;
                float displayTime = safeDuration * chunkFraction;

                // Ensure readable on-screen time for each line
                displayTime = Mathf.Max(1.4f, displayTime);

                if (isCinemaSubtitleMode)
                {
                    subtitleText.text = $"<size=68%><color=#00E5FF><b>ARIA</b></color></size>\n<color=#FFFFFF><b>{chunk}</b></color>";
                }
                else
                {
                    subtitleText.text = $"<color=#00E5FF><b>ARIA:</b></color> <color=#FFFFFF><b>{chunk}</b></color>";
                }

                yield return new WaitForSeconds(displayTime);
            }

            // Natural pause at end of dialogue before clearing
            yield return new WaitForSeconds(2.0f);

            if (subtitleText != null) subtitleText.text = "";
            timedSubtitleRoutine = null;
        }

        public void ShowSubtitle(string text)
        {
            PlayTimedSubtitles(text, 0f);
        }

        public void ClearDialogue()
        {
            activeUserQuery = "";
            if (subtitleFadeRoutine != null) { StopCoroutine(subtitleFadeRoutine); subtitleFadeRoutine = null; }
            if (timedSubtitleRoutine != null) { StopCoroutine(timedSubtitleRoutine); timedSubtitleRoutine = null; }
            if (subtitleText != null) subtitleText.text = "";
        }

        private IEnumerator ClearSubtitleAfterDelay(float delay)
        {
            yield return new WaitForSeconds(delay);
            activeUserQuery = "";
            if (subtitleText != null) subtitleText.text = "";
        }

        private List<string> SplitIntoSubtitleChunks(string text)
        {
            var chunks = new List<string>();
            if (string.IsNullOrWhiteSpace(text)) return chunks;

            string clean = Regex.Replace(text, @"\s+", " ").Trim();
            string[] sentences = Regex.Split(clean, @"(?<=[.!?])\s+");

            foreach (var rawSentence in sentences)
            {
                string s = rawSentence.Trim();
                if (string.IsNullOrEmpty(s)) continue;

                string[] words = s.Split(' ');
                if (words.Length <= 12 || s.Length <= 70)
                {
                    chunks.Add(s);
                }
                else
                {
                    string[] subParts = Regex.Split(s, @"(?<=[,;—:])\s+");
                    string current = "";
                    foreach (var part in subParts)
                    {
                        string candidate = string.IsNullOrEmpty(current) ? part : (current + " " + part).Trim();
                        if (string.IsNullOrEmpty(current))
                        {
                            current = part;
                        }
                        else if (candidate.Split(' ').Length <= 13 && candidate.Length <= 75)
                        {
                            current = candidate;
                        }
                        else if (current.Split(' ').Length < 4)
                        {
                            current = candidate;
                        }
                        else
                        {
                            chunks.Add(current);
                            current = part;
                        }
                    }
                    if (!string.IsNullOrEmpty(current))
                    {
                        chunks.Add(current);
                    }
                }
            }

            if (chunks.Count == 0 && !string.IsNullOrEmpty(clean))
            {
                chunks.Add(clean);
            }

            return chunks;
        }

        private void LateUpdate()
        {
            // Buttery-smooth lazy camera follow with SmoothDamp
            if (Camera.main != null && hudRoot != null)
            {
                Transform cam = Camera.main.transform;
                float currentVOffset = isCinemaSubtitleMode ? -0.48f : verticalOffset;
                float currentFDistance = isCinemaSubtitleMode ? 1.35f : forwardDistance;
                Vector3 targetPos = cam.position + cam.forward * currentFDistance + cam.up * currentVOffset;
                Quaternion targetRot = Quaternion.LookRotation(targetPos - cam.position);

                hudRoot.transform.position = Vector3.SmoothDamp(hudRoot.transform.position, targetPos, ref followVelocity, 0.10f);
                hudRoot.transform.rotation = Quaternion.Slerp(hudRoot.transform.rotation, targetRot, Time.deltaTime * smoothFollowSpeed);
            }

            if (canvas != null && canvas.worldCamera == null && Camera.main != null)
            {
                canvas.worldCamera = Camera.main;
            }

            // Animate next button breathing pulse if active
            if (isNextButtonActive && nextButtonRoot != null && nextButtonRoot.activeSelf)
            {
                float pulse = 1.0f + 0.035f * Mathf.Sin(Time.unscaledTime * 4.5f);
                nextButtonRoot.transform.localScale = Vector3.one * pulse;
                if (nextButtonOutline != null)
                {
                    float glow = 0.65f + 0.35f * Mathf.Sin(Time.unscaledTime * 4.5f);
                    nextButtonOutline.effectColor = new Color(0f, 0.90f, 1f, glow);
                }
            }

            // Keyboard shortcuts to advance tour: [N], [Space], [Enter]
            if (isNextButtonActive)
            {
                bool advancePressed = false;
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.nKey.wasPressedThisFrame ||
                        Keyboard.current.spaceKey.wasPressedThisFrame ||
                        Keyboard.current.enterKey.wasPressedThisFrame)
                    {
                        advancePressed = true;
                    }
                }
                try
                {
                    if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Space) || Input.GetKeyDown(KeyCode.Return))
                    {
                        advancePressed = true;
                    }
                }
                catch { }

                if (advancePressed)
                {
                    TriggerNext();
                }
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
                if (Camera.main != null) canvas.worldCamera = Camera.main;
                var scaler = canvasGO.AddComponent<CanvasScaler>();
                scaler.dynamicPixelsPerUnit = 10f;
                canvasGO.AddComponent<GraphicRaycaster>();
            }
            else if (canvas.worldCamera == null && Camera.main != null)
            {
                canvas.worldCamera = Camera.main;
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

            // 8. Next Module Navigation Button (World Space)
            if (hudRoot != null)
            {
                var allNextButtons = new List<GameObject>();
                foreach (Transform child in hudRoot.transform)
                {
                    if (child != null && child.name == "HUD_NextButton") allNextButtons.Add(child.gameObject);
                }

                if (allNextButtons.Count > 0)
                {
                    nextButtonRoot = allNextButtons[0];
                    for (int i = 1; i < allNextButtons.Count; i++)
                    {
                        Destroy(allNextButtons[i]);
                    }
                    nextButton = nextButtonRoot.GetComponent<Button>();
                    nextButtonBg = nextButtonRoot.GetComponent<Image>();
                    nextButtonOutline = nextButtonRoot.GetComponent<Outline>();
                    nextButtonText = nextButtonRoot.GetComponentInChildren<TextMeshProUGUI>();
                }
                else if (nextButtonRoot == null)
                {
                    nextButtonRoot = new GameObject("HUD_NextButton");
                    nextButtonRoot.transform.SetParent(hudRoot.transform, false);

                    var btnRect = nextButtonRoot.AddComponent<RectTransform>();
                    btnRect.anchorMin = new Vector2(0.5f, 1f);
                    btnRect.anchorMax = new Vector2(0.5f, 1f);
                    btnRect.pivot = new Vector2(0.5f, 0f);
                    btnRect.sizeDelta = new Vector2(320f, 46f);
                    btnRect.anchoredPosition = new Vector2(0f, 16f);

                    nextButtonBg = nextButtonRoot.AddComponent<Image>();
                    nextButtonBg.color = new Color(0.03f, 0.06f, 0.12f, 0.94f);

                    nextButtonOutline = nextButtonRoot.AddComponent<Outline>();
                    nextButtonOutline.effectColor = new Color(0f, 0.90f, 1f, 0.90f);
                    nextButtonOutline.effectDistance = new Vector2(2f, 2f);

                    var shadow = nextButtonRoot.AddComponent<Shadow>();
                    shadow.effectColor = new Color(0f, 0f, 0f, 0.70f);
                    shadow.effectDistance = new Vector2(0f, -3f);

                    nextButton = nextButtonRoot.AddComponent<Button>();
                    var colors = nextButton.colors;
                    colors.normalColor = Color.white;
                    colors.highlightedColor = new Color(0.5f, 1f, 1f, 1f);
                    colors.pressedColor = new Color(0f, 0.85f, 1f, 1f);
                    colors.selectedColor = Color.white;
                    nextButton.colors = colors;
                    nextButton.onClick.AddListener(OnNextButtonClicked);

                    var textGO = new GameObject("ButtonText");
                    textGO.transform.SetParent(nextButtonRoot.transform, false);
                    var tRect = textGO.AddComponent<RectTransform>();
                    tRect.anchorMin = Vector2.zero;
                    tRect.anchorMax = Vector2.one;
                    tRect.offsetMin = new Vector2(10, 0);
                    tRect.offsetMax = new Vector2(-10, 0);

                    nextButtonText = textGO.AddComponent<TextMeshProUGUI>();
                    nextButtonText.fontSize = 15;
                    nextButtonText.fontStyle = FontStyles.Bold;
                    nextButtonText.alignment = TextAlignmentOptions.Center;
                    nextButtonText.color = new Color(0f, 0.95f, 1f, 1f);
                    nextButtonText.text = "NEXT >";

                    nextButtonRoot.SetActive(false);
                }
            }
        }

        public void ShowNextButton(string label, System.Action onClick)
        {
            EnsureUIStructure();
            onNextClickAction = onClick;
            isNextButtonActive = true;

            if (nextButtonRoot != null)
            {
                nextButtonRoot.SetActive(true);
                nextButtonRoot.transform.localScale = Vector3.one;
            }
            if (nextButtonText != null)
            {
                nextButtonText.text = label;
            }
        }

        public void HideNextButton()
        {
            isNextButtonActive = false;
            onNextClickAction = null;

            if (nextButtonRoot != null)
            {
                nextButtonRoot.SetActive(false);
            }
        }

        private void OnNextButtonClicked()
        {
            TriggerNext();
        }

        private float lastNextTriggerTime = 0f;

        public void TriggerNext()
        {
            if (!isNextButtonActive) return;
            if (Time.unscaledTime - lastNextTriggerTime < 0.35f) return;
            lastNextTriggerTime = Time.unscaledTime;

            var action = onNextClickAction;
            action?.Invoke();
        }
    }
}
