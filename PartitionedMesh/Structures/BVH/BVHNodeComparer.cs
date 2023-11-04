using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Structures.BVH
{
    public class BVHNodeComparer : IComparer<BVHNode>
    {
        private int splitAxis;

        public BVHNodeComparer(int splitAxis)
        {
            this.splitAxis = splitAxis;
        }

        public int Compare(BVHNode a, BVHNode b)
        { 
            switch (splitAxis)
            {
                case 0: // Compare along X-axis
                    return a.Bounds.Min.X.CompareTo(b.Bounds.Min.X);
                case 1: // Compare along Y-axis
                    return a.Bounds.Min.Y.CompareTo(b.Bounds.Min.Y);
                case 2: // Compare along Z-axis
                    return a.Bounds.Min.Z.CompareTo(b.Bounds.Min.Z);
                default:
                    throw new ArgumentOutOfRangeException("splitAxis", "Invalid split axis.");
            }
        }
    }

}
