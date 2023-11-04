using PartitionedMesh.Common;
using PartitionedMesh.MeshCodec;
using PartitionedMesh.Shapes;
using System.Numerics;

namespace PartitionedMesh.Structures
{
    public class OctreeMesh
    {
        public float CellSize = 25;
        public OctreeNode RootNode { get; set; } 
        public Mesh Mesh { get; set; }  
        public OctreeMesh(Mesh mesh, float cellSize)
        {
            Mesh = mesh;
            CellSize = cellSize;
            RootNode = CreateTree(Mesh);
        }
        public OctreeMesh(InputStream stream, Mesh mesh)
        {
            var bounds = new BoundingBox3(stream.ReadVector(), stream.ReadVector());
            var treeDepth = stream.Reader.ReadInt32();
            CellSize = stream.Reader.ReadSingle();

            Mesh = mesh;

            var divs = new int[] { (int)(bounds.Size.X / CellSize), (int)(bounds.Size.Y / CellSize), (int)(bounds.Size.Z / CellSize) };
            var rootGrid = new Grid(bounds.Min, bounds.Max, divs);

            RootNode = new OctreeNode(bounds, rootGrid, treeDepth);
            RootNode.Children = RootNode.Subdivide();

            var nodesCount = stream.Reader.ReadInt32();
            bool compressed = stream.Reader.ReadBoolean();
             
            var zeroIndexByteSize = stream.Reader.ReadInt32();
            var restByteSize = stream.Reader.ReadInt32();
             
            byte[] rleEncodedData;
            byte[] rleEncodedRestData;

            if (compressed)
            { 
                rleEncodedData = stream.readCompressedData(zeroIndexByteSize); 
                rleEncodedRestData = stream.readCompressedData(restByteSize);
            }
            else
            {
                rleEncodedData = stream.Reader.ReadBytes(zeroIndexByteSize);
                rleEncodedRestData = stream.Reader.ReadBytes(restByteSize);
            }
            var gridZeroIndexRaw = MeshDecoder.RLEDecode(rleEncodedData);
            var gridRest = MeshDecoder.RLEDecode(rleEncodedRestData); 

            var gridZeroIndex = MeshDecoder.restoreVDelta(gridZeroIndexRaw);

             

            Dictionary<Vector3, List<int>> restoredGridIndices = new();
            int currentIdx = 0;
            for (int i = 0; i < nodesCount; i++)
            {
                var cellIdx = stream.Reader.Read7BitEncodedInt();
                var min = rootGrid.gridIdxToVector3(cellIdx);
                var cellIndicesCount = stream.Reader.ReadByte();

                if (!restoredGridIndices.ContainsKey(min))
                    restoredGridIndices.Add(min, new());

                List<int> chunk = new() { gridZeroIndex[i] };
                if(cellIndicesCount > 0)
                    chunk.AddRange(gridRest.GetRange(currentIdx, cellIndicesCount - 1));

                var chunkRestored = MeshDecoder.restoreVDelta(chunk);
                restoredGridIndices[min].AddRange(chunkRestored);
                currentIdx += cellIndicesCount-1; 
            }

              
            List<OctreeNode> nodes = new(); 
            foreach (var nodeCell in restoredGridIndices)
            {
                var min = nodeCell.Key; 
                var max = min + new Vector3(CellSize, CellSize, CellSize); 
                var node = new OctreeNode(new BoundingBox3(min, max), rootGrid, treeDepth);

                node.Triangles = nodeCell.Value.Select(a => Mesh.Triangles[a]).ToList(); 
                nodes.Add(node);
            }

            foreach (var node in nodes)
                RootNode.Insert(node);
        } 

        public void SaveSpatialPartitioning(OutputStream outputStream, bool useCompression = true, int[] LUTTable = null)
        {
            var rootNode = RootNode ?? CreateTree(Mesh);

            Dictionary<TriangleIndices, int> TriangleIndexDict = Mesh.GetTriangleDict();

            outputStream.WriteVector(rootNode.Bounds.Min);
            outputStream.WriteVector(rootNode.Bounds.Max);
            outputStream.Writer.Write(rootNode.MaxTreeDepth);
            outputStream.Writer.Write(CellSize);

            List<OctreeNode> nodes = new();
            rootNode.ExtractNodes(nodes);

            var divs = new int[] { (int)(rootNode.Bounds.Size.X / CellSize), (int)(rootNode.Bounds.Size.Y / CellSize), (int)(rootNode.Bounds.Size.Z / CellSize) };
            var grid = new Grid(rootNode.Bounds.Min, rootNode.Bounds.Max, divs);

            var pairedNodes = nodes.Where(a => a.Triangles.Count > 0).Select(a => new GridCell(grid.pointToGridIdx(a.Bounds.Min), a)).ToList(); 
              
            foreach (var node in pairedNodes)
            { 
                List<int> cellIndices = new();
                for (int i = 0; i < node.Cell.Triangles.Count; ++i)
                {
                    if (LUTTable != null)
                    {
                        var reIndexTriangle = new TriangleIndices(LUTTable[node.Cell.Triangles[i].V0], LUTTable[node.Cell.Triangles[i].V1], LUTTable[node.Cell.Triangles[i].V2]); 
                        cellIndices.Add(TriangleIndexDict[reIndexTriangle]); 
                    }
                    else 
                        cellIndices.Add(TriangleIndexDict[node.Cell.Triangles[i]]);   
                }
                cellIndices = cellIndices.OrderBy(a => a).ToList();
                  
                int[] encodedIndices = new int[cellIndices.Count];
                encodedIndices[0] = cellIndices[0];
                for (int i = 1; i < cellIndices.Count; i++)
                    encodedIndices[i] = cellIndices[i] - cellIndices[i - 1];
                  
                node.GridIndices = cellIndices.ToArray();
                node.EncodeGridIndices(); 
            }
             
            var nodeGridIndices = pairedNodes.SelectMany(a => a.EncodedGridIndices).OrderBy(a => a.Indices[0]).ToList(); 

            int[] vdeltas = MeshEncoder.vDelta(nodeGridIndices.Select(a => a.Indices[0]).ToList());
            var restGridItems = nodeGridIndices.SelectMany(a => a.Indices.Skip(1)).ToList();
             
            var zeroIndex = MeshEncoder.RLEEncodeByte(vdeltas.ToList());
            var rest = MeshEncoder.RLEEncodeByte(restGridItems.ToList()); 

            if (useCompression && vdeltas.Length < 5 && restGridItems.Count < 5)
                useCompression = false;

            outputStream.Writer.Write(nodeGridIndices.Count);
            outputStream.Writer.Write(useCompression);
             

            outputStream.Writer.Write(zeroIndex.Length);
            outputStream.Writer.Write(rest.Length);

             
            if (useCompression)
            {
                outputStream.writeCompressedData(zeroIndex);
                outputStream.writeCompressedData(rest);
            }
            else
            {
                outputStream.Writer.Write(zeroIndex);
                outputStream.Writer.Write(rest);
            } 

            foreach (var node in nodeGridIndices)
            {
                outputStream.Writer.Write7BitEncodedInt(node.Index);
                outputStream.Writer.Write((byte)node.Indices.Length);
            }
             
        }

        public List<OctreeNode> GetAllPopulatedNodes()
        {
            List<OctreeNode> nodes = new();
            RootNode.ExtractNodes(nodes);
            return nodes.Where(a => a.Triangles.Count > 0).ToList();
        }

        public OctreeNode CreateTree(Mesh Mesh)
        {  
            var bounds = new BoundingBox3(Mesh.Grid.Bounds.Min, Mesh.Grid.Bounds.Max);
            var maxDemision = OctreeHelper.GetOctreeSquare(bounds, CellSize);
 
            while (maxDemision.Item1 == 0)
            {
                CellSize = maxDemision.Item2 / 2;
                maxDemision = OctreeHelper.GetOctreeSquare(bounds, CellSize);
            }

            bounds.Min.X = (float) Math.Floor(bounds.Min.X);
            bounds.Min.Y = (float) Math.Floor(bounds.Min.Y);
            bounds.Min.Z = (float) Math.Floor(bounds.Min.Z);
            bounds.Max = bounds.Min + new Vector3(maxDemision.Item2, maxDemision.Item2, maxDemision.Item2);

            var divs = new int[] { (int)(bounds.Size.X / CellSize), (int)(bounds.Size.Y / CellSize), (int)(bounds.Size.Z / CellSize) };
            var grid = new Grid(bounds.Min, bounds.Max, divs);
             
            var rootNode = new OctreeNode(bounds, grid, maxDemision.Item1);
            rootNode.Children = rootNode.Subdivide(); 

            foreach (var triangle in Mesh.Triangles)
            {
                triangle.CreateBounds(Mesh.Vertices);
                rootNode.Insert(triangle);
            }
            rootNode.BuildChildren();
            return rootNode;
        } 

        public bool Intersects(Ray ray, out TriangleVertex triangle)
        {
            triangle = null;
            if (!Mesh.Grid.Bounds.IntersectRay(ray))
                return false;
             
            return CheckIntersection(RootNode, ray, out triangle);
        } 

        private bool CheckIntersection(OctreeNode node, Ray ray, out TriangleVertex triangle)
        {
            triangle = null;
            if (!node.Bounds.IntersectRay(ray))
                return false;

            if (node.Triangles.Count > 0)
            {
                for(int i = 0; i < node.Triangles.Count;i++)
                    if (Mesh.Intersects(ray, node.Triangles[i]))
                    {
                        triangle = new TriangleVertex(Mesh.Vertices[node.Triangles[i].V0], Mesh.Vertices[node.Triangles[i].V1], Mesh.Vertices[node.Triangles[i].V2]);
                        return true;
                    }
            }

            if (node.Children == null)
                return false;
            for (int i = 0; i < node.Children.Length; i++)
            {
                if (CheckIntersection(node.Children[i], ray, out triangle))
                    return true;
            } 
            return false;
        }
        
         
    }
} 
