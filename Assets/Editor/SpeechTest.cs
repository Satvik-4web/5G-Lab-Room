using UnityEngine;
using UnityEditor;
using UnityEngine.Windows.Speech;

public class SpeechTest
{
    [MenuItem("5G Lab/Test Windows Dictation")]
    public static void Test()
    {
        Debug.Log("DictationRecognizer supported: " + PhraseRecognitionSystem.isSupported);
    }
}
