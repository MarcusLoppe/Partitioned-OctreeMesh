using PartitionedMesh.Common;
using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Structures.BVH
{
    public struct BVHNodeStruct
    {
        public int NodeIdx { get; set; }
        public int LeftChildIdx { get; set; }
        public int RightChild { get; set; }
        public BoundingBox3 Bounds { get; set; }
        public OctreeMesh OctreeMesh { get; set; } 
    }

    public class BVHNode
    {
        public BVHNode? LeftChild { get; set; }
        public BVHNode? RightChild { get; set; }
        public BoundingBox3 Bounds { get; set; }
        public OctreeMesh OctreeMesh { get; set; }

        public BVHNode(OctreeMesh octreeMesh, BoundingBox3 bounds)
        {
            OctreeMesh = octreeMesh;
            Bounds = bounds;
        }
        public void ExtractNodes(List<BVHNode> nodes)
        {
            nodes.Add(this);
            if (LeftChild != null)
                LeftChild.ExtractNodes(nodes);
            if (RightChild != null)
                RightChild.ExtractNodes(nodes);
        } 
     
    }
}
