using PartitionedMesh.Common;
using PartitionedMesh.MeshCodec;
using PartitionedMesh.Shapes;
using PartitionedMesh.Structures;
using System.Numerics;

namespace PartitionedMesh
{
    public class SpatialCreator
    { 
        public float CellSize = -1;
        public OutputStream OutputStream { get; set; } 
        public SortableVertex[]? SortedVertices { get; set; }

        public float VertexPrecision = 1.0f / 1024.0f; 

        public OctreeMesh? TreeMesh { get; set; }
        public SpatialCreator(string path, float? vertexPrecision = null)
        {
            OutputStream = new OutputStream(path);
            VertexPrecision = vertexPrecision ?? VertexPrecision;
        }
        public SpatialCreator(OutputStream stream, float? vertexPrecision = null)
        {
            OutputStream = stream;
            VertexPrecision = vertexPrecision ?? VertexPrecision;
        }
         

        public void WriteHeader(byte[] bytes)
        {
            OutputStream.Writer.Write(bytes);
        }
        public void Close()
        {
            OutputStream.Save();
        }

        public void SetCellSize(float size) =>
            CellSize = size;
        public void SetCellSizeByDepth(BoundingBox3 bounds, int depth)
        {
            float size = Math.Max(Math.Max(bounds.Width, bounds.Height), bounds.Depth);
            for (int i = 0; i < depth; i++)
                size /= 2;
            CellSize = (float) Math.Max(Math.Round(size,0),2);
        }

        public void SaveOctree(OctreeMesh tree)
        { 
            EncodeMeshOnly(tree.Mesh);
            tree.SaveSpatialPartitioning(OutputStream);
        }
         
        public void EncodeMesh(Mesh mesh)
        {
            if (CellSize == -1)
                SetCellSizeByDepth(mesh.Grid.Bounds, 2);

            EncodeMeshOnly(mesh);
            OctreeMesh treeMesh = new(mesh, CellSize);
            treeMesh.SaveSpatialPartitioning(OutputStream); 
        }
 
        public void EncodeMeshOnly(Mesh mesh, bool replaceTriangles = false, bool reIndexVertices = false)
        {
            var Triangles = mesh.Triangles;
            List<float> vertices = new();
            foreach (var vert in mesh.Vertices)
            {
                vertices.Add(vert.X);
                vertices.Add(vert.Y);
                vertices.Add(vert.Z);
            }

            Grid grid = new Grid(vertices.ToArray(), VertexPrecision);
         
            OutputStream.Writer.Write(VertexPrecision);
            OutputStream.WriteVector(grid.Bounds.Min);
            OutputStream.WriteVector(grid.Bounds.Max);

            OutputStream.Writer.Write(grid.Division[0]);
            OutputStream.Writer.Write(grid.Division[1]);
            OutputStream.Writer.Write(grid.Division[2]);

           SortedVertices = MeshEncoder.sortVertices(grid, vertices.ToArray());
            int[] vdeltas = MeshEncoder.makeVertexDeltas(vertices.ToArray(), SortedVertices, grid, VertexPrecision);

            var vertexCount = vertices.Count() / 3;
            int[] gridIndicies = new int[vertexCount];

            gridIndicies[0] = SortedVertices[0].gridIndex;
            for (int i = 1; i < vertexCount; ++i)
                gridIndicies[i] = SortedVertices[i].gridIndex - SortedVertices[i - 1].gridIndex;

            OutputStream.Writer.Write(mesh.Vertices.Count());

            OutputStream.writePackedInts(vdeltas, vertexCount, 3, false);
            OutputStream.writePackedInts(gridIndicies, vertexCount, 1, false);

            var triangleCount = Triangles.Count();
            OutputStream.Writer.Write(triangleCount);

            int[] indices = MeshEncoder.reIndexIndices(SortedVertices, Triangles.SelectMany(a => new int[] { a.V0, a.V1, a.V2 }).ToArray());
             
            MeshEncoder.rearrangeTriangles(indices);
            var reArranged = indices.ToArray();

            MeshEncoder.makeIndexDeltas(indices);
            OutputStream.writePackedInts(indices, Triangles.Count(), 3, false);
             
            for (int i = 1; i < vertexCount; i++)
                gridIndicies[i] += gridIndicies[i - 1];

            if(reIndexVertices)
            {
                mesh.Vertices = MeshEncoder.reIndexVertices(SortedVertices, mesh.Vertices);
            }
            else
            { 
                var restored = MeshDecoder.restoreVertices(vdeltas, gridIndicies, grid, VertexPrecision);
                List<Vector3> newVerts = new();
                for (int i = 0; i < vertexCount; i += 1)
                    newVerts.Add(new(restored[i * 3], restored[i * 3 + 1], restored[i * 3 + 2])); 
                mesh.Vertices = newVerts;
            }
             
            mesh.TriangleDict = null; 
            if (replaceTriangles)
            {
                mesh.Triangles = new(); 
                for (int i = 0; i < triangleCount; i++)
                    mesh.Triangles.Add(new TriangleIndices(reArranged[i * 3], reArranged[i * 3 + 1], reArranged[i * 3 + 2]));
            }
            else
            { 
                List<TriangleIndices> newTriangles = new();
                for (int i = 0; i < triangleCount; i++)
                    newTriangles.Add(new TriangleIndices(reArranged[i * 3], reArranged[i * 3 + 1], reArranged[i * 3 + 2]));
                 
                MeshEncoder.reIndexTrianglesVertices(SortedVertices, mesh.Triangles);  
                MeshEncoder.rearrangeTriangles(mesh.Triangles);  
            }
        } 
    }
}
