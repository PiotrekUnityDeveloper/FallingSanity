using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FallingSanity
{
    public static class WorldSettings
    {
        public static float Gravity = 9.8f;

        // Today: ignores position entirely. Once chunking exists, this is the
        // only method that changes — check a per-chunk override here first,
        // fall back to the global default. Simulation never needs to know.
        public static float GetGravity(int cellX, int cellY) => Gravity;
    }
}
