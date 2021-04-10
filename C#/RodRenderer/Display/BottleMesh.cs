using System;
using System.Collections.Generic;
using GMath;
using Renderer;
using Rendering;
using static GMath.Gfx;


namespace Renderer
{
    public class DisplayBottleMesh<V, P> where V : struct, IVertex<V> where P : struct, IProjectedVertex<P>
    {
        public static void DrawBottleMesh(Raster<V, P> render)
        {
            SetRenderSettings(render);

            var coffeMaker = GetCoffeMakerMesh();
            var milkBottle = GetMilkBottleMesh();
            var waterBottle = GetWaterBottleMesh();

            ApplyTransforms(Transforms.Translate(-0.7f, 0, 0.6f), coffeMaker);
            ApplyTransforms(Transforms.Translate(-0.7f, 0, -0.625f), milkBottle);

            DrawMeshAll(render, milkBottle);
            DrawMeshAll(render, coffeMaker);
            DrawMeshAll(render, waterBottle);
            render.RenderTarget.Save("test.rbm");
        }

        private static List<Mesh<V>> GetWaterBottleMesh()
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

            var mainBody = CreateCyllinder(radius: mainBodyRadius, max_height: mainBodyHeight / 2, min_height: -mainBodyHeight / 2);

            float3[] controlCone = { float3(mainBodyRadius, 0, 0), float3(neckRadius, upperConeHeight, 0) };
            var upperCone = BezierCurves(controlCone, slices: 15, stacks: 15);
            upperCone = upperCone.Transform(Transforms.Translate(0, mainBodyHeight / 2, 0));

            var neck = CreateCyllinder(radius: neckRadius, max_height: neckHeight);
            neck = neck.Transform(Transforms.Translate(0, mainBodyHeight / 2 + upperConeHeight, 0));

            float3[] controlBottom = { float3(bottomRadius, 0, 0), float3(mainBodyRadius, bottomHeight, 0) };
            var bottomCone = BezierCurves(controlBottom, slices: 20, stacks: 30);
            bottomCone = bottomCone.Transform(Transforms.Translate(0, -mainBodyHeight, 0));

            var lid = GetBottleLidMesh(lidRadius, lidHeight);
            ApplyTransforms(Transforms.Translate(0, mainBodyHeight / 2 + upperConeHeight + neckHeight, 0), lid);

            var waterBottle = new List<Mesh<V>>() { mainBody, upperCone, neck, bottomCone, lid[0], lid[1] };

            var bottom = CreateCircle(bottomRadius, slices: 15);

            ApplyTransforms(Transforms.Translate(0, 0.2f, 0), waterBottle);
            waterBottle.Add(bottom);

            ConvertTo(Topology.Lines, waterBottle);

            return waterBottle;

        }

        private static List<Mesh<V>> GetMilkBottleMesh()
        {
            float bodyHeight = 1.5f;
            float upperPartHeight = 0.7f * bodyHeight;

            float upperBodyRadius = 0.5f;
            float bottomRadius = 0.95f * upperBodyRadius;
            float upperRadius = 0.4f * upperBodyRadius;

            float3[] bodyControl = { float3(bottomRadius, -bodyHeight / 2, 0), float3(upperBodyRadius, bodyHeight / 2, 0) };
            var mainBody = BezierCurves(bodyControl);

            float a1 = 0.45f;
            float a2 = 0.8f;
            float3[] upperPartControl = { float3(upperBodyRadius, 0, 0), float3(upperBodyRadius * 1.1f, upperPartHeight * 0.2f, 0), float3(upperBodyRadius * a2, upperPartHeight * a1, 0) };
            var upperPart = BezierCurves(upperPartControl, slices: 15, stacks: 15);
            upperPart = upperPart.Transform(Transforms.Translate(0, bodyHeight / 2, 0));

            float b1 = 0.7f;
            float b2 = 0.6f;

            float3[] upperPart2Control = { float3(upperBodyRadius * a2, upperPartHeight * a1, 0), float3(upperBodyRadius * 0.65f, upperPartHeight * 0.6f, 0), float3(upperBodyRadius * b2, upperPartHeight * b1, 0) };
            var upperPart2 = BezierCurves(upperPart2Control, slices: 10, stacks: 10);
            upperPart2 = upperPart2.Transform(Transforms.Translate(0, bodyHeight / 2, 0));

            float3[] upperPart3Control = { float3(upperBodyRadius * b2, upperPartHeight * b1, 0), float3(upperBodyRadius * 0.5f, upperPartHeight, 0) };
            var upperPart3 = BezierCurves(upperPart3Control, slices: 10, stacks: 10);
            upperPart3 = upperPart3.Transform(Transforms.Translate(0, bodyHeight / 2, 0));

            var lid = GetBottleLidMesh(upperBodyRadius * 0.6f, 0.1f);
            ApplyTransforms(Transforms.Translate(0, bodyHeight / 2 + upperPartHeight, 0), lid);

            var milkBottle = new List<Mesh<V>>() { mainBody, upperPart, upperPart2, upperPart3, lid[0], lid[1] };
            ApplyTransforms(Transforms.Translate(0, -0.25f, 0), milkBottle);

            var bottom = CreateCircle(radius: bottomRadius, slices: 10, stacks: 10);

            milkBottle.Add(bottom);
            ConvertTo(Topology.Lines, milkBottle);

            return milkBottle;
        }

        private static List<Mesh<V>> GetCoffeMakerMesh()
        {
            float bodyHeight = 1.2f;

            float bottomRadius = 0.4f;
            float upperRadius = 0.8f * bottomRadius;

            float3[] controlBody = { float3(bottomRadius, -bodyHeight / 2, 0), float3(upperRadius, bodyHeight / 2, 0) };
            var body = BezierCurves(controlBody);

            var bottom = CreateCircle(bottomRadius, slices: 15, stacks: 15);

            var coffeMaker = GetCofferMakerLidMesh(upperRadius, bodyHeight / 2, slices: 10, stacks: 10);
            coffeMaker.Add(body);

            ApplyTransforms(Transforms.Translate(0, -0.4f, 0), coffeMaker);

            coffeMaker.Add(bottom);


            ConvertTo(Topology.Lines, coffeMaker);
            return coffeMaker;

        }

        private static List<Mesh<V>> GetCofferMakerLidMesh(float radius, float yPos = 1, int slices = 10, int stacks = 10)
        {
            float3 startPoint = lerp(float3(0, -yPos, 0), float3(radius * 1.2f, -yPos + 0.5f, 0), 0.8f);

            float3[] control = { startPoint, float3(radius * 1.2f, -yPos + 0.5f, 0) };
            var upperShape = BezierCurves(control, slices: 9, stacks: 30);
            upperShape = upperShape.Transform(Transforms.RotateXGrad(7f));
            upperShape = upperShape.Transform(Transforms.Translate(0, yPos + 0.15f, 0));

            var upperCircle = CreateCircle(radius, slices: 7, stacks: 7);
            upperCircle = upperCircle.Transform(Transforms.Translate(0, 1f + yPos, 0));

            var lid = new List<Mesh<V>>() { upperShape, upperCircle };
            return lid;
        }

        static Mesh<V> Intersect(Mesh<V> mesh, Func<float3, bool> f)
        {
            var indices = mesh.Indices;
            var vertices = mesh.Vertices;
            //System.Console.WriteLine(indices.Length);
            //System.Console.WriteLine(vertices.Length);
            //System.Console.Write(indices[0]); System.Console.Write(indices[1]); System.Console.Write(indices[2]); System.Console.WriteLine("\n");
            //System.Console.WriteLine(vertices[0]);

            var newIndices = new List<int>();
            var newVertices = new List<V>();
            for (int i = 0; i < indices.Length; i++)
            {
                var vertex = vertices[indices[i]];
                if (f(vertex.Position))
                {
                    newIndices.Add(indices[i]);
                    newVertices.Add(vertices[indices[i]]);
                }
            }

            return new Mesh<V>(newVertices.ToArray(), newIndices.ToArray(), mesh.Topology);
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

        static void SetRenderSettings(Raster<V, P> render)
        {
            render.ClearRT(float4(0, 0, 0.2f, 1));

            float4x4 viewMatrix = Transforms.LookAtLH(float3(5, 0f, 0), float3(0, 0f, 0), float3(0, 1, 0));
            //viewMatrix = Transforms.LookAtLH(float3(5, 0f, 0), float3(0, 0f, 0), float3(0, 1, 0));
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

        public static void DrawMeshAll(Raster<V, P> render, List<Mesh<V>> mesh_list)
        {
            for (int i = 0; i < mesh_list.Count; i++)
            {
                render.DrawMesh(mesh_list[i]);
            }
        }

    }

    //struct MyVertex : IVertex<MyVertex>
    //{
    //    public float3 Position { get; set; }
    //
    //    public MyVertex Add(MyVertex other)
    //    {
    //        return new MyVertex
    //        {
    //            Position = this.Position + other.Position,
    //        };
    //    }
    //
    //    public MyVertex Mul(float s)
    //    {
    //        return new MyVertex
    //        {
    //            Position = this.Position * s,
    //        };
    //    }
    //}
    //
    //struct MyProjectedVertex : IProjectedVertex<MyProjectedVertex>
    //{
    //    public float4 Homogeneous { get; set; }
    //
    //    public MyProjectedVertex Add(MyProjectedVertex other)
    //    {
    //        return new MyProjectedVertex
    //        {
    //            Homogeneous = this.Homogeneous + other.Homogeneous
    //        };
    //    }
    //
    //    public MyProjectedVertex Mul(float s)
    //    {
    //        return new MyProjectedVertex
    //        {
    //            Homogeneous = this.Homogeneous * s
    //        };
    //    }
    //}
}