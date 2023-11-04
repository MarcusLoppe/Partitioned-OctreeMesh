using PartitionedMesh.Common;
using PartitionedMesh.MeshCodec;
using PartitionedMesh.Shapes;
using PartitionedMesh.Structures.BVH;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Structures
{
    public class BVHMesh
    {
        public BVHNode? RootNode { get; set; }

        public float VertexPrecision = 1.0f / 1024.0f;
        public BVHMesh(float vertexPrecision)
        {
            VertexPrecision = vertexPrecision; 
        } 
        public BVHMesh(string path)  
        {
            InputStream stream = new InputStream(path);
            var magiChars = stream.Reader.ReadChars(3);
            var magic = new string(magiChars);
            if(magic != "BVH")
                return;

            var volCount = stream.Reader.ReadInt32();
            List<OctreeMesh> volumes = new();
            SpatialReader treeReader = new(stream);

            VertexPrecision = treeReader.VertexPrecision;
            var Mesh = treeReader.Mesh;

            for (int i = 0; i < volCount; i++)
            {
                OctreeMesh tree = new OctreeMesh(stream, Mesh);
                var populatedNodes = tree.GetAllPopulatedNodes();
                var usedTriangles = populatedNodes.SelectMany(a => a.Triangles).ToList();
                var usedVertices = usedTriangles.SelectMany(a => a.IndiceArray).Distinct().ToList();

                List<TriangleIndices> triangles = new();
                List<Vector3> vertices = new();

                int[] LUTTable = new int[Mesh.Vertices.Count];
                for (int v = 0; v < usedVertices.Count; v++) {
                    LUTTable[usedVertices[v]] = v;
                    vertices.Add(new Vector3(Mesh.Vertices[usedVertices[v]].X, Mesh.Vertices[usedVertices[v]].Y, Mesh.Vertices[usedVertices[v]].Z));
                }

                foreach(var node in populatedNodes) 
                    for(int t = 0; t < node.Triangles.Count; t++)
                    {
                        var tri = new TriangleIndices(LUTTable[node.Triangles[t].V0], LUTTable[node.Triangles[t].V1], LUTTable[node.Triangles[t].V2]);
                        triangles.Add(tri);
                        node.Triangles[t] = tri;
                    }
                
                Mesh treeMesh = new Mesh(triangles, vertices);
                tree.Mesh = treeMesh;
                volumes.Add(tree);
            }

            List<BVHNodeStruct> nodesRaw = new(); 
            for (int i = 0; i < volCount; i++)
            {
                var idx = stream.Reader.ReadInt32();
                var bounds = new BoundingBox3(stream.ReadVector(), stream.ReadVector());
                var leftIdx = stream.Reader.ReadInt32();
                var rightIdx = stream.Reader.ReadInt32();
                var node = new BVHNodeStruct()
                {
                    NodeIdx = idx,
                    OctreeMesh = volumes[idx],
                    Bounds = bounds,
                    LeftChildIdx = leftIdx,
                    RightChild = rightIdx
                };
                nodesRaw.Add(node);
            } 

            RootNode = BVHBuilder.RecursiveReconstruct(nodesRaw[0], nodesRaw); 
            stream.Close(); 
        }

        public void SaveTree(string path, List<OctreeMesh>? volumes = null)
        {
            if (volumes != null)
                RootNode = CreateRootNode(volumes);

            if (RootNode == null)
                return;

            OutputStream stream = new(path);
            stream.Writer.Write("BVH".ToCharArray());
            var allNodes = new List<BVHNode>();
            RootNode.ExtractNodes(allNodes);

            stream.Writer.Write(allNodes.Count());

            SpatialCreator creator = new SpatialCreator(stream, VertexPrecision);
            var combinedMesh = new Mesh(allNodes.Select(a => a.OctreeMesh.Mesh).ToList(), out List<int[]> remappedMeshVertices);

            creator.EncodeMeshOnly(combinedMesh, true, true); 

            int[] indexLUT = new int[creator.SortedVertices.Length];
            for (int i = 0; i < creator.SortedVertices.Length; ++i)
                indexLUT[creator.SortedVertices[i].originalIndex] = i;

            for (int b = 0; b < remappedMeshVertices.Count; b++)
                for (int c = 0; c < remappedMeshVertices[b].Length; c++)
                    remappedMeshVertices[b][c] = indexLUT[remappedMeshVertices[b][c]]; 


            for (int i = 0; i < allNodes.Count(); i++)
            {
                var node = allNodes[i];
                var oldMesh = node.OctreeMesh.Mesh; 
                node.OctreeMesh.Mesh = combinedMesh;
                node.OctreeMesh.SaveSpatialPartitioning(stream, false, remappedMeshVertices[i]);
                node.OctreeMesh.Mesh = oldMesh;
            } 

            for (int i = 0; i < allNodes.Count(); i++)
            {
                var node = allNodes[i];
                stream.Writer.Write(i);
                stream.WriteVector(node.Bounds.Min);
                stream.WriteVector(node.Bounds.Max);

                if (node.LeftChild != null)
                    stream.Writer.Write(allNodes.IndexOf(node.LeftChild));
                else
                    stream.Writer.Write(-1);

                if (node.RightChild != null)
                    stream.Writer.Write(allNodes.IndexOf(node.RightChild));
                else
                    stream.Writer.Write(-1);
            }

            stream.Save();
        }

        public BVHNode CreateRootNode(List<OctreeMesh> volumes)
        {
            var nodes = volumes.Select(a => new BVHNode(a, a.Mesh.Grid.Bounds)).ToList();
            BVHBuilder builder = new BVHBuilder(nodes);
            return builder.BuildBVH();
        }

        public bool Intersects(Ray ray, out TriangleVertex triangle)
        {
            triangle = null; 
            if (RootNode == null || !RootNode.Bounds.IntersectRay(ray))
                return false;

            return CheckIntersection(RootNode, ray, out triangle);
        }
        private bool CheckIntersection(BVHNode node, Ray ray, out TriangleVertex triangle)
        {
            triangle = null;

            if (!node.Bounds.IntersectRay(ray))
                return false;

            if (node.OctreeMesh.Intersects(ray, out triangle))
                return true;

            if (node.LeftChild != null)
                if (CheckIntersection(node.LeftChild, ray, out triangle))
                    return true;

            if (node.RightChild != null)
                if (CheckIntersection(node.RightChild, ray, out triangle))
                    return true;

            return false;
        }

    }
}
