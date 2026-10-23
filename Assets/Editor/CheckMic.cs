using UnityEngine;
using UnityEditor;

public class CheckMic
{
    [MenuItem("5G Lab/Check Microphones")]
    public static void Run()
    {
        Debug.Log("Microphones count: " + Microphone.devices.Length);
        foreach (var m in Microphone.devices)
        {
            int min, max;
            Microphone.GetDeviceCaps(m, out min, out max);
            Debug.Log($"Device: '{m}', MinFreq: {min}, MaxFreq: {max}");
        }
    }
}
