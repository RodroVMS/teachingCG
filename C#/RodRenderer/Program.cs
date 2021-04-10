using System;
using GMath;
using Rendering;
using static GMath.Gfx;

namespace Renderer
{
    class Program
    {
        public struct MyVertex : IVertex<MyVertex>
        {
            public float3 Position { get; set; }

            public MyVertex Add(MyVertex other)
            {
                return new MyVertex
                {
                    Position = this.Position + other.Position,
                };
            }

            public MyVertex Mul(float s)
            {
                return new MyVertex
                {
                    Position = this.Position * s,
                };
            }

            public override string ToString()
            {
                return Position.ToString();
            }
        }

        public struct MyProjectedVertex : IProjectedVertex<MyProjectedVertex>
        {
            public float4 Homogeneous { get; set; }

            public MyProjectedVertex Add(MyProjectedVertex other)
            {
                return new MyProjectedVertex
                {
                    Homogeneous = this.Homogeneous + other.Homogeneous
                };
            }

            public MyProjectedVertex Mul(float s)
            {
                return new MyProjectedVertex
                {
                    Homogeneous = this.Homogeneous * s
                };
            }
        }

        static void Main(string[] args)
        {

            Raster<MyVertex, MyProjectedVertex> render = new Raster<MyVertex, MyProjectedVertex>(1024, 512);
            //GeneratingMeshes(render);
            var m = new DisplayBottleMesh<MyVertex, MyProjectedVertex>();
            DisplayBottleMesh<MyVertex, MyProjectedVertex>.DrawBottleMesh(render);
            Console.WriteLine("Done.");
        }
    }
}
