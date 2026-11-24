using System;
using System.IO;
using System.Text;
using ThroatBalls.Logic;
using Xunit;

namespace ThroatBalls.Tests
{
    public class WavReaderTests
    {
        private const ushort Pcm = 1;
        private const ushort Float = 3;
        private const ushort Extensible = 0xFFFE;

        /// <summary>A WAV file: RIFF header, the given chunks in order.</summary>
        private static byte[] Wav(params byte[][] chunks)
        {
            using var body = new MemoryStream();
            body.Write(Encoding.ASCII.GetBytes("WAVE"), 0, 4);
            foreach (var chunk in chunks)
            {
                body.Write(chunk, 0, chunk.Length);
            }
            using var file = new MemoryStream();
            var w = new BinaryWriter(file);
            w.Write(Encoding.ASCII.GetBytes("RIFF"));
            w.Write((int)body.Length);
            w.Write(body.ToArray());
            return file.ToArray();
        }

        private static byte[] Chunk(string id, byte[] data)
        {
            using var s = new MemoryStream();
            var w = new BinaryWriter(s);
            w.Write(Encoding.ASCII.GetBytes(id));
            w.Write(data.Length);
            w.Write(data);
            if (data.Length % 2 == 1)
            {
                w.Write((byte)0); // RIFF pads odd chunks
            }
            return s.ToArray();
        }

        private static byte[] Fmt(ushort format, ushort channels, int rate, ushort bits, ushort subFormat = 0)
        {
            using var s = new MemoryStream();
            var w = new BinaryWriter(s);
            w.Write(format);
            w.Write(channels);
            w.Write(rate);
            w.Write(rate * channels * bits / 8);
            w.Write((ushort)(channels * bits / 8));
            w.Write(bits);
            if (format == Extensible)
            {
                w.Write((ushort)22);
                w.Write(bits);
                w.Write(0);
                w.Write(subFormat);
                w.Write(new byte[14]); // rest of the sub-format GUID
            }
            return Chunk("fmt ", s.ToArray());
        }

        private static byte[] Data16(params short[] samples)
        {
            var bytes = new byte[samples.Length * 2];
            Buffer.BlockCopy(samples, 0, bytes, 0, bytes.Length);
            return Chunk("data", bytes);
        }

        [Fact]
        public void Reads_16_bit_mono()
        {
            Assert.True(WavReader.TryRead(Wav(Fmt(Pcm, 1, 22050, 16), Data16(0, 16384, -32768, 32767)), out var wav, out var error), error);
            Assert.Equal(1, wav.Channels);
            Assert.Equal(22050, wav.SampleRate);
            Assert.Equal(new[] { 0f, 0.5f, -1f, 32767f / 32768f }, wav.Samples);
        }

        [Fact]
        public void Reads_16_bit_stereo_interleaved()
        {
            Assert.True(WavReader.TryRead(Wav(Fmt(Pcm, 2, 44100, 16), Data16(16384, -16384)), out var wav, out _));
            Assert.Equal(2, wav.Channels);
            Assert.Equal(new[] { 0.5f, -0.5f }, wav.Samples);
        }

        [Fact]
        public void Reads_8_bit_unsigned()
        {
            Assert.True(WavReader.TryRead(Wav(Fmt(Pcm, 1, 8000, 8), Chunk("data", new byte[] { 128, 0, 255, 192 })), out var wav, out _));
            Assert.Equal(new[] { 0f, -1f, 127f / 128f, 0.5f }, wav.Samples);
        }

        [Fact]
        public void Reads_24_bit()
        {
            // 0x400000 = half scale, 0x800000 = -1 (little-endian, 3 bytes each)
            var data = Chunk("data", new byte[] { 0x00, 0x00, 0x40, 0x00, 0x00, 0x80 });
            Assert.True(WavReader.TryRead(Wav(Fmt(Pcm, 1, 48000, 24), data), out var wav, out _));
            Assert.Equal(new[] { 0.5f, -1f }, wav.Samples);
        }

        [Fact]
        public void Reads_32_bit_float()
        {
            var bytes = new byte[8];
            Buffer.BlockCopy(new[] { 0.25f, -0.75f }, 0, bytes, 0, 8);
            Assert.True(WavReader.TryRead(Wav(Fmt(Float, 1, 48000, 32), Chunk("data", bytes)), out var wav, out _));
            Assert.Equal(new[] { 0.25f, -0.75f }, wav.Samples);
        }

        [Fact]
        public void Reads_extensible_pcm_as_audio_editors_write_it()
        {
            Assert.True(WavReader.TryRead(Wav(Fmt(Extensible, 1, 44100, 16, Pcm), Data16(16384)), out var wav, out var error), error);
            Assert.Equal(new[] { 0.5f }, wav.Samples);
        }

        [Fact]
        public void Skips_other_chunks_before_and_between_including_odd_sized_ones()
        {
            var file = Wav(Chunk("LIST", new byte[] { 1, 2, 3 }), Fmt(Pcm, 1, 22050, 16), Chunk("fact", new byte[4]), Data16(16384));
            Assert.True(WavReader.TryRead(file, out var wav, out var error), error);
            Assert.Equal(new[] { 0.5f }, wav.Samples);
        }

        [Fact]
        public void A_data_chunk_longer_than_the_file_reads_what_is_there()
        {
            // Some recorders leave the size at its maximum while streaming.
            var file = Wav(Fmt(Pcm, 1, 22050, 16), Data16(16384, 16384));
            file[file.Length - 8] = 0xFF; // data size low byte: far larger than the 4 bytes that follow
            Assert.True(WavReader.TryRead(file, out var wav, out _));
            Assert.Equal(new[] { 0.5f, 0.5f }, wav.Samples);
        }

        [Fact]
        public void Refuses_what_is_not_a_wav_file()
        {
            Assert.False(WavReader.TryRead(Encoding.ASCII.GetBytes("OggS and more bytes here"), out _, out var error));
            Assert.Contains("not a WAV file", error);
            Assert.False(WavReader.TryRead(null, out _, out _));
            Assert.False(WavReader.TryRead(new byte[3], out _, out _));
        }

        [Fact]
        public void Refuses_a_file_without_format()
        {
            Assert.False(WavReader.TryRead(Wav(Data16(1)), out _, out var error));
            Assert.Contains("no format", error);
        }

        [Fact]
        public void Refuses_a_file_without_sound()
        {
            Assert.False(WavReader.TryRead(Wav(Fmt(Pcm, 1, 22050, 16)), out _, out var error));
            Assert.Contains("no sound data", error);
            Assert.False(WavReader.TryRead(Wav(Fmt(Pcm, 1, 22050, 16), Chunk("data", new byte[0])), out _, out error));
            Assert.Contains("no sound data", error);
        }

        [Theory]
        [InlineData(2, 16)] // ADPCM
        [InlineData(Pcm, 12)]
        [InlineData(Float, 64)]
        public void Refuses_compressed_or_odd_formats(ushort format, ushort bits)
        {
            Assert.False(WavReader.TryRead(Wav(Fmt(format, 1, 22050, bits), Chunk("data", new byte[16])), out _, out var error));
            Assert.Contains("unsupported", error);
        }

        [Fact]
        public void Refuses_zero_channels_or_rate()
        {
            Assert.False(WavReader.TryRead(Wav(Fmt(Pcm, 0, 22050, 16), Data16(1)), out _, out var error));
            Assert.Contains("unsupported", error);
            Assert.False(WavReader.TryRead(Wav(Fmt(Pcm, 1, 0, 16), Data16(1)), out _, out error));
            Assert.Contains("unsupported", error);
        }

        [Fact]
        public void Drops_a_trailing_partial_frame()
        {
            // Stereo 16 bit: 6 bytes are one full frame plus half of one.
            Assert.True(WavReader.TryRead(Wav(Fmt(Pcm, 2, 22050, 16), Chunk("data", new byte[] { 0, 64, 0, 192, 0, 64 })), out var wav, out _));
            Assert.Equal(new[] { 0.5f, -0.5f }, wav.Samples);
        }
    }
}
