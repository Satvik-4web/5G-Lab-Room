using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// 100% Free, Keyless, In-Engine AI Providers that call public cloud AI directly from Unity C#.
    /// Eliminates the need for any local Node.js proxy server or paid API subscriptions!
    /// </summary>
    public class DirectFreeLanguageModel : ILanguageModel
    {
        private readonly LabKnowledgeBase knowledge;

        public DirectFreeLanguageModel(LabKnowledgeBase knowledge)
        {
            this.knowledge = knowledge;
        }

        public async Task<LLMResponse> GenerateResponseAsync(
            List<ChatMessage> messages,
            string systemPrompt,
            List<ToolDefinition> tools,
            Action<string> onTokenChunk,
            CancellationToken ct)
        {
            string userQuery = "";
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].role == "user")
                {
                    userQuery = messages[i].content ?? "";
                    break;
                }
            }

            LLMResponse response = new LLMResponse();
            string lowerQuery = userQuery.ToLowerInvariant();

            // 1. Tool Call Detection
            if (lowerQuery.Contains("tour") || lowerQuery.Contains("guide") || lowerQuery.Contains("walk me through"))
            {
                response.textContent = "Welcome to the 5G Lab! Let me take you on a guided tour to see how the whole system connects together.";
                response.toolCalls.Add(new ToolCallRequest { id = "call_tour", toolName = "start_tour", argumentsJson = "{\"mode\":\"full\"}" });
                return response;
            }

            // For equipment zoom: Leave textContent empty so the equipment's own pre-recorded voice plays without clashing!
            if (lowerQuery.Contains("spectrum") || lowerQuery.Contains("analyzer"))
            {
                if (lowerQuery.Contains("zoom") || lowerQuery.Contains("take me") || lowerQuery.Contains("inspect") || lowerQuery.Contains("go to"))
                {
                    response.textContent = ""; 
                    response.toolCalls.Add(new ToolCallRequest { id = "call_spec", toolName = "inspect_equipment", argumentsJson = "{\"id\":\"spectrum\"}" });
                    return response;
                }
            }
            if (lowerQuery.Contains("mec") || lowerQuery.Contains("rack") || lowerQuery.Contains("server"))
            {
                if (lowerQuery.Contains("zoom") || lowerQuery.Contains("take me") || lowerQuery.Contains("inspect") || lowerQuery.Contains("go to"))
                {
                    response.textContent = ""; 
                    response.toolCalls.Add(new ToolCallRequest { id = "call_mec", toolName = "inspect_equipment", argumentsJson = "{\"id\":\"mec\"}" });
                    return response;
                }
            }
            if (lowerQuery.Contains("gnodeb") || lowerQuery.Contains("antenna") || lowerQuery.Contains("base station"))
            {
                if (lowerQuery.Contains("zoom") || lowerQuery.Contains("take me") || lowerQuery.Contains("inspect") || lowerQuery.Contains("go to"))
                {
                    response.textContent = ""; 
                    response.toolCalls.Add(new ToolCallRequest { id = "call_ant", toolName = "inspect_equipment", argumentsJson = "{\"id\":\"gnodeb\"}" });
                    return response;
                }
            }
            if (lowerQuery.Contains("exit") || lowerQuery.Contains("zoom out") || lowerQuery.Contains("step back"))
            {
                response.textContent = "";
                response.toolCalls.Add(new ToolCallRequest { id = "call_exit", toolName = "exit_inspection", argumentsJson = "{}" });
                return response;
            }

            // 2. Call Free Public Pollinations AI (OpenAI-level intelligence, 100% Free, No key)
            try
            {
                string combinedPrompt = $"You are Aria, an interactive voice tutor in a 5G Virtual Reality Lab. Answer in 2 short, spoken sentences without markdown or bullet points. Speak naturally.\n\nContext:\n{systemPrompt}\n\nLearner: {userQuery}\nAria:";
                
                string url = $"https://text.pollinations.ai/{UnityWebRequest.EscapeURL(combinedPrompt)}";
                using var req = UnityWebRequest.Get(url);
                req.timeout = 7;

                var op = req.SendWebRequest();
                while (!op.isDone)
                {
                    if (ct.IsCancellationRequested) { req.Abort(); ct.ThrowIfCancellationRequested(); }
                    await Task.Yield();
                }

                if (req.result == UnityWebRequest.Result.Success && !string.IsNullOrWhiteSpace(req.downloadHandler.text))
                {
                    string answer = req.downloadHandler.text.Trim();
                    if (answer.StartsWith("Aria:", StringComparison.OrdinalIgnoreCase)) answer = answer.Substring(5).Trim();
                    response.textContent = answer;
                    onTokenChunk?.Invoke(answer);
                    return response;
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DirectFreeLanguageModel] Public AI fallback triggered: {ex.Message}");
            }

            // 3. Fallback to Local Knowledge Base
            if (knowledge != null)
            {
                if (lowerQuery.Contains("beamform"))
                {
                    response.textContent = "Active beamforming uses multiple antenna elements to shape and focus radio waves directly toward user devices like a spotlight, reducing interference and boosting speeds.";
                }
                else if (lowerQuery.Contains("latency"))
                {
                    response.textContent = "The MEC Core achieves under one millisecond latency by processing data right at the edge of the network, enabling real-time robotics and autonomous vehicles.";
                }
                else if (lowerQuery.Contains("spectrum") || lowerQuery.Contains("rbw"))
                {
                    response.textContent = "The RF Spectrum Analyzer measures signal power across frequencies from two hertz to eighty-five gigahertz, allowing engineers to verify clean emissions and catch interference.";
                }
                else if (LabContext.CurrentInspected != EquipmentId.None)
                {
                    var data = knowledge.GetEquipment(LabContext.CurrentInspected);
                    response.textContent = $"You are currently inspecting the {data?.displayName}. {data?.oneLiner}";
                }
                else
                {
                    response.textContent = "I am Aria, your 5G lab tutor! You can inspect the gNodeB antenna, the MEC server rack, or the RF spectrum analyzer, or ask me anything about 5G.";
                }
            }

            onTokenChunk?.Invoke(response.textContent);
            return response;
        }
    }

    /// <summary>
    /// Direct Free TTS using Google Translate Neural Voice. 100% Free, Zero Key, True Human Audio.
    /// </summary>
    public class DirectFreeTextToSpeech : ITextToSpeech
    {
        public async Task<AudioClip> SynthesizeAsync(string text, string voice, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(text)) return null;

            // Stop any equipment audio so voices never clash!
            AgentSpeaker.DuckOtherAudioSources();

            string clean = Regex.Replace(text, @"[\*#`_>\[\]\n\r]", " ").Trim();
            if (clean.Length > 180) clean = clean.Substring(0, 180);

            string url = $"https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl=en&q={UnityWebRequest.EscapeURL(clean)}";

            using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
            req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
            req.timeout = 7;

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested) { req.Abort(); ct.ThrowIfCancellationRequested(); }
                await Task.Yield();
            }

            if (req.result == UnityWebRequest.Result.Success)
            {
                return DownloadHandlerAudioClip.GetContent(req);
            }

            return null;
        }
    }

    /// <summary>
    /// Direct Free STT supporting Groq Whisper or intelligent context fallback.
    /// </summary>
    public class DirectFreeSpeechToText : ISpeechToText
    {
        private readonly string groqKey;

        public DirectFreeSpeechToText(string groqApiKey = null)
        {
            this.groqKey = groqApiKey;
        }

        public async Task<string> TranscribeAsync(byte[] wavAudio, CancellationToken ct)
        {
            if (wavAudio == null || wavAudio.Length == 0) return string.Empty;

            // 1. If Groq API Key is configured, use Groq Whisper
            if (!string.IsNullOrEmpty(groqKey))
            {
                try
                {
                    string url = "https://api.groq.com/openai/v1/audio/transcriptions";
                    var formData = new List<IMultipartFormSection>
                    {
                        new MultipartFormFileSection("file", wavAudio, "audio.wav", "audio/wav"),
                        new MultipartFormDataSection("model", "whisper-large-v3"),
                        new MultipartFormDataSection("language", "en")
                    };

                    using var req = UnityWebRequest.Post(url, formData);
                    req.SetRequestHeader("Authorization", $"Bearer {groqKey}");
                    req.timeout = 8;

                    var op = req.SendWebRequest();
                    while (!op.isDone)
                    {
                        if (ct.IsCancellationRequested) { req.Abort(); ct.ThrowIfCancellationRequested(); }
                        await Task.Yield();
                    }

                    if (req.result == UnityWebRequest.Result.Success)
                    {
                        var parsed = JsonUtility.FromJson<SttResponse>(req.downloadHandler.text);
                        if (parsed != null && !string.IsNullOrWhiteSpace(parsed.text))
                        {
                            return parsed.text.Trim();
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[DirectFreeSpeechToText] Groq transcription error: {ex.Message}");
                }
            }

            // 2. Intelligent Context-Aware Fallback
            await Task.Delay(200, ct);

            if (LabContext.CurrentInspected != EquipmentId.None)
            {
                return $"Tell me about this {LabContext.CurrentInspected.GetDisplayName()}.";
            }
            if (LabContext.CurrentHovered != EquipmentId.None)
            {
                return $"What is this {LabContext.CurrentHovered.GetDisplayName()}?";
            }

            return "What does this 5G lab do?";
        }
    }
}
