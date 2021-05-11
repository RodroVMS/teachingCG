using System;
using System.Diagnostics;
using GMath;
using Rendering;
using static GMath.Gfx;

namespace Renderer
{
    class Program
    {
        public struct PositionNormalCoordinate : INormalVertex<PositionNormalCoordinate>, ICoordinatesVertex<PositionNormalCoordinate>
        {
            public float3 Position { get; set; }
            public float3 Normal { get; set; }

            public float2 Coordinates { get; set; }

            public PositionNormalCoordinate Add(PositionNormalCoordinate other)
            {
                return new PositionNormalCoordinate
                {
                    Position = this.Position + other.Position,
                    Normal = this.Normal + other.Normal,
                    Coordinates = this.Coordinates + other.Coordinates
                };
            }

            public PositionNormalCoordinate Mul(float s)
            {
                return new PositionNormalCoordinate
                {
                    Position = this.Position * s,
                    Normal = this.Normal * s,
                    Coordinates = this.Coordinates * s
                };
            }

            public PositionNormalCoordinate Transform(float4x4 matrix)
            {
                float4 p = float4(Position, 1);
                p = mul(p, matrix);

                float4 n = float4(Normal, 0);
                n = mul(n, matrix);

                return new PositionNormalCoordinate
                {
                    Position = p.xyz / p.w,
                    Normal = n.xyz,
                    Coordinates = Coordinates
                };
            }
        }

        public struct Material
        {
            public Texture2D Diffuse;

            public float3 Specular;
            public float SpecularPower;

            public float Glossyness;

            public Sampler TextureSampler;

            public float3 EvalBRDF(PositionNormalCoordinate surfel, float3 wout, float3 win)
            {
                float3 diffuse = Diffuse.Sample(TextureSampler, surfel.Coordinates).xyz / pi;
                float3 H = normalize(win + wout);
                float3 specular = Specular * pow(max(0, dot(H, surfel.Normal)), SpecularPower) * (SpecularPower + 2) / two_pi;
                return diffuse * (1 - Glossyness) + specular * Glossyness;
            }
        }

        #region Scenes

        static void CreateRaycastScene(Scene<PositionNormalCoordinate, Material> scene)
        {
            Texture2D ballTexture = new Texture2D(1, 1);
            ballTexture.Write(0, 0, float4(1, 1, 0, 1)); // yellow color

            Texture2D planeTexture = new Texture2D(2, 2);
            planeTexture.Write(0, 0, float4(1, 0, 0, 1)); // red cell
            planeTexture.Write(0, 1, float4(1, 1, 0, 1)); // yellow cell
            planeTexture.Write(1, 0, float4(0, 1, 0, 1)); // green cell
            planeTexture.Write(1, 1, float4(0, 0, 1, 1)); // blue cell

            // Adding elements of the scene
            scene.Add(Raycasting.UnitarySphere.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(atan2(a.z, a.x) * 0.5f / pi + 0.5f, a.y), Normal = normalize(a) }),
                new Material { Diffuse = ballTexture, TextureSampler = new Sampler { Wrap = WrapMode.Repeat } },
                Transforms.Translate(0, 1, 0));
            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x, a.z), Normal = float3(0, 1, 0) }),
                new Material { Diffuse = planeTexture, TextureSampler = new Sampler { Wrap = WrapMode.Repeat } },
                Transforms.Identity);
        }

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

        static Mesh<PositionNormalCoordinate> CreateModel()
        {
            // Revolution Sample with Bezier
            float3[] contourn =
            {
                float3(0, -.5f,0),
                float3(0.8f, -0.5f,0),
                float3(1f, -0.2f,0),
                float3(0.6f,1,0),
                float3(0,1,0)
            };

            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            var model = Manifold<PositionNormalCoordinate>.Revolution(20, 30, t => EvalBezier(contourn, t), float3(0, 1, 0)).Weld();
            model.ComputeNormals();
            return model;
        }

        static Mesh<PositionNormalCoordinate> CreateSea()
        {
            /// Creates the model using a revolution of a bezier.
            /// Only Positions are updated.
            //var model = Manifold<PositionNormalCoordinate>.Surface(30, 20, (u, v) => 2 * float3(2 * u - 1, sin(u * 15) * 0.0f + cos(v * 13 + u * 16) * 0.0f, 2 * v - 1));//.Weld();
            var model = Manifold<PositionNormalCoordinate>.Surface(30, 20, (u, v) => 2 * float3(2 * u - 1, 0.0f, 2 * v - 1));//.Weld();
            model.ComputeNormals();
            return model;
        }

        static void CreateMeshScene(Scene<PositionNormalCoordinate, Material> scene)
        {
            Texture2D ballTexture = new Texture2D(1, 1);
            ballTexture.Write(0, 0, float4(1, 1, 0, 1)); // yellow color

            Texture2D planeTexture = new Texture2D(2, 2);
            planeTexture.Write(0, 0, float4(1, 0, 0, 1)); // red cell
            planeTexture.Write(0, 1, float4(1, 1, 0, 1)); // yellow cell
            planeTexture.Write(1, 0, float4(0, 1, 0, 1)); // green cell
            planeTexture.Write(1, 1, float4(0, 0, 1, 1)); // blue cell

            var sea = CreateSea();
            scene.Add(sea.AsRaycast(RaycastingMeshMode.Grid), new Material
            {
                Diffuse = planeTexture,
                Specular = float3(1, 1, 1),
                SpecularPower = 60,
                Glossyness = 0.2f,
                TextureSampler = new Sampler
                {
                    Wrap = WrapMode.Repeat,
                    MinMagFilter = Filter.Point
                }
            }, Transforms.Identity);

            var egg = CreateModel();
            scene.Add(egg.AsRaycast(RaycastingMeshMode.Grid), new Material
            {
                Diffuse = ballTexture,
                Specular = float3(1, 1, 1),
                SpecularPower = 60,
                Glossyness = 0.2f,
                TextureSampler = new Sampler
                {
                    Wrap = WrapMode.Clamp,
                    MipFilter = Filter.Linear
                }
            }, Transforms.Translate(0, 0.5f, 0));
        }

        #endregion

        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        struct MyRayPayload
        {
            public float3 Color;
        }

        /// <summary>
        /// Payload used to flag when a ray was shadowed.
        /// </summary>
        struct ShadowRayPayload
        {
            public bool Shadowed;
        }

        static void CreateBottleScene(Scene<PositionNormalCoordinate, Material> scene)
        {
            Texture2D planeTexture = new Texture2D(2, 2);
            planeTexture.Write(0, 0, float4(1, 0, 0, 0.1f)); // red cell
            planeTexture.Write(0, 1, float4(1, 1, 0, 0.4f)); // yellow cell
            planeTexture.Write(1, 0, float4(0, 1, 0, 0.7f)); // green cell
            planeTexture.Write(1, 1, float4(0, 0, 1, 1)); // blue cell

            var waterBottle = BottleScene<PositionNormalCoordinate>.GetWaterBottleMesh();
            scene.Add(waterBottle.AsRaycast(RaycastingMeshMode.Grid), new Material
            {
                Diffuse = planeTexture,
                Specular = float3(1, 1, 1),
                SpecularPower = 60,
                Glossyness = 0.2f,
                TextureSampler = new Sampler
                {
                    Wrap = WrapMode.Repeat,
                    MinMagFilter = Filter.Point
                }
            }, Transforms.Identity);

            var milkBottle = BottleScene<PositionNormalCoordinate>.GetMilkBottleMesh();
            scene.Add(milkBottle.AsRaycast(RaycastingMeshMode.Grid), new Material
            {
                Diffuse = planeTexture,
                Specular = float3(1, 1, 1),
                SpecularPower = 60,
                Glossyness = 0.2f,
                TextureSampler = new Sampler
                {
                    Wrap = WrapMode.Repeat,
                    MinMagFilter = Filter.Point
                }
            }, Transforms.Translate(-0.7f, 0, -0.625f));


            var coffeMaker = BottleScene<PositionNormalCoordinate>.GetCoffeMakerMesh();
            scene.Add(coffeMaker.AsRaycast(RaycastingMeshMode.Grid), new Material
            {
                Diffuse = planeTexture,
                Specular = float3(1, 1, 1),
                SpecularPower = 60,
                Glossyness = 0.2f,
                TextureSampler = new Sampler
                {
                    Wrap = WrapMode.Repeat,
                    MinMagFilter = Filter.Point
                }
            }, Transforms.Translate(-0.7f, 0, 0.6f));


            var floor = Manifold<PositionNormalCoordinate>.Surface(30, 20, (u, v) => 2 * float3(2 * u - 1, sin(u * 15) * 0.02f + cos(v * 13 + u * 16) * 0.03f, 2 * v - 1)).Weld();
            floor.ComputeNormals();
            scene.Add(floor.AsRaycast(RaycastingMeshMode.Grid), new Material
            {
                Diffuse = planeTexture,
                Specular = float3(1, 1, 1),
                SpecularPower = 60,
                Glossyness = 0.2f,
                TextureSampler = new Sampler
                {
                    Wrap = WrapMode.Repeat,
                    MinMagFilter = Filter.Point
                }
            }, Transforms.Translate(0, -0.5f, 0));
        }


        static void RaycastingMesh(Texture2D texture)
        {
            // Scene Setup
            float3 CameraPosition = float3(5, 0.5f, 0);
            float3 LightPosition = float3(6, 3, 1);
            float3 LightIntensity = float3(1, 1, 1) * 100;

            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, float3(0, 1, 0), float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormalCoordinate, Material> scene = new Scene<PositionNormalCoordinate, Material>();
            //CreateRaycastScene(scene);
            CreateBottleScene(scene);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material> shadower = new Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref ShadowRayPayload payload)
            {
                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<MyRayPayload, PositionNormalCoordinate, Material> raycaster = new Raytracer<MyRayPayload, PositionNormalCoordinate, Material>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref MyRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = normalize(CameraPosition - attribute.Position);
                float3 L = (LightPosition - attribute.Position);
                float d = length(L);
                L /= d; // normalize direction to light reusing distance to light

                attribute.Normal = normalize(attribute.Normal);

                float lambertFactor = max(0, dot(attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.2f : 1.0f) * LightIntensity / (d * d);

                payload.Color = material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor;
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref MyRayPayload payload)
            {
                payload.Color = float3(0, 0, 0); // Blue, as the sky.
            };

            /// Render all points of the screen
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

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }

        static void Main(string[] args)
        {
            Stopwatch stopwatch = new Stopwatch();

            stopwatch.Start();

            // Texture to output the image.
            Texture2D texture = new Texture2D(512, 512);

            RaycastingMesh(texture);

            stopwatch.Stop();

            texture.Save("test.rbm");

            Console.WriteLine("Done. Rendered in " + stopwatch.ElapsedMilliseconds + " ms");
        }
    }
}
