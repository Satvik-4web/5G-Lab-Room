using UnityEngine;
using UnityEngine.InputSystem;

namespace FiveGLab.VoiceAgent
{
    public class AgentInputBinder : MonoBehaviour
    {
        [Header("VR Input Action (Optional)")]
        [SerializeField] private InputActionReference vrPttAction;

        private VoiceAgentManager manager;
        private bool isPttActive = false;
        private float pttStartTime = 0f;
        private bool isToggleMode = false;

        // UI Collapsible State
        private bool isUIMinimized = false;

        // VAD state
        private float silenceTimer = 0f;

        // Typed input state
        private bool isTypingOpen = false;
        private string typedQuery = "";

        private void Start()
        {
            manager = VoiceAgentManager.Instance;

            if (vrPttAction != null && vrPttAction.action != null)
            {
                vrPttAction.action.Enable();
                vrPttAction.action.performed += ctx => OnPttDown();
                vrPttAction.action.canceled += ctx => OnPttUp();
            }
        }

        private void Update()
        {
            if (manager == null) manager = VoiceAgentManager.Instance;
            if (manager == null) return;

            // Toggle UI Banner minimize with 'H'
            bool hPressed = false;
            if (Keyboard.current != null && Keyboard.current.hKey.wasPressedThisFrame) hPressed = true;
            try { if (Input.GetKeyDown(KeyCode.H)) hPressed = true; } catch { }
            if (hPressed && !isTypingOpen)
            {
                isUIMinimized = !isUIMinimized;
            }

            // Toggle Typed Input with 'T'
            bool tPressed = false;
            if (Keyboard.current != null && Keyboard.current.tKey.wasPressedThisFrame) tPressed = true;
            try { if (Input.GetKeyDown(KeyCode.T)) tPressed = true; } catch { }

            if (tPressed && !isTypingOpen)
            {
                isTypingOpen = true;
                typedQuery = "";
            }

            // Quick Shortcut keys for testing
            if (!isTypingOpen)
            {
                CheckQuickKeys();
            }

            // Microphone device switch key [M]
            bool mPressed = false;
            if (Keyboard.current != null && Keyboard.current.mKey.wasPressedThisFrame) mPressed = true;
            try { if (Input.GetKeyDown(KeyCode.M)) mPressed = true; } catch { }

            if (mPressed)
            {
                var mic = VoiceAgentManager.Instance?.GetComponentInChildren<MicrophoneCapture>();
                if (mic != null)
                {
                    mic.CycleToNextDevice();
                    if (manager.HUD != null)
                    {
                        manager.HUD.UpdateStateVisuals(manager.CurrentState);
                        manager.HUD.ShowSubtitle($"<color=#8AB4F8><b>Microphone switched to: {mic.GetFriendlyDeviceName()}</b></color>");
                    }
                }
            }

            // Push-to-Talk & Toggle-to-Talk Handling (Both New Input System and Legacy)
            bool pttDown = false;
            bool pttUp = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.vKey.wasPressedThisFrame) pttDown = true;
                if (Keyboard.current.vKey.wasReleasedThisFrame) pttUp = true;
            }
            try
            {
                if (Input.GetKeyDown(KeyCode.V)) pttDown = true;
                if (Input.GetKeyUp(KeyCode.V)) pttUp = true;
            }
            catch { }

            if (pttDown)
            {
                if (!isPttActive)
                {
                    pttStartTime = Time.time;
                    isToggleMode = false;
                    OnPttDown();
                }
                else if (isToggleMode)
                {
                    // User tapped V again to stop recording and send
                    isToggleMode = false;
                    OnPttUp();
                }
            }
            else if (pttUp && isPttActive)
            {
                float holdDuration = Time.time - pttStartTime;
                if (holdDuration < 0.35f)
                {
                    // Quick tap -> switch to toggle mode (keep recording until next V tap)
                    isToggleMode = true;
                    if (manager.HUD != null)
                    {
                        manager.HUD.ShowSubtitle("<color=#00E676><b>[RECORDING] Speak your doubt now... (Press [V] when done)</b></color>");
                    }
                }
                else
                {
                    // Held V and released -> stop recording and process
                    isToggleMode = false;
                    OnPttUp();
                }
            }

            // VAD Handling
            if (manager.Config != null && manager.Config.vadEnabled)
            {
                HandleVoiceActivityDetection();
            }
        }

        private void CheckQuickKeys()
        {
            bool k1 = false, k2 = false, k3 = false, k4 = false, k5 = false;
            if (Keyboard.current != null)
            {
                k1 = Keyboard.current.digit1Key.wasPressedThisFrame;
                k2 = Keyboard.current.digit2Key.wasPressedThisFrame;
                k3 = Keyboard.current.digit3Key.wasPressedThisFrame;
                k4 = Keyboard.current.digit4Key.wasPressedThisFrame;
                k5 = Keyboard.current.digit5Key.wasPressedThisFrame;
            }
            try
            {
                if (Input.GetKeyDown(KeyCode.Alpha1)) k1 = true;
                if (Input.GetKeyDown(KeyCode.Alpha2)) k2 = true;
                if (Input.GetKeyDown(KeyCode.Alpha3)) k3 = true;
                if (Input.GetKeyDown(KeyCode.Alpha4)) k4 = true;
                if (Input.GetKeyDown(KeyCode.Alpha5)) k5 = true;
            }
            catch { }

            if (k1) _ = manager.ProcessTypedInputAsync("Start the 5G guided tour");
            if (k2) _ = manager.ProcessTypedInputAsync("Take me to the spectrum analyzer");
            if (k3) _ = manager.ProcessTypedInputAsync("Zoom in on the MEC core server rack");
            if (k4) _ = manager.ProcessTypedInputAsync("Take me to the gNodeB base station");
            if (k5) _ = manager.ProcessTypedInputAsync("What is active beamforming?");
        }

        public void ToggleSpeechRecording()
        {
            if (!isPttActive)
            {
                isToggleMode = true;
                OnPttDown();
                if (manager.HUD != null)
                {
                    manager.HUD.ShowSubtitle("<color=#00E676><b>[RECORDING] Speak now... (Click Stop or press [V] to send)</b></color>");
                }
            }
            else
            {
                isToggleMode = false;
                OnPttUp();
            }
        }

        private void OnPttDown()
        {
            if (isPttActive) return;
            isPttActive = true;
            if (manager != null)
            {
                manager.OnPttDown();
                if (manager.HUD != null)
                {
                    manager.HUD.ShowSubtitle("<color=#00E676><b>[RECORDING] Listening... Speak clearly into your mic!</b></color>");
                }
            }
        }

        private void OnPttUp()
        {
            if (!isPttActive) return;
            isPttActive = false;
            if (manager != null)
            {
                if (manager.HUD != null)
                {
                    manager.HUD.ShowSubtitle("<color=#FFCA28><b>[PROCESSING] Transcribing speech...</b></color>");
                }
                manager.OnPttUp();
            }
        }

        private void HandleVoiceActivityDetection()
        {
            var mic = manager.GetComponentInChildren<MicrophoneCapture>();
            if (mic == null || !mic.IsRecording) return;

            float threshold = manager.Config.vadRmsThreshold;
            if (mic.CurrentRms < threshold)
            {
                silenceTimer += Time.deltaTime;
                if (silenceTimer >= manager.Config.vadSilenceHangTime)
                {
                    silenceTimer = 0f;
                    OnPttUp();
                }
            }
            else
            {
                silenceTimer = 0f;
            }
        }

        private void OnGUI()
        {
            if (manager == null) return;

            var mic = manager.GetComponentInChildren<MicrophoneCapture>();
            string micName = mic != null ? mic.GetFriendlyDeviceName() : "Mic";

            if (isUIMinimized)
            {
                // Sleek Minimalist Floating Pill in top-left
                GUILayout.BeginArea(new Rect(15, 15, 240, 36));
                GUI.color = new Color(0.06f, 0.09f, 0.14f, 0.92f);
                GUI.Box(new Rect(0, 0, 240, 36), GUIContent.none);
                GUI.color = Color.white;

                GUILayout.BeginHorizontal();
                string statusDot = isPttActive ? "<color=#00E676>● REC</color>" : "<color=#00E5FF>● ARIA</color>";
                if (GUILayout.Button($"<b>{statusDot} 5G TUTOR</b>", GetRichStyle(), GUILayout.Height(30)))
                {
                    isUIMinimized = false;
                }
                if (GUILayout.Button("[+] Show [H]", GUILayout.Width(80), GUILayout.Height(28)))
                {
                    isUIMinimized = false;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
            else
            {
                // Full Desktop Quick Access Banner in top-left
                GUILayout.BeginArea(new Rect(15, 15, 530, 118));
                GUI.color = new Color(0.06f, 0.09f, 0.14f, 0.94f);
                GUI.Box(new Rect(0, 0, 530, 118), GUIContent.none);
                GUI.color = Color.white;

                GUILayout.BeginVertical();

                GUILayout.BeginHorizontal();
                string micStatus = isPttActive
                    ? "<color=#00E676><b>● RECORDING... (V TO STOP)</b></color>"
                    : "<color=#00E5FF><b>● ARIA 5G VOICE TUTOR</b></color> <color=#94A3B8>[Hold V to Talk]</color>";
                GUILayout.Label(micStatus, GetRichStyle(), GUILayout.ExpandWidth(true));
                if (GUILayout.Button("[—] Hide [H]", GUILayout.Width(80), GUILayout.Height(22)))
                {
                    isUIMinimized = true;
                }
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button(isPttActive ? "🔴 STOP SPEAKING" : "🎙️ SPEAK NOW (V)")) ToggleSpeechRecording();
                if (GUILayout.Button($"🎧 Mic: {micName} [M]"))
                {
                    mic?.CycleToNextDevice();
                    manager.HUD?.UpdateStateVisuals(manager.CurrentState);
                }
                if (GUILayout.Button("[1] Tour")) _ = manager.ProcessTypedInputAsync("Start the 5G guided tour");
                if (GUILayout.Button("[2] Spectrum")) _ = manager.ProcessTypedInputAsync("Take me to the spectrum analyzer");
                GUILayout.EndHorizontal();

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("[3] MEC Rack")) _ = manager.ProcessTypedInputAsync("Zoom in on the MEC core server rack");
                if (GUILayout.Button("[4] gNodeB")) _ = manager.ProcessTypedInputAsync("Take me to the gNodeB base station");
                if (GUILayout.Button("[5] Beamforming")) _ = manager.ProcessTypedInputAsync("What is active beamforming?");
                if (GUILayout.Button("[T] Ask")) { isTypingOpen = true; }
                GUILayout.EndHorizontal();

                GUILayout.EndVertical();
                GUILayout.EndArea();
            }

            // Custom typing modal
            if (isTypingOpen)
            {
                GUI.color = new Color(0.06f, 0.09f, 0.14f, 0.97f);
                GUI.Box(new Rect(Screen.width / 2 - 250, Screen.height / 2 - 60, 500, 120), GUIContent.none);
                GUI.color = Color.white;

                GUILayout.BeginArea(new Rect(Screen.width / 2 - 240, Screen.height / 2 - 50, 480, 100));
                GUILayout.Label("<color=#00E5FF><b>Ask Aria Anything about 5G:</b></color>", GetRichStyle());
                GUI.SetNextControlName("AriaInputField");
                typedQuery = GUILayout.TextField(typedQuery, 140);
                GUI.FocusControl("AriaInputField");

                GUILayout.BeginHorizontal();
                if (GUILayout.Button("Ask Aria") || (Event.current.isKey && Event.current.keyCode == KeyCode.Return))
                {
                    if (!string.IsNullOrWhiteSpace(typedQuery))
                    {
                        _ = manager.ProcessTypedInputAsync(typedQuery);
                    }
                    isTypingOpen = false;
                }
                if (GUILayout.Button("Cancel"))
                {
                    isTypingOpen = false;
                }
                GUILayout.EndHorizontal();
                GUILayout.EndArea();
            }
        }

        private GUIStyle richStyle;
        private GUIStyle GetRichStyle()
        {
            if (richStyle == null)
            {
                richStyle = new GUIStyle(GUI.skin.label);
                richStyle.richText = true;
            }
            return richStyle;
        }

        private void OnDestroy()
        {
            if (vrPttAction != null && vrPttAction.action != null)
            {
                vrPttAction.action.Disable();
            }
        }
    }
}
