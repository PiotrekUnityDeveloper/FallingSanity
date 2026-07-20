namespace MyApp
{
    /// <summary>
    /// Pure data container for the world: one Cell per cell, flat-packed
    /// for cache locality. No simulation logic lives here — that comes
    /// later as a Simulation class that reads and writes this grid.
    /// </summary>
    public class Grid
    {
        public int Width { get; }
        public int Height { get; }

        private readonly Cell[] _cells;

        public Grid(int width, int height)
        {
            Width = width;
            Height = height;
            _cells = new Cell[width * height];

            for (int i = 0; i < _cells.Length; i++)
                _cells[i] = Cell.Empty;
        }

        public bool InBounds(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height;

        public int Index(int x, int y) => y * Width + x;

        public Cell Get(int x, int y) =>
            InBounds(x, y) ? _cells[Index(x, y)] : Cell.Empty;

        public void Set(int x, int y, Cell cell)
        {
            if (!InBounds(x, y)) return;
            _cells[Index(x, y)] = cell;
        }

        public bool IsEmpty(int x, int y) => Get(x, y).MaterialId == MaterialType.Empty;
    }
}