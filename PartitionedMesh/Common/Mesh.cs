using PartitionedMesh.MeshCodec;
using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Common
{
    public class Mesh
    {
        public List<TriangleIndices> Triangles { get; set; }
        public List<Vector3> Vertices { get; set; } 
        public Dictionary<TriangleIndices, int>? TriangleDict { get; set; }
        public Grid Grid { get; set; }


        public Mesh(int[] trianglesIndices, float[] vertices)
        {
            Triangles = new();
            Vertices = new();

            for (int i = 0; i < trianglesIndices.Length; i += 3)
                Triangles.Add(new(trianglesIndices[i], trianglesIndices[i + 1], trianglesIndices[i + 2]));

            for (int i = 0; i < vertices.Length; i += 3)
                Vertices.Add(new(vertices[i], vertices[i + 1], vertices[i + 2]));

             
            Grid = MeshEncoder.setupGrid(Vertices);
            Triangles = new HashSet<TriangleIndices>(Triangles).ToList();
        }

        public Mesh(List<TriangleIndices> triangles, List<Vector3> vertices)
        {
            Vertices = vertices;  
            Grid = MeshEncoder.setupGrid(vertices);
            Triangles = new HashSet<TriangleIndices>(triangles).ToList(); 
        }
        public Mesh(List<TriangleIndices> triangles, List<Vector3> vertices, Grid grid)
        {
            Triangles = new HashSet<TriangleIndices>(triangles).ToList();
            Vertices = vertices;
            Grid = grid;
        }
         
        public Mesh(List<Mesh> meshes, out List<int[]> RemappedMeshVertices)
        {
            Dictionary<TriangleIndices, int> triangleDict = new();
            Dictionary<Vector3, int> verticeDict = new();
            RemappedMeshVertices = new(); 
            foreach (var mesh in meshes)
            {
                Dictionary<TriangleIndices, TriangleIndices> reindexedTris = new();
                List<int> lut = new();
                List<int> lutTris = new();
                for (int i = 0; i < mesh.Vertices.Count(); i++)
                {
                    var vert = mesh.Vertices[i];
                    if (!verticeDict.ContainsKey(vert)) 
                        verticeDict.Add(vert, verticeDict.Count()); 
                    
                    lut.Add(verticeDict[vert]);
                }
                RemappedMeshVertices.Add(lut.ToArray());
                for (int i = 0; i < mesh.Triangles.Count(); i++)
                {
                    var tri = mesh.Triangles[i];
                    var v0 = verticeDict[mesh.Vertices[tri.V0]];
                    var v1 = verticeDict[mesh.Vertices[tri.V1]];
                    var v2 = verticeDict[mesh.Vertices[tri.V2]];
                    var triangle = new TriangleIndices(v0, v1, v2);
                    if (!triangleDict.ContainsKey(triangle))
                        triangleDict.Add(triangle, triangleDict.Count());

                    lutTris.Add(triangleDict[triangle]);
                    reindexedTris.Add(tri, triangle);
                } 
            } 

            Triangles = triangleDict.Select(a => a.Key).ToList();
            Vertices = verticeDict.Select(a => a.Key).ToList();
            Grid = MeshEncoder.setupGrid(Vertices);
        }

        public List<TriangleVertex> GetAllVertexs()
        {
            List<TriangleVertex> tris = new();
            foreach (var tri in Triangles)
                tris.Add(new TriangleVertex(Vertices[tri.V0], Vertices[tri.V1], Vertices[tri.V2]));
            return tris;
        }

        public Dictionary<TriangleIndices, int> GetTriangleDict()
        {
            if (TriangleDict == null)
            {
                TriangleDict = new();
                for (int i = 0; i < Triangles.Count; i++)
                    if (!TriangleDict.ContainsKey(Triangles[i]))
                        TriangleDict.Add(Triangles[i], i);
                
            }
            return TriangleDict;
        }
        public bool Intersects(Ray ray, TriangleIndices triangle)
        { 
            float Epsilon = 1e-6f;
            var v0 = Vertices[triangle.V0];
            var v1 = Vertices[triangle.V1];
            var v2 = Vertices[triangle.V2];

            Vector3 h, s, q;
            float a, f, u, v, t;

            Vector3 e1 = v1 - v0;
            Vector3 e2 = v2 - v0;

            h = Vector3.Cross(ray.Direction, e2);
            a = Vector3.Dot(e1, h);

            if (a > -Epsilon && a < Epsilon)
            {
                t = 0;
                return false;
            }

            f = 1.0f / a;
            s = ray.Origin - v0;
            u = f * Vector3.Dot(s, h);

            if (u < 0.0f || u > 1.0f)
            {
                t = 0;
                return false;
            }

            q = Vector3.Cross(s, e1);
            v = f * Vector3.Dot(ray.Direction, q);

            if (v < 0.0f || u + v > 1.0f)
            {
                t = 0;
                return false;
            }

            t = f * Vector3.Dot(e2, q);


            return t >= 0 && t <= ray.Distance;
        } 
    }
}
