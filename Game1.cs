using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyApp
{
    public class Game1 : Game
    {
        // World is simulated at low resolution and scaled up — same idea Noita uses.
        private const int CellSize = 4;
        private const int WorldWidth = 320;
        private const int WorldHeight = 180;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Grid _grid;
        private WorldRenderer _worldRenderer;
        private InputHandler _inputHandler;
        private Simulation _simulation;
        private CursorRenderer _brushCursor;
        private ImGuiRenderer _imGuiRenderer;
        private MaterialSelectorUI _materialSelector;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = WorldWidth * CellSize;
            _graphics.PreferredBackBufferHeight = WorldHeight * CellSize;
        }

        protected override void Initialize()
        {
            _grid = new Grid(WorldWidth, WorldHeight);
            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _worldRenderer = new WorldRenderer(GraphicsDevice, _grid, CellSize);
            _worldRenderer.Refresh();

            _inputHandler = new InputHandler(_grid, _worldRenderer, brushRadius: 3);
            _simulation = new Simulation(_grid, _worldRenderer);
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