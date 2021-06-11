using GMath;
using Rendering;
using System;
using System.Diagnostics;
using static GMath.Gfx;
using System.Collections.Generic;

namespace Renderer
{
    public class Program
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

        public struct Impulse
        {
            public float3 Direction;
            public float3 Ratio;
        }

        public struct ScatteredRay
        {
            public float3 Direction;
            public float3 Ratio;
            public float PDF;
        }

        public struct Material
        {
            public float3 Emissive;

            public Texture2D DiffuseMap;
            public Texture2D BumpMap;
            public Sampler TextureSampler;

            public float3 Diffuse;
            public float3 Specular;
            public float SpecularPower;
            public float RefractionIndex;

            // 4 float values with Diffuseness, Glossyness, Mirrorness, Fresnelness
            public float WeightDiffuse { get { return 1 - OneMinusWeightDiffuse; } set { OneMinusWeightDiffuse = 1 - value; } }
            float OneMinusWeightDiffuse; // This is intended for default values of the struct to work as 1, 0, 0, 0 weight initial settings
            public float WeightGlossy; 
            public float WeightMirror; 
            public float WeightFresnel;

            public float WeightNormalization
            {
                get { return max(0.0001f, WeightDiffuse + WeightGlossy + WeightMirror + WeightFresnel); }
            }

            public float3 EvalBRDF(PositionNormalCoordinate surfel, float3 wout, float3 win)
            {
                float3 diffuse = Diffuse * (DiffuseMap == null ? float3(1, 1, 1) : DiffuseMap.Sample(TextureSampler, surfel.Coordinates).xyz) / pi;
                float3 H = normalize(win + wout);
                float3 specular = Specular * pow(max(0, dot(H, surfel.Normal)), SpecularPower) * (SpecularPower + 2) / two_pi;
                return diffuse * WeightDiffuse / WeightNormalization + specular * WeightGlossy / WeightNormalization;
            }

            // Compute fresnel reflection component given the cosine of input direction and refraction index ratio.
            // Refraction can be obtained subtracting to one.
            // Uses the Schlick's approximation
            float ComputeFresnel(float NdotL, float ratio)
            {
                float f = pow((1 - ratio) / (1 + ratio), 2);
                return (f + (1.0f - f) * pow((1.0f - NdotL), 5));
            }

            public IEnumerable<Impulse> GetBRDFImpulses(PositionNormalCoordinate surfel, float3 wout)
            {
                if (!any(Specular))
                    yield break; // No specular => Ratio == 0

                float NdotL = dot(surfel.Normal, wout);
                // Check if ray is entering the medium or leaving
                bool entering = NdotL > 0;

                // Invert all data if leaving
                NdotL = entering ? NdotL : -NdotL;
                surfel.Normal = entering ? surfel.Normal : -surfel.Normal;
                float ratio = entering ? 1.0f / this.RefractionIndex : this.RefractionIndex / 1.0f; // 1.0f air refraction index approx

                // Reflection vector
                float3 R = reflect(wout, surfel.Normal);

                // Refraction vector
                float3 T = refract(wout, surfel.Normal, ratio);

                // Reflection quantity, (1 - F) will be the refracted quantity.
                float F = ComputeFresnel(NdotL, ratio);

                if (!any(T))
                    F = 1; // total internal reflection (produced with critical angles)

                if (WeightMirror + WeightFresnel * F > 0) // something is reflected
                    yield return new Impulse
                    {
                        Direction = R,
                        Ratio = Specular * (WeightMirror + WeightFresnel * F) / WeightNormalization
                    };

                if (WeightFresnel * (1 - F) > 0) // something to refract
                    yield return new Impulse
                    {
                        Direction = T,
                        Ratio = Specular * WeightFresnel * (1 - F) / WeightNormalization
                    };
            }

            /// <summary>
            /// Scatter a ray using the BRDF and Impulses
            /// </summary>
            public ScatteredRay Scatter(PositionNormalCoordinate surfel, float3 w)
            {
                float selection = random();
                float impulseProb = 0;

                foreach (var impulse in GetBRDFImpulses(surfel, w))
                {
                    float pdf = (impulse.Ratio.x + impulse.Ratio.y + impulse.Ratio.z) / 3;
                    if (selection < pdf) // this impulse is choosen
                        return new ScatteredRay
                        {
                            Ratio = impulse.Ratio,
                            Direction = impulse.Direction,
                            PDF = pdf
                        };
                    selection -= pdf;
                    impulseProb += pdf;
                }

                float3 wout = randomHSDirection(surfel.Normal);
                /// BRDF uniform sampling
                return new ScatteredRay
                {
                    Direction = wout,
                    Ratio = EvalBRDF(surfel, wout, w) * abs(dot(surfel.Normal, wout)),
                    PDF = (1 - impulseProb) / (2 * pi)
                };
            }
            
        }

        #region Scenes
        static void CreateBottleRayCastScene(Scene<PositionNormalCoordinate, Material> scene)
        {   
            int slices = 15, stacks = 15;
            (Mesh<PositionNormalCoordinate> waterBottle, Mesh<PositionNormalCoordinate> waterLid, Mesh<PositionNormalCoordinate> labelOut, Mesh<PositionNormalCoordinate> labelIn) = SceneLogic.MeshObjects<PositionNormalCoordinate>.GetWaterBottle(slices, stacks);
            float4x4 waterBottlePosition = Transforms.Identity;
            SceneLogic.TextureObjects.TextureCrystalBottle(waterBottle, waterBottlePosition, scene);
            SceneLogic.TextureObjects.TextureBottleLid(waterLid, waterBottlePosition, scene);
            SceneLogic.TextureObjects.TextureBottleLabel(labelOut, waterBottlePosition, scene);
            SceneLogic.TextureObjects.TextureBottleLabel(labelIn, waterBottlePosition, scene);

            (Mesh<PositionNormalCoordinate> milkBottle, Mesh<PositionNormalCoordinate> milkLid, Mesh<PositionNormalCoordinate> milk) = SceneLogic.MeshObjects<PositionNormalCoordinate>.GetMilkBottle(slices, stacks);
            float4x4 milkBottlePosition = Transforms.Translate(-0.7f, 0, -0.625f);
            // milkBottlePosition = Transforms.Identity;
            SceneLogic.TextureObjects.TextureCrystalBottle(milkBottle, milkBottlePosition, scene);
            SceneLogic.TextureObjects.TextureBottleLid(milkLid, milkBottlePosition, scene);
            SceneLogic.TextureObjects.TextureMilk(milk, milkBottlePosition, scene);

            Mesh<PositionNormalCoordinate> coffeMaker = SceneLogic.MeshObjects<PositionNormalCoordinate>.GetCoffeMaker(slices, stacks);
            float4x4 coffeMakerPosition = Transforms.Translate(-0.7f, 0, 0.6f);
            // coffeMakerPosition = Transforms.Identity;
            SceneLogic.TextureObjects.TextureCoffeMaker(coffeMaker, coffeMakerPosition, scene);

            Mesh<PositionNormalCoordinate> windows = SceneLogic.MeshObjects<PositionNormalCoordinate>.GetBalconyWindow(slices, stacks);
            windows = windows.Transform(Transforms.Scale(4, 4, 4));
            SceneLogic.TextureObjects.TextureBalconyWindows(windows, float3(5.1f, -1f, -2.3f), LightIntensity, scene);

            SceneLogic.TextureObjects.TextureWoodBoxScene(LightPosition, LightIntensity, scene);
        }
        #endregion

        /// <summary>
        /// Payload used to pick a color from a hit intersection
        /// </summary>
        struct RTRayPayload
        {
            public float3 Color;
            public int Bounces; // Maximum value of allowed bounces
        }

        struct PTRayPayload
        {
            public float3 Color; // Accumulated color to the viewer
            public float3 Importance; // Importance of the ray to the viewer
            public int Bounces; // Maximum value of allowed bounces
        }

        /// <summary>
        /// Payload used to flag when a ray was shadowed.
        /// </summary>
        struct ShadowRayPayload
        {
            public bool Shadowed;
        }

        // Scene Setup
        // static float3 CameraPosition = float3(0f, 0f, 0);
        // static float3 Target = float3(5.5f, 0.5f, 0);
        // static float3 LightPosition = float3(-2, 0, 0);
        // static float3 LightIntensity = float3(1, 1, 1) * 250;
        
        static float3 CameraPosition;
        static float3 Target;
        static float3 LightPosition;
        static float3 LightIntensity;
        static int bounces;
        static int res;

        public static void SetParameters(bool raytracing)
        {
            CameraPosition = float3(5f, 0.5f, 0);
            Target = float3(0, 0.5f, 0);
            res = 64;

            bounces = !raytracing ? 9: 4;
            LightPosition = !raytracing ? float3(5.1f, -1f, -2.3f): float3(5.1f, 1f, 0f);
            LightIntensity = !raytracing ? float3(1, 1, 1) * 150 :  float3(1, 1, 1) * 650;

            //Debug
            // CameraPosition = float3(-1, 0, 0);
            // Target = LightPosition;
        }

        static void Raytracing (Texture2D texture)
        {
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, Target, float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormalCoordinate, Material> scene = new Scene<PositionNormalCoordinate, Material>();
            // CreateTestScene(scene);
            CreateBottleRayCastScene(scene);
            // CreateRaycastScene(scene);

            // Raycaster to trace rays and check for shadow rays.
            Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material> shadower = new Raytracer<ShadowRayPayload, PositionNormalCoordinate, Material>();
            shadower.OnAnyHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref ShadowRayPayload payload)
            {
                if (any(material.Emissive))
                    return HitResult.Discard; // Discard light sources during shadow test.

                // If any object is found in ray-path to the light, the ray is shadowed.
                payload.Shadowed = true;
                // No neccessary to continue checking other objects
                return HitResult.Stop;
            };

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<RTRayPayload, PositionNormalCoordinate, Material> raycaster = new Raytracer<RTRayPayload, PositionNormalCoordinate, Material>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref RTRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = -normalize(context.GlobalRay.Direction);

                float3 L = (LightPosition - attribute.Position);
                float d = length(L);
                L /= d; // normalize direction to light reusing distance to light

                attribute.Normal = normalize(attribute.Normal);

                if (material.BumpMap != null)
                {
                    float3 T, B;
                    createOrthoBasis(attribute.Normal, out T, out B);
                    float3 tangentBump = material.BumpMap.Sample(material.TextureSampler, attribute.Coordinates).xyz * 2 - 1;
                    float3 globalBump = tangentBump.x * T + tangentBump.y * B + tangentBump.z * attribute.Normal;
                    attribute.Normal = globalBump;// normalize(attribute.Normal + globalBump * 5f);
                }

                float lambertFactor = max(0, dot(attribute.Normal, L));

                // Check ray to light...
                ShadowRayPayload shadow = new ShadowRayPayload();
                shadower.Trace(scene,
                    RayDescription.FromDir(attribute.Position + attribute.Normal * 0.001f, // Move an epsilon away from the surface to avoid self-shadowing 
                    L), ref shadow);

                float3 Intensity = (shadow.Shadowed ? 0.2f : 1.0f) * LightIntensity / (d * d);

                payload.Color = material.Emissive + material.EvalBRDF(attribute, V, L) * Intensity * lambertFactor; // direct light computation

                // Recursive calls for indirect light due to reflections and refractions
                if (payload.Bounces > 0)
                    foreach (var impulse in material.GetBRDFImpulses(attribute, V))
                    {
                        float3 D = impulse.Direction; // recursive direction to check
                        float3 facedNormal = dot(D, attribute.Normal) > 0 ? attribute.Normal : -attribute.Normal; // normal respect to direction

                        RayDescription ray = new RayDescription { Direction = D, Origin = attribute.Position + facedNormal * 0.001f, MinT = 0.0001f, MaxT = 10000 };

                        RTRayPayload newPayload = new RTRayPayload
                        {
                            Bounces = payload.Bounces - 1
                        };

                        raycaster.Trace(scene, ray, ref newPayload);

                        payload.Color += newPayload.Color * impulse.Ratio;
                    }
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref RTRayPayload payload)
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

                    RTRayPayload coloring = new RTRayPayload();
                    coloring.Bounces = bounces;

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4(coloring.Color, 1));
                }
        }

        static void Pathtracing(Texture2D texture, int pass)
        {
            // View and projection matrices
            float4x4 viewMatrix = Transforms.LookAtLH(CameraPosition, Target, float3(0, 1, 0));
            float4x4 projectionMatrix = Transforms.PerspectiveFovLH(pi_over_4, texture.Height / (float)texture.Width, 0.01f, 20);

            Scene<PositionNormalCoordinate, Material> scene = new Scene<PositionNormalCoordinate, Material>();
            CreateBottleRayCastScene(scene);
            // CreateRaycastScene(scene);

            // Raycaster to trace rays and lit closest surfaces
            Raytracer<PTRayPayload, PositionNormalCoordinate, Material> raycaster = new Raytracer<PTRayPayload, PositionNormalCoordinate, Material>();
            raycaster.OnClosestHit += delegate (IRaycastContext context, PositionNormalCoordinate attribute, Material material, ref PTRayPayload payload)
            {
                // Move geometry attribute to world space
                attribute = attribute.Transform(context.FromGeometryToWorld);

                float3 V = -normalize(context.GlobalRay.Direction);

                attribute.Normal = normalize(attribute.Normal);

                if (material.BumpMap != null)
                {
                    float3 T, B;
                    createOrthoBasis(attribute.Normal, out T, out B);
                    float3 tangentBump = material.BumpMap.Sample(material.TextureSampler, attribute.Coordinates).xyz * 2 - 1;
                    float3 globalBump = tangentBump.x * T + tangentBump.y * B + tangentBump.z * attribute.Normal;
                    attribute.Normal = globalBump;// normalize(attribute.Normal + globalBump * 5f);
                }

                ScatteredRay outgoing = material.Scatter(attribute, V);

                payload.Color += payload.Importance * material.Emissive;
                
                // Recursive calls for indirect light due to reflections and refractions
                if (payload.Bounces > 0)
                {
                    float3 D = outgoing.Direction; // recursive direction to check
                    float3 facedNormal = dot(D, attribute.Normal) > 0 ? attribute.Normal : -attribute.Normal; // normal respect to direction

                    RayDescription ray = new RayDescription { Direction = D, Origin = attribute.Position + facedNormal * 0.001f, MinT = 0.0001f, MaxT = 10000 };

                    payload.Importance *= outgoing.Ratio / outgoing.PDF;
                    payload.Bounces--;

                    raycaster.Trace(scene, ray, ref payload);
                }
            };
            raycaster.OnMiss += delegate (IRaycastContext context, ref PTRayPayload payload)
            {
                payload.Color = float3(0, 0, 0); // Blue, as the sky.
            };

            /// Render all points of the screen
            for (int px = 0; px < texture.Width; px++)
                for (int py = 0; py < texture.Height; py++)
                {
                    int progress = (px * texture.Height + py);
                    if (progress % 10000 == 0)
                    {
                        Console.Write("\r" + progress * 100 / (float)(texture.Width * texture.Height) + "%            ");
                    }

                    RayDescription ray = RayDescription.FromScreen(px + 0.5f, py + 0.5f, texture.Width, texture.Height, inverse(viewMatrix), inverse(projectionMatrix), 0, 1000);

                    float4 accum = texture.Read(px, py) * pass;
                    PTRayPayload coloring = new PTRayPayload();
                    coloring.Importance = float3(1, 1, 1);
                    coloring.Bounces = bounces;

                    raycaster.Trace(scene, ray, ref coloring);

                    texture.Write(px, py, float4((accum.xyz + coloring.Color) / (pass + 1), 1));
                }
        }

        public static void Main()
        {
            // Texture to output the image.
            bool raytracing = false;
            SetParameters(raytracing);


            Texture2D texture = new Texture2D(res, res);
            if (raytracing)
            {
                Stopwatch stopwatch = new Stopwatch();

                stopwatch.Start();

                Raytracing(texture);

                stopwatch.Stop();

                texture.Save("test.rbm");

                Console.WriteLine("Done. Rendered in " + stopwatch.ElapsedMilliseconds + " ms");
            }
            else
            {
                int pass = 1;
                while (true)
                {
                    Console.WriteLine("Pass: " + pass);
                    Pathtracing(texture, pass);
                    texture.Save("test.rbm");
                    pass++;

                    if (pass % 500 == 0)
                        texture.Save($"path{pass}.rbm");
                }
            }
        }
    }
}
