using Microsoft.Xna.Framework;
using System;

namespace MyApp
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("Hello World!");
            using var game = new Game1();
            game.Run();
        }
    }
}