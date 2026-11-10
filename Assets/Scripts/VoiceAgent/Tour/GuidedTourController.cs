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
            }

            // ==========================================
            // INTRO: Welcome to the 5G Modular Academy
            // ==========================================
            currentStep = ModuleStep.Intro;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Welcome to the 5G VR Laboratory! Today, we will master 5G through four interactive modules. We will start at the wall banner to study the complete CUPS architecture, and then inspect our 3D lab instruments one by one.");

            // ==========================================
            // MODULE 1: 5G ARCHITECTURE BASED ON CUPS
            // ==========================================
            currentStep = ModuleStep.Module1_Banner_Overview;
            OnStepChanged?.Invoke(currentStep);

            // Zoom camera directly in front of the wall banner and pop it forward
            PCInteractionTester.InspectEquipment(EquipmentId.banner);
            yield return new WaitForSeconds(1.3f);
            CupsBannerDisplay.Instance?.SetPopUpState(true);
            CupsBannerDisplay.Instance?.SetHighlightSection("all");

            yield return SpeakAndWait("Module 1: 5G Architecture based on CUPS, as standardized in 3GPP Specification 23.214. CUPS stands for Control and User Plane Separation, allowing the control signaling and user data forwarding to scale independently.");

            // Module 1 - Step A: UEs and Access Networks
            currentStep = ModuleStep.Module1_Banner_UE_Access;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("ue");
            yield return SpeakAndWait("On the left, notice the diverse User Equipment, spanning IoT sensors, connected autonomous cars, 5G smartphones, and fixed computers. Each demands radically different latency and throughput.");

            CupsBannerDisplay.Instance?.SetHighlightSection("access");
            yield return SpeakAndWait("These UEs connect through heterogeneous access networks: 5G gNodeB New Radio, legacy 4G eNodeB, untrusted Wi-Fi routed via the N3IWF IPsec gateway, and fixed wireline.");

            // Module 1 - Step B: 5G Control Plane (SBA)
            currentStep = ModuleStep.Module1_Banner_ControlPlane;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("control");
            yield return SpeakAndWait("Look at the top-right box: the 5G Control Plane. Built on a cloud-native Service-Based Architecture, modular functions like the AMF for mobility, SMF for session management, UDM and AUSF for authentication, and NSSF for network slicing communicate over REST HTTP/2 APIs.");

            // Module 1 - Step C: 5G User Plane & CUPS
            currentStep = ModuleStep.Module1_Banner_UserPlane_CUPS;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("user_plane");
            yield return SpeakAndWait("Below it is the 5G User Plane. Here lies the true power of CUPS: while the SMF centrally manages session rules over the N4 PFCP interface, high-speed User Plane Functions, or UPFs, are distributed directly to the cell site. This enables local breakout into a Local Data Network, cutting round-trip latency to under one millisecond!");

            // Module 1 Quiz
            currentStep = ModuleStep.Module1_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "In the 5G CUPS architecture, which network function handles data packet forwarding at the edge?",
                acceptedKeywords = new[] { "upf", "user plane", "user plane function" },
                explanation = "The UPF (User Plane Function) forwards user payload data and enables local edge breakout under SMF control."
            };
            yield return SpeakAndWait($"Check on Module 1: {currentQuizQuestion.question} Hold [V] to answer!");
            yield return WaitForAnswerOrTimeout(8f);

            // Clean up Module 1 visuals before transitioning
            CupsBannerDisplay.Instance?.SetHighlightSection("none");
            CupsBannerDisplay.Instance?.SetPopUpState(false);
            yield return new WaitForSeconds(0.6f);

            // ==========================================
            // MODULE 2: gNodeB BASE STATION & RADIO
            // ==========================================
            currentStep = ModuleStep.Module2_gNodeB_Intro;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Outstanding! Now let's move to Module 2 to inspect our 3D gNodeB base station antenna!");

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
