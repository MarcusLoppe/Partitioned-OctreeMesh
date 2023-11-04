using PartitionedMesh.Structures;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Shapes
{
    public class TriangleIndices : IComparable
    {
        public BoundingBox3 oldBounds;

        public Vector3[] oldVector3s = new Vector3[3];
        public int TriangleIndex;
        public int V0 { get; set; }
        public int V1 { get; set; }
        public int V2 { get; set; }
        public BoundingBox3? Bounds { get; set; }
        public TriangleIndices(int v0, int v1, int v2)
        {
            V0 = v0;
            V1 = v1;
            V2 = v2;
        }

        public override string ToString()
        {
            return $"{V0} {V1} {V2}";
        }
        public void CreateBounds(List<Vector3> vertices)
        {
            Bounds = BoundingBox3.CreateFromPoints(new List<Vector3>() { vertices[V0], vertices[V1], vertices[V2] });

        } 
        public int[] IndiceArray => new[] { V0, V1, V2 };
        public void copyBack(int[] dest, int offset)
        {
            Array.Copy(new[] { V0, V1, V2 }, 0, dest, offset, 3);
        }
        public int CompareTo(object obj)
        {
            if (obj == null) return 1;

            TriangleIndices otherTriangle = obj as TriangleIndices;
            if (otherTriangle != null)
                return compareTo(otherTriangle);
            else
                throw new ArgumentException("Object is not a Triangle");
        } 
        public int compareTo(TriangleIndices o)
        {
            if (V0 != o.V0)
            {
                return V0 - o.V0;
            }
            else if (V1 != o.V1)
            {
                return V1 - o.V1;
            }
            return V2 - o.V2;
        }

        public override bool Equals(object obj)
        { 
            if (obj == null) 
                return false; 

            TriangleIndices other = obj as TriangleIndices; 
            var elements = IndiceArray;
            var elementsOther = other.IndiceArray; 
            Array.Sort(elements);
            Array.Sort(elementsOther);

            // Loop through the array and add each element to the hash code
            for (int i = 0; i < 3; i++)
                if (elements[i] != elementsOther[i])
                    return false;

            return true;
        }

        public override int GetHashCode()
        {
            var elements = new[] { V0, V1, V2 };
            var hash = new HashCode();
            Array.Sort(elements);

            // Loop through the array and add each element to the hash code
            foreach (var value in elements)
            {
                hash.Add(value);
            }
            return hash.ToHashCode();//GetArrayHash(elements);
        } 
    } 

}
