using System;
using System.Collections.Generic;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    [Serializable]
    internal class ChatRequestPayload
    {
        public List<ChatMessage> messages;
        public string system;
        public List<ToolDefinition> tools;
        public int maxTokens;
    }

    [Serializable]
    internal class ToolCallPayload
    {
        public string id;
        public string name;
        public string inputJson;
    }

    [Serializable]
    internal class ChatResponsePayload
    {
        public string text;
        public List<ToolCallPayload> toolCalls;
    }

    public class ClaudeLanguageModel : ILanguageModel
    {
        private readonly ProxyHttpClient client;
        private readonly string endpoint;
        private readonly float timeout;
        private readonly int maxTokens;

        public ClaudeLanguageModel(ProxyHttpClient client, string endpoint = "/chat", float timeout = 8f, int maxTokens = 150)
        {
            this.client = client;
            this.endpoint = endpoint;
            this.timeout = timeout;
            this.maxTokens = maxTokens;
        }

        public async Task<LLMResponse> GenerateResponseAsync(
            List<ChatMessage> messages,
            string systemPrompt,
            List<ToolDefinition> tools,
            Action<string> onTokenChunk,
            CancellationToken ct)
        {
            var req = new ChatRequestPayload
            {
                messages = messages ?? new List<ChatMessage>(),
                system = systemPrompt ?? "",
                tools = tools ?? new List<ToolDefinition>(),
                maxTokens = maxTokens
            };

            string jsonPayload = JsonUtility.ToJson(req);
            string responseJson = await client.PostJsonAsync(endpoint, jsonPayload, timeout, ct);

            LLMResponse result = new LLMResponse();
            if (string.IsNullOrEmpty(responseJson)) return result;

            try
            {
                var parsed = JsonUtility.FromJson<ChatResponsePayload>(responseJson);
                if (parsed != null)
                {
                    result.textContent = parsed.text ?? "";
                    if (parsed.toolCalls != null)
                    {
                        foreach (var tc in parsed.toolCalls)
                        {
                            result.toolCalls.Add(new ToolCallRequest
                            {
                                id = tc.id,
                                toolName = tc.name,
                                argumentsJson = tc.inputJson
                            });
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[ClaudeLanguageModel] Failed to parse chat response: {ex.Message}");
                result.textContent = responseJson;
            }

            onTokenChunk?.Invoke(result.textContent);
            return result;
        }
    }
}
