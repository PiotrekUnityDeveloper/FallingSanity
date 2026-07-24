using Microsoft.Xna.Framework;

namespace FallingSanity.Simulation
{
    /// <summary>
    /// Per-material static data — one instance per material TYPE, not per
    /// pixel. Lives in a table in MaterialDatabase, indexed by MaterialType.
    /// This is the struct you'll be filling out over and over as you add
    /// materials, so it's kept separate from anything per-instance.
    /// </summary>
    public struct MaterialDefinition
    {
        public string Name;
        public MaterialBehavior Behavior;
        public Color[] ColorPalette;   // a few shade variants so cells aren't flat-colored
        public float Density;          // (kg/m^3)
        public float Toughness;
        public float Conductivity;
        public float Flammability;
        public byte MaxHP;
    }
}