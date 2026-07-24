using FallingSanity.Rendering;
using FallingSanity.Settings;
using FallingSanity.Simulation;
using FallingSanity.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace FallingSanity.Core
{
    public class Game1 : Game
    {
        // World is simulated at low resolution and scaled up — same idea Noita uses.
        private int CellSize = 4;
        private int WorldWidth = 320; //world width in cells
        private int WorldHeight = 180; //world height in cells
        private int ChunkSize = 32; //chunk size in cells

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Grid _grid;
        private ChunkManager _chunkManager;
        private WorldRenderer _worldRenderer;
        private InputHandler _inputHandler;
        private FallingSanity.Simulation.Simulation _simulation;
        private CursorRenderer _brushCursor;
        private ImGuiRenderer _imGuiRenderer;
        private MaterialSelectorUI _materialSelector;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            //set the world settings
            CellSize = WorldSettings.DefaultWorldCellSize;
            WorldWidth = WorldSettings.DefaultWorldWidth;
            WorldHeight = WorldSettings.DefaultWorldHeight;
            ChunkSize = WorldSettings.DefaultWorldChunkSize;

            _graphics.PreferredBackBufferWidth = WorldWidth * CellSize;
            _graphics.PreferredBackBufferHeight = WorldHeight * CellSize;
        }

        protected override void Initialize()
        {
            _grid = new Grid(WorldWidth, WorldHeight);
            _chunkManager = new ChunkManager(_grid, WorldWidth, WorldHeight, ChunkSize);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _worldRenderer = new WorldRenderer(GraphicsDevice, _grid, CellSize);
            _worldRenderer.Refresh();

            _inputHandler = new InputHandler(_grid, _worldRenderer, brushRadius: 3);
            _simulation = new FallingSanity.Simulation.Simulation(_grid, _chunkManager, _worldRenderer);
            _brushCursor = new CursorRenderer(GraphicsDevice);

            _imGuiRenderer = new ImGuiRenderer(this);
            _imGuiRenderer.RebuildFontAtlas();
            _materialSelector = new MaterialSelectorUI(_inputHandler);
        }

        protected override void Update(GameTime gameTime)
        {
            _inputHandler.Update();
            _simulation.Step();
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _worldRenderer.Flush();

            // PointClamp keeps the upscaled pixels crisp instead of blurry.
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _worldRenderer.Draw(_spriteBatch);

            var mouse = Mouse.GetState();
            float radiusInPixels = _inputHandler.BrushRadius * CellSize;
            _brushCursor.Draw(_spriteBatch, mouse.X, mouse.Y, radiusInPixels, Color.Red);

            _spriteBatch.End();

            _imGuiRenderer.BeforeLayout(gameTime);
            _materialSelector.Draw();
            _imGuiRenderer.AfterLayout();

            base.Draw(gameTime);
        }
    }
}