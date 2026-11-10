using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Reframed Module-Wise Course Controller for the 5G VR Simulator.
    /// Progresses step-by-step from Module 1 (Wall Banner: CUPS Architecture)
    /// through the 3D lab instruments (gNodeB -> MEC -> Spectrum Analyzer),
    /// featuring animated highlighting, synchronized voice lecturing, and interactive quizzes.
    /// </summary>
    public class GuidedTourController : MonoBehaviour
    {
        public static GuidedTourController Instance { get; private set; }

        public enum ModuleStep
        {
            NotStarted,
            Intro,

            // MODULE 1: 5G Network Architecture based on CUPS (Wall Banner)
            Module1_Banner_Overview,
            Module1_Banner_UE_Access,
            Module1_Banner_ControlPlane,
            Module1_Banner_UserPlane_CUPS,
            Module1_Quiz,

            // MODULE 2: gNodeB Base Station & Massive MIMO Radio
            Module2_gNodeB_Intro,
            Module2_gNodeB_Beamforming,
            Module2_gNodeB_Quiz,

            // MODULE 3: Mobile Edge Computing (MEC) Server Rack & Distributed UPF
            Module3_MEC_Intro,
            Module3_MEC_LowLatency,
            Module3_MEC_Quiz,

            // MODULE 4: RF Spectrum Analyzer & Signal Diagnostics
            Module4_Spectrum_Intro,
            Module4_Spectrum_Diagnostics,
            Module4_Spectrum_Quiz,

            // COMPLETION & RECAP
            RecapAndWrapUp,
            Completed
        }

        [SerializeField] private LabKnowledgeBase knowledgeBase;
        [Header("Auto Start Configuration")]
        [Tooltip("Automatically launch directly into Module 1 (Wall Banner) when entering Play Mode")]
        [SerializeField] private bool autoStartModule1OnPlay = true;
        [SerializeField] private float autoStartDelaySeconds = 1.0f;

        private ModuleStep currentStep = ModuleStep.NotStarted;
        private Coroutine tourRoutine;
        private bool isWaitingForQuizAnswer = false;
        private QuizQuestion currentQuizQuestion = null;

        public bool IsTourActive => currentStep != ModuleStep.NotStarted && currentStep != ModuleStep.Completed;
        public ModuleStep CurrentStep => currentStep;

        public event Action<ModuleStep> OnStepChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            if (autoStartModule1OnPlay)
            {
                StartCoroutine(AutoStartRoutine());
            }
        }

        private IEnumerator AutoStartRoutine()
        {
            // Allow all singletons, VR Origin, and audio systems to initialize cleanly
            yield return new WaitForSeconds(autoStartDelaySeconds);
            if (currentStep == ModuleStep.NotStarted)
            {
                Debug.Log("[GuidedTourController] Auto-starting directly into Module 1 on Play mode entry...");
                StartTour("full");
            }
        }

        public void StartTour(string mode = "full", string specificEquipment = null)
        {
            if (tourRoutine != null) StopCoroutine(tourRoutine);
            tourRoutine = StartCoroutine(RunModularCourseSequence(mode, specificEquipment));
        }

        public void NextStop()
        {
            isWaitingForQuizAnswer = false;
        }

        public void EndTour()
        {
            if (tourRoutine != null)
            {
                StopCoroutine(tourRoutine);
                tourRoutine = null;
            }
            currentStep = ModuleStep.Completed;
            CupsBannerDisplay.Instance?.SetHighlightSection("none");
            CupsBannerDisplay.Instance?.SetPopUpState(false);
            VoiceAgentManager.Instance?.HUD?.SetCinemaSubtitleMode(false);
            PCInteractionTester.ExitCurrentInspection();
            OnStepChanged?.Invoke(currentStep);

            if (VoiceAgentManager.Instance != null)
            {
                VoiceAgentManager.Instance.SetState(AgentState.Idle);
            }
        }

        public bool SubmitSpokenAnswer(string transcript, out string feedback)
        {
            feedback = "";
            if (!isWaitingForQuizAnswer || currentQuizQuestion == null) return false;

            isWaitingForQuizAnswer = false;
            string clean = transcript.ToLowerInvariant();
            bool correct = false;

            if (currentQuizQuestion.acceptedKeywords != null)
            {
                foreach (var kw in currentQuizQuestion.acceptedKeywords)
                {
                    if (clean.Contains(kw.ToLowerInvariant()))
                    {
                        correct = true;
                        break;
                    }
                }
            }

            if (correct)
            {
                if (VoiceAgentManager.Instance != null && VoiceAgentManager.Instance.Memory != null)
                {
                    VoiceAgentManager.Instance.Memory.QuizScore++;
                    VoiceAgentManager.Instance.Memory.TotalQuizQuestions++;
                }
                feedback = $"Excellent! {currentQuizQuestion.explanation}";
            }
            else
            {
                if (VoiceAgentManager.Instance != null && VoiceAgentManager.Instance.Memory != null)
                {
                    VoiceAgentManager.Instance.Memory.TotalQuizQuestions++;
                }
                feedback = $"Good try! The correct concept is: {currentQuizQuestion.explanation}";
            }

            return true;
        }

        private IEnumerator RunModularCourseSequence(string mode, string specificEquipment)
        {
            if (VoiceAgentManager.Instance != null)
            {
                VoiceAgentManager.Instance.SetState(AgentState.Touring);
                VoiceAgentManager.Instance.HUD?.SetCinemaSubtitleMode(true);
            }

            // ==========================================
            // MODULE 1: 5G ARCHITECTURE BASED ON CUPS
            // Direct launch: camera immediately glides to the wall banner and pops it forward
            // ==========================================
            currentStep = ModuleStep.Module1_Banner_Overview;
            OnStepChanged?.Invoke(currentStep);

            // Pop banner forward at professional 6-degree angle and glide camera
            PCInteractionTester.InspectEquipment(EquipmentId.banner);
            yield return new WaitForSeconds(1.0f);
            CupsBannerDisplay.Instance?.SetHighlightSection("all");

            yield return SpeakAndWait("Hey there! Welcome to our 5G Virtual Reality Lab. I'm Aria, your interactive lab mentor. To help you truly visualize how next-generation mobile networks work, I've projected our university's CUPS architecture banner right here in front of us as a holographic smart display. Throughout this module, I'll be using dynamic laser outlines and target markers so you can clearly trace how signaling and data split across modern 5G networks. Let's begin with Module 1!");

            // Module 1 - Step A: UEs and Access Networks
            currentStep = ModuleStep.Module1_Banner_UE_Access;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("ue");
            CupsBannerDisplay.Instance?.SetFocusTarget(new Vector3(-0.30f, 0.0f, -0.02f));
            yield return SpeakAndWait("Look over at the left side, where our cyan laser outline is pulsing. These are our User Equipments and heterogeneous Access Networks. You see, 5G isn't just about faster smartphones; it unifies smart city IoT sensors, connected autonomous cars, and high-speed workstations. Notice how they connect through diverse radios: 5G New Radio, legacy 4G LTE, and even untrusted home Wi-Fi via our secure N3IWF gateway. The graphics highlight this in cyan because this is the access frontier where all user traffic enters our network.");

            // Module 1 - Step B: 5G Control Plane (SBA)
            currentStep = ModuleStep.Module1_Banner_ControlPlane;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("control");
            CupsBannerDisplay.Instance?.SetFocusTarget(new Vector3(0.12f, 0.18f, -0.02f));
            yield return SpeakAndWait("Now, follow the golden laser up to the top box: our 5G Control Plane. I've highlighted this in gold to represent the brain of the network. Standardized in 3GPP Release 15 as a cloud-native Service-Based Architecture, microservice functions like the AMF for device mobility, the SMF for session control, and the UDM for authentication communicate over lightweight REST APIs. Crucially, the control plane only processes signaling—it never carries the heavy user payload data.");

            // Module 1 - Step C: 5G User Plane & CUPS
            currentStep = ModuleStep.Module1_Banner_UserPlane_CUPS;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("user_plane");
            CupsBannerDisplay.Instance?.SetFocusTarget(new Vector3(0.12f, -0.20f, -0.02f));
            yield return SpeakAndWait("Down below, look at the emerald green laser framing our 5G User Plane. Here is the true breakthrough of CUPS—Control and User Plane Separation. While the SMF centrally manages session policies from afar over the N4 interface, lightweight User Plane Functions, or UPFs, are distributed all the way out to the cell edge. This enables local breakout into a Local Data Network, cutting round-trip latency to under one millisecond! That's why real-time robotics and autonomous driving are finally possible.");

            // Module 1 Quiz
            currentStep = ModuleStep.Module1_Quiz;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.HideFocusTarget();
            currentQuizQuestion = new QuizQuestion
            {
                question = "In this CUPS architecture, which edge network function handles user data forwarding and enables that sub-millisecond local breakout?",
                acceptedKeywords = new[] { "upf", "user plane", "user plane function" },
                explanation = "The UPF, or User Plane Function, forwards user data packets at the edge under the central session control of the SMF."
            };
            yield return SpeakAndWait($"Quick check on Module 1! {currentQuizQuestion.question} Hold down [V] and tell me!");
            yield return WaitForAnswerOrTimeout(10f);

            // Clean up Module 1 visuals before transitioning
            CupsBannerDisplay.Instance?.SetHighlightSection("none");
            CupsBannerDisplay.Instance?.HideFocusTarget();
            CupsBannerDisplay.Instance?.SetPopUpState(false);
            yield return new WaitForSeconds(1.0f);

            // ==========================================
            // MODULE 2: gNodeB BASE STATION & RADIO
            // ==========================================
            currentStep = ModuleStep.Module2_gNodeB_Intro;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Great job! Now that we have the architectural blueprint in mind, let's step over to Module 2 to inspect our physical 3D gNodeB base station!");

            PCInteractionTester.InspectEquipment(EquipmentId.gnodeb);
            yield return new WaitForSeconds(1.3f);

            currentStep = ModuleStep.Module2_gNodeB_Beamforming;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("This is our gNodeB, connecting mobile UEs over 5G New Radio. It features Massive MIMO with 64 transmit and 64 receive elements. Using active beamforming, it shapes radio waves into narrow, high-gain beams directed straight at active users, drastically eliminating interference.");

            // Module 2 Quiz
            currentStep = ModuleStep.Module2_gNodeB_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "What does active beamforming do to 5G radio signals?",
                acceptedKeywords = new[] { "focus", "direct", "narrow", "target", "pencil" },
                explanation = "Beamforming focuses radio wave energy into targeted, narrow beams directed at user devices."
            };
            yield return SpeakAndWait($"Check on Module 2: {currentQuizQuestion.question} Hold [V] and tell me!");
            yield return WaitForAnswerOrTimeout(8f);

            // ==========================================
            // MODULE 3: MEC-CORE SERVER RACK & EDGE UPF
            // ==========================================
            currentStep = ModuleStep.Module3_MEC_Intro;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Next, let's advance to Module 3: the Mobile Edge Computing core server rack!");

            PCInteractionTester.InspectEquipment(EquipmentId.mec);
            yield return new WaitForSeconds(1.3f);

            currentStep = ModuleStep.Module3_MEC_LowLatency;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("This is the MEC server rack. As we saw on the CUPS banner, placing baseband units and a localized UPF right here at the edge delivers processing latency under one millisecond. This ultra-reliable low latency is mandatory for tele-surgery, robotics, and self-driving cars.");

            // Module 3 Quiz
            currentStep = ModuleStep.Module3_MEC_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "Why do we place MEC servers at the edge of the network?",
                acceptedKeywords = new[] { "latency", "speed", "fast", "closer", "delay", "millisecond" },
                explanation = "Placing MEC servers at the edge eliminates backhaul delays and delivers sub-millisecond response times."
            };
            yield return SpeakAndWait($"Check on Module 3: {currentQuizQuestion.question} Hold [V] to answer!");
            yield return WaitForAnswerOrTimeout(8f);

            // ==========================================
            // MODULE 4: RF SPECTRUM ANALYZER
            // ==========================================
            currentStep = ModuleStep.Module4_Spectrum_Intro;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Finally, let's step to Module 4 to inspect our precision RF Spectrum Analyzer!");

            PCInteractionTester.InspectEquipment(EquipmentId.spectrum);
            yield return new WaitForSeconds(1.3f);

            currentStep = ModuleStep.Module4_Spectrum_Diagnostics;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("This RF Spectrum Analyzer measures signal power across frequencies from two hertz up to eighty-five gigahertz. Telecom engineers use it to verify 5G Sub-6 and millimeter wave transmissions, measure Error Vector Magnitude, and hunt down radio interference.");

            // Module 4 Quiz
            currentStep = ModuleStep.Module4_Spectrum_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "What two variables does a spectrum analyzer display on its screen?",
                acceptedKeywords = new[] { "power", "amplitude", "frequency", "freq" },
                explanation = "The spectrum analyzer plots signal power or amplitude on the vertical axis against frequency on the horizontal axis."
            };
            yield return SpeakAndWait($"Final check on Module 4: {currentQuizQuestion.question} Hold [V] and answer!");
            yield return WaitForAnswerOrTimeout(8f);

            // ==========================================
            // RECAP & GRADUATION
            // ==========================================
            currentStep = ModuleStep.RecapAndWrapUp;
            OnStepChanged?.Invoke(currentStep);
            PCInteractionTester.ExitCurrentInspection();
            yield return new WaitForSeconds(1.1f);

            int score = VoiceAgentManager.Instance?.Memory?.QuizScore ?? 0;
            int total = VoiceAgentManager.Instance?.Memory?.TotalQuizQuestions ?? 4;
            yield return SpeakAndWait($"Congratulations! You have completed all four modules of our 5G VR Laboratory! From the CUPS architecture banner on the wall to the gNodeB, the MEC edge servers, and the Spectrum Analyzer. You scored {score} out of {total} on the quiz! Feel free to ask me anything about 5G anytime.");

            currentStep = ModuleStep.Completed;
            OnStepChanged?.Invoke(currentStep);
            if (VoiceAgentManager.Instance != null)
            {
                VoiceAgentManager.Instance.HUD?.SetCinemaSubtitleMode(false);
                VoiceAgentManager.Instance.SetState(AgentState.Idle);
            }
        }

        private IEnumerator SpeakAndWait(string text)
        {
            if (VoiceAgentManager.Instance != null)
            {
                bool finished = false;
                VoiceAgentManager.Instance.SpeakNarrativeDirect(text, () => finished = true);
                while (!finished)
                {
                    yield return null;
                }
            }
            else
            {
                yield return new WaitForSeconds(3.5f);
            }
        }

        private IEnumerator WaitForAnswerOrTimeout(float timeoutSeconds)
        {
            isWaitingForQuizAnswer = true;
            float elapsed = 0;
            while (isWaitingForQuizAnswer && elapsed < timeoutSeconds)
            {
                if (VoiceAgentManager.Instance != null &&
                    (VoiceAgentManager.Instance.CurrentState == AgentState.Listening ||
                     VoiceAgentManager.Instance.CurrentState == AgentState.Transcribing ||
                     VoiceAgentManager.Instance.CurrentState == AgentState.Thinking))
                {
                    elapsed = 0f;
                }
                else
                {
                    elapsed += Time.deltaTime;
                }
                yield return null;
            }

            if (isWaitingForQuizAnswer)
            {
                isWaitingForQuizAnswer = false;
                yield return SpeakAndWait($"Let's keep moving! {currentQuizQuestion?.explanation}");
            }
        }
    }
}
