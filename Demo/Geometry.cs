using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Drawing;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace Demo
{
     
    public class Geometry
    {
        public List<Vector3> Vertices { get; set; } = new List<Vector3>(500000);
        public List<TriangleIndices> Triangles { get; set; } = new();
        public List<Color> Colors { get; set; } = new();
        public List<bool> Transparent { get; set; } = new();
        public bool Transform { get; set; } = true;

        public Geometry()
        {
        }
        public List<TriangleVertex> GetTrianglesForLine(Vector3 start, Vector3 end, float diameter)
        {
            List<TriangleVertex> triangles = new();

            Vector3 direction = start - end;

            // Check if the direction is close to vertical (along Z axis)
            Vector3 fixedVector;
            if (Math.Abs(direction.Z) > Math.Sqrt(direction.X * direction.X + direction.Y * direction.Y))
            {
                fixedVector = new Vector3(1, 0, 0); // Use X axis as the fixed vector for vertical lines
            }
            else
            {
                fixedVector = new Vector3(0, 0, 1); // Otherwise use Z axis as the fixed vector
            }

            Vector3 normal = Vector3.Normalize(Vector3.Cross(direction, fixedVector));

            // The half of the diameter
            float radius = diameter / 2f;

            // The four points that form the base of the cylinder
            Vector3 p0 = start + normal * radius;
            Vector3 p1 = start - normal * radius;
            Vector3 p2 = end + normal * radius;
            Vector3 p3 = end - normal * radius;

            // Add the first two triangles that form the first cap of the cylinder
            triangles.Add(new TriangleVertex(start, p0, p1));
            triangles.Add(new TriangleVertex(start, p1, p0));

            // Add the four triangles that form the sides of the cylinder
            triangles.Add(new TriangleVertex(p0, p2, p1));
            triangles.Add(new TriangleVertex(p1, p2, p0));
            triangles.Add(new TriangleVertex(p1, p2, p3));
            triangles.Add(new TriangleVertex(p3, p2, p1));

            // Add the last two triangles that form the second cap of the cylinder
            triangles.Add(new TriangleVertex(end, p2, p3));
            triangles.Add(new TriangleVertex(end, p3, p2));

            return triangles;
        }


        public void CreateLine(Vector3 start, Vector3 end, float diameter, Color color)
        {
            List<TriangleVertex> triangles = GetTrianglesForLine(start, end, diameter);

            foreach (var tri in triangles)
            {
                Colors.Add(color);
                AddTriangleVertex(tri);
            }
        }
        public List<TriangleVertex> GetTrianglesForCircularLine(Vector3 start, Vector3 end, float diameter)
        {
            List<TriangleVertex> triangles = new();

            Vector3 direction = end - start;
            Vector3 normal = Vector3.Normalize(direction);
            Vector3 up = Vector3.UnitZ;

            Vector3 side = Vector3.Cross(normal, up);
            if (side == Vector3.Zero) // Check for collinearity
                side = Vector3.Cross(normal, Vector3.UnitX);

            side = Vector3.Normalize(side);
            Vector3 left = Vector3.Cross(normal, side);

            float halfDiameter = diameter * 0.5f; // Diameter of 1, so radius (half-diameter) is 0.5
            float halfHeight = halfDiameter * (float)Math.Sqrt(2); // Half height of the hexoctogon

            int sides = 4; // Hexoctogon (8 sides)

            for (int i = 0; i < sides; i++)
            {
                float angle = i * (2 * (float)Math.PI) / sides;
                float nextAngle = (i + 1) * (2 * (float)Math.PI) / sides;

                Vector3 vertex1 = start + side * (halfDiameter * (float)Math.Cos(angle)) + left * (halfHeight * (float)Math.Sin(angle));
                Vector3 vertex2 = start + side * (halfDiameter * (float)Math.Cos(nextAngle)) + left * (halfHeight * (float)Math.Sin(nextAngle));
                Vector3 vertex3 = end + side * (halfDiameter * (float)Math.Cos(angle)) + left * (halfHeight * (float)Math.Sin(angle));
                Vector3 vertex4 = end + side * (halfDiameter * (float)Math.Cos(nextAngle)) + left * (halfHeight * (float)Math.Sin(nextAngle));

                triangles.Add(new TriangleVertex(vertex1, vertex2, vertex3));
                triangles.Add(new TriangleVertex(vertex2, vertex3, vertex4));
            }

            return triangles;
        }

        public void AddTriangleVertex(TriangleVertex vert, Color? color = null)
        {
            color = color ?? Color.Beige;
            var vertOffset = Vertices.Count();
            Vertices.Add(vert.V0);
            Vertices.Add(vert.V1);
            Vertices.Add(vert.V2);
            Colors.Add((Color)color);
            Transparent.Add(false);
            Triangles.Add(new(vertOffset, vertOffset + 1, vertOffset + 2));

        } 
        public void AddOutlineOfBoundingBox3(BoundingBox3 box, Color type, bool transparent = false)
        { 
            Vector3[] vertices = new Vector3[8];
             
            vertices[0] = new Vector3(box.Min.X, box.Min.Y, box.Min.Z);
            vertices[1] = new Vector3(box.Max.X, box.Min.Y, box.Min.Z);
            vertices[2] = new Vector3(box.Max.X, box.Max.Y, box.Min.Z);
            vertices[3] = new Vector3(box.Min.X, box.Max.Y, box.Min.Z);
            vertices[4] = new Vector3(box.Min.X, box.Min.Y, box.Max.Z);
            vertices[5] = new Vector3(box.Max.X, box.Min.Y, box.Max.Z);
            vertices[6] = new Vector3(box.Max.X, box.Max.Y, box.Max.Z);
            vertices[7] = new Vector3(box.Min.X, box.Max.Y, box.Max.Z);

            // Front face
            List<TriangleIndices> triangles = new();
            Vector3[,] edges = new Vector3[12, 2]
              {
                    { vertices[0], vertices[1] },  // Bottom front
                    { vertices[1], vertices[2] },  // Right front
                    { vertices[2], vertices[3] },  // Top front
                    { vertices[3], vertices[0] },  // Left front

                    { vertices[4], vertices[5] },  // Bottom back
                    { vertices[5], vertices[6] },  // Right back
                    { vertices[6], vertices[7] },  // Top back
                    { vertices[7], vertices[4] },  // Left back

                    { vertices[0], vertices[4] },  // Bottom left
                    { vertices[1], vertices[5] },  // Bottom right
                    { vertices[2], vertices[6] },  // Top right
                    { vertices[3], vertices[7] }   // Top left
              }; 
            for (int i = 0; i < edges.GetLength(0); i++)
            {
                var tris = GetTrianglesForCircularLine(edges[i, 0], edges[i, 1], 0.5f);
                foreach (var tri in tris)
                {
                    int vertOffset = Vertices.Count();
                    Vertices.Add(tri.V0);
                    Vertices.Add(tri.V1);
                    Vertices.Add(tri.V2);

                    triangles.Add(new(vertOffset, vertOffset + 1, vertOffset + 2)); 
                }
            }

            foreach (var tri in triangles)
            {
                Transparent.Add(transparent);
                Colors.Add(type);
                Triangles.Add(tri);
            }
            foreach (var vert in vertices)
            {
                Vertices.Add(vert);
            }
        }
         
        public void ExportToPLY(string fileName)
        {
            using (StreamWriter writer = new StreamWriter(fileName))
            {

                writer.WriteLine("ply");
                writer.WriteLine("format ascii 1.0");
                writer.WriteLine("element vertex " + Triangles.Count * 3);
                writer.WriteLine("property float x");
                writer.WriteLine("property float y");
                writer.WriteLine("property float z");
                writer.WriteLine("property uchar red");
                writer.WriteLine("property uchar green");
                writer.WriteLine("property uchar blue");
                writer.WriteLine("property uchar alpha");
                writer.WriteLine("element face " + Triangles.Count);
                writer.WriteLine("property list uchar int vertex_indices");
                writer.WriteLine("end_header");
                for (int i = 0; i < Triangles.Count(); i++)
                {
                    Color color = Colors[i];
                    var triangle = Triangles[i];
                    byte alpha = Transparent[i] ? (byte)1 : (byte)255;
                    writer.WriteLine(Vertices[(int)triangle.V0].X.ToString(CultureInfo.InvariantCulture) + " " +
                                       Vertices[(int)triangle.V0].Y.ToString(CultureInfo.InvariantCulture) + " " +
                                       Vertices[(int)triangle.V0].Z.ToString(CultureInfo.InvariantCulture) + " " +
                                        color.R + " " +
                                        color.G + " " +
                                        color.B + " " +
                                        alpha);
                    writer.WriteLine(Vertices[(int)triangle.V1].X.ToString(CultureInfo.InvariantCulture) + " " +
                                       Vertices[(int)triangle.V1].Y.ToString(CultureInfo.InvariantCulture) + " " +
                                       Vertices[(int)triangle.V1].Z.ToString(CultureInfo.InvariantCulture) + " " +
                                        color.R + " " +
                                        color.G + " " +
                                        color.B + " " +
                                        alpha);
                    writer.WriteLine(Vertices[(int)triangle.V2].X.ToString(CultureInfo.InvariantCulture) + " " +
                                       Vertices[(int)triangle.V2].Y.ToString(CultureInfo.InvariantCulture) + " " +
                                       Vertices[(int)triangle.V2].Z.ToString(CultureInfo.InvariantCulture) + " " +
                                        color.R + " " +
                                        color.G + " " +
                                        color.B + " " +
                                        alpha);

                }
                for (int i = 0; i < Triangles.Count; i++)
                {
                    int vertexIndex = i * 3;
                    writer.WriteLine($"3 {vertexIndex} {vertexIndex + 1} {vertexIndex + 2}");
                }

            }
        } 
    }
}
