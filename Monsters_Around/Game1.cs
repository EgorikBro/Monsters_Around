using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;
using System.Collections.Generic;
using System.Text;

namespace Monsters_Around
{
    public class Game1 : Game
    {
        private enum PauseScreen
        {
            Main,
            Controls
        }

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Map _map;
        private Player _player;
        private Camera2D _camera;
        private Texture2D _tilesetTexture;
        private readonly Dictionary<int, Map> _generatedFloors = new Dictionary<int, Map>();
        private readonly Random _random = new Random();
        private int _currentFloorIndex;
        private bool _stairTransitionLock;
        private bool _isApplyingDisplayChange;
        private int _windowedWidth = WindowWidth;
        private int _windowedHeight = WindowHeight;
        private bool _showFps;
        private bool _showMapOverlay;
        private SpriteFont _debugFont;
        private Texture2D _uiPixel;
        private float _fpsTimer;
        private int _fpsFrameCounter;
        private int _currentFps;

        private bool _isPaused;
        private PauseScreen _pauseScreen = PauseScreen.Main;
        private MouseState _prevMouseState;
        private int _pauseKeyboardIndex;
        private bool _gamePadBackHeld;
        private bool _voluntaryExitRequested;

        private const int TileSize = 16;
        private const int WindowWidth = 800;
        private const int WindowHeight = 480;
        private const int MapWidth = 70;
        private const int MapHeight = 70;
        private const float CameraZoom = 5f;

        private Texture2D _dummyPlayerTex;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnClientSizeChanged;
            _graphics.HardwareModeSwitch = false;
            _graphics.IsFullScreen = false;

            _graphics.PreferredBackBufferWidth = WindowWidth;
            _graphics.PreferredBackBufferHeight = WindowHeight;

            Exiting += OnGameExiting;
        }

        protected override void Initialize()
        {
            _map = GetOrCreateFloor(0);
            _player = new Player(_map.PlayerSpawnPoint, _map);
            _map.UpdateExploration(_player.Position);
            _camera = new Camera2D(CameraZoom);

            base.Initialize();
            SetFullscreen(true);
            IsMouseVisible = false;
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            _tilesetTexture = Content.Load<Texture2D>("Dungeon tileset");
            _debugFont = Content.Load<SpriteFont>("DebugFont");
            _uiPixel = new Texture2D(GraphicsDevice, 1, 1);
            _uiPixel.SetData(new[] { Color.White });

            _dummyPlayerTex = new Texture2D(GraphicsDevice, 1, 1);
            _dummyPlayerTex.SetData(new[] { Color.Green });

            foreach (var floor in _generatedFloors.Values)
            {
                floor.LoadContent(_tilesetTexture);
            }

            _player.LoadContent(_dummyPlayerTex);
            _prevMouseState = Mouse.GetState();
            _gamePadBackHeld = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;
        }

        protected override void Update(GameTime gameTime)
        {
            if (_voluntaryExitRequested)
            {
                Exit();
                base.Update(gameTime);
                return;
            }

            InputHandler.Update();

            var mouse = Mouse.GetState();
            var gamePad = GamePad.GetState(PlayerIndex.One);
            var gamePadBackNow = gamePad.Buttons.Back == ButtonState.Pressed;
            var gamePadBackEdge = gamePadBackNow && !_gamePadBackHeld;
            _gamePadBackHeld = gamePadBackNow;

            if (_isPaused)
            {
                UpdatePauseMenu(mouse);
                _prevMouseState = mouse;
                base.Update(gameTime);
                return;
            }

            if (InputHandler.IsKeyPressed(Keys.Escape) || gamePadBackEdge)
            {
                OpenPauseMenu();
                _prevMouseState = mouse;
                base.Update(gameTime);
                return;
            }

            if (InputHandler.IsKeyPressed(Keys.F11))
            {
                ToggleFullscreen();
            }
            if (InputHandler.IsKeyPressed(Keys.F1))
            {
                _showFps = !_showFps;
            }
            if (InputHandler.IsKeyPressed(Keys.M))
            {
                _showMapOverlay = !_showMapOverlay;
            }

            _player.Update(gameTime);
            HandleStairTransitions();
            _map.UpdateExploration(_player.Position);
            UpdateFpsCounter(gameTime);
            _camera.Follow(
                _player.WorldPosition + new Vector2(TileSize * 0.5f, TileSize * 0.5f),
                GraphicsDevice.Viewport,
                new Point(MapWidth * TileSize, MapHeight * TileSize)
            );

            _prevMouseState = mouse;
            base.Update(gameTime);
        }

        private void OpenPauseMenu()
        {
            _isPaused = true;
            _pauseScreen = PauseScreen.Main;
            _pauseKeyboardIndex = 0;
            IsMouseVisible = true;
        }

        private void ClosePauseMenu()
        {
            _isPaused = false;
            _pauseScreen = PauseScreen.Main;
            IsMouseVisible = false;
        }

        private void UpdatePauseMenu(MouseState mouse)
        {
            if (InputHandler.IsKeyPressed(Keys.Escape))
            {
                if (_pauseScreen == PauseScreen.Controls)
                {
                    _pauseScreen = PauseScreen.Main;
                    _pauseKeyboardIndex = 3;
                    return;
                }

                ClosePauseMenu();
                return;
            }

            var vp = GraphicsDevice.Viewport;
            const int btnW = 320;
            const int btnH = 44;
            const int gap = 10;
            const int panelPad = 28;

            var mainBtnCount = 5;
            var mainPanelH = panelPad * 2 + 52 + mainBtnCount * btnH + (mainBtnCount - 1) * gap;
            var mainPanel = new Rectangle(
                (vp.Width - btnW - panelPad * 2) / 2,
                (vp.Height - mainPanelH) / 2,
                btnW + panelPad * 2,
                mainPanelH
            );

            var btnLeft = mainPanel.X + panelPad;
            var btnTop = mainPanel.Y + panelPad + 52;

            if (_pauseScreen == PauseScreen.Main)
            {
                if (InputHandler.IsKeyPressed(Keys.Down))
                {
                    _pauseKeyboardIndex = (_pauseKeyboardIndex + 1) % mainBtnCount;
                }
                if (InputHandler.IsKeyPressed(Keys.Up))
                {
                    _pauseKeyboardIndex = (_pauseKeyboardIndex - 1 + mainBtnCount) % mainBtnCount;
                }

                for (var i = 0; i < mainBtnCount; i++)
                {
                    var r = new Rectangle(btnLeft, btnTop + i * (btnH + gap), btnW, btnH);
                    var hover = r.Contains(mouse.X, mouse.Y);
                    if (hover)
                    {
                        _pauseKeyboardIndex = i;
                    }

                    var click = mouse.LeftButton == ButtonState.Released &&
                                 _prevMouseState.LeftButton == ButtonState.Pressed &&
                                 hover;
                    var activate = click || (InputHandler.IsKeyPressed(Keys.Enter) && _pauseKeyboardIndex == i);

                    if (!activate)
                    {
                        continue;
                    }

                    switch (i)
                    {
                        case 0:
                            ClosePauseMenu();
                            return;
                        case 1:
                        case 2:
                            break;
                        case 3:
                            _pauseScreen = PauseScreen.Controls;
                            _pauseKeyboardIndex = 0;
                            return;
                        case 4:
                            RequestVoluntaryExit();
                            return;
                    }
                }
            }
            else
            {
                ComputeControlsPauseLayout(vp, out _, out var backBtn, out _, out _, out _);
                var backHover = backBtn.Contains(mouse.X, mouse.Y);
                var backClick = mouse.LeftButton == ButtonState.Released &&
                                 _prevMouseState.LeftButton == ButtonState.Pressed &&
                                 backHover;
                if (backClick || InputHandler.IsKeyPressed(Keys.Enter) || InputHandler.IsKeyPressed(Keys.Back))
                {
                    _pauseScreen = PauseScreen.Main;
                    _pauseKeyboardIndex = 3;
                }
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(
                samplerState: SamplerState.PointClamp,
                transformMatrix: _camera.GetViewMatrix()
            );

            _map.Draw(_spriteBatch);
            _player.Draw(_spriteBatch);

            _spriteBatch.End();

            if (_showFps && _debugFont != null)
            {
                DrawFpsCounter();
            }

            DrawMinimap();
            if (_showMapOverlay)
            {
                DrawFullMapOverlay();
            }

            if (_isPaused)
            {
                DrawPauseMenu();
            }

            base.Draw(gameTime);
        }

        private void OnGameExiting(object sender, EventArgs args)
        {
            if (_voluntaryExitRequested)
            {
                return;
            }

            if (args is not ExitingEventArgs exitingArgs)
            {
                return;
            }

            exitingArgs.Cancel = true;
            _isPaused = true;
            _pauseScreen = PauseScreen.Main;
            _pauseKeyboardIndex = 0;
            IsMouseVisible = true;
        }

        private void RequestVoluntaryExit()
        {
            _voluntaryExitRequested = true;
        }

        private void DrawPauseMenu()
        {
            if (_debugFont == null || _uiPixel == null)
            {
                return;
            }

            var vp = GraphicsDevice.Viewport;
            const int btnW = 320;
            const int btnH = 44;
            const int gap = 10;
            const int panelPad = 28;

            var overlay = Color.Black * 0.55f;
            var panelBg = new Color(22, 33, 62);
            var panelBorder = new Color(233, 69, 96);
            var btnIdle = new Color(15, 52, 96);
            var btnHover = new Color(26, 76, 130);
            var btnDisabled = new Color(40, 45, 60);
            var titleColor = new Color(238, 238, 238);
            var accent = new Color(233, 69, 96);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            try
            {
            _spriteBatch.Draw(_uiPixel, new Rectangle(0, 0, vp.Width, vp.Height), overlay);

            if (_pauseScreen == PauseScreen.Main)
            {
                var mainBtnCount = 5;
                var mainPanelH = panelPad * 2 + 52 + mainBtnCount * btnH + (mainBtnCount - 1) * gap;
                var mainPanel = new Rectangle(
                    (vp.Width - btnW - panelPad * 2) / 2,
                    (vp.Height - mainPanelH) / 2,
                    btnW + panelPad * 2,
                    mainPanelH
                );

                DrawPausePanelFrame(mainPanel, panelBg, panelBorder);
                var titlePos = new Vector2(mainPanel.X + panelPad, mainPanel.Y + panelPad);
                DrawPauseTitle("ПАУЗА", titlePos, accent, titleColor);

                var mouse = Mouse.GetState();
                var btnLeft = mainPanel.X + panelPad;
                var btnTop = mainPanel.Y + panelPad + 52;

                var labels = new[] { "Продолжить", "Сохранить", "Загрузить", "Управление", "Выйти из игры" };
                var disabled = new[] { false, true, true, false, false };

                for (var i = 0; i < labels.Length; i++)
                {
                    var r = new Rectangle(btnLeft, btnTop + i * (btnH + gap), btnW, btnH);
                    var hover = !disabled[i] && r.Contains(mouse.X, mouse.Y);
                    var selected = _pauseKeyboardIndex == i;
                    var fill = disabled[i] ? btnDisabled : (hover || selected ? btnHover : btnIdle);
                    DrawPauseButton(r, fill, labels[i], disabled[i]);
                }
            }
            else
            {
                ComputeControlsPauseLayout(vp, out var controlsPanel, out var backBtn, out var textOrigin, out var wrappedBody, out var textScale);

                DrawPausePanelFrame(controlsPanel, panelBg, panelBorder);
                DrawPauseTitle("Управление", new Vector2(controlsPanel.X + panelPad, controlsPanel.Y + panelPad), accent, titleColor);

                var shadowOffset = Vector2.One * Math.Max(1f, textScale);
                _spriteBatch.DrawString(_debugFont, wrappedBody, textOrigin + shadowOffset, Color.Black * 0.35f, 0f, Vector2.Zero, new Vector2(textScale), SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_debugFont, wrappedBody, textOrigin, new Color(220, 225, 235), 0f, Vector2.Zero, new Vector2(textScale), SpriteEffects.None, 0f);

                var mouse = Mouse.GetState();
                var backHover = backBtn.Contains(mouse.X, mouse.Y);
                DrawPauseButton(backBtn, backHover ? btnHover : btnIdle, "Назад", false);
            }

            }
            finally
            {
                _spriteBatch.End();
            }
        }

        protected override void UnloadContent()
        {
            _spriteBatch?.Dispose();
            _spriteBatch = null;
            base.UnloadContent();
        }

        private static string GetControlsHelpText()
        {
            return
                "Движение по клеткам\n" +
                "  WASD или стрелки - шаг по сетке\n\n" +
                "Камера и экран\n" +
                "  F11 - полный экран / окно\n\n" +
                "Интерфейс\n" +
                "  F1 - счётчик FPS\n" +
                "  M - большая карта этажа\n" +
                "  Мини-карта - всегда слева снизу\n\n" +
                "Подземелье\n" +
                "  Лестница вниз / вверх - переход между этажами\n\n" +
                "Пауза\n" +
                "  Esc - меню паузы";
        }

        private static string WrapTextToWidth(SpriteFont font, string text, float maxWidth)
        {
            if (font == null || maxWidth < 48f)
            {
                return text;
            }

            var sb = new StringBuilder();
            foreach (var rawLine in text.Split('\n'))
            {
                var line = rawLine.TrimEnd('\r');
                if (line.Length == 0)
                {
                    sb.AppendLine();
                    continue;
                }

                var indent = line.StartsWith("  ", StringComparison.Ordinal) ? "  " : "";
                var rest = indent.Length > 0 ? line.Substring(2).TrimStart() : line.TrimStart();
                if (rest.Length == 0)
                {
                    sb.AppendLine();
                    continue;
                }

                var indentW = font.MeasureString(indent).X;
                var lineBudget = Math.Max(48f, maxWidth - indentW);

                var words = rest.Split(new[] { ' ' }, StringSplitOptions.RemoveEmptyEntries);
                var lineSb = new StringBuilder();
                foreach (var word in words)
                {
                    var candidate = lineSb.Length == 0 ? word : lineSb + " " + word;
                    if (font.MeasureString(candidate).X > lineBudget && lineSb.Length > 0)
                    {
                        sb.Append(indent).AppendLine(lineSb.ToString());
                        lineSb.Clear();
                        lineSb.Append(word);
                    }
                    else
                    {
                        if (lineSb.Length > 0)
                        {
                            lineSb.Append(' ');
                        }

                        lineSb.Append(word);
                    }
                }

                if (lineSb.Length > 0)
                {
                    sb.Append(indent).AppendLine(lineSb.ToString());
                }
            }

            return sb.ToString();
        }

        private void ComputeControlsPauseLayout(
            Viewport vp,
            out Rectangle controlsPanel,
            out Rectangle backBtn,
            out Vector2 textOrigin,
            out string wrappedBody,
            out float textScale)
        {
            const int btnH = 44;
            const int gap = 10;
            const int panelPad = 28;
            const int contentTopOffset = 52;
            const int maxPanelOuterW = 560;
            const int marginV = 16;

            var panelOuterW = Math.Min(maxPanelOuterW, Math.Max(376, vp.Width - 48));
            var innerW = panelOuterW - panelPad * 2;

            wrappedBody = WrapTextToWidth(_debugFont, GetControlsHelpText(), innerW);
            var sz = _debugFont.MeasureString(wrappedBody);

            var reservedTop = panelPad + contentTopOffset;
            var reservedBottom = gap + btnH + panelPad;
            var maxPanelH = Math.Max(btnH + reservedTop + 8, vp.Height - marginV);
            var maxTextH = Math.Max(24, maxPanelH - reservedTop - reservedBottom);

            textScale = sz.Y <= 0 ? 1f : Math.Min(1f, maxTextH / sz.Y);
            var scaledTextH = (int)Math.Ceiling(sz.Y * textScale);
            if (scaledTextH > maxTextH && sz.Y > 0)
            {
                textScale = maxTextH / sz.Y;
                scaledTextH = maxTextH;
            }

            var panelH = reservedTop + scaledTextH + reservedBottom;
            panelH = Math.Min(panelH, maxPanelH);

            controlsPanel = new Rectangle(
                (vp.Width - panelOuterW) / 2,
                (vp.Height - panelH) / 2,
                panelOuterW,
                panelH);

            textOrigin = new Vector2(controlsPanel.X + panelPad, controlsPanel.Y + reservedTop);
            backBtn = new Rectangle(
                controlsPanel.X + panelPad,
                controlsPanel.Bottom - panelPad - btnH,
                innerW,
                btnH);
        }

        private void DrawPausePanelFrame(Rectangle panel, Color fill, Color border)
        {
            _spriteBatch.Draw(_uiPixel, panel, fill);
            var t = 3;
            _spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Y, panel.Width, t), border);
            _spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Bottom - t, panel.Width, t), border);
            _spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Y, t, panel.Height), border);
            _spriteBatch.Draw(_uiPixel, new Rectangle(panel.Right - t, panel.Y, t, panel.Height), border);
        }

        private void DrawPauseTitle(string title, Vector2 position, Color accent, Color textColor)
        {
            _spriteBatch.DrawString(_debugFont, title, position + Vector2.One * 2, Color.Black * 0.45f);
            _spriteBatch.DrawString(_debugFont, title, position, textColor);
            var w = _debugFont.MeasureString(title).X;
            _spriteBatch.Draw(_uiPixel, new Rectangle((int)position.X, (int)(position.Y + _debugFont.LineSpacing + 6), (int)w, 3), accent);
        }

        private void DrawPauseButton(Rectangle rect, Color fill, string text, bool disabled)
        {
            _spriteBatch.Draw(_uiPixel, rect, fill);
            var inset = 2;
            _spriteBatch.Draw(_uiPixel, new Rectangle(rect.X + inset, rect.Y + inset, rect.Width - inset * 2, rect.Height - inset * 2), Color.Black * 0.25f);

            var label = disabled ? $"{text}  (скоро)" : text;
            var size = _debugFont.MeasureString(label);
            var pos = new Vector2(
                rect.X + (rect.Width - size.X) * 0.5f,
                rect.Y + (rect.Height - size.Y) * 0.5f
            );
            var textColor = disabled ? new Color(140, 145, 160) : Color.White;
            _spriteBatch.DrawString(_debugFont, label, pos + Vector2.One, Color.Black * 0.5f);
            _spriteBatch.DrawString(_debugFont, label, pos, textColor);
        }

        private void UpdateFpsCounter(GameTime gameTime)
        {
            _fpsTimer += (float)gameTime.ElapsedGameTime.TotalSeconds;
            _fpsFrameCounter++;

            if (_fpsTimer >= 1f)
            {
                _currentFps = _fpsFrameCounter;
                _fpsFrameCounter = 0;
                _fpsTimer -= 1f;
            }
        }

        private void DrawFpsCounter()
        {
            var fpsText = $"FPS: {_currentFps}";
            var textSize = _debugFont.MeasureString(fpsText);
            var position = new Vector2(
                GraphicsDevice.Viewport.Width - textSize.X - 10f,
                10f
            );

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.DrawString(_debugFont, fpsText, position + new Vector2(1f, 1f), Color.Black);
            _spriteBatch.DrawString(_debugFont, fpsText, position, Color.White);
            _spriteBatch.End();
        }

        private void DrawFullMapOverlay()
        {
            var size = Math.Min(GraphicsDevice.Viewport.Width, GraphicsDevice.Viewport.Height) - 120;
            size = Math.Max(220, size);
            var mapRect = new Rectangle(
                (GraphicsDevice.Viewport.Width - size) / 2,
                (GraphicsDevice.Viewport.Height - size) / 2,
                size,
                size
            );

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_uiPixel, mapRect, Color.Black * 0.65f);
            DrawMapTiles(mapRect, false);
            _spriteBatch.End();
        }

        private void DrawMinimap()
        {
            var diameter = 230;
            var padding = 16;
            const int minimapVisibleTiles = 16;
            var miniRect = new Rectangle(
                padding,
                GraphicsDevice.Viewport.Height - diameter - padding,
                diameter,
                diameter
            );

            var viewWidth = Math.Min(minimapVisibleTiles, _map.Width);
            var viewHeight = Math.Min(minimapVisibleTiles, _map.Height);
            var playerMapX = _player.WorldPosition.X / TileSize;
            var playerMapY = _player.WorldPosition.Y / TileSize;
            var originX = Math.Clamp(playerMapX - viewWidth / 2f, 0f, _map.Width - viewWidth);
            var originY = Math.Clamp(playerMapY - viewHeight / 2f, 0f, _map.Height - viewHeight);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            DrawCircularBackdrop(miniRect, Color.Black * 0.6f);
            DrawMapTiles(miniRect, true, originX, originY, viewWidth, viewHeight);
            _spriteBatch.End();
        }

        private void DrawMapTiles(Rectangle targetRect, bool circularMask, float? originX = null, float? originY = null, int? viewWidth = null, int? viewHeight = null)
        {
            var startX = originX ?? 0f;
            var startY = originY ?? 0f;
            var visibleW = viewWidth ?? _map.Width;
            var visibleH = viewHeight ?? _map.Height;

            var cellW = targetRect.Width / (float)visibleW;
            var cellH = targetRect.Height / (float)visibleH;
            var centerX = targetRect.X + targetRect.Width * 0.5f;
            var centerY = targetRect.Y + targetRect.Height * 0.5f;
            var radius = targetRect.Width * 0.5f;

            var tileStartX = Math.Max(0, (int)Math.Floor(startX));
            var tileStartY = Math.Max(0, (int)Math.Floor(startY));
            var tileEndX = Math.Min(_map.Width, (int)Math.Ceiling(startX + visibleW));
            var tileEndY = Math.Min(_map.Height, (int)Math.Ceiling(startY + visibleH));

            for (var x = tileStartX; x < tileEndX; x++)
            {
                for (var y = tileStartY; y < tileEndY; y++)
                {
                    if (!_map.IsExplored(x, y))
                    {
                        continue;
                    }

                    var cellRect = new Rectangle(
                        targetRect.X + (int)MathF.Floor((x - startX) * cellW),
                        targetRect.Y + (int)MathF.Floor((y - startY) * cellH),
                        Math.Max(1, (int)Math.Ceiling(cellW)),
                        Math.Max(1, (int)Math.Ceiling(cellH))
                    );

                    if (circularMask)
                    {
                        var px = cellRect.Center.X;
                        var py = cellRect.Center.Y;
                        var dx = px - centerX;
                        var dy = py - centerY;
                        if (dx * dx + dy * dy > radius * radius)
                        {
                            continue;
                        }
                    }

                    var type = _map.GetTileType(x, y);
                    var color = type == TileType.Wall ? new Color(70, 70, 80, 190) : new Color(190, 190, 200, 230);
                    if (type == TileType.StairDown) color = Color.OrangeRed;
                    if (type == TileType.StairUp) color = Color.CornflowerBlue;

                    _spriteBatch.Draw(_uiPixel, cellRect, color);
                }
            }

            DrawPlayerDot(targetRect, circularMask, startX, startY, visibleW, visibleH);
        }

        private void DrawPlayerDot(Rectangle targetRect, bool circularMask, float startX, float startY, int visibleW, int visibleH)
        {
            var cellW = targetRect.Width / (float)visibleW;
            var cellH = targetRect.Height / (float)visibleH;
            var playerMapX = _player.WorldPosition.X / TileSize;
            var playerMapY = _player.WorldPosition.Y / TileSize;
            var playerRect = new Rectangle(
                targetRect.X + (int)((playerMapX - startX) * cellW),
                targetRect.Y + (int)((playerMapY - startY) * cellH),
                Math.Max(2, (int)Math.Ceiling(cellW)),
                Math.Max(2, (int)Math.Ceiling(cellH))
            );

            if (circularMask)
            {
                var centerX = targetRect.X + targetRect.Width * 0.5f;
                var centerY = targetRect.Y + targetRect.Height * 0.5f;
                var radius = targetRect.Width * 0.5f;
                var dx = playerRect.Center.X - centerX;
                var dy = playerRect.Center.Y - centerY;
                if (dx * dx + dy * dy > radius * radius)
                {
                    return;
                }
            }

            _spriteBatch.Draw(_uiPixel, playerRect, Color.LimeGreen);
        }

        private void DrawCircularBackdrop(Rectangle rect, Color color)
        {
            var centerX = rect.X + rect.Width * 0.5f;
            var centerY = rect.Y + rect.Height * 0.5f;
            var radius = rect.Width * 0.5f;

            for (var x = rect.Left; x < rect.Right; x++)
            {
                for (var y = rect.Top; y < rect.Bottom; y++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx + dy * dy <= radius * radius)
                    {
                        _spriteBatch.Draw(_uiPixel, new Rectangle(x, y, 1, 1), color);
                    }
                }
            }
        }

        private void ToggleFullscreen()
        {
            SetFullscreen(!_graphics.IsFullScreen);
        }

        private void OnClientSizeChanged(object sender, System.EventArgs e)
        {
            if (_isApplyingDisplayChange || _graphics.IsFullScreen)
            {
                return;
            }

            var width = Window.ClientBounds.Width;
            var height = Window.ClientBounds.Height;
            if (width <= 0 || height <= 0)
            {
                return;
            }

            _windowedWidth = width;
            _windowedHeight = height;
            _graphics.PreferredBackBufferWidth = width;
            _graphics.PreferredBackBufferHeight = height;
        }

        private void SetFullscreen(bool isFullscreen)
        {
            if (_isApplyingDisplayChange)
            {
                return;
            }

            _isApplyingDisplayChange = true;
            try
            {
                if (isFullscreen)
                {
                    _windowedWidth = Window.ClientBounds.Width > 0 ? Window.ClientBounds.Width : _windowedWidth;
                    _windowedHeight = Window.ClientBounds.Height > 0 ? Window.ClientBounds.Height : _windowedHeight;

                    var displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                    _graphics.PreferredBackBufferWidth = displayMode.Width;
                    _graphics.PreferredBackBufferHeight = displayMode.Height;
                }
                else
                {
                    _graphics.PreferredBackBufferWidth = _windowedWidth;
                    _graphics.PreferredBackBufferHeight = _windowedHeight;
                }

                _graphics.IsFullScreen = isFullscreen;
                _graphics.ApplyChanges();
            }
            finally
            {
                _isApplyingDisplayChange = false;
            }
        }

        private Map GetOrCreateFloor(int floorIndex)
        {
            if (_generatedFloors.TryGetValue(floorIndex, out var existingMap))
            {
                return existingMap;
            }

            var map = new Map(MapWidth, MapHeight, TileSize, floorIndex, _random);
            if (_tilesetTexture != null)
            {
                map.LoadContent(_tilesetTexture);
            }

            _generatedFloors[floorIndex] = map;
            return map;
        }

        private void HandleStairTransitions()
        {
            var currentTile = _map.GetTileType(_player.Position.X, _player.Position.Y);
            if (currentTile != TileType.StairDown && currentTile != TileType.StairUp)
            {
                _stairTransitionLock = false;
                return;
            }

            if (_stairTransitionLock)
            {
                return;
            }

            if (currentTile == TileType.StairDown)
            {
                MoveToFloor(_currentFloorIndex + 1, true);
            }
            else if (currentTile == TileType.StairUp && _currentFloorIndex > 0)
            {
                MoveToFloor(_currentFloorIndex - 1, false);
            }

            _stairTransitionLock = true;
        }

        private void MoveToFloor(int targetFloorIndex, bool comingFromAbove)
        {
            _currentFloorIndex = targetFloorIndex;
            _map = GetOrCreateFloor(_currentFloorIndex);

            var spawnPoint = comingFromAbove ? _map.StairUpPoint : _map.StairDownPoint;
            _player.SetMapAndPosition(_map, spawnPoint);
            _map.UpdateExploration(_player.Position);
        }
    }
}
