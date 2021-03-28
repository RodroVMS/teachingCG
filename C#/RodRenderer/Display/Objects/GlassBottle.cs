using GMath;
using System;
using Rendering;
using static Utils.Tools;
using static GMath.Gfx;

namespace Objects
{
    public static class GlassBottle
    {
        public static float3[] GetBottleBottom()
        {
            int N = 100000;
            float3[] bottomPlane = RandomPointsInSurface(N, "PlaneZX");
            float3[] bottomElips = RandomPointsInSurface(N, "Ellipsoid");

            bottomPlane = ApplyTransform(bottomPlane, Transforms.Translate(0, -0.5f, 0));
            bottomPlane = Intersect(bottomPlane, p => pow(p[0], 2) + pow(p[2], 2) <= pow(0.95f,2));
            bottomElips = Intersect(bottomElips, p => p[1] < 0 && p[1] > -0.5f);
            
            float3[] bottomPart = JoinPoints(bottomPlane, bottomElips);
            return bottomPart;
        }

        public static float3[] GetBottleLid()
        {
            int N = 100000;
            float3[] upperLid = RandomPointsInSurface(N/2, "PlaneZX");
            float3[] bodyLid = RandomPointsInSurface(N, "Cylinder");
            

            upperLid = Intersect(upperLid, p => pow(p[0],2) + pow(p[2], 2) <= 1);
            bodyLid = Intersect(bodyLid, p => p[1] > 0  && p[1] < 0.3);


            float scale = 0.4f;
            upperLid = ApplyTransform(upperLid, mul(Transforms.Translate(0, 0.3f, 0), Transforms.Scale(scale, 1f, scale)));
            bodyLid = ApplyTransform(bodyLid, Transforms.Scale(scale, 1f, scale));

            float3[] lid = JoinPoints(upperLid, bodyLid);
            return lid;
        }

        //private static
    }
}