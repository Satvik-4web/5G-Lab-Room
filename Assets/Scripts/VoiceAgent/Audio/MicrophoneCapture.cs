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
        private int captureSampleRate = 44100;
        private bool isRecording = false;

        private float currentRms = 0f;

        public bool IsRecording => isRecording;
        public float CurrentRms => currentRms;
        public string ActiveDeviceName => micDevice;

        public event Action<bool> OnPermissionResult;

        private void Awake()
        {
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

        private string FindBestMicrophoneDevice()
        {
            if (Microphone.devices == null || Microphone.devices.Length == 0) return null;

            // Prioritize physical microphones (Realtek, Array, etc.) over virtual disconnected devices (Oculus)
            foreach (var dev in Microphone.devices)
            {
                if (dev.IndexOf("Oculus", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dev.IndexOf("Virtual", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    continue;
                }
                if (dev.IndexOf("Realtek", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dev.IndexOf("Array", StringComparison.OrdinalIgnoreCase) >= 0 ||
                    dev.IndexOf("Microphone", StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    return dev;
                }
            }

            // Fallback to first available device
            return Microphone.devices[0];
        }

        public bool StartRecording()
        {
            if (isRecording) return true;

            if (!HasMicrophoneDevice())
            {
                Debug.LogWarning("[MicrophoneCapture] No microphone device detected on this system.");
                return false;
            }

            micDevice = FindBestMicrophoneDevice();
            Debug.Log($"[MicrophoneCapture] Recording from microphone: '{micDevice}'");

            // Query supported frequency range
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

            if (micPosition <= 128)
            {
                Debug.LogWarning("[MicrophoneCapture] Recording duration too short (<128 samples).");
                if (recordingClip != null) { Destroy(recordingClip); recordingClip = null; }
                return null;
            }

            float[] rawSamples = new float[micPosition];
            recordingClip.GetData(rawSamples, 0);

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
