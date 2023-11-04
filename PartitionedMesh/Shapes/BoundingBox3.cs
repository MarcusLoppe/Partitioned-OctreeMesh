using PartitionedMesh.Common;
using PartitionedMesh.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Shapes
{

    public enum ContainmentType
    {
        Disjoint,
        Contains,
        Intersects,
    } 
    public class BoundingBox3 : IEquatable<BoundingBox3>
    {  
        public Vector3 Min;

        public Vector3 Max; 
         
        public float Width => Math.Abs(Min.X - Max.X);

        public float Height => Math.Abs(Min.Y - Max.Y);

        public float Depth => Math.Abs(Min.Z - Max.Z); 
 
        public BoundingBox3(Vector3 min, Vector3 max)
        {
            Min = min;
            Max = max;
        }
        public BoundingBox3()
        {
        }
        public bool IntersectRay(Ray traceLine)
        {
            // Calculate the Direction and length of the trace line
            Vector3 Direction = traceLine.Destination - traceLine.Origin;
            float length = Direction.Length();
            Direction = Vector3.Normalize(Direction);

            // Calculate the minimum and maximum distances for intersection
            float tMin = (Min.X - traceLine.Origin.X) / Direction.X;
            float tMax = (Max.X - traceLine.Origin.X) / Direction.X;

            // Swap min and max values if necessary
            if (tMin > tMax)
            {
                float temp = tMin;
                tMin = tMax;
                tMax = temp;
            }

            float tyMin = (Min.Y - traceLine.Origin.Y) / Direction.Y;
            float tyMax = (Max.Y - traceLine.Origin.Y) / Direction.Y;

            if (tyMin > tyMax)
            {
                float temp = tyMin;
                tyMin = tyMax;
                tyMax = temp;
            }

            // Check for intersection with X-axis
            if ((tMin > tyMax) || (tyMin > tMax))
            {
                return false;
            }

            if (tyMin > tMin)
            {
                tMin = tyMin;
            }

            if (tyMax < tMax)
            {
                tMax = tyMax;
            }

            float tzMin = (Min.Z - traceLine.Origin.Z) / Direction.Z;
            float tzMax = (Max.Z - traceLine.Origin.Z) / Direction.Z;

            if (tzMin > tzMax)
            {
                float temp = tzMin;
                tzMin = tzMax;
                tzMax = temp;
            }
             
            if ((tMin > tzMax) || (tzMin > tMax))
            {
                return false;
            }

            if (tzMin > tMin)
            {
                tMin = tzMin;
            }

            if (tzMax < tMax)
            {
                tMax = tzMax;
            }
             
            return (tMin < length) && (tMax > 0);
        } 
        public BoundingBox3[] Split()
        {
            Vector3 center = (Min + Max) / 2; 
            // Create eight new boxes using the combinations of Min, Max, and center as the corners
            BoundingBox3[] result = new BoundingBox3[8];
            result[0] = new BoundingBox3(Min, center); // Bottom left front
            result[1] = new BoundingBox3(new Vector3(center.X, Min.Y, Min.Z), new Vector3(Max.X, center.Y, center.Z)); // Bottom right front
            result[2] = new BoundingBox3(new Vector3(Min.X, center.Y, Min.Z), new Vector3(center.X, Max.Y, center.Z)); // Top left front
            result[3] = new BoundingBox3(new Vector3(center.X, center.Y, Min.Z), new Vector3(Max.X, Max.Y, center.Z)); // Top right front
            result[4] = new BoundingBox3(new Vector3(Min.X, Min.Y, center.Z), new Vector3(center.X, center.Y, Max.Z)); // Bottom left back
            result[5] = new BoundingBox3(new Vector3(center.X, Min.Y, center.Z), new Vector3(Max.X, center.Y, Max.Z)); // Bottom right back
            result[6] = new BoundingBox3(new Vector3(Min.X, center.Y, center.Z), new Vector3(center.X, Max.Y, Max.Z)); // Top left back
            result[7] = new BoundingBox3(center, Max); // Top right back
            return result;
        } 
         
        public Vector3 Size
        {
            get
            {
                return (Max - Min);
            }
        }
         
        public void ExpandToFit(BoundingBox3 other)
        {
            Min.X = Math.Min(Min.X, other.Min.X);
            Min.Y = Math.Min(Min.Y, other.Min.Y);
            Min.Z = Math.Min(Min.Z, other.Min.Z);
            Max.X = Math.Max(Max.X, other.Max.X);
            Max.Y = Math.Max(Max.Y, other.Max.Y);
            Max.Z = Math.Max(Max.Z, other.Max.Z);
        }
        public double Volume()
        {
            return (Max.X - Min.X) + (Max.Y - Min.Y) + (Max.Z - Min.Z);
        }  
        public bool Contains(Vector3 v)
        {
            return (double)v.X >= (double)Min.X && (double)v.X <= (double)Max.X && (double)v.Y >= (double)Min.Y && (double)v.Y <= (double)Max.Y && (double)v.Z >= (double)Min.Z && (double)v.Z <= (double)Max.Z;
        } 
        public bool Contains(params Vector3[] vs)
        {
            foreach (var v in vs)
                if (Contains(v))
                    return true;
            return false;
        }

        public override bool Equals(object other)
        {
            var box = other as BoundingBox3;
            if (box == null)
                return false; 
            return box.Min.X == Min.X && box.Min.Y == Min.Y && box.Min.Z == Min.Z && box.Max.X == Max.X && box.Max.Y == Max.Y && box.Max.Z == Max.Z;
        }
        public bool Equals(BoundingBox3 box)
        {
            return box.Min.X == Min.X && box.Min.Y == Min.Y && box.Min.Z == Min.Z && box.Max.X == Max.X && box.Max.Y == Max.Y && box.Max.Z == Max.Z;
        } 
        public ContainmentType Contains(BoundingBox3 box)
        {
            return Contains(this, ref box);
        } 
        public static ContainmentType Contains(BoundingBox3 box1, ref BoundingBox3 box2)
        {
            if ((double)box1.Max.X < (double)box2.Min.X || (double)box1.Min.X > (double)box2.Max.X || (double)box1.Max.Y < (double)box2.Min.Y || (double)box1.Min.Y > (double)box2.Max.Y || (double)box1.Max.Z < (double)box2.Min.Z || (double)box1.Min.Z > (double)box2.Max.Z)
            {
                return ContainmentType.Disjoint;
            }

            return ((double)box1.Min.X <= (double)box2.Min.X && (double)box2.Max.X <= (double)box1.Max.X && (double)box1.Min.Y <= (double)box2.Min.Y && (double)box2.Max.Y <= (double)box1.Max.Y && (double)box1.Min.Z <= (double)box2.Min.Z && (double)box2.Max.Z <= (double)box1.Max.Z) ? ContainmentType.Contains : ContainmentType.Intersects;
        } 
        public static BoundingBox3 CreateFromPoints(IEnumerable<Vector3> points)
        {
            using IEnumerator<Vector3> enumerator = points.GetEnumerator();
            if (!enumerator.MoveNext())
            {
                return default(BoundingBox3);
            }

            BoundingBox3 result = new();
            result.Min = new Vector3(float.MaxValue);
            result.Max = new Vector3(float.MinValue);
            do
            {
                result.Min = Vector3.Min(result.Min, enumerator.Current);
                result.Max = Vector3.Max(result.Max, enumerator.Current);
            }
            while (enumerator.MoveNext());
            return result;
        } 
        public override int GetHashCode()
        {
            unchecked // Overflow is fine for a hash code
            {
                int hash = 17;
                hash = hash * 23 + Min.GetHashCode();
                hash = hash * 23 + Max.GetHashCode();
                return hash;
            }
        }
         
        public override string ToString()
        {
            return $"Min: {Min} Max: {Max} Size: {Width}/{Height}/{Depth}";
        }
    }

}
