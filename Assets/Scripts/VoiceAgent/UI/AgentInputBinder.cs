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

            // Toggle Typed Input with 'T'
            bool tPressed = false;
            if (Keyboard.current != null)
            {
                if (Keyboard.current.tKey.wasPressedThisFrame) tPressed = true;
            }
            else
            {
                try { if (Input.GetKeyDown(KeyCode.T)) tPressed = true; } catch { }
            }

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

            // Push-to-Talk & Toggle-to-Talk Handling
            KeyCode pttKey = manager.Config != null ? manager.Config.pttKey : KeyCode.V;
            bool pttDown = false;
            bool pttUp = false;

            if (Keyboard.current != null)
            {
                if (Keyboard.current.vKey.wasPressedThisFrame) pttDown = true;
                if (Keyboard.current.vKey.wasReleasedThisFrame) pttUp = true;
            }
            else
            {
                try
                {
                    if (Input.GetKeyDown(pttKey)) pttDown = true;
                    if (Input.GetKeyUp(pttKey)) pttUp = true;
                }
                catch { }
            }

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
                    // User pressed V again to finish speaking
                    isToggleMode = false;
                    OnPttUp();
                }
            }
            else if (pttUp && isPttActive)
            {
                float holdDuration = Time.time - pttStartTime;
                if (holdDuration < 0.4f)
                {
                    // User tapped V quickly! Keep recording in Toggle Mode
                    isToggleMode = true;
                    if (manager.HUD != null)
                    {
                        manager.HUD.ShowSubtitle("<color=#4CAF50><b>Listening... (Speak your doubt, then press [V] to send)</b></color>");
                    }
                }
                else
                {
                    // User held V and released after speaking
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
            else
            {
                try
                {
                    k1 = Input.GetKeyDown(KeyCode.Alpha1);
                    k2 = Input.GetKeyDown(KeyCode.Alpha2);
                    k3 = Input.GetKeyDown(KeyCode.Alpha3);
                    k4 = Input.GetKeyDown(KeyCode.Alpha4);
                    k5 = Input.GetKeyDown(KeyCode.Alpha5);
                }
                catch { }
            }

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
            if (manager != null) manager.OnPttDown();
        }

        private void OnPttUp()
        {
            if (!isPttActive) return;
            isPttActive = false;
            if (manager != null) manager.OnPttUp();
        }

        private void HandleVoiceActivityDetection()
        {
            var mic = manager.GetComponentInChildren<MicrophoneCapture>();
            if (mic == null) return;

            if (manager.CurrentState == AgentState.Idle)
            {
                if (mic.CurrentRms > manager.Config.vadRmsThreshold)
                {
                    OnPttDown();
                    silenceTimer = 0f;
                }
            }
            else if (manager.CurrentState == AgentState.Listening)
            {
                if (mic.CurrentRms < manager.Config.vadRmsThreshold)
                {
                    silenceTimer += Time.deltaTime;
                    if (silenceTimer >= manager.Config.vadSilenceHangTime)
                    {
                        OnPttUp();
                        silenceTimer = 0f;
                    }
                }
                else
                {
                    silenceTimer = 0f;
                }
            }
        }

        private void OnGUI()
        {
            // Dynamic scale helper for all screen resolutions (1080p, QHD 1440p, 4K)
            float uiWidth = Mathf.Min(Screen.width * 0.8f, 620f);
            float uiHeight = 110f;
            float posX = (Screen.width - uiWidth) / 2f; // Centered at bottom
            float posY = Screen.height - uiHeight - 20f;

            GUILayout.BeginArea(new Rect(posX, posY, uiWidth, uiHeight));
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.92f);
            GUI.Box(new Rect(0, 0, uiWidth, uiHeight), GUIContent.none);
            GUI.color = Color.white;

            GUILayout.BeginVertical();
            GUILayout.Space(6);
            
            string micStatus = isPttActive ? "<color=#4CAF50><b>[🎙️ RECORDING... CLICK TO STOP]</b></color>" : "<color=#8AB4F8><b>[🎙️ CLICK TO TALK (OR PRESS V)]</b></color>";
            GUILayout.Label($" <b>ARIA 5G VOICE TUTOR</b>  {micStatus}", GetRichStyle());

            GUILayout.BeginHorizontal();
            if (GUILayout.Button(isPttActive ? "🔴 STOP SPEAKING" : "🎙️ SPEAK NOW (V)")) ToggleSpeechRecording();
            if (GUILayout.Button("[1] 5G Tour")) _ = manager.ProcessTypedInputAsync("Start the 5G guided tour");
            if (GUILayout.Button("[2] Spectrum")) _ = manager.ProcessTypedInputAsync("Take me to the spectrum analyzer");
            if (GUILayout.Button("[3] MEC Rack")) _ = manager.ProcessTypedInputAsync("Zoom in on the MEC core server rack");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("[4] gNodeB")) _ = manager.ProcessTypedInputAsync("Take me to the gNodeB base station");
            if (GUILayout.Button("[5] Beamforming")) _ = manager.ProcessTypedInputAsync("What is active beamforming?");
            if (GUILayout.Button("[T] Type Doubt")) { isTypingOpen = true; }
            if (GUILayout.Button("Exit Inspection")) _ = manager.ProcessTypedInputAsync("Exit inspection");
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // Custom typing modal
            if (isTypingOpen)
            {
                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.96f);
                GUI.Box(new Rect(Screen.width / 2 - 250, Screen.height / 2 - 60, 500, 120), GUIContent.none);
                GUI.color = Color.white;

                GUILayout.BeginArea(new Rect(Screen.width / 2 - 240, Screen.height / 2 - 50, 480, 100));
                GUILayout.Label("<color=#8AB4F8><b>Ask Aria Anything about 5G:</b></color>", GetRichStyle());
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
