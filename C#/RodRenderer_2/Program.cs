using System;
using GMath;
using Rendering;
using static GMath.Gfx;

namespace Renderer
{
    struct PositionNormal : INormalVertex<PositionNormal>
    {
        public float3 Position { get; set; }
        public float3 Normal { get; set; }

        public PositionNormal Add(PositionNormal other)
        {
            return new PositionNormal
            {
                Position = this.Position + other.Position,
                Normal = this.Normal + other.Normal
            };
        }

        public PositionNormal Mul(float s)
        {
            return new PositionNormal
            {
                Position = this.Position * s,
                Normal = this.Normal * s
            };
        }

        public PositionNormal Transform(float4x4 matrix)
        {
            float4 p = float4(Position, 1);
            p = mul(p, matrix);

            float4 n = float4(Normal, 0);
            n = mul(n, matrix);

            return new PositionNormal
            {
                Position = p.xyz / p.w,
                Normal = n.xyz
            };
        }
    }

    class Program
    {
        static void Main(string[] args)
        {
            Texture2D texture = new Texture2D(512, 512);
            DisplayBottleMesh<PositionNormal>.DrawBottleRayCastingMesh(texture);
            texture.Save("test.rbm");
            Console.WriteLine("Done.");
        }
    }
}
