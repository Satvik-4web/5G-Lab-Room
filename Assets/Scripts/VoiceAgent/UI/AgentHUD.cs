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

        [Header("Guided Tour Visuals")]
        [SerializeField] private GameObject conceptCardRoot;
        [SerializeField] private TextMeshProUGUI conceptTitleText;
        [SerializeField] private TextMeshProUGUI conceptCaptionText;
        [SerializeField] private Image[] conceptNodeImages;
        [SerializeField] private TextMeshProUGUI[] conceptNodeTexts;
        [SerializeField] private RectTransform[] conceptNodeRects;
        [SerializeField] private TextMeshProUGUI[] conceptArrows;

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
        private CanvasGroup subtitleCanvasGroup;
        private RectTransform subtitleRect;
        private CanvasGroup conceptCanvasGroup;
        private Coroutine conceptIntroRoutine;
        private int conceptNodeCount;
        private Vector3 defaultHudScale = Vector3.one * 0.0016f;
        private bool hasCapturedHudScale;

        public bool IsCinemaSubtitleMode => isCinemaSubtitleMode;
        public bool IsNextButtonActive => isNextButtonActive;

        private float[] audioSamples = new float[128];
        private float smoothedVoiceAmp = 0f;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            EnsureUIStructure();
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
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
#if UNITY_ANDROID && !UNITY_EDITOR
                    statusText.text = "<color=#00E5FF><b>ARIA</b></color>  <color=#CBD5E1>A: Next  |  X: Tour  |  Right grip: Speak</color>";
#else
                    statusText.text = $"<color=#00E5FF><b>ARIA</b></color> <color=#94A3B8>| 5G AI Tutor</color>  <color=#CBD5E1>Hold [V] while speaking</color>  <color=#94A3B8>[M] {micName}  |  [1] Tour</color>";
#endif
                    if (topAccentLine != null) topAccentLine.color = new Color(0f, 0.9f, 1f, 0.8f);
                    if (levelMeterBar != null)
                    {
                        levelMeterBar.color = new Color(0f, 0.9f, 1f, 0.4f);
                        levelMeterBar.fillAmount = 0.02f;
                    }
                    break;

                case AgentState.Listening:
#if UNITY_ANDROID && !UNITY_EDITOR
                    statusText.text = $"<color=#00E676><b>LISTENING</b></color> <color=#E2E8F0>| Press right grip again to send</color> <color=#94A3B8>({micName})</color>";
#else
                    statusText.text = $"<color=#00E676><b>LISTENING</b></color> <color=#E2E8F0>| Speak, then release [V] or tap again to send</color> <color=#94A3B8>({micName})</color>";
#endif
                    if (topAccentLine != null) topAccentLine.color = new Color(0f, 1f, 0.45f, 1f);
                    if (levelMeterBar != null) levelMeterBar.color = new Color(0f, 1f, 0.45f, 1f);
                    break;

                case AgentState.Transcribing:
                    statusText.text = "<color=#FFD54F><b>PROCESSING</b></color> <color=#E2E8F0>| Converting your speech to text...</color>";
                    if (topAccentLine != null) topAccentLine.color = new Color(1f, 0.83f, 0.31f, 1f);
                    if (levelMeterBar != null) levelMeterBar.fillAmount = 0.05f;
                    break;

                case AgentState.Thinking:
                    statusText.text = "<color=#FFD54F><b>THINKING</b></color> <color=#E2E8F0>| Aria is preparing a reply...</color>";
                    if (topAccentLine != null) topAccentLine.color = new Color(1f, 0.83f, 0.31f, 1f);
                    break;

                case AgentState.Speaking:
                    statusText.text = "<color=#40C4FF><b>ARIA SPEAKING</b></color> <color=#E2E8F0>| Hold [V] to interrupt</color>";
                    if (topAccentLine != null) topAccentLine.color = new Color(0.25f, 0.77f, 1f, 1f);
                    if (levelMeterBar != null) levelMeterBar.color = new Color(0.25f, 0.77f, 1f, 0.8f);
                    break;

                case AgentState.Touring:
                    statusText.text = "<color=#00E5FF><b>GUIDED TOUR</b></color> <color=#94A3B8>| 5G Interactive Lab Walkthrough</color>";
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
                subtitleText.maxVisibleCharacters = int.MaxValue;
                if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 1f;
            }
            if (subtitleFadeRoutine != null) StopCoroutine(subtitleFadeRoutine);
            subtitleFadeRoutine = StartCoroutine(ClearSubtitleAfterDelay(16f));
        }

        public void SetCinemaSubtitleMode(bool enabled)
        {
            isCinemaSubtitleMode = enabled;
            if (!enabled) HideModuleConcept();
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
#if UNITY_ANDROID && !UNITY_EDITOR
                          rect.localScale = Vector3.one * 0.00105f;
#else
                          rect.localScale = Vector3.one * 0.00072f;
#endif
                        rect.sizeDelta = new Vector2(880, 112);
                        if (backgroundImage != null)
                        {
                            backgroundImage.color = new Color(0.018f, 0.032f, 0.052f, 0.96f);
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
                            subtitleText.fontSize = 21;
                            subtitleText.fontStyle = FontStyles.Bold;
                            subtitleText.alignment = TextAlignmentOptions.Center;
                            subtitleText.lineSpacing = 1f;
                            var sRect = subtitleText.GetComponent<RectTransform>();
                            if (sRect != null)
                            {
                                sRect.anchorMin = new Vector2(0.035f, 0.08f);
                                sRect.anchorMax = new Vector2(0.965f, 0.92f);
                                sRect.offsetMin = new Vector2(18f, 0f);
                                sRect.offsetMax = new Vector2(-18f, 0f);
                            }
                        }
                        if (nextButtonRoot != null)
                        {
                            var buttonRect = nextButtonRoot.GetComponent<RectTransform>();
                            if (buttonRect != null)
                            {
                                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 1f);
                                buttonRect.pivot = new Vector2(0.5f, 0f);
                                buttonRect.sizeDelta = new Vector2(250f, 40f);
                                buttonRect.anchoredPosition = new Vector2(0f, 12f);
                            }
                            if (nextButtonText != null) nextButtonText.fontSize = 13f;
                        }
                    }
                    else
                    {
                        rect.localScale = hasCapturedHudScale ? defaultHudScale : Vector3.one * 0.0016f;
                        rect.sizeDelta = new Vector2(650, 150);
                        if (backgroundImage != null) backgroundImage.color = new Color(0.04f, 0.06f, 0.10f, 0.92f);
                        if (nextButtonRoot != null)
                        {
                            var buttonRect = nextButtonRoot.GetComponent<RectTransform>();
                            if (buttonRect != null)
                            {
                                buttonRect.anchorMin = buttonRect.anchorMax = new Vector2(0.5f, 1f);
                                buttonRect.pivot = new Vector2(0.5f, 0f);
                                buttonRect.sizeDelta = new Vector2(320f, 46f);
                                buttonRect.anchoredPosition = new Vector2(0f, 16f);
                            }
                            if (nextButtonText != null) nextButtonText.fontSize = 15f;
                        }
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
                string styledChunk = isCinemaSubtitleMode
                    ? $"<color=#4AD5E6><b>ARIA</b></color>  <color=#F3F7FA>{chunk}</color>"
                    : $"<color=#4AD5E6><b>ARIA:</b></color> <color=#F3F7FA>{chunk}</color>";
                yield return AnimateSubtitleChunk(styledChunk, displayTime);
            }

            // Natural pause at end of dialogue before clearing
            yield return new WaitForSecondsRealtime(1.2f);

            if (subtitleText != null)
            {
                subtitleText.text = "";
                if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 0f;
            }
            timedSubtitleRoutine = null;
        }

        private IEnumerator AnimateSubtitleChunk(string value, float displayTime)
        {
            if (subtitleText == null) yield break;
            if (subtitleCanvasGroup == null) subtitleCanvasGroup = subtitleText.GetComponent<CanvasGroup>();
            if (subtitleCanvasGroup == null) subtitleCanvasGroup = subtitleText.gameObject.AddComponent<CanvasGroup>();
            if (subtitleRect == null) subtitleRect = subtitleText.rectTransform;

            subtitleText.text = value;
            subtitleText.maxVisibleCharacters = 0;
            subtitleText.ForceMeshUpdate();
            int characterCount = subtitleText.textInfo.characterCount;
            Vector2 finalPosition = subtitleRect.anchoredPosition;
            subtitleRect.anchoredPosition = finalPosition + new Vector2(0f, -8f);
            subtitleCanvasGroup.alpha = 0f;

            float revealDuration = Mathf.Clamp(characterCount / 90f, 0.16f, 0.48f);
            float elapsed = 0f;
            while (elapsed < displayTime)
            {
                elapsed += Time.unscaledDeltaTime;
                float entrance = Mathf.Clamp01(elapsed / 0.20f);
                float reveal = Mathf.Clamp01(elapsed / revealDuration);
                subtitleCanvasGroup.alpha = Mathf.SmoothStep(0f, 1f, entrance);
                subtitleRect.anchoredPosition = Vector2.Lerp(finalPosition + new Vector2(0f, -8f), finalPosition, Mathf.SmoothStep(0f, 1f, entrance));
                subtitleText.maxVisibleCharacters = Mathf.CeilToInt(characterCount * reveal);
                yield return null;
            }

            subtitleText.maxVisibleCharacters = int.MaxValue;
            float fadeElapsed = 0f;
            while (fadeElapsed < 0.14f)
            {
                fadeElapsed += Time.unscaledDeltaTime;
                subtitleCanvasGroup.alpha = 1f - Mathf.Clamp01(fadeElapsed / 0.14f);
                yield return null;
            }
            subtitleCanvasGroup.alpha = 0f;
            subtitleRect.anchoredPosition = finalPosition;
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
            if (subtitleCanvasGroup != null) subtitleCanvasGroup.alpha = 0f;
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
                float currentFDistance = isCinemaSubtitleMode ? 0.84f : forwardDistance;
#if UNITY_ANDROID && !UNITY_EDITOR
                  // Keep subtitles at a comfortable headset focus distance and below the sight line.
                  currentVOffset = isCinemaSubtitleMode ? -0.43f : -0.36f;
                  currentFDistance = 1.35f;
#endif
                Vector3 targetPos = cam.position + cam.forward * currentFDistance + cam.up * currentVOffset;
                Quaternion targetRot = Quaternion.LookRotation(targetPos - cam.position);

                hudRoot.transform.position = Vector3.SmoothDamp(hudRoot.transform.position, targetPos, ref followVelocity, 0.10f);
                hudRoot.transform.rotation = Quaternion.Slerp(hudRoot.transform.rotation, targetRot, Time.deltaTime * smoothFollowSpeed);

                if (isCinemaSubtitleMode && conceptCardRoot != null && conceptCardRoot.activeSelf)
                {
                    Vector3 cardPosition = cam.position + cam.forward * 1.12f + cam.up * 0.39f;
                    conceptCardRoot.transform.position = Vector3.Lerp(conceptCardRoot.transform.position, cardPosition, Time.deltaTime * 8f);
                    conceptCardRoot.transform.rotation = Quaternion.Slerp(conceptCardRoot.transform.rotation, targetRot, Time.deltaTime * 8f);
                    AnimateConceptNodes();
                }

                if (isCinemaSubtitleMode && isNextButtonActive && nextButtonRoot != null && nextButtonRoot.activeSelf)
                {
                    Vector3 buttonPosition = cam.position + cam.forward * 0.96f + cam.right * 0.42f + cam.up * -0.42f;
                    nextButtonRoot.transform.position = Vector3.Lerp(nextButtonRoot.transform.position, buttonPosition, Time.deltaTime * 10f);
                    nextButtonRoot.transform.rotation = Quaternion.Slerp(nextButtonRoot.transform.rotation, targetRot, Time.deltaTime * 10f);
                }
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
                        if (Application.isPlaying) Destroy(allNextButtons[i]);
                        else DestroyImmediate(allNextButtons[i]);
                    }
                    nextButton = nextButtonRoot.GetComponent<Button>();
                    if (nextButton != null)
                    {
                        nextButton.onClick.RemoveAllListeners();
                        nextButton.onClick.AddListener(OnNextButtonClicked);
                    }
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

            if (subtitleText != null)
            {
                subtitleRect = subtitleText.rectTransform;
                if (subtitleCanvasGroup == null)
                {
                    subtitleCanvasGroup = subtitleText.GetComponent<CanvasGroup>();
                    if (subtitleCanvasGroup == null) subtitleCanvasGroup = subtitleText.gameObject.AddComponent<CanvasGroup>();
                }
            }

            if (hudRoot != null && !hasCapturedHudScale)
            {
                defaultHudScale = hudRoot.transform.localScale;
                hasCapturedHudScale = true;
            }

            EnsureConceptVisuals();
        }

        private void EnsureConceptVisuals()
        {
            if (canvas == null || conceptCardRoot != null) return;

            conceptCardRoot = new GameObject("Tour_ConceptCard", typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            conceptCardRoot.transform.SetParent(canvas.transform, false);
            var cardRect = conceptCardRoot.GetComponent<RectTransform>();
            cardRect.sizeDelta = new Vector2(700f, 150f);
            cardRect.localScale = Vector3.one * 0.00080f;

            var cardImage = conceptCardRoot.GetComponent<Image>();
            cardImage.color = new Color(0.018f, 0.035f, 0.060f, 0.96f);
            conceptCanvasGroup = conceptCardRoot.GetComponent<CanvasGroup>();
            conceptCanvasGroup.alpha = 0f;
            conceptCardRoot.SetActive(false);

            var accent = new GameObject("Accent", typeof(RectTransform), typeof(Image));
            accent.transform.SetParent(conceptCardRoot.transform, false);
            var accentRect = accent.GetComponent<RectTransform>();
            accentRect.anchorMin = new Vector2(0f, 1f);
            accentRect.anchorMax = new Vector2(1f, 1f);
            accentRect.pivot = new Vector2(0.5f, 1f);
            accentRect.sizeDelta = new Vector2(0f, 3f);
            accentRect.anchoredPosition = Vector2.zero;
            accent.GetComponent<Image>().color = new Color(0.12f, 0.72f, 0.86f, 1f);

            conceptTitleText = CreateConceptText("ModuleTitle", 18f, FontStyles.Bold,
                TextAlignmentOptions.MidlineLeft, new Color(0.45f, 0.90f, 0.96f, 1f),
                new Vector2(0f, 0.70f), new Vector2(1f, 1f), new Vector2(22f, 0f), new Vector2(-18f, -12f));
            conceptTitleText.transform.SetParent(conceptCardRoot.transform, false);

            conceptCaptionText = CreateConceptText("ModuleCaption", 13f, FontStyles.Normal,
                TextAlignmentOptions.MidlineLeft, new Color(0.74f, 0.82f, 0.88f, 1f),
                new Vector2(0f, 0.48f), new Vector2(1f, 0.73f), new Vector2(22f, 0f), new Vector2(-18f, 0f));
            conceptCaptionText.transform.SetParent(conceptCardRoot.transform, false);

            conceptNodeImages = new Image[4];
            conceptNodeTexts = new TextMeshProUGUI[4];
            conceptNodeRects = new RectTransform[4];
            conceptArrows = new TextMeshProUGUI[3];
            for (int i = 0; i < 4; i++)
            {
                var node = new GameObject($"ConceptNode_{i + 1}", typeof(RectTransform), typeof(Image), typeof(Outline));
                node.transform.SetParent(conceptCardRoot.transform, false);
                var nodeRect = node.GetComponent<RectTransform>();
                nodeRect.sizeDelta = new Vector2(126f, 36f);
                nodeRect.anchoredPosition = new Vector2(-228f + i * 152f, -43f);
                node.GetComponent<Image>().color = new Color(0.035f, 0.090f, 0.13f, 0.96f);
                var outline = node.GetComponent<Outline>();
                outline.effectColor = new Color(0.11f, 0.42f, 0.54f, 0.65f);
                outline.effectDistance = new Vector2(1f, 1f);

                var labelObject = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                labelObject.transform.SetParent(node.transform, false);
                var labelRect = labelObject.GetComponent<RectTransform>();
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = new Vector2(5f, 2f);
                labelRect.offsetMax = new Vector2(-5f, -2f);
                conceptNodeTexts[i] = labelObject.GetComponent<TextMeshProUGUI>();
                conceptNodeTexts[i].fontSize = 12f;
                conceptNodeTexts[i].fontStyle = FontStyles.Bold;
                conceptNodeTexts[i].alignment = TextAlignmentOptions.Center;
                conceptNodeTexts[i].color = new Color(0.90f, 0.95f, 0.98f, 1f);
                conceptNodeImages[i] = node.GetComponent<Image>();
                conceptNodeRects[i] = nodeRect;
                node.SetActive(false);

                if (i < 3)
                {
                    var arrow = CreateConceptText($"Arrow_{i + 1}", 16f, FontStyles.Bold,
                        TextAlignmentOptions.Center, new Color(0.28f, 0.65f, 0.74f, 0.9f),
                        Vector2.zero, Vector2.zero, Vector2.zero, Vector2.zero);
                    arrow.transform.SetParent(conceptCardRoot.transform, false);
                    var arrowRect = arrow.rectTransform;
                    arrowRect.sizeDelta = new Vector2(24f, 30f);
                    arrowRect.anchoredPosition = new Vector2(-152f + i * 152f, -43f);
                    arrow.text = "›";
                    conceptArrows[i] = arrow;
                }
            }
        }

        private TextMeshProUGUI CreateConceptText(string name, float size, FontStyles style,
            TextAlignmentOptions alignment, Color color, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 offsetMin, Vector2 offsetMax)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            var text = go.GetComponent<TextMeshProUGUI>();
            text.fontSize = size;
            text.fontStyle = style;
            text.alignment = alignment;
            text.color = color;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            var rect = text.rectTransform;
            rect.anchorMin = anchorMin;
            rect.anchorMax = anchorMax;
            rect.offsetMin = offsetMin;
            rect.offsetMax = offsetMax;
            return text;
        }

        public void ShowModuleConcept(int moduleNumber, string title, string caption, params string[] nodes)
        {
            EnsureUIStructure();
            if (conceptCardRoot == null || conceptTitleText == null || conceptNodeImages == null) return;

            conceptTitleText.text = $"MODULE {moduleNumber:00}   /   {title.ToUpperInvariant()}";
            conceptCaptionText.text = caption;
            conceptNodeCount = Mathf.Clamp(nodes != null ? nodes.Length : 0, 0, conceptNodeImages.Length);

            float gap = conceptNodeCount > 1 ? 460f / (conceptNodeCount - 1) : 0f;
            float width = conceptNodeCount >= 4 ? 126f : conceptNodeCount == 3 ? 148f : 180f;
            for (int i = 0; i < conceptNodeImages.Length; i++)
            {
                bool visible = i < conceptNodeCount;
                conceptNodeImages[i].gameObject.SetActive(visible);
                if (visible)
                {
                    conceptNodeRects[i].sizeDelta = new Vector2(width, 36f);
                    conceptNodeRects[i].anchoredPosition = new Vector2(-230f + i * gap, -43f);
                    conceptNodeTexts[i].text = nodes[i].ToUpperInvariant();
                }
            }

            for (int i = 0; i < conceptArrows.Length; i++)
            {
                bool visible = i < conceptNodeCount - 1;
                conceptArrows[i].gameObject.SetActive(visible);
                if (visible)
                {
                    conceptArrows[i].rectTransform.anchoredPosition = new Vector2(-230f + (i + 0.5f) * gap, -43f);
                }
            }

            conceptCardRoot.SetActive(true);
            if (conceptIntroRoutine != null) StopCoroutine(conceptIntroRoutine);
            conceptIntroRoutine = StartCoroutine(AnimateConceptCardIn());
        }

        public void HideModuleConcept()
        {
            if (conceptIntroRoutine != null)
            {
                StopCoroutine(conceptIntroRoutine);
                conceptIntroRoutine = null;
            }
            if (conceptCanvasGroup != null) conceptCanvasGroup.alpha = 0f;
            if (conceptCardRoot != null) conceptCardRoot.SetActive(false);
        }

        private IEnumerator AnimateConceptCardIn()
        {
            if (conceptCanvasGroup == null || conceptCardRoot == null) yield break;
            var rect = conceptCardRoot.GetComponent<RectTransform>();
            Vector3 targetScale = Vector3.one * 0.00080f;
            conceptCanvasGroup.alpha = 0f;
            rect.localScale = targetScale * 0.94f;

            float elapsed = 0f;
            while (elapsed < 0.28f)
            {
                elapsed += Time.unscaledDeltaTime;
                float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / 0.28f));
                conceptCanvasGroup.alpha = t;
                rect.localScale = Vector3.Lerp(targetScale * 0.94f, targetScale, t);
                yield return null;
            }
            conceptCanvasGroup.alpha = 1f;
            rect.localScale = targetScale;
            conceptIntroRoutine = null;
        }

        private void AnimateConceptNodes()
        {
            if (conceptNodeCount <= 0) return;
            int activeNode = Mathf.FloorToInt(Time.unscaledTime * 0.8f) % conceptNodeCount;
            for (int i = 0; i < conceptNodeCount; i++)
            {
                bool active = i == activeNode;
                conceptNodeImages[i].color = active
                    ? new Color(0.035f, 0.21f, 0.28f, 0.98f)
                    : new Color(0.035f, 0.075f, 0.10f, 0.92f);
                var outline = conceptNodeImages[i].GetComponent<Outline>();
                if (outline != null)
                    outline.effectColor = active
                        ? new Color(0.22f, 0.85f, 0.92f, 0.95f)
                        : new Color(0.11f, 0.35f, 0.42f, 0.55f);
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
            if (Time.unscaledTime - lastNextTriggerTime < 0.6f) return;
            lastNextTriggerTime = Time.unscaledTime;

            var action = onNextClickAction;
            action?.Invoke();
        }
    }
}
