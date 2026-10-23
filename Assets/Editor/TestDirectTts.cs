using UnityEngine;
using UnityEditor;
using UnityEngine.Networking;
using System.Threading.Tasks;

public class TestDirectTts
{
    [MenuItem("5G Lab/Test Direct TTS")]
    public static async void RunTest()
    {
        string text = UnityWebRequest.EscapeURL("Hello, I am Aria, your 5G lab tutor.");
        string url = "https://translate.google.com/translate_tts?ie=UTF-8&client=tw-ob&tl=en&q=" + text;
        
        using var req = UnityWebRequestMultimedia.GetAudioClip(url, AudioType.MPEG);
        req.SetRequestHeader("User-Agent", "Mozilla/5.0");
        var op = req.SendWebRequest();
        while (!op.isDone) await Task.Yield();
        
        if (req.result == UnityWebRequest.Result.Success)
        {
            AudioClip clip = DownloadHandlerAudioClip.GetContent(req);
            Debug.Log($"SUCCESS! Downloaded direct clip: length={clip.length}s, samples={clip.samples}, channels={clip.channels}");
        }
        else
        {
            Debug.LogError("Failed: " + req.error);
        }
    }
}
