using GMath;
using Rendering;
using System;
using static Utils.Tools;
using static GMath.Gfx;
using static Display.BottlesArt;

namespace Renderer
{
    class Program
    {
        static void Main(string[] args)
        {
            Raster render = new Raster(1024, 2048);
            DrawBottleArt(render);
            render.RenderTarget.Save("test.rbm");
            Console.WriteLine("Done.");
        }
    }
}
