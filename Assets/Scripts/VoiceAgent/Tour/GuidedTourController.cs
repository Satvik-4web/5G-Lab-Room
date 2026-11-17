using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Master 5-Module Guided Tour & Course Controller for the 5G Virtual Lab Simulator.
    /// Strictly sequences through:
    /// Module 1: 5G Network Architecture based on CUPS (Wall Banner 1)
    /// Module 2: Summary of 5G Use Cases (Wall Banner 2: eMBB, mMTC, uRLLC)
    /// Module 3: Spectrometer (RF Spectrum Analyzer on Workbench)
    /// Module 4: 5G Antenna & Massive MIMO Radio (Tripod Array)
    /// Module 5: gNodeB Base Station & 5G Core Server Rack
    /// Followed by Lab Course Completion & Free Roam.
    /// </summary>
    public class GuidedTourController : MonoBehaviour
    {
        public static GuidedTourController Instance { get; private set; }

        public enum ModuleStep
        {
            NotStarted,
            Intro,

            // MODULE 1: 5G Network Architecture based on CUPS (Wall Banner 1)
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

            // MODULE 3: Spectrometer (RF Spectrum Analyzer on Workbench)
            Module3_Spectrum_Intro,
            Module3_Spectrum_Diagnostics,
            Module3_Spectrum_Quiz,

            // MODULE 4: 5G Antenna & Massive MIMO Radio (Tripod Array)
            Module4_Antenna_Intro,
            Module4_Antenna_Beamforming,
            Module4_Antenna_Quiz,

            // MODULE 5: gNodeB Base Station & 5G Core Rack
            Module5_gNodeB_Intro,
            Module5_gNodeB_ComputeAndEdge,
            Module5_gNodeB_Quiz,

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
        private int currentModuleIndex = 0; // 0=None, 1=Mod1, 2=Mod2, 3=Mod3, 4=Mod4, 5=Mod5, 6=Completed
        private uint tourSessionId = 0;
        private float lastAdvanceTime = 0f;

        private bool isWaitingForQuizAnswer = false;
        private bool isWaitingForNextButton = false;
        private QuizQuestion currentQuizQuestion = null;

        private Transform xrOrigin;
        private Coroutine cameraGlideRoutine;

        public bool IsTourActive => currentStep != ModuleStep.NotStarted && currentStep != ModuleStep.Completed;
        public bool IsWaitingForNextButton => isWaitingForNextButton;
        public ModuleStep CurrentStep => currentStep;
        public int CurrentModuleIndex => currentModuleIndex;

        public event Action<ModuleStep> OnStepChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else { Destroy(gameObject); return; }

            CacheSceneReferences();
            UseCasesBannerDisplay.EnsureInstance();
        }

        private void Start()
        {
            UseCasesBannerDisplay.EnsureInstance();
            EnsureAllEquipmentActive();

            if (autoStartModule1OnPlay)
            {
                StartCoroutine(AutoStartRoutine());
            }
        }

        private void Update()
        {
            // Global keyboard advance shortcut: [N], [Space]
            if (IsTourActive)
            {
                bool nPressed = false;
                if (Keyboard.current != null)
                {
                    if (Keyboard.current.nKey.wasPressedThisFrame || Keyboard.current.spaceKey.wasPressedThisFrame)
                    {
                        nPressed = true;
                    }
                }
                try
                {
                    if (Input.GetKeyDown(KeyCode.N) || Input.GetKeyDown(KeyCode.Space))
                    {
                        nPressed = true;
                    }
                }
                catch { }

                if (nPressed)
                {
                    AdvanceToNextModule();
                }
            }
        }

        private IEnumerator AutoStartRoutine()
        {
            UseCasesBannerDisplay.EnsureInstance();
            yield return new WaitForSeconds(autoStartDelaySeconds);

            if (currentStep == ModuleStep.NotStarted)
            {
                Debug.Log("[GuidedTourController] Auto-starting directly into Module 1...");
                StartTour("full");
            }
        }

        public void StartTour(string mode = "full", string specificEquipment = null)
        {
            JumpToModule(1);
        }

        public void NextStop()
        {
            AdvanceToNextModule();
        }

        public void AdvanceToNextModule()
        {
            if (Time.unscaledTime - lastAdvanceTime < 0.4f) return;
            lastAdvanceTime = Time.unscaledTime;

            if (currentModuleIndex <= 0)
            {
                JumpToModule(1);
            }
            else if (currentModuleIndex == 1)
            {
                JumpToModule(2);
            }
            else if (currentModuleIndex == 2)
            {
                JumpToModule(3);
            }
            else if (currentModuleIndex == 3)
            {
                JumpToModule(4);
            }
            else if (currentModuleIndex == 4)
            {
                JumpToModule(5);
            }
            else
            {
                JumpToModule(6);
            }
        }

        public void AdvanceToModule2() => JumpToModule(2);
        public void AdvanceToModule3() => JumpToModule(3);
        public void AdvanceToModule4() => JumpToModule(4);
        public void AdvanceToModule5() => JumpToModule(5);
        public void AdvanceToCompletion() => JumpToModule(6);

        public void JumpToModule(int targetModule)
        {
            if (Time.unscaledTime - lastAdvanceTime < 0.25f && targetModule == currentModuleIndex) return;
            lastAdvanceTime = Time.unscaledTime;

            tourSessionId++;
            uint session = tourSessionId;

            Debug.Log($"[GuidedTourController] Jumping directly to Module {targetModule} (Session {session})...");

            // Stop all in-flight coroutines on this controller
            StopAllCoroutines();
            if (cameraGlideRoutine != null)
            {
                StopCoroutine(cameraGlideRoutine);
                cameraGlideRoutine = null;
            }

            // Immediately silence active speech and clear subtitles
            VoiceAgentManager.Instance?.CancelInFlightOperations();
            VoiceAgentManager.Instance?.GetComponentInChildren<AgentSpeaker>()?.StopSpeaking();
            VoiceAgentManager.Instance?.HUD?.ClearDialogue();

            isWaitingForQuizAnswer = false;
            isWaitingForNextButton = false;

            // Reset banner positions, laser reticles, and ensure all equipment is active
            ResetAllBannersAndEquipment();

            if (VoiceAgentManager.Instance != null)
            {
                VoiceAgentManager.Instance.SetState(AgentState.Touring);
                VoiceAgentManager.Instance.HUD?.SetCinemaSubtitleMode(true);
            }

            currentModuleIndex = targetModule;

            switch (targetModule)
            {
                case 1:
                    StartCoroutine(RunModule1Sequence(session));
                    break;
                case 2:
                    StartCoroutine(RunModule2Sequence(session));
                    break;
                case 3:
                    StartCoroutine(RunModule3Sequence(session));
                    break;
                case 4:
                    StartCoroutine(RunModule4Sequence(session));
                    break;
                case 5:
                    StartCoroutine(RunModule5Sequence(session));
                    break;
                default:
                    StartCoroutine(RunCourseCompletion(session));
                    break;
            }
        }

        // =========================================================================
        // MODULE SEQUENCES
        // =========================================================================

        private IEnumerator RunModule1Sequence(uint session)
        {
            // ==========================================
            // MODULE 1: 5G ARCHITECTURE BASED ON CUPS (WALL BANNER 1)
            // ==========================================
            currentStep = ModuleStep.Module1_Banner_Overview;
            OnStepChanged?.Invoke(currentStep);

            // Pop Banner 1 forward and glide camera for crystal-clear view
            CupsBannerDisplay.Instance?.SetPopUpState(true);
            CupsBannerDisplay.Instance?.SetHighlightSection("all");
            CupsBannerDisplay.Instance?.HideTargetElement();

            // Camera: (2.10, 1.95, -1.80) facing +X
            GlideCameraTo(new Vector3(2.10f, 1.95f, -1.80f), Quaternion.Euler(0f, 90f, 0f), 1.2f);
            ShowNextButton("NEXT: MODULE 2 (USE CASES) >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule2());

            yield return new WaitForSeconds(0.6f);
            if (session != tourSessionId) yield break;

            yield return SpeakAndWait("Hey there! Welcome to our 5G Lab. I'm Aria, and I'll be walking you through the lab today. Right in front of us on the wall is our university's official 5G Architecture poster. It lays out the foundation of modern 5G—called CUPS, or Control and User Plane Separation. Basically, older cellular networks bundled everything into one big bottleneck. In 5G, we completely separated the brain of the network from the muscle that moves data. Let's break it down step-by-step!", "mod_line_0.wav");
            if (session != tourSessionId) yield break;

            // Step A: UE
            currentStep = ModuleStep.Module1_Banner_UE;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("ue");
            CupsBannerDisplay.Instance?.HideTargetElement();
            yield return SpeakAndWait("First, let's look at the left column under UE, or User Equipment. 5G isn't just for phones—it connects IoT drones at the top, autonomous self-driving cars, handheld smartphones, and enterprise workstations. Notice how every single device starts its journey right here.", "mod_line_1.wav");
            if (session != tourSessionId) yield break;

            // Step B: Access Network
            currentStep = ModuleStep.Module1_Banner_Access;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("access");
            CupsBannerDisplay.Instance?.HideTargetElement();
            yield return SpeakAndWait("Right next to the devices is the Access Network column. They connect through 5G New Radio base stations, existing 4G LTE towers, and even home Wi-Fi and wired fiber. Everything plugs into the network through one of these radio links.", "mod_line_2.wav");
            if (session != tourSessionId) yield break;

            // Step C: Control Plane
            currentStep = ModuleStep.Module1_Banner_ControlPlane;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("control");
            CupsBannerDisplay.Instance?.SetTargetElement("cp_amf_smf");
            yield return SpeakAndWait("Now, look up at the top box highlighted in gold. That's the 5G Control Plane. Think of this as the brain. You've got the AMF tracking your device as you move around, the SMF setting up your data sessions, and the UDM verifying your SIM. But here's the golden rule of CUPS: this entire top box only handles signaling instructions. It never touches your actual videos, downloads, or user data packets.", "mod_line_3.wav");
            if (session != tourSessionId) yield break;

            // Step D: User Plane
            currentStep = ModuleStep.Module1_Banner_UserPlane_CUPS;
            OnStepChanged?.Invoke(currentStep);
            CupsBannerDisplay.Instance?.SetHighlightSection("user_plane");
            CupsBannerDisplay.Instance?.SetTargetElement("up_localdn");
            yield return SpeakAndWait("Down below, in green, is the 5G User Plane. This is the muscle that carries all your data. Instead of routing traffic all the way to a distant cloud server, 5G distributes these lightweight UPFs right to the edge of the network. See that Local DN icon at the bottom? User data can break out right here to a local server in under one millisecond! That's the secret to zero-lag gaming, real-time robotics, and safe autonomous driving.", "mod_line_4.wav");
            if (session != tourSessionId) yield break;

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
            if (session != tourSessionId) yield break;

            yield return WaitForAnswerOrTimeout(8f, session);
            if (session != tourSessionId) yield break;

            CupsBannerDisplay.Instance?.SetPopUpState(false);
            ShowNextButton("NEXT: MODULE 2 (USE CASES) >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule2());
            yield return SpeakAndWait("Great job on Module 1! Click Next or press N whenever you're ready to head over to Module 2.", "mod1_next_prompt.wav");
            if (session != tourSessionId) yield break;

            yield return WaitForNextButton(session);
        }

        private IEnumerator RunModule2Sequence(uint session)
        {
            // ==========================================
            // MODULE 2: SUMMARY OF 5G USE CASES (WALL BANNER 2)
            // ==========================================
            currentStep = ModuleStep.Module2_UseCases_Overview;
            OnStepChanged?.Invoke(currentStep);

            // Pop Banner 2 forward and glide camera into front alignment
            UseCasesBannerDisplay.EnsureInstance()?.SetPopUpState(true);
            UseCasesBannerDisplay.Instance?.SetHighlightSection("all");
            UseCasesBannerDisplay.Instance?.HideTargetElement();

            // Camera: (2.10, 1.95, 3.10) facing +X
            GlideCameraTo(new Vector3(2.10f, 1.95f, 3.10f), Quaternion.Euler(0f, 90f, 0f), 1.2f);
            ShowNextButton("NEXT: MODULE 3 (SPECTROMETER) >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule3());

            yield return new WaitForSeconds(0.6f);
            if (session != tourSessionId) yield break;

            yield return SpeakAndWait("Awesome job on Module 1! Now let's slide right over to Module 2 to explore our 5G Use Cases banner. You see, 5G isn't just about faster downloads on our smartphones; the telecom world actually split all human and device communication into three distinct superpowers: eMBB, mMTC, and uRLLC. I'll highlight each tier with our laser frame as we go, so you can follow right along with me. Let's start at the top!", "usecases_line_0.wav");
            if (session != tourSessionId) yield break;

            // Step A: eMBB
            currentStep = ModuleStep.Module2_UseCases_eMBB;
            OnStepChanged?.Invoke(currentStep);
            UseCasesBannerDisplay.Instance?.SetHighlightSection("embb");
            UseCasesBannerDisplay.Instance?.SetTargetElement("embb_vr");
            yield return SpeakAndWait("Up first in electric cyan is eMBB, or Enhanced Mobile Broadband. Think of this as the ultimate superhighway for raw speed and bandwidth. We're talking up to 20 Gigabits per second! Notice the apps highlighted here: it powers lag-free 8K streaming, immersive virtual and augmented reality worlds, and fiber-speed fixed wireless for our homes—even if you're cruising on a high-speed train at 500 kilometers an hour.", "usecases_line_1.wav");
            if (session != tourSessionId) yield break;

            // Step B: mMTC
            currentStep = ModuleStep.Module2_UseCases_mMTC;
            OnStepChanged?.Invoke(currentStep);
            UseCasesBannerDisplay.Instance?.SetHighlightSection("mmtc");
            UseCasesBannerDisplay.Instance?.SetTargetElement("mmtc_smartcities");
            yield return SpeakAndWait("Next, look at the middle green tier: mMTC, which stands for Massive Machine-Type Communication. If eMBB is a superhighway for fast cars, mMTC is a giant stadium parking lot. It isn't about peak speed; it's engineered to connect up to one million smart devices in a single square kilometer! Imagine city water meters, smart agriculture sensors, and wearable fitness trackers that can quietly run on a single tiny coin battery for more than ten years.", "usecases_line_2.wav");
            if (session != tourSessionId) yield break;

            // Step C: uRLLC
            currentStep = ModuleStep.Module2_UseCases_uRLLC;
            OnStepChanged?.Invoke(currentStep);
            UseCasesBannerDisplay.Instance?.SetHighlightSection("urllc");
            UseCasesBannerDisplay.Instance?.SetTargetElement("urllc_surgery");
            yield return SpeakAndWait("And down at the bottom in coral red is 5G's most critical superpower: uRLLC, or Ultra-Reliable Low Latency Communication. This is the life-saver. It slashes network delay to under one millisecond with ninety-nine point nine-nine-nine percent reliability! Look at the robotic surgery and autonomous vehicle icons highlighted here: when a surgeon operates on a patient from across the world, or self-driving cars coordinate split-second emergency braking, every microsecond counts.", "usecases_line_3.wav");
            if (session != tourSessionId) yield break;

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
            if (session != tourSessionId) yield break;

            yield return WaitForAnswerOrTimeout(8f, session);
            if (session != tourSessionId) yield break;

            UseCasesBannerDisplay.Instance?.SetPopUpState(false);
            ShowNextButton("NEXT: MODULE 3 (SPECTROMETER) >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule3());
            yield return SpeakAndWait("Outstanding job on Use Cases! Whenever you're ready, click Next or press N to head to Module 3 and check out our lab Spectrometer.");
            if (session != tourSessionId) yield break;

            yield return WaitForNextButton(session);
        }

        private IEnumerator RunModule3Sequence(uint session)
        {
            // ==========================================
            // MODULE 3: SPECTROMETER (RF SPECTRUM ANALYZER ON WORKBENCH)
            // ==========================================
            currentStep = ModuleStep.Module3_Spectrum_Intro;
            OnStepChanged?.Invoke(currentStep);

            // Banners stay on walls, glide camera directly to the Spectrum Analyzer on the workbench
            Vector3 camPos = new Vector3(1.473f, 1.25f, -0.65f);
            Vector3 targetLook = new Vector3(1.473f, 0.92f, 0.127f);
            Quaternion camRot = Quaternion.LookRotation(targetLook - camPos);

            GlideCameraTo(camPos, camRot, 1.4f);
            ShowNextButton("NEXT: MODULE 4 (ANTENNA) >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule4());

            yield return new WaitForSeconds(0.8f);
            if (session != tourSessionId) yield break;

            yield return SpeakAndWait("Awesome job on the use cases! Now, let's step over to Module 3 to check out our lab's Spectrometer—specifically, this RF Spectrum Analyzer right here on the workbench. In wireless communication, radio waves are completely invisible, but this instrument is basically our superpower: it captures raw 5G airwaves and translates them into live visual graphs on screen so we can see every single signal.", "mod3_spectrum_0.wav");
            if (session != tourSessionId) yield break;

            currentStep = ModuleStep.Module3_Spectrum_Diagnostics;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Take a look at the display grid. The horizontal axis plots frequency, spanning from low-band Sub-6 gigahertz all the way up to high-band 28 gigahertz millimeter wave. The vertical axis shows signal amplitude, or power. Whenever a 5G transmitter fires, you'll see a clean, sharp peak rise right above the background noise floor. That lets our engineers instantly spot signal distortion, verify transmission bandwidth, and hunt down any rogue interference.", "mod3_spectrum_1.wav");
            if (session != tourSessionId) yield break;

            // Module 3 Quiz
            currentStep = ModuleStep.Module3_Spectrum_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "On this spectrum analyzer, what two key variables are plotted on the screen to show how our 5G signal is performing?",
                acceptedKeywords = new[] { "frequency", "amplitude", "power", "freq", "strength" },
                explanation = "The spectrum analyzer plots frequency along the horizontal axis and signal amplitude or power on the vertical axis."
            };
            yield return SpeakAndWait("Let's do a quick knowledge check for Module 3! On this spectrum analyzer, what two key variables are plotted on the screen to show how our 5G signal is performing? Hold down the V key and let me know!", "mod3_spectrum_2.wav");
            if (session != tourSessionId) yield break;

            yield return WaitForAnswerOrTimeout(8f, session);
            if (session != tourSessionId) yield break;

            yield return SpeakAndWait("Spot on! The spectrum analyzer plots frequency on the horizontal axis and signal amplitude or power on the vertical axis. Now that we know how to measure RF signals, let's step over to Module 4 to inspect the hardware actually transmitting those waves: our 5G Antenna!", "mod3_spectrum_3.wav");
            if (session != tourSessionId) yield break;

            ShowNextButton("NEXT: MODULE 4 (ANTENNA) >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule4());
            yield return WaitForNextButton(session);
        }

        private IEnumerator RunModule4Sequence(uint session)
        {
            // ==========================================
            // MODULE 4: 5G ANTENNA & MASSIVE MIMO (TRIPOD ARRAY)
            // ==========================================
            currentStep = ModuleStep.Module4_Antenna_Intro;
            OnStepChanged?.Invoke(currentStep);

            // Camera frames the massive MIMO antenna on tripod at eye level
            Vector3 camPos = new Vector3(4.60f, 1.68f, -1.82f);
            Vector3 targetLook = new Vector3(6.22f, 1.62f, -1.82f);
            Quaternion camRot = Quaternion.LookRotation(targetLook - camPos);

            GlideCameraTo(camPos, camRot, 1.4f);
            ShowNextButton("NEXT: MODULE 5 (GNODEB) >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule5());

            yield return new WaitForSeconds(0.8f);
            if (session != tourSessionId) yield break;

            yield return SpeakAndWait("Welcome to Module 4! Take a look at this tripod setup right in front of us. This is our 5G Antenna unit. In older 4G cell towers, antennas were passive and blasted radio waves in every direction like an unfocused lightbulb, which wasted huge amounts of energy. 5G completely revolutionized this with an active phased array technology called Massive MIMO.", "mod4_antenna_0.wav");
            if (session != tourSessionId) yield break;

            currentStep = ModuleStep.Module4_Antenna_Beamforming;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Notice the antenna array mounted up top. Packed behind that casing are 64 transmit and 64 receive elements. Instead of broadcasting broad signals, the antenna dynamically shifts the phase of each element to synthesize tight, steerable radio beams. It literally tracks active users—beaming high-speed gigabit data straight to your phone while you're walking, and creating zero interference for everyone else!", "mod4_antenna_1.wav");
            if (session != tourSessionId) yield break;

            // Module 4 Quiz
            currentStep = ModuleStep.Module4_Antenna_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "What advanced technique does this 5G antenna use to steer focused, high-power radio beams directly at active users?",
                acceptedKeywords = new[] { "beamforming", "beam forming", "mimo", "massive mimo", "beam", "focus" },
                explanation = "Beamforming focuses and steers radio wave energy directly to active users, multiplying signal strength and killing interference."
            };
            yield return SpeakAndWait("Quick check for Module 4: What advanced technique does this 5G antenna use to steer focused, high-power radio beams directly at active users? Hold down the V key and tell me!", "mod4_antenna_2.wav");
            if (session != tourSessionId) yield break;

            yield return WaitForAnswerOrTimeout(8f, session);
            if (session != tourSessionId) yield break;

            yield return SpeakAndWait("Exactly right! Active beamforming focuses radio energy directly at target devices. Now that we've seen how the antenna transmits over the air, let's head over to Module 5 to inspect the massive computing brain running the whole show: the gNodeB base station!", "mod4_antenna_3.wav");
            if (session != tourSessionId) yield break;

            ShowNextButton("NEXT: MODULE 5 (GNODEB) >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToModule5());
            yield return WaitForNextButton(session);
        }

        private IEnumerator RunModule5Sequence(uint session)
        {
            // ==========================================
            // MODULE 5: GNODEB BASE STATION & 5G CORE RACK
            // ==========================================
            currentStep = ModuleStep.Module5_gNodeB_Intro;
            OnStepChanged?.Invoke(currentStep);

            // Camera frames the gNodeB base station & core processing server rack
            Vector3 camPos = new Vector3(2.81f, 1.45f, -4.80f);
            Vector3 targetLook = new Vector3(2.81f, 1.25f, -6.66f);
            Quaternion camRot = Quaternion.LookRotation(targetLook - camPos);

            GlideCameraTo(camPos, camRot, 1.4f);
            ShowNextButton("FINISH LAB TOUR >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToCompletion());

            yield return new WaitForSeconds(0.8f);
            if (session != tourSessionId) yield break;

            yield return SpeakAndWait("And here we are at Module 5! Take a look at this large illuminated server cabinet. This is the heart of the gNodeB and our 5G processing system. gNodeB stands for next-generation Node B, and it's the official 3GPP standard name for modern 5G base stations. While the antenna handles the wireless radio waves, this rack houses the massive digital processing units that control the entire network.", "mod5_gnodeb_0.wav");
            if (session != tourSessionId) yield break;

            currentStep = ModuleStep.Module5_gNodeB_ComputeAndEdge;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Inside this rack, the gNodeB is split into the Distributed Unit and Centralized Unit. It handles microsecond-level packet scheduling, quality of service, security encryption, and manages seamless handovers between cell towers. Mounted right alongside it is our Mobile Edge Computing server, hosting the UPF so data breaks out locally without long trips to distant clouds. Together, they deliver the ultra-reliable, high-throughput promise of 5G!", "mod5_gnodeb_1.wav");
            if (session != tourSessionId) yield break;

            // Module 5 Quiz
            currentStep = ModuleStep.Module5_gNodeB_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = new QuizQuestion
            {
                question = "What is the official 3GPP name for the next-generation 5G base station that manages radio resources and connects users to the core network?",
                acceptedKeywords = new[] { "gnodeb", "g-node-b", "gnb", "next generation node b", "next-generation node b", "next gen node b" },
                explanation = "gNodeB, or Next-Generation Node B, is the official 3GPP standard name for the 5G base station."
            };
            yield return SpeakAndWait("Final knowledge check for Module 5! What is the official 3GPP name for the next-generation 5G base station that manages radio resources and connects users to the core network? Hold down the V key and give it a shot!", "mod5_gnodeb_2.wav");
            if (session != tourSessionId) yield break;

            yield return WaitForAnswerOrTimeout(8f, session);
            if (session != tourSessionId) yield break;

            yield return SpeakAndWait("Bingo! gNodeB, or next-generation Node B, is the true engine of 5G connectivity! Incredible job: you have now successfully completed all five modules in our virtual 5G lab, from the CUPS architecture and Use Cases banners to the Spectrometer, the Massive MIMO Antenna, and the gNodeB base station. Feel free to explore the lab freely, click any piece of equipment, or hold V to ask me anything anytime!", "mod5_gnodeb_3.wav");
            if (session != tourSessionId) yield break;

            ShowNextButton("FINISH LAB TOUR >  <size=75%><color=#80D8FF>[N / Click]</color></size>", () => AdvanceToCompletion());
            yield return WaitForNextButton(session);
        }

        private IEnumerator RunCourseCompletion(uint session)
        {
            // ==========================================
            // RECAP & GRADUATION
            // ==========================================
            currentStep = ModuleStep.RecapAndWrapUp;
            OnStepChanged?.Invoke(currentStep);
            HideNextButton();

            // Return camera smoothly to default open roaming perspective
            GlideCameraTo(new Vector3(3.5f, 1.70f, 0.0f), Quaternion.Euler(0f, 0f, 0f), 1.5f);
            yield return new WaitForSeconds(1.5f);
            if (session != tourSessionId) yield break;

            currentStep = ModuleStep.Completed;
            OnStepChanged?.Invoke(currentStep);

            if (VoiceAgentManager.Instance != null)
            {
                VoiceAgentManager.Instance.HUD?.SetCinemaSubtitleMode(false);
                VoiceAgentManager.Instance.SetState(AgentState.Idle);
            }
        }

        // =========================================================================
        // CAMERA GLIDE & SCENE HELPERS
        // =========================================================================

        public void GlideCameraTo(Vector3 desiredCamPos, Quaternion desiredCamRot, float duration = 1.2f)
        {
            if (cameraGlideRoutine != null) StopCoroutine(cameraGlideRoutine);
            cameraGlideRoutine = StartCoroutine(GlideCameraRoutine(desiredCamPos, desiredCamRot, duration));
        }

        private IEnumerator GlideCameraRoutine(Vector3 desiredCamPos, Quaternion desiredCamRot, float duration)
        {
            if (xrOrigin == null)
            {
                GameObject originGO = GameObject.Find("XR Origin (VR)");
                if (originGO != null) xrOrigin = originGO.transform;
            }

            if (xrOrigin == null || Camera.main == null) yield break;

            Transform cam = Camera.main.transform;
            Vector3 cameraLocalOffset = cam.position - xrOrigin.position;
            Vector3 targetOriginPos = desiredCamPos - cameraLocalOffset;
            Quaternion targetOriginRot = desiredCamRot * Quaternion.Inverse(cam.localRotation);

            Vector3 startPos = xrOrigin.position;
            Quaternion startRot = xrOrigin.rotation;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.Clamp01(elapsed / duration);
                float smoothT = t * t * (3f - 2f * t);

                xrOrigin.position = Vector3.Lerp(startPos, targetOriginPos, smoothT);
                xrOrigin.rotation = Quaternion.Slerp(startRot, targetOriginRot, smoothT);
                yield return null;
            }

            xrOrigin.position = targetOriginPos;
            xrOrigin.rotation = targetOriginRot;
            cameraGlideRoutine = null;
        }

        private void ResetAllBannersAndEquipment()
        {
            EnsureAllEquipmentActive();

            CupsBannerDisplay.Instance?.SetHighlightSection("none");
            CupsBannerDisplay.Instance?.HideTargetElement();
            CupsBannerDisplay.Instance?.SetPopUpState(false);

            UseCasesBannerDisplay.Instance?.SetHighlightSection("none");
            UseCasesBannerDisplay.Instance?.HideTargetElement();
            UseCasesBannerDisplay.Instance?.SetPopUpState(false);
        }

        private GameObject cachedGNodeB;
        private GameObject cachedPC;
        private GameObject cachedMEC;

        private void CacheSceneReferences()
        {
            if (cachedGNodeB == null) cachedGNodeB = GameObject.Find("gNodeB (Base Station)");
            if (cachedPC == null) cachedPC = GameObject.Find("PC");
            if (cachedMEC == null) cachedMEC = GameObject.Find("Mobile Edge Computing (MEC-Core)");
            if (xrOrigin == null)
            {
                GameObject originGO = GameObject.Find("XR Origin (VR)");
                if (originGO != null) xrOrigin = originGO.transform;
            }
        }

        private void EnsureAllEquipmentActive()
        {
            CacheSceneReferences();
            if (cachedGNodeB != null && !cachedGNodeB.activeSelf) cachedGNodeB.SetActive(true);
            if (cachedPC != null && !cachedPC.activeSelf) cachedPC.SetActive(true);
            if (cachedMEC != null && !cachedMEC.activeSelf) cachedMEC.SetActive(true);
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

        public void EndTour()
        {
            tourSessionId++;
            StopAllCoroutines();
            HideNextButton();
            ResetAllBannersAndEquipment();
            currentStep = ModuleStep.Completed;
            currentModuleIndex = 6;
            VoiceAgentManager.Instance?.HUD?.SetCinemaSubtitleMode(false);
            if (VoiceAgentManager.Instance != null)
            {
                VoiceAgentManager.Instance.SetState(AgentState.Idle);
            }
            OnStepChanged?.Invoke(currentStep);
        }

        public bool SubmitSpokenAnswer(string transcript, out string feedback)
        {
            feedback = "";
            string clean = transcript.ToLowerInvariant();

            if (clean.Contains("next") || clean.Contains("continue") || clean.Contains("proceed"))
            {
                AdvanceToNextModule();
                feedback = $"Advancing to next module!";
                return true;
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

        private IEnumerator WaitForAnswerOrTimeout(float timeoutSeconds, uint session)
        {
            isWaitingForQuizAnswer = true;
            float elapsed = 0;
            while (isWaitingForQuizAnswer && elapsed < timeoutSeconds)
            {
                if (session != tourSessionId) yield break;

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

        private IEnumerator WaitForNextButton(uint session)
        {
            isWaitingForNextButton = true;
            while (isWaitingForNextButton)
            {
                if (session != tourSessionId) yield break;
                yield return null;
            }
        }

        private void OnDestroy()
        {
            tourSessionId++;
            StopAllCoroutines();
            HideNextButton();
            ResetAllBannersAndEquipment();
        }
    }
}
