using GMath;
using System;
using Rendering;
using static Utils.Tools;
using static GMath.Gfx;
using static Objects.GlassBottle;

namespace Objects
{
    class MilkBottle
    {
        public static float3[] GetMilkBottlePoints()
        {
            float3[] lid = GetBottleLid();
            float3[] neck = shapeBottleNeck();
            float3[] body = shapeBottleBody();
            float3[] bottom = GetBottleBottom();

            lid = ApplyTransform(lid, mul(Transforms.Translate(0f, 3f, 0f), Transforms.Scale(1.2f, 1f, 1.2f)));
            neck = ApplyTransform(neck, Transforms.Translate(0f, 1f, 0f));
            body = ApplyTransform(body, Transforms.Scale(1f, 1.1f, 1f));
            bottom = ApplyTransform(bottom, Transforms.Translate(0, -1.55f, 0));

            
            float3[] milkBottle = JoinPoints(lid, neck, body, bottom);
            return milkBottle;
        }
        
        private static float3[] shapeBottleNeck()
        {
            int N = 400000;
            float3[] hip = RandomPointsInSurface(N, "Hiperboloid");
            hip = ApplyTransform(hip, Transforms.Translate(0f, 2.4f, 0f));
            hip = Intersect(hip, p => p[1] > 0.65 && p[1] < 2);
            return hip;
        }

        private static float3[] shapeBottleNeck2()
        {
            int N = 400000;
            float3[] ellipsoid = RandomPointsInSurface(N, "Ellipsoid");
            float3[] cone = RandomPointsInSurface(N, "Cone");

            float3[] cone1 = ApplyTransform(cone, mul(Transforms.Scale(0.8f, 1.9f, 0.8f), Transforms.RotateZGrad(180)));
            cone1 = ApplyTransform(cone1, Transforms.Translate(0f, 1.6f, 0f));

            float3[] cone2 = ApplyTransform(cone, mul(Transforms.Scale(3.0f, 2.5f, 3.0f), Transforms.RotateZGrad(180)));
            cone1 = ApplyTransform(cone1, Transforms.Translate(0f, 1f, 0f));
            
            //return cone;
            float3[] neck = JoinPoints(cone1, cone2);
            return neck;
        }
        private static float3[] shapeBottleBody()
        {
            int N = 100000;
            float3[] middleCylinder = RandomPointsInSurface(N, "Cylinder");
            
            return middleCylinder;
        }
    }
}