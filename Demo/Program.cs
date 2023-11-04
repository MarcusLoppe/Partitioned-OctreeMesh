using Demo.Util;
using PartitionedMesh.Common;
using PartitionedMesh.Shapes;
using PartitionedMesh;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Demo;
using System.Drawing;
using System.IO;
using System.Runtime.CompilerServices;
using PartitionedMesh.Structures;
using PartitionedMesh.Structures.BVH;
using System.Xml.Linq;

string GetProjectDirectory([CallerFilePath] string sourceFilePath = "") =>  Path.GetDirectoryName(sourceFilePath); 
 
var projectDir = GetProjectDirectory();
Console.WriteLine(projectDir);


string pathIn = projectDir + @"\Samples\DemoMesh.obj";
var octoMeshPathOut = projectDir + @"\Samples\PrationedMeshFile.bin";
var bvhMeshPathOut = projectDir + @"\Samples\PrationedBVHMeshFile.bin";
var objPath = projectDir + @"\Samples\";

ParseSave(pathIn, octoMeshPathOut, bvhMeshPathOut);
ExtractMesh(octoMeshPathOut, bvhMeshPathOut, objPath);


void ExtractMesh(string octoMeshPathOut, string bvhMeshPathOut, string pathOut)
{
    Stopwatch sw = Stopwatch.StartNew();
    SpatialReader reader = new(octoMeshPathOut);
    var tree = reader.ReadOctreeMesh();
    var loadTime = sw.ElapsedMilliseconds;

    Geometry geo = new();
    var vertices = tree.Mesh.Vertices;
    var nodes = tree.GetAllPopulatedNodes();
    Random rand = new Random();
    foreach (var node in nodes)
    {
        var color = Color.FromArgb(rand.Next(256), rand.Next(256), rand.Next(256));
        geo.AddOutlineOfBoundingBox3(node.Bounds, color);
        foreach (var tri in node.Triangles)
        {
            geo.AddTriangleVertex(new(vertices[tri.V0], vertices[tri.V1], vertices[tri.V2]), color);
        }
    }
    geo.ExportToPLY(pathOut+ "ReconstructedOctoMesh.ply");
    reader.Close();
    var totalConnections = nodes.Sum(a => a.Triangles.Count());
    var avgConnections = nodes.Average(a => a.Triangles.Count());
    var maxConnections = nodes.Max(a => a.Triangles.Count());
    var minConnections = nodes.Min(a => a.Triangles.Count());

    Console.WriteLine($"OctoMesh:\n" +
        $"Load time: {loadTime}ms\n" +
        $"OctoMeshNodes with triangles: {nodes.Count}\n" +
        $"Total connections: {totalConnections}\n" +
        $"Average connections: {avgConnections}\n" +
        $"Maximum connections: {maxConnections}\n" +
        $"Minimum connections: {minConnections}\n\n");

    sw.Restart();
    var bvhMesh = new BVHMesh(bvhMeshPathOut);
    var bvhLoadTime = sw.ElapsedMilliseconds;

    var allNodes = new List<BVHNode>();
    bvhMesh.RootNode.ExtractNodes(allNodes);
    allNodes = allNodes.Where(a => a.OctreeMesh.Mesh.Triangles.Count() > 0).ToList();
    geo = new();
    int i = 0;
    List<Color> colors = new List<Color>()
    {
        Color.Black, // #000000
        Color.White, // #FFFFFF
        Color.Red,   // #FF0000
        Color.Green, // #008000
        Color.Blue,  // #0000FF
        Color.Yellow, // #FFFF00
        Color.Purple, // #800080
        Color.Orange  // #FFA500
    };
    foreach (var node in allNodes)
    {
        var color = colors[i];
        i++;
        geo.AddOutlineOfBoundingBox3(node.Bounds, color);

        vertices = node.OctreeMesh.Mesh.Vertices;
        foreach (var tri in node.OctreeMesh.Mesh.Triangles) 
            geo.AddTriangleVertex(new(vertices[tri.V0], vertices[tri.V1], vertices[tri.V2]), color);
    }
    geo.ExportToPLY(pathOut + "ReconstructedBVHMesh.ply");


    totalConnections = allNodes.Sum(a => a.OctreeMesh.Mesh.Triangles.Count());
    avgConnections = allNodes.Average(a => a.OctreeMesh.Mesh.Triangles.Count());
    maxConnections = allNodes.Max(a => a.OctreeMesh.Mesh.Triangles.Count());
    minConnections = allNodes.Min(a => a.OctreeMesh.Mesh.Triangles.Count());

    Console.WriteLine($"" +
        $"BVHMesh:\n" +
        $"Load time: {bvhLoadTime}ms\n" +
        $"OctoMeshNodes with triangles: {allNodes.Count}\n" +
        $"Total connections: {totalConnections}\n" +
        $"Average connections: {avgConnections}\n" +
        $"Maximum connections: {maxConnections}\n" +
        $"Minimum connections: {minConnections}");
}

void ParseSave(string pathIn, string octoMeshPathOut, string bvhMeshPathOut)
{ 
    Console.WriteLine($"Proccesing {pathIn}"); 

    Stopwatch sw = Stopwatch.StartNew();
    var fileMesh = ObjToTriangles(pathIn);
    List<TriangleIndices> triangles = new(fileMesh.Item1);
    List<Vector3> vertices = new(fileMesh.Item2);

    var mesh = new Mesh(triangles, vertices); 
    float vertexPrecision = 1.0f / 8.0f;
    sw.Restart();

    SpatialCreator creator = new(octoMeshPathOut, vertexPrecision);
    creator.SetCellSize(25);
    creator.EncodeMesh(mesh);
    creator.Close();
    var createTime = sw.ElapsedMilliseconds;
 
    sw.Restart();
    SpatialReader reader = new(octoMeshPathOut);
    var tree = reader.ReadOctreeMesh();
    reader.Close();
    var loadTime = sw.ElapsedMilliseconds;

    fileMesh = ObjToTriangles(pathIn);
    triangles = new(fileMesh.Item1);
    vertices = new(fileMesh.Item2); 

    var bounds = BoundingBox3.CreateFromPoints(vertices).Split(); 
    List<OctreeMesh> volumes = new(); 
    foreach (var bound in bounds)
    {
        List<TriangleIndices> treeTriangles = new();
        Dictionary<Vector3,int> treeVertices = new();
        foreach(var tri in triangles)
        {
            if (bound.Contains(vertices[tri.V0], vertices[tri.V1], vertices[tri.V2]))
            {
                if (!treeVertices.ContainsKey(vertices[tri.V0]))
                    treeVertices.Add(vertices[tri.V0], treeVertices.Count);
                if (!treeVertices.ContainsKey(vertices[tri.V1]))
                    treeVertices.Add(vertices[tri.V1], treeVertices.Count);
                if (!treeVertices.ContainsKey(vertices[tri.V2]))
                    treeVertices.Add(vertices[tri.V2], treeVertices.Count);

                treeTriangles.Add(new(treeVertices[vertices[tri.V0]], treeVertices[vertices[tri.V1]], treeVertices[vertices[tri.V2]]));
            }
        }
        var mesh_vertices = treeVertices.Select(a => a.Key).ToList();
        mesh = new Mesh(treeTriangles, mesh_vertices);
        OctreeMesh treeMesh = new(mesh, 25);
        volumes.Add(treeMesh); 
    }  
      
    sw.Restart();
    var bvhMesh = new BVHMesh(vertexPrecision);
    bvhMesh.SaveTree(bvhMeshPathOut, volumes);
    var bvhCreateTime = sw.ElapsedMilliseconds;

    sw.Restart();
    bvhMesh = new BVHMesh(bvhMeshPathOut);
    var bvhLoadTime = sw.ElapsedMilliseconds;


    Console.WriteLine($"Create time: {createTime}ms\n" +
        $"Load time: {loadTime}ms\n" + 
        $"Triangles {triangles.Count} Vertex: {vertices.Count()}\n" +
        $"File size: {Math.Round(File.ReadAllBytes(octoMeshPathOut).Length / 1024.0f, 0)}kb\n\n" +
        $"BVH mesh:\n" +
        $"Create time: {bvhCreateTime}\n" +
        $"Load time: {bvhLoadTime}\n" +
        $"OctoMesh volumes: {volumes.Count}\n" +
        $"File size: {Math.Round(File.ReadAllBytes(bvhMeshPathOut).Length / 1024.0f, 0)}kb\n\n");

}
(List<TriangleIndices>, List<Vector3>) ObjToTriangles(string filename)
{ 
    List<TriangleIndices> triangles = new(300000);

    var objLines = File.ReadAllLines(filename);
    List<Vector3> vertices = new(); 
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
            vertices.Add(point); 
        }
        else if (line.Contains("f"))
        { 
            triangles.Add(new TriangleIndices(
                int.Parse(split[1].Split("//")[0]) - 1,
                int.Parse(split[2].Split("//")[0]) - 1, 
                int.Parse(split[3].Split("//")[0]) - 1));
        }
    }
    return (triangles, vertices);
}
