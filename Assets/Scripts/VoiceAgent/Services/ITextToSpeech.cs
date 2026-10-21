using System.Threading;
using System.Threading.Tasks;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Contract for synthesizing spoken text into a playable Unity AudioClip.
    /// </summary>
    public interface ITextToSpeech
    {
        Task<AudioClip> SynthesizeAsync(string text, string voice, CancellationToken ct);
    }
}
