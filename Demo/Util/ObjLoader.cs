using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Demo.Util
{
    internal class ObjLoader
    {
        public static List<TriangleVertex> ObjToTriangles(string filename)
        {
            List<Vector3> verts = new();
            List<TriangleVertex> triangles = new(300000);

            var objLines = File.ReadAllLines(filename);
            Dictionary<int, Vector3> vertices = new();
            int idx = 0;
            foreach (var line in objLines)
            {
                if (line.Contains("#") || line.Contains("vn"))
                    continue;

                var split = line.Replace('.', ',').Split(' ');
                if (line.Contains("v"))
                {
                    float x = float.Parse(split[1]);
                    float y = float.Parse(split[2]);
                    float z = float.Parse(split[3]);
                    Vector3 point = new(x, y, z);
                    vertices.Add(idx, point);
                    idx++;
                }
                else if (line.Contains("f"))
                {
                    List<Vector3> points = new()
                    {
                        vertices[int.Parse(split[1].Split("//")[0])-1],
                        vertices[int.Parse(split[2].Split("//")[0])-1],
                        vertices[int.Parse(split[3].Split("//")[0])-1]
                    };

                    triangles.Add(new TriangleVertex(points[0], points[1], points[2]));
                }
            }
            return triangles;
        }

    }
}
