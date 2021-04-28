using System;
using System.Collections.Generic;
using GMath;
using Renderer;
using Rendering;
using static GMath.Gfx;


namespace Renderer
{
    public static class DisplayBottleMesh<V> where V : struct, INormalVertex<V>
    {
        struct ShadowRayPayload
        {
            public bool Shadowed;
        }

        struct MyRayPayload
        {
            public float3 Color;
        }

        public delegate float3 BRDF(float3 N, float3 Lin, float3 Lout);

        static BRDF LambertBRDF(float3 diffuse)
        {
            return (N, Lin, Lout) => diffuse / pi;
        }

        static BRDF BlinnBRDF(float3 specular, float power)
        {
            return (N, Lin, Lout) =>
            {
                float3 H = normalize(Lin + Lout);
                return specular * pow(max(0, dot(H, N)), power) * (power + 2) / two_pi;
            };
        }

        static BRDF Mixture(BRDF f1, BRDF f2, float alpha)
        {
            return (N, Lin, Lout) => lerp(f1(N, Lin, Lout), f2(N, Lin, Lout), alpha);
        }

        public static void DrawBottleRayCastingMesh(Texture2D texture)
        {
            float3 CameraPosition = float3(5, 0f, 0); //float3(3, 2f, 4);
            float3 LightPosition = float3(4f, 5, 1f);
            float3 LightIntensity = float3(1, 1, 1) * 100;

            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<V> scene = new Scene<V>();
            var coffeMaker = GetCoffeMakerMesh();
            var milkBottle = GetMilkBottleMesh();
            var waterBottle = GetWaterBottleMesh();
            var floor = Manifold<V>.Surface(30, 20, (u, v) => 2 * float3(2 * u - 1, sin(u * 15) * 0.02f + cos(v * 13 + u * 16) * 0.03f, 2 * v - 1)).Weld();
            floor = floor.Transform(Transforms.Translate(0, -0.5f, 0));

            coffeMaker = coffeMaker.Transform(Transforms.Translate(-0.7f, 0, 0.6f));
            milkBottle = milkBottle.Transform(Transforms.Translate(-0.7f, 0, -0.625f));

            AddToScene(scene, new List<Mesh<V>>() { waterBottle, milkBottle, coffeMaker, floor });

            BRDF[] brdfs =
            {
                Mixture(LambertBRDF(float3(0.9f,0.9f,0.9f)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                Mixture(LambertBRDF(float3(0.9f,0.7f,0.7f)), BlinnBRDF(float3(1f,1f,1f), 70), 0.3f),
                Mixture(LambertBRDF(float3(0.6f,0.6f,0.6f)), BlinnBRDF(float3(1,1,1), 70), 0.3f),
                LambertBRDF(float3(0.4f,0.5f,1f)),
            };

            Raytracer<ShadowRayPayload, V> shadower = new Raytracer<ShadowRayPayload, V>();
            shadower.OnAnyHit += delegate (IRaycastContext context, V attribute, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            Raytracer<MyRayPayload, PositionNormal> raycaster = new Raytracer<MyRayPayload, PositionNormal>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormal attribute, ref MyRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = normalize(CameraPosition - attribute.Position);
                float3 L = (LightPosition - attribute.Position);
                float d = length(L);
                L /= d; // normalize direction to light reusing distance to light

                float3 N = normalize(attribute.Normal);

                float lambertFactor = max(0, dot(N, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + N * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.0f : 1.0f) * LightIntensity / (d * d);

                payload.Color = brdfs[context.GeometryIndex](N, L, V) * Intensity * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 1); // Blue, as the sky.
            };

            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++)
                {
                    int progress = (px * texture.Height + py);
                    if (progress % 1000 == 0)
                    {
                        Console.Write("\r" + progress * 100 / (float)(texture.Width * texture.Height) + "%            ");
                    }

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    MyRayPayload coloring = new MyRayPayload();

                    raycaster.Trace(scene as Scene<PositionNormal>, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }

        private static void AddToScene(Scene<V> scene, List<Mesh<V>> fig_list)
        {
            foreach (var fig in fig_list)
            {
                var new_fig = fig.Weld();
                new_fig.ComputeNormals();
                scene.Add(new_fig.AsRaycast(RaycastingMeshMode.Grid), Transforms.Identity);
            }
        }

        public static void DrawBottleMesh<P>(Raster<V, P> render) where P : struct, IProjectedVertex<P>
        {
            SetRenderSettings(render);

            var coffeMaker = GetCoffeMakerMesh();
            var milkBottle = GetMilkBottleMesh();
            var waterBottle = GetWaterBottleMesh();

            coffeMaker = coffeMaker.Transform(Transforms.Translate(-0.7f, 0, 0.6f));
            milkBottle = milkBottle.Transform(Transforms.Translate(-0.7f, 0, -0.625f));

            render.DrawMesh(milkBottle);
            render.DrawMesh(coffeMaker);
            render.DrawMesh(waterBottle);
            render.RenderTarget.Save("test.rbm");
        }

        private static Mesh<V> GetWaterBottleMesh()
        {
            var l = GetWaterBottleMeshList();
            Mesh<V> waterBottle = Manifold<V>.MorphMeshes(l, Topology.Triangles);
            return waterBottle;
        }

        private static List<Mesh<V>> GetWaterBottleMeshList()
        {
            float mainBodyRadius = 0.5f;
            float lidRadius = 0.5f * mainBodyRadius;
            float neckRadius = 0.425f * mainBodyRadius;
            float bottomRadius = 0.9f * mainBodyRadius;

            float mainBodyHeight = 1.2f;
            float lidHeight = 0.1f * mainBodyHeight;
            float upperConeHeight = 0.2f * mainBodyHeight;
            float neckHeight = 0.5f * mainBodyHeight;
            float bottomHeight = 0.5f * mainBodyHeight;

            float3[] controlCone = { float3(mainBodyRadius, 0, 0), float3(mainBodyRadius, upperConeHeight * 0.7f, 0), float3(neckRadius, upperConeHeight * 0.8f, 0), float3(neckRadius, upperConeHeight, 0) };
            var upperCone = BezierCurves(controlCone, slices: 15, stacks: 15);
            upperCone = upperCone.Transform(Transforms.Translate(0, mainBodyHeight / 2, 0));

            var neck = CreateCyllinder(radius: neckRadius, max_height: neckHeight, slices: 15, stacks: 15);
            neck = neck.Transform(Transforms.Translate(0, mainBodyHeight / 2 + upperConeHeight, 0));

            float3[] controlBody = {
                float3(bottomRadius, 0, 0),
                float3(mainBodyRadius, bottomHeight * 0.7f, 0),
                float3(mainBodyRadius, bottomHeight * 0.8f, 0),
                float3(mainBodyRadius, bottomHeight, 0),
                float3(mainBodyRadius, bottomHeight + mainBodyHeight / 2, 0),
                float3(mainBodyRadius, bottomHeight + mainBodyHeight, 0),
            };
            var body = BezierCurves(controlBody, slices: 15, stacks: 15);
            body = body.Transform(Transforms.Translate(0, -mainBodyHeight, 0));

            var lid = GetBottleLidMesh(lidRadius, lidHeight, slices: 15, stacks: 15);
            ApplyTransforms(Transforms.Translate(0, mainBodyHeight / 2 + upperConeHeight + neckHeight, 0), lid);

            var waterBottle = new List<Mesh<V>>() { upperCone, neck, body, lid[0], lid[1] };
            var bottom = CreateCircle(bottomRadius, slices: 15, stacks: 15);

            ApplyTransforms(Transforms.Translate(0, 0.2f, 0), waterBottle);
            waterBottle.Add(bottom);

            //waterBottle = new List<Mesh<V>>() { upperCone };
            ApplyTransforms(Transforms.Translate(0, 0.5f, 0), waterBottle);
            return waterBottle;

        }

        private static Mesh<V> GetMilkBottleMesh()
        {
            var l = GetMilkBottleMeshList();
            Mesh<V> milkBottle = Manifold<V>.MorphMeshes(l, Topology.Triangles);
            return milkBottle;
        }

        private static List<Mesh<V>> GetMilkBottleMeshList()
        {
            float bodyHeight = 1.5f;
            float upperPartHeight = 0.7f * bodyHeight;

            float upperBodyRadius = 0.5f;
            float bottomBodyRadius = 0.95f * upperBodyRadius;
            float neckRadius = 0.4f * upperBodyRadius;

            float3[] bodyControl = { float3(bottomBodyRadius, -bodyHeight / 2, 0), float3(upperBodyRadius, bodyHeight / 2, 0) };
            var mainBody = BezierCurves(bodyControl, slices: 15, stacks: 15);

            float a1 = 0.45f;
            float a2 = 0.8f;
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
            var upperPart = BezierCurves(upperPartControl, slices: 15, stacks: 15);
            upperPart = upperPart.Transform(Transforms.Translate(0, bodyHeight / 2, 0));


            var lid = GetBottleLidMesh(upperBodyRadius * 0.6f, 0.1f, slices: 15, stacks: 15);
            ApplyTransforms(Transforms.Translate(0, bodyHeight / 2 + upperPartHeight, 0), lid);

            var milkBottle = new List<Mesh<V>>() { mainBody, upperPart, lid[0], lid[1] };
            ApplyTransforms(Transforms.Translate(0, -0.25f, 0), milkBottle);

            var bottom = CreateCircle(radius: bottomBodyRadius, slices: 15, stacks: 15);

            milkBottle.Add(bottom);

            ApplyTransforms(Transforms.Translate(0, 0.5f, 0), milkBottle);

            return milkBottle;
        }

        private static Mesh<V> GetCoffeMakerMesh()
        {
            var l = GetCoffeMakerMeshList();
            Mesh<V> coffeMaker = Manifold<V>.MorphMeshes(l, Topology.Triangles);
            return coffeMaker;
        }

        private static List<Mesh<V>> GetCoffeMakerMeshList()
        {
            float bodyHeight = 1.2f;

            float bottomRadius = 0.4f;
            float upperRadius = 0.8f * bottomRadius;

            float3[] controlBody = { float3(bottomRadius, -bodyHeight / 2, 0), float3(upperRadius, bodyHeight / 2, 0) };
            var body = BezierCurves(controlBody, slices: 15, stacks: 15);

            var bottom = CreateCircle(bottomRadius, slices: 15, stacks: 15);

            var coffeMaker = GetCofferMakerLidMesh(upperRadius, bodyHeight / 2, slices: 15, stacks: 15);
            coffeMaker.Add(body);

            ApplyTransforms(Transforms.Translate(0, -0.4f, 0), coffeMaker);

            coffeMaker.Add(bottom);
            ApplyTransforms(Transforms.Translate(0, 0.5f, 0), coffeMaker);

            return coffeMaker;

        }

        private static List<Mesh<V>> GetCofferMakerLidMesh(float radius, float yPos = 1, int slices = 10, int stacks = 10)
        {
            float3 startPoint = lerp(float3(0, -yPos, 0), float3(radius * 1.2f, -yPos + 0.5f, 0), 0.8f);

            float3[] control = { startPoint, float3(radius * 1.2f, -yPos + 0.5f, 0) };
            var upperShape = BezierCurves(control, slices: slices, stacks: stacks);
            upperShape = upperShape.Transform(Transforms.RotateXGrad(7f));
            upperShape = upperShape.Transform(Transforms.Translate(0, yPos + 0.15f, 0));

            var upperCircle = CreateCircle(radius, slices: slices, stacks: stacks);
            upperCircle = upperCircle.Transform(Transforms.Translate(0, 1f + yPos, 0));

            var lid = new List<Mesh<V>>() { upperShape, upperCircle };
            return lid;
        }

        private static List<Mesh<V>> GetBottleLidMesh(float radius, float height, int slices = 10, int stacks = 10)
        {
            var lidTop = CreateCircle(radius, slices: slices, stacks: stacks);
            var lidBody = CreateCyllinder(radius, height, slices: slices, stacks: stacks);

            lidTop = lidTop.Transform(Transforms.Translate(0, 1f + height, 0));

            return new List<Mesh<V>>() { lidTop, lidBody };
        }

        static Mesh<V> BezierCurves(float3[] control, int slices = 30, int stacks = 30)
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

        static Mesh<V> CreatePlane(float maxSideA, float maxSideB, float minSideA = 0, float minSideB = 0, int slices = 30, int stacks = 30)
        {
            return Manifold<V>.Surface(slices, stacks, (u, v) =>
            {
                float x = u * (maxSideA - minSideA) + minSideA;
                float z = v * (maxSideB - minSideB) + minSideB;

                return float3(x, -1, z);
            });
        }

        static Mesh<V> CreateCircle(float radius, int slices = 30, int stacks = 30)
        {
            return Manifold<V>.Surface(slices, stacks, (u, v) =>
            {
                float theta = u * pi * 2;
                float beta = v * pi * 2;

                float x = v * radius * cos(theta);
                float z = v * radius * sin(theta);

                return float3(x, -1, z);
            });
        }

        static Mesh<V> CreateCone(float radius, float max_height, float min_height = 0, float top = int.MaxValue, int slices = 30, int stacks = 30)
        {
            return Manifold<V>.Surface(slices, stacks, (u, v) =>
            {
                float max_v = -min_height;
                float min_v = -max_height;

                float alpha = u * 2 * pi;

                float x = v * radius * cos(alpha);
                float y = v * (max_v - min_v) + min_v;
                float z = v * radius * sin(alpha);

                return float3(-x, min(-y, top), -z);
            });
        }

        static Mesh<V> CreateCyllinder(float radius, float max_height, float min_height = 0, int slices = 30, int stacks = 30)
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

        static Mesh<V> CreateHiperboloid(float radius, float max_height, float min_height = 4, int direction = 1, int resX = 30, int resY = 30)
        {
            max_height = max_height == 0 ? 4 : max_height;
            min_height = min_height == 0 ? 4 : min_height;

            return Manifold<V>.Surface(30, 30, (u, v) =>
            {
                float uu = u * 2 * pi;
                float vv = pi / max_height - v * pi / min_height; //Sets upper and lower limmits

                float x = radius * cosh(vv) * cos(uu);
                float z = radius * cosh(vv) * sin(uu);
                float y = sinh(vv);

                return float3(x, direction * y, z);
            });
        }

        static void SetRenderSettings<P>(Raster<V, P> render) where P : struct, IProjectedVertex<P>
        {
            render.ClearRT(float4(0, 0, 0.2f, 1));

            float4x4 viewMatrix = Transforms.LookAtLH(float3(5, 0f, 0), float3(0, 0f, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, render.RenderTarget.Height / (float)render.RenderTarget.Width, 0.01f, 40);

            // Define a vertex shader that projects a vertex into the NDC.
            render.VertexShader = v =>
            {
                float4 hPosition = float4(v.Position, 1);
                hPosition = mul(hPosition, viewMatrix);
                hPosition = mul(hPosition, projectionMatrix);
                return new P { Homogeneous = hPosition };
            };

            // Define a pixel shader that colors using a constant value
            render.PixelShader = p =>
            {
                return float4(p.Homogeneous.x / 1024.0f, p.Homogeneous.y / 512.0f, 1, 1);
            };
        }

        public static void ApplyTransforms(float4x4 transform, List<Mesh<V>> mesh_list)
        {
            for (int i = 0; i < mesh_list.Count; i++)
            {
                mesh_list[i] = mesh_list[i].Transform(transform);
            }
        }

        public static void ConvertTo(Topology topology, List<Mesh<V>> mesh_list)
        {
            for (int i = 0; i < mesh_list.Count; i++)
            {
                mesh_list[i] = mesh_list[i].ConvertTo(topology);
            }
        }

        public static void DrawMeshAll<P>(Raster<V, P> render, List<Mesh<V>> mesh_list) where P : struct, IProjectedVertex<P>
        {
            for (int i = 0; i < mesh_list.Count; i++)
            {
                render.DrawMesh(mesh_list[i]);
            }
        }
    }
}