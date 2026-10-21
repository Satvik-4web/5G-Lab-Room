using System.Threading;
using System.Threading.Tasks;

namespace FiveGLab.VoiceAgent
{
    /// <summary>
    /// Contract for converting recorded audio WAV bytes to transcribed text.
    /// </summary>
    public interface ISpeechToText
    {
        Task<string> TranscribeAsync(byte[] wavAudio, CancellationToken ct);
    }
}
