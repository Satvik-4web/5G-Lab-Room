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

            KeyCode pttKey = manager.Config != null ? manager.Config.pttKey : KeyCode.V;

            // 1. Keyboard PTT (Desktop)
            if (Input.GetKeyDown(pttKey))
            {
                OnPttDown();
            }
            else if (Input.GetKeyUp(pttKey))
            {
                OnPttUp();
            }

            // 2. VAD Handling (if enabled in config)
            if (manager.Config != null && manager.Config.vadEnabled)
            {
                HandleVoiceActivityDetection();
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

        private void OnDestroy()
        {
            if (vrPttAction != null && vrPttAction.action != null)
            {
                vrPttAction.action.Disable();
            }
        }
    }
}
