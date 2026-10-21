using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    public class MockSpeechToText : ISpeechToText
    {
        public static string NextCannedTranscript = null;

        public async Task<string> TranscribeAsync(byte[] wavAudio, CancellationToken ct)
        {
            await Task.Delay(350, ct); // Simulate quick local inference

            if (!string.IsNullOrEmpty(NextCannedTranscript))
            {
                string t = NextCannedTranscript;
                NextCannedTranscript = null;
                return t;
            }

            // Default fallback if mic was recorded without typed text override
            if (LabContext.CurrentInspected != EquipmentId.None)
            {
                return $"Tell me about this {LabContext.CurrentInspected.GetDisplayName()}.";
            }
            if (LabContext.CurrentHovered != EquipmentId.None)
            {
                return $"What is this {LabContext.CurrentHovered.GetDisplayName()}?";
            }

            return "What can you teach me about this 5G lab?";
        }
    }

    public class MockLanguageModel : ILanguageModel
    {
        private readonly LabKnowledgeBase knowledge;

        public MockLanguageModel(LabKnowledgeBase knowledge)
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
            await Task.Delay(400, ct); // Simulate quick streaming inference

            string userQuery = "";
            for (int i = messages.Count - 1; i >= 0; i--)
            {
                if (messages[i].role == "user")
                {
                    userQuery = messages[i].content?.ToLowerInvariant() ?? "";
                    break;
                }
            }

            LLMResponse response = new LLMResponse();

            // Check for tool call triggers in mock
            if (userQuery.Contains("tour") || userQuery.Contains("guide") || userQuery.Contains("walk me through"))
            {
                response.textContent = "Welcome to the 5G Lab! Let me take you on a guided tour to see how the whole system connects together.";
                response.toolCalls.Add(new ToolCallRequest
                {
                    id = "mock_call_tour",
                    toolName = "start_tour",
                    argumentsJson = "{\"mode\":\"full\"}"
                });
            }
            else if (userQuery.Contains("take me to") || userQuery.Contains("zoom into") || userQuery.Contains("inspect"))
            {
                EquipmentId target = EquipmentId.None;
                if (userQuery.Contains("spectrum") || userQuery.Contains("analyzer")) target = EquipmentId.spectrum;
                else if (userQuery.Contains("rack") || userQuery.Contains("mec") || userQuery.Contains("server")) target = EquipmentId.mec;
                else if (userQuery.Contains("gnodeb") || userQuery.Contains("antenna") || userQuery.Contains("base station")) target = EquipmentId.gnodeb;

                if (target != EquipmentId.None)
                {
                    response.textContent = $"Let me bring you closer to the {target.GetDisplayName()}.";
                    response.toolCalls.Add(new ToolCallRequest
                    {
                        id = "mock_call_inspect",
                        toolName = "inspect_equipment",
                        argumentsJson = $"{{\"id\":\"{target.ToIdString()}\"}}"
                    });
                }
                else
                {
                    response.textContent = "Which equipment would you like to inspect? We have the gNodeB base station, the MEC core server rack, and the RF spectrum analyzer.";
                }
            }
            else if (userQuery.Contains("exit") || userQuery.Contains("zoom out") || userQuery.Contains("go back"))
            {
                response.textContent = "Stepping back out to the lab view.";
                response.toolCalls.Add(new ToolCallRequest
                {
                    id = "mock_call_exit",
                    toolName = "exit_inspection",
                    argumentsJson = "{}"
                });
            }
            else
            {
                // Answer queries contextually using knowledge base
                EquipmentId relevantId = EquipmentId.None;
                if (userQuery.Contains("beamform") || userQuery.Contains("gnodeb") || userQuery.Contains("antenna") || userQuery.Contains("mimo"))
                {
                    relevantId = EquipmentId.gnodeb;
                }
                else if (userQuery.Contains("latency") || userQuery.Contains("mec") || userQuery.Contains("rack") || userQuery.Contains("edge") || userQuery.Contains("bbu"))
                {
                    relevantId = EquipmentId.mec;
                }
                else if (userQuery.Contains("spectrum") || userQuery.Contains("analyzer") || userQuery.Contains("rbw") || userQuery.Contains("frequency") || userQuery.Contains("interference"))
                {
                    relevantId = EquipmentId.spectrum;
                }
                else if (LabContext.CurrentInspected != EquipmentId.None)
                {
                    relevantId = LabContext.CurrentInspected;
                }
                else if (LabContext.CurrentHovered != EquipmentId.None)
                {
                    relevantId = LabContext.CurrentHovered;
                }

                if (relevantId != EquipmentId.None && knowledge != null)
                {
                    var data = knowledge.GetEquipment(relevantId);
                    if (data != null)
                    {
                        if (userQuery.Contains("beamform"))
                        {
                            response.textContent = "Active beamforming uses multiple antenna elements to shape and focus radio waves directly at your device, like a lens on a flashlight. This saves power, reduces interference, and boosts speeds.";
                        }
                        else if (userQuery.Contains("latency"))
                        {
                            response.textContent = "The MEC Core achieves under one millisecond latency by processing data right here at the edge rather than sending packets to distant cloud servers. This makes real-time applications like autonomous vehicles and VR possible.";
                        }
                        else if (userQuery.Contains("rbw") || userQuery.Contains("resolution bandwidth"))
                        {
                            response.textContent = "Resolution bandwidth is the filter size of the spectrum analyzer. A narrow RBW reveals closely spaced frequencies and lowers the noise floor, while a wider RBW sweeps faster.";
                        }
                        else
                        {
                            response.textContent = $"You are viewing the {data.displayName}. {data.oneLiner} It operates with {string.Join(", ", data.keySpecs ?? Array.Empty<string>())}. Would you like to know how it works in detail?";
                        }
                    }
                    else
                    {
                        response.textContent = "I am ready to help you explore our 5G lab. Feel free to inspect the gNodeB antenna, the MEC server rack, or the RF spectrum analyzer!";
                    }
                }
                else
                {
                    response.textContent = "Welcome to the 5G Lab! You can point at any equipment and ask me what it does, or say 'start the tour' to let me guide you through the whole system.";
                }
            }

            onTokenChunk?.Invoke(response.textContent);
            return response;
        }
    }

    public class MockTextToSpeech : ITextToSpeech
    {
        public async Task<AudioClip> SynthesizeAsync(string text, string voice, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(text)) return null;
            await Task.Delay(150, ct);

            // Calculate realistic speaking duration (~150 words/min = 2.5 words/sec)
            int wordCount = Mathf.Max(1, text.Split(' ', System.StringSplitOptions.RemoveEmptyEntries).Length);
            float duration = Mathf.Clamp(wordCount / 2.5f, 1.2f, 12f);

            int sampleRate = 16000;
            int totalSamples = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            // Generate soft warm harmonic pulse simulating gentle speech modulation
            float baseFreq = 180f; // Warm vocal pitch
            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / sampleRate;
                float envelope = Mathf.Sin(t * Mathf.PI / duration); // Smooth fade in and out
                float modulation = 0.5f + 0.5f * Mathf.Sin(t * 8f * Mathf.PI); // Syllable modulation
                float carrier = Mathf.Sin(t * 2f * Mathf.PI * baseFreq) * 0.12f;
                float overtone = Mathf.Sin(t * 4f * Mathf.PI * baseFreq) * 0.04f;
                samples[i] = (carrier + overtone) * envelope * modulation;
            }

            AudioClip clip = AudioClip.Create("MockSpeechClip", totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
