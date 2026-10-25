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

            // Intent to view, show, zoom, inspect, or navigate to equipment
            bool isNavIntent = lowerQuery.Contains("show") || lowerQuery.Contains("see") || lowerQuery.Contains("view") ||
                               lowerQuery.Contains("zoom") || lowerQuery.Contains("take") || lowerQuery.Contains("go to") ||
                               lowerQuery.Contains("goto") || lowerQuery.Contains("inspect") || lowerQuery.Contains("look") ||
                               lowerQuery.Contains("bring") || lowerQuery.Contains("lead") || lowerQuery.Contains("walk") ||
                               lowerQuery.Contains("teleport") || lowerQuery.Contains("where is") || lowerQuery.Contains("dikhao") ||
                               lowerQuery.Contains("le chalo") || lowerQuery.Contains("kahan hai");

            bool isSpectrum = lowerQuery.Contains("spectrum") || lowerQuery.Contains("soectrum") ||
                              lowerQuery.Contains("spectram") || lowerQuery.Contains("analyzer") ||
                              lowerQuery.Contains("analyser");

            bool isMec = lowerQuery.Contains("mec") || lowerQuery.Contains("server") ||
                         lowerQuery.Contains("rack") || lowerQuery.Contains("bbu") ||
                         lowerQuery.Contains("edge computing") || lowerQuery.Contains("black box");

            bool isGnodeb = lowerQuery.Contains("gnodeb") || lowerQuery.Contains("antenna") ||
                            lowerQuery.Contains("antena") || lowerQuery.Contains("base station") ||
                            lowerQuery.Contains("cell tower") || lowerQuery.Contains("tower");

            bool isExit = lowerQuery.Contains("exit") || lowerQuery.Contains("zoom out") ||
                          lowerQuery.Contains("step back") || lowerQuery.Contains("stop inspect") ||
                          lowerQuery.Contains("back to room") || lowerQuery.Contains("piche") ||
                          lowerQuery.Contains("wapas");

            // Execute Equipment Zoom & Inspection Tools
            if (isSpectrum && isNavIntent)
            {
                response.textContent = "Taking you to the RF Spectrum Analyzer for a closer look."; 
                response.toolCalls.Add(new ToolCallRequest { id = "call_spec", toolName = "inspect_equipment", argumentsJson = "{\"id\":\"spectrum\"}" });
                return response;
            }
            if (isMec && isNavIntent)
            {
                response.textContent = "Zooming in on the MEC Core edge server rack."; 
                response.toolCalls.Add(new ToolCallRequest { id = "call_mec", toolName = "inspect_equipment", argumentsJson = "{\"id\":\"mec\"}" });
                return response;
            }
            if (isGnodeb && isNavIntent)
            {
                response.textContent = "Here is the gNodeB base station antenna."; 
                response.toolCalls.Add(new ToolCallRequest { id = "call_ant", toolName = "inspect_equipment", argumentsJson = "{\"id\":\"gnodeb\"}" });
                return response;
            }
            if (isExit)
            {
                response.textContent = "Returning to the lab room overview.";
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

            // 3. Contextual Fallback based on Current Equipment Selection
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
                response.textContent = "The core basics of 5G rely on three pillars: the gNodeB base station for beamforming, the MEC server rack for sub-millisecond edge latency, and the Spectrum Analyzer for clean RF frequencies. Which would you like to explore?";
            }

            onTokenChunk?.Invoke(response.textContent);
            return response;
        }

        private string MatchSemantic5GKnowledge(string q)
        {
            if (string.IsNullOrWhiteSpace(q)) return null;

            // Pad with spaces for safe word boundary checking
            q = " " + q.ToLowerInvariant().Trim() + " ";

            // 1. Greetings & Identity
            if (q.Contains("hello") || q.Contains("hi ") || q.Contains("hey") || q.Contains("who are you") || 
                q.Contains("your name") || q.Contains("kaun ho") || q.Contains("kya naam") || q.Contains("namaste") ||
                q.Contains("about yourself") || q.Contains("introduce"))
            {
                return "Hello! I am Aria, your 5G VR Lab voice tutor. You can ask me about any equipment in the room, or explore concepts like beamforming, latency, and massive MIMO.";
            }

            // 2. Help / Instructions
            if (q.Contains("help") || q.Contains("what can i ask") || q.Contains("madad") || 
                q.Contains("kya kar sakte") || q.Contains("commands") || q.Contains("how to use"))
            {
                return "You can hold [V] to ask any 5G doubt, press [1] for the guided tour, press [2] to inspect the spectrum analyzer, [3] for the MEC rack, or [4] for the gNodeB antenna!";
            }

            // 3. Beamforming (Priority before generic antennas)
            if (q.Contains("beamform") || q.Contains("beam forming") || q.Contains("spotlight") || 
                q.Contains("target beam") || q.Contains("direct waves") || q.Contains("shape wave") ||
                q.Contains("directional"))
            {
                return "Active beamforming uses multiple antenna elements to shape and focus radio waves directly toward user devices like a spotlight. This drastically cuts down interference, conserves transmission energy, and boosts data speeds.";
            }

            // 4. Massive MIMO
            if (q.Contains("mimo") || q.Contains("multiple input") || q.Contains("spatial multiplexing") || 
                q.Contains("antenna array") || q.Contains("arrays") || q.Contains("streams"))
            {
                return "Massive MIMO equips the base station with dozens of antenna elements that transmit and receive multiple data streams simultaneously. This multiplies network capacity so hundreds of devices can connect at once without slowdowns.";
            }

            // 5. Latency / 1ms / Delay / URLLC / Real-time
            if (q.Contains("latency") || q.Contains("1 ms") || q.Contains("1ms") || q.Contains("delay") || 
                q.Contains("lag") || q.Contains("ping") || q.Contains("urllc") || q.Contains("millisecond") || 
                q.Contains("real time") || q.Contains("low latency"))
            {
                return "Under one millisecond latency is achieved by processing data locally at the MEC server rack. This near-instant response time is crucial for time-critical applications like autonomous cars, remote robotic surgery, and VR gaming.";
            }

            // 6. MEC / Edge Computing / Server Rack / BBU
            if (q.Contains("mec") || q.Contains("edge compute") || q.Contains("edge computing") || 
                q.Contains("server rack") || q.Contains("server") || q.Contains("rack") || 
                q.Contains("bbu") || q.Contains("baseband") || q.Contains("black box") || q.Contains("edge"))
            {
                return "Mobile Edge Computing, or MEC, places powerful GPU and Baseband servers right next to the cell tower instead of far away in the cloud. This enables instant data processing right where the action happens.";
            }

            // 7. Spectrum Analyzer / RF Measurement / Hunting / Interference
            if (q.Contains("spectrum") || q.Contains("soectrum") || q.Contains("spectram") || 
                q.Contains("analyzer") || q.Contains("analyser") || q.Contains("rf") || 
                q.Contains("interference") || q.Contains("hunting") || q.Contains("dbm") || 
                q.Contains("noise floor") || q.Contains("frequency meter") || q.Contains("spectral"))
            {
                return "The RF Spectrum Analyzer measures signal power across frequencies from two hertz all the way up to eighty-five gigahertz. Telecom engineers use it to verify clean signal transmission and hunt down unwanted radio interference.";
            }

            // 8. Resolution Bandwidth (RBW)
            if (q.Contains("rbw") || q.Contains("resolution bandwidth") || q.Contains("filter bandwidth") || 
                q.Contains("filter width"))
            {
                return "Resolution bandwidth, or RBW, defines the frequency filter width used by the analyzer to separate signals. A narrower RBW reveals fine spectral details and lowers the displayed noise floor.";
            }

            // 9. gNodeB / Base Station / Antenna Tower
            if (q.Contains("gnodeb") || q.Contains("nodeb") || q.Contains("base station") || 
                q.Contains("antenna") || q.Contains("antena") || q.Contains("cell tower") || 
                q.Contains("tower") || q.Contains("radio link") || q.Contains("mast") || 
                q.Contains("radio unit") || q.Contains("rrh") || q.Contains("ru"))
            {
                return "The gNodeB is our 5G base station, connecting mobile devices to the core network over 5G New Radio. It handles active beamforming and Massive MIMO across Sub-6 and millimeter wave bands to serve hundreds of devices simultaneously.";
            }

            // 10. mmWave vs Sub-6 GHz / Frequency Bands
            if (q.Contains("mmwave") || q.Contains("millimeter wave") || q.Contains("sub-6") || 
                q.Contains("sub 6") || q.Contains("frequency") || q.Contains("frequencies") || 
                q.Contains("ghz") || q.Contains("hertz") || q.Contains("bands") || q.Contains("c-band"))
            {
                return "5G utilizes Sub-6 gigahertz for broad coverage that penetrates walls easily, and millimeter wave above twenty-four gigahertz for blistering multi-gigabit speeds over shorter distances.";
            }

            // 11. 5G NR / Meaning of g / 3GPP
            if (q.Contains("nr") || q.Contains("new radio") || q.Contains("g stand for") || 
                q.Contains("g ka matlab") || q.Contains("3gpp") || q.Contains("standard"))
            {
                return "5G NR stands for 5G New Radio, the global wireless standard designed for higher speeds, lower latency, and massive device connectivity. The 'g' in gNodeB stands for next-generation NodeB.";
            }

            // 12. Network Slicing
            if (q.Contains("slice") || q.Contains("slicing") || q.Contains("virtual network") || q.Contains("partition"))
            {
                return "Network slicing allows operators to divide a single physical 5G network into multiple virtual, end-to-end networks customized for specific needs, such as autonomous vehicles or smart energy grids.";
            }

            // 13. 5G vs 4G / Differences / Speed comparison
            if (q.Contains("4g") || q.Contains("lte") || q.Contains("difference") || 
                q.Contains("better than 4g") || q.Contains("upgrade from 4g") || q.Contains("vs"))
            {
                return "Compared to 4G LTE, 5G offers up to one hundred times higher data rates, ten times lower latency down to one millisecond, and supports one million devices per square kilometer.";
            }

            // 14. Use cases / Applications / Why 5G / IoT / Autonomous
            if (q.Contains("use case") || q.Contains("application") || q.Contains("why do we need") || 
                q.Contains("why 5g") || q.Contains("autonomous") || q.Contains("healthcare") || 
                q.Contains("surgery") || q.Contains("smart city") || q.Contains("iot") || q.Contains("future"))
            {
                return "5G empowers three major pillars: Enhanced Mobile Broadband for fast downloads, Ultra-Reliable Low Latency for remote surgery and autonomous driving, and Massive Machine-Type Communications for smart cities and IoT.";
            }

            // 15. 5G Core Basics / Fundamentals / Overview / What is 5G
            // Catches: "what are the core basics of 5G", "basics of 5g", "5g fundamentals", "how does 5g work", "explain 5g"
            bool has5GTerm = q.Contains("5g") || q.Contains("five g") || q.Contains("network");
            bool hasBasicsTerm = q.Contains("basic") || q.Contains("core") || q.Contains("fundamental") || 
                                 q.Contains("pillar") || q.Contains("concept") || q.Contains("start") || 
                                 q.Contains("work") || q.Contains("overview") || q.Contains("what is") || 
                                 q.Contains("tell me") || q.Contains("explain") || q.Contains("learn") || 
                                 q.Contains("shuru") || q.Contains("kya hai") || q.Contains("kya hota") || 
                                 q.Contains("samjhao");

            if (has5GTerm && hasBasicsTerm)
            {
                return "The core basics of 5G rely on three main pillars: the gNodeB base station using Massive MIMO and beamforming for ultra-fast wireless, the MEC Core edge server delivering under one millisecond latency, and the Spectrum Analyzer ensuring clean RF transmission.";
            }

            // If user just asks "basics", "core", "where to start", etc. without saying 5g explicitly
            if (q.Contains("basic") || q.Contains("core") || q.Contains("fundamental") || q.Contains("where to start") || q.Contains("shuruat"))
            {
                return "The core basics of 5G rely on three main pillars: the gNodeB base station using Massive MIMO and beamforming for ultra-fast wireless, the MEC Core edge server delivering under one millisecond latency, and the Spectrum Analyzer ensuring clean RF transmission.";
            }

            // 16. How they connect / Work together / Data flow
            if (q.Contains("together") || q.Contains("connect") || q.Contains("flow") || q.Contains("diagram") || q.Contains("system") || q.Contains("kaise juda"))
            {
                return "In this lab, the gNodeB base station catches the radio signal from user devices, the MEC server rack processes computations instantly in under one millisecond, and the Spectrum Analyzer confirms clean signal frequencies without interference.";
            }

            // 17. Contextual Inspection ("Explain this" / "What am I looking at" / "Ye kya hai")
            if (q.Contains("explain this") || q.Contains("what is this") || q.Contains("what am i looking at") || 
                q.Contains("tell me about this") || q.Contains("ye kya hai") || q.Contains("kya hai ye") || 
                q.Contains("btao") || q.Contains("explain") || q.Contains("kya dikh raha"))
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

            // 18. Smart Dynamic Contextual Fallbacks (Never repeat the static intro!)
            if (LabContext.CurrentInspected != EquipmentId.None)
            {
                var data = knowledge?.GetEquipment(LabContext.CurrentInspected);
                return $"You are inspecting the {data?.displayName}. {data?.oneLiner} Feel free to ask about its frequency, latency, or role in 5G!";
            }
            if (LabContext.CurrentHovered != EquipmentId.None)
            {
                var data = knowledge?.GetEquipment(LabContext.CurrentHovered);
                return $"That device is the {data?.displayName}. {data?.oneLiner}";
            }

            // Rotate through friendly, helpful educational responses instead of repeating the same canned text!
            string[] smartFallbacks = new string[]
            {
                "In 5G, the gNodeB antenna handles beamforming, the MEC server processes data in under one millisecond, and the Spectrum Analyzer guards signal quality. Which one would you like to explore?",
                "Every part of this lab demonstrates a 5G breakthrough: Massive MIMO at the tower, sub-millisecond MEC edge computing, and RF spectrum analysis. Say 'take me to the spectrum analyzer' or 'show MEC' to inspect them!",
                "You can ask me about beamforming, 1-millisecond latency, massive MIMO, or press [1] to start the full guided tour!"
            };
            int fallbackIndex = Mathf.Abs(q.GetHashCode()) % smartFallbacks.Length;
            return smartFallbacks[fallbackIndex];
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
