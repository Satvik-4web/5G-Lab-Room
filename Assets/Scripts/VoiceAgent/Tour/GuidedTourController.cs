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
            Module1_Banner_UE,
            Module1_Banner_Access,
            Module1_Banner_ControlPlane,
            Module1_Banner_UserPlane_CUPS,
            Module1_Quiz,

            // MODULE 2: Summary of 5G Use Cases (Wall Banner 2: eMBB, mMTC, uRLLC)
            Module2_UseCases_Overview,
            Module2_UseCases_eMBB,
            Module2_UseCases_mMTC,
            Module2_UseCases_uRLLC,
            Module2_UseCases_Quiz,

            // MODULE 3: gNodeB Base Station & Massive MIMO Radio
            Module3_gNodeB_Intro,
            Module3_gNodeB_Beamforming,
            Module3_gNodeB_Quiz,

            // MODULE 4: Mobile Edge Computing (MEC) Server Rack & Distributed UPF
            Module4_MEC_Intro,
            Module4_MEC_LowLatency,
            Module4_MEC_Quiz,

            // MODULE 5: RF Spectrum Analyzer & Signal Diagnostics
            Module5_Spectrum_Intro,
            Module5_Spectrum_Diagnostics,
            Module5_Spectrum_Quiz,

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
        private bool isWaitingForNextButton = false;
        private QuizQuestion currentQuizQuestion = null;

        public bool IsTourActive => currentStep != ModuleStep.NotStarted && currentStep != ModuleStep.Completed;
        public bool IsWaitingForNextButton => isWaitingForNextButton;
        public ModuleStep CurrentStep => currentStep;

        public event Action<ModuleStep> OnStepChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        private void Start()
        {
            UseCasesBannerDisplay.EnsureInstance();

            if (autoStartModule1OnPlay)
            {
                StartCoroutine(AutoStartRoutine());
            }
        }

        private IEnumerator AutoStartRoutine()
        {
            UseCasesBannerDisplay.EnsureInstance();

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
            AdvanceToNextModule();
        }

        public void AdvanceToNextModule()
        {
            if (currentStep < ModuleStep.Module2_UseCases_Overview)
            {
                AdvanceToModule2();
            }
            else if (currentStep < ModuleStep.Module3_gNodeB_Intro)
            {
                AdvanceToModule3();
            }
            else
            {
                isWaitingForQuizAnswer = false;
                isWaitingForNextButton = false;
            }
        }

        public void AdvanceToModule2()
        {
            if (currentStep >= ModuleStep.Module2_UseCases_Overview && currentStep <= ModuleStep.Module2_UseCases_Quiz)
            {
                Debug.Log("[GuidedTourController] Already in Module 2.");
                return;
            }

            Debug.Log("[GuidedTourController] Advancing from Module 1 to Module 2 (5G Use Cases)...");
            isWaitingForQuizAnswer = false;
            isWaitingForNextButton = false;

            // Stop any in-flight narrative audio and clear dialogue
            VoiceAgentManager.Instance?.CancelInFlightOperations();
            VoiceAgentManager.Instance?.GetComponentInChildren<AgentSpeaker>()?.StopSpeaking();
            VoiceAgentManager.Instance?.HUD?.ClearDialogue();

            // Clean up Module 1 visuals immediately
            CupsBannerDisplay.Instance?.SetHighlightSection("none");
            CupsBannerDisplay.Instance?.HideTargetElement();
            CupsBannerDisplay.Instance?.SetPopUpState(false);

            if (tourRoutine != null)
            {
                StopCoroutine(tourRoutine);
                tourRoutine = null;
            }

            tourRoutine = StartCoroutine(ResumeTourFromModule2());
        }

        public void AdvanceToModule3()
        {
            if (currentStep >= ModuleStep.Module3_gNodeB_Intro && currentStep <= ModuleStep.Module3_gNodeB_Quiz)
            {
                return;
            }

            Debug.Log("[GuidedTourController] Advancing to Module 3 (gNodeB)...");
            isWaitingForQuizAnswer = false;
            isWaitingForNextButton = false;

            VoiceAgentManager.Instance?.CancelInFlightOperations();
            VoiceAgentManager.Instance?.GetComponentInChildren<AgentSpeaker>()?.StopSpeaking();
            VoiceAgentManager.Instance?.HUD?.ClearDialogue();

            UseCasesBannerDisplay.Instance?.SetHighlightSection("none");
            UseCasesBannerDisplay.Instance?.HideTargetElement();
            UseCasesBannerDisplay.Instance?.SetPopUpState(false);
            SetObscuringEquipmentVisible(true);

            if (tourRoutine != null)
            {
                StopCoroutine(tourRoutine);
                tourRoutine = null;
            }

            tourRoutine = StartCoroutine(ResumeTourFromModule3());
        }

        private IEnumerator ResumeTourFromModule2()
        {
            yield return RunModule2Sequence();
            yield return RunModule3Sequence();
            yield return RunModule4Sequence();
            yield return RunModule5Sequence();
            yield return RunCourseCompletion();
        }

        private IEnumerator ResumeTourFromModule3()
        {
            yield return RunModule3Sequence();
            yield return RunModule4Sequence();
            yield return RunModule5Sequence();
            yield return RunCourseCompletion();
        }

        private void ShowNextButton(string label, Action onClick)
        {
            if (VoiceAgentManager.Instance != null && VoiceAgentManager.Instance.HUD != null)
            {
                VoiceAgentManager.Instance.HUD.ShowNextButton(label, onClick);
            }
        }

        private void HideNextButton()
        {
            VoiceAgentManager.Instance?.HUD?.HideNextButton();
        }

        private GameObject cachedGNodeB;
        private GameObject cachedPC;
        private GameObject cachedMEC;

        private void CacheSceneReferences()
        {
            if (cachedGNodeB == null) cachedGNodeB = GameObject.Find("gNodeB (Base Station)");
            if (cachedPC == null) cachedPC = GameObject.Find("PC");
            if (cachedMEC == null) cachedMEC = GameObject.Find("Mobile Edge Computing (MEC-Core)");
        }

        private void SetObscuringEquipmentVisible(bool visible)
        {
            CacheSceneReferences();
            if (cachedGNodeB != null) cachedGNodeB.SetActive(visible);
            if (cachedPC != null) cachedPC.SetActive(visible);
            if (cachedMEC != null) cachedMEC.SetActive(visible);
        }

        public void EndTour()
        {
            if (tourRoutine != null)
            {
                StopCoroutine(tourRoutine);
                tourRoutine = null;
            }
            HideNextButton();
            currentStep = ModuleStep.Completed;
            SetObscuringEquipmentVisible(true);
            CupsBannerDisplay.Instance?.SetHighlightSection("none");
            CupsBannerDisplay.Instance?.HideTargetElement();
            CupsBannerDisplay.Instance?.SetPopUpState(false);
            UseCasesBannerDisplay.Instance?.SetHighlightSection("none");
            UseCasesBannerDisplay.Instance?.HideTargetElement();
            UseCasesBannerDisplay.Instance?.SetPopUpState(false);
            VoiceAgentManager.Instance?.HUD?.SetCinemaSubtitleMode(false);
            PCInteractionTester.ExitCurrentInspection();
            OnStepChanged?.Invoke(currentStep);

            if (VoiceAgentManager.Instance != null)
            {
                VoiceAgentManager.Instance.SetState(AgentState.Idle);
            }
        }

        private void OnDestroy()
        {
            if (tourRoutine != null)
            {
                StopCoroutine(tourRoutine);
                tourRoutine = null;
            }
            HideNextButton();
            SetObscuringEquipmentVisible(true);
        }

        public bool SubmitSpokenAnswer(string transcript, out string feedback)
        {
            feedback = "";
            string clean = transcript.ToLowerInvariant();

            // Check if user requested next module via speech
            if (clean.Contains("next") || clean.Contains("continue") || clean.Contains("proceed") || clean.Contains("module 2") || clean.Contains("module two"))
            {
                if (currentStep < ModuleStep.Module2_UseCases_Overview)
                {
                    AdvanceToModule2();
                    feedback = "Advancing to Module 2: 5G Use Cases!";
                    return true;
                }
            }

            if (!isWaitingForQuizAnswer || currentQuizQuestion == null) return false;

            isWaitingForQuizAnswer = false;
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

            yield return RunModule1Sequence();
            yield return RunModule2Sequence();
            yield return RunModule3Sequence();
            yield return RunModule4Sequence();
            yield return RunModule5Sequence();
            yield return RunCourseCompletion();
        }

        private IEnumerator RunModule1Sequence()
        {
            // ==========================================
            // MODULE 1: 5G ARCHITECTURE BASED ON CUPS (WALL BANNER)
            // Direct launch: camera immediately glides to the wall banner and pops it forward
            // ==========================================
            currentStep = ModuleStep.Module1_Banner_Overview;
            OnStepChanged?.Invoke(currentStep);

            // Temporarily hide gNodeB, PC, and MEC so they never obscure the banner presentation
            SetObscuringEquipmentVisible(false);

            // Pop banner forward into open aisle and glide camera for crystal-clear view
            CupsBannerDisplay.Instance?.SetPopUpState(true);
            PCInteractionTester.InspectEquipment(EquipmentId.banner);
            yield return new WaitForSeconds(1.0f);

            CupsBannerDisplay.Instance?.SetHighlightSection("all");
            CupsBannerDisplay.Instance?.HideTargetElement();

            // Display Next button right away so user can advance anytime
            ShowNextButton("NEXT: MODULE 2 >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule2());

            yield return SpeakAndWait("Hey there! Welcome to our 5G Lab. I'm Aria, and I'll be walking you through the lab today. Right in front of us on the wall is our university's official 5G Architecture poster. It lays out the foundation of modern 5G—called CUPS, or Control and User Plane Separation. Basically, older cellular networks bundled everything into one big bottleneck. In 5G, we completely separated the brain of the network from the muscle that moves data. Let's break it down step-by-step!", "mod_line_0.wav");

            // Module 1 - Step A: User Equipment (UE) Column & Icons
            currentStep = ModuleStep.Module1_Banner_UE;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("ue");
            CupsBannerDisplay.Instance?.HideTargetElement();
            yield return SpeakAndWait("First, let's look at the left column under UE, or User Equipment. 5G isn't just for phones—it connects IoT drones at the top, autonomous self-driving cars, handheld smartphones, and enterprise workstations. Notice how every single device starts its journey right here.", "mod_line_1.wav");

            // Module 1 - Step B: Access Network Radios
            currentStep = ModuleStep.Module1_Banner_Access;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("access");
            CupsBannerDisplay.Instance?.HideTargetElement();
            yield return SpeakAndWait("Right next to the devices is the Access Network column. They connect through 5G New Radio base stations, existing 4G LTE towers, and even home Wi-Fi and wired fiber. Everything plugs into the network through one of these radio links.", "mod_line_2.wav");

            // Module 1 - Step C: 5G Control Plane (SBA)
            currentStep = ModuleStep.Module1_Banner_ControlPlane;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("control");
            CupsBannerDisplay.Instance?.SetTargetElement("cp_amf_smf");
            yield return SpeakAndWait("Now, look up at the top box highlighted in gold. That's the 5G Control Plane. Think of this as the brain. You've got the AMF tracking your device as you move around, the SMF setting up your data sessions, and the UDM verifying your SIM. But here's the golden rule of CUPS: this entire top box only handles signaling instructions. It never touches your actual videos, downloads, or user data packets.", "mod_line_3.wav");

            // Module 1 - Step D: 5G User Plane & CUPS
            currentStep = ModuleStep.Module1_Banner_UserPlane_CUPS;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("user_plane");
            CupsBannerDisplay.Instance?.SetTargetElement("up_localdn");
            yield return SpeakAndWait("Down below, in green, is the 5G User Plane. This is the muscle that carries all your data. Instead of routing traffic all the way to a distant cloud server, 5G distributes these lightweight UPFs right to the edge of the network. See that Local DN icon at the bottom? User data can break out right here to a local server in under one millisecond! That's the secret to zero-lag gaming, real-time robotics, and safe autonomous driving.", "mod_line_4.wav");

            // Module 1 Quiz
            currentStep = ModuleStep.Module1_Quiz;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("none");
            CupsBannerDisplay.Instance?.HideTargetElement();
            currentQuizQuestion = new QuizQuestion
            {
                question = "On this poster, which edge function actually forwards the user data to give us that sub-millisecond local speed?",
                acceptedKeywords = new[] { "upf", "user plane", "user plane function" },
                explanation = "The UPF, or User Plane Function, forwards user data packets at the edge under the central session control of the SMF."
            };
            yield return SpeakAndWait("Alright, quick check to see if that makes sense! On this poster, which edge function is doing the heavy lifting of forwarding user data so we get that sub-millisecond local speed? Hold down the V key and let me know!", "mod_line_5.wav");
            yield return WaitForAnswerOrTimeout(10f);

            // Clean up Module 1 visuals before transitioning
            CupsBannerDisplay.Instance?.SetHighlightSection("none");
            CupsBannerDisplay.Instance?.HideTargetElement();
            CupsBannerDisplay.Instance?.SetPopUpState(false);
            yield return new WaitForSeconds(0.4f);

            // Prompt user with Next button to proceed to Module 2
            ShowNextButton("NEXT: MODULE 2 >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule2());
            yield return SpeakAndWait("Great job on Module 1! Click Next or press N whenever you're ready to head over to Module 2.", "mod1_next_prompt.wav");

            // Wait until user clicks Next or presses [N] / [Space]
            isWaitingForNextButton = true;
            while (isWaitingForNextButton)
            {
                yield return null;
            }
        }

        private IEnumerator RunModule2Sequence()
        {
            // ==========================================
            // MODULE 2: SUMMARY OF 5G USE CASES (WALL BANNER 2)
            // Glide camera to Poster2 and pop it into the open aisle
            // ==========================================
            currentStep = ModuleStep.Module2_UseCases_Overview;
            OnStepChanged?.Invoke(currentStep);

            // Clean up Module 1 visuals immediately
            CupsBannerDisplay.Instance?.SetHighlightSection("none");
            CupsBannerDisplay.Instance?.HideTargetElement();
            CupsBannerDisplay.Instance?.SetPopUpState(false);
            SetObscuringEquipmentVisible(false);

            // Update Next button for Module 3
            ShowNextButton("NEXT: MODULE 3 >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule3());

            UseCasesBannerDisplay.EnsureInstance()?.SetPopUpState(true);
            PCInteractionTester.InspectEquipment(EquipmentId.banner_usecases);
            yield return new WaitForSeconds(1.0f);

            UseCasesBannerDisplay.Instance?.SetHighlightSection("all");
            UseCasesBannerDisplay.Instance?.HideTargetElement();

            yield return SpeakAndWait("Awesome job on Module 1! Now let's slide right over to Module 2 to explore our 5G Use Cases banner. You see, 5G isn't just about faster downloads on our smartphones; the telecom world actually split all human and device communication into three distinct superpowers: eMBB, mMTC, and uRLLC. I'll highlight each tier with our laser frame as we go, so you can follow right along with me. Let's start at the top!", "usecases_line_0.wav");

            // Module 2 - Step A: eMBB (Enhanced Mobile Broadband)
            currentStep = ModuleStep.Module2_UseCases_eMBB;
            OnStepChanged?.Invoke(currentStep);
            UseCasesBannerDisplay.Instance?.SetHighlightSection("embb");
            UseCasesBannerDisplay.Instance?.SetTargetElement("embb_vr");
            yield return SpeakAndWait("Up first in electric cyan is eMBB, or Enhanced Mobile Broadband. Think of this as the ultimate superhighway for raw speed and bandwidth. We're talking up to 20 Gigabits per second! Notice the apps highlighted here: it powers lag-free 8K streaming, immersive virtual and augmented reality worlds, and fiber-speed fixed wireless for our homes—even if you're cruising on a high-speed train at 500 kilometers an hour.", "usecases_line_1.wav");

            // Module 2 - Step B: mMTC (Massive Machine Type Communication)
            currentStep = ModuleStep.Module2_UseCases_mMTC;
            OnStepChanged?.Invoke(currentStep);
            UseCasesBannerDisplay.Instance?.SetHighlightSection("mmtc");
            UseCasesBannerDisplay.Instance?.SetTargetElement("mmtc_smartcities");
            yield return SpeakAndWait("Next, look at the middle green tier: mMTC, which stands for Massive Machine-Type Communication. If eMBB is a superhighway for fast cars, mMTC is a giant stadium parking lot. It isn't about peak speed; it's engineered to connect up to one million smart devices in a single square kilometer! Imagine city water meters, smart agriculture sensors, and wearable fitness trackers that can quietly run on a single tiny coin battery for more than ten years.", "usecases_line_2.wav");

            // Module 2 - Step C: uRLLC (Ultra-Reliable Low Latency Communication)
            currentStep = ModuleStep.Module2_UseCases_uRLLC;
            OnStepChanged?.Invoke(currentStep);
            UseCasesBannerDisplay.Instance?.SetHighlightSection("urllc");
            UseCasesBannerDisplay.Instance?.SetTargetElement("urllc_surgery");
            yield return SpeakAndWait("And down at the bottom in coral red is 5G's most critical superpower: uRLLC, or Ultra-Reliable Low Latency Communication. This is the life-saver. It slashes network delay to under one millisecond with ninety-nine point nine-nine-nine percent reliability! Look at the robotic surgery and autonomous vehicle icons highlighted here: when a surgeon operates on a patient from across the world, or self-driving cars coordinate split-second emergency braking, every microsecond counts.", "usecases_line_3.wav");

            // Module 2 Quiz
            currentStep = ModuleStep.Module2_UseCases_Quiz;
            OnStepChanged?.Invoke(currentStep);
            UseCasesBannerDisplay.Instance?.SetHighlightSection("none");
            UseCasesBannerDisplay.Instance?.HideTargetElement();
            currentQuizQuestion = new QuizQuestion
            {
                question = "If a surgeon is performing a delicate remote robotic surgery from another city, which of the three 5G pillars makes that life-saving precision possible?",
                acceptedKeywords = new[] { "urllc", "ultra reliable", "low latency", "ultra-reliable" },
                explanation = "uRLLC provides sub-millisecond latency and 99.999% reliability essential for real-time robotic surgery and life-critical applications."
            };
            yield return SpeakAndWait("Let's do a quick knowledge check for Module 2! If a surgeon is performing a delicate remote robotic surgery from another city, which of the three 5G pillars makes that life-saving precision possible? Hold down the V key and let me know!", "usecases_line_4.wav");
            yield return WaitForAnswerOrTimeout(10f);

            // Transition from Module 2 to Module 3 physical hardware
            UseCasesBannerDisplay.Instance?.SetHighlightSection("none");
            UseCasesBannerDisplay.Instance?.HideTargetElement();
            UseCasesBannerDisplay.Instance?.SetPopUpState(false);
            yield return new WaitForSeconds(0.8f);

            // Re-activate physical lab equipment for Module 3 hands-on inspection
            SetObscuringEquipmentVisible(true);
        }

        private IEnumerator RunModule3Sequence()
        {
            // ==========================================
            // MODULE 3: gNodeB BASE STATION & RADIO
            // ==========================================
            currentStep = ModuleStep.Module3_gNodeB_Intro;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Spot on! uRLLC provides the sub-millisecond reaction time and rock-solid reliability critical for robotic surgery. Now that we understand both the network architecture and the use cases, let's step over to Module 3 to inspect our physical 5G base station in 3D!", "usecases_line_5.wav");

            PCInteractionTester.InspectEquipment(EquipmentId.gnodeb);
            yield return new WaitForSeconds(1.3f);

            currentStep = ModuleStep.Module3_gNodeB_Beamforming;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("This is our gNodeB, connecting mobile devices over 5G New Radio. Notice the Massive MIMO antenna array on top—it packs 64 transmit and 64 receive elements. Instead of broadcasting radio signals everywhere like older towers, it uses beamforming to focus radio waves into tight, steerable beams aimed straight at active users. That eliminates interference and gives you blazing fast speeds.", "mod_line_7.wav");

            // Module 3 Quiz
            currentStep = ModuleStep.Module3_gNodeB_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "What does active beamforming actually do to 5G radio signals?",
                acceptedKeywords = new[] { "focus", "direct", "narrow", "target", "aim", "steer" },
                explanation = "Beamforming focuses radio wave energy into targeted, narrow beams directed at active user devices."
            };
            yield return SpeakAndWait("Quick check on Module 3: What does active beamforming actually do to 5G radio signals? Hold down the V key and tell me!", "mod_line_8.wav");
            yield return WaitForAnswerOrTimeout(8f);
        }

        private IEnumerator RunModule4Sequence()
        {
            // ==========================================
            // MODULE 4: MEC-CORE SERVER RACK & EDGE UPF
            // ==========================================
            currentStep = ModuleStep.Module4_MEC_Intro;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Awesome! Beamforming focuses radio energy directly at active users. Next up, let's step to Module 4: our Mobile Edge Computing core server rack!", "mod_line_9.wav");

            PCInteractionTester.InspectEquipment(EquipmentId.mec);
            yield return new WaitForSeconds(1.3f);

            currentStep = ModuleStep.Module4_MEC_LowLatency;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Here's our MEC server rack. Remember the UPF and Local Data Network we saw on the banner? This rack is where they actually live! By running high-performance compute and user plane functions right here on-site, we cut out long internet round-trips. That's how we get sub-millisecond response times for real-time AI and robotics.", "mod_line_10.wav");

            // Module 4 Quiz
            currentStep = ModuleStep.Module4_MEC_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "Why do we put MEC servers right at the edge of the network instead of the cloud?",
                acceptedKeywords = new[] { "latency", "speed", "fast", "closer", "delay", "millisecond", "lag" },
                explanation = "Placing MEC servers at the edge eliminates backhaul delays and delivers sub-millisecond response times."
            };
            yield return SpeakAndWait("Quick check on Module 4: Why do we put MEC servers right at the edge of the network instead of the cloud? Hold down the V key to answer!", "mod_line_11.wav");
            yield return WaitForAnswerOrTimeout(8f);
        }

        private IEnumerator RunModule5Sequence()
        {
            // ==========================================
            // MODULE 5: RF SPECTRUM ANALYZER
            // ==========================================
            currentStep = ModuleStep.Module5_Spectrum_Intro;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Exactly—putting compute at the edge kills latency. Finally, let's head over to Module 5 to inspect our precision RF Spectrum Analyzer!", "mod_line_12.wav");

            PCInteractionTester.InspectEquipment(EquipmentId.spectrum);
            yield return new WaitForSeconds(1.3f);

            currentStep = ModuleStep.Module5_Spectrum_Diagnostics;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("This is our RF Spectrum Analyzer. It's the ultimate lab diagnostic tool. It captures high-frequency radio waves across Sub-6 and millimeter wave bands, plotting signal amplitude on the vertical axis against frequency on the horizontal axis. That lets us verify signal purity, check bandwidth, and hunt down any rogue interference.", "mod_line_13.wav");

            // Module 5 Quiz
            currentStep = ModuleStep.Module5_Spectrum_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "What two variables does the spectrum analyzer display on its screen?",
                acceptedKeywords = new[] { "power", "amplitude", "frequency", "freq" },
                explanation = "The spectrum analyzer plots signal power or amplitude on the vertical axis against frequency on the horizontal axis."
            };
            yield return SpeakAndWait("Final check on Module 5: What two variables does the spectrum analyzer display on its screen? Hold down the V key and let me know!", "mod_line_14.wav");
            yield return WaitForAnswerOrTimeout(8f);
        }

        private IEnumerator RunCourseCompletion()
        {
            // ==========================================
            // RECAP & GRADUATION
            // ==========================================
            currentStep = ModuleStep.RecapAndWrapUp;
            OnStepChanged?.Invoke(currentStep);
            HideNextButton();
            PCInteractionTester.ExitCurrentInspection();
            yield return new WaitForSeconds(1.1f);

            int score = VoiceAgentManager.Instance?.Memory?.QuizScore ?? 0;
            int total = VoiceAgentManager.Instance?.Memory?.TotalQuizQuestions ?? 5;
            yield return SpeakAndWait("Fantastic work! You have successfully finished all five modules in our 5G Lab! From the CUPS architecture and Use Cases banners on the wall to the gNodeB, the MEC edge servers, and the Spectrum Analyzer. Whenever you want to explore more or test your knowledge, just hold V and ask me anything!", "mod_line_15.wav");

            currentStep = ModuleStep.Completed;
            OnStepChanged?.Invoke(currentStep);
            if (VoiceAgentManager.Instance != null)
            {
                VoiceAgentManager.Instance.HUD?.SetCinemaSubtitleMode(false);
                VoiceAgentManager.Instance.SetState(AgentState.Idle);
            }
        }

        private IEnumerator SpeakAndWait(string text, string audioCacheClip = null)
        {
            if (VoiceAgentManager.Instance != null)
            {
                bool finished = false;
                VoiceAgentManager.Instance.SpeakNarrativeDirect(text, audioCacheClip, () => finished = true);
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
