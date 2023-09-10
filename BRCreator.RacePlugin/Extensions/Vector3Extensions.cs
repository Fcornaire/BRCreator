using UnityEngine;

namespace BRCreator.RacePlugin.Extensions
{
    public static class Vector3Extensions
    {
        public static Vector3 ToUnityVector3(this System.Numerics.Vector3 vector3)
        {
            return new Vector3(vector3.X, vector3.Y, vector3.Z);
        }
    }
}
