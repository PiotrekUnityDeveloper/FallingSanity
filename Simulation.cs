using FallingSanity;
using Microsoft.Xna.Framework;
using System;
using System.Runtime.CompilerServices;

namespace MyApp
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
        private readonly WorldRenderer _renderer;
        private readonly Random _rng = new Random();

        // Alternated each tick so nothing systematically drifts left or right.
        private bool _sweepLeftToRight;

        public Simulation(Grid grid, WorldRenderer renderer)
        {
            _grid = grid;
            _renderer = renderer;
        }

        /// <summary>
        /// Advances the world by one tick. Iterates bottom row first so a
        /// cell that falls into an already-processed row isn't moved again
        /// within the same tick.
        /// </summary>
        public void Step()
        {
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

            _grid.Set(fromX, fromY, target);
            _grid.Set(toX, toY, mover);

            _renderer.UpdatePixel(fromX, fromY, target);
            _renderer.UpdatePixel(toX, toY, mover);

            return true;
        }

        private bool TryMoveToPosition(int fromX, int fromY, int toX, int toY, out int endX, out int endY)
        {
            endX = fromX;
            endY = fromY;

            if (!_grid.InBounds(toX, toY)) return false;

            Point[] path = GetLineTraversePositionsBR(fromX, fromY, toX, toY);

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

        private Point[] GetLineTraversePositionsBR(int startX, int startY, int endX, int endY)
        {
            /// formula
            /// (dx, dy) = (ix + 1, iy - 1/2) - (x0, y0)
            /// // i want to kill myself
             
            int currentX = startX;
            int currentY = startY;

            int looplength = (Math.Abs(startX - endX) > Math.Abs(startY - endY)) ? Math.Abs(startX - endX) : Math.Abs(startY - endY);

            Point[] positions = new Point[looplength];

            for (int i = 0; i < looplength; i++)
            {
                currentX = startX + i;
                currentY = startY + i;
                Point nextPos = new Point((currentX + 1) - startX, (currentY + 1/2) - startY);
                positions[i] = nextPos;
            }

            return positions;
        }

        private Point[] IfThisDoesntWorkImUninstalling(int startX, int startY, int endX, int endY)
        {
            // calculate the direction
            int xdev = 0;
            int ydev = 0;

            if(startX - endX < 0) { xdev = 1; }
            else if(startX - endX > 0) { xdev = -1; }
            else { xdev = 0; }

            if (startY - endY < 0) { ydev = 1; }
            else if (startY - endY > 0) { ydev = -1; }
            else { ydev = 0; }

            int xLength = Math.Abs(startX - endX);
            int yLength = Math.Abs(startY - endY);
            int looplength = (xLength > yLength) ? xLength + 1 : yLength + 1;
            bool xLonger = (xLength > yLength) ? true : false;
            Point[] points = new Point[looplength];

            int currentX = startX;
            int currentY = startY;

            for (int i = 0; i < looplength; i++)
            {
                currentX = currentX + (xLonger == true ? (1) : (1 / (yLength / xLength))) * xdev;
                currentY = currentY + (xLonger == false ? (1) : (1 / (xLength / yLength))) * ydev;
                points[i] = new Point(currentX, currentY);
            }

            return points;
        }

        private Point[] GetLineTraversePositionsBR(int startX, int endX, int startY, int endY) 
        {
            float xLength, yLength;
            xLength = Math.Abs(startX - endX);
            yLength = Math.Abs(startY - endY);

            float currentX = startX;
            float currentY = startY;

            int difference = (int)Math.Abs(xLength - yLength);
            int looplength = (int)Math.Max(xLength, yLength);

            if(looplength == 0) { return new Point[] { new Point(startX, startY) }; }

            Point[] points = new Point[looplength];

            for (int i = 0; i < looplength; i++)
            {
                points[i] = new Point((int)Math.Round(currentX), (int)Math.Round(currentY));

                if(xLength != 0) currentX = (currentX + (xLength / looplength) * Math.Sign(endX - startX));
                if(yLength != 0) currentY = (currentY + (yLength / looplength) * Math.Sign(endY - startY));
            }

            return points;
        }

        private Point[] GetLineTraversePositions(int startX, int startY, int endX, int endY)
        {
            var points = new List<Point>();

            int dx = Math.Abs(endX - startX);
            int dy = Math.Abs(endY - startY);
            int stepX = endX > startX ? 1 : -1;
            int stepY = endY > startY ? 1 : -1;

            int x = startX;
            int y = startY;
            int error = dx - dy;

            while (true)
            {
                points.Add(new Point(x, y));
                if (x == endX && y == endY) break;

                int error2 = 2 * error;
                if (error2 > -dy)
                {
                    error -= dy;
                    x += stepX;
                }
                if (error2 < dx)
                {
                    error += dx;
                    y += stepY;
                }
            }

            return points.ToArray();
        }

        private Point[] GetLineTraversePositionsSC(int startX, int startY, int endX, int endY)
        {
            //idk

            return new Point[1]
            {
                new Point(0, 0),
            };
        }
    }
}