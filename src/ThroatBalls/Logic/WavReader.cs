using System;
using System.Text;

namespace ThroatBalls.Logic
{
    /// <summary>A decoded WAV file: interleaved samples in [-1, 1], as Unity's AudioClip.SetData takes them.</summary>
    public sealed class WavData
    {
        public WavData(int channels, int sampleRate, float[] samples)
        {
            Channels = channels;
            SampleRate = sampleRate;
            Samples = samples;
        }

        public int Channels { get; }
        public int SampleRate { get; }
        public float[] Samples { get; }
    }

    /// <summary>
    /// Reads uncompressed WAV files (PCM 8, 16, 24, 32 bit and 32-bit float, also the extensible header audio editors
    /// write). Pure code, so the mod needs no Unity web request to load a sound from its folder.
    /// </summary>
    public static class WavReader
    {
        private const int FormatPcm = 1;
        private const int FormatFloat = 3;
        private const int FormatExtensible = 0xFFFE;

        public static bool TryRead(byte[] data, out WavData wav, out string error)
        {
            wav = null;
            if (data == null || data.Length < 12 || Id(data, 0) != "RIFF" || Id(data, 8) != "WAVE")
            {
                error = "not a WAV file";
                return false;
            }
            int format = 0, channels = 0, rate = 0, bits = 0;
            var haveFormat = false;
            var pos = 12;
            while (pos + 8 <= data.Length)
            {
                var id = Id(data, pos);
                var size = BitConverter.ToInt32(data, pos + 4);
                var body = pos + 8;
                if (id == "fmt " && size >= 16 && body + 16 <= data.Length)
                {
                    format = BitConverter.ToUInt16(data, body);
                    channels = BitConverter.ToUInt16(data, body + 2);
                    rate = BitConverter.ToInt32(data, body + 4);
                    bits = BitConverter.ToUInt16(data, body + 14);
                    if (format == FormatExtensible && size >= 26 && body + 26 <= data.Length)
                    {
                        format = BitConverter.ToUInt16(data, body + 24);
                    }
                    haveFormat = true;
                }
                else if (id == "data")
                {
                    if (!haveFormat)
                    {
                        error = "no format before the sound data";
                        return false;
                    }
                    var length = size < 0 || body + size > data.Length ? data.Length - body : size;
                    return Decode(data, body, length, format, channels, rate, bits, out wav, out error);
                }
                if (size < 0)
                {
                    break;
                }
                pos = body + size + (size % 2);
            }
            error = haveFormat ? "no sound data" : "no format";
            return false;
        }

        private static bool Decode(byte[] data, int start, int length, int format, int channels, int rate, int bits, out WavData wav, out string error)
        {
            wav = null;
            var supported = channels > 0 && rate > 0
                && ((format == FormatPcm && (bits == 8 || bits == 16 || bits == 24 || bits == 32)) || (format == FormatFloat && bits == 32));
            if (!supported)
            {
                error = "unsupported: format " + format + ", " + bits + " bit, " + channels + " channels, " + rate + " Hz (save it as 16-bit PCM)";
                return false;
            }
            var bytesPerSample = bits / 8;
            var frames = length / (bytesPerSample * channels);
            if (frames <= 0)
            {
                error = "no sound data";
                return false;
            }
            var samples = new float[frames * channels];
            for (var i = 0; i < samples.Length; i++)
            {
                samples[i] = Sample(data, start + i * bytesPerSample, format, bits);
            }
            wav = new WavData(channels, rate, samples);
            error = null;
            return true;
        }

        private static float Sample(byte[] data, int at, int format, int bits)
        {
            if (format == FormatFloat)
            {
                return BitConverter.ToSingle(data, at);
            }
            switch (bits)
            {
                case 8:
                    return (data[at] - 128) / 128f;
                case 16:
                    return BitConverter.ToInt16(data, at) / 32768f;
                case 24:
                    return ((data[at] << 8 | data[at + 1] << 16 | data[at + 2] << 24) >> 8) / 8388608f;
                default:
                    return BitConverter.ToInt32(data, at) / 2147483648f;
            }
        }

        private static string Id(byte[] data, int at)
        {
            return Encoding.ASCII.GetString(data, at, 4);
        }
    }
}
