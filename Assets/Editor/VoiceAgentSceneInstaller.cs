using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using System.IO;

namespace FiveGLab.VoiceAgent.Editor
{
    public static class VoiceAgentSceneInstaller
    {
        [MenuItem("5G Lab/Setup Complete Voice Agent (Aria)")]
        public static void SetupVoiceAgent()
        {
            // 1. Ensure knowledge and config assets exist
            KnowledgeAssetGenerator.GenerateAssets();

            // 2. Open ComputerLab scene
            string scenePath = "Assets/Scenes/ComputerLab.unity";
            var scene = EditorSceneManager.OpenScene(scenePath, OpenSceneMode.Single);

            // 3. Find or create VoiceAgent root
            GameObject agentRoot = GameObject.Find("VoiceAgent_Aria");
            if (agentRoot == null)
            {
                agentRoot = new GameObject("VoiceAgent_Aria");
            }

            var manager = agentRoot.GetComponent<VoiceAgentManager>();
            if (manager == null) manager = agentRoot.AddComponent<VoiceAgentManager>();

            var tour = agentRoot.GetComponent<GuidedTourController>();
            if (tour == null) tour = agentRoot.AddComponent<GuidedTourController>();

            var binder = agentRoot.GetComponent<AgentInputBinder>();
            if (binder == null) binder = agentRoot.AddComponent<AgentInputBinder>();

            var contextProvider = agentRoot.GetComponent<LabContextProvider>();
            if (contextProvider == null) contextProvider = agentRoot.AddComponent<LabContextProvider>();

            // Child 1: MicrophoneCapture
            Transform micChild = agentRoot.transform.Find("MicrophoneCapture");
            if (micChild == null)
            {
                var go = new GameObject("MicrophoneCapture");
                go.transform.SetParent(agentRoot.transform, false);
                micChild = go.transform;
            }
            var micCapture = micChild.GetComponent<MicrophoneCapture>();
            if (micCapture == null) micCapture = micChild.gameObject.AddComponent<MicrophoneCapture>();

            // Child 2: AgentSpeaker
            Transform speakerChild = agentRoot.transform.Find("AgentSpeaker");
            if (speakerChild == null)
            {
                var go = new GameObject("AgentSpeaker");
                go.transform.SetParent(agentRoot.transform, false);
                speakerChild = go.transform;
            }
            var speaker = speakerChild.GetComponent<AgentSpeaker>();
            if (speaker == null) speaker = speakerChild.gameObject.AddComponent<AgentSpeaker>();
            var audioSource = speakerChild.GetComponent<AudioSource>();
            if (audioSource == null) audioSource = speakerChild.gameObject.AddComponent<AudioSource>();

            // Child 3: AgentHUD
            Transform hudChild = agentRoot.transform.Find("AgentHUD");
            if (hudChild == null)
            {
                var go = new GameObject("AgentHUD");
                go.transform.SetParent(agentRoot.transform, false);
                hudChild = go.transform;
            }
            var hud = hudChild.GetComponent<AgentHUD>();
            if (hud == null) hud = hudChild.gameObject.AddComponent<AgentHUD>();

            // Wire serialized fields on VoiceAgentManager via SerializedObject
            SerializedObject soManager = new SerializedObject(manager);
            soManager.FindProperty("config").objectReferenceValue = Resources.Load<AgentConfig>("VoiceAgent/AgentConfig");
            soManager.FindProperty("knowledgeBase").objectReferenceValue = Resources.Load<LabKnowledgeBase>("VoiceAgent/LabKnowledge");
            soManager.FindProperty("micCapture").objectReferenceValue = micCapture;
            soManager.FindProperty("speaker").objectReferenceValue = speaker;
            soManager.FindProperty("contextProvider").objectReferenceValue = contextProvider;
            soManager.FindProperty("hud").objectReferenceValue = hud;
            soManager.ApplyModifiedProperties();

            // Wire GuidedTourController
            SerializedObject soTour = new SerializedObject(tour);
            soTour.FindProperty("knowledgeBase").objectReferenceValue = Resources.Load<LabKnowledgeBase>("VoiceAgent/LabKnowledge");
            soTour.ApplyModifiedProperties();

            // Save Prefab
            string prefabsDir = "Assets/Prefabs";
            if (!AssetDatabase.IsValidFolder(prefabsDir))
            {
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            }
            string prefabPath = $"{prefabsDir}/VoiceAgent.prefab";
            PrefabUtility.SaveAsPrefabAssetAndConnect(agentRoot, prefabPath, InteractionMode.AutomatedAction);

            // Mark and save scene
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[VoiceAgent] VoiceAgent_Aria successfully configured, prefab saved to " + prefabPath + ", and scene saved!");
            EditorUtility.DisplayDialog("Voice Agent Configured", "Aria Voice Agent has been configured and added to ComputerLab scene!\n\nPrefab saved at: Assets/Prefabs/VoiceAgent.prefab\n\nYou can now enter Play Mode and hold [V] to talk with Aria!", "Awesome!");
        }
    }
}
