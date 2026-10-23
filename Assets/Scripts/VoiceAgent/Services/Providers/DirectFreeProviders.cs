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

            // 2. Call Free Public Pollinations AI via JSON POST (100% Free, Zero Key)
            try
            {
                string prompt = $"You are Aria, an interactive voice tutor in a 5G Virtual Reality Lab. Answer in 2 short, spoken sentences without markdown, asterisks, or bullet points. Speak naturally.\nLearner asked: {userQuery}";
                var payloadObj = new PollinationsPayload
                {
                    messages = new List<PollinationsMessage>
                    {
                        new PollinationsMessage { role = "user", content = prompt }
                    }
                };

                string jsonBody = JsonUtility.ToJson(payloadObj);
                byte[] bodyBytes = Encoding.UTF8.GetBytes(jsonBody);

                using var req = new UnityWebRequest("https://text.pollinations.ai/", "POST");
                req.uploadHandler = new UploadHandlerRaw(bodyBytes);
                req.downloadHandler = new DownloadHandlerBuffer();
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("User-Agent", "Mozilla/5.0 (Windows NT 10.0; Win64; x64)");
                req.timeout = 10;

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
                    answer = Regex.Replace(answer, @"[\*#`_>\[\]]", "").Trim();
                    if (!string.IsNullOrWhiteSpace(answer) && !answer.Contains("<!DOCTYPE html>"))
                    {
                        response.textContent = answer;
                        onTokenChunk?.Invoke(answer);
                        return response;
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[DirectFreeLanguageModel] Public AI fallback triggered: {ex.Message}");
            }

            // 3. Fallback to Local 5G Knowledge Base (Instant & Offline)
            if (knowledge != null)
            {
                if (lowerQuery.Contains("beamform") || lowerQuery.Contains("focus") || lowerQuery.Contains("spotlight"))
                {
                    response.textContent = "Active beamforming uses multiple antenna elements to shape and focus radio waves directly toward user devices like a spotlight, reducing interference and boosting data speeds.";
                }
                else if (lowerQuery.Contains("mimo"))
                {
                    response.textContent = "Massive MIMO employs large arrays of antenna elements to transmit and receive multiple data streams simultaneously on the exact same frequency band.";
                }
                else if (lowerQuery.Contains("gnodeb") || lowerQuery.Contains("base station") || lowerQuery.Contains("antenna") || lowerQuery.Contains("tower"))
                {
                    response.textContent = "The gNodeB is our 5G base station. It connects mobile devices to the core network over 5G New Radio using Massive MIMO and high-performance radio units.";
                }
                else if (lowerQuery.Contains("latency") || lowerQuery.Contains("1 ms") || lowerQuery.Contains("delay") || lowerQuery.Contains("speed"))
                {
                    response.textContent = "Under one millisecond latency is achieved by placing the MEC Core right next to the base station, enabling instant responses for robotics and autonomous vehicles.";
                }
                else if (lowerQuery.Contains("mec") || lowerQuery.Contains("edge") || lowerQuery.Contains("server") || lowerQuery.Contains("rack") || lowerQuery.Contains("bbu"))
                {
                    response.textContent = "The Mobile Edge Computing server rack processes heavy compute loads and baseband data right at the edge of the network, eliminating round-trips to distant cloud servers.";
                }
                else if (lowerQuery.Contains("spectrum") || lowerQuery.Contains("analyzer") || lowerQuery.Contains("rf") || lowerQuery.Contains("measure"))
                {
                    response.textContent = "The RF Spectrum Analyzer measures signal power across frequencies from two hertz to eighty-five gigahertz, allowing engineers to verify clean emissions and catch interference.";
                }
                else if (lowerQuery.Contains("rbw") || lowerQuery.Contains("resolution") || lowerQuery.Contains("bandwidth"))
                {
                    response.textContent = "Resolution bandwidth, or RBW, defines the frequency resolution of the analyzer. A narrower RBW distinguishes closely spaced signals and lowers the display noise floor.";
                }
                else if (lowerQuery.Contains("mmwave") || lowerQuery.Contains("millimeter") || lowerQuery.Contains("frequency") || lowerQuery.Contains("ghz"))
                {
                    response.textContent = "5G operates across Sub-6 gigahertz for wide geographic coverage, and millimeter wave above twenty-four gigahertz for multi-gigabit throughput.";
                }
                else if (lowerQuery.Contains("nr") || lowerQuery.Contains("new radio") || lowerQuery.Contains("stand for"))
                {
                    response.textContent = "5G NR stands for 5G New Radio, the global wireless standard designed for higher speeds, lower latency, and massive device connectivity.";
                }
                else if (LabContext.CurrentInspected != EquipmentId.None)
                {
                    var data = knowledge.GetEquipment(LabContext.CurrentInspected);
                    response.textContent = $"You are currently inspecting the {data?.displayName}. {data?.oneLiner}";
                }
                else if (LabContext.CurrentHovered != EquipmentId.None)
                {
                    var data = knowledge.GetEquipment(LabContext.CurrentHovered);
                    response.textContent = $"That is the {data?.displayName}. {data?.oneLiner}";
                }
                else
                {
                    response.textContent = "5G combines high-speed radio from the gNodeB, under one millisecond computing from the MEC Core, and precision RF diagnostics from the Spectrum Analyzer. What would you like to explore?";
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
                        while (!proc.HasExited)
                        {
                            if (ct.IsCancellationRequested)
                            {
                                try { proc.Kill(); } catch { }
                                ct.ThrowIfCancellationRequested();
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
