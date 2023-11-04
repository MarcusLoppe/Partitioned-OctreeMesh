using PartitionedMesh.Shapes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.MeshCodec
{
    public class Grid
    {
        public float[] Min { get; set; }
        public float[] Max { get; set; }
        public float[] Size { get; set; }
        public int[] Division { get; set; }
          
        public BoundingBox3 Bounds { get; set; }
        public Grid(float[] min, float[] max, int[] division)
        {
            Min = min;
            Max = max;
            Division = division;
            Bounds = new BoundingBox3(new Vector3(min[0], min[1], min[2]), new Vector3(max[0], max[1], max[2]));

            Size = new float[3];
            for (int i = 0; i < 3; i++)
            {
                Size[i] = (Max[i] - Min[i]) / Division[i];
            }
        }
        public Grid(Vector3 min, Vector3 max, int[] division)
        {
            Min = new float[3] { min.X, min.Y, min.Z };
            Max = new float[3] { max.X, max.Y, max.Z };
            Division = division;
            Bounds = new BoundingBox3(min,max);

            Size = new float[3];
            for (int i = 0; i < 3; i++)
            {
                Size[i] = (Max[i] - Min[i]) / Division[i];
            }
        }

        public Grid(float[] vertices, float vertexPrecision = 1.0f / 1024.0f)
        {
            int vc = vertices.Length / 3;
            float[] min = new float[3];
            float[] max = new float[3];
            int[] division = new int[3];

            for (int i = 0; i < 3; ++i)
            {
                min[i] = max[i] = vertices[i];

            }
            for (int i = 1; i < vc; ++i)
            {
                for (int j = 0; j < 3; j++)
                {
                    min[j] = Math.Min(min[j], vertices[i * 3 + j]);
                    max[j] = Math.Max(max[j], vertices[i * 3 + j]);
                }
            }
              
            float[] factor = new float[3];
            for (int i = 0; i < 3; ++i)
            {
                factor[i] = max[i] - min[i];
            }

            float sum = factor[0] + factor[1] + factor[2];

            if (sum > 1e-30f)
            {
                sum = 1.0f / sum;
                for (int i = 0; i < 3; ++i)
                {
                    factor[i] *= sum;
                }
                double wantedGrids = Math.Pow(100.0f * vc, 1.0f / 3.0f);
                for (int i = 0; i < 3; ++i)
                {
                    division[i] = (int)Math.Ceiling(wantedGrids * factor[i]);
                    if (division[i] < 1)
                    {
                        division[i] = 1;
                    }
                }
            }
            else
            {
                division[0] = 4;
                division[1] = 4;
                division[2] = 4;
            }

            Min = min;
            Max = max; 
            Bounds = new BoundingBox3(new Vector3(min[0], min[1], min[2]), new Vector3(max[0], max[1], max[2]));
            Division = division;

            Size = new float[3];
            for (int i = 0; i < 3; i++)
            {
                Size[i] = (Max[i] - Min[i]) / Division[i];
            }
        }
 
        public float[] gridIdxToPoint(int idx)
        {
            int[] gridIdx = new int[3];

            int ydiv = Division[0];
            int zdiv = ydiv * Division[1];

            gridIdx[2] = idx / zdiv;
            idx -= gridIdx[2] * zdiv;
            gridIdx[1] = idx / ydiv;
            idx -= gridIdx[1] * ydiv;
            gridIdx[0] = idx;
             
            float[] point = new float[3];
            for (int i = 0; i < 3; ++i)
            {
                point[i] = gridIdx[i] * Size[i] + Min[i];
            }
            return point;
        }
         
        public Vector3 gridIdxToVector3(int idx)
        {
            int[] gridIdx = new int[3];

            int ydiv = Division[0];
            int zdiv = ydiv * Division[1];

            gridIdx[2] = idx / zdiv;
            idx -= gridIdx[2] * zdiv;
            gridIdx[1] = idx / ydiv;
            idx -= gridIdx[1] * ydiv;
            gridIdx[0] = idx;

            float[] point = new float[3];
            for (int i = 0; i < 3; ++i)
            {
                point[i] = gridIdx[i] * Size[i] + Min[i];
            }
            return new Vector3(point[0], point[1], point[2]);
        }

        public int pointToGridIdx(float[] point)
        {
            int[] idx = new int[3]; 

            for (int i = 0; i < 3; ++i)
            {
                idx[i] = (int)Math.Floor((point[i] - Min[i]) / Size[i]);
                if (idx[i] >= Division[i])
                {
                    idx[i] = Division[i] - 1;
                }
            }

            return idx[0] + Division[0] * (idx[1] + Division[1] * idx[2]);
        }
        public int pointToGridIdx(Vector3 vector)
        {
            int[] idx = new int[3];
            float[] point = new float[3] { vector.X, vector.Y, vector.Z };
              
            for (int i = 0; i < 3; ++i)
            {
                idx[i] = (int)Math.Floor((point[i] - Min[i]) / Size[i]);
                if (idx[i] >= Division[i])
                {
                    idx[i] = Division[i] - 1;
                }
            }

            return idx[0] + Division[0] * (idx[1] + Division[1] * idx[2]);
        } 
    }
}
