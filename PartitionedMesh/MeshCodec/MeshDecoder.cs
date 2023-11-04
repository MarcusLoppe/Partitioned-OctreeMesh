using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.MeshCodec
{
    public class MeshDecoder
    { 
        public static List<int> RLEDecode(InputStream stream, int elements)
        { 
            var output = new List<int>(); 
            
            for(int i = 0; i < elements; i++)
            {
                var value = stream.Reader.ReadInt32();
                var count = stream.Reader.ReadInt32();
                for (int b = 0; b < count; b++)
                    output.Add(value);
            }
            return output;
        }
        public static List<int> restoreVDelta(List<int> ints)
        {
            List<int> gridIndicies = new(ints);
            for (int i = 1; i < ints.Count(); ++i)
                gridIndicies[i] += gridIndicies[i - 1];
            return gridIndicies;
        }
        public static List<int> Decode(BitArray bits, int numBits)
        {
            List<int> result = new List<int>();
            int count = 0;
            int headerBit = numBits + 1;
            bool more = false;
            List<bool> valueBits = new();
            foreach (bool bit in bits)
            {
                count++;

                if (count % headerBit == 0)
                {
                    more = bit;
                    // If not, add the value to the result list and reset
                    if (!more)
                    {
                        int decodedValue = 0;
                        for (int i = 0; i < valueBits.Count; i += 4)
                        {
                            int segment = 0;
                            for (int j = 0; j < 4; j++)
                            {
                                if (i + j < valueBits.Count && valueBits[i + j])
                                {
                                    segment |= 1 << j; // Set the corresponding bit in the segment using bitwise OR
                                }
                            }

                            decodedValue |= segment << (i / 4 * 4); // Add the segment to the decoded value
                        }
                        result.Add(decodedValue);
                        count = 0;

                        valueBits = new();
                    }
                }
                else
                    valueBits.Add(bit);
            }
            // If there are leftover bits, throw an exception
            if (more || count > 0)
            {
                //throw new ArgumentException("Invalid VLQ encoding");
            }
            return result;
        }
        public static List<int> RLEDecode7Int(byte[] input)
        {
            if (input.Length == 0)
                return new();


            MemoryStream stream = new(input);
            var reader = new BinaryReader(stream);

            var valuesLength = reader.ReadInt32();
            var countLength = reader.ReadInt32();

            var output = new List<int>();
            var decodedValues = new List<int>();
            var decodedCounts = new List<int>();

            for (int i = 0; i < valuesLength; i++)
                decodedValues.Add(reader.ReadInt32());

            for (int i = 0; i < countLength; i++)
                decodedCounts.Add(reader.ReadByte());

            for (int i = 0; i < decodedValues.Count(); i++)
                for (int c = 0; c < decodedCounts[i]; c++)
                    output.Add(decodedValues[i]);

            return output;
        }
        public static List<int> RLEDecode(byte[] input)
        {
            if (input.Length == 0)
                return new();

            var valuesLength = BitConverter.ToInt32(input, 0);
            var countLength = BitConverter.ToInt32(input, 4);

            var valuesSize = BitConverter.ToInt32(input, 8);
            var countSize = BitConverter.ToInt32(input, 12);

            var byteStart = 16;

            var valueBytes = new byte[valuesSize];
            int valueIdx = 0;
            for (int i = byteStart; i < valuesSize + byteStart; i++)
                valueBytes[valueIdx++] = input[i];

            var countBytes = new byte[countSize];
            int countIdx = 0;
            for (int i = valuesSize + byteStart; i < valuesSize + countSize + byteStart; i++)
                countBytes[countIdx++] = input[i];
             
            var output = new List<int>();
            var decodedValues = Decode(new BitArray(valueBytes), 3).Take(valuesLength).ToList();
            var decodedCounts = Decode(new BitArray(countBytes), 3).Take(countLength).ToList();

            for (int i = 0; i < decodedValues.Count(); i++)
                for (int c = 0; c < decodedCounts[i]; c++)
                    output.Add(decodedValues[i]);

            return output;
        }
        public static int GetBitsAsInt(BitArray bitArray, int index, int bits)
        {
            int result = 0;
            // Loop through the bits from the index
            for (int i = 0; i < bits; i++)
            { 
                if (bitArray[index + i]) 
                    result |= 1 << (bits - i - 1); 
            }
            return result;
        } 
        public static float[] restoreVertices(int[] intVertices, int[] gridIndices, Grid grid,float vertexPrecision)
        {
            int ve = 3;
            int vc = intVertices.Length / ve; 

            int prevGridIndex = 0x7fffffff;
            int prevDeltaX = 0;
            float[] vertices = new float[vc * ve];
            for (int i = 0; i < vc; ++i)
            {
                // Get grid box origin
                int gridIdx = gridIndices[i];
                float[] gridOrigin = grid.gridIdxToPoint(gridIdx);

                // Restore original point
                int deltaX = intVertices[i * ve];
                if (gridIdx == prevGridIndex)
                {
                    deltaX += prevDeltaX;
                }
                vertices[i * ve] = vertexPrecision * deltaX + gridOrigin[0];
                vertices[i * ve + 1] = vertexPrecision * intVertices[i * ve + 1] + gridOrigin[1];
                vertices[i * ve + 2] = vertexPrecision * intVertices[i * ve + 2] + gridOrigin[2];

                prevGridIndex = gridIdx;
                prevDeltaX = deltaX;
            }
            return vertices;
        }
        public static void restoreIndices(int triangleCount, int[] indices)
        {
            for (int i = 0; i < triangleCount; ++i)
            {
                // Step 1: Reverse derivative of the first triangle index
                if (i >= 1)
                {
                    indices[i * 3] += indices[(i - 1) * 3];
                }

                // Step 2: Reverse delta from third triangle index to the first triangle
                // index
                indices[i * 3 + 2] += indices[i * 3];

                // Step 3: Reverse delta from second triangle index to the previous
                // second triangle index, if the previous triangle shares the same first
                // index, otherwise reverse the delta to the first triangle index
                if ((i >= 1) && (indices[i * 3] == indices[(i - 1) * 3]))
                {
                    indices[i * 3 + 1] += indices[(i - 1) * 3 + 1];
                }
                else
                {
                    indices[i * 3 + 1] += indices[i * 3];
                }
            }
        }
    }
}
