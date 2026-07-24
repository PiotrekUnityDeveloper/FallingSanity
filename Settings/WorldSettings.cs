using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FallingSanity.Settings
{
    public static class WorldSettings
    {
        private static float DefaultGravity = 9.8f;
        public static float DefaultTemperature = 20; //c

        public static float GetGravity(int cellX, int cellY) => DefaultGravity;

        ///the default temperature is not chunk-specific. this applies to the whole world.
        ///temperature is handled per-cell instead and default temperature cannot be changed
        ///for specific chunks (unlike gravity which can be modified with custom items)
        ///public static float GetDefaultTemperature() => DefaultTemperature;

        public static readonly int DefaultWorldCellSize = 4;
        public static readonly int DefaultWorldWidth = 320;
        public static readonly int DefaultWorldHeight = 180;
        public static readonly int DefaultWorldChunkSize = 32;
    }
}
