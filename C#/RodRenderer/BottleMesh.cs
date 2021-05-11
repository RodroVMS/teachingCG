using System;
using System.Collections.Generic;
using GMath;
using Renderer;
using Rendering;
using static GMath.Gfx;


namespace Renderer
{
    public static class BottleScene<V> where V : struct, INormalVertex<V>, ICoordinatesVertex<V>
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

        public static Mesh<V> GetWaterBottleMesh()
        {
            var l = GetWaterBottleMeshList();
            Mesh<V> waterBottle = MorphMeshes(l, Topology.Triangles);
            waterBottle = waterBottle.Weld();
            waterBottle.ComputeNormals();
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

        public static Mesh<V> GetMilkBottleMesh()
        {
            var l = GetMilkBottleMeshList();
            Mesh<V> milkBottle = MorphMeshes(l, Topology.Triangles);
            milkBottle = milkBottle.Weld();
            milkBottle.ComputeNormals();
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

        public static Mesh<V> GetCoffeMakerMesh()
        {
            var l = GetCoffeMakerMeshList();
            Mesh<V> coffeMaker = MorphMeshes(l, Topology.Triangles);
            coffeMaker = coffeMaker.Weld();
            coffeMaker.ComputeNormals();
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

            return new Mesh<V>(newVertices, newIndices, topology);
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

        public static void ApplyTransforms(float4x4 transform, List<Mesh<V>> mesh_list)
        {
            for (int i = 0; i < mesh_list.Count; i++)
            {
                mesh_list[i] = mesh_list[i].Transform(transform);
            }
        }
    }
}