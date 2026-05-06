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

        private readonly List<Enemy> _enemies = new List<Enemy>();
        private readonly HashSet<Point> _enemyPositions = new HashSet<Point>();

        private bool _justResolvedHeroBump;

        private bool _isGameOver;
        private int _gameOverSelectedIndex;

        private bool _heroActionLocked;
        private float _pendingEnemyCounterDelayRemaining;
        private Enemy _pendingEnemyCounter;
        private bool _pendingEnemyActionPhase;
        private bool _pendingEnemyActionAllowMovement;
        private int _heroStepsSinceEnemyTurn;

        private float _heroBumpAnimTimer;
        private Vector2 _heroBumpAnimDirection;
        private readonly Dictionary<Enemy, float> _enemyBumpAnimTimers = new Dictionary<Enemy, float>();
        private readonly Dictionary<Enemy, Vector2> _enemyBumpAnimDirections = new Dictionary<Enemy, Vector2>();
        private readonly List<DamagePopup> _damagePopups = new List<DamagePopup>();
        private readonly List<CombatLogEntry> _combatLogEntries = new List<CombatLogEntry>();
        private readonly List<CombatLogHistoryItem> _combatLogHistory = new List<CombatLogHistoryItem>();

        private const int HeroMaxHealth = 100;
        private int _heroHealth = HeroMaxHealth;
        private const int HeroDefense = 5;

        private const int EnemyMaxHealth = 30;
        private const int EnemyDefense = 7;
        private const int HeroDamageMin = 4;
        private const int HeroDamageMax = 7;
        private const int HeroCritDamage = 10;
        private const float HeroCritChance = 0.16f;

        private const int EnemyDamageMin = 2;
        private const int EnemyDamageMax = 5;
        private const int EnemyCritDamage = 8;
        private const float EnemyCritChance = 0.14f;
        private const float HeroMissChance = 0.05f;
        private const float EnemyMissChance = 0.05f;

        private const float EnemyCounterDelaySeconds = 0.22f;
        private const float EnemyActionDelaySeconds = 0.5f;

        private float _edgeFlashStrength;
        private const float EdgeCriticalStrength = 0.16f;
        private const float EdgeFlashDurationSeconds = 0.35f;
        private const float BumpAnimDurationSeconds = 0.09f;
        private const float BumpAnimAmplitudePx = 4f;

        private float _heroOverHeadHpBarTimer;
        private const float HeroOverHeadHpBarDurationSeconds = 1.0f;
        private int _heroTurnsSinceRegen;
        private const int CombatLogVisibleCount = 3;
        private const int CombatLogExpandedVisibleCount = 7;
        private const float CombatLogLineHeight = 28f;
        private const float CombatLogTextScale = 1.2f;
        private const float CombatLogFadeSpeed = 2.8f;
        private const float CombatLogMoveSpeed = 16f;
        private bool _isCombatLogExpanded;
        private int _combatLogScrollOffset;
        private bool _combatLogScrollbarDragging;
        private float _combatLogScrollbarGrabOffset;
        private int _lastScrollWheelValue;

        private float _gameCursorAlpha;
        private float _mouseIdleTime;
        private Point _lastMousePosition;
        private const float MouseCursorIdleBeforeFade = 0.5f;
        private const float MouseCursorFadeSpeed = 2.2f;

        private struct DamagePopup
        {
            public Vector2 WorldPosition;
            public int Value;
            public bool IsCrit;
            public float TimeLeft;
            public float Lifetime;
        }

        private class CombatLogEntry
        {
            public string Text;
            public Color Color;
            public float Y;
            public float TargetY;
            public float Alpha;
            public bool IsExiting;
        }

        private struct CombatLogHistoryItem
        {
            public string Text;
            public Color Color;
        }

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
            _player.IsEnemyAt = IsEnemyAt;
            _player.OnBumpRequested = HandleHeroBumpIntoEnemy;
            _map.UpdateExploration(_player.Position);
            SpawnEnemiesForCurrentMap();
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
            _lastScrollWheelValue = _prevMouseState.ScrollWheelValue;
            _lastMousePosition = _prevMouseState.Position;
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
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var gamePad = GamePad.GetState(PlayerIndex.One);
            var gamePadBackNow = gamePad.Buttons.Back == ButtonState.Pressed;
            var gamePadBackEdge = gamePadBackNow && !_gamePadBackHeld;
            _gamePadBackHeld = gamePadBackNow;

            if (InputHandler.IsKeyPressed(Keys.T))
            {
                _isCombatLogExpanded = !_isCombatLogExpanded;
                _combatLogScrollbarDragging = false;
                ClampCombatLogScroll();
                if (_isCombatLogExpanded)
                {
                    _combatLogScrollOffset = Math.Max(0, _combatLogHistory.Count - CombatLogExpandedVisibleCount);
                }
            }

            if (!_isPaused && !_isGameOver)
            {
                UpdateGameplayCursor(mouse, dt);
                UpdateCombatLogExpandedInput(mouse);
            }

            if (_isGameOver)
            {
                UpdateGameOverMenu(mouse);
                _prevMouseState = mouse;
                base.Update(gameTime);
                return;
            }

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
            if (_edgeFlashStrength > 0f)
            {
                _edgeFlashStrength = Math.Max(0f, _edgeFlashStrength - dt / EdgeFlashDurationSeconds);
            }

            if (_heroOverHeadHpBarTimer > 0f)
            {
                _heroOverHeadHpBarTimer = Math.Max(0f, _heroOverHeadHpBarTimer - dt);
            }
            UpdateDamagePopups(dt);
            UpdateCombatLog(dt);

            if (_heroBumpAnimTimer > 0f)
            {
                _heroBumpAnimTimer = Math.Max(0f, _heroBumpAnimTimer - dt);
            }

            if (_heroActionLocked)
            {
                UpdatePendingEnemyCounter(dt);
            }
            else
            {
                var heroPosBefore = _player.Position;
                var mapBefore = _map;
                _justResolvedHeroBump = false;

                if (InputHandler.IsKeyPressed(Keys.Space) && !_player.IsMoving)
                {
                    AdvanceHeroTurnAndRegen();
                    _heroStepsSinceEnemyTurn++;
                    var allowMovement = _heroStepsSinceEnemyTurn >= 2;
                    if (allowMovement)
                    {
                        _heroStepsSinceEnemyTurn = 0;
                    }

                    ProcessEnemyTurn(allowMovement);
                    HandleStairTransitions();
                }
                else
                {
                    _player.Update(gameTime);
                }

                if (!_heroActionLocked)
                {
                    HandleStairTransitions();
                }

                if (!_heroActionLocked && _map == mapBefore && !_justResolvedHeroBump && _player.Position != heroPosBefore)
                {
                    AdvanceHeroTurnAndRegen();
                    _heroStepsSinceEnemyTurn++;
                    var allowMovement = _heroStepsSinceEnemyTurn >= 2;
                    if (allowMovement)
                    {
                        _heroStepsSinceEnemyTurn = 0;
                    }

                    ProcessEnemyTurn(allowMovement);
                    HandleStairTransitions();
                }
            }

            UpdateEnemies(gameTime);
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
            DrawEnemies();
            _player.Draw(_spriteBatch, GetHeroBumpDrawOffset());
            DrawHeroOverHeadHpBar();
            DrawDamagePopups();

            _spriteBatch.End();

            if (_showFps && _debugFont != null)
            {
                DrawFpsCounter();
            }

            DrawMinimap();
            DrawCombatLog();
            if (_showMapOverlay)
            {
                DrawFullMapOverlay();
            }

            if (_isGameOver)
            {
                DrawGameOverMenu();
            }
            else
            {
                if (_isPaused)
                {
                    DrawPauseMenu();
                }

                DrawHeroHealthUi();
                if (!_isPaused)
                {
                    DrawHeroDamageEdges();
                }
            }

            DrawGameplayCursor();
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

        private void DrawGameOverMenu()
        {
            if (_debugFont == null || _uiPixel == null)
            {
                return;
            }

            var vp = GraphicsDevice.Viewport;
            var overlay = Color.Black * 0.65f;
            var panelBg = new Color(22, 33, 62);
            var panelBorder = new Color(233, 69, 96);
            var accent = new Color(233, 69, 96);
            var titleColor = new Color(238, 238, 238);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            try
            {
                _spriteBatch.Draw(_uiPixel, new Rectangle(0, 0, vp.Width, vp.Height), overlay);

                const int panelW = 520;
                const int panelH = 220;
                var panel = new Rectangle(
                    (vp.Width - panelW) / 2,
                    (vp.Height - panelH) / 2,
                    panelW,
                    panelH);

                DrawPausePanelFrame(panel, panelBg, panelBorder);

                DrawPauseTitle("ИГРА ОКОНЧЕНА", new Vector2(panel.X + 28, panel.Y + 28), accent, titleColor);

                var mouse = Mouse.GetState();
                const int btnW = 320;
                const int btnH = 44;
                const int gap = 10;
                var btnLeft = panel.X + (panel.Width - btnW) / 2;
                var btnTop = panel.Y + 120;

                var labels = new[] { "Играть снова", "Выход" };

                for (var i = 0; i < labels.Length; i++)
                {
                    var r = new Rectangle(btnLeft, btnTop + i * (btnH + gap), btnW, btnH);
                    var selected = _gameOverSelectedIndex == i;
                    var hover = r.Contains(mouse.X, mouse.Y);
                    var fill = hover || selected ? new Color(26, 76, 130) : new Color(15, 52, 96);
                    DrawPauseButton(r, fill, labels[i], false);
                }
            }
            finally
            {
                _spriteBatch.End();
            }
        }

        private void UpdateGameOverMenu(MouseState mouse)
        {
            const int btnW = 320;
            const int btnH = 44;
            const int gap = 10;
            var vp = GraphicsDevice.Viewport;

            const int panelW = 520;
            const int panelH = 220;
            var panelX = (vp.Width - panelW) / 2;
            var panelY = (vp.Height - panelH) / 2;

            var btnLeft = panelX + (panelW - btnW) / 2;
            var btnTop = panelY + 120;

            var labels = new[] { "Играть снова", "Выход" };

            for (var i = 0; i < labels.Length; i++)
            {
                var r = new Rectangle(btnLeft, btnTop + i * (btnH + gap), btnW, btnH);
                var hover = r.Contains(mouse.X, mouse.Y);

                if (hover)
                {
                    _gameOverSelectedIndex = i;
                }

                var click = mouse.LeftButton == ButtonState.Released &&
                             _prevMouseState.LeftButton == ButtonState.Pressed &&
                             hover;
                if (click || (InputHandler.IsKeyPressed(Keys.Enter) && _gameOverSelectedIndex == i))
                {
                    if (i == 0)
                    {
                        ResetGame();
                        _isGameOver = false;
                        return;
                    }

                    RequestVoluntaryExit();
                    return;
                }
            }

            if (InputHandler.IsKeyPressed(Keys.Down))
            {
                _gameOverSelectedIndex = (_gameOverSelectedIndex + 1) % labels.Length;
            }
            else if (InputHandler.IsKeyPressed(Keys.Up))
            {
                _gameOverSelectedIndex = (_gameOverSelectedIndex - 1 + labels.Length) % labels.Length;
            }
        }

        private void ResetGame()
        {
            _heroHealth = HeroMaxHealth;
            _heroActionLocked = false;
            _pendingEnemyCounter = null;
            _pendingEnemyCounterDelayRemaining = 0f;
            _pendingEnemyActionPhase = false;
            _pendingEnemyActionAllowMovement = false;
            _heroStepsSinceEnemyTurn = 0;
            _heroTurnsSinceRegen = 0;
            _heroOverHeadHpBarTimer = 0f;
            _edgeFlashStrength = 0f;
            _heroBumpAnimTimer = 0f;
            _heroBumpAnimDirection = Vector2.Zero;
            _enemyBumpAnimTimers.Clear();
            _enemyBumpAnimDirections.Clear();
            _damagePopups.Clear();
            _combatLogEntries.Clear();
            _stairTransitionLock = false;
            _justResolvedHeroBump = false;
            _gameOverSelectedIndex = 0;
            _isPaused = false;
            _pauseScreen = PauseScreen.Main;
            IsMouseVisible = false;

            _generatedFloors.Clear();
            _currentFloorIndex = 0;
            _map = GetOrCreateFloor(_currentFloorIndex);

            _player.SetMapAndPosition(_map, _map.PlayerSpawnPoint);
            _map.UpdateExploration(_player.Position);

            SpawnEnemiesForCurrentMap();
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
                "  T - раскрыть историю событий\n" +
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

        private void SpawnEnemiesForCurrentMap()
        {
            _enemies.Clear();
            _enemyPositions.Clear();
            _enemyBumpAnimTimers.Clear();
            _enemyBumpAnimDirections.Clear();
            _damagePopups.Clear();
            _heroStepsSinceEnemyTurn = 0;
            _heroTurnsSinceRegen = 0;

            if (_map == null || _player == null)
            {
                return;
            }

            var rooms = _map.Rooms;
            var startingRoomIndex = _map.StartingRoomIndex;

            var reserved = new HashSet<Point>
            {
                _map.PlayerSpawnPoint,
                _map.StairUpPoint,
                _map.StairDownPoint
            };

            for (var roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
            {
                if (roomIndex == startingRoomIndex)
                {
                    continue;
                }

                var room = rooms[roomIndex];
                var toSpawn = RollEnemyCount();
                if (toSpawn <= 0)
                {
                    continue;
                }

                for (var i = 0; i < toSpawn; i++)
                {
                    const int maxAttempts = 60;
                    var spawned = false;

                    for (var attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        var xMin = room.Left + 1;
                        var xMaxExclusive = room.Right - 1;
                        var yMin = room.Top + 1;
                        var yMaxExclusive = room.Bottom - 1;

                        if (xMaxExclusive <= xMin || yMaxExclusive <= yMin)
                        {
                            break;
                        }

                        var x = _random.Next(xMin, xMaxExclusive);
                        var y = _random.Next(yMin, yMaxExclusive);
                        var p = new Point(x, y);

                        if (reserved.Contains(p) || !IsWalkableCell(p) || _enemyPositions.Contains(p))
                        {
                            continue;
                        }

                        if (IsEnemyTooClose(p, 1))
                        {
                            continue;
                        }

                        _enemies.Add(new Enemy(p, EnemyMaxHealth, EnemyDefense, TileSize));
                        _enemyPositions.Add(p);
                        spawned = true;
                        break;
                    }

                    if (!spawned)
                    {
                        break;
                    }
                }
            }
        }

        private int RollEnemyCount()
        {
            var r = _random.NextDouble();
            if (r < 0.35) return 0;
            if (r < 0.62) return 1;
            if (r < 0.80) return 2;
            if (r < 0.92) return 3;
            if (r < 0.985) return 4;
            return 5;
        }

        private bool IsEnemyAt(Point p) => _enemyPositions.Contains(p);

        private bool IsEnemyTooClose(Point p, int minSeparation)
        {
            for (var i = 0; i < _enemies.Count; i++)
            {
                var e = _enemies[i];
                if (e.IsDead)
                {
                    continue;
                }

                var dx = Math.Abs(e.Position.X - p.X);
                var dy = Math.Abs(e.Position.Y - p.Y);
                if (dx <= minSeparation && dy <= minSeparation)
                {
                    return true;
                }
            }

            return false;
        }

        private Enemy FindEnemyAt(Point p)
        {
            for (var i = 0; i < _enemies.Count; i++)
            {
                if (_enemies[i].Position == p)
                {
                    return _enemies[i];
                }
            }

            return null;
        }

        // Проверяем видимость по клеткам: стена между врагом и героем блокирует обзор.
        private bool HasLineOfSight(Point from, Point to)
        {
            if (from == to)
            {
                return true;
            }

            var x0 = from.X;
            var y0 = from.Y;
            var x1 = to.X;
            var y1 = to.Y;

            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                if (x0 == x1 && y0 == y1)
                {
                    return true;
                }

                var e2 = 2 * err;
                if (e2 > -dy)
                {
                    err -= dy;
                    x0 += sx;
                }

                if (e2 < dx)
                {
                    err += dx;
                    y0 += sy;
                }

                if (x0 == x1 && y0 == y1)
                {
                    return true;
                }

                var p = new Point(x0, y0);
                if (!IsWalkableCell(p))
                {
                    return false;
                }
            }
        }

        private void HandleHeroBumpIntoEnemy(Point heroPos, Point enemyPos)
        {
            if (_heroActionLocked)
            {
                return;
            }

            _justResolvedHeroBump = true;

            if (_heroHealth <= 0)
            {
                return;
            }

            var enemy = FindEnemyAt(enemyPos);
            if (enemy == null)
            {
                return;
            }

            AdvanceHeroTurnAndRegen();
            _heroStepsSinceEnemyTurn++;

            var bumpDirX = Math.Sign(enemyPos.X - heroPos.X);
            var bumpDirY = Math.Sign(enemyPos.Y - heroPos.Y);
            var bumpDir = new Vector2(bumpDirX, bumpDirY);
            if (bumpDir != Vector2.Zero)
            {
                bumpDir.Normalize();
                StartHeroBumpAnimation(bumpDir);
                StartEnemyBumpAnimation(enemy, -bumpDir);
            }

            var allowMovement = _heroStepsSinceEnemyTurn >= 2;
            if (allowMovement)
            {
                _heroStepsSinceEnemyTurn = 0;
            }

            ResolveDelayedHeroStrike(enemy);
            if (enemy.IsDead)
            {
                return;
            }

            _heroActionLocked = true;
            _pendingEnemyCounter = enemy;
            _pendingEnemyCounterDelayRemaining = EnemyActionDelaySeconds;
            _pendingEnemyActionPhase = true;
            _pendingEnemyActionAllowMovement = allowMovement;
        }

        private bool IsWalkableCell(Point p) => _map.IsWalkable(p.X, p.Y);

        private bool CanMoveEnemyTo(Point p)
        {
            if (!IsWalkableCell(p))
            {
                return false;
            }

            if (p == _player.Position)
            {
                return false;
            }

            return !_enemyPositions.Contains(p);
        }

        private void EnemyAttackHero(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead || _heroHealth <= 0)
            {
                return;
            }

            var heroPos = _player.Position;
            var enemyPos = enemy.Position;
            var dist = Math.Abs(heroPos.X - enemyPos.X) + Math.Abs(heroPos.Y - enemyPos.Y);
            if (dist != 1)
            {
                return;
            }

            var enemyToHeroDir = new Vector2(heroPos.X - enemyPos.X, heroPos.Y - enemyPos.Y);
            if (enemyToHeroDir != Vector2.Zero)
            {
                enemyToHeroDir.Normalize();
                StartEnemyBumpAnimation(enemy, enemyToHeroDir);
                StartHeroBumpAnimation(-enemyToHeroDir);
            }

            if (_random.NextDouble() < EnemyMissChance)
            {
                AddCombatLog("Монстр промахивается", new Color(200, 200, 200));
                return;
            }

            if (_random.NextDouble() < GetBlockChance(HeroDefense))
            {
                AddCombatLog("Вы блокируете удар", new Color(170, 220, 255));
                return;
            }

            _edgeFlashStrength = 1f;
            _heroOverHeadHpBarTimer = HeroOverHeadHpBarDurationSeconds;
            var enemyCrit = _random.NextDouble() < EnemyCritChance;
            var enemyDamage = enemyCrit ? EnemyCritDamage : _random.Next(EnemyDamageMin, EnemyDamageMax + 1);
            enemyDamage = ApplyDefenseReduction(enemyDamage, HeroDefense);
            _heroHealth = Math.Max(0, _heroHealth - enemyDamage);
            AddDamagePopup(_player.WorldPosition, enemyDamage, enemyCrit);
            AddCombatLog(enemyCrit ? $"Крит по вам: {enemyDamage} ед. урона" : $"Вы получили {enemyDamage} ед. урона", new Color(255, 150, 150));
            if (_heroHealth <= 0)
            {
                _isGameOver = true;
                IsMouseVisible = true;
                AddCombatLog("Вы погибли", Color.OrangeRed);
                return;
            }
        }

        private void ResolveDelayedHeroStrike(Enemy enemy)
        {
            if (enemy == null || enemy.IsDead || _heroHealth <= 0)
            {
                return;
            }

            var heroPos = _player.Position;
            var enemyPos = enemy.Position;
            var dist = Math.Abs(heroPos.X - enemyPos.X) + Math.Abs(heroPos.Y - enemyPos.Y);
            if (dist != 1)
            {
                return;
            }

            var heroCrit = _random.NextDouble() < HeroCritChance;
            var heroDamage = heroCrit ? HeroCritDamage : _random.Next(HeroDamageMin, HeroDamageMax + 1);
            if (_random.NextDouble() < HeroMissChance)
            {
                AddCombatLog("Вы промахнулись", new Color(200, 200, 200));
                return;
            }

            if (_random.NextDouble() < GetBlockChance(enemy.Defense))
            {
                AddCombatLog("Монстр блокирует удар", new Color(170, 220, 255));
                return;
            }

            heroDamage = ApplyDefenseReduction(heroDamage, enemy.Defense);

            enemy.TakeDamage(heroDamage);
            AddDamagePopup(enemy.WorldPosition, heroDamage, heroCrit);
            AddCombatLog(heroCrit ? $"Крит! Вы нанесли {heroDamage} ед. урона" : $"Вы нанесли {heroDamage} ед. урона", new Color(255, 210, 120));

            if (enemy.IsDead)
            {
                _enemies.Remove(enemy);
                _enemyPositions.Remove(enemyPos);
                _enemyBumpAnimTimers.Remove(enemy);
                _enemyBumpAnimDirections.Remove(enemy);
                AddCombatLog("Монстр убит", new Color(255, 130, 130));
            }
        }

        private void UpdatePendingEnemyCounter(float dt)
        {
            _pendingEnemyCounterDelayRemaining = Math.Max(0f, _pendingEnemyCounterDelayRemaining - dt);
            if (_pendingEnemyCounterDelayRemaining > 0f)
            {
                return;
            }

            if (_pendingEnemyActionPhase)
            {
                _pendingEnemyActionPhase = false;
                if (!_isGameOver)
                {
                    ProcessEnemyTurn(_pendingEnemyActionAllowMovement);
                    HandleStairTransitions();
                }

                _pendingEnemyCounter = null;
                _heroActionLocked = false;
                return;
            }

            _pendingEnemyCounter = null;
            _heroActionLocked = false;
        }

        private void ProcessEnemyTurn(bool allowMovement)
        {
            if (_heroHealth <= 0)
            {
                return;
            }

            for (var i = 0; i < _enemies.Count; i++)
            {
                var enemy = _enemies[i];
                if (enemy.IsDead)
                {
                    continue;
                }

                var heroPos = _player.Position;
                var enemyPos = enemy.Position;
                var dist = Math.Abs(heroPos.X - enemyPos.X) + Math.Abs(heroPos.Y - enemyPos.Y);

                if (dist == 1)
                {
                    EnemyAttackHero(enemy);
                    if (_isGameOver)
                    {
                        return;
                    }
                    continue;
                }

                if (!HasLineOfSight(enemyPos, heroPos))
                {
                    continue;
                }

                if (!allowMovement)
                {
                    continue;
                }

                var next = ChooseEnemyMove(enemyPos, heroPos);
                if (next.HasValue && next.Value != enemyPos)
                {
                    _enemyPositions.Remove(enemyPos);
                    enemy.MoveTo(next.Value);
                    _enemyPositions.Add(next.Value);
                }

                if (_isGameOver)
                {
                    return;
                }
            }
        }

        private Point? ChooseEnemyMove(Point enemyPos, Point heroPos)
        {
            var bestDist = int.MaxValue;
            var candidates = new List<Point>();

            TryDir(1, 0);
            TryDir(-1, 0);
            TryDir(0, 1);
            TryDir(0, -1);

            if (candidates.Count == 0)
            {
                return null;
            }

            return candidates[_random.Next(candidates.Count)];

            void TryDir(int dx, int dy)
            {
                var p = new Point(enemyPos.X + dx, enemyPos.Y + dy);
                if (p == heroPos)
                {
                    return;
                }

                if (!IsWalkableCell(p) || _enemyPositions.Contains(p))
                {
                    return;
                }

                var d = Math.Abs(p.X - heroPos.X) + Math.Abs(p.Y - heroPos.Y);
                if (d < bestDist)
                {
                    bestDist = d;
                    candidates.Clear();
                    candidates.Add(p);
                }
                else if (d == bestDist)
                {
                    candidates.Add(p);
                }
            }
        }

        private void DrawEnemies()
        {
            if (_uiPixel == null)
            {
                return;
            }

            foreach (var enemy in _enemies)
            {
                if (enemy.IsDead)
                {
                    continue;
                }

                enemy.Draw(_spriteBatch, _uiPixel, Color.IndianRed, Color.OrangeRed, GetEnemyBumpDrawOffset(enemy));
            }
        }

        private void UpdateEnemies(GameTime gameTime)
        {
            var dt = (float)gameTime.ElapsedGameTime.TotalSeconds;
            var keysToClear = new List<Enemy>();
            foreach (var kv in _enemyBumpAnimTimers)
            {
                var newTimer = Math.Max(0f, kv.Value - dt);
                if (newTimer <= 0f || kv.Key == null || kv.Key.IsDead)
                {
                    keysToClear.Add(kv.Key);
                }
                else
                {
                    _enemyBumpAnimTimers[kv.Key] = newTimer;
                }
            }

            foreach (var k in keysToClear)
            {
                _enemyBumpAnimTimers.Remove(k);
                _enemyBumpAnimDirections.Remove(k);
            }

            foreach (var enemy in _enemies)
            {
                if (enemy.IsDead)
                {
                    continue;
                }

                enemy.Update(gameTime);
            }
        }

        private void StartHeroBumpAnimation(Vector2 direction)
        {
            if (direction == Vector2.Zero)
            {
                return;
            }

            direction.Normalize();
            _heroBumpAnimDirection = direction;
            _heroBumpAnimTimer = BumpAnimDurationSeconds;
        }

        private void StartEnemyBumpAnimation(Enemy enemy, Vector2 direction)
        {
            if (enemy == null || direction == Vector2.Zero)
            {
                return;
            }

            direction.Normalize();
            _enemyBumpAnimDirections[enemy] = direction;
            _enemyBumpAnimTimers[enemy] = BumpAnimDurationSeconds;
        }

        private Vector2 GetHeroBumpDrawOffset()
        {
            if (_heroBumpAnimTimer <= 0f || _heroBumpAnimDirection == Vector2.Zero)
            {
                return Vector2.Zero;
            }

            var progress = 1f - (_heroBumpAnimTimer / BumpAnimDurationSeconds);
            var amp = MathF.Sin(progress * MathF.PI) * BumpAnimAmplitudePx;
            return _heroBumpAnimDirection * amp;
        }

        private Vector2 GetEnemyBumpDrawOffset(Enemy enemy)
        {
            if (enemy == null)
            {
                return Vector2.Zero;
            }

            if (!_enemyBumpAnimTimers.TryGetValue(enemy, out var timer) ||
                !_enemyBumpAnimDirections.TryGetValue(enemy, out var dir) ||
                timer <= 0f || dir == Vector2.Zero)
            {
                return Vector2.Zero;
            }

            var progress = 1f - (timer / BumpAnimDurationSeconds);
            var amp = MathF.Sin(progress * MathF.PI) * BumpAnimAmplitudePx;
            return dir * amp;
        }

        private void AddDamagePopup(Vector2 targetWorldPosition, int damage, bool isCrit)
        {
            _damagePopups.Add(new DamagePopup
            {
                WorldPosition = targetWorldPosition + new Vector2(_map.TileSize * 0.35f, -4f),
                Value = damage,
                IsCrit = isCrit,
                Lifetime = 0.70f,
                TimeLeft = 0.70f
            });
        }

        private void AddCombatLog(string text, Color color)
        {
            _combatLogHistory.Add(new CombatLogHistoryItem { Text = text, Color = color });
            if (_combatLogHistory.Count > 400)
            {
                _combatLogHistory.RemoveRange(0, 100);
            }

            var baseY = GetCombatLogBaseY();
            for (var i = 0; i < _combatLogEntries.Count; i++)
            {
                var e = _combatLogEntries[i];
                if (e.IsExiting)
                {
                    continue;
                }

                e.TargetY -= CombatLogLineHeight;
                if (e.TargetY < baseY - (CombatLogVisibleCount - 1) * CombatLogLineHeight - 0.5f)
                {
                    e.IsExiting = true;
                    e.TargetY = baseY - CombatLogVisibleCount * CombatLogLineHeight;
                }
            }

            _combatLogEntries.Add(new CombatLogEntry
            {
                Text = text,
                Color = color,
                Y = baseY + 10f,
                TargetY = baseY,
                Alpha = 0f,
                IsExiting = false
            });

            ClampCombatLogScroll();
            _combatLogScrollOffset = Math.Max(0, _combatLogHistory.Count - CombatLogExpandedVisibleCount);
        }

        private void UpdateCombatLog(float dt)
        {
            for (var i = _combatLogEntries.Count - 1; i >= 0; i--)
            {
                var e = _combatLogEntries[i];
                e.Y = MathHelper.Lerp(e.Y, e.TargetY, Math.Clamp(dt * CombatLogMoveSpeed, 0f, 1f));

                if (e.IsExiting)
                {
                    e.Alpha = Math.Max(0f, e.Alpha - dt * CombatLogFadeSpeed);
                    if (e.Alpha <= 0.001f)
                    {
                        _combatLogEntries.RemoveAt(i);
                    }
                }
                else
                {
                    e.Alpha = Math.Min(1f, e.Alpha + dt * 6f);
                }
            }
        }

        private float GetCombatLogBaseY()
        {
            var diameter = 230f;
            var padding = 16f;
            return GraphicsDevice.Viewport.Height - diameter - padding - 42f;
        }

        private void DrawCombatLog()
        {
            if (_debugFont == null)
            {
                return;
            }

            if (_isCombatLogExpanded)
            {
                DrawExpandedCombatLog();
                return;
            }

            if (_combatLogEntries.Count == 0)
            {
                return;
            }

            const float x = 16f;
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            for (var i = 0; i < _combatLogEntries.Count; i++)
            {
                var e = _combatLogEntries[i];
                if (e.Alpha <= 0.001f)
                {
                    continue;
                }

                var pos = new Vector2(x, e.Y);
                var c = e.Color * e.Alpha;
                _spriteBatch.DrawString(_debugFont, e.Text, pos + Vector2.One, Color.Black * (0.45f * e.Alpha), 0f, Vector2.Zero, CombatLogTextScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_debugFont, e.Text, pos, c, 0f, Vector2.Zero, CombatLogTextScale, SpriteEffects.None, 0f);
            }
            _spriteBatch.End();
        }

        private void UpdateCombatLogExpandedInput(MouseState mouse)
        {
            var moved = mouse.Position != _lastMousePosition;
            if (_isCombatLogExpanded)
            {
                var wheelDelta = mouse.ScrollWheelValue - _lastScrollWheelValue;
                if (wheelDelta != 0)
                {
                    _combatLogScrollOffset -= Math.Sign(wheelDelta);
                    ClampCombatLogScroll();
                }

                var panel = GetExpandedCombatLogPanel();
                var track = GetCombatLogScrollTrackRect(panel);
                var thumb = GetCombatLogScrollbarThumbRect(panel, _combatLogHistory.Count, CombatLogExpandedVisibleCount);

                var leftPressedNow = mouse.LeftButton == ButtonState.Pressed;
                var leftPressedBefore = _prevMouseState.LeftButton == ButtonState.Pressed;
                var leftPressedEdge = leftPressedNow && !leftPressedBefore;

                if (leftPressedEdge && thumb.Contains(mouse.X, mouse.Y))
                {
                    _combatLogScrollbarDragging = true;
                    _combatLogScrollbarGrabOffset = mouse.Y - thumb.Y;
                }
                else if (!leftPressedNow)
                {
                    _combatLogScrollbarDragging = false;
                }

                if (_combatLogScrollbarDragging && leftPressedNow)
                {
                    var maxOffset = Math.Max(0, _combatLogHistory.Count - CombatLogExpandedVisibleCount);
                    if (maxOffset > 0)
                    {
                        var thumbH = thumb.Height;
                        var minY = track.Y;
                        var maxY = track.Bottom - thumbH;
                        var newY = (int)Math.Clamp(mouse.Y - _combatLogScrollbarGrabOffset, minY, maxY);
                        var t = (newY - minY) / (float)Math.Max(1, maxY - minY);
                        _combatLogScrollOffset = (int)Math.Round(t * maxOffset);
                        ClampCombatLogScroll();
                    }
                }
            }
            else
            {
                _combatLogScrollbarDragging = false;
            }

            if (moved)
            {
                _lastMousePosition = mouse.Position;
            }

            _lastScrollWheelValue = mouse.ScrollWheelValue;
        }

        private void UpdateGameplayCursor(MouseState mouse, float dt)
        {
            var moved = mouse.Position != _lastMousePosition;
            if (moved)
            {
                _mouseIdleTime = 0f;
                _gameCursorAlpha = 1f;
                _lastMousePosition = mouse.Position;
            }
            else
            {
                _mouseIdleTime += dt;
                if (_mouseIdleTime > MouseCursorIdleBeforeFade)
                {
                    _gameCursorAlpha = Math.Max(0f, _gameCursorAlpha - dt * MouseCursorFadeSpeed);
                }
            }

            IsMouseVisible = false;
        }

        private void DrawGameplayCursor()
        {
            if (_uiPixel == null || _gameCursorAlpha <= 0.001f || _isPaused || _isGameOver)
            {
                return;
            }

            var p = _lastMousePosition;
            var c = Color.White * _gameCursorAlpha;
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_uiPixel, new Rectangle(p.X - 1, p.Y - 6, 2, 12), c);
            _spriteBatch.Draw(_uiPixel, new Rectangle(p.X - 6, p.Y - 1, 12, 2), c);
            _spriteBatch.End();
        }

        private void DrawExpandedCombatLog()
        {
            var panel = GetExpandedCombatLogPanel();
            var content = GetExpandedCombatLogContentRect(panel);
            var visibleCount = CombatLogExpandedVisibleCount;
            var total = _combatLogHistory.Count;
            var maxOffset = Math.Max(0, total - visibleCount);
            var start = Math.Clamp(_combatLogScrollOffset, 0, maxOffset);
            var endExclusive = Math.Min(total, start + visibleCount);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_uiPixel, panel, Color.Black * 0.5f);
            _spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Y, panel.Width, 2), Color.White * 0.2f);
            _spriteBatch.Draw(_uiPixel, new Rectangle(panel.X, panel.Bottom - 2, panel.Width, 2), Color.White * 0.2f);

            for (var i = start; i < endExclusive; i++)
            {
                var row = i - start;
                var y = content.Y + row * CombatLogLineHeight;
                var item = _combatLogHistory[i];
                var pos = new Vector2(content.X, y);
                _spriteBatch.DrawString(_debugFont, item.Text, pos + Vector2.One, Color.Black * 0.45f, 0f, Vector2.Zero, CombatLogTextScale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_debugFont, item.Text, pos, item.Color, 0f, Vector2.Zero, CombatLogTextScale, SpriteEffects.None, 0f);
            }

            DrawCombatLogScrollbar(panel, total, visibleCount);
            _spriteBatch.End();
        }

        private void DrawCombatLogScrollbar(Rectangle panel, int total, int visibleCount)
        {
            var track = GetCombatLogScrollTrackRect(panel);
            _spriteBatch.Draw(_uiPixel, track, Color.Black * 0.45f);
            if (total <= 0)
            {
                return;
            }

            var thumb = GetCombatLogScrollbarThumbRect(panel, total, visibleCount);
            _spriteBatch.Draw(_uiPixel, thumb, Color.White * 0.55f);
        }

        private Rectangle GetExpandedCombatLogPanel()
        {
            var x = 8;
            var y = (int)(GetCombatLogBaseY() - 180f);
            var h = (int)(CombatLogExpandedVisibleCount * CombatLogLineHeight + 16f);
            var w = 500;
            return new Rectangle(x, y, w, h);
        }

        private Rectangle GetExpandedCombatLogContentRect(Rectangle panel)
        {
            return new Rectangle(panel.X + 8, panel.Y + 8, panel.Width - 30, panel.Height - 16);
        }

        private Rectangle GetCombatLogScrollTrackRect(Rectangle panel)
        {
            return new Rectangle(panel.Right - 14, panel.Y + 8, 8, panel.Height - 16);
        }

        private Rectangle GetCombatLogScrollbarThumbRect(Rectangle panel, int total, int visibleCount)
        {
            var track = GetCombatLogScrollTrackRect(panel);
            if (total <= 0)
            {
                return track;
            }

            var maxOffset = Math.Max(0, total - visibleCount);
            var thumbH = Math.Max(14, (int)Math.Round(track.Height * (visibleCount / (float)Math.Max(visibleCount, total))));
            if (maxOffset == 0)
            {
                return new Rectangle(track.X, track.Y, track.Width, thumbH);
            }

            var t = _combatLogScrollOffset / (float)maxOffset;
            var y = track.Y + (int)Math.Round((track.Height - thumbH) * t);
            return new Rectangle(track.X, y, track.Width, thumbH);
        }

        private void ClampCombatLogScroll()
        {
            var maxOffset = Math.Max(0, _combatLogHistory.Count - CombatLogExpandedVisibleCount);
            _combatLogScrollOffset = Math.Clamp(_combatLogScrollOffset, 0, maxOffset);
        }

        private static float GetBlockChance(int defense)
        {
            var clamped = Math.Clamp(defense, 0, 100);
            return clamped / 100f;
        }

        private static int ApplyDefenseReduction(int rawDamage, int defense)
        {
            if (rawDamage <= 0)
            {
                return 0;
            }

            var reduction = (int)Math.Ceiling(rawDamage * (Math.Clamp(defense, 0, 100) / 100f));
            var reduced = rawDamage - reduction;
            return Math.Max(1, reduced);
        }

        private void AdvanceHeroTurnAndRegen()
        {
            _heroTurnsSinceRegen++;
            if (_heroTurnsSinceRegen < 3)
            {
                return;
            }

            _heroTurnsSinceRegen = 0;
            if (_heroHealth > 0 && _heroHealth < HeroMaxHealth)
            {
                _heroHealth = Math.Min(HeroMaxHealth, _heroHealth + 1);
            }
        }

        private void UpdateDamagePopups(float dt)
        {
            for (var i = _damagePopups.Count - 1; i >= 0; i--)
            {
                var p = _damagePopups[i];
                p.TimeLeft -= dt;
                p.WorldPosition.Y -= (p.IsCrit ? 34f : 26f) * dt;

                if (p.TimeLeft <= 0f)
                {
                    _damagePopups.RemoveAt(i);
                    continue;
                }

                _damagePopups[i] = p;
            }
        }

        private void DrawDamagePopups()
        {
            if (_debugFont == null || _damagePopups.Count == 0)
            {
                return;
            }

            for (var i = 0; i < _damagePopups.Count; i++)
            {
                var p = _damagePopups[i];
                var alpha = MathHelper.Clamp(p.TimeLeft / p.Lifetime, 0f, 1f);
                var text = p.Value.ToString();
                var color = p.IsCrit ? Color.Gold : new Color(255, 110, 110);
                var scale = p.IsCrit ? 1.05f : 0.95f;
                var pos = p.WorldPosition;

                _spriteBatch.DrawString(_debugFont, text, pos + Vector2.One, Color.Black * (0.45f * alpha), 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
                _spriteBatch.DrawString(_debugFont, text, pos, color * alpha, 0f, Vector2.Zero, scale, SpriteEffects.None, 0f);
            }
        }

        private void DrawHeroHealthUi()
        {
            if (_uiPixel == null || _debugFont == null)
            {
                return;
            }

            var vp = GraphicsDevice.Viewport;
            const int barH = 16;
            const int padding = 12;
            const int barW = 320;

            var hpText = _heroHealth.ToString();
            var textSize = _debugFont.MeasureString(hpText);
            var y = vp.Height - barH - padding;
            var barX = (vp.Width - barW) / 2;
            var textX = barX - (int)textSize.X - 12;
            if (textX < 0) textX = 0;
            var textPos = new Vector2(textX, y - (textSize.Y - barH) * 0.5f);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            _spriteBatch.DrawString(_debugFont, hpText, textPos + Vector2.One, Color.Black * 0.35f);
            _spriteBatch.DrawString(_debugFont, hpText, textPos, Color.White);

            var ratio = HeroMaxHealth <= 0 ? 0f : (float)_heroHealth / HeroMaxHealth;
            ratio = MathHelper.Clamp(ratio, 0f, 1f);
            var fillW = (int)(barW * ratio);

            _spriteBatch.Draw(_uiPixel, new Rectangle(barX, y, barW, barH), Color.Black * 0.5f);
            if (fillW > 0)
            {
                _spriteBatch.Draw(_uiPixel, new Rectangle(barX, y, fillW, barH), Color.Red);
            }

            _spriteBatch.End();
        }

        private void DrawHeroDamageEdges()
        {
            if (_uiPixel == null)
            {
                return;
            }

            var intensity = Math.Max(_edgeFlashStrength, _heroHealth < 30 ? EdgeCriticalStrength : 0f);
            if (intensity <= 0.001f)
            {
                return;
            }

            var vp = GraphicsDevice.Viewport;
            const int thickness = 26;
            var alpha = 0.12f + intensity * 0.7f;
            var c = Color.Red * alpha;

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_uiPixel, new Rectangle(0, 0, vp.Width, thickness), c);
            _spriteBatch.Draw(_uiPixel, new Rectangle(0, vp.Height - thickness, vp.Width, thickness), c);
            _spriteBatch.Draw(_uiPixel, new Rectangle(0, 0, thickness, vp.Height), c);
            _spriteBatch.Draw(_uiPixel, new Rectangle(vp.Width - thickness, 0, thickness, vp.Height), c);
            _spriteBatch.End();
        }

        private void DrawHeroOverHeadHpBar()
        {
            if (_uiPixel == null || _map == null)
            {
                return;
            }

            if (_heroOverHeadHpBarTimer <= 0f || _heroHealth <= 0)
            {
                return;
            }

            var ratio = HeroMaxHealth <= 0 ? 0f : (float)_heroHealth / HeroMaxHealth;
            ratio = MathHelper.Clamp(ratio, 0f, 1f);

            var alpha = _heroOverHeadHpBarTimer / HeroOverHeadHpBarDurationSeconds;
            alpha = MathHelper.Clamp(alpha, 0f, 1f);

            var barH = 4;
            var tileSize = _map.TileSize;
            var barW = tileSize;

            var heroRect = new Rectangle(
                (int)_player.WorldPosition.X,
                (int)_player.WorldPosition.Y,
                tileSize,
                tileSize);

            var topY = heroRect.Y - barH - 2;
            var leftX = heroRect.X;
            var fillW = (int)(barW * ratio);

            var bgAlpha = 0.45f * alpha;
            var fillAlpha = 0.95f * alpha;

            _spriteBatch.Draw(_uiPixel, new Rectangle(leftX, topY, barW, barH), Color.Black * bgAlpha);
            if (fillW > 0)
            {
                _spriteBatch.Draw(_uiPixel, new Rectangle(leftX, topY, fillW, barH), Color.Red * fillAlpha);
            }
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
            SpawnEnemiesForCurrentMap();
            var floorNumber = _currentFloorIndex + 1;
            AddCombatLog(
                comingFromAbove ? $"Спуск на этаж {floorNumber}" : $"Подъем на этаж {floorNumber}",
                new Color(170, 205, 255));
        }
    }
}
