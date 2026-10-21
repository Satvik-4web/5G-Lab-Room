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
        [SerializeField] private int maxRecordingSeconds = 20;

        private AudioClip recordingClip;
        private string micDevice = null; // null uses Windows/Android default recording device
        private int captureSampleRate = 44100;
        private bool isRecording = false;

        // Preallocated buffer for RMS computation
        private float[] preallocatedBuffer;
        private float currentRms = 0f;

        public bool IsRecording => isRecording;
        public float CurrentRms => currentRms;

        public event Action<bool> OnPermissionResult;

        private void Awake()
        {
            preallocatedBuffer = new float[1024];
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
                Debug.LogWarning("[MicrophoneCapture] No active microphone detected on this system.");
                return false;
            }

            // Always use null to select system default microphone
            micDevice = null;

            // Determine optimal supported hardware sample rate
            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(micDevice, out minFreq, out maxFreq);

            if (minFreq == 0 && maxFreq == 0)
            {
                captureSampleRate = 44100;
            }
            else if (maxFreq > 0 && maxFreq < 44100)
            {
                captureSampleRate = maxFreq;
            }
            else
            {
                captureSampleRate = 44100;
            }

            recordingClip = Microphone.Start(micDevice, false, maxRecordingSeconds, captureSampleRate);
            if (recordingClip == null)
            {
                Debug.LogError("[MicrophoneCapture] Microphone.Start failed to create audio clip.");
                return false;
            }

            isRecording = true;
            currentRms = 0.05f; // Initial visual pulse
            return true;
        }

        public byte[] StopRecordingAndGetWav()
        {
            if (!isRecording || recordingClip == null) return null;

            int micPosition = Microphone.GetPosition(micDevice);
            Microphone.End(micDevice);
            isRecording = false;
            currentRms = 0f;

            if (micPosition <= 0)
            {
                // If position hasn't updated yet (very short tap), take whatever samples are in clip
                micPosition = Mathf.Min(recordingClip.samples, captureSampleRate * 2);
            }

            if (micPosition <= 128)
            {
                Debug.LogWarning("[MicrophoneCapture] Recording too short (<128 samples).");
                if (recordingClip != null) { Destroy(recordingClip); recordingClip = null; }
                return null;
            }

            float[] rawSamples = new float[micPosition];
            recordingClip.GetData(rawSamples, 0);

            // Destroy clip to prevent memory leaks
            Destroy(recordingClip);
            recordingClip = null;

            // Resample down to standard 16kHz mono for speech recognition
            float[] resampled16k = ResampleTo16k(rawSamples, captureSampleRate);

            return WavEncoder.EncodeToWav(resampled16k, 16000);
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

        private float[] ResampleTo16k(float[] input, int sourceRate)
        {
            if (sourceRate == 16000 || input.Length == 0) return input;

            int targetLength = Mathf.CeilToInt((float)input.Length * 16000f / sourceRate);
            float[] output = new float[targetLength];

            float step = (float)(input.Length - 1) / (targetLength - 1);
            for (int i = 0; i < targetLength; i++)
            {
                float srcIdx = i * step;
                int idx0 = Mathf.FloorToInt(srcIdx);
                int idx1 = Mathf.Min(idx0 + 1, input.Length - 1);
                float frac = srcIdx - idx0;
                output[i] = Mathf.Lerp(input[idx0], input[idx1], frac);
            }

            return output;
        }

        private void Update()
        {
            if (!isRecording || recordingClip == null) return;

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
