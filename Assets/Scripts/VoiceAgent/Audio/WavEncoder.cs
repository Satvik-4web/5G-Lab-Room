using System;
using System.IO;
using System.Text;
using UnityEngine;

namespace FiveGLab.VoiceAgent
{
    public static class WavEncoder
    {
        /// <summary>
        /// Encodes raw float PCM samples into standard 16-bit Mono 16kHz RIFF/WAV format bytes.
        /// </summary>
        public static byte[] EncodeToWav(float[] samples, int sampleRate = 16000)
        {
            if (samples == null || samples.Length == 0) return Array.Empty<byte>();

            using MemoryStream stream = new MemoryStream();
            using BinaryWriter writer = new BinaryWriter(stream);

            int channels = 1;
            int bitsPerSample = 16;
            int bytesPerSample = bitsPerSample / 8;
            int byteRate = sampleRate * channels * bytesPerSample;
            short blockAlign = (short)(channels * bytesPerSample);
            int subChunk2Size = samples.Length * bytesPerSample;
            int chunkSize = 36 + subChunk2Size;

            // RIFF header
            writer.Write(Encoding.ASCII.GetBytes("RIFF"));
            writer.Write(chunkSize);
            writer.Write(Encoding.ASCII.GetBytes("WAVE"));

            // fmt sub-chunk
            writer.Write(Encoding.ASCII.GetBytes("fmt "));
            writer.Write(16); // Subchunk1Size for PCM
            writer.Write((short)1); // AudioFormat: 1 = PCM
            writer.Write((short)channels);
            writer.Write(sampleRate);
            writer.Write(byteRate);
            writer.Write(blockAlign);
            writer.Write((short)bitsPerSample);

            // data sub-chunk
            writer.Write(Encoding.ASCII.GetBytes("data"));
            writer.Write(subChunk2Size);

            // Write 16-bit PCM samples
            for (int i = 0; i < samples.Length; i++)
            {
                float clamped = Mathf.Clamp(samples[i], -1f, 1f);
                short pcmShort = (short)(clamped * 32767f);
                writer.Write(pcmShort);
            }

            return stream.ToArray();
        }
    }
}
