using Microsoft.Xna.Framework;

namespace MyApp
{
    /// <summary>
    /// Per-pixel instance data — what actually lives in the Grid array.
    /// Kept small and blittable since there will be hundreds of thousands
    /// of these. Static, shared-per-material data (density, toughness, etc.)
    /// lives in MaterialDefinition instead; look it up via MaterialId.
    /// </summary>
    public struct Cell
    {
        public MaterialType MaterialId;  // which material this pixel is
        public byte HP;                  // this pixel's remaining durability
        public Color Color;              // baked-in render color (with per-pixel variance)
        public byte Temperature;         // current temperature, 0-255 scale

        // Hardcoded (not pulled from MaterialDatabase) so Cell has no static
        // dependency on MaterialDatabase — avoids a static-init ordering issue.
        // Keep this color in sync with MaterialDatabase's Empty registration below.
        public static readonly Cell Empty = new Cell
        {
            MaterialId = MaterialType.Empty,
            HP = 0,
            Color = new Color(12, 12, 16),
            Temperature = 20
        };
    }
}