using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FallingSanity.Util
{
    public static class GridHelper
    {
        // 2D VECTOR CELL TRAVERSAL

        public static Point[] GetLineTraversalPositionsDDA(int startX, int startY, int endX, int endY)
        {
            float xLength, yLength;
            xLength = Math.Abs(startX - endX);
            yLength = Math.Abs(startY - endY);

            float currentX = startX;
            float currentY = startY;

            int difference = (int)Math.Abs(xLength - yLength);
            int looplength = (int)Math.Max(xLength, yLength);

            if (looplength == 0) { return new Point[] { new Point(startX, startY) }; }

            Point[] points = new Point[looplength];

            for (int i = 0; i < looplength; i++)
            {
                points[i] = new Point((int)Math.Round(currentX), (int)Math.Round(currentY));

                if (xLength != 0) currentX = currentX + xLength / looplength * Math.Sign(endX - startX);
                if (yLength != 0) currentY = currentY + yLength / looplength * Math.Sign(endY - startY);
            }

            return points;
        }

        public static Point[] GetLineTraversalPositionsBR(int startX, int startY, int endX, int endY)
        {
            int dx = Math.Abs(endX - startX);
            int dy = Math.Abs(endY - startY);
            int sx = startX < endX ? 1 : -1;
            int sy = startY < endY ? 1 : -1;
            int err = dx - dy;

            var points = new List<Point>();
            int x = startX, y = startY;

            while (true)
            {
                points.Add(new Point(x, y));
                if (x == endX && y == endY) break;

                int e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x += sx; }
                if (e2 < dx) { err += dx; y += sy; }
            }

            return points.ToArray();
        }

        public static Point[] GetLineTraversalPositionsSC(int startX, int startY, int endX, int endY)
        {
            int x1 = startX, y1 = startY;
            int dx = endX - startX;
            int dy = endY - startY;

            int xstep = dx < 0 ? -1 : 1;
            int ystep = dy < 0 ? -1 : 1;
            dx = Math.Abs(dx);
            dy = Math.Abs(dy);

            int ddx = 2 * dx;
            int ddy = 2 * dy;

            var points = new List<Point> { new Point(x1, y1) };

            if (ddx >= ddy)
            {
                int errorPrev = dx;
                int error = dx;

                for (int i = 0; i < dx; i++)
                {
                    x1 += xstep;
                    error += ddy;

                    if (error > ddx)
                    {
                        y1 += ystep;
                        error -= ddx;

                        if (error + errorPrev < ddx) points.Add(new Point(x1, y1 - ystep));
                        else if (error + errorPrev > ddx) points.Add(new Point(x1 - xstep, y1));
                        else
                        {
                            points.Add(new Point(x1, y1 - ystep));
                            points.Add(new Point(x1 - xstep, y1));
                        }
                    }

                    points.Add(new Point(x1, y1));
                    errorPrev = error;
                }
            }
            else
            {
                int errorPrev = dy;
                int error = dy;

                for (int i = 0; i < dy; i++)
                {
                    y1 += ystep;
                    error += ddx;

                    if (error > ddy)
                    {
                        x1 += xstep;
                        error -= ddy;

                        if (error + errorPrev < ddy) points.Add(new Point(x1 - xstep, y1));
                        else if (error + errorPrev > ddy) points.Add(new Point(x1, y1 - ystep));
                        else
                        {
                            points.Add(new Point(x1 - xstep, y1));
                            points.Add(new Point(x1, y1 - ystep));
                        }
                    }

                    points.Add(new Point(x1, y1));
                    errorPrev = error;
                }
            }

            return points.ToArray();
        }
    }
}
