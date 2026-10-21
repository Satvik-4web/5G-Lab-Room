using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    [Serializable]
    internal class SttResponse
    {
        public string text;
    }

    public class WhisperSpeechToText : ISpeechToText
    {
        private readonly ProxyHttpClient client;
        private readonly string endpoint;
        private readonly float timeout;

        public WhisperSpeechToText(ProxyHttpClient client, string endpoint = "/stt", float timeout = 10f)
        {
            this.client = client;
            this.endpoint = endpoint;
            this.timeout = timeout;
        }

        public async Task<string> TranscribeAsync(byte[] wavAudio, CancellationToken ct)
        {
            if (wavAudio == null || wavAudio.Length == 0) return string.Empty;

            string json = await client.PostWavAudioAsync(endpoint, wavAudio, timeout, ct);
            if (string.IsNullOrEmpty(json)) return string.Empty;

            try
            {
                var resp = JsonUtility.FromJson<SttResponse>(json);
                return resp != null ? resp.text?.Trim() : string.Empty;
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[WhisperSpeechToText] Failed to parse STT response: {ex.Message}");
                return string.Empty;
            }
        }
    }
}
