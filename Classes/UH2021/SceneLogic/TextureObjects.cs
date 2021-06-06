using Rendering;
using static Renderer.Program;
using GMath;
using static GMath.Gfx;

namespace SceneLogic
{
    public static class TextureObjects
    {
        public static void TextureCrystalBottle(
            Mesh<PositionNormalCoordinate> bottle,
            float4x4 transform,
            Scene<PositionNormalCoordinate, Material> scene)
        {
            scene.Add(bottle.AsRaycast(RaycastingMeshMode.Grid), 
            new Material {
                Specular = float3(1, 1, 1),
                SpecularPower = 260,

                WeightDiffuse = 0f,
                WeightFresnel = 1f,
                RefractionIndex = 1.2f//1.6f,
            }, transform);
        }

        public static void TextureMilk(
            Mesh<PositionNormalCoordinate> milk,
            float4x4 transform,
            Scene<PositionNormalCoordinate, Material> scene
        )
        {
            Texture2D texture = new Texture2D(1, 1);
            texture.Write(0, 0, float4(1, 1f, 1f, 1));

            scene.Add(milk.AsRaycast(RaycastingMeshMode.Grid),
            new Material {
                DiffuseMap = texture,
                Diffuse = float3(1, 1, 1),
                TextureSampler = new Sampler { 
                    Wrap = WrapMode.Repeat,
                    MinMagFilter = Filter.Linear 
                    },
                
            }, transform);
        }

        public static void TextureCoffeMaker(
            Mesh<PositionNormalCoordinate> coffeMaker,
            float4x4 transform,
            Scene<PositionNormalCoordinate, Material> scene
        )
        {
            Texture2D steelTexture = Texture2D.LoadFromFile("./Textures/SteelTexture2.jpeg");

            scene.Add(coffeMaker.AsRaycast(RaycastingMeshMode.Grid),
            new Material {
                DiffuseMap = steelTexture,
                    Diffuse = float3(1, 1, 1),
                    TextureSampler = new Sampler { 
                        Wrap = WrapMode.Repeat,
                        MinMagFilter = Filter.Linear 
                    },
                    Specular = float3(0.1f, 0.1f, 0.1f),
                    SpecularPower = 350,
                    WeightDiffuse = 1f,
                    WeightMirror = 1.0f,
            }, transform);
        }

        public static void TextureBottleLid(
            Mesh<PositionNormalCoordinate> lid,
            float4x4 transform,
            Scene<PositionNormalCoordinate, Material> scene
        )
        {
            Texture2D lidTexture = new Texture2D(1, 1);
            lidTexture.Write(0, 0, float4(0.01f, 0.01f, 0.01f, 1));

            scene.Add(lid.AsRaycast(RaycastingMeshMode.Grid),
            new Material {
                DiffuseMap = lidTexture,
                Diffuse = float3(1, 1, 1),
                TextureSampler = new Sampler { 
                    Wrap = WrapMode.Repeat,
                    MinMagFilter = Filter.Linear 
                },            
                Specular = float3(0.1f, 0.1f, 0.1f),
                SpecularPower = 350,
                WeightDiffuse = 1f,
                WeightMirror = 1.0f,
            }, transform);
        }

        public static void TextureBottleLabel(
            Mesh<PositionNormalCoordinate> label,
            float4x4 transform,
            Scene<PositionNormalCoordinate, Material> scene
        )
        {
            Texture2D labelTexture = Texture2D.LoadFromFile("./Textures/DonSimon.jpg");
            scene.Add(label.AsRaycast(RaycastingMeshMode.Grid),
            new Material {
                DiffuseMap = labelTexture,
                Diffuse = float3(1, 1, 1),
                TextureSampler = new Sampler { 
                    Wrap = WrapMode.Repeat,
                    MinMagFilter = Filter.Linear 
                },
            }, transform);
        }

        public static void TextureWoodBoxScene(float3 lightPosition, float3 lightIntensity, Scene<PositionNormalCoordinate, Material> scene)
        {
            Texture2D planeTexture = Texture2D.LoadFromFile("./Textures/Wood.jpeg");

            // Floor
            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x*0.2f, a.z*0.2f), Normal = float3(0, 1, 0) }),
                new Material { 
                    DiffuseMap = planeTexture,
                    Diffuse = float3(1, 1, 1),
                    TextureSampler = new Sampler { 
                        Wrap = WrapMode.Repeat,
                        MinMagFilter = Filter.Linear 
                    },
                    // Emissive = 0.03f
                    // Specular = float3(1, 1, 1),
                    // SpecularPower = 260,
                    // WeightDiffuse = 2.5f,
                    // WeightMirror = 1.0f,
                },
                Transforms.Translate(0, -1.0f, 0));

            // Back wall
            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x*0.2f, a.z*0.2f), Normal = float3(0, 1, 0) }),
                new Material {
                    DiffuseMap = planeTexture,
                    Diffuse = float3(1, 1, 1),
                    TextureSampler = new Sampler {
                        Wrap = WrapMode.Repeat,
                        MinMagFilter = Filter.Linear 
                    },
                    // Emissive = 0.03f
                },
                mul(Transforms.RotateZGrad(90), Transforms.Translate(-3f, 0, -0.5f)));
            
            // Left Wall
            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x*0.2f, a.z*0.2f), Normal = float3(0, 1, 0) }),
                new Material {
                    DiffuseMap = planeTexture,
                    Diffuse = float3(1, 1, 1),
                    TextureSampler = new Sampler {
                        Wrap = WrapMode.Repeat,
                        MinMagFilter = Filter.Linear
                    },
                },
                mul(Transforms.RotateXGrad(270), Transforms.Translate(0, 0, -3.5f)));
            
            // Rigt Wall
            scene.Add(Raycasting.PlaneXZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x*0.2f, a.z*0.2f), Normal = float3(0, 1, 0) }),
                new Material {
                    DiffuseMap = planeTexture,
                    Diffuse = float3(1, 1, 1),
                    TextureSampler = new Sampler {
                        Wrap = WrapMode.Repeat,
                        MinMagFilter = Filter.Linear 
                    },
                    //Emissive = 0.003f
                },
                mul(Transforms.RotateXGrad(90), Transforms.Translate(0, 0, 3.5f)));
            

            // Front Wall
            scene.Add(Raycasting.PlaneYZ.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(a.x*0.2f, a.z*0.2f), Normal = float3(0, 1, 0) }),
                new Material {
                    DiffuseMap = planeTexture,
                    Diffuse = float3(1, 1, 1),
                    TextureSampler = new Sampler {
                        Wrap = WrapMode.Repeat,
                        MinMagFilter = Filter.Linear
                    }
                },
               Transforms.Translate(7, 1, -2f));
            
            // Light Bomb
            var sphereModel = Raycasting.UnitarySphere.AttributesMap(a => new PositionNormalCoordinate { Position = a, Coordinates = float2(atan2(a.z, a.x) * 0.5f / pi + 0.5f, a.y), Normal = normalize(a) });
            scene.Add(sphereModel, new Material
                {
                    Emissive = lightIntensity / (4 * pi), // power per unit area
                    WeightDiffuse = 0,
                    WeightFresnel = 1.0f, // Glass sphere
                    RefractionIndex = 1.0f
                },
               mul(Transforms.Scale(0.4f), Transforms.Translate(lightPosition)));
        }
    }
}