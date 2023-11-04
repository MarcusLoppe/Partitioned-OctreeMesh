using PartitionedMesh.Common;
using PartitionedMesh.MeshCodec;
using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Structures
{
    public class GridCell
    {
        public OctreeNode Cell { get; set; }
        public int Index { get; set; } 
        public int[] GridIndices { get; set; }
        public List<DeltaEncodedInts> EncodedGridIndices { get; set; }
        public GridCell(int idx, OctreeNode cell)
        {
            Index = idx;
            Cell = cell;
        }

        public void EncodeGridIndices()
        {
            EncodedGridIndices = new();
            List<int> currentSplit = new() { GridIndices[0] }; 
            for(int i = 1; i < GridIndices.Length; i++)
            { 
                if (Math.Abs(currentSplit.Last() - GridIndices[i]) > 255 || currentSplit.Count() >= 255)
                {
                    EncodedGridIndices.Add(new(Index, MeshEncoder.vDelta(currentSplit))); 
                    currentSplit = new();
                }
                currentSplit.Add(GridIndices[i]);
            }
            EncodedGridIndices.Add(new(Index, MeshEncoder.vDelta(currentSplit))); 
        } 
    }

    public class DeltaEncodedInts
    { 
        public int Index { get; set; } 
        public int[] Indices { get; set; } 
        public DeltaEncodedInts(int idx, int[] indices)
        {
            Index = idx;
            Indices = indices;
        }
        public override string ToString()
        {
            return Indices.Count().ToString();
        }
    }
     
}
