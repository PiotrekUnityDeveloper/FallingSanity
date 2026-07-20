using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyApp
{
    /// <summary>
    /// Renders the Grid as one pixel-per-cell texture, scaled up on the GPU.
    /// Maintain one Color[] buffer the same size as the grid, write into it
    /// directly when cells change, and upload it to the GPU with a single
    /// SetData call per frame (only if dirty).
    ///
    /// Notice this class never touches MaterialDatabase: a cell's Color is
    /// baked in when it's created (see MaterialDatabase.CreateCell), so the
    /// renderer only ever needs Cell.Color. That's what keeps rendering
    /// completely decoupled from material logic.
    /// </summary>
    public class WorldRenderer
    {
        private readonly Grid _grid;
        private readonly Texture2D _texture;
        private readonly Color[] _pixels;
        private bool _dirty;

        public int CellSize { get; }

        public WorldRenderer(GraphicsDevice device, Grid grid, int cellSize)
        {
            _grid = grid;
            CellSize = cellSize;

            _texture = new Texture2D(device, grid.Width, grid.Height, false, SurfaceFormat.Color);
            _pixels = new Color[grid.Width * grid.Height];
            _dirty = true;
        }

        /// <summary>
        /// Call this whenever a single cell changes (input, or later,
        /// simulation). Cheap: just writes into the CPU-side buffer.
        /// </summary>
        public void UpdatePixel(int x, int y, Cell cell)
        {
            if (!_grid.InBounds(x, y)) return;
            _pixels[_grid.Index(x, y)] = cell.Color;
            _dirty = true;
        }

        /// <summary>
        /// Rebuilds the entire pixel buffer from the grid. Use sparingly
        /// (startup, or after a bulk world edit/load) — prefer UpdatePixel
        /// for incremental changes.
        /// </summary>
        public void Refresh()
        {
            for (int y = 0; y < _grid.Height; y++)
            {
                for (int x = 0; x < _grid.Width; x++)
                {
                    _pixels[_grid.Index(x, y)] = _grid.Get(x, y).Color;
                }
            }
            _dirty = true;
        }

        /// <summary>
        /// Uploads the pixel buffer to the GPU texture, but only if
        /// something actually changed since the last flush.
        /// </summary>
        public void Flush()
        {
            if (!_dirty) return;
            _texture.SetData(_pixels);
            _dirty = false;
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            var destination = new Rectangle(0, 0, _grid.Width * CellSize, _grid.Height * CellSize);
            spriteBatch.Draw(_texture, destination, Color.White);
        }
    }
}