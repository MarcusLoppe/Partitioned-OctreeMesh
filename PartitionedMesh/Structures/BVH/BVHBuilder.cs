using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Structures.BVH
{
    public class BVHBuilder
    {
        private List<BVHNode> Nodes;
        private BVHNode root;

        public BVHBuilder(List<BVHNode> objects)
        {
            Nodes = objects;
        }
        public static BVHNode RecursiveReconstruct(BVHNodeStruct info, List<BVHNodeStruct> structures)
        {
            BVHNode node = new BVHNode(info.OctreeMesh, info.Bounds);
            node.LeftChild = info.LeftChildIdx == -1 ? null : RecursiveReconstruct(structures[info.LeftChildIdx], structures);
            node.RightChild = info.RightChild == -1 ? null : RecursiveReconstruct(structures[info.RightChild], structures);
            return node;
        }

        public BVHNode BuildBVH()
        {
            root = RecursiveBuild(Nodes);
            return root;
        }

        private BVHNode RecursiveBuild(List<BVHNode> nodes)
        {
            BVHNode node = nodes[0];

            if (nodes.Count() > 1)
            { 
                BoundingBox3 nodeBoundingBox = node.Bounds;
                for (int i = 1; i < nodes.Count(); i++)
                {
                    nodeBoundingBox.ExpandToFit(nodes[i].Bounds);
                }

                int splitAxis = GetLongestAxis(nodeBoundingBox);

                int mid = nodes.Count() / 2; 
                nodes.Sort(new BVHNodeComparer(splitAxis));
                nodes.Remove(node);

                BVHNode leftChild = RecursiveBuild(nodes.GetRange(0, mid).ToList());  
                node.LeftChild = leftChild;
                 
                node.Bounds.ExpandToFit(leftChild.Bounds);
 
                if (nodes.Count() > 1)
                {
                    BVHNode rightChild = RecursiveBuild(nodes.GetRange(mid, nodes.Count() - mid).ToList());
                    node.RightChild = rightChild;
                    node.Bounds.ExpandToFit(rightChild.Bounds);
                }
            } 
            return node;
        }

        private int GetLongestAxis(BoundingBox3 boundingBox)
        {
            Vector3 boxSize = boundingBox.Size;

            if (boxSize.X > boxSize.Y && boxSize.X > boxSize.Z)
            {
                return 0; // X-axis is the longest
            }
            else if (boxSize.Y > boxSize.X && boxSize.Y > boxSize.Z)
            {
                return 1; // Y-axis is the longest
            }
            else
            {
                return 2; // Z-axis is the longest
            }
        }

    }
}
