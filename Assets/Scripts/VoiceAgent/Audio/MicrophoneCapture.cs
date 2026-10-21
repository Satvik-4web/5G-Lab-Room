using System;
using System.Collections;
using UnityEngine;
#if UNITY_ANDROID && !UNITY_EDITOR
using UnityEngine.Android;
#endif

namespace FiveGLab.VoiceAgent
{
    public class MicrophoneCapture : MonoBehaviour
    {
        [Header("Settings")]
        [SerializeField] private int targetSampleRate = 16000;
        [SerializeField] private int maxRecordingSeconds = 30;

        private AudioClip recordingClip;
        private string micDevice = null;
        private bool isRecording = false;
        private float recordingStartTime = 0f;

        // Preallocated buffer for RMS computation and sample retrieval
        private float[] preallocatedBuffer;
        private float currentRms = 0f;

        public bool IsRecording => isRecording;
        public float CurrentRms => currentRms;

        public event Action<bool> OnPermissionResult;

        private void Awake()
        {
            preallocatedBuffer = new float[targetSampleRate * 2]; // 2-second reusable chunk
            RequestMicrophonePermission();
        }

        public void RequestMicrophonePermission()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            if (!Permission.HasUserAuthorizedPermission(Permission.Microphone))
            {
                Permission.RequestUserPermission(Permission.Microphone);
            }
            OnPermissionResult?.Invoke(Permission.HasUserAuthorizedPermission(Permission.Microphone));
#else
            bool hasMic = Microphone.devices != null && Microphone.devices.Length > 0;
            OnPermissionResult?.Invoke(hasMic);
#endif
        }

        public bool HasMicrophoneDevice()
        {
            return Microphone.devices != null && Microphone.devices.Length > 0;
        }

        public bool StartRecording()
        {
            if (isRecording) return true;

            if (!HasMicrophoneDevice())
            {
                Debug.LogWarning("[MicrophoneCapture] No microphone device detected on this system.");
                return false;
            }

            micDevice = Microphone.devices[0];
            recordingClip = Microphone.Start(micDevice, false, maxRecordingSeconds, targetSampleRate);
            if (recordingClip == null)
            {
                Debug.LogError("[MicrophoneCapture] Failed to start microphone recording.");
                return false;
            }

            isRecording = true;
            recordingStartTime = Time.time;
            return true;
        }

        public byte[] StopRecordingAndGetWav()
        {
            if (!isRecording) return null;

            int micPosition = Microphone.GetPosition(micDevice);
            Microphone.End(micDevice);
            isRecording = false;
            currentRms = 0f;

            if (micPosition <= 0)
            {
                Debug.LogWarning("[MicrophoneCapture] Empty recording (0 samples).");
                return null;
            }

            // Extract valid recorded samples
            float[] recordedSamples = new float[micPosition];
            recordingClip.GetData(recordedSamples, 0);

            // Destroy temporary recording clip to save memory
            Destroy(recordingClip);
            recordingClip = null;

            return WavEncoder.EncodeToWav(recordedSamples, targetSampleRate);
        }

        public void CancelRecording()
        {
            if (isRecording)
            {
                Microphone.End(micDevice);
                isRecording = false;
                currentRms = 0f;
                if (recordingClip != null)
                {
                    Destroy(recordingClip);
                    recordingClip = null;
                }
            }
        }

        private void Update()
        {
            if (!isRecording || recordingClip == null)
            {
                currentRms = 0f;
                return;
            }

            int micPos = Microphone.GetPosition(micDevice);
            int window = 256;
            if (micPos > window)
            {
                float[] temp = new float[window];
                recordingClip.GetData(temp, micPos - window);
                float sum = 0f;
                for (int i = 0; i < window; i++)
                {
                    sum += temp[i] * temp[i];
                }
                currentRms = Mathf.Sqrt(sum / window);
            }
        }
    }
}
