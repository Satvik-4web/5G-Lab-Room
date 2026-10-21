using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    public delegate Task<string> ToolExecutionDelegate(string argumentsJson);

    public class AgentToolRegistry
    {
        private readonly Dictionary<string, (ToolDefinition definition, ToolExecutionDelegate handler)> tools = 
            new Dictionary<string, (ToolDefinition, ToolExecutionDelegate)>();

        public void RegisterTool(string name, string description, string parametersJson, ToolExecutionDelegate handler)
        {
            tools[name] = (new ToolDefinition
            {
                name = name,
                description = description,
                parametersJson = parametersJson
            }, handler);
        }

        public List<ToolDefinition> GetToolDefinitions()
        {
            var list = new List<ToolDefinition>();
            foreach (var kvp in tools.Values)
            {
                list.Add(kvp.definition);
            }
            return list;
        }

        public async Task<string> ExecuteToolAsync(string toolName, string argumentsJson)
        {
            if (tools.TryGetValue(toolName, out var entry))
            {
                try
                {
                    return await entry.handler(argumentsJson);
                }
                catch (Exception ex)
                {
                    Debug.LogError($"[AgentToolRegistry] Error executing tool '{toolName}': {ex}");
                    return $"{{\"error\":\"{ex.Message}\"}}";
                }
            }
            return $"{{\"error\":\"Tool '{toolName}' not found\"}}";
        }
    }
}
