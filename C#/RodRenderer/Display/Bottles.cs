using GMath;
using System;
using Rendering;
using static Utils.Tools;
using static GMath.Gfx;
using static Objects.WaterBottle;
using static Objects.MilkBottle;
using static Objects.CoffeMaker;

namespace Display
{
    public static class BottlesArt
    {
        public static void DrawBottleArt(Raster render)
        {
            render.ClearRT(float4(0, 0, 0.2f, 1));
            //float4x4 viewMatrix = Transforms.LookAtLH(float3(5f, 4.6f, 2), float3(0, 0, 0), float3(0, 1, 0));
            //float4x4 projMatrix = Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 0.01f, 10);
            float4x4 viewMatrix = Transforms.LookAtLH(float3(10, 0f, 0), float3(0, 0.5f, 0), float3(0, 1, 0));
            float4x4 projMatrix = Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 8f, 40);

            float4x4 transforms = mul(viewMatrix, projMatrix);

            float3[] scene = SetScene(transforms);
            render.DrawPoints(scene);
        }

        
        private static float3[] SetScene(float4x4 transforms)
        {
            float3[] waterBottle = GetWaterBottlePoints();
            float3[] milkBottle = GetMilkBottlePoints();
            float3[] coffeMaker = GetCoffeMakerPoints();
            

            waterBottle = ApplyTransform(waterBottle, Transforms.Translate(0f, 0f, 0f));
            coffeMaker = ApplyTransform(coffeMaker, Transforms.Translate(-1.9f, 0f, 1.2f));
            milkBottle = ApplyTransform(milkBottle, Transforms.Translate(-1.9f, 0f, -1.2f));
        
            float3[] scene =  JoinPoints(coffeMaker, milkBottle, waterBottle);
            scene = Intersect(scene, p => p[1] > -5);
            scene = ApplyTransform(scene, transforms);
            return scene;
        }
    }
}