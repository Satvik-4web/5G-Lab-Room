using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    public class GuidedTourController : MonoBehaviour
    {
        public static GuidedTourController Instance { get; private set; }

        public enum TourStep
        {
            NotStarted,
            Intro,
            gNodeB_Explaining,
            gNodeB_Quiz,
            MEC_Explaining,
            MEC_Quiz,
            Spectrum_Explaining,
            Spectrum_Quiz,
            RecapAndWrapUp,
            Completed
        }

        [SerializeField] private LabKnowledgeBase knowledgeBase;
        private TourStep currentStep = TourStep.NotStarted;
        private Coroutine tourRoutine;
        private bool isWaitingForQuizAnswer = false;
        private QuizQuestion currentQuizQuestion = null;

        public bool IsTourActive => currentStep != TourStep.NotStarted && currentStep != TourStep.Completed;
        public TourStep CurrentStep => currentStep;

        public event Action<TourStep> OnStepChanged;

        private void Awake()
        {
            if (Instance == null) Instance = this;
            else Destroy(gameObject);
        }

        public void StartTour(string mode = "full", string specificEquipment = null)
        {
            if (tourRoutine != null) StopCoroutine(tourRoutine);
            tourRoutine = StartCoroutine(RunTourSequence(mode, specificEquipment));
        }

        public void NextStop()
        {
            // Advance step if waiting
            isWaitingForQuizAnswer = false;
        }

        public void EndTour()
        {
            if (tourRoutine != null)
            {
                StopCoroutine(tourRoutine);
                tourRoutine = null;
            }
            currentStep = TourStep.Completed;
            PCInteractionTester.ExitCurrentInspection();
            OnStepChanged?.Invoke(currentStep);
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
                feedback = $"Spot on! {currentQuizQuestion.explanation}";
            }
            else
            {
                if (VoiceAgentManager.Instance != null && VoiceAgentManager.Instance.Memory != null)
                {
                    VoiceAgentManager.Instance.Memory.TotalQuizQuestions++;
                }
                feedback = $"Good try! The key takeaway is: {currentQuizQuestion.explanation}";
            }

            return true;
        }

        private IEnumerator RunTourSequence(string mode, string specificEquipment)
        {
            if (VoiceAgentManager.Instance != null)
            {
                VoiceAgentManager.Instance.SetState(AgentState.Touring);
            }

            // 1. Intro Step
            currentStep = TourStep.Intro;
            OnStepChanged?.Invoke(currentStep);
            yield return SpeakAndWait("Welcome to the interactive 5G Lab Tour! Today, we will explore how high-speed radio waves, edge computing, and precision diagnostics connect to make 5G possible. Let's begin at the antenna!");

            // 2. gNodeB Base Station
            currentStep = TourStep.gNodeB_Explaining;
            OnStepChanged?.Invoke(currentStep);
            PCInteractionTester.InspectEquipment(EquipmentId.gnodeb);
            yield return new WaitForSeconds(1.2f);

            var gnodebData = knowledgeBase?.GetEquipment(EquipmentId.gnodeb);
            string gnodebExplain = gnodebData != null 
                ? "This is the gNodeB, our 5G base station. It connects mobile devices to the core network using Massive MIMO and Active Beamforming. Instead of radiating in all directions like a lightbulb, it focuses energy into narrow beams aimed directly at users."
                : "You are looking at the gNodeB base station, which beams 5G signals directly to mobile devices.";
            yield return SpeakAndWait(gnodebExplain);

            // gNodeB Quiz
            currentStep = TourStep.gNodeB_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = (gnodebData?.quizQuestions != null && gnodebData.quizQuestions.Length > 0)
                ? gnodebData.quizQuestions[0]
                : new QuizQuestion { question = "What does active beamforming do to radio signals?", acceptedKeywords = new[] { "focus", "direct", "narrow" }, explanation = "Beamforming focuses radio energy into narrow beams toward user devices." };

            yield return SpeakAndWait($"Quick check: {currentQuizQuestion.question} Press V and tell me!");
            yield return WaitForAnswerOrTimeout(8f);

            // 3. MEC-Core Server Rack
            currentStep = TourStep.MEC_Explaining;
            OnStepChanged?.Invoke(currentStep);
            PCInteractionTester.InspectEquipment(EquipmentId.mec);
            yield return new WaitForSeconds(1.2f);

            var mecData = knowledgeBase?.GetEquipment(EquipmentId.mec);
            string mecExplain = mecData != null
                ? "Next, we have the Mobile Edge Computing core, or MEC. By placing GPU and Baseband compute servers right next to the cell tower, we achieve round-trip latencies under one millisecond for robotics and autonomous driving."
                : "This server rack is the MEC Core, processing edge compute in under one millisecond.";
            yield return SpeakAndWait(mecExplain);

            // MEC Quiz
            currentStep = TourStep.MEC_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = (mecData?.quizQuestions != null && mecData.quizQuestions.Length > 0)
                ? mecData.quizQuestions[0]
                : new QuizQuestion { question = "Why do we place MEC servers at the edge of the network?", acceptedKeywords = new[] { "latency", "speed", "closer" }, explanation = "Placing servers at the edge drastically cuts latency." };

            yield return SpeakAndWait($"Quick check: {currentQuizQuestion.question} Hold V to answer!");
            yield return WaitForAnswerOrTimeout(8f);

            // 4. Spectrum Analyzer
            currentStep = TourStep.Spectrum_Explaining;
            OnStepChanged?.Invoke(currentStep);
            PCInteractionTester.InspectEquipment(EquipmentId.spectrum);
            yield return new WaitForSeconds(1.2f);

            var spectrumData = knowledgeBase?.GetEquipment(EquipmentId.spectrum);
            string spectrumExplain = spectrumData != null
                ? "Finally, this is the RF Spectrum Analyzer. It measures signal amplitude across frequencies from two hertz up to eighty-five gigahertz, helping engineers verify transmissions and eliminate interference."
                : "This is the RF Spectrum Analyzer, our precision frequency diagnostic tool.";
            yield return SpeakAndWait(spectrumExplain);

            // Spectrum Quiz
            currentStep = TourStep.Spectrum_Quiz;
            OnStepChanged?.Invoke(currentStep);
            currentQuizQuestion = (spectrumData?.quizQuestions != null && spectrumData.quizQuestions.Length > 0)
                ? spectrumData.quizQuestions[0]
                : new QuizQuestion { question = "What two variables does a spectrum analyzer plot on its screen?", acceptedKeywords = new[] { "amplitude", "power", "frequency" }, explanation = "It plots signal power or amplitude against frequency." };

            yield return SpeakAndWait($"Last question: {currentQuizQuestion.question} Press V to tell me!");
            yield return WaitForAnswerOrTimeout(8f);

            // 5. Recap and Wrap-Up
            currentStep = TourStep.RecapAndWrapUp;
            OnStepChanged?.Invoke(currentStep);
            PCInteractionTester.ExitCurrentInspection();
            yield return new WaitForSeconds(1.0f);

            int score = VoiceAgentManager.Instance?.Memory?.QuizScore ?? 0;
            int total = VoiceAgentManager.Instance?.Memory?.TotalQuizQuestions ?? 3;
            yield return SpeakAndWait($"That concludes our 5G lab tour! Together, the gNodeB radiates targeted beams, the MEC Core powers instant responses under one millisecond, and the Spectrum Analyzer ensures clean transmission. You scored {score} out of {total} on the quiz! Feel free to ask me any more questions anytime.");

            currentStep = TourStep.Completed;
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
                elapsed += Time.deltaTime;
                yield return null;
            }

            if (isWaitingForQuizAnswer)
            {
                // Timed out with no answer
                isWaitingForQuizAnswer = false;
                yield return SpeakAndWait($"Let's keep going! {currentQuizQuestion?.explanation}");
            }
        }
    }
}
