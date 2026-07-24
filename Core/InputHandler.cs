using FallingSanity.Rendering;
using FallingSanity.Simulation;
using FallingSanity.Util;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace FallingSanity.Core
{
    /// <summary>
    /// Translates raw mouse/keyboard input into world edits: left click
    /// paints a circular brush of the currently-selected material into the
    /// grid, and pushes the same change straight into the renderer's buffer.
    /// Scroll wheel adjusts brush size; 1/2 switch material.
    /// </summary>
    public class InputHandler
    {
        private const int MinBrushRadius = 0;
        private const int MaxBrushRadius = 24;

        private bool FixDrawGaps = true;
        private bool PixelPerfect = false;

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
            int cellX = mouse.X / _renderer.CellSize;
            int cellY = mouse.Y / _renderer.CellSize;

            // check if hovering over gui
            if (io.WantCaptureMouse) return;

            int scrollDelta = mouse.ScrollWheelValue - _previousScrollValue;
            _previousScrollValue = mouse.ScrollWheelValue;

            if (scrollDelta != 0)
            {
                int steps = scrollDelta / 120;
                if (steps == 0) steps = scrollDelta > 0 ? 1 : -1;
                _brushRadius = MathHelper.Clamp(_brushRadius + steps, MinBrushRadius, MaxBrushRadius);
            }

            if (mouse.LeftButton == ButtonState.Pressed)
            {
                Point? previousPos = GetLastMouseDrawingPos();

                SpawnBrush(cellX, cellY, _currentMaterial);
                
                if(previousPos != null && FixDrawGaps)
                {
                    SpawnLine(previousPos.Value.X, previousPos.Value.Y, cellX, cellY, CurrentMaterial);
                }
            }

                bool wasDown = _previousLeftButton == ButtonState.Pressed;
            bool isDown = mouse.LeftButton == ButtonState.Pressed;

            if (!wasDown && isDown)
            {
                // save mousedown position
                _dragStart = new Point(cellX, cellY);
                OnMousePressed(new Point(cellX, cellY));
            }
            else if (wasDown && !isDown)
            {
                if (_dragStart.HasValue)
                {
                    OnMouseReleased(_dragStart.Value, new Point(cellX, cellY));
                    _dragStart = null;
                }
            }

            _previousLeftButton = mouse.LeftButton;
        }

        private void OnMousePressed(Point start)
        {
            
        }

        private void OnMouseReleased(Point start, Point end)
        {
            if(GetLastMouseDrawingPos() != null && FixDrawGaps)
            {
                Point[] line = GridHelper.GetLineTraversalPositionsBR(lastMouseDrawingPos.Value.X, lastMouseDrawingPos.Value.Y, end.X, end.Y);
                foreach (var p in line)
                {
                    SpawnBrush(p.X, p.Y, _currentMaterial);
                }
            }

            ResetLastMouseDrawingPos();
        }

        private Point? lastMouseDrawingPos = null;

        public Point? GetLastMouseDrawingPos() => lastMouseDrawingPos;
        public Point? SetLastMouseDrawingPos(Point? pos) => lastMouseDrawingPos = pos;
        public Point? ResetLastMouseDrawingPos() => lastMouseDrawingPos = null;

        private ButtonState _previousLeftButton = ButtonState.Released;
        private Point? _dragStart; //position of last mousedown

        /////////////////////////////////////////
        /// SPAWNING                          ///
        /////////////////////////////////////////

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

            lastMouseDrawingPos = new Point(centerX, centerY);
        }

        private void SpawnLine(int startX, int startY, int endX, int endY, MaterialType material)
        {
            Point[] line = GridHelper.GetLineTraversalPositionsBR(startX, startY, endX, endY);
            
            if(PixelPerfect == false) { line = GridHelper.GetLineTraversalPositionsSC(startX, startY, endX, endY); }

            foreach(var p in  line)
            {
                SpawnBrush(p.X, p.Y, material);
            }
        }

        /////////////////////////////////////////
        /// ERASING                           ///
        /////////////////////////////////////////
        /// 'erasing' replaces all element types with bg element using either
        /// the same pressure used by neighboring elements or the default world
        /// pressure (set in world properties).
        
        private void EraseBrush(int centerX, int centerY)
        {
            
        }
    }
}