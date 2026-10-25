using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    public class VoiceAgentManager : MonoBehaviour
    {
        public static VoiceAgentManager Instance { get; private set; }

        [Header("Configuration")]
        [SerializeField] private AgentConfig config;
        [SerializeField] private LabKnowledgeBase knowledgeBase;

        [Header("Component References")]
        [SerializeField] private MicrophoneCapture micCapture;
        [SerializeField] private AgentSpeaker speaker;
        [SerializeField] private LabContextProvider contextProvider;
        [SerializeField] private AgentHUD hud;

        private AgentState currentState = AgentState.Idle;
        private CancellationTokenSource activeCts;

        private ISpeechToText sttService;
        private ILanguageModel llmService;
        private ITextToSpeech ttsService;

        private AgentToolRegistry toolRegistry;
        private ConversationMemory memory;

        public AgentConfig Config => config;
        public LabKnowledgeBase KnowledgeBase => knowledgeBase;
        public AgentState CurrentState => currentState;
        public ConversationMemory Memory => memory;
        public AgentHUD HUD => hud;
        public AgentSpeaker Speaker => speaker;

        public event Action<AgentState> OnStateChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else
            {
                Destroy(gameObject);
                return;
            }

            if (config == null)
            {
                config = Resources.Load<AgentConfig>("VoiceAgent/AgentConfig");
                if (config == null) config = ScriptableObject.CreateInstance<AgentConfig>();
            }

            if (knowledgeBase == null)
            {
                knowledgeBase = Resources.Load<LabKnowledgeBase>("VoiceAgent/LabKnowledge");
            }

            if (micCapture == null) micCapture = GetComponentInChildren<MicrophoneCapture>();
            if (speaker == null) speaker = GetComponentInChildren<AgentSpeaker>();
            if (contextProvider == null) contextProvider = GetComponentInChildren<LabContextProvider>();
            if (hud == null) hud = GetComponentInChildren<AgentHUD>();

            if (contextProvider != null && knowledgeBase != null)
            {
                contextProvider.SetKnowledgeBase(knowledgeBase);
            }

            InitializeServices();
        }

        private void InitializeServices()
        {
            memory = new ConversationMemory(config.maxTurnsPerSession);
            toolRegistry = new AgentToolRegistry();
            LabTools.RegisterDefaultTools(toolRegistry, knowledgeBase);

            if (config.useMockServices)
            {
                if (config.debugLogging) Debug.Log("[VoiceAgentManager] Running with Free Direct In-Engine AI (Pollinations LLM + Google Neural Voice - No proxy needed!).");
                sttService = new DirectFreeSpeechToText(config.proxyAuthToken);
                llmService = new DirectFreeLanguageModel(knowledgeBase);
                ttsService = new DirectFreeTextToSpeech();
            }
            else
            {
                if (config.debugLogging) Debug.Log($"[VoiceAgentManager] Connecting to proxy at {config.proxyBaseUrl}");
                var client = new ProxyHttpClient(config.proxyBaseUrl, config.proxyAuthToken);
                sttService = new WhisperSpeechToText(client, config.sttEndpoint, config.sttTimeoutSeconds);
                llmService = new ClaudeLanguageModel(client, config.chatEndpoint, config.llmTimeoutSeconds, config.maxTokensPerReply);
                ttsService = new OpenAITextToSpeech(client, config.ttsEndpoint, config.ttsTimeoutSeconds);
            }

            if (speaker != null)
            {
                speaker.OnSentenceStarted += (text) =>
                {
                    if (hud != null) hud.ShowSubtitle(text);
                };

                speaker.OnSpeakingComplete += () =>
                {
                    if (currentState == AgentState.Speaking)
                    {
                        bool isTour = GuidedTourController.Instance != null && GuidedTourController.Instance.IsTourActive;
                        SetState(isTour ? AgentState.Touring : AgentState.Idle);
                    }
                };
            }
        }

        public void SetState(AgentState newState)
        {
            if (currentState != newState)
            {
                currentState = newState;
                if (config.debugLogging) Debug.Log($"[VoiceAgentManager] State: {newState}");
                OnStateChanged?.Invoke(newState);
                if (hud != null) hud.UpdateStateVisuals(newState);
            }
        }

        public void OnPttDown()
        {
            // Barge-in: cut off any speaking or in-flight thinking
            if (currentState == AgentState.Speaking || currentState == AgentState.Thinking || currentState == AgentState.Transcribing)
            {
                CancelInFlightOperations();
                if (speaker != null) speaker.StopSpeaking();
            }

            // Duck any playing equipment audio
            AgentSpeaker.DuckOtherAudioSources();

            if (micCapture != null && micCapture.StartRecording())
            {
                SetState(AgentState.Listening);
            }
        }

        public void OnPttUp()
        {
            if (currentState != AgentState.Listening) return;

            if (micCapture != null)
            {
                byte[] wavData = micCapture.StopRecordingAndGetWav();
                if (wavData != null && wavData.Length > 0)
                {
                    _ = ProcessVoiceInputAsync(wavData);
                }
                else
                {
                    if (hud != null) hud.ShowSubtitle("Hold [V] while speaking into your microphone.");
                    SetState(AgentState.Idle);
                }
            }
        }

        public async Task ProcessTypedInputAsync(string typedText)
        {
            if (string.IsNullOrWhiteSpace(typedText)) return;
            CancelInFlightOperations();
            activeCts = new CancellationTokenSource();

            await ProcessTextQueryAsync(typedText.Trim(), activeCts.Token);
        }

        private async Task ProcessVoiceInputAsync(byte[] wavData)
        {
            CancelInFlightOperations();
            activeCts = new CancellationTokenSource();
            var ct = activeCts.Token;

            try
            {
                SetState(AgentState.Transcribing);
                string transcript = await sttService.TranscribeAsync(wavData, ct);

                if (string.IsNullOrWhiteSpace(transcript))
                {
                    var mic = GetComponentInChildren<MicrophoneCapture>();
                    string micName = mic != null ? mic.GetFriendlyDeviceName() : "Mic";
                    if (hud != null) hud.ShowSubtitle($"<color=#FF9800>I couldn't hear any voice on <b>{micName}</b>. Speak louder or press [M] to switch mic!</color>");
                    SetState(AgentState.Idle);
                    return;
                }

                if (config.debugLogging) Debug.Log($"[VoiceAgentManager] Transcribed: \"{transcript}\"");
                if (hud != null) hud.ShowSubtitle($"<color=#8AB4F8><b>You:</b> \"{transcript}\"</color>");
                await ProcessTextQueryAsync(transcript, ct);
            }
            catch (OperationCanceledException)
            {
                if (config.debugLogging) Debug.Log("[VoiceAgentManager] Voice input cancelled.");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VoiceAgentManager] Error processing voice turn: {ex}");
                if (hud != null) hud.ShowSubtitle("I had trouble processing that. Please try again.");
                SetState(AgentState.Idle);
            }
        }

        private async Task ProcessTextQueryAsync(string userText, CancellationToken ct)
        {
            if (config.debugLogging) Debug.Log($"[VoiceAgentManager] Processing Query: \"{userText}\"");

            // 1. Check if Guided Tour is expecting a quiz response
            if (GuidedTourController.Instance != null && GuidedTourController.Instance.IsTourActive)
            {
                if (GuidedTourController.Instance.SubmitSpokenAnswer(userText, out string feedback))
                {
                    memory.AddUserMessage(userText);
                    memory.AddAssistantMessage(feedback);
                    await SynthesizeAndSpeakAsync(feedback, ct);
                    return;
                }
            }

            // 2. Normal LLM Conversation Loop
            memory.AddUserMessage(userText);
            SetState(AgentState.Thinking);

            string liveContext = contextProvider != null 
                ? contextProvider.BuildLiveContextString(memory) 
                : "";

            string systemPrompt = $"{config.baseSystemPrompt}\n\n{liveContext}";
            var tools = toolRegistry != null ? toolRegistry.GetToolDefinitions() : null;

            var llmResponse = await llmService.GenerateResponseAsync(
                new List<ChatMessage>(memory.History),
                systemPrompt,
                tools,
                null,
                ct);

            ct.ThrowIfCancellationRequested();

            // 3. Execute any tool calls
            if (llmResponse.HasToolCalls)
            {
                foreach (var toolCall in llmResponse.toolCalls)
                {
                    if (config.debugLogging) Debug.Log($"[VoiceAgentManager] Executing Tool: {toolCall.toolName}({toolCall.argumentsJson})");
                    string toolResult = await toolRegistry.ExecuteToolAsync(toolCall.toolName, toolCall.argumentsJson);
                    memory.AddToolResult(toolCall.id, toolCall.toolName, toolResult);
                }
            }

            // 4. Synthesize spoken response (ONLY if speechText is not empty)
            string speechText = CleanSpokenText(llmResponse.textContent);
            if (!string.IsNullOrEmpty(speechText))
            {
                memory.AddAssistantMessage(speechText);
                await SynthesizeAndSpeakAsync(speechText, ct);
            }
            else
            {
                SetState(AgentState.Idle);
            }
        }

        private async Task SynthesizeAndSpeakAsync(string text, CancellationToken ct)
        {
            SetState(AgentState.Speaking);

            // Split into sentences for smooth streaming audio playback
            string[] sentences = Regex.Split(text, @"(?<=[.?!])\s+");
            foreach (var sentence in sentences)
            {
                if (string.IsNullOrWhiteSpace(sentence)) continue;
                ct.ThrowIfCancellationRequested();

                try
                {
                    AudioClip clip = await ttsService.SynthesizeAsync(sentence.Trim(), config.ttsVoice, ct);
                    if (clip != null && speaker != null)
                    {
                        speaker.EnqueueSpeech(clip, sentence.Trim());
                    }
                    else if (hud != null)
                    {
                        hud.ShowSubtitle(sentence.Trim());
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[VoiceAgentManager] TTS synthesis failed for chunk: {ex.Message}");
                    if (hud != null) hud.ShowSubtitle(sentence);
                }
            }
        }

        public void SpeakNarrativeDirect(string text, Action onComplete)
        {
            _ = SpeakNarrativeDirectAsync(text, onComplete);
        }

        private async Task SpeakNarrativeDirectAsync(string text, Action onComplete)
        {
            CancelInFlightOperations();
            activeCts = new CancellationTokenSource();

            try
            {
                SetState(AgentState.Speaking);
                AudioClip clip = await ttsService.SynthesizeAsync(text, config.ttsVoice, activeCts.Token);
                if (clip != null && speaker != null)
                {
                    void OnFinish()
                    {
                        speaker.OnSpeakingComplete -= OnFinish;
                        onComplete?.Invoke();
                    }
                    speaker.OnSpeakingComplete += OnFinish;
                    speaker.EnqueueSpeech(clip, text);
                }
                else
                {
                    if (hud != null) hud.ShowSubtitle(text);
                    await Task.Delay(3000);
                    onComplete?.Invoke();
                }
            }
            catch
            {
                onComplete?.Invoke();
            }
        }

        public void CancelInFlightOperations()
        {
            if (activeCts != null)
            {
                activeCts.Cancel();
                activeCts.Dispose();
                activeCts = null;
            }
        }

        private string CleanSpokenText(string raw)
        {
            if (string.IsNullOrEmpty(raw)) return "";
            // Remove markdown asterisks, hashes, backticks
            string clean = Regex.Replace(raw, @"[\*#`_>\[\]]", "");
            return clean.Trim();
        }

        private void OnDestroy()
        {
            CancelInFlightOperations();
        }
    }
}
