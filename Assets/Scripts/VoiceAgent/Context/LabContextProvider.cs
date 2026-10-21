using System.Text;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    public class LabContextProvider : MonoBehaviour
    {
        [SerializeField] private LabKnowledgeBase knowledgeBase;

        public void SetKnowledgeBase(LabKnowledgeBase kb) => knowledgeBase = kb;

        public string BuildLiveContextString(ConversationMemory memory = null, string tourContext = null)
        {
            StringBuilder sb = new StringBuilder();
            sb.AppendLine("=== CURRENT CONTEXT ===");

            // Inspected vs Hovered
            if (LabContext.CurrentInspected != EquipmentId.None)
            {
                sb.AppendLine($"USER IS CURRENTLY INSPECTING (CLOSE-UP ZOOM): {LabContext.CurrentInspected.GetDisplayName()} (ID: {LabContext.CurrentInspected.ToIdString()})");
                sb.AppendLine("NOTE: Words like 'this', 'that', 'here', 'it' refer directly to this inspected equipment.");
            }
            else if (LabContext.CurrentHovered != EquipmentId.None)
            {
                sb.AppendLine($"USER IS CURRENTLY HOVERING OVER: {LabContext.CurrentHovered.GetDisplayName()} (ID: {LabContext.CurrentHovered.ToIdString()})");
                sb.AppendLine("NOTE: Words like 'this', 'that', 'here' refer to this hovered equipment.");
            }
            else
            {
                sb.AppendLine("USER IS CURRENTLY LOOKING AT THE GENERAL LAB ROOM (No specific equipment hovered or inspected).");
            }

            // Tour context
            if (!string.IsNullOrEmpty(tourContext))
            {
                sb.AppendLine($"GUIDED TOUR STATUS: {tourContext}");
            }

            // User camera position
            if (Camera.main != null)
            {
                Vector3 pos = Camera.main.transform.position;
                sb.AppendLine($"User Camera Position: ({pos.x:F1}, {pos.y:F1}, {pos.z:F1})");
            }

            // Visited summary
            if (memory != null)
            {
                sb.AppendLine($"Equipments Explored This Session: {memory.VisitedCount}/3");
                if (memory.TotalQuizQuestions > 0)
                {
                    sb.AppendLine($"Current Quiz Score: {memory.QuizScore}/{memory.TotalQuizQuestions}");
                }
            }

            sb.AppendLine();
            if (knowledgeBase != null)
            {
                sb.AppendLine(knowledgeBase.BuildKnowledgeContext());
            }

            return sb.ToString();
        }
    }
}
