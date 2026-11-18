using UnityEngine;
using UnityEngine.XR;

namespace FiveGLab.VoiceAgent
{
    /// <summary>Quest controller fallback for the keyboard-driven tour and lab.</summary>
    public sealed class QuestLabControls : MonoBehaviour
    {
        private Transform rig;
        private Camera view;
        private InputDevice left;
        private InputDevice right;
        private bool previousA;
        private bool previousX;
        private bool previousGrip;
        private bool previousTrigger;
        private bool turnReady = true;

        private void Start()
        {
            var origin = GameObject.Find("XR Origin (VR)");
            if (origin != null) rig = origin.transform;
            view = Camera.main;
        }

        private void Update()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (rig == null || view == null) return;
            if (!left.isValid) left = InputDevices.GetDeviceAtXRNode(XRNode.LeftHand);
            if (!right.isValid) right = InputDevices.GetDeviceAtXRNode(XRNode.RightHand);

            bool a = ReadButton(right, CommonUsages.primaryButton);
            bool x = ReadButton(left, CommonUsages.primaryButton);
            bool grip = ReadButton(right, CommonUsages.gripButton);
            bool trigger = ReadButton(right, CommonUsages.triggerButton);

            var tour = GuidedTourController.Instance;
            if (a && !previousA && tour != null)
            {
                var hud = VoiceAgentManager.Instance != null ? VoiceAgentManager.Instance.HUD : null;
                if (hud != null && hud.IsNextButtonActive) hud.TriggerNext();
                else if (!tour.IsTourActive) tour.StartTour();
            }
            if (x && !previousX && tour != null && !tour.IsTourActive) tour.StartTour();
            if (grip && !previousGrip)
            {
                var input = FindFirstObjectByType<AgentInputBinder>();
                if (input != null) input.ToggleSpeechRecording();
            }
            if (trigger && !previousTrigger && (tour == null || !tour.IsTourActive))
            {
                if (Physics.Raycast(view.transform.position, view.transform.forward, out RaycastHit hit, 5f))
                {
                    var equipment = hit.collider.GetComponentInParent<PCInteractionTester>();
                    if (equipment != null) equipment.EnterInspection();
                }
            }

            previousA = a;
            previousX = x;
            previousGrip = grip;
            previousTrigger = trigger;

            // Keep tour camera glides and free locomotion from fighting each other.
            if (tour != null && tour.IsTourActive) return;

            if (left.isValid && left.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 move))
            {
                Vector3 forward = view.transform.forward;
                Vector3 rightVector = view.transform.right;
                forward.y = rightVector.y = 0f;
                Vector3 direction = forward.normalized * move.y + rightVector.normalized * move.x;
                rig.position += Vector3.ClampMagnitude(direction, 1f) * (1.7f * Time.deltaTime);
            }
            if (right.isValid && right.TryGetFeatureValue(CommonUsages.primary2DAxis, out Vector2 turn))
            {
                if (Mathf.Abs(turn.x) < 0.35f) turnReady = true;
                else if (turnReady && Mathf.Abs(turn.x) > 0.75f)
                {
                    rig.RotateAround(view.transform.position, Vector3.up, Mathf.Sign(turn.x) * 30f);
                    turnReady = false;
                }
            }
#endif
        }

        private static bool ReadButton(InputDevice device, InputFeatureUsage<bool> usage)
        {
            return device.isValid && device.TryGetFeatureValue(usage, out bool pressed) && pressed;
        }
    }
}
