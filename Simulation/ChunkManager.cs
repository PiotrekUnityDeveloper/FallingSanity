using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading.Tasks;

namespace FallingSanity.Simulation
{
    public class ChunkManager
    {
        private readonly Grid _grid;
        private Chunk[] _chunks;
        public Chunk[] GetChunks() => _chunks;
        private static int _chunkSize;

        // horizontal and vertical chunk count
        private static int _chunksHorizontal, _chunksVertical;

        public ChunkManager(Grid grid, int gridWidth, int gridHeight, int chunkSize)
        {
            _grid = grid;
            _chunkSize = chunkSize;
            //calculate the total amount of chunks vertically and horizonstally
            //based on height and width of the world grid
            _chunksHorizontal = (gridWidth + chunkSize - 1) / chunkSize;
            _chunksVertical = (gridHeight + chunkSize - 1) / chunkSize; //round up
            _chunks = new Chunk[_chunksHorizontal * _chunksVertical];

            for (int i = 0; i < _chunks.Length; i++)
            {
                _chunks[i] = new Chunk(ChunkPosFromIndex(i).X, ChunkPosFromIndex(i).Y);
                _chunks[i].IsActive = true;
            }
        }

        // util

        public bool InBoundsCellPos(int cellX, int cellY)
        {
            int index = ChunkIndexFromCellPos(cellX, cellY);
            return _chunks.Length > index && index >= 0;
        }

        public bool InBoundsChunkPos(int chunkX, int chunkY)
        {
            int index = ChunkIndexFromChunkPos(chunkX, chunkY);
            return _chunks.Length > index && index >= 0;
        }

        public static int ChunkIndexFromCellPos(int cellX, int cellY) =>
            (cellY / _chunkSize) * _chunksHorizontal + (cellX / _chunkSize);

        public static int ChunkIndexFromChunkPos(int chunkX, int chunkY) =>
            (chunkY * _chunksHorizontal) + chunkX;

        public static Point ChunkPosFromIndex(int index)
        {
            int x = index % _chunksHorizontal;
            int y = index / _chunksHorizontal;
            return new Point(x, y);
        }

        public static Point CellPosToChunkPos(int cellX, int cellY) =>
            new Point((cellX / _chunkSize), (cellY / _chunkSize));

        public static Point ChunkPosToCellPos(int chunkX, int chunkY) =>
            new Point((chunkX * _chunkSize), (chunkY * _chunkSize));

        public bool IsChunkActiveCellPos(int cellX, int cellY) => 
            _chunks[ChunkIndexFromCellPos(cellX, cellY)].IsActive ? true : false;

        public bool IsChunkActiveChunkPos(int chunkX, int chunkY) =>
            _chunks[ChunkIndexFromChunkPos(chunkX, chunkY)].IsActive ? true : false;

        public void MarkAllNeighborsDirtyCellPos(int cellX, int cellY)
        {
            int cx = cellX / _chunkSize, cy = cellY / _chunkSize;

            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = cx + dx, ny = cy + dy;
                    if (nx < 0 || ny < 0 || nx >= _chunksHorizontal || ny >= _chunksVertical) continue;
                    _chunks[ny * _chunksHorizontal + nx].ActiveNextFrame = true;
                }
            }
        }

        public void MarkAllNeighborsDirtyChunkPos(int chunkX, int chunkY)
        {
            for (int dy = -1; dy <= 1; dy++)
            {
                for (int dx = -1; dx <= 1; dx++)
                {
                    int nx = chunkX + dx, ny = chunkY + dy;
                    if (nx < 0 || ny < 0 || nx >= _chunksHorizontal || ny >= _chunksVertical) continue;
                    _chunks[ny * _chunksHorizontal + nx].ActiveNextFrame = true;
                }
            }
        }

        public void MarkDirtyDirectCellPos(int cellX, int cellY)
        {
            if (!_grid.InBounds(cellX, cellY)) return;
            _chunks[ChunkIndexFromCellPos(cellX, cellY)].ActiveNextFrame = true;
        }

        public void MarkDirtyDirectChunkPos(int chunkX, int chunkY)
        {
            if (!InBoundsChunkPos(chunkX, chunkY)) return;
            _chunks[ChunkIndexFromChunkPos(chunkX, chunkY)].ActiveNextFrame = true;
        }
    }
}
