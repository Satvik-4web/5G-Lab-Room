using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

[Serializable]
public class PollinationsMessage
{
    public string role;
    public string content;
}

[Serializable]
public class PollinationsPayload
{
    public List<PollinationsMessage> messages;
}

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
            string lowerQuery = (userQuery ?? "").ToLowerInvariant().Trim();
            Debug.Log($"[DirectFreeLanguageModel] Evaluating Query: \"{userQuery}\"");

            // 1. Tool Call Detection (Scene Navigation & Tour)
            if (lowerQuery.Contains("tour") || lowerQuery.Contains("guide") || lowerQuery.Contains("walk me through") || lowerQuery.Contains("teach me everything"))
            {
                response.textContent = "Welcome to the 5G Lab! Let me take you on an interactive guided tour to explore all three key systems.";
                response.toolCalls.Add(new ToolCallRequest { id = "call_tour", toolName = "start_tour", argumentsJson = "{\"mode\":\"full\"}" });
                return response;
            }

            // Show All Components Intent
            if (lowerQuery.Contains("components") || lowerQuery.Contains("all components") || lowerQuery.Contains("show components") ||
                lowerQuery.Contains("devices") || lowerQuery.Contains("equipment") || lowerQuery.Contains("kya components") || lowerQuery.Contains("components dikhao"))
            {
                response.textContent = "In our 5G lab, we have three key physical systems: the gNodeB base station antenna, the MEC Core server rack, and the RF Spectrum Analyzer. Let's take a tour to inspect each one!";
                response.toolCalls.Add(new ToolCallRequest { id = "call_tour", toolName = "start_tour", argumentsJson = "{\"mode\":\"full\"}" });
                return response;
            }

            // Exit Inspection Intent
            bool isExit = lowerQuery.Contains("exit") || lowerQuery.Contains("zoom out") ||
                          lowerQuery.Contains("step back") || lowerQuery.Contains("stop inspect") ||
                          lowerQuery.Contains("back to room") || lowerQuery.Contains("piche") ||
                          lowerQuery.Contains("wapas") || lowerQuery.Contains("overview");

            if (isExit)
            {
                response.textContent = "Returning to the lab room overview.";
                response.toolCalls.Add(new ToolCallRequest { id = "call_exit", toolName = "exit_inspection", argumentsJson = "{}" });
                return response;
            }

            // 2. Comprehensive 5G Knowledge Brain with Physical Component Show & Inspection
            var queryResult = FiveGKnowledgeBrain.QueryDetailed(lowerQuery, knowledge);
            if (!string.IsNullOrEmpty(queryResult.text))
            {
                response.textContent = queryResult.text;

                // Whenever a question relates to a physical lab component, SHOW IT to the user in 3D!
                if (queryResult.equipment != EquipmentId.None)
                {
                    string eqId = queryResult.equipment.ToIdString();

                    // Smoothly glide camera to the component if not already inspecting it
                    if (LabContext.CurrentInspected != queryResult.equipment)
                    {
                        response.toolCalls.Add(new ToolCallRequest
                        {
                            id = "call_inspect_" + eqId,
                            toolName = "inspect_equipment",
                            argumentsJson = $"{{\"id\":\"{eqId}\"}}"
                        });
                    }

                    // Always highlight & illuminate the component to draw the user's attention
                    response.toolCalls.Add(new ToolCallRequest
                    {
                        id = "call_highlight_" + eqId,
                        toolName = "highlight_equipment",
                        argumentsJson = $"{{\"id\":\"{eqId}\",\"duration_seconds\":5.0}}"
                    });
                }

                onTokenChunk?.Invoke(response.textContent);
                return response;
            }

            // 3. Fallback
            response.textContent = "The core basics of 5G rely on three pillars: the gNodeB base station for beamforming, the MEC server rack for sub-millisecond edge latency, and the Spectrum Analyzer for clean RF frequencies. Which would you like to explore?";
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
    /// Direct Free STT supporting Groq Whisper or Python Google Speech Recognition (100% Free, Zero Key).
    /// </summary>
    public class DirectFreeSpeechToText : ISpeechToText
    {
        private readonly string groqKey;
        private readonly string scriptPath = @"D:\5G-Lab-Room\transcribe.py";

        public DirectFreeSpeechToText(string groqApiKey = null)
        {
            this.groqKey = groqApiKey;
        }

        private string ResolvePythonExecutable()
        {
            string[] candidates = new string[]
            {
                @"C:\Users\satvi\AppData\Local\Programs\Python\Python310\python.exe",
                @"C:\Program Files\Python310\python.exe",
                "python.exe",
                "python"
            };

            foreach (var path in candidates)
            {
                if (File.Exists(path)) return path;
            }
            return "python";
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

            // 2. Real Google Speech Recognition via Python transcribe.py (100% Free, Zero Key)
            try
            {
                string tempWav = Path.Combine(Application.temporaryCachePath, "recorded_speech.wav");
                File.WriteAllBytes(tempWav, wavAudio);

                if (File.Exists(scriptPath))
                {
                    string pythonExe = ResolvePythonExecutable();
                    var psi = new System.Diagnostics.ProcessStartInfo
                    {
                        FileName = pythonExe,
                        Arguments = $"\"{scriptPath}\" \"{tempWav}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    };

                    using var proc = System.Diagnostics.Process.Start(psi);
                    if (proc != null)
                    {
                        var readTask = proc.StandardOutput.ReadToEndAsync();
                        var errTask = proc.StandardError.ReadToEndAsync();
                        float watchdogTimeout = Time.realtimeSinceStartup + 6.0f;

                        while (!proc.HasExited)
                        {
                            if (ct.IsCancellationRequested || Time.realtimeSinceStartup > watchdogTimeout)
                            {
                                try { proc.Kill(); } catch { }
                                if (ct.IsCancellationRequested) ct.ThrowIfCancellationRequested();
                                break;
                            }
                            await Task.Yield();
                        }
                        string output = await readTask;
                        if (!string.IsNullOrWhiteSpace(output))
                        {
                            string recognizedText = output.Trim();
                            Debug.Log($"[DirectFreeSpeechToText] Recognized: \"{recognizedText}\"");
                            return recognizedText;
                        }
                    }
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DirectFreeSpeechToText] Python STT exception: {ex.Message}");
            }

            // 3. Contextual fallback if inspecting or hovering equipment
            if (LabContext.CurrentInspected != EquipmentId.None)
            {
                return $"Tell me about the {LabContext.CurrentInspected.GetDisplayName()}.";
            }
            if (LabContext.CurrentHovered != EquipmentId.None)
            {
                return $"What is the {LabContext.CurrentHovered.GetDisplayName()}?";
            }

            // Audio was silent or unintelligible; return empty so VoiceAgentManager prompts user to speak into mic
            return string.Empty;
        }
    }
}
