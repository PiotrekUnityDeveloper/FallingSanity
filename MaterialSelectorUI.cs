using System.Numerics;
using ImGuiNET;

namespace MyApp
{
    /// <summary>
    /// A Dear ImGui panel for picking the active material with the mouse
    /// instead of memorizing number keys. Reads the list straight from
    /// MaterialDatabase, so registering a new material makes it show up
    /// here automatically — this file never needs to change.
    /// </summary>
    public class MaterialSelectorUI
    {
        private readonly InputHandler _inputHandler;

        public MaterialSelectorUI(InputHandler inputHandler)
        {
            _inputHandler = inputHandler;
        }

        /// <summary>Call once per frame, between ImGuiRenderer.BeforeLayout/AfterLayout.</summary>
        public void Draw()
        {
            ImGui.SetNextWindowPos(new Vector2(10, 10), ImGuiCond.FirstUseEver);
            ImGui.Begin("Materials");

            foreach (var type in MaterialDatabase.AllMaterials)
            {
                if (type == MaterialType.Empty) continue; // not something you paint with

                var definition = MaterialDatabase.Get(type);
                bool isSelected = _inputHandler.CurrentMaterial == type;

                if (ImGui.Selectable(definition.Name, isSelected))
                {
                    _inputHandler.SetMaterial(type);
                }
            }

            ImGui.Separator();
            ImGui.Text($"Brush size: {_inputHandler.BrushRadius} (scroll to resize)");

            ImGui.End();
        }
    }
}