using System;
using System.Collections;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    [Serializable]
    internal class EquipmentArgs
    {
        public string id;
        public float duration_seconds = 3f;
    }

    [Serializable]
    internal class TourArgs
    {
        public string mode; // "full", "single"
        public string id;
    }

    [Serializable]
    internal class GlossaryArgs
    {
        public string term;
    }

    public static class LabTools
    {
        public static void RegisterDefaultTools(AgentToolRegistry registry, LabKnowledgeBase knowledge)
        {
            // 1. highlight_equipment
            registry.RegisterTool(
                "highlight_equipment",
                "Highlights a piece of 5G lab equipment to guide the user's attention.",
                "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\",\"enum\":[\"gnodeb\",\"mec\",\"spectrum\"],\"description\":\"The equipment ID to highlight.\"},\"duration_seconds\":{\"type\":\"number\",\"description\":\"Highlight duration in seconds.\"}},\"required\":[\"id\"]}",
                async (argsJson) =>
                {
                    EquipmentId id = EquipmentId.None;
                    float duration = 3f;
                    try
                    {
                        var args = JsonUtility.FromJson<EquipmentArgs>(argsJson);
                        if (args != null)
                        {
                            id = EquipmentIdExtensions.FromIdString(args.id);
                            if (args.duration_seconds > 0) duration = args.duration_seconds;
                        }
                    }
                    catch { }

                    HighlightInScene(id, duration);
                    await Task.CompletedTask;
                    return $"{{\"status\":\"success\",\"message\":\"Highlighted {id.GetDisplayName()}\"}}";
                }
            );

            // 2. inspect_equipment
            registry.RegisterTool(
                "inspect_equipment",
                "Triggers the inspection zoom to bring the user directly in front of the specified 5G equipment.",
                "{\"type\":\"object\",\"properties\":{\"id\":{\"type\":\"string\",\"enum\":[\"gnodeb\",\"mec\",\"spectrum\"],\"description\":\"The equipment ID to zoom in and inspect.\"}},\"required\":[\"id\"]}",
                async (argsJson) =>
                {
                    EquipmentId id = EquipmentId.None;
                    try
                    {
                        var args = JsonUtility.FromJson<EquipmentArgs>(argsJson);
                        if (args != null) id = EquipmentIdExtensions.FromIdString(args.id);
                    }
                    catch { }

                    if (id != EquipmentId.None)
                    {
                        PCInteractionTester.InspectEquipment(id, playEquipmentAudio: false);
                        await Task.CompletedTask;
                        return $"{{\"status\":\"success\",\"message\":\"Camera smoothly zoomed to {id.GetDisplayName()}\"}}";
                    }
                    return "{\"status\":\"error\",\"message\":\"Invalid equipment ID.\"}";
                }
            );

            // 3. exit_inspection
            registry.RegisterTool(
                "exit_inspection",
                "Exits the current inspection zoom and returns the camera back to the general room view.",
                "{\"type\":\"object\",\"properties\":{}}",
                async (argsJson) =>
                {
                    PCInteractionTester.ExitCurrentInspection();
                    await Task.CompletedTask;
                    return "{\"status\":\"success\",\"message\":\"Exited inspection zoom.\"}";
                }
            );

            // 4. start_tour
            registry.RegisterTool(
                "start_tour",
                "Starts the interactive guided tour of all 5G lab components.",
                "{\"type\":\"object\",\"properties\":{\"mode\":{\"type\":\"string\",\"enum\":[\"full\",\"single\"]},\"id\":{\"type\":\"string\"}}}",
                async (argsJson) =>
                {
                    string mode = "full";
                    string targetId = null;
                    try
                    {
                        var args = JsonUtility.FromJson<TourArgs>(argsJson);
                        if (args != null)
                        {
                            if (!string.IsNullOrEmpty(args.mode)) mode = args.mode;
                            targetId = args.id;
                        }
                    }
                    catch { }

                    if (GuidedTourController.Instance != null)
                    {
                        GuidedTourController.Instance.StartTour(mode, targetId);
                        await Task.CompletedTask;
                        return $"{{\"status\":\"success\",\"message\":\"Guided tour started in {mode} mode.\"}}";
                    }
                    return "{\"status\":\"error\",\"message\":\"GuidedTourController not found in scene.\"}";
                }
            );

            // 5. next_tour_stop
            registry.RegisterTool(
                "next_tour_stop",
                "Advances the guided tour to the next equipment stop.",
                "{\"type\":\"object\",\"properties\":{}}",
                async (argsJson) =>
                {
                    if (GuidedTourController.Instance != null)
                    {
                        GuidedTourController.Instance.NextStop();
                        await Task.CompletedTask;
                        return "{\"status\":\"success\",\"message\":\"Moved to next tour stop.\"}";
                    }
                    return "{\"status\":\"error\",\"message\":\"GuidedTourController not found in scene.\"}";
                }
            );

            // 6. end_tour
            registry.RegisterTool(
                "end_tour",
                "Ends the currently active guided tour.",
                "{\"type\":\"object\",\"properties\":{}}",
                async (argsJson) =>
                {
                    if (GuidedTourController.Instance != null)
                    {
                        GuidedTourController.Instance.EndTour();
                        await Task.CompletedTask;
                        return "{\"status\":\"success\",\"message\":\"Tour ended.\"}";
                    }
                    return "{\"status\":\"error\",\"message\":\"GuidedTourController not found in scene.\"}";
                }
            );

            // 7. show_glossary_card
            registry.RegisterTool(
                "show_glossary_card",
                "Displays a technical glossary definition card on the user's HUD.",
                "{\"type\":\"object\",\"properties\":{\"term\":{\"type\":\"string\",\"description\":\"The telecom term to define.\"}},\"required\":[\"term\"]}",
                async (argsJson) =>
                {
                    string term = "";
                    try
                    {
                        var args = JsonUtility.FromJson<GlossaryArgs>(argsJson);
                        if (args != null) term = args.term;
                    }
                    catch { }

                    var entry = knowledge?.FindGlossaryTerm(term);
                    if (entry != null && PopupUIManager.Instance != null)
                    {
                        string desc = $"<color=#8AB4F8><b>DEFINITION:</b></color>\n{entry.definition}";
                        PopupUIManager.Instance.ShowPopup(entry.term.ToUpperInvariant(), desc, null);
                        await Task.CompletedTask;
                        return $"{{\"status\":\"success\",\"term\":\"{entry.term}\",\"definition\":\"{entry.definition}\"}}";
                    }
                    return $"{{\"status\":\"not_found\",\"term\":\"{term}\"}}";
                }
            );
        }

        private static void HighlightInScene(EquipmentId id, float duration)
        {
            if (id == EquipmentId.None) return;

            if (PCInteractionTester.Instances.TryGetValue(id, out var tester))
            {
                tester.Highlight(duration);
                return;
            }

            string searchKey = id switch
            {
                EquipmentId.gnodeb => "gNodeB",
                EquipmentId.mec => "Computing",
                EquipmentId.spectrum => "Spectrum",
                _ => ""
            };

            var renderers = GameObject.FindObjectsByType<Renderer>(FindObjectsInactive.Exclude, FindObjectsSortMode.None);
            foreach (var r in renderers)
            {
                if (r.gameObject.name.Contains(searchKey) || (r.transform.parent != null && r.transform.parent.name.Contains(searchKey)))
                {
                    if (VoiceAgentManager.Instance != null)
                    {
                        VoiceAgentManager.Instance.StartCoroutine(PulseRenderer(r, duration));
                    }
                    break;
                }
            }
        }

        private static IEnumerator PulseRenderer(Renderer r, float duration)
        {
            if (r == null || r.material == null) yield break;
            Color originalColor = r.material.color;
            Color highlightColor = new Color(0.54f, 0.71f, 0.97f); // Soft blue #8AB4F8

            float elapsed = 0;
            while (elapsed < duration)
            {
                float t = Mathf.PingPong(elapsed * 3f, 1f);
                r.material.color = Color.Lerp(originalColor, highlightColor, t * 0.5f);
                elapsed += Time.deltaTime;
                yield return null;
            }

            r.material.color = originalColor;
        }
    }
}
