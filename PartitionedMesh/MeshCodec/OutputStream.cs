using SevenZip;
using System.Numerics;
using Encoder = SevenZip.Compression.LZMA.Encoder;

namespace PartitionedMesh.MeshCodec
{
    public class OutputStream
    {
        public Stream Stream { get; set; }
        public BinaryWriter Writer { get; set; }
        public string SavePath = "";
        public OutputStream()
        {
            Stream = new MemoryStream();
            Writer = new BinaryWriter(Stream);
        }
        public OutputStream(string path)
        {
            SavePath = path;
            Stream = File.Create(path); 
            Writer = new BinaryWriter(Stream);
        } 
        public void Save()
        { 
            if (Stream != null)
                Stream.Close();
            if (Writer != null)
                Writer.Close(); 

        }

        public void WriteVector(Vector3 v)
        {
            Writer.Write(v.X);
            Writer.Write(v.Y);
            Writer.Write(v.Z);
        } 
        public void writePackedInts(int[] data, int count, int size, bool signed, bool compress = true)
        {
            if (data.Length < count * size)
                throw new Exception("The data to be written is smaller"
                    + " as stated by other parameters. Needed: " + (count * size) + " Provided: " + data.Length);
            // Allocate memory for interleaved array
            byte[] tmp = new byte[count * size * 4];

            // Convert integers to an interleaved array
            for (int i = 0; i < count; ++i)
            {
                for (int k = 0; k < size; ++k)
                {
                    int val = data[i * size + k];
                    // Convert two's complement to signed magnitude?
                    if (signed)
                    {
                        val = val < 0 ? -1 - (val << 1) : val << 1;
                    }
                    interleavedInsert(val, tmp, i + k * count, count * size);
                }
            }
            // writer.Write(tmp);
            if (compress)
                writeCompressedData(tmp);
            else
                Writer.Write(tmp);
        }

        public void interleavedInsert(int value, byte[] data, int offset, int stride)
        {
            data[offset + 3 * stride] = (byte)(value & 0xff);
            data[offset + 2 * stride] = (byte)((value >> 8) & 0xff);
            data[offset + stride] = (byte)((value >> 16) & 0xff);
            data[offset] = (byte)((value >> 24) & 0xff);
        }
        private static CoderPropID[] propIDs =
            {
                    CoderPropID.DictionarySize,
                    CoderPropID.PosStateBits,
                    CoderPropID.LitContextBits,
                    CoderPropID.LitPosBits,
                    CoderPropID.Algorithm,
                    CoderPropID.NumFastBytes,
                    CoderPropID.MatchFinder,
                    CoderPropID.EndMarker
                };

        public byte[] Compress(byte[] toCompress)
        {
            SevenZip.Compression.LZMA.Encoder coder = new SevenZip.Compression.LZMA.Encoder();

            using (MemoryStream input = new MemoryStream(toCompress))
            using (MemoryStream output = new MemoryStream())
            {

                coder.WriteCoderProperties(output);

                output.Write(BitConverter.GetBytes(input.Length), 0, 8);

                coder.Code(input, output, -1, -1, null);
                return output.ToArray();
            }
        }

        public void writeCompressedData(byte[] data)
        {
            //var cmp = Compress(data);
            //Writer.Write(cmp);
            //return;
            int dicSize = 1 << 26;
            object[] properties =
                {
                    (Int32)dicSize,
                    (Int32)(2),
                    (Int32)(3),
                    (Int32)(0),
                    (Int32)(2),
                    (Int32)(64),
                    "bt4",
                    true
                };

            Encoder encoder = new Encoder();
            encoder.SetCoderProperties(propIDs, properties);

            MemoryStream inStream = new MemoryStream(data);
            MemoryStream outStream = new MemoryStream();
            encoder.WriteCoderProperties(outStream);
            encoder.Code(inStream, outStream, -1, -1, null);
            byte[] compressedData = outStream.ToArray();
            //writer.Write(data.Length);
            Writer.Write(compressedData.Length);
            Writer.Write(compressedData);
        }
    }
}
