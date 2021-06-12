using System;
using System.Collections.Generic;
using Rendering;
using GMath;
using static GMath.Gfx;

namespace  SceneLogic
{
    public static class MeshObjects<V> where V : struct, INormalVertex<V>, ICoordinatesVertex<V>
    {
        public static (Mesh<V>, Mesh<V>, Mesh<V>, Mesh<V>) GetWaterBottle(int slices, int stacks)
        {
            (Mesh<V> waterBottle, Mesh<V> lid, Mesh<V> labelOut, Mesh<V> labelIn) = CreaterWaterBottle(slices, stacks);
            waterBottle = waterBottle.Weld();
            waterBottle.ComputeNormals();

            lid = lid.Weld();
            lid.ComputeNormals();

            labelOut = labelOut.Weld();
            labelOut.ComputeNormals();

            labelIn = labelIn.Weld();
            labelIn.NegativeNormal = true;
            labelIn.ComputeNormals();

            return (waterBottle, lid, labelOut, labelIn);
        }

        private static (Mesh<V>, Mesh<V>, Mesh<V>, Mesh<V>) CreaterWaterBottle(int slices = 15, int stacks = 15)
        {
            // Values to have the bottle correctly proportioned ...
            float mainBodyRadius = 0.5f;
            float lidRadius = 0.5f * mainBodyRadius;
            float neckRadius = 0.425f * mainBodyRadius;
            float bottomRadius = 0.9f * mainBodyRadius;

            float mainBodyHeight = 1.2f;
            float bottomHeight = 0.5f * mainBodyHeight;
            float upperConeHeight = 0.2f * mainBodyHeight;
            float neckHeight = 0.5f * mainBodyHeight;
            float lidHeight = 0.1f * mainBodyHeight;
            
            float3[] controlCone = { 
                float3(mainBodyRadius, 0, 0),
                float3(mainBodyRadius, upperConeHeight * 0.7f, 0),
                float3(neckRadius, upperConeHeight * 0.8f, 0),
                float3(neckRadius, upperConeHeight, 0)
            };
            var upperCone = SceneLogic.Utils<V>.BezierCurves(controlCone, slices: slices, stacks: stacks);
            upperCone = upperCone.Transform(Transforms.Translate(0, mainBodyHeight / 2, 0));

            var neck = SceneLogic.Utils<V>.CreateCyllinder(radius: neckRadius, max_height: neckHeight, slices: slices, stacks: stacks);
            neck = neck.Transform(Transforms.Translate(0, mainBodyHeight / 2 + upperConeHeight, 0));

            float3[] controlBody = {
                float3(0, 0, 0),
                float3(bottomRadius, 0, 0),
                float3(bottomRadius, 0, 0),
                float3(bottomRadius, 0, 0),
                float3(bottomRadius, 0, 0),
                float3(bottomRadius, 0, 0),

                float3(mainBodyRadius, bottomHeight * 0.7f, 0),
                float3(mainBodyRadius, bottomHeight * 0.8f, 0),
                float3(mainBodyRadius, bottomHeight, 0),
                float3(mainBodyRadius, bottomHeight + mainBodyHeight / 2f, 0),
                float3(mainBodyRadius, bottomHeight + mainBodyHeight, 0),
                // float3(mainBodyRadius, bottomHeight + mainBodyHeight, 0),
                // float3(mainBodyRadius, bottomHeight + mainBodyHeight, 0),
                // float3(mainBodyRadius, bottomHeight + mainBodyHeight, 0),
                // float3(mainBodyRadius, bottomHeight + mainBodyHeight, 0),
                // float3(mainBodyRadius, bottomHeight + mainBodyHeight, 0),
 
                // float3(mainBodyRadius*0.9f, bottomHeight + mainBodyHeight + upperConeHeight * 0.4f, 0),
                // float3(mainBodyRadius*0.9f, bottomHeight + mainBodyHeight + upperConeHeight * 0.4f, 0),
                // float3(mainBodyRadius*0.9f, bottomHeight + mainBodyHeight + upperConeHeight * 0.4f, 0),
                // float3(mainBodyRadius*0.9f, bottomHeight + mainBodyHeight + upperConeHeight * 0.4f, 0),
                // float3(mainBodyRadius*0.5f, bottomHeight + mainBodyHeight + upperConeHeight * 0.9f, 0),
                // float3(mainBodyRadius*0.5f, bottomHeight + mainBodyHeight + upperConeHeight * 0.9f, 0),
                // float3(mainBodyRadius*0.5f, bottomHeight + mainBodyHeight + upperConeHeight * 0.9f, 0),
                // float3(mainBodyRadius*0.5f, bottomHeight + mainBodyHeight + upperConeHeight * 0.9f, 0),

                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight, 0),
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight, 0),
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight, 0),
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight, 0),
                 
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight + neckHeight*0.1f, 0),
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight + neckHeight*0.1f, 0),
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight + neckHeight*0.1f, 0),
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight + neckHeight*0.1f, 0),
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight + neckHeight*0.1f, 0),
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight + neckHeight*0.1f, 0),
                // float3(neckRadius, bottomHeight + mainBodyHeight + upperConeHeight + neckHeight, 0),
            };
            var body = SceneLogic.Utils<V>.BezierCurves(controlBody, slices: slices, stacks: stacks);
            body = body.Transform(Transforms.Translate(0, -mainBodyHeight, 0));

            var waterBottle = new List<Mesh<V>>() { upperCone, neck, body};
            SceneLogic.Utils<V>.ApplyTransforms(Transforms.Translate(0, 0.2f, 0), waterBottle);
            // var bottom = SceneLogic.Utils<V>.CreateCircle(bottomRadius, slices, stacks);
            // bottom = bottom.Transform(Transforms.Translate(0, -1, 0));
            // waterBottle.Add(bottom);

            var lid = GetBottleLid(lidRadius, lidHeight, slices, stacks);
            lid = lid.Transform(Transforms.Translate(0, mainBodyHeight/2 + upperConeHeight + neckHeight + 0.2f, 0));

            var labelOut = SceneLogic.Utils<V>.CreateCyllinder(radius: neckRadius + 0.0012f, max_height: neckHeight - 0.1f, min_height: 0.1f, slices, stacks);
            labelOut = labelOut.Transform(Transforms.Translate(0, mainBodyHeight/2 + upperConeHeight + 0.2f, 0));

            var labelIn = SceneLogic.Utils<V>.CreateCyllinder(radius: neckRadius - 0.0012f, max_height: neckHeight - 0.1f, min_height: 0.1f, slices, stacks);
            labelIn = labelIn.Transform(Transforms.Translate(0, mainBodyHeight/2 + upperConeHeight + 0.2f, 0));
            
            return (SceneLogic.Utils<V>.MorphMeshes(waterBottle, Topology.Triangles), lid, labelOut, labelIn);
        }

        public static (Mesh<V>, Mesh<V>, Mesh<V>) GetMilkBottle(int slices, int stacks)
        {
            (Mesh<V> milkBottle, Mesh<V> lid) = CreateMilkBottle(slices, stacks);
            milkBottle = milkBottle.Weld();
            milkBottle.ComputeNormals();

            lid = lid.Weld();
            lid.ComputeNormals();

            Mesh<V> milk = CreateMilk(slices, stacks);
            milk = milk.Weld();
            milk.ComputeNormals();

            return (milkBottle, lid, milk);
        }

        private static (Mesh<V>, Mesh<V>) CreateMilkBottle(int slices = 15, int stacks = 15)
        {
            // Values to have the bottle correctly proportioned ...
            float bodyHeight = 1.5f;
            float upperPartHeight = 0.7f * bodyHeight;

            float upperBodyRadius = 0.5f;
            float bottomBodyRadius = 0.95f * upperBodyRadius;
            float neckRadius = 0.4f * upperBodyRadius;

            float3[] bodyControl = { 
                float3(0, -bodyHeight / 2, 0), 
                float3(bottomBodyRadius, -bodyHeight / 2, 0),
                float3(bottomBodyRadius, -bodyHeight / 2, 0), 
                float3(bottomBodyRadius, -bodyHeight / 2, 0),
                float3(bottomBodyRadius, -bodyHeight / 2, 0), 
                float3(bottomBodyRadius, -bodyHeight / 2, 0), 
                float3(upperBodyRadius, bodyHeight / 2, 0) 
            };
            var mainBody =  SceneLogic.Utils<V>.BezierCurves(bodyControl, slices, stacks);

            float3[] upperPartControl = {
                float3(upperBodyRadius, 0, 0),
                float3(upperBodyRadius, upperPartHeight * 0.2f, 0),
                float3(upperBodyRadius * 0.80f, upperPartHeight * 0.40f, 0),
                float3(upperBodyRadius * 0.75f, upperPartHeight * 0.50f, 0),
                float3(upperBodyRadius * 0.70f, upperPartHeight * 0.55f, 0),
                float3(upperBodyRadius * 0.5f, upperPartHeight * 0.70f, 0),
                float3(upperBodyRadius * 0.5f, upperPartHeight * 0.80f, 0),
                float3(upperBodyRadius * 0.5f, upperPartHeight * 0.90f, 0),
                float3(upperBodyRadius * 0.5f, upperPartHeight, 0)
            };
            var upperPart =  SceneLogic.Utils<V>.BezierCurves(upperPartControl, slices, stacks);
            upperPart = upperPart.Transform(Transforms.Translate(0, bodyHeight / 2, 0));

            var milkBottle = new List<Mesh<V>>() { mainBody, upperPart};
            SceneLogic.Utils<V>.ApplyTransforms(Transforms.Translate(0, -0.25f, 0), milkBottle);

            var lid = GetBottleLid(upperBodyRadius * 0.6f, 0.1f, slices, stacks);
            //lid = lid.Transform(Transforms.RotateZGrad(60));
            lid = lid.Transform(Transforms.Translate(0, bodyHeight / 2 + upperPartHeight - 0.25f, 0));


            return (SceneLogic.Utils<V>.MorphMeshes(milkBottle, Topology.Triangles), lid);
        }

        public static Mesh<V> CreateMilk(int slices = 15, int stacks = 15)
        {
            float bodyHeight = 1.5f;
            float upperPartHeight = 0.7f * bodyHeight;

            float upperBodyRadius = 0.5f - 0.005f;
            float bottomBodyRadius = 0.95f * upperBodyRadius;
            float neckRadius = 0.4f * upperBodyRadius;

            float3[] bodyControl = {
                float3(0, -bodyHeight / 2, 0), 
                float3(bottomBodyRadius, -bodyHeight / 2, 0),
                float3(bottomBodyRadius, -bodyHeight / 2, 0), 
                float3(bottomBodyRadius, -bodyHeight / 2, 0),
                float3(upperBodyRadius, bodyHeight / 2, 0)
            };
            var mainBody =  SceneLogic.Utils<V>.BezierCurves(bodyControl, slices, stacks);
            
            float3[] upperPartControl = {
                float3(upperBodyRadius, 0, 0),
                float3(upperBodyRadius, upperPartHeight * 0.2f, 0),
                float3(upperBodyRadius * 0.80f, upperPartHeight * 0.40f, 0),
                float3(upperBodyRadius * 0.75f, upperPartHeight * 0.50f, 0),
                float3(upperBodyRadius * 0.75f, upperPartHeight * 0.50f, 0),
                float3(upperBodyRadius * 0.75f, upperPartHeight * 0.50f, 0),
                float3(0, upperPartHeight*0.50f, 0),
                // float3(upperBodyRadius * 0.70f, upperPartHeight * 0.55f, 0),
                // float3(upperBodyRadius * 0.5f, upperPartHeight * 0.70f, 0),
                // float3(upperBodyRadius * 0.5f, upperPartHeight * 0.80f, 0),
                // float3(upperBodyRadius * 0.5f, upperPartHeight * 0.90f, 0),
                // float3(upperBodyRadius * 0.5f, upperPartHeight, 0)
            };
            var upperPart =  SceneLogic.Utils<V>.BezierCurves(upperPartControl, slices, stacks);
            upperPart = upperPart.Transform(Transforms.Translate(0, bodyHeight / 2, 0));

            var milk = new List<Mesh<V>>(){ mainBody, upperPart};
            
            SceneLogic.Utils<V>.ApplyTransforms(Transforms.Translate(0, -0.25f, 0), milk);

            return SceneLogic.Utils<V>.MorphMeshes(milk, Topology.Triangles);
        }

        public static Mesh<V> GetBottleLid(float radius, float height, int slices = 15, int stacks = 15)
        {
            float3[] control = {
                float3(0, 0, 0),
                float3(radius, 0, 0),
                float3(radius, 0, 0),
                float3(radius, 0, 0),
                float3(radius, 0, 0),
                float3(radius, 0, 0),
                float3(radius, height, 0),
                float3(radius, height, 0),
                float3(radius, height, 0),
                float3(radius, height, 0),
                float3(radius, height, 0),
                float3(0, height, 0),
            };
            var lid = SceneLogic.Utils<V>.BezierCurves(control, slices, stacks);
            return lid;
        }

        public static Mesh<V> GetCoffeMaker(int slices, int stacks)
        {
            var coffeMaker = CreateCoffeMaker(slices, stacks);

            coffeMaker = coffeMaker.Weld();
            coffeMaker.ComputeNormals();

            return coffeMaker;
        }

        public static Mesh<V> CreateCoffeMaker(int slices, int stacks)
        {
            float bodyHeight = 1.3f;
            float bottomRadius = 0.4f;
            float upperRadius = 0.8f * bottomRadius;

            float3[] controlLower = {
                float3(0, 0, 0),
                float3(bottomRadius * 0.95f, 0, 0),
                float3(bottomRadius * 0.95f, 0, 0),
                float3(bottomRadius * 0.95f, 0, 0),
                float3(bottomRadius * 0.95f, 0, 0),
                float3(bottomRadius * 0.9f, bodyHeight * (3/8f) + 0.05f, 0),
                float3(bottomRadius * 0.9f, bodyHeight * (3/8f) + 0.05f, 0),
                float3(bottomRadius * 0.9f, bodyHeight * (3/8f) + 0.05f, 0),
                float3(bottomRadius * 0.9f, bodyHeight * (3/8f) + 0.05f, 0),
                float3(0f, bodyHeight * (3/8f), 0) 
            };
            var lowerBody = SceneLogic.Utils<V>.BezierCurves(controlLower, slices, stacks);
            //return lowerBody;

            float3[] controlUpper = {
                float3(bottomRadius * 0.9f, bodyHeight * (3/8f) + 0.015f, 0),
                float3(upperRadius, bodyHeight, 0),
                //float3(upperRadius, bodyHeight, 0),
                //float3(upperRadius, bodyHeight, 0),
                //float3(upperRadius, bodyHeight, 0),
                //float3(0, bodyHeight, 0) 
            };
            var upperBody = SceneLogic.Utils<V>.BezierCurves(controlUpper, slices, stacks);
            
            var topBody = SceneLogic.Utils<V>.CreateCartesianOval(upperRadius, maxHeight:0.05f, zGrowth:0.125f, yGrowth:0.04f, minHeight:0, slices:slices, stacks:stacks);
            topBody = topBody.Transform(Transforms.Translate(0, bodyHeight, 0));

            var decorator1 = SceneLogic.Utils<V>.CreateCircle(0.05f, slices, stacks);
            var decorator2 = SceneLogic.Utils<V>.CreateCircle(0.025f, slices, stacks);

            decorator1 = decorator1.Transform(mul(Transforms.RotateZGrad(-90), Transforms.Translate(bottomRadius, bodyHeight*(3/8f) - 0.05f, 0)));

            var coffeMaker = new List<Mesh<V>>() {lowerBody, upperBody, topBody, decorator1};
            SceneLogic.Utils<V>.ApplyTransforms(Transforms.Translate(0, -1, 0), coffeMaker);
            return SceneLogic.Utils<V>.MorphMeshes(coffeMaker, Topology.Triangles);
        }

        public static Mesh<V> GetBalconyWindow(int slices, int stacks)
        {
            List<Mesh<V>> balconyWindows = new List<Mesh<V>>();
            float height = 0.25f, width = 0.5f;
            
            int count = 4;
            float sepparation = 0.3f;
            for (int i = 0; i < count; i++)
            {
                balconyWindows.Add(CreateBalconyWindow(height, width, slices, stacks, Transforms.Translate(0, 1 - sepparation*i, 0)));
                balconyWindows.Add(CreateBalconyWindow(height, width, slices, stacks, Transforms.Translate(0, 1 - sepparation*i, -0.6f)));
                //balconyWindows.Add(CreateBalconyWindow(height, width, slices, stacks, Transforms.Translate(0, 1 - sepparation*i, -1.2f)));
            }
            Mesh<V> windows = Utils<V>.MorphMeshes(balconyWindows, Topology.Triangles);
            windows = windows.Weld();
            windows.ComputeNormals();
            return windows;
        }

        private static Mesh<V> CreateBalconyWindow(float height, float width, int slices, int stacks, float4x4 transform)
        {
            float h = height/2;
            float w = width/2;

            return Utils<V>.CreatePlane(h, w, -h, -w, slices, stacks).Transform(mul(Transforms.RotateZGrad(90), transform));
        }

        public static (Mesh<V>, Mesh<V>) TestScene(int slices, int stacks)
        {
            var lightSource = Utils<V>.CreateCircle(0.5f, slices, stacks);
            lightSource = lightSource.Transform(Transforms.RotateZGrad(90));

            var glass = CreateBalconyWindow(0.5f, 0.5f, slices, stacks, Transforms.Translate(0.5f, 0, 0));
            glass = glass.Transform(mul(Transforms.RotateZGrad(90), Transforms.Translate(0.5f, 0, 0)));


            lightSource = lightSource.Weld();
            lightSource.ComputeNormals();

            glass = glass.Weld();
            glass.ComputeNormals();
            return (lightSource, glass);
        }
    }
}