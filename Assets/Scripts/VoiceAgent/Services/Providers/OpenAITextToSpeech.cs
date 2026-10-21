using System;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    [Serializable]
    internal class TtsRequestPayload
    {
        public string text;
        public string voice;
    }

    public class OpenAITextToSpeech : ITextToSpeech
    {
        private readonly ProxyHttpClient client;
        private readonly string endpoint;
        private readonly float timeout;

        public OpenAITextToSpeech(ProxyHttpClient client, string endpoint = "/tts", float timeout = 8f)
        {
            this.client = client;
            this.endpoint = endpoint;
            this.timeout = timeout;
        }

        public async Task<AudioClip> SynthesizeAsync(string text, string voice, CancellationToken ct)
        {
            if (string.IsNullOrEmpty(text)) return null;

            var req = new TtsRequestPayload
            {
                text = text,
                voice = string.IsNullOrEmpty(voice) ? "alloy" : voice
            };

            string jsonPayload = JsonUtility.ToJson(req);
            return await client.PostTtsAudioAsync(endpoint, jsonPayload, timeout, ct);
        }
    }
}
