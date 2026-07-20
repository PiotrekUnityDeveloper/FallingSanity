using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;

namespace MyApp
{
    /// <summary>
    /// Registry of per-material static data (MaterialDefinition), indexed by
    /// MaterialType. This is the single place that knows what a material
    /// "is" — Grid, WorldRenderer, and InputHandler never hardcode material
    /// specifics; they just carry a MaterialType/Cell around and ask this
    /// class when they need a definition or a freshly-spawned cell.
    ///
    /// Dictionary for now since the material list is small and will keep
    /// changing. Once your list stabilizes, swap to a flat array indexed by
    /// a compact sequential ID for O(1) lookups instead of hashing — matters
    /// once a real simulation is reading this every cell, every frame.
    /// </summary>
    public static class MaterialDatabase
    {
        private static readonly Dictionary<MaterialType, MaterialDefinition> _definitions = new();
        private static readonly Random _rng = new Random();

        static MaterialDatabase()
        {
            Register(MaterialType.Empty, new MaterialDefinition
            {
                Name = "Empty",
                Behavior = MaterialBehavior.Gas,
                ColorPalette = new[] { new Color(12, 12, 16) },
                Density = 0f,
                Toughness = 0f,
                Conductivity = 0f,
                Flammability = 0f,
                MaxHP = 0
            });

            Register(MaterialType.Sand, new MaterialDefinition
            {
                Name = "Sand",
                Behavior = MaterialBehavior.Powder,
                ColorPalette = new[]
                {
                    new Color(237, 201, 175),
                    new Color(230, 193, 165),
                    new Color(224, 185, 155),
                },
                Density = 1.6f,
                Toughness = 0.2f,
                Conductivity = 0.1f,
                Flammability = 0f,
                MaxHP = 10
            });

            Register(MaterialType.Water, new MaterialDefinition
            {
                Name = "Water",
                Behavior = MaterialBehavior.Liquid,
                ColorPalette = new[]
                {
                    new Color(64, 121, 196),
                    new Color(58, 112, 184),
                    new Color(72, 130, 204),
                },
                Density = 1.0f,
                Toughness = 0f,
                Conductivity = 0.6f,
                Flammability = 0f,
                MaxHP = 1
            });
        }

        public static void Register(MaterialType type, MaterialDefinition definition) =>
            _definitions[type] = definition;

        public static MaterialDefinition Get(MaterialType type) => _definitions[type];

        /// <summary>
        /// Every material currently registered. Lets UI (or anything else)
        /// enumerate materials dynamically instead of hardcoding a list —
        /// register a new material here and it just shows up everywhere.
        /// </summary>
        public static IEnumerable<MaterialType> AllMaterials => _definitions.Keys;

        /// <summary>
        /// Builds a brand-new cell of the given material: picks a random
        /// shade from its palette (so a patch of sand isn't flat-colored),
        /// sets HP to the material's max, and defaults temperature to ambient.
        /// This is the ONE place new cells get created — spawning, loading a
        /// save, anything — so every cell is always consistent with its definition.
        /// </summary>
        public static Cell CreateCell(MaterialType type, byte ambientTemperature = 20)
        {
            var def = Get(type);
            var palette = def.ColorPalette;
            var color = palette != null && palette.Length > 0
                ? palette[_rng.Next(palette.Length)]
                : Color.Magenta; // loud fallback so a missing palette is obvious, not invisible

            return new Cell
            {
                MaterialId = type,
                HP = def.MaxHP,
                Color = color,
                Temperature = ambientTemperature
            };
        }
    }
}