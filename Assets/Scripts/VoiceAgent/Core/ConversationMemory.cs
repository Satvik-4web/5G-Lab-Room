using System;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    public class ConversationMemory
    {
        private readonly List<ChatMessage> history = new List<ChatMessage>();
        private readonly HashSet<EquipmentId> visitedEquipment = new HashSet<EquipmentId>();
        private readonly int maxTurns;

        public int QuizScore { get; set; } = 0;
        public int TotalQuizQuestions { get; set; } = 0;
        public IReadOnlyList<ChatMessage> History => history;

        public ConversationMemory(int maxTurns = 10)
        {
            this.maxTurns = Mathf.Max(4, maxTurns);
        }

        public void AddUserMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            history.Add(ChatMessage.User(text.Trim()));
            TrimHistoryIfNeeded();
        }

        public void AddAssistantMessage(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return;
            history.Add(ChatMessage.Assistant(text.Trim()));
            TrimHistoryIfNeeded();
        }

        public void AddToolResult(string toolCallId, string toolName, string result)
        {
            history.Add(ChatMessage.ToolResult(toolCallId, toolName, result));
            TrimHistoryIfNeeded();
        }

        public void MarkVisited(EquipmentId id)
        {
            if (id != EquipmentId.None) visitedEquipment.Add(id);
        }

        public bool HasVisited(EquipmentId id) => visitedEquipment.Contains(id);

        public int VisitedCount => visitedEquipment.Count;

        public void Clear()
        {
            history.Clear();
            visitedEquipment.Clear();
            QuizScore = 0;
            TotalQuizQuestions = 0;
        }

        private void TrimHistoryIfNeeded()
        {
            // Maintain maxTurns pairs of user/assistant interactions (approx 2 * maxTurns items)
            int limit = maxTurns * 2;
            while (history.Count > limit)
            {
                history.RemoveAt(0);
            }
        }
    }
}
