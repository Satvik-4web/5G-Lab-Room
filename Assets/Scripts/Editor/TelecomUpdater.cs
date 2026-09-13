using UnityEngine;
using UnityEditor;

public class TelecomUpdater
{
    public static string UpdateLabels()
    {
        GameObject rack = EditorUtility.InstanceIDToObject(56376) as GameObject;
        GameObject antenna = EditorUtility.InstanceIDToObject(58410) as GameObject;
        
        if (rack != null)
        {
            rack.name = "Mobile Edge Computing (MEC-Core)";
            InteractableLabel label = rack.GetComponent<InteractableLabel>();
            if (label != null)
            {
                label.objectName = "MOBILE EDGE COMPUTING (MEC-CORE)";
                label.objectDescription = "<color=#8AB4F8>• Compute: High-Performance GPU/CPU\n• Latency: Ultra-Low (< 1ms)\n• Status: ONLINE - Processing</color>\n\n<color=#E0E0E0><size=80%><b>OVERVIEW:</b>\nThe Mobile Edge Computing (MEC) core. By bringing cloud computing power directly to the edge of the network, it drastically reduces latency and enables real-time data processing for smart applications.</size></color>";
                EditorUtility.SetDirty(label);
            }
            EditorUtility.SetDirty(rack);
        }
        
        if (antenna != null)
        {
            antenna.name = "gNodeB (Base Station)";
            InteractableLabel label = antenna.GetComponent<InteractableLabel>();
            if (label != null)
            {
                label.objectName = "gNodeB (BASE STATION)";
                label.objectDescription = "<color=#8AB4F8>• Interface: 5G NR (New Radio)\n• Tech: Massive MIMO & Beamforming\n• Status: ONLINE - Transmitting</color>\n\n<color=#E0E0E0><size=80%><b>OVERVIEW:</b>\nThe gNodeB (Next-Generation Node B). It directly communicates with user equipment, utilizing advanced beamforming to deliver high-capacity, low-latency 5G signals exactly where needed without waste.</size></color>";
                EditorUtility.SetDirty(label);
            }
            EditorUtility.SetDirty(antenna);
        }

        UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene());
        return "Updated successfully!";
    }
}
