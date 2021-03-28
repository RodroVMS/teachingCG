using GMath;
using System;
using Rendering;
using static Utils.Tools;
using static GMath.Gfx;
using static Objects.GlassBottle;

namespace Objects
{
    public static class WaterBottle
    {
        public static float3[] GetWaterBottlePoints()
        {
            float3[] bottleNeck = shapeBottleNeck();
            float3[] upperCone = shapeCone();
            float3[] bottleBody = shapeBottleBody();

            float3[] lid = GetBottleLid();    
            lid = ApplyTransform(lid, Transforms.Translate(0, 3f, 0));

            float3[] upperPart = JoinPoints(bottleNeck, upperCone);
            upperPart = ApplyTransform(upperPart, Transforms.Translate(0, 1.80f, 0));


            float3[] bottom = GetBottleBottom();
            bottom = ApplyTransform(bottom, Transforms.Translate(0, -1.55f, 0));
            
            //upperPart = ApplyTransform(upperPart, Transforms.RotateZGrad(0));
            //return lid;
            //return bottom;
            //return upperCylinder;
            //return middleCylinder;
            //return upperCone;
            //return upperPart;
            float3[] waterBottle = JoinPoints(lid, upperPart, bottleBody, bottom);
            //waterBottle = ApplyTransform(waterBottle, Transforms.RotateZGrad(90));
            return waterBottle;
        }

        private static float3[] shapeBottleBody()
        {
            int N = 100000;
            float3[] middleCylinder = RandomPointsInSurface(N, "Cylinder");
            
            return middleCylinder;
        }

        private static float3[] shapeBottleNeck()
        {
            int N = 50000;
            float3[] upperCylinder = RandomPointsInSurface(N, "Cylinder");
            float4x4 upperCylinderTransf = mul(Transforms.Scale(0.35f, 1f, 0.35f), Transforms.Translate(0, 0.55f, 0));
            upperCylinder = ApplyTransform(upperCylinder, upperCylinderTransf);

            upperCylinder = Intersect(upperCylinder, p => p[1] < 1.3);
            upperCylinder = Intersect(upperCylinder, p => p[1] > 0.1);

            return upperCylinder;
        }

        private static float3[] shapeCone()
        {
            int N = 70000;
            float3[] upperCone = RandomPointsInSurface(N, "Cone");

            float4x4 upConeTransform =  mul(Transforms.RotateXGrad(180), Transforms.Scale(1f, 0.3f, 1f));
            upperCone = ApplyTransform(upperCone, upConeTransform);

            upperCone = Intersect(upperCone, p => p[1] < 0.1);

            return upperCone;
        }
    }
}