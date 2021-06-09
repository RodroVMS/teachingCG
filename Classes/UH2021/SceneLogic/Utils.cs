using System;
using System.Collections.Generic;
using Rendering;
using GMath;
using static GMath.Gfx;

namespace SceneLogic
{
    public static class Utils<V> where V: struct, INormalVertex<V>, ICoordinatesVertex<V>
    {

        public static Mesh<V> BezierCurves(float3[] control, int slices = 30, int stacks = 30)
        {
            static float3 EvalBezier(float3[] control, float t)
            {
                // DeCasteljau
                if (control.Length == 1)
                    return control[0]; // stop condition
                float3[] nestedPoints = new float3[control.Length - 1];
                for (int i = 0; i < nestedPoints.Length; i++)
                    nestedPoints[i] = lerp(control[i], control[i + 1], t);
                return EvalBezier(nestedPoints, t);
            }

            return Manifold<V>.Revolution(slices, stacks, t => EvalBezier(control, t), float3(0, 1, 0));
        }

        public static Mesh<V> CreateCyllinder(float radius, float max_height, float min_height = 0, int slices = 30, int stacks = 30)
        {
            return Manifold<V>.Surface(slices, stacks, (u, v) =>
            {
                float alpha = u * 2 * pi;

                float x = radius * cos(alpha);
                float y = v * (max_height - min_height) + min_height;
                float z = radius * sin(alpha);

                return float3(x, y, z);
            });
        }

        public static Mesh<V> CreateCircle(float radius, int slices = 30, int stacks = 30)
        {
            return Manifold<V>.Surface(slices, stacks, (u, v) =>
            {
                float alpha = u * pi * 2;

                float x = v * radius * cos(alpha);
                float z = v * radius * sin(alpha);

                return float3(x, 0, z);
            });
        }

        public static Mesh<V> CreatePlane(float maxSideA, float maxSideB, float minSideA = 0, float minSideB = 0, int slices = 30, int stacks = 30)
        {
            return Manifold<V>.Surface(slices, stacks, (u, v) =>
            {
                float x = u * (maxSideA - minSideA) + minSideA;
                float z = v * (maxSideB - minSideB) + minSideB;

                return float3(x, 0, z);
            });
        }

        public static Mesh<V> CreateCartesianOval(float baseRadius, float maxHeight, float zGrowth, float yGrowth, float minHeight = 0, int slices = 15, int stacks = 15)
        {
            return Manifold<V>.Surface(slices, stacks, (p1, p2) => 
            {
                float alpha = p1 * 2 * pi;
                
                float x = baseRadius * cos(alpha);

                float z = sin(alpha);
                float y = (1 + z) * p2 * (maxHeight - minHeight) + minHeight + p2*yGrowth;

                z = z > 0? (baseRadius + p2*zGrowth) * z: baseRadius * z;

                return float3(x, y, z);
            });
        }

        public static void ApplyTransforms(float4x4 transform, List<Mesh<V>> mesh_list)
        {
            for (int i = 0; i < mesh_list.Count; i++)
            {
                mesh_list[i] = mesh_list[i].Transform(transform);
            }
        }

        public static Mesh<V> MorphMeshes(List<Mesh<V>> l, Topology topology)
        {
            int[] allStacks = new int[l.Count];

            int newVerticesLenght = 0;
            int newIndicesLenght = 0;

            for (int i = 0; i < l.Count; i++)
            {
                var mesh = l[i];
                int slices = (int)mesh.Slices;
                int stacks = (mesh.Vertices.Length / (slices + 1)) - 1;
                allStacks[i] = stacks;

                newVerticesLenght += (slices + 1) * (stacks + 1);
                newIndicesLenght += slices * stacks * 6;
            }

            V[] newVertices = new V[newVerticesLenght];
            int[] newIndices = new int[newIndicesLenght];

            int acc = 0;
            for (int i = 0; i < l.Count; i++)
            {
                var vertices = l[i].Vertices;
                Array.Copy(vertices, 0, newVertices, acc, vertices.Length);
                acc += vertices.Length;
            }

            acc = 0;
            int index = 0;
            for (int f = 0; f < l.Count; f++)
            {
                int slices = (int)l[f].Slices;
                int stacks = allStacks[f];
                for (int i = 0; i < slices; i++)
                    for (int j = 0; j < stacks; j++)
                    {
                        newIndices[index++] = acc + i * (slices + 1) + j;
                        newIndices[index++] = acc + (i + 1) * (slices + 1) + j;
                        newIndices[index++] = acc + (i + 1) * (slices + 1) + (j + 1);

                        newIndices[index++] = acc + i * (slices + 1) + j;
                        newIndices[index++] = acc + (i + 1) * (slices + 1) + (j + 1);
                        newIndices[index++] = acc + i * (slices + 1) + (j + 1);
                    }
                acc += (slices + 1) * (stacks + 1);
            }

            return new Mesh<V>(newVertices, newIndices, null, topology);
        }

    }
}