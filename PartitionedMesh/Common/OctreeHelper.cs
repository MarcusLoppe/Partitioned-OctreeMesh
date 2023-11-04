using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Common
{
    public class OctreeHelper
    {
        public static (int, float) GetOctreeSquare(BoundingBox3 bounds, float cellSize)
        {
            return new List<(int, float)>()
            {
                RequiredMax(bounds.Width,cellSize),
                RequiredMax(bounds.Height,cellSize),
                RequiredMax(bounds.Depth,cellSize)
            }
            .OrderByDescending(a => a.Item2).FirstOrDefault();
        }
        public static (int, float) RequiredMax(float size, float CellSize)
        {
            float inc = CellSize;
            int times = 0;
            while (inc < size)
            {
                inc *= 2;
                times++;
            }
            return new(times, inc);
        } 
    }
}
