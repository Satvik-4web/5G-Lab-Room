using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.Networking;

namespace FiveGLab.VoiceAgent
{
    public class ProxyHttpClient
    {
        private readonly string baseUrl;
        private readonly string authToken;

        public ProxyHttpClient(string baseUrl, string authToken = "")
        {
            this.baseUrl = baseUrl?.TrimEnd('/') ?? "http://localhost:3000";
            this.authToken = authToken ?? "";
        }

        public async Task<string> PostJsonAsync(string endpoint, string jsonPayload, float timeoutSeconds, CancellationToken ct)
        {
            string url = $"{baseUrl}{endpoint}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(authToken))
            {
                req.SetRequestHeader("X-Agent-Secret", authToken);
            }

            req.timeout = Mathf.CeilToInt(timeoutSeconds);

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested)
                {
                    req.Abort();
                    ct.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"[ProxyHttpClient] Error ({req.responseCode}): {req.error} - {req.downloadHandler?.text}");
            }

            return req.downloadHandler.text;
        }

        public async Task<string> PostWavAudioAsync(string endpoint, byte[] wavBytes, float timeoutSeconds, CancellationToken ct)
        {
            string url = $"{baseUrl}{endpoint}";
            List<IMultipartFormSection> formData = new List<IMultipartFormSection>
            {
                new MultipartFormFileSection("audio", wavBytes, "audio.wav", "audio/wav")
            };

            using var req = UnityWebRequest.Post(url, formData);
            req.downloadHandler = new DownloadHandlerBuffer();

            if (!string.IsNullOrEmpty(authToken))
            {
                req.SetRequestHeader("X-Agent-Secret", authToken);
            }

            req.timeout = Mathf.CeilToInt(timeoutSeconds);

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested)
                {
                    req.Abort();
                    ct.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"[ProxyHttpClient] STT Error ({req.responseCode}): {req.error} - {req.downloadHandler?.text}");
            }

            return req.downloadHandler.text;
        }

        public async Task<AudioClip> PostTtsAudioAsync(string endpoint, string jsonPayload, float timeoutSeconds, CancellationToken ct)
        {
            string url = $"{baseUrl}{endpoint}";
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonPayload);

            using var req = new UnityWebRequest(url, "POST");
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerAudioClip(url, AudioType.WAV);
            req.SetRequestHeader("Content-Type", "application/json");

            if (!string.IsNullOrEmpty(authToken))
            {
                req.SetRequestHeader("X-Agent-Secret", authToken);
            }

            req.timeout = Mathf.CeilToInt(timeoutSeconds);

            var op = req.SendWebRequest();
            while (!op.isDone)
            {
                if (ct.IsCancellationRequested)
                {
                    req.Abort();
                    ct.ThrowIfCancellationRequested();
                }
                await Task.Yield();
            }

            if (req.result != UnityWebRequest.Result.Success)
            {
                throw new Exception($"[ProxyHttpClient] TTS Error ({req.responseCode}): {req.error}");
            }

            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            return clip;
        }
    }
}
