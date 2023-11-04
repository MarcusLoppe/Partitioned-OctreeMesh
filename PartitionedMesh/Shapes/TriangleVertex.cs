using PartitionedMesh.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Shapes
{
    public class TriangleVertex
    {
        public Vector3 V0;
        public Vector3 V1;
        public Vector3 V2; 
        public TriangleVertex(Vector3 v0, Vector3 v1, Vector3 v2)
        {
            V0 = v0;
            V1 = v1;
            V2 = v2; 
        } 
    }
}
