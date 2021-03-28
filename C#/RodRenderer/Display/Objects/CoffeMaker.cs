using GMath;
using System;
using Rendering;
using static Utils.Tools;
using static GMath.Gfx;


namespace Objects
{
    public static class CoffeMaker
    {
        public static float3[] GetCoffeMakerPoints()
        {
            float3[] upper = shapeUpper();
            float3[] body = shapeBody();
            float3[] bottom = shapeBottom();

            float3[] coffeMaker = JoinPoints(upper, body, bottom);
            
            //coffeMaker = ApplyTransform(coffeMaker, Transforms.RotateZGrad(90));

            return coffeMaker;
        }

        private static float3[]  shapeUpper()
        {
            int N = 50000;
            float3[] cone = RandomPointsInSurface(N, "Cone");
            float3[] plane = RandomPointsInSurface(N, "PlaneZX");

            plane = Intersect(plane, p => pow(p[0],2) + pow(p[2],2) <= 1);
            plane = ApplyTransform(plane, Transforms.Translate(0f, 1f, 0f));
            
            float3[] upper = JoinPoints(cone, plane);
            upper = ApplyTransform(upper, mul(Transforms.Scale(0.7f, 0.7f, 0.7f), Transforms.Translate(0f, -0.2f, 0.1f)));
            upper = ApplyTransform(upper, Transforms.RotateXGrad(7));
            upper = Intersect(upper, p => p[1] > 0.3);

            return upper;
        }

        private static float3[] shapeBody()
        {
            int N = 500000;
            float3[] cone = RandomPointsInSurface(N, "LongCone"); 

            cone = ApplyTransform(cone, mul(Transforms.Scale(1f, 1f, 1f), Transforms.RotateXGrad(180)));
            cone = ApplyTransform(cone, Transforms.Translate(0f, 3f, 0f));

            cone = Intersect(cone, p => p[1] > -2 && p[1] < 0.4);

            return cone;
        }

        private static float3[] shapeBottom()
        {
            int N = 100000;
            float3[] plane = RandomPointsInSurface(N, "PlaneZX");
            plane = Intersect(plane, p => pow(p[0],2) + pow(p[2],2) <= 1);
            plane = ApplyTransform(plane, Transforms.Translate(0f, -2f, 0f));
            
            return plane;
        }
    }
}