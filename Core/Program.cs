using Microsoft.Xna.Framework;
using FallingSanity;
using System;

namespace FallingSanity.Core
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