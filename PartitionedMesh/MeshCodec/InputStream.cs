using SevenZip;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.MeshCodec
{
    public class InputStream
    {
        public Stream Stream { get; set; }
        public BinaryReader Reader { get; set; }

        public InputStream(string path)
        {
            Stream = File.OpenRead(path);
            Reader = new BinaryReader(Stream);

        }
        public void Close()
        {
            if (Stream != null)
                Stream.Close();
            if (Reader != null)
                Reader.Close();
        }
        public Vector3 ReadVector()
        {
            return new Vector3(Reader.ReadSingle(), Reader.ReadSingle(), Reader.ReadSingle());
        }
        public int interleavedRetrive(byte[] data, int offset, int stride)
        {
            byte b1 = data[offset + 3 * stride];
            byte b2 = data[offset + 2 * stride];
            byte b3 = data[offset + 1 * stride];
            byte b4 = data[offset];

            int i1 = ((int)b1) & 0xff;
            int i2 = ((int)b2) & 0xff;
            int i3 = ((int)b3) & 0xff;
            int i4 = ((int)b4) & 0xff;

            return i1 | (i2 << 8) | (i3 << 16) | (i4 << 24);
        }
        public int[] readPackedInts(int count, int size, bool signed)
        {
            int[] data = new int[count * size];
            byte[] tmp = readCompressedData(count * size * 4);//a Integer is 4 bytes
                                                              // Convert interleaved array to integers
            for (int i = 0; i < count; ++i)
            {
                for (int k = 0; k < size; ++k)
                {
                    int val = interleavedRetrive(tmp, i + k * count, count * size);
                    if (signed)
                    {
                        long x = ((long)val) & 0xFFFFFFFFL;//not sure if correct
                        val = (x & 1) != 0 ? -(int)((x + 1) >> 1) : (int)(x >> 1);
                    }
                    data[i * size + k] = val;
                }
            }
            return data;
        }
        public byte[] readCompressedData(int size)
        {
            int compressedSize = Reader.ReadInt32();
            byte[] toDecompress = Reader.ReadBytes(compressedSize); 

            SevenZip.Compression.LZMA.Decoder decoder = new SevenZip.Compression.LZMA.Decoder();
            using (MemoryStream input = new MemoryStream(toDecompress))
            using (MemoryStream output = new MemoryStream())
            {

                // Read the decoder properties
                byte[] properties = new byte[5];
                input.Read(properties, 0, 5);

                decoder.SetDecoderProperties(properties);
                decoder.Code(input, output, input.Length, size, null);

                return output.ToArray();
            }

        }

    }
}
