using FallingSanity.Core;
using FallingSanity.Rendering;
using FallingSanity.Settings;
using FallingSanity.Util;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace FallingSanity.Simulation
{
    /// <summary>
    /// Layer 1 of the material sim: generic movement driven purely by each
    /// cell's MaterialBehavior + Density. No per-material code here — Sand,
    /// Dirt, Coal all fall through the exact same Powder case just by having
    /// Behavior = Powder in their MaterialDefinition.
    ///
    /// Deliberately NOT handled here (future layers, see conversation):
    ///  - Reactions between materials (fire spreading, acid dissolving)
    ///  - Per-cell status flags (Burning, Wet, Bonded)
    ///  - Bespoke one-off behaviors for weird materials
    /// Those get bolted on as additional passes/hooks without touching this
    /// movement logic.
    /// </summary>
    public class Simulation
    {
        private readonly Grid _grid;
        private readonly ChunkManager _chunkManager;
        private readonly WorldRenderer _renderer;
        private readonly Random _rng = new Random();

        // Alternated each tick so nothing systematically drifts left or right.
        private bool _sweepLeftToRight;

        public Simulation(Grid grid, ChunkManager chunkManager, WorldRenderer renderer)
        {
            _grid = grid;
            _chunkManager = chunkManager;
            _renderer = renderer;
        }

        /// <summary>
        /// Advances the world by one tick. Iterates bottom row first so a
        /// cell that falls into an already-processed row isn't moved again
        /// within the same tick.
        /// </summary>
        public void Step()
        {
            /*
            _sweepLeftToRight = !_sweepLeftToRight;

            for (int y = _grid.Height - 1; y >= 0; y--)
            {
                if (_sweepLeftToRight)
                {
                    for (int x = 0; x < _grid.Width; x++)
                        StepCell(x, y);
                }
                else
                {
                    for (int x = _grid.Width - 1; x >= 0; x--)
                        StepCell(x, y);
                }
            }*/

            //compute the chunks in a chessboard pattern

            Chunk[] chunks = _chunkManager.GetChunks();

            // compute the even index number chunks first
            for (int i = 0; i < chunks.Length; i++)
            {
                if(i % 2 == 0)
                {
                    if (chunks[i].IsActive)
                    {
                        // iterate through the contained cells
                        for (int k = WorldSettings.DefaultWorldChunkSize - 1; k >= 0; k--) //rows
                        {
                            for (int j = 0; j < WorldSettings.DefaultWorldChunkSize; j++) //columns
                            {
                                Point chunkPos = ChunkManager.ChunkPosFromIndex(i);
                                Point chunkcellPos = ChunkManager.ChunkPosToCellPos(chunkPos.X, chunkPos.Y);
                                StepCell(chunkcellPos.X + k, chunkcellPos.Y + j);
                            }
                        }
                    }
                    else if (chunks[i].ActiveNextFrame)
                    {
                        /// set this chunk to be active, but skip iteration for this frame
                        /// (it will iterate next simulation step)
                        chunks[i].IsActive = true;
                    }
                }
            }

            // odd
            for (int i = 0; i < chunks.Length; i++)
            {
                if (i % 2 == 1)
                {
                    if (chunks[i].IsActive)
                    {
                        // iterate through the contained cells
                        for (int k = WorldSettings.DefaultWorldChunkSize - 1; k >= 0; k--) //rows
                        {
                            for (int j = 0; j < WorldSettings.DefaultWorldChunkSize; j++) //columns
                            {
                                Point chunkPos = ChunkManager.ChunkPosFromIndex(i);
                                Point chunkcellPos = ChunkManager.ChunkPosToCellPos(chunkPos.X, chunkPos.Y);
                                StepCell(chunkcellPos.X + k, chunkcellPos.Y + j);
                            }
                        }
                    }
                    else if (chunks[i].ActiveNextFrame)
                    {
                        /// set this chunk to be active, but skip iteration for this frame
                        /// (it will iterate next simulation step)
                        chunks[i].IsActive = true;
                    }
                }
            }
        }

        private void StepCell(int x, int y)
        {
            var cell = _grid.Get(x, y);
            if (cell.MaterialId == MaterialType.Empty) return;

            var def = MaterialDatabase.Get(cell.MaterialId);

            switch (def.Behavior)
            {
                case MaterialBehavior.Powder:
                case MaterialBehavior.Dust:
                    StepPowder(x, y);
                    break;
                case MaterialBehavior.Liquid:
                    StepLiquid(x, y);
                    break;
                case MaterialBehavior.Gas:
                    StepGas(x, y);
                    break;
                case MaterialBehavior.Solid:
                    break; // never moves on its own
            }
        }

        private void StepPowder(int x, int y)
        {
            //try to move downwards first
            if (TryMoveDirect(x, y, x, y + 1)) return;

            int firstDx = _rng.Next(2) == 0 ? -1 : 1;
            if (TryMoveDirect(x, y, x + firstDx, y + 1)) return;
            TryMoveDirect(x, y, x - firstDx, y + 1);
        }

        private void StepLiquid(int x, int y)
        {
            if (TryMoveDirect(x, y, x, y + 1)) return;

            int firstDx = _rng.Next(2) == 0 ? -1 : 1;
            if (TryMoveDirect(x, y, x + firstDx, y + 1)) return;
            if (TryMoveDirect(x, y, x - firstDx, y + 1)) return;

            // Can't fall further — spread sideways looking for the furthest
            // open spot. This is a simplification of real fluid flow (no
            // pressure/volume tracking) but gets most of the visual result.
            const int flowDistance = 4;
            if (TryFlow(x, y, firstDx, flowDistance)) return;
            TryFlow(x, y, -firstDx, flowDistance);
        }

        private void StepGas(int x, int y)
        {
            if (TryMoveDirect(x, y, x, y - 1)) return;

            int firstDx = _rng.Next(2) == 0 ? -1 : 1;
            if (TryMoveDirect(x, y, x + firstDx, y - 1)) return;
            TryMoveDirect(x, y, x - firstDx, y - 1);
        }

        /// <summary>
        /// Scans in a straight line for the furthest empty cell and relocates
        /// the mover there directly (a single swap), rather than stepping one
        /// cell at a time.
        /// </summary>
        private bool TryFlow(int x, int y, int dx, int maxDistance)
        {
            int targetX = x;
            for (int step = 1; step <= maxDistance; step++)
            {
                int candidateX = x + dx * step;
                if (!_grid.InBounds(candidateX, y)) break;
                if (_grid.Get(candidateX, y).MaterialId != MaterialType.Empty) break;
                targetX = candidateX;
            }

            return targetX != x && TryMoveDirect(x, y, targetX, y);
        }

        /// <summary>
        /// tries to move a cell from one position to another. checks only the target position for avaibility.
        /// </summary>
        /// <param name="fromX">X coordinates of the cell to be moved</param>
        /// <param name="fromY">Y coordinates of the cell to be moved</param>
        /// <param name="toX">X coordinates of the end target position cell</param>
        /// <param name="toY">Y coordinates of the end target position cell</param>
        /// <param name="test">if true, the move will be tested but not executed</param>
        /// <returns>
        /// <para><c>true</c> if the target cell is not occupied and the move has succeeded.</para>
        /// <para><c>false</c> if the target cell is currently occupied and the move has failed.</para>
        /// </returns>
        private bool TryMoveDirect(int fromX, int fromY, int toX, int toY, bool test = false)
        {
            if (!_grid.InBounds(toX, toY)) return false;

            var mover = _grid.Get(fromX, fromY);
            var target = _grid.Get(toX, toY);

            bool canDisplace = target.MaterialId == MaterialType.Empty ||
                MaterialDatabase.Get(target.MaterialId).Density < MaterialDatabase.Get(mover.MaterialId).Density;

            if (!canDisplace) return false;

            if(!test)
            {
                _grid.Set(fromX, fromY, target);
                _grid.Set(toX, toY, mover);

                _renderer.UpdatePixel(fromX, fromY, target);
                _renderer.UpdatePixel(toX, toY, mover);
            }

            return true;
        }

        private bool TryMoveToPosition(int fromX, int fromY, int toX, int toY, out int endX, out int endY)
        {
            endX = fromX;
            endY = fromY;

            if (!_grid.InBounds(toX, toY)) return false;

            Point[] path = GridHelper.GetLineTraversalPositionsDDA(fromX, fromY, toX, toY);

            int currentX = fromX;
            int currentY = fromY;

            for (int i = 0; i < path.Length; i++)
            {
                if (!TryMoveDirect(currentX, currentY, path[i].X, path[i].Y, true))
                    return false; // the path is blocked

                currentX = path[i].X;
                currentY = path[i].Y;
                endX = currentX;
                endY = currentY;
            }

            return true; // path end
        }

        
    }
}