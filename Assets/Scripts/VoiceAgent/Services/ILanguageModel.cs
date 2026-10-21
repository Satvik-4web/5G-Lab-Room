using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace FiveGLab.VoiceAgent
{
    [Serializable]
    public class ChatMessage
    {
        public string role; // "user", "assistant", "system", "tool"
        public string content;
        public string toolCallId;
        public string name;

        public static ChatMessage User(string text) => new ChatMessage { role = "user", content = text };
        public static ChatMessage Assistant(string text) => new ChatMessage { role = "assistant", content = text };
        public static ChatMessage System(string text) => new ChatMessage { role = "system", content = text };
        public static ChatMessage ToolResult(string toolCallId, string toolName, string result) => 
            new ChatMessage { role = "tool", toolCallId = toolCallId, name = toolName, content = result };
    }

    [Serializable]
    public class ToolDefinition
    {
        public string name;
        public string description;
        public string parametersJson;
    }

    [Serializable]
    public class ToolCallRequest
    {
        public string id;
        public string toolName;
        public string argumentsJson;
    }

    [Serializable]
    public class LLMResponse
    {
        public string textContent = "";
        public List<ToolCallRequest> toolCalls = new List<ToolCallRequest>();
        public bool HasToolCalls => toolCalls != null && toolCalls.Count > 0;
    }

    /// <summary>
    /// Contract for LLM generation supporting streaming chunks and tool calls.
    /// </summary>
    public interface ILanguageModel
    {
        Task<LLMResponse> GenerateResponseAsync(
            List<ChatMessage> messages,
            string systemPrompt,
            List<ToolDefinition> tools,
            Action<string> onTokenChunk,
            CancellationToken ct);
    }
}
