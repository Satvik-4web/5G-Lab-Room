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
        private string micDevice = null;
        private int captureSampleRate = 16000;
        private bool isRecording = false;
        private int currentDeviceIndex = 0;

        private float currentRms = 0f;

        public bool IsRecording => isRecording;
        public float CurrentRms => currentRms;
        public string ActiveDeviceName => micDevice;
        public int ActiveDeviceIndex => currentDeviceIndex;

        public event Action<bool> OnPermissionResult;
        public event Action<string> OnDeviceChanged;

        private void Awake()
        {
            RequestMicrophonePermission();
            InitializeDefaultDevice();
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

        private void InitializeDefaultDevice()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0) return;

            // Priority:
            // 1. Bluetooth headset (e.g. Master Buds) if connected
            // 2. Built-in Laptop Microphone Array (Realtek)
            // 3. Fallback to first non-virtual device
            int bestIdx = 0;
            for (int i = 0; i < Microphone.devices.Length; i++)
            {
                string dev = Microphone.devices[i];
                if (dev.IndexOf("Oculus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dev.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }

                if (dev.IndexOf("Buds", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dev.IndexOf("Headset", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dev.IndexOf("Bluetooth", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bestIdx = i;
                    break;
                }

                if (dev.IndexOf("Realtek", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dev.IndexOf("Array", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    bestIdx = i;
                }
            }

            currentDeviceIndex = bestIdx;
            micDevice = Microphone.devices[currentDeviceIndex];
        }

        public string GetFriendlyDeviceName()
        {
            if (string.IsNullOrEmpty(micDevice)) return "No Mic";
            if (micDevice.IndexOf("AirBass", StringComparison.OrdinalIgnoreCase) >= 0) return "AirBass Earbuds";
            if (micDevice.IndexOf("Master Buds", StringComparison.OrdinalIgnoreCase) >= 0) return "Master Buds";
            if (micDevice.IndexOf("Buds", StringComparison.OrdinalIgnoreCase) >= 0 || micDevice.IndexOf("Earbuds", StringComparison.OrdinalIgnoreCase) >= 0) return "Earbuds";
            if (micDevice.IndexOf("Realtek", StringComparison.OrdinalIgnoreCase) >= 0) return "Laptop Mic (Realtek)";
            if (micDevice.IndexOf("Array", StringComparison.OrdinalIgnoreCase) >= 0) return "Mic Array";
            if (micDevice.IndexOf("Headset", StringComparison.OrdinalIgnoreCase) >= 0) return "Headset Mic";
            if (micDevice.Length > 20) return micDevice.Substring(0, 20) + "...";
            return micDevice;
        }

        public void CycleToNextDevice()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0) return;

            if (isRecording)
            {
                CancelRecording();
            }

            currentDeviceIndex = (currentDeviceIndex + 1) % Microphone.devices.Length;
            // Skip Oculus/Virtual if others exist
            if (Microphone.devices.Length > 1 &&
                (Microphone.devices[currentDeviceIndex].IndexOf("Oculus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                 Microphone.devices[currentDeviceIndex].IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0))
            {
                currentDeviceIndex = (currentDeviceIndex + 1) % Microphone.devices.Length;
            }

            micDevice = Microphone.devices[currentDeviceIndex];
            Debug.Log($"[MicrophoneCapture] Active mic switched to [{currentDeviceIndex}]: '{micDevice}'");
            OnDeviceChanged?.Invoke(micDevice);
        }

        public bool StartRecording()
        {
            if (isRecording) return true;

            if (!HasMicrophoneDevice())
            {
                Debug.LogWarning("[MicrophoneCapture] No microphone device detected on this system.");
                return false;
            }

            if (string.IsNullOrEmpty(micDevice))
            {
                InitializeDefaultDevice();
            }

            // Query hardware supported frequency range
            int minFreq, maxFreq;
            Microphone.GetDeviceCaps(micDevice, out minFreq, out maxFreq);

            if (minFreq == 0 && maxFreq == 0)
            {
                // Unrestricted; 16000 is ideal standard for speech recognition
                captureSampleRate = 16000;
            }
            else if (16000 >= minFreq && 16000 <= maxFreq)
            {
                captureSampleRate = 16000;
            }
            else if (48000 >= minFreq && 48000 <= maxFreq)
            {
                captureSampleRate = 48000;
            }
            else
            {
                captureSampleRate = Mathf.Clamp(44100, minFreq, maxFreq);
            }

            Debug.Log($"[MicrophoneCapture] Starting recording on '{micDevice}' at {captureSampleRate} Hz (Caps: {minFreq}-{maxFreq})");

            recordingClip = Microphone.Start(micDevice, true, maxRecordingSeconds, captureSampleRate);
            if (recordingClip == null)
            {
                Debug.LogError($"[MicrophoneCapture] Microphone.Start failed for device '{micDevice}'.");
                return false;
            }

            isRecording = true;
            currentRms = 0.05f;
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
                micPosition = Mathf.Min(recordingClip.samples, captureSampleRate * 2);
            }

            if (micPosition < 512)
            {
                Debug.LogWarning($"[MicrophoneCapture] Recording duration too short ({micPosition} samples).");
                if (recordingClip != null) { Destroy(recordingClip); recordingClip = null; }
                return null;
            }

            float[] rawSamples = new float[micPosition];
            recordingClip.GetData(rawSamples, 0);

            Destroy(recordingClip);
            recordingClip = null;

            // Amplify / Normalize quiet speech signals (gain boost for laptop mic / Bluetooth buds)
            float maxPeak = 0f;
            for (int i = 0; i < rawSamples.Length; i++)
            {
                float abs = Mathf.Abs(rawSamples[i]);
                if (abs > maxPeak) maxPeak = abs;
            }

            Debug.Log($"[MicrophoneCapture] Captured {rawSamples.Length} samples from '{micDevice}'. Max peak: {maxPeak:F5}");

            if (maxPeak > 0.0002f && maxPeak < 0.70f)
            {
                float gain = Mathf.Min(0.85f / maxPeak, 60f);
                Debug.Log($"[MicrophoneCapture] Normalizing audio with gain boost: {gain:F1}x");
                for (int i = 0; i < rawSamples.Length; i++)
                {
                    rawSamples[i] = Mathf.Clamp(rawSamples[i] * gain, -1f, 1f);
                }
            }

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
