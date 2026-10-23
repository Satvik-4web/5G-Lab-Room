using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEditor;
using FiveGLab.VoiceAgent;

public class EndToEndVoiceTest
{
    [MenuItem("5G Lab/Run End To End Voice Test")]
    public static void Run()
    {
        Debug.Log("[EndToEndVoiceTest] Starting End-to-End Voice Agent Verification...");
        
        string wavPath = Path.Combine(Application.dataPath, "AntennaTTS.wav");
        if (!File.Exists(wavPath))
        {
            Debug.LogError($"[EndToEndVoiceTest] WAV file not found at: {wavPath}");
            return;
        }

        byte[] wavBytes = File.ReadAllBytes(wavPath);
        Debug.Log($"[EndToEndVoiceTest] Loaded audio file ({wavBytes.Length} bytes).");

        var kb = Resources.Load<LabKnowledgeBase>("VoiceAgent/LabKnowledge");
        var stt = new DirectFreeSpeechToText();
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(15));

        Task.Run(async () =>
        {
            try
            {
                Debug.Log("[EndToEndVoiceTest] Step 1: Testing Speech-to-Text transcription...");
                string transcript = await stt.TranscribeAsync(wavBytes, cts.Token);
                Debug.Log($"[EndToEndVoiceTest] SUCCESS! Transcribed: \"{transcript}\"");

                Debug.Log("[EndToEndVoiceTest] Step 2: Testing LLM Response Generation...");
                var llm = new DirectFreeLanguageModel(kb);

                var messages = new System.Collections.Generic.List<ChatMessage>
                {
                    new ChatMessage { role = "user", content = transcript }
                };

                var resp = await llm.GenerateResponseAsync(messages, "You are Aria in 5G Lab.", null, null, cts.Token);
                Debug.Log($"[EndToEndVoiceTest] SUCCESS! Aria LLM Reply: \"{resp.textContent}\"");

                Debug.Log("[EndToEndVoiceTest] >>> ALL TESTS PASSED SUCCESSFULLY! <<<");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[EndToEndVoiceTest] FAILED with exception: {ex}");
            }
        }).Wait();
    }
}
