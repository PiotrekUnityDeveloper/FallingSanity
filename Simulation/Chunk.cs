using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FallingSanity.Simulation
{
    public struct Chunk
    {
        private readonly Point _chunkPos;

        public Chunk(int chunkX, int chunkY)
        {
            _chunkPos = new Point(chunkX, chunkY);
        }

        public bool IsActive { get; set; }
        public bool ActiveNextFrame { get; set; }
    }
}
