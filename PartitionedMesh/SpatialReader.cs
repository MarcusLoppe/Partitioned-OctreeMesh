using PartitionedMesh.Common;
using PartitionedMesh.MeshCodec;
using PartitionedMesh.Shapes;
using PartitionedMesh.Structures;
using System.Numerics;

namespace PartitionedMesh
{
    public class SpatialReader
    {
        public InputStream InputStream { get; set; }
        public float VertexPrecision;
        public Mesh Mesh { get; set; }
        public SpatialReader(string path)
        {
            InputStream = new InputStream(path);
            Mesh = ReadMesh();
        }
        public SpatialReader(InputStream stream)
        {
            InputStream = stream;
            Mesh = ReadMesh();
        } 
        public void Close()
        {
            InputStream.Close();
        } 
        public OctreeMesh ReadOctreeMesh()
        {
            OctreeMesh tree = new OctreeMesh(InputStream, Mesh);
            return tree;
        }
        public Mesh ReadMesh()
        {
            VertexPrecision = InputStream.Reader.ReadSingle();
            var gridMin = new Vector3(InputStream.Reader.ReadSingle(), InputStream.Reader.ReadSingle(), InputStream.Reader.ReadSingle());
            var gridMax = new Vector3(InputStream.Reader.ReadSingle(), InputStream.Reader.ReadSingle(), InputStream.Reader.ReadSingle());
            var gridDivision = new int[3] { InputStream.Reader.ReadInt32(), InputStream.Reader.ReadInt32(), InputStream.Reader.ReadInt32() };
            Grid grid = new Grid(gridMin, gridMax, gridDivision);

            var vcount = InputStream.Reader.ReadInt32();
            int[] intVertices = InputStream.readPackedInts(vcount, 3, false);

            int[] gridIndices = InputStream.readPackedInts(vcount, 1, false);
            for (int i = 1; i < vcount; i++)
                gridIndices[i] += gridIndices[i - 1];

            var restored = MeshDecoder.restoreVertices(intVertices, gridIndices, grid, VertexPrecision);

            Vector3[] vertices = new Vector3[vcount];
            for (int i = 0; i < vcount; i++)
                vertices[i] = new (restored[i * 3], restored[i * 3 + 1], restored[i * 3 + 2]);

            var triCount = InputStream.Reader.ReadInt32();
            int[] indices = InputStream.readPackedInts(triCount, 3, false);
            MeshDecoder.restoreIndices(triCount, indices);

            TriangleIndices[] TriangleIndices = new TriangleIndices[triCount];
            for (int i = 0; i < triCount; i++)
                TriangleIndices[i] = new(indices[i*3], indices[i *3+ 1], indices[i*3 + 2]);

            Mesh = new Mesh(TriangleIndices.ToList(), vertices.ToList(), grid); 
            return Mesh;
        }

    }
}
