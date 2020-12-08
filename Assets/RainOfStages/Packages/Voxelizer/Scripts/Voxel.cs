using System;
using System.Collections;
using System.Collections.Generic;
using System.Diagnostics;
using System.Runtime.InteropServices;

using UnityEngine;

namespace VoxelSystem
{

    [StructLayout(LayoutKind.Sequential)]
	public struct Voxel_t {
        public uint3 id;
		public Vector3 position;
        public Vector2 uv;
		public uint fill;
		public uint front;

        public bool IsFrontFace()
        {
            return fill > 0 && front > 0;
        }

        public bool IsBackFace()
        {
            return fill > 0 && front > 0;
        }

        public bool IsEmpty()
        {
            return fill < 1;
        }
	}

    [DebuggerDisplay("({x}, {y}, {z})")]
    [StructLayout(LayoutKind.Sequential)]
    public struct uint3 : IEquatable<uint3>
    {
        public uint x;
        public uint y;
        public uint z;

        public uint3(uint x, uint y, uint z)
        {
            this.x = x;
            this.y = y;
            this.z = z;
        }

        public override bool Equals(object obj)
        {
            return obj is uint3 @uint && Equals(@uint);
        }

        public bool Equals(uint3 other)
        {
            return x == other.x &&
                   y == other.y &&
                   z == other.z;
        }

        public override int GetHashCode()
        {
            int hashCode = 373119288;
            hashCode = hashCode * -1521134295 + x.GetHashCode();
            hashCode = hashCode * -1521134295 + y.GetHashCode();
            hashCode = hashCode * -1521134295 + z.GetHashCode();
            return hashCode;
        }

        public override string ToString()
        {
            return $"({x}, {y}, {z})";
        }

        public static bool operator ==(uint3 left, uint3 right)
        {
            return left.Equals(right);
        }

        public static bool operator !=(uint3 left, uint3 right)
        {
            return !(left == right);
        }
    }
}

