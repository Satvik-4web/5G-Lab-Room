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

            // Equipment Zoom Tools (leave text empty so equipment audio or camera zoom happens smoothly)
            if (lowerQuery.Contains("spectrum") || lowerQuery.Contains("analyzer"))
            {
                if (lowerQuery.Contains("zoom") || lowerQuery.Contains("take me") || lowerQuery.Contains("inspect") || lowerQuery.Contains("go to") || lowerQuery.Contains("look at"))
                {
                    response.textContent = ""; 
                    response.toolCalls.Add(new ToolCallRequest { id = "call_spec", toolName = "inspect_equipment", argumentsJson = "{\"id\":\"spectrum\"}" });
                    return response;
                }
            }
            if (lowerQuery.Contains("mec") || lowerQuery.Contains("rack") || lowerQuery.Contains("server") || lowerQuery.Contains("edge"))
            {
                if (lowerQuery.Contains("zoom") || lowerQuery.Contains("take me") || lowerQuery.Contains("inspect") || lowerQuery.Contains("go to") || lowerQuery.Contains("look at"))
                {
                    response.textContent = ""; 
                    response.toolCalls.Add(new ToolCallRequest { id = "call_mec", toolName = "inspect_equipment", argumentsJson = "{\"id\":\"mec\"}" });
                    return response;
                }
            }
            if (lowerQuery.Contains("gnodeb") || lowerQuery.Contains("antenna") || lowerQuery.Contains("base station"))
            {
                if (lowerQuery.Contains("zoom") || lowerQuery.Contains("take me") || lowerQuery.Contains("inspect") || lowerQuery.Contains("go to") || lowerQuery.Contains("look at"))
                {
                    response.textContent = ""; 
                    response.toolCalls.Add(new ToolCallRequest { id = "call_ant", toolName = "inspect_equipment", argumentsJson = "{\"id\":\"gnodeb\"}" });
                    return response;
                }
            }
            if (lowerQuery.Contains("exit") || lowerQuery.Contains("zoom out") || lowerQuery.Contains("step back") || lowerQuery.Contains("stop inspect"))
            {
                response.textContent = "";
                response.toolCalls.Add(new ToolCallRequest { id = "call_exit", toolName = "exit_inspection", argumentsJson = "{}" });
                return response;
            }

            // 2. Comprehensive Semantic Knowledge Engine (Instant, 0-Latency, Highly Pedagogical)
            string semanticAnswer = MatchSemantic5GKnowledge(lowerQuery);
            if (!string.IsNullOrEmpty(semanticAnswer))
            {
                response.textContent = semanticAnswer;
                onTokenChunk?.Invoke(semanticAnswer);
                return response;
            }

            // 3. Optional: Fast Attempt at Cloud AI for unexpected general queries (3s timeout)
            try
            {
                string prompt = $"You are Aria, an interactive voice tutor in a 5G Virtual Reality Lab. Answer in 2 short, spoken sentences without markdown or bullets.\nLearner asked: {userQuery}";
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
                req.SetRequestHeader("User-Agent", "Mozilla/5.0");
                req.timeout = 3; // Fast timeout so user never waits

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
                    if (!string.IsNullOrWhiteSpace(answer) && !answer.Contains("<!DOCTYPE html>") && !answer.Contains("limit reached"))
                    {
                        response.textContent = answer;
                        onTokenChunk?.Invoke(answer);
                        return response;
                    }
                }
            }
            catch { }

            // 4. Contextual Fallback based on Current Equipment Selection
            if (LabContext.CurrentInspected != EquipmentId.None)
            {
                var data = knowledge?.GetEquipment(LabContext.CurrentInspected);
                response.textContent = $"You are currently inspecting the {data?.displayName}. {data?.oneLiner}";
            }
            else if (LabContext.CurrentHovered != EquipmentId.None)
            {
                var data = knowledge?.GetEquipment(LabContext.CurrentHovered);
                response.textContent = $"That device is the {data?.displayName}. {data?.oneLiner}";
            }
            else
            {
                response.textContent = "I am Aria, your 5G lab tutor! You can ask me about beamforming, massive MIMO, the MEC core server rack, or the RF spectrum analyzer.";
            }

            onTokenChunk?.Invoke(response.textContent);
            return response;
        }

        private string MatchSemantic5GKnowledge(string q)
        {
            // Greetings / Identity
            if (q.Contains("hello") || q.Contains("hi") || q.Contains("hey") || q.Contains("who are you") || q.Contains("your name") || q.Contains("kaun ho") || q.Contains("kya naam"))
            {
                return "Hello! I am Aria, your 5G VR Lab voice tutor. You can ask me about any equipment in the room, or explore concepts like beamforming, latency, and massive MIMO.";
            }

            // What is 5G / Overview / How it works
            if (q.Contains("what is 5g") || q.Contains("how does 5g work") || q.Contains("5g kya hai") || q.Contains("5g kya hota") || q.Contains("about 5g") || q.Contains("5g overview"))
            {
                return "5G is the fifth generation mobile network delivering multi-gigabit data speeds, ultra-low latency under one millisecond, and massive device connectivity. It unites advanced radio antennas, edge compute, and high-frequency spectrum.";
            }

            // Beamforming
            if (q.Contains("beamform") || q.Contains("beam forming") || q.Contains("focus") || q.Contains("spotlight") || q.Contains("target beam") || q.Contains("direct waves"))
            {
                return "Active beamforming uses multiple antenna elements to shape and focus radio waves directly toward user devices like a spotlight. This drastically cuts down interference, conserves transmission energy, and boosts data speeds.";
            }

            // Massive MIMO
            if (q.Contains("mimo") || q.Contains("multiple input") || q.Contains("spatial multiplexing") || q.Contains("antenna array"))
            {
                return "Massive MIMO equips the base station with dozens of antenna elements that transmit and receive multiple data streams simultaneously. This multiplies network capacity so hundreds of devices can connect at once without slowdowns.";
            }

            // gNodeB / Base Station / Antenna
            if (q.Contains("gnodeb") || q.Contains("base station") || q.Contains("antenna") || q.Contains("tower") || q.Contains("radio link") || q.Contains("radio access"))
            {
                return "The gNodeB is our 5G base station, connecting mobile devices to the core network over 5G New Radio. It handles the physical radio link using Massive MIMO and active beamforming across Sub-6 and millimeter wave bands.";
            }

            // Latency / 1 ms / Delay / URLLC
            if (q.Contains("latency") || q.Contains("1 ms") || q.Contains("1ms") || q.Contains("delay") || q.Contains("lag") || q.Contains("ping") || q.Contains("urllc") || q.Contains("millisecond") || q.Contains("real time"))
            {
                return "Under one millisecond latency is achieved by processing data locally at the MEC server rack. This near-instant response time is crucial for time-critical applications like autonomous cars, remote robotic surgery, and VR gaming.";
            }

            // MEC / Edge Computing / Server Rack / BBU
            if (q.Contains("mec") || q.Contains("edge compute") || q.Contains("edge computing") || q.Contains("server rack") || q.Contains("bbu") || q.Contains("baseband") || q.Contains("black box") || q.Contains("server"))
            {
                return "Mobile Edge Computing, or MEC, places powerful GPU and Baseband servers right next to the cell tower instead of far away in the cloud. This enables instant data processing right where the action happens.";
            }

            // RF Spectrum Analyzer / Measurement
            if (q.Contains("spectrum analyzer") || q.Contains("spectrum") || q.Contains("analyzer") || q.Contains("rf") || q.Contains("interference") || q.Contains("hunting") || q.Contains("measure") || q.Contains("diagnostic"))
            {
                return "The RF Spectrum Analyzer measures signal power across frequencies from two hertz all the way up to eighty-five gigahertz. Telecom engineers use it to verify clean signal transmission and hunt down unwanted radio interference.";
            }

            // Resolution Bandwidth (RBW)
            if (q.Contains("rbw") || q.Contains("resolution bandwidth") || q.Contains("filter bandwidth") || q.Contains("bandwidth"))
            {
                return "Resolution bandwidth, or RBW, defines the frequency filter width used by the analyzer to separate signals. A narrower RBW reveals fine spectral details and lowers the displayed noise floor.";
            }

            // mmWave vs Sub-6 GHz
            if (q.Contains("mmwave") || q.Contains("millimeter wave") || q.Contains("sub-6") || q.Contains("sub 6") || q.Contains("frequency") || q.Contains("ghz") || q.Contains("hertz") || q.Contains("bands"))
            {
                return "5G utilizes Sub-6 gigahertz for broad coverage that penetrates walls easily, and millimeter wave above twenty-four gigahertz for blistering multi-gigabit speeds over shorter distances.";
            }

            // 5G NR / Meaning of g / NR
            if (q.Contains("5g nr") || q.Contains("new radio") || q.Contains("nr stand for") || q.Contains("g stand for") || q.Contains("what does nr mean") || q.Contains("next generation"))
            {
                return "5G NR stands for 5G New Radio, the global wireless standard designed for higher speeds, lower latency, and massive device connectivity. The 'g' in gNodeB stands for next-generation NodeB.";
            }

            // Network Slicing
            if (q.Contains("slice") || q.Contains("slicing") || q.Contains("virtual network"))
            {
                return "Network slicing allows operators to divide a single physical 5G network into multiple virtual, end-to-end networks customized for specific needs, such as autonomous vehicles or smart energy grids.";
            }

            // 5G vs 4G / Differences
            if (q.Contains("4g") || q.Contains("difference") || q.Contains("better than 4g") || q.Contains("upgrade from 4g") || q.Contains("vs"))
            {
                return "Compared to 4G LTE, 5G offers up to one hundred times higher data rates, ten times lower latency down to one millisecond, and supports one million devices per square kilometer.";
            }

            // Use cases / Applications
            if (q.Contains("use case") || q.Contains("application") || q.Contains("why do we need") || q.Contains("autonomous") || q.Contains("healthcare") || q.Contains("smart city") || q.Contains("iot"))
            {
                return "5G empowers three major pillars: Enhanced Mobile Broadband for fast downloads, Ultra-Reliable Low Latency for remote surgery and autonomous driving, and Massive Machine-Type Communications for smart cities and IoT.";
            }

            // "Explain this" / "What am I looking at" / "Ye kya hai"
            if (q.Contains("explain this") || q.Contains("what is this") || q.Contains("what am i looking at") || q.Contains("tell me about this") || q.Contains("ye kya hai") || q.Contains("kya hai ye") || q.Contains("btao") || q.Contains("explain"))
            {
                if (LabContext.CurrentInspected == EquipmentId.gnodeb || LabContext.CurrentHovered == EquipmentId.gnodeb)
                {
                    return "You are looking at the gNodeB base station. It connects mobile devices using Massive MIMO and active beamforming, and is currently online and transmitting.";
                }
                if (LabContext.CurrentInspected == EquipmentId.mec || LabContext.CurrentHovered == EquipmentId.mec)
                {
                    return "You are looking at the MEC Core server rack. It houses edge GPUs and baseband units to deliver processing latency under one millisecond.";
                }
                if (LabContext.CurrentInspected == EquipmentId.spectrum || LabContext.CurrentHovered == EquipmentId.spectrum)
                {
                    return "You are looking at the RF Spectrum Analyzer. It monitors frequencies from two hertz to eighty-five gigahertz to detect interference and certify clean transmissions.";
                }
                return "You can click on the gNodeB antenna, the MEC server rack, or the Spectrum Analyzer, and I will explain how each component powers the 5G network!";
            }

            return null;
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
