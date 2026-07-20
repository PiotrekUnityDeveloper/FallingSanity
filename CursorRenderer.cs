using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace MyApp
{
    /// <summary>
    /// Draws a circular outline at the mouse position showing the current
    /// brush size. Purely visual — knows nothing about the grid or input,
    /// just takes a center point and a radius (in screen pixels) and draws
    /// a ring. SpriteBatch has no built-in shape drawing, so this uses the
    /// standard trick: a 1x1 white texture, stretched and rotated into line
    /// segments.
    /// </summary>
    public class CursorRenderer
    {
        private const int Segments = 32;

        private readonly Texture2D _pixel;

        public CursorRenderer(GraphicsDevice device)
        {
            _pixel = new Texture2D(device, 1, 1);
            _pixel.SetData(new[] { Color.White });
        }

        public void Draw(SpriteBatch spriteBatch, int centerX, int centerY, float radius, Color color, float thickness = 1.5f)
        {
            var center = new Vector2(centerX, centerY);
            var previous = center + new Vector2(radius, 0);

            for (int i = 1; i <= Segments; i++)
            {
                float angle = MathHelper.TwoPi * i / Segments;
                var point = center + new Vector2(
                    (float)Math.Cos(angle) * radius,
                    (float)Math.Sin(angle) * radius);

                DrawLine(spriteBatch, previous, point, color, thickness);
                previous = point;
            }
        }

        private void DrawLine(SpriteBatch spriteBatch, Vector2 start, Vector2 end, Color color, float thickness)
        {
            Vector2 delta = end - start;
            float length = delta.Length();
            float angle = (float)Math.Atan2(delta.Y, delta.X);

            spriteBatch.Draw(
                _pixel,
                start,
                null,
                color,
                angle,
                Vector2.Zero,
                new Vector2(length, thickness),
                SpriteEffects.None,
                0f);
        }
    }
}