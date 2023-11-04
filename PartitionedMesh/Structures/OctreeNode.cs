using PartitionedMesh.Common;
using PartitionedMesh.MeshCodec;
using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Structures
{
    public class OctreeNode  
    {
        public Grid Grid { get; set; }
        public float BoundsTolerance { get; set; }
        public BoundingBox3 Bounds { get; set; }
        public OctreeNode[]? Children { get; set; }
        public int TreeDepth { get; set; }
        public int MaxTreeDepth { get; set; }
        public List<TriangleIndices> Triangles { get; set; } = new();

        public OctreeNode(OctreeNode parent, BoundingBox3 bounds)
        {
            Bounds = bounds;
            TreeDepth = parent.TreeDepth + 1; ;
            MaxTreeDepth = parent.MaxTreeDepth;
            Grid = parent.Grid; 
            BoundsTolerance = Grid.Size[0] / 2;
        }

        public OctreeNode(BoundingBox3 bounds, Grid grid,  int maxTreeDepth)
        {
            Bounds = bounds;
            TreeDepth = 0;
            MaxTreeDepth = maxTreeDepth;
            Grid = grid; 
            BoundsTolerance = Grid.Size[0] / 3;
        } 
        public OctreeNode[] Subdivide()
        { 
            var kids = new OctreeNode[8];
            var split = Bounds.Split();
            for (int i = 0; i < 8; i++)
                kids[i] = new OctreeNode(this, split[i]);
            return kids;
        }
        public void Insert(TriangleIndices triangle)
        {
            if (Bounds.Contains(triangle.Bounds) == ContainmentType.Disjoint)
                return;

            Triangles.Add(triangle);
        }
        public bool Insert(OctreeNode node)
        {
            if (Bounds.Contains(node.Bounds) == ContainmentType.Disjoint)
                return false;

            if (TreeDepth > Math.Max(1,MaxTreeDepth))
                return false;

            if (Children == null)
                Children = Subdivide(); 

            if (TreeDepth >= MaxTreeDepth-1)
            { 
                for (int i = 0; i < 8; i++)
                {
                    if (Vector3.Distance(Children[i].Bounds.Min,node.Bounds.Min) < BoundsTolerance &&
                        Vector3.Distance(Children[i].Bounds.Max, node.Bounds.Max) < BoundsTolerance)  
                    {
                        Children[i] = node;
                        return true;
                    }
                } 
            }
            else 
                foreach (var kid in Children)
                {
                    if (kid.Insert(node))
                        return true;
                }
            return false;
        }

        public void BuildChildren()
        { 
            if (TreeDepth < MaxTreeDepth && Triangles.Count() > 0)
            { 
                Children = Subdivide();

               for(int i = 0; i < Triangles.Count;i++)
                    foreach (var child in Children) 
                        child.Insert(Triangles[i]); 

                Triangles.Clear();
                foreach (var child in Children)
                    child.BuildChildren();
            }
        }
          
        public void ExtractNodes(List<OctreeNode> nodes)
        {
            nodes.Add(this);
            if (Children != null)
                foreach (var kid in Children)
                {
                    kid.ExtractNodes(nodes);
                }
        }
        public override string ToString()
        {
            return Bounds.ToString();
        }
    }

}
