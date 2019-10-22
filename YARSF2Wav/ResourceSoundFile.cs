using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace YARSF2Wav
{
    class ResourceSoundFile
    {
        public const ushort SOUND_CHUNK = 250;
        public const ushort SOUND_ADPCM_CHUNK = 125;
        public const ushort SOUND_FILE_BUFFER_SIZE = SOUND_CHUNK + 2; // 12.8 mS @ 8KHz

        public const ushort FILEFORMAT_RAW_SOUND = 0x0100;
        public const ushort FILEFORMAT_ADPCM_SOUND = 0x0101;
        public const ushort SOUND_MODE_ONCE = 0x00;
        public const ushort SOUND_LOOP = 0x01;
        public const short SOUND_ADPCM_INIT_VALPREV = 0x7F;
        public const short SOUND_ADPCM_INIT_INDEX = 20;

        public const short STEP_SIZE_TABLE_ENTRIES = 89;
        public const short INDEX_TABLE_ENTRIES = 16;

        public static readonly short[] StepSizeTable = new short[STEP_SIZE_TABLE_ENTRIES] {
            7, 8, 9, 10, 11, 12, 13, 14, 16, 17,
            19, 21, 23, 25, 28, 31, 34, 37, 41, 45,
            50, 55, 60, 66, 73, 80, 88, 97, 107, 118,
            130, 143, 157, 173, 190, 209, 230, 253, 279, 307,
            337, 371, 408, 449, 494, 544, 598, 658, 724, 796,
            876, 963, 1060, 1166, 1282, 1411, 1552, 1707, 1878, 2066,
            2272, 2499, 2749, 3024, 3327, 3660, 4026, 4428, 4871, 5358,
            5894, 6484, 7132, 7845, 8630, 9493, 10442, 11487, 12635, 13899,
            15289, 16818, 18500, 20350, 22385, 24623, 27086, 29794, 32767
        };

        public static readonly short[] IndexTable = new short[INDEX_TABLE_ENTRIES] {
            -1, -1, -1, -1, 2, 4, 6, 8,
            -1, -1, -1, -1, 2, 4, 6, 8
        };

        public long SoundFileLength;
        public ushort SoundFileFormat;
        public ushort SoundDataLength;
        public ushort SoundSampleRate;
        public ushort SoundPlayMode;
        public byte[] SoundData;
        public int BytesToWrite;

        short ValPrev;
        short Index;
        short Step;

        public ResourceSoundFile(string path)
        {
            FileInfo fi = new FileInfo(path);
            SoundFileLength = fi.Length;

            using (var file = File.OpenRead(path))
            {
                var tmp1 = file.ReadByte();
                var tmp2 = file.ReadByte();
                SoundFileFormat = (UInt16)(tmp1 << 8 | tmp2);

                tmp1 = file.ReadByte();
                tmp2 = file.ReadByte();
                SoundDataLength = (UInt16)(tmp1 << 8 | tmp2);

                tmp1 = file.ReadByte();
                tmp2 = file.ReadByte();
                SoundSampleRate = (UInt16)(tmp1 << 8 | tmp2);

                tmp1 = file.ReadByte();
                tmp2 = file.ReadByte();
                SoundPlayMode = (UInt16)(tmp1 << 8 | tmp2);

                byte[] AdPcmData;

                if (FILEFORMAT_ADPCM_SOUND == SoundFileFormat)
                {
                    ValPrev = SOUND_ADPCM_INIT_VALPREV;
                    Index = SOUND_ADPCM_INIT_INDEX;
                    Step = StepSizeTable[Index];

                    AdPcmData = new byte[SoundDataLength];
                    SoundData = new byte[SoundDataLength * 2];

                    var BytesRead = file.Read(AdPcmData, 0, SoundDataLength);

                    for (var i = 0; i < BytesRead; i++)
                    {
                        SoundData[2 * i] = SoundGetAdPcmValue((byte)((AdPcmData[i] >> 4) & 0x0F));
                        SoundData[2 * i + 1] = SoundGetAdPcmValue((byte)(AdPcmData[i] & 0x0F));
                    }

                    BytesToWrite = (BytesRead * 2);
                }
                else
                {
                    SoundData = new byte[SoundDataLength];
                    var BytesRead = file.Read(SoundData, 0, SoundDataLength);
                    BytesToWrite = BytesRead;
                }
            }

        }

        private byte SoundGetAdPcmValue(byte Delta)
        {
            short VpDiff;
            byte Sign;

            Step = StepSizeTable[Index];
            Index += IndexTable[Delta];

            if (Index < 0)
            {
                Index = 0;
            }
            else
            {
                if (Index > (STEP_SIZE_TABLE_ENTRIES - 1))
                {
                    Index = STEP_SIZE_TABLE_ENTRIES - 1;
                }
            }

            Sign = (byte)(Delta & 8);                     // Separate sign
            Delta = (byte)(Delta & 7);                    // Separate magnitude
            VpDiff = (short)(Step >> 3);     // Compute difference and new predicted value

            if ((Delta & 4) != 0) VpDiff += Step;
            if ((Delta & 2) != 0) VpDiff += (short)(Step >> 1);
            if ((Delta & 1) != 0) VpDiff += (short)(Step >> 2);

            if (Sign != 0)
                ValPrev -= VpDiff;    // "Add" with sign
            else
                ValPrev += VpDiff;

            if (ValPrev > 255)       // Clamp value to 8-bit unsigned
            {
                ValPrev = 255;
            }
            else
            {
                if (ValPrev < 0)
                {
                    ValPrev = 0;
                }
            }

            Step = StepSizeTable[Index];  // Update step value

            return (byte)ValPrev;                     // Return decoded byte (nibble xlated -> 8 bit)
        }
    }
}
