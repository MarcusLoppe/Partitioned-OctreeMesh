using System;
using System.Collections.Generic;
using System.Linq;
using System.Numerics;
using System.Text;
using System.Threading.Tasks;

namespace PartitionedMesh.Common
{
    public struct Ray
    {
        public Vector3 Origin; // The starting point of the ray
        public Vector3 Direction; // The normalized direction vector of the ray
        public Vector3 Destination; // The normalized direction vector of the ray 
        public float Distance;
        public Ray(Vector3 origin, Vector3 destination)
        {
            Origin = origin;
            Destination = destination;
            // Calculate the direction vector by subtracting the origin from the destination
            Direction = destination - origin;
            Distance = Vector3.Distance(origin, destination);
            // Normalize the direction vector to have unit length
            Direction = Direction / Direction.Length();
        }

    }
}
