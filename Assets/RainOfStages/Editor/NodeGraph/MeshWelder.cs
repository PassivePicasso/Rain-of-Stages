using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

/*
	Originally created by Bunny83.
	Was streamlined and modified a bit by Terrev to better fit the needs of this project,
	then overhauled by grappigegovert for major speed improvements.
	Bunny83's original version:
	https://www.dropbox.com/s/u0wfq42441pkoat/MeshWelder.cs?dl=0
	Which was posted here:
	http://answers.unity3d.com/questions/1382854/welding-vertices-at-runtime.html
*/

namespace PassivePicasso.RainOfStages
{
    public class Vertex
    {
        public Vector3 pos;

        public Vertex(Vector3 aPos)
        {
            pos = aPos;
        }

        public override bool Equals(object obj)
        {
            Vertex other = obj as Vertex;
            if (other != null)
            {
                return other.pos.Equals(pos);
            }
            return false;
        }

        public override int GetHashCode()
        {
            unchecked
            {
                int hashCode = pos.x.GetHashCode();
                hashCode = (hashCode * 397) ^ pos.y.GetHashCode();
                hashCode = (hashCode * 397) ^ pos.z.GetHashCode();
                return hashCode;
            }
        }
    }

    public class MeshWelder
    {
        private static Vertex[] CreateVertexList(Vector3[] positions)
        {
            var vertices = new Vertex[positions.Length];
            for (int i = 0; i < positions.Length; i++)
            {
                var v = new Vertex(positions[i]);
                vertices[i] = v;
            }
            return vertices;
        }

        private static Dictionary<Vertex, List<int>> RemoveDuplicates(Vertex[] vertices)
        {
            var newVerts = new Dictionary<Vertex, List<int>>(vertices.Length);
            for (int i = 0; i < vertices.Length; i++)
            {
                Vertex v = vertices[i];
                List<int> originals;
                if (newVerts.TryGetValue(v, out originals))
                {
                    originals.Add(i);
                }
                else
                {
                    newVerts.Add(v, new List<int> { i });
                }
            }
            return newVerts;
        }

        private static (Vector3[] positions, int[] triangles) AssignNewVertexArrays(Vertex[] vertices, Dictionary<Vertex, List<int>> newVerts)
        {
            var map = new int[vertices.Length];
            var verts = new Vector3[newVerts.Count()];
            int i = 0;
            foreach (var vertMap in newVerts)
            {
                foreach (int index in vertMap.Value)
                {
                    map[index] = i;
                }
                verts[i] = vertMap.Key.pos;
                i++;
            }
            return (verts, map);
        }

        private static void RemapTriangles(int[] map, ref int[] tris)
        {
            for (int i = 0; i < tris.Length; i++)
                tris[i] = map[tris[i]];
        }

        public static void Weld(ref Vector3[] positions, ref int[] indices)
        {
            var vertices = CreateVertexList(positions);
            var map = RemoveDuplicates(vertices);
            var remapData = AssignNewVertexArrays(vertices, map);
            RemapTriangles(remapData.triangles, ref indices);
            positions = remapData.positions;
        }
    }
}