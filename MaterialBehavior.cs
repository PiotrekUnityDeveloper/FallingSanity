namespace MyApp
{
    /// <summary>
    /// Broad category of simulation behavior. New materials plug into one of
    /// these instead of getting bespoke code — a material's specific numbers
    /// (density, viscosity, flammability...) live in MaterialDefinition and
    /// get read by whichever behavior handles its category. This is what lets
    /// you add a hundred materials without writing a hundred behaviors.
    /// </summary>
    public enum MaterialBehavior : byte
    {
        Solid,   // never moves on its own: stone, glass, metal
        Powder,  // falls straight down / diagonally: sand, dust
        Liquid,  // falls and spreads sideways: water, oil, lava
        Gas,     // rises and disperses: steam, smoke
        Dust,    // like Powder but lighter, disperses more: ash, spores
    }
}