using System;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    [Serializable]
    public class QuizQuestion
    {
        [TextArea(2, 3)]
        public string question;
        public string[] acceptedKeywords;
        [TextArea(2, 3)]
        public string explanation;
    }

    [Serializable]
    public class QuestionAnswerPair
    {
        public string question;
        [TextArea(2, 4)]
        public string answer;
    }

    [Serializable]
    public class EquipmentData
    {
        public EquipmentId id;
        public string displayName;
        public string uiTitle;
        public string oneLiner;
        public string role;
        [TextArea(4, 8)]
        public string howItWorks;
        public string[] keySpecs;
        public string[] analogies;
        public QuestionAnswerPair[] commonQuestions;
        public EquipmentId[] relatedEquipment;
        public QuizQuestion[] quizQuestions;
    }

    [Serializable]
    public class GlossaryEntry
    {
        public string term;
        [TextArea(2, 4)]
        public string definition;
    }

    [CreateAssetMenu(fileName = "LabKnowledge", menuName = "5G Lab/Knowledge Base")]
    public class LabKnowledgeBase : ScriptableObject
    {
        [Header("System Overview")]
        [TextArea(4, 8)]
        public string systemStory = "User Equipment (phones, headsets) connects wirelessly to the gNodeB base station using 5G NR beams. The gNodeB routes signals through high-speed fiber and optical multiplexing to the MEC-Core, where Baseband Units (BBU) and edge servers process compute-heavy workloads in under one millisecond. The RF Spectrum Analyzer continuously monitors and diagnoses radio emissions to eliminate interference and ensure flawless signal integrity.";

        [Header("Equipment Knowledge Items")]
        public List<EquipmentData> equipmentList = new List<EquipmentData>();

        [Header("System Glossary")]
        public List<GlossaryEntry> glossary = new List<GlossaryEntry>();

        public EquipmentData GetEquipment(EquipmentId id)
        {
            if (equipmentList == null) return null;
            return equipmentList.Find(e => e.id == id);
        }

        public GlossaryEntry FindGlossaryTerm(string term)
        {
            if (glossary == null || string.IsNullOrEmpty(term)) return null;
            string lowerTerm = term.Trim().ToLowerInvariant();
            return glossary.Find(g => g.term.ToLowerInvariant() == lowerTerm || g.term.ToLowerInvariant().Contains(lowerTerm));
        }

        /// <summary>
        /// Generates a prompt context block detailing lab knowledge.
        /// </summary>
        public string BuildKnowledgeContext()
        {
            System.Text.StringBuilder sb = new System.Text.StringBuilder();
            sb.AppendLine("=== LAB KNOWLEDGE BASE ===");
            sb.AppendLine($"SYSTEM STORY: {systemStory}\n");

            if (equipmentList != null)
            {
                foreach (var eq in equipmentList)
                {
                    sb.AppendLine($"[EQUIPMENT: {eq.displayName} (ID: {eq.id.ToIdString()})]");
                    sb.AppendLine($"- Role: {eq.role}");
                    sb.AppendLine($"- Summary: {eq.oneLiner}");
                    sb.AppendLine($"- Technical Specs: {string.Join("; ", eq.keySpecs ?? Array.Empty<string>())}");
                    sb.AppendLine($"- How It Works:\n{eq.howItWorks}");
                    if (eq.analogies != null && eq.analogies.Length > 0)
                    {
                        sb.AppendLine($"- Analogies: {string.Join(" | ", eq.analogies)}");
                    }
                    sb.AppendLine();
                }
            }

            return sb.ToString();
        }
    }
}
