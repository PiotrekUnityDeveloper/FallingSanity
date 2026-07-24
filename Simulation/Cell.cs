using Microsoft.Xna.Framework;

namespace FallingSanity.Simulation
{
    /// <summary>
    /// Per-pixel instance data — what actually lives in the Grid array.
    /// Kept small and blittable since there will be hundreds of thousands
    /// of these. Static, shared-per-material data (density, toughness, etc.)
    /// lives in MaterialDefinition instead; look it up via MaterialId.
    /// </summary>
    public struct Cell
    {
        public MaterialType MaterialId;  // currently occupying material
        public byte HP;                  // remaining durability. used along with material toughness
        public Color Color;              // very basic rendering
        public float Temperature;        // temperature
        public float Pressure;           // pressure
        private bool IsActive { get; set; }

        // Hardcoded (not pulled from MaterialDatabase) so Cell has no static
        // dependency on MaterialDatabase — avoids a static-init ordering issue.
        // Keep this color in sync with MaterialDatabase's Empty registration below.
        public static readonly Cell Empty = new Cell
        {
            MaterialId = MaterialType.Empty,
            HP = 0,
            Color = new Color(12, 12, 16),
            Temperature = 20f,
        };
    }
}