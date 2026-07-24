using Microsoft.Xna.Framework.Graphics;

namespace FallingSanity.Rendering
{
    /// <summary>
    /// Describes the memory layout of ImGui's ImDrawVert struct (position,
    /// UV, packed color) to MonoGame's vertex buffer, so the GPU can read
    /// ImGui's vertex data directly without us reshaping it first.
    /// </summary>
    public static class DrawVertDeclaration
    {
        public static readonly VertexDeclaration Declaration;
        public static readonly int Size;

        static DrawVertDeclaration()
        {
            unsafe { Size = sizeof(ImGuiNET.ImDrawVert); }

            Declaration = new VertexDeclaration(
                Size,
                new VertexElement(0, VertexElementFormat.Vector2, VertexElementUsage.Position, 0),
                new VertexElement(8, VertexElementFormat.Vector2, VertexElementUsage.TextureCoordinate, 0),
                new VertexElement(16, VertexElementFormat.Color, VertexElementUsage.Color, 0)
            );
        }
    }
}