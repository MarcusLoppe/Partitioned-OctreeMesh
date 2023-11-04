
using PartitionedMesh.Shapes;
using System.Collections;
using System.Numerics;

namespace PartitionedMesh.MeshCodec
{
    public class MeshEncoder
    { 
        public static Grid setupGrid(List<Vector3> vertices)
        {
            int[] division = new int[3];
            var bounds = BoundingBox3.CreateFromPoints(vertices);

            float[] factor = new float[3] { bounds.Size.X, bounds.Size.Y, bounds.Size.Z };
            float sum = factor[0] + factor[1] + factor[2];

            sum = 1.0f / sum;
            for (int i = 0; i < 3; ++i)
            {
                factor[i] *= sum;
            }
            if (bounds.Volume() < 10)
            {
                division = new[] { 4, 4, 4 };
            }
            else
            {
                double wantedGrids = Math.Pow(100.0f * vertices.Count(), 1.0f / 3.0f);
                for (int i = 0; i < 3; ++i)
                {
                    division[i] = (int)Math.Ceiling(wantedGrids * factor[i]);
                    if (division[i] < 1)
                    {
                        division[i] = 1;
                    }
                }
            }
            Grid grid = new Grid(bounds.Min, bounds.Max, division);
            return grid;
        }
        
        public static int[] reIndexIndices(SortableVertex[] sortVertices, int[] indices)
        {
            // Create temporary lookup-array, O(n)
            int[] indexLUT = new int[sortVertices.Length];
            int[] newIndices = new int[indices.Length];

            for (int i = 0; i < sortVertices.Length; ++i)
            {
                indexLUT[sortVertices[i].originalIndex] = i;
            }

            // Convert old indices to new indices, O(n)
            for (int i = 0; i < indices.Length; ++i)
            {
                newIndices[i] = indexLUT[indices[i]];
            }
            return newIndices;
        }

        public static void reIndexTrianglesVertices(SortableVertex[] sortVertices, List<TriangleIndices> indices)
        {
            // Create temporary lookup-array, O(n)
            int[] indexLUT = new int[sortVertices.Length];  
            for (int i = 0; i < sortVertices.Length; ++i)
            {
                indexLUT[sortVertices[i].originalIndex] = i;
            }

            // Convert old indices to new indices, O(n)
            for (int i = 0; i < indices.Count; ++i)
            {
                indices[i].V0 = indexLUT[indices[i].V0];
                indices[i].V1 = indexLUT[indices[i].V1];
                indices[i].V2 = indexLUT[indices[i].V2]; 
            }
        }
        public static List<Vector3> reIndexVertices(SortableVertex[] sortVertices, List<Vector3> vertices)
        { 
            Vector3[] newVerts = new Vector3[vertices.Count];

            for (int i = 0; i < sortVertices.Length; ++i)
            {
                newVerts[i] = vertices[sortVertices[i].originalIndex];
            } 
            return newVerts.ToList();
        } 
        public static int[] makeVertexDeltas(float[] vertices, SortableVertex[] sortVertices, Grid grid, float vertexPrecision)
        {
            int vc = vertices.Length / 3; 
            // Vertex scaling factor
            float scale = 1.0f / vertexPrecision;

            float prevGridIndex = 0x7fffffff;
            int prevDeltaX = 0;
            int[] intVertices = new int[vc * 3];
            for (int i = 0; i < vc; ++i)
            {
                // Get grid box origin
                int gridIdx = sortVertices[i].gridIndex;
                float[] gridOrigin = grid.gridIdxToPoint(gridIdx);

                // Get old vertex coordinate index (before vertex sorting)
                int oldIdx = sortVertices[i].originalIndex;

                // Store delta to the grid box origin in the integer vertex array. For the
                // X axis (which is sorted) we also do the delta to the previous coordinate
                // in the box.
                int deltaX = (int)Math.Floor(scale * (vertices[oldIdx * 3] - gridOrigin[0]) + 0.5f);
                if (gridIdx == prevGridIndex)
                {
                    intVertices[i * 3] = deltaX - prevDeltaX;
                }
                else
                {
                    intVertices[i * 3] = deltaX;
                }

                intVertices[i * 3 + 1] = (int)Math.Floor(scale * (vertices[oldIdx * 3 + 1] - gridOrigin[1]) + 0.5f);
                intVertices[i * 3 + 2] = (int)Math.Floor(scale * (vertices[oldIdx * 3 + 2] - gridOrigin[2]) + 0.5f);

                prevGridIndex = gridIdx;
                prevDeltaX = deltaX;
            }

            return intVertices;
        } 
        public static BitArray EncodeVLC(List<uint> values, int numBits)
        {
            BitArray result = new BitArray(0);
            List<bool> bools = new();
            int numIntervals = (int)Math.Pow(2, numBits) - 1;
            foreach (uint value in values)
            {
                // Split the value into 4-bit chunks
                List<bool> chunks = new List<bool>();
                int temp = (int) value; 
                do
                {
                    int segment = temp & numIntervals;
                    for (int j = 0; j < numBits; j++)
                    {
                        chunks.Add((segment & (1 << j)) != 0); // Extract individual bit using bitwise AND
                    }
                    temp >>= numBits; 
                    if (temp > 0)
                    {
                        chunks.Add(true);
                    }
                    else
                    {
                        chunks.Add(false);
                    }
                } while (temp > 0);

                result.Length += chunks.Count;
                int index = result.Length - chunks.Count;  
                foreach(var val in chunks)
                {
                    result[index] = val;
                    bools.Add(result[index]);
                    index++;
                }
            }
            return result;
        }
         
        public static byte[] RLEEncodeByte(List<int> input)
        {
            if (input.Count() == 0)
                return new byte[0];
            var maxValues = new List<int> { 0, 1, 3, 7, 15, 31, 63, 127, 255, 511, 1023, 2047, 4095, 8191, 16383, 32767, 65535 };

            var output = new List<(int, byte)>();
            var currentValue = input[0];
            var currentCount = 1;
            for (int i = 1; i < input.Count; i++)
            {
                if (input[i] == currentValue && currentCount < 255)
                {
                    currentCount++;
                }
                else
                {

                    output.Add(new(currentValue, (byte)currentCount));

                    currentValue = input[i];
                    currentCount = 1;
                }
            }

            output.Add(new(currentValue, (byte)currentCount));
             
            MemoryStream bitarr = new();
            BinaryWriter binWriter = new(bitarr); 

            var vals = output.Select(a => (uint) a.Item1).ToList();
            var counts = output.Select(a =>(uint) a.Item2).ToList();

            var val = EncodeVLC(vals, 3);
            var count = EncodeVLC(counts, 3); 
            
            byte[] ret = new byte[(val.Length - 1) / 8 + 1];
            val.CopyTo(ret, 0);

            byte[] ret2 = new byte[(count.Length - 1) / 8 + 1];
            count.CopyTo(ret2, 0); 

            binWriter.Write(vals.Count);
            binWriter.Write(counts.Count);

            binWriter.Write(ret.Length);
            binWriter.Write(ret2.Length);

            binWriter.Write(ret);
            binWriter.Write(ret2); 

            var buf = bitarr.GetBuffer(); 
            return buf;
        } 
        public static int[] vDelta(List<int> ints)
        {
            int[] gridIndicies = new int[ints.Count()];
            gridIndicies[0] = ints[0];
            for (int i = 1; i < ints.Count(); ++i)
                gridIndicies[i] = ints[i] - ints[i - 1];
            return gridIndicies;
        } 
        public static SortableVertex[] sortVertices(Grid grid, float[] v)
        {
            // Prepare sort vertex array
            int vc = v.Length / 3;
            SortableVertex[] sortVertices = new SortableVertex[vc];
            for (int i = 0; i < vc; ++i)
            {
                // Store vertex properties in the sort vertex array
                float[] point = new float[] { v[i * 3], v[i * 3 + 1], v[i * 3 + 2] };
                int p2g = grid.pointToGridIdx(point);
                sortVertices[i] = new SortableVertex(v[i * 3], p2g, i);
            }

            // Sort vertices. The elements are first sorted by their grid indices, and
            // scondly by their x coordinates.
            Array.Sort(sortVertices);
            return sortVertices;
        }
        public static void rearrangeTriangles(int[] indices)
        {
            if (indices.Length % 3 != 0)
                throw new Exception();
            for (int off = 0; off < indices.Length; off += 3)
            {
                if ((indices[off + 1] < indices[off]) && (indices[off + 1] < indices[off + 2]))
                {
                    int tmp = indices[off];
                    indices[off] = indices[off + 1];
                    indices[off + 1] = indices[off + 2];
                    indices[off + 2] = tmp;
                }
                else if ((indices[off + 2] < indices[off]) && (indices[off + 2] < indices[off + 1]))
                {
                    int tmp = indices[off];
                    indices[off] = indices[off + 2];
                    indices[off + 2] = indices[off + 1];
                    indices[off + 1] = tmp;
                }
            }

            // Step 2: Sort the triangles based on the first triangle index
            TriangleIndices[] tris = new TriangleIndices[indices.Length / 3];
            for (int i = 0; i < tris.Length; i++)
            {
                int off = i * 3;
                tris[i] = new TriangleIndices(indices[off], indices[off + 1], indices[off + 2]);
            } 
            Array.Sort(tris); 
            for (int i = 0; i < tris.Length; i++)
            {
                int off = i * 3;

                tris[i].copyBack(indices, off);
            }
        }
        public static void rearrangeTriangles(List<TriangleIndices> indices)
        { 
            for (int off = 0; off < indices.Count; off ++)
            {
                if ((indices[off].V1 < indices[off].V0) && (indices[off].V1 < indices[off].V2))
                {
                    int tmp = indices[off].V0;
                    indices[off].V0 = indices[off].V1;
                    indices[off].V1 = indices[off].V2;
                    indices[off].V2 = tmp;
                }
                else if ((indices[off].V2 < indices[off].V0) && (indices[off].V2 < indices[off].V1))
                {
                    int tmp = indices[off].V0;
                    indices[off].V0 = indices[off].V2;
                    indices[off].V2 = indices[off].V1;
                    indices[off].V1 = tmp;
                } 
            } 
            indices.Sort(); 
        }
        public static void makeIndexDeltas(int[] indices)
        {
            if (indices.Length % 3 != 0)
                throw new Exception();

            for (int i = indices.Length / 3 - 1; i >= 0; --i)
            {
                // Step 1: Calculate delta from second triangle index to the previous
                // second triangle index, if the previous triangle shares the same first
                // index, otherwise calculate the delta to the first triangle index
                if ((i >= 1) && (indices[i * 3] == indices[(i - 1) * 3]))
                {
                    indices[i * 3 + 1] -= indices[(i - 1) * 3 + 1];
                }
                else
                {
                    indices[i * 3 + 1] -= indices[i * 3];
                }

                // Step 2: Calculate delta from third triangle index to the first triangle
                // index
                indices[i * 3 + 2] -= indices[i * 3];

                // Step 3: Calculate derivative of the first triangle index
                if (i >= 1)
                {
                    indices[i * 3] -= indices[(i - 1) * 3];
                }
            }
        }
    }
}
