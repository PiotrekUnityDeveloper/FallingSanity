using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using ImGuiNET;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace MyApp
{
    /// <summary>
    /// Bridges Dear ImGui to MonoGame. ImGui.NET is only the C# binding to
    /// the native cimgui library — it has no idea how to actually put pixels
    /// on screen for any given platform. This class is that missing half:
    /// each frame it forwards MonoGame's mouse/keyboard state into ImGui's
    /// IO, then takes the vertex/index buffers ImGui produces and issues
    /// MonoGame draw calls for them.
    ///
    /// Adapted from the official ImGui.NET MonoGame/XNA sample
    /// (ImGui.NET.SampleProgram.XNA) to match the current event-based IO
    /// API (AddMousePosEvent/AddKeyEvent/etc) rather than the older
    /// KeysDown[]/KeyMap[] arrays from pre-1.87 ImGui, which is a common
    /// source of stale/broken snippets floating around online.
    /// </summary>
    public class ImGuiRenderer
    {
        private readonly Game _game;
        private readonly GraphicsDevice _graphicsDevice;

        private BasicEffect _effect;
        private RasterizerState _rasterizerState;

        private byte[] _vertexData;
        private VertexBuffer _vertexBuffer;
        private int _vertexBufferSize;

        private byte[] _indexData;
        private IndexBuffer _indexBuffer;
        private int _indexBufferSize;

        private readonly Dictionary<IntPtr, Texture2D> _loadedTextures = new();
        private int _textureId;
        private IntPtr? _fontTextureId;

        private int _scrollWheelValue;
        private int _horizontalScrollWheelValue;
        private const float WheelDelta = 120f;
        private readonly Keys[] _allKeys = Enum.GetValues<Keys>();

        public ImGuiRenderer(Game game)
        {
            _game = game ?? throw new ArgumentNullException(nameof(game));
            _graphicsDevice = game.GraphicsDevice;

            var context = ImGui.CreateContext();
            ImGui.SetCurrentContext(context);

            _rasterizerState = new RasterizerState
            {
                CullMode = CullMode.None,
                DepthBias = 0,
                FillMode = FillMode.Solid,
                MultiSampleAntiAlias = false,
                ScissorTestEnable = true,
                SlopeScaleDepthBias = 0
            };

            SetupInput();
        }

        /// <summary>
        /// Builds the font texture ImGui needs to render text and registers
        /// it with MonoGame. Call once, after the GraphicsDevice exists but
        /// before any ImGui rendering.
        /// </summary>
        public unsafe void RebuildFontAtlas()
        {
            var io = ImGui.GetIO();
            io.Fonts.GetTexDataAsRGBA32(out byte* pixelData, out int width, out int height, out int bytesPerPixel);

            var pixels = new byte[width * height * bytesPerPixel];
            Marshal.Copy(new IntPtr(pixelData), pixels, 0, pixels.Length);

            var texture = new Texture2D(_graphicsDevice, width, height, false, SurfaceFormat.Color);
            texture.SetData(pixels);

            if (_fontTextureId.HasValue) UnbindTexture(_fontTextureId.Value);

            _fontTextureId = BindTexture(texture);
            io.Fonts.SetTexID(_fontTextureId.Value);
            io.Fonts.ClearTexData();
        }

        /// <summary>
        /// Registers a Texture2D with ImGui so it can be drawn via
        /// ImGui.Image(...). Returns the handle ImGui uses to refer to it.
        /// </summary>
        public IntPtr BindTexture(Texture2D texture)
        {
            var id = new IntPtr(_textureId++);
            _loadedTextures.Add(id, texture);
            return id;
        }

        public void UnbindTexture(IntPtr textureId) => _loadedTextures.Remove(textureId);

        /// <summary>Call at the very start of Draw, before any ImGui.* calls this frame.</summary>
        public void BeforeLayout(GameTime gameTime)
        {
            ImGui.GetIO().DeltaTime = (float)gameTime.ElapsedGameTime.TotalSeconds;
            UpdateInput();
            ImGui.NewFrame();
        }

        /// <summary>Call after all ImGui.* UI calls for this frame; actually renders them.</summary>
        public void AfterLayout()
        {
            ImGui.Render();
            unsafe { RenderDrawData(ImGui.GetDrawData()); }
        }

        private void SetupInput()
        {
            // MonoGame fires TextInput as actual typed characters (handles
            // shift/layout/etc for us), which ImGui text fields need.
            _game.Window.TextInput += (_, args) =>
            {
                if (args.Character == '\t') return;
                ImGui.GetIO().AddInputCharacter(args.Character);
            };
        }

        private void UpdateInput()
        {
            if (!_game.IsActive) return;

            var io = ImGui.GetIO();
            var mouse = Mouse.GetState();
            var keyboard = Keyboard.GetState();

            io.AddMousePosEvent(mouse.X, mouse.Y);
            io.AddMouseButtonEvent(0, mouse.LeftButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(1, mouse.RightButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(2, mouse.MiddleButton == ButtonState.Pressed);
            io.AddMouseButtonEvent(3, mouse.XButton1 == ButtonState.Pressed);
            io.AddMouseButtonEvent(4, mouse.XButton2 == ButtonState.Pressed);

            io.AddMouseWheelEvent(
                (mouse.HorizontalScrollWheelValue - _horizontalScrollWheelValue) / WheelDelta,
                (mouse.ScrollWheelValue - _scrollWheelValue) / WheelDelta);
            _scrollWheelValue = mouse.ScrollWheelValue;
            _horizontalScrollWheelValue = mouse.HorizontalScrollWheelValue;

            foreach (var key in _allKeys)
            {
                if (TryMapKey(key, out ImGuiKey imguiKey))
                    io.AddKeyEvent(imguiKey, keyboard.IsKeyDown(key));
            }

            io.DisplaySize = new System.Numerics.Vector2(
                _graphicsDevice.PresentationParameters.BackBufferWidth,
                _graphicsDevice.PresentationParameters.BackBufferHeight);
            io.DisplayFramebufferScale = new System.Numerics.Vector2(1f, 1f);
        }

        private static bool TryMapKey(Keys key, out ImGuiKey imguiKey)
        {
            if (key == Keys.None)
            {
                imguiKey = ImGuiKey.None;
                return true;
            }

            imguiKey = key switch
            {
                Keys.Back => ImGuiKey.Backspace,
                Keys.Tab => ImGuiKey.Tab,
                Keys.Enter => ImGuiKey.Enter,
                Keys.CapsLock => ImGuiKey.CapsLock,
                Keys.Escape => ImGuiKey.Escape,
                Keys.Space => ImGuiKey.Space,
                Keys.PageUp => ImGuiKey.PageUp,
                Keys.PageDown => ImGuiKey.PageDown,
                Keys.End => ImGuiKey.End,
                Keys.Home => ImGuiKey.Home,
                Keys.Left => ImGuiKey.LeftArrow,
                Keys.Right => ImGuiKey.RightArrow,
                Keys.Up => ImGuiKey.UpArrow,
                Keys.Down => ImGuiKey.DownArrow,
                Keys.PrintScreen => ImGuiKey.PrintScreen,
                Keys.Insert => ImGuiKey.Insert,
                Keys.Delete => ImGuiKey.Delete,
                >= Keys.D0 and <= Keys.D9 => ImGuiKey._0 + (key - Keys.D0),
                >= Keys.A and <= Keys.Z => ImGuiKey.A + (key - Keys.A),
                >= Keys.NumPad0 and <= Keys.NumPad9 => ImGuiKey.Keypad0 + (key - Keys.NumPad0),
                Keys.Multiply => ImGuiKey.KeypadMultiply,
                Keys.Add => ImGuiKey.KeypadAdd,
                Keys.Subtract => ImGuiKey.KeypadSubtract,
                Keys.Decimal => ImGuiKey.KeypadDecimal,
                Keys.Divide => ImGuiKey.KeypadDivide,
                >= Keys.F1 and <= Keys.F24 => ImGuiKey.F1 + (key - Keys.F1),
                Keys.NumLock => ImGuiKey.NumLock,
                Keys.Scroll => ImGuiKey.ScrollLock,
                Keys.LeftShift => ImGuiKey.ModShift,
                Keys.LeftControl => ImGuiKey.ModCtrl,
                Keys.LeftAlt => ImGuiKey.ModAlt,
                Keys.OemSemicolon => ImGuiKey.Semicolon,
                Keys.OemPlus => ImGuiKey.Equal,
                Keys.OemComma => ImGuiKey.Comma,
                Keys.OemMinus => ImGuiKey.Minus,
                Keys.OemPeriod => ImGuiKey.Period,
                Keys.OemQuestion => ImGuiKey.Slash,
                Keys.OemTilde => ImGuiKey.GraveAccent,
                Keys.OemOpenBrackets => ImGuiKey.LeftBracket,
                Keys.OemCloseBrackets => ImGuiKey.RightBracket,
                Keys.OemPipe => ImGuiKey.Backslash,
                Keys.OemQuotes => ImGuiKey.Apostrophe,
                Keys.BrowserBack => ImGuiKey.AppBack,
                Keys.BrowserForward => ImGuiKey.AppForward,
                _ => ImGuiKey.None,
            };

            return imguiKey != ImGuiKey.None;
        }

        private void RenderDrawData(ImDrawDataPtr drawData)
        {
            var lastViewport = _graphicsDevice.Viewport;
            var lastScissorBox = _graphicsDevice.ScissorRectangle;
            var lastRasterizer = _graphicsDevice.RasterizerState;
            var lastDepthStencil = _graphicsDevice.DepthStencilState;
            var lastBlendFactor = _graphicsDevice.BlendFactor;
            var lastBlendState = _graphicsDevice.BlendState;

            _graphicsDevice.BlendFactor = Color.White;
            _graphicsDevice.BlendState = BlendState.NonPremultiplied;
            _graphicsDevice.RasterizerState = _rasterizerState;
            _graphicsDevice.DepthStencilState = DepthStencilState.DepthRead;

            drawData.ScaleClipRects(ImGui.GetIO().DisplayFramebufferScale);

            _graphicsDevice.Viewport = new Viewport(
                0, 0,
                _graphicsDevice.PresentationParameters.BackBufferWidth,
                _graphicsDevice.PresentationParameters.BackBufferHeight);

            UpdateBuffers(drawData);
            RenderCommandLists(drawData);

            _graphicsDevice.Viewport = lastViewport;
            _graphicsDevice.ScissorRectangle = lastScissorBox;
            _graphicsDevice.RasterizerState = lastRasterizer;
            _graphicsDevice.DepthStencilState = lastDepthStencil;
            _graphicsDevice.BlendState = lastBlendState;
            _graphicsDevice.BlendFactor = lastBlendFactor;
        }

        private unsafe void UpdateBuffers(ImDrawDataPtr drawData)
        {
            if (drawData.TotalVtxCount == 0) return;

            if (drawData.TotalVtxCount > _vertexBufferSize)
            {
                _vertexBuffer?.Dispose();
                _vertexBufferSize = (int)(drawData.TotalVtxCount * 1.5f);
                _vertexBuffer = new VertexBuffer(_graphicsDevice, DrawVertDeclaration.Declaration, _vertexBufferSize, BufferUsage.None);
                _vertexData = new byte[_vertexBufferSize * DrawVertDeclaration.Size];
            }

            if (drawData.TotalIdxCount > _indexBufferSize)
            {
                _indexBuffer?.Dispose();
                _indexBufferSize = (int)(drawData.TotalIdxCount * 1.5f);
                _indexBuffer = new IndexBuffer(_graphicsDevice, IndexElementSize.SixteenBits, _indexBufferSize, BufferUsage.None);
                _indexData = new byte[_indexBufferSize * sizeof(ushort)];
            }

            int vtxOffset = 0;
            int idxOffset = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdLists[n];

                fixed (void* vtxDstPtr = &_vertexData[vtxOffset * DrawVertDeclaration.Size])
                fixed (void* idxDstPtr = &_indexData[idxOffset * sizeof(ushort)])
                {
                    Buffer.MemoryCopy((void*)cmdList.VtxBuffer.Data, vtxDstPtr, _vertexData.Length, cmdList.VtxBuffer.Size * DrawVertDeclaration.Size);
                    Buffer.MemoryCopy((void*)cmdList.IdxBuffer.Data, idxDstPtr, _indexData.Length, cmdList.IdxBuffer.Size * sizeof(ushort));
                }

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }

            _vertexBuffer.SetData(_vertexData, 0, drawData.TotalVtxCount * DrawVertDeclaration.Size);
            _indexBuffer.SetData(_indexData, 0, drawData.TotalIdxCount * sizeof(ushort));
        }

        private Effect UpdateEffect(Texture2D texture)
        {
            _effect ??= new BasicEffect(_graphicsDevice);

            var io = ImGui.GetIO();
            _effect.World = Matrix.Identity;
            _effect.View = Matrix.Identity;
            _effect.Projection = Matrix.CreateOrthographicOffCenter(0f, io.DisplaySize.X, io.DisplaySize.Y, 0f, -1f, 1f);
            _effect.TextureEnabled = true;
            _effect.Texture = texture;
            _effect.VertexColorEnabled = true;

            return _effect;
        }

        private void RenderCommandLists(ImDrawDataPtr drawData)
        {
            _graphicsDevice.SetVertexBuffer(_vertexBuffer);
            _graphicsDevice.Indices = _indexBuffer;

            int vtxOffset = 0;
            int idxOffset = 0;

            for (int n = 0; n < drawData.CmdListsCount; n++)
            {
                ImDrawListPtr cmdList = drawData.CmdLists[n];

                for (int cmdi = 0; cmdi < cmdList.CmdBuffer.Size; cmdi++)
                {
                    ImDrawCmdPtr drawCmd = cmdList.CmdBuffer[cmdi];
                    if (drawCmd.ElemCount == 0) continue;

                    if (!_loadedTextures.ContainsKey(drawCmd.TextureId))
                        throw new InvalidOperationException($"No texture bound for id '{drawCmd.TextureId}' — check BindTexture calls.");

                    _graphicsDevice.ScissorRectangle = new Rectangle(
                        (int)drawCmd.ClipRect.X,
                        (int)drawCmd.ClipRect.Y,
                        (int)(drawCmd.ClipRect.Z - drawCmd.ClipRect.X),
                        (int)(drawCmd.ClipRect.W - drawCmd.ClipRect.Y));

                    var effect = UpdateEffect(_loadedTextures[drawCmd.TextureId]);

                    foreach (var pass in effect.CurrentTechnique.Passes)
                    {
                        pass.Apply();
#pragma warning disable CS0618
                        _graphicsDevice.DrawIndexedPrimitives(
                            primitiveType: PrimitiveType.TriangleList,
                            baseVertex: (int)drawCmd.VtxOffset + vtxOffset,
                            minVertexIndex: 0,
                            numVertices: cmdList.VtxBuffer.Size,
                            startIndex: (int)drawCmd.IdxOffset + idxOffset,
                            primitiveCount: (int)drawCmd.ElemCount / 3);
#pragma warning restore CS0618
                    }
                }

                vtxOffset += cmdList.VtxBuffer.Size;
                idxOffset += cmdList.IdxBuffer.Size;
            }
        }
    }
}