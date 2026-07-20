using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace MyApp
{
    /// <summary>
    /// Translates raw mouse/keyboard input into world edits: left click
    /// paints a circular brush of the currently-selected material into the
    /// grid, and pushes the same change straight into the renderer's buffer.
    /// Scroll wheel adjusts brush size; 1/2 switch material.
    /// </summary>
    public class InputHandler
    {
        private const int MinBrushRadius = 1;
        private const int MaxBrushRadius = 24;

        private readonly Grid _grid;
        private readonly WorldRenderer _renderer;

        private int _brushRadius;
        private int _previousScrollValue;
        private MaterialType _currentMaterial = MaterialType.Sand;

        public int BrushRadius => _brushRadius;
        public MaterialType CurrentMaterial => _currentMaterial;

        public void SetMaterial(MaterialType material) => _currentMaterial = material;

        public InputHandler(Grid grid, WorldRenderer renderer, int brushRadius = 3)
        {
            _grid = grid;
            _renderer = renderer;
            _brushRadius = brushRadius;
        }

        public void Update()
        {
            var io = ImGui.GetIO();

            var keyboard = Keyboard.GetState();
            if (!io.WantCaptureKeyboard)
            {
                if (keyboard.IsKeyDown(Keys.D1)) _currentMaterial = MaterialType.Sand;
                if (keyboard.IsKeyDown(Keys.D2)) _currentMaterial = MaterialType.Water;
            }

            var mouse = Mouse.GetState();

            // ImGui is handling this mouse input (hovering/dragging a panel)
            // — don't paint or resize the brush underneath it.
            if (io.WantCaptureMouse) return;

            int scrollDelta = mouse.ScrollWheelValue - _previousScrollValue;
            _previousScrollValue = mouse.ScrollWheelValue;

            if (scrollDelta != 0)
            {
                // 120 per notch is the standard mouse-wheel unit; fall back
                // to a 1-step nudge for trackpads that report smaller deltas.
                int steps = scrollDelta / 120;
                if (steps == 0) steps = scrollDelta > 0 ? 1 : -1;
                _brushRadius = MathHelper.Clamp(_brushRadius + steps, MinBrushRadius, MaxBrushRadius);
            }

            if (mouse.LeftButton == ButtonState.Pressed)
            {
                int cellX = mouse.X / _renderer.CellSize;
                int cellY = mouse.Y / _renderer.CellSize;
                SpawnBrush(cellX, cellY, _currentMaterial);
            }
        }

        private void SpawnBrush(int centerX, int centerY, MaterialType material)
        {
            int radiusSquared = _brushRadius * _brushRadius;

            for (int dy = -_brushRadius; dy <= _brushRadius; dy++)
            {
                for (int dx = -_brushRadius; dx <= _brushRadius; dx++)
                {
                    if (dx * dx + dy * dy > radiusSquared) continue;

                    int x = centerX + dx;
                    int y = centerY + dy;

                    if (_grid.IsEmpty(x, y))
                    {
                        var cell = MaterialDatabase.CreateCell(material);
                        _grid.Set(x, y, cell);
                        _renderer.UpdatePixel(x, y, cell);
                    }
                }
            }
        }
    }
}