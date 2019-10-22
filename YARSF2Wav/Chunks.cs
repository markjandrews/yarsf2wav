using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YARSF2Wav
{
    class Chunks
    {
    }

    public class WaveHeader
    {
        public string GroupId;
        public uint FileLength;
        public string RiffType;

        public WaveHeader()
        {
            FileLength = 0;
            GroupId = "RIFF";
            RiffType = "WAVE";
        }
    }

    public class WaveFormatChunk
    {
        public string ChunkId;
        public uint ChunkSize;
        public ushort FormatTag;
        public ushort NumChannels;
        public uint SampleRate;
        public uint ByteRate;
        public ushort BlockAlign;
        public ushort BitsPerSample;

        public WaveFormatChunk()
        {
            ChunkId = "fmt ";
            ChunkSize = 16;
            FormatTag = 1;
            NumChannels = 1; // 2 for stereo
            SampleRate = 8000; // 44100 etc;
            BitsPerSample = 8; // 16 etc;
            BlockAlign = (ushort)(NumChannels * (BitsPerSample / 8));
            ByteRate = SampleRate * BlockAlign;
        }
    }

    public class WaveDataChunk
    {
        public string ChunkId;
        public uint ChunkSize;
        public byte[] Array;

        public WaveDataChunk()
        {
            Array = new byte[0];
            ChunkSize = 0;
            ChunkId = "data";
        }
    }

    public class WaveFile
    {
        public WaveHeader Header;
        public WaveFormatChunk Format;
        public WaveDataChunk Data;

        public WaveFile(ushort channels, ushort sampleRate, byte[] soundData, ushort bitsPerSample)
        {
            Header = new WaveHeader();
            Format = new WaveFormatChunk();
            Data = new WaveDataChunk();

            Format.NumChannels = channels;
            Format.SampleRate = sampleRate;
            Format.BitsPerSample = bitsPerSample;

            Format.BlockAlign = (ushort)(Format.NumChannels * (Format.BitsPerSample / 8));
            Format.ByteRate = Format.SampleRate * Format.BlockAlign;

            Data.Array = soundData;
            Data.ChunkSize = (uint)Data.Array.Length;
        }

        public WaveFile(ushort channels, ushort sampleRate, byte[] soundData) : this(channels, sampleRate, soundData, 8) { }

        public T Save<T>() where T: Stream, new()
        {
            var writer = new BinaryWriter(new T(), Encoding.Default, true);
            writer.Write(Header.GroupId.ToCharArray());
            writer.Write(Header.FileLength);
            writer.Write(Header.RiffType.ToCharArray());
            writer.Write(Format.ChunkId.ToCharArray());
            writer.Write(Format.ChunkSize);
            writer.Write(Format.FormatTag);
            writer.Write(Format.NumChannels);
            writer.Write(Format.SampleRate);
            writer.Write(Format.ByteRate);
            writer.Write(Format.BlockAlign);
            writer.Write(Format.BitsPerSample);

            writer.Write(Data.ChunkId.ToCharArray());
            writer.Write(Data.ChunkSize);
            writer.Write(Data.Array);

            writer.Seek(4, SeekOrigin.Begin);
            uint filesize = (uint)writer.BaseStream.Length;
            writer.Write(filesize - 8);
            writer.Seek(0, SeekOrigin.Begin);
            var result = writer.BaseStream;
            writer.Close();

            return (T)result;
        }
    }
}
