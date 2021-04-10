using System;
using System.Collections.Generic;
using GMath;
using static GMath.Gfx;

namespace Utils
{
    public static class Tools
    {
        private delegate float3 figure_function();
        private static figure_function SelectFigure(string figure)
        {
            switch (figure)
            {
                case "Box": return randomInBox;
                case "Cylinder": return randomInCylinder;
                case "Cone": return randomInCone;
                case "LongCone": return randomInLongCone;
                case "Ellipsoid": return randomInEllipsoid;
                case "RectTrapezoid": return randomInRectTrapezoid;
                case "Trapezoid": return randomInTrapezoid;
                case "HalfParabole": return randomInHalfParabole;
                case "PlaneZX": return RandomInPlaneZX;
                case "Hiperboloid": return randomInHiperboloid;
            }
            throw new Exception("" + figure + " is not a valid figure.");
        }
        public static float3[] RandomPointsInSurface(int N, string figure)
        {
            figure_function randomPoints = SelectFigure(figure);
            float3[] points = new float3[N];
            for (int i = 0; i < N; i++)
                points[i] = randomPoints();

            return points;
        }

        public static float3[] Intersect(float3[] points, Func<float3, bool> fxyz)
        {
            var intersectPoints = new List<float3>();
            for (int i = 0; i < points.Length; i++)
            {
                float3 point = points[i];
                if (fxyz(point))
                    intersectPoints.Add(point);
            }
            return intersectPoints.ToArray();
        }

        public static float3[] ApplyTransform(float3[] points, float4x4 matrix)
        {
            float3[] result = new float3[points.Length];

            for (int i = 0; i < points.Length; i++)
            {
                float4 h = float4(points[i], 1);
                h = mul(h, matrix);
                result[i] = h.xyz / h.w;
            }

            return result;
        }

        public static float3[] ApplyTransform(float3[] points, Func<float3, float3> freeTransform)
        {
            float3[] result = new float3[points.Length];

            for (int i = 0; i < points.Length; i++)
                result[i] = freeTransform(points[i]);

            return result;
        }

        public static float3[] JoinPoints(params float3[][] points)
        {
            int total_length = 0;
            for (int i = 0; i < points.Length; i++)
            {
                total_length += points[i].Length;
            }

            int index = 0;
            float3[] joinPoints = new float3[total_length];
            for (int i = 0; i < points.Length; i++)
            {
                copy<float3>(joinPoints, points[i], index);
                index += points[i].Length;
            }
            return joinPoints;
        }


        public static void copy<T>(T[] vessel, T[] demon, int index = 0)
        {
            for (int i = index; i < demon.Length + index; i++)
            {
                vessel[i] = demon[i - index];
            }
        }
    }
}