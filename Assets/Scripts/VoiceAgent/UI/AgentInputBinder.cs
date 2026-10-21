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

            // Quick Shortcut keys for testing in Mock / Desktop
            if (!isTypingOpen)
            {
                CheckQuickKeys();
            }

            // Push-to-Talk Handling
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
                OnPttDown();
            }
            else if (pttUp)
            {
                OnPttUp();
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
            // Sleek quick-test helper on Desktop
            GUILayout.BeginArea(new Rect(15, Screen.height - 120, 520, 110));
            GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.85f);
            GUI.Box(new Rect(0, 0, 520, 110), GUIContent.none);
            GUI.color = Color.white;

            GUILayout.BeginVertical();
            GUILayout.Space(6);
            GUILayout.Label(" <color=#8AB4F8><b>ARIA VOICE TUTOR SHORTCUTS</b></color> (Press Key or Hold [V] to Talk):", GetRichStyle());

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("[1] Start Tour")) _ = manager.ProcessTypedInputAsync("Start the 5G guided tour");
            if (GUILayout.Button("[2] Spectrum")) _ = manager.ProcessTypedInputAsync("Take me to the spectrum analyzer");
            if (GUILayout.Button("[3] MEC Rack")) _ = manager.ProcessTypedInputAsync("Zoom in on the MEC core server rack");
            if (GUILayout.Button("[4] gNodeB")) _ = manager.ProcessTypedInputAsync("Take me to the gNodeB base station");
            GUILayout.EndHorizontal();

            GUILayout.BeginHorizontal();
            if (GUILayout.Button("[5] Ask: Beamforming?")) _ = manager.ProcessTypedInputAsync("What is active beamforming?");
            if (GUILayout.Button("[T] Type Custom Question")) { isTypingOpen = true; }
            if (GUILayout.Button("Exit Inspection")) _ = manager.ProcessTypedInputAsync("Exit inspection");
            GUILayout.EndHorizontal();

            GUILayout.EndVertical();
            GUILayout.EndArea();

            // Custom typing modal
            if (isTypingOpen)
            {
                GUI.color = new Color(0.1f, 0.1f, 0.1f, 0.95f);
                GUI.Box(new Rect(Screen.width / 2 - 250, Screen.height / 2 - 60, 500, 120), GUIContent.none);
                GUI.color = Color.white;

                GUILayout.BeginArea(new Rect(Screen.width / 2 - 240, Screen.height / 2 - 50, 480, 100));
                GUILayout.Label("<color=#8AB4F8><b>Ask Aria Anything:</b></color>", GetRichStyle());
                GUI.SetNextControlName("AriaInputField");
                typedQuery = GUILayout.TextField(typedQuery, 120);
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
