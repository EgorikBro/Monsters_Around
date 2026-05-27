using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monsters_Around.Controllers;
using Monsters_Around.Models;
using Monsters_Around.Views;
using System;

namespace Monsters_Around
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private Texture2D _tilesetTexture;
        private SpriteFont _debugFont;
        private Texture2D _uiPixel;

        private Map _map;
        private Player _player;
        private Camera2D _camera;
        private readonly Random _random = new();
        private MouseState _prevMouseState;
        private int _lastScrollWheelValue;
        private bool _gamePadBackHeld;
        private int _windowedWidth = GameConstants.WindowWidth;
        private int _windowedHeight = GameConstants.WindowHeight;
        private bool _isApplyingDisplayChange;

        private GameState _state;
        private CombatLog _log;
        private CombatController _combat;
        private EnemyController _enemies;
        private FloorController _floors;

        private HudView _hudView;
        private MinimapView _minimapView;
        private CombatLogView _combatLogView;
        private MenuView _menuView;
        private LevelUpView _levelUpView;
        private CharacterView _characterView;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = false;
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnClientSizeChanged;
            _graphics.HardwareModeSwitch = false;
            _graphics.IsFullScreen = false;
            _graphics.PreferredBackBufferWidth = GameConstants.WindowWidth;
            _graphics.PreferredBackBufferHeight = GameConstants.WindowHeight;
            Exiting += OnGameExiting;
        }

        protected override void Initialize()
        {
            _state   = new GameState();
            _log     = new CombatLog();
            _combat  = new CombatController(_random);
            _enemies = new EnemyController(_random);
            _floors  = new FloorController(_random);

            _map    = _floors.GetOrCreate(0, null);
            _player = new Player(_map.PlayerSpawnPoint, _map);
            _player.IsEnemyAt       = p => _enemies.IsEnemyAt(p);
            _player.OnBumpRequested = HandleHeroBumpIntoEnemy;
            _map.UpdateExploration(_player.Position);
            _enemies.SpawnEnemies(_map, _player, _state);
            _camera = new Camera2D(GameConstants.CameraZoom);

            base.Initialize();
            SetFullscreen(true);
            IsMouseVisible = false;
        }

        protected override void LoadContent()
        {
            _spriteBatch    = new SpriteBatch(GraphicsDevice);
            _tilesetTexture = Content.Load<Texture2D>("Dungeon tileset");
            _debugFont      = Content.Load<SpriteFont>("DebugFont");

            _uiPixel = new Texture2D(GraphicsDevice, 1, 1);
            _uiPixel.SetData(new[] { Color.White });

            var playerTex = new Texture2D(GraphicsDevice, 1, 1);
            playerTex.SetData(new[] { new Color(50, 120, 220) });

            _floors.LoadAllContent(_tilesetTexture);
            _player.LoadContent(playerTex);

            _hudView       = new HudView(_spriteBatch, _debugFont, _uiPixel, GraphicsDevice);
            _minimapView   = new MinimapView(_spriteBatch, _uiPixel, GraphicsDevice);
            _combatLogView = new CombatLogView(_spriteBatch, _debugFont, _uiPixel, GraphicsDevice);
            _menuView      = new MenuView(_spriteBatch, _debugFont, _uiPixel, GraphicsDevice);
            _levelUpView   = new LevelUpView(_spriteBatch, _debugFont, _uiPixel, GraphicsDevice);
            _characterView = new CharacterView(_spriteBatch, _debugFont, _uiPixel, GraphicsDevice);

            _prevMouseState = Mouse.GetState();
            _lastScrollWheelValue = _prevMouseState.ScrollWheelValue;
            _state.LastMousePosition = _prevMouseState.Position;
            _gamePadBackHeld = GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed;
        }

        protected override void Update(GameTime gameTime)
        {
            if (_state.VoluntaryExitRequested)
            {
                Exit();
                base.Update(gameTime);
                return;
            }

            InputHandler.Update();
            var mouse = Mouse.GetState();
            var dt    = (float)gameTime.ElapsedGameTime.TotalSeconds;

            var gamePad      = GamePad.GetState(PlayerIndex.One);
            var backNow      = gamePad.Buttons.Back == ButtonState.Pressed;
            var backEdge     = backNow && !_gamePadBackHeld;
            _gamePadBackHeld = backNow;

            if (InputHandler.IsKeyPressed(Keys.T))
            {
                _log.IsExpanded = !_log.IsExpanded;
                _log.ScrollbarDragging = false;
                _log.ClampScroll();
                if (_log.IsExpanded)
                    _log.ScrollOffset = Math.Max(0, _log.History.Count - GameConstants.CombatLogExpandedVisibleCount);
            }

            if (!_state.IsPaused && !_state.IsGameOver)
            {
                UpdateCursor(mouse, dt);
                _combatLogView.UpdateScrollInput(mouse, _prevMouseState, _lastScrollWheelValue,
                    _log, out _lastScrollWheelValue);
            }

            if (_state.IsVictory)
            {
                _menuView.UpdateVictory(_state, mouse, _prevMouseState);
                if (!_state.IsVictory) ResetGame();
                _prevMouseState = mouse;
                base.Update(gameTime);
                return;
            }

            if (_state.IsGameOver)
            {
                var wasOver = _state.IsGameOver;
                _menuView.UpdateGameOver(_state, mouse, _prevMouseState);
                if (wasOver && !_state.IsGameOver) ResetGame();
                _prevMouseState = mouse;
                base.Update(gameTime);
                return;
            }

            if (_state.IsPaused)
            {
                _menuView.UpdatePause(_state, mouse, _prevMouseState);
                _prevMouseState = mouse;
                base.Update(gameTime);
                return;
            }

            if (InputHandler.IsKeyPressed(Keys.Escape) || backEdge)
            {
                if (_state.IsCharacterScreenOpen)
                    CharacterView.CloseScreen(_state);
                else
                    OpenPauseMenu();
                _prevMouseState = mouse;
                base.Update(gameTime);
                return;
            }

            if (InputHandler.IsKeyPressed(Keys.Tab))
            {
                _state.IsCharacterScreenOpen = !_state.IsCharacterScreenOpen;
                if (!_state.IsCharacterScreenOpen) CharacterView.CloseScreen(_state);
                _prevMouseState = mouse;
                base.Update(gameTime);
                return;
            }

            if (_state.IsCharacterScreenOpen)
            {
                var overlayAlreadyOpen = _state.ShowLevelUpOverlay;
                _characterView.Update(_state, mouse, _prevMouseState);
                // Не вызываем LevelUpView в том же кадре, когда overlay только открылся —
                // иначе клик по кнопке экрана персонажа просочится в LevelUpView.
                if (_state.ShowLevelUpOverlay && overlayAlreadyOpen)
                    _levelUpView.Update(_state, mouse, _prevMouseState);
                _prevMouseState = mouse;
                base.Update(gameTime);
                return;
            }

            if (InputHandler.IsKeyPressed(Keys.F11)) ToggleFullscreen();
            if (InputHandler.IsKeyPressed(Keys.F1))  _state.ShowFps = !_state.ShowFps;
            if (InputHandler.IsKeyPressed(Keys.M))   _state.ShowMapOverlay = !_state.ShowMapOverlay;

            if (_state.EdgeFlashStrength > 0f)
                _state.EdgeFlashStrength = Math.Max(0f, _state.EdgeFlashStrength - dt / GameConstants.EdgeFlashDurationSeconds);
            if (_state.LevelUpEdgeFlashStrength > 0f)
                _state.LevelUpEdgeFlashStrength = Math.Max(0f, _state.LevelUpEdgeFlashStrength - dt / GameConstants.LevelUpEdgeFlashDuration);
            if (_state.PendingLevelUp)
                _state.LevelUpBlinkAccumulator += dt;
            if (_state.HeroOverHeadHpBarTimer > 0f)
                _state.HeroOverHeadHpBarTimer = Math.Max(0f, _state.HeroOverHeadHpBarTimer - dt);
            if (_state.HeroBumpAnimTimer > 0f)
                _state.HeroBumpAnimTimer = Math.Max(0f, _state.HeroBumpAnimTimer - dt);

            _log.UpdateDamagePopups(dt);
            _combatLogView.Update(dt, _log);
            UpdateFps(dt);

            if (_state.HeroActionLocked)
            {
                UpdatePendingEnemyCounter(dt);
            }
            else
            {
                var heroPosBefore = _player.Position;
                var mapBefore     = _map;
                _state.JustResolvedHeroBump = false;

                if (InputHandler.IsKeyPressed(Keys.Space) && !_player.IsMoving)
                {
                    _combat.AdvanceTurnAndRegen(_state);
                    _enemies.ProcessTurn(_map, _player, _state, _log, _combat);
                    TryStairTransition();
                }
                else
                {
                    _player.Update(gameTime);
                }

                if (!_state.HeroActionLocked) TryStairTransition();

                if (!_state.HeroActionLocked && _map == mapBefore &&
                    !_state.JustResolvedHeroBump && _player.Position != heroPosBefore)
                {
                    _combat.AdvanceTurnAndRegen(_state);
                    _enemies.ProcessTurn(_map, _player, _state, _log, _combat);
                    TryStairTransition();
                }
            }

            _enemies.UpdateAnimations(dt, _state);
            _enemies.UpdateMovement(gameTime);
            _map.UpdateExploration(_player.Position);

            _camera.Follow(
                _player.WorldPosition + new Vector2(GameConstants.TileSize * 0.5f, GameConstants.TileSize * 0.5f),
                GraphicsDevice.Viewport,
                new Point(GameConstants.MapWidth * GameConstants.TileSize, GameConstants.MapHeight * GameConstants.TileSize));

            _prevMouseState = mouse;
            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp, transformMatrix: _camera.GetViewMatrix());
            _map.Draw(_spriteBatch);
            DrawEnemies();
            _player.Draw(_spriteBatch, GetHeroBumpOffset());
            _hudView.DrawOverheadHpBar(_state, _player, _map);
            _combatLogView.DrawDamagePopups(_log);
            _spriteBatch.End();

            if (_state.ShowFps && _debugFont != null) _hudView.DrawFps(_state);
            _minimapView.DrawMinimap(_map, _player);
            _combatLogView.Draw(_log);
            if (_state.ShowMapOverlay) _minimapView.DrawFullMapOverlay(_map, _player);

            if (_state.IsVictory)
            {
                _menuView.DrawVictory(_state, Mouse.GetState());
            }
            else if (_state.IsGameOver)
            {
                _menuView.DrawGameOver(_state, Mouse.GetState());
            }
            else
            {
                if (_state.IsPaused) _menuView.DrawPause(_state, Mouse.GetState());
                _hudView.DrawHealthBar(_state);
                if (!_state.IsPaused)
                {
                    _hudView.DrawDamageEdges(_state);
                    _hudView.DrawLevelUpEdge(_state);
                }
            }

            if (_state.IsCharacterScreenOpen)
            {
                _characterView.Draw(_state, Mouse.GetState());
                if (_state.ShowLevelUpOverlay)
                    _levelUpView.Draw(_state, Mouse.GetState());
            }

            _hudView.DrawCursor(_state);
            base.Draw(gameTime);
        }

        protected override void UnloadContent()
        {
            _spriteBatch?.Dispose();
            _spriteBatch = null;
            base.UnloadContent();
        }

        private void DrawEnemies()
        {
            if (_uiPixel == null) return;
            foreach (var enemy in _enemies.Enemies)
            {
                if (enemy.IsDead) continue;
                if (!_map.IsExplored(enemy.Position.X, enemy.Position.Y)) continue;
                // Цвет полоски HP отражает уровень врага
                var barColor = enemy.EnemyLevel switch
                {
                    1 => new Color(220, 40,  40),   // красный
                    2 => new Color(230, 120, 20),   // оранжевый
                    3 => new Color(200, 195, 20),   // жёлтый
                    4 => new Color(170, 30,  210),  // фиолетовый
                    _ => new Color(230, 30,  130),  // малиновый (lv5)
                };
                enemy.Draw(_spriteBatch, _uiPixel, enemy.BodyColor, barColor, GetEnemyBumpOffset(enemy));
            }
        }

        /// <summary>Вычисляет смещение героя для анимации удара (синусоида по направлению).</summary>
        private Vector2 GetHeroBumpOffset()
        {
            if (_state.HeroBumpAnimTimer <= 0f || _state.HeroBumpAnimDirection == Vector2.Zero)
                return Vector2.Zero;
            var progress = 1f - (_state.HeroBumpAnimTimer / GameConstants.BumpAnimDurationSeconds);
            var amp = MathF.Sin(progress * MathF.PI) * GameConstants.BumpAnimAmplitudePx;
            return _state.HeroBumpAnimDirection * amp;
        }

        private Vector2 GetEnemyBumpOffset(Enemy enemy)
        {
            if (!_state.EnemyBumpAnimTimers.TryGetValue(enemy, out var timer) ||
                !_state.EnemyBumpAnimDirections.TryGetValue(enemy, out var dir) ||
                timer <= 0f || dir == Vector2.Zero)
                return Vector2.Zero;
            var progress = 1f - (timer / GameConstants.BumpAnimDurationSeconds);
            var amp = MathF.Sin(progress * MathF.PI) * GameConstants.BumpAnimAmplitudePx;
            return dir * amp;
        }

        /// <summary>
        /// Вызывается когда герой пытается войти на клетку с врагом.
        /// Разрешает атаку и запускает задержанный ответный ход всех врагов.
        /// </summary>
        private void HandleHeroBumpIntoEnemy(Point heroPos, Point enemyPos)
        {
            if (_state.HeroActionLocked || _state.HeroHealth <= 0) return;

            _state.JustResolvedHeroBump = true;
            var enemy = _enemies.FindEnemyAt(enemyPos);
            if (enemy == null) return;

            _combat.AdvanceTurnAndRegen(_state);

            var bumpDir = new Vector2(enemyPos.X - heroPos.X, enemyPos.Y - heroPos.Y);
            if (bumpDir != Vector2.Zero)
            {
                bumpDir.Normalize();
                _state.StartHeroBump(bumpDir);
                _state.StartEnemyBump(enemy, -bumpDir);
            }

            var died = _combat.ResolveHeroAttack(enemy, _player, _state, _log);
            if (died)
            {
                _enemies.RemoveEnemy(enemy);
                _log.AddMessage($"{enemy.Name} убит", new Color(255, 130, 130));
                _state.KillsByType[(int)enemy.Type]++;
                GainXp(enemy.XpReward);
                return;
            }

            _state.HeroActionLocked = true;
            _state.PendingEnemyCounter = enemy;
            _state.PendingEnemyCounterDelayRemaining = GameConstants.EnemyActionDelaySeconds;
            _state.PendingEnemyActionPhase = true;
        }

        /// <summary>Ждёт истечения задержки и проводит ответный ход врага.</summary>
        private void UpdatePendingEnemyCounter(float dt)
        {
            _state.PendingEnemyCounterDelayRemaining = Math.Max(0f, _state.PendingEnemyCounterDelayRemaining - dt);
            if (_state.PendingEnemyCounterDelayRemaining > 0f) return;

            if (_state.PendingEnemyActionPhase)
            {
                _state.PendingEnemyActionPhase = false;
                if (!_state.IsGameOver)
                {
                    _enemies.ProcessTurn(_map, _player, _state, _log, _combat);
                    TryStairTransition();
                }
            }

            _state.PendingEnemyCounter = null;
            _state.HeroActionLocked = false;
        }

        private void TryStairTransition()
        {
            var newMap = _floors.HandleStairTransitions(_map, _state, _player, _enemies, _log, _tilesetTexture);
            if (newMap != null) _map = newMap;
        }

        private void OpenPauseMenu()
        {
            _state.IsPaused = true;
            _state.PauseScreenState = PauseScreen.Main;
            _state.PauseKeyboardIndex = 0;
            IsMouseVisible = true;
        }

        private void ResetGame()
        {
            _state.HeroHealth           = GameConstants.HeroMaxHealth;
            _state.HeroLevel            = 1;
            _state.HeroXp               = 0;
            _state.HeroDamageBonus      = 0;
            _state.HeroMaxHealthBonus   = 0;
            _state.HeroCritChanceBonus  = 0f;
            _state.PendingLevelUp           = false;
            _state.IsCharacterScreenOpen    = false;
            _state.ShowLevelUpOverlay       = false;
            _state.LevelUpSelectedIndex     = 0;
            _state.LevelUpEdgeFlashStrength = 0f;
            _state.LevelUpBlinkAccumulator  = 0f;
            _state.HeroActionLocked = false;
            _state.PendingEnemyCounter = null;
            _state.PendingEnemyCounterDelayRemaining = 0f;
            _state.PendingEnemyActionPhase = false;
            _state.HeroTurnsSinceRegen = 0;
            _state.HeroOverHeadHpBarTimer = 0f;
            _state.EdgeFlashStrength = 0f;
            _state.HeroBumpAnimTimer = 0f;
            _state.HeroBumpAnimDirection = Vector2.Zero;
            _state.EnemyBumpAnimTimers.Clear();
            _state.EnemyBumpAnimDirections.Clear();
            _log.DamagePopups.Clear();
            _log.Entries.Clear();
            _state.StairTransitionLock = false;
            _state.JustResolvedHeroBump = false;
            _state.GameOverSelectedIndex = 0;
            _state.IsVictory = false;
            _state.VictorySelectedIndex = 0;
            Array.Clear(_state.KillsByType, 0, _state.KillsByType.Length);
            _state.TotalXpGained = 0;
            _state.IsPaused = false;
            _state.PauseScreenState = PauseScreen.Main;
            IsMouseVisible = false;

            _floors.ClearAll();
            _state.CurrentFloorIndex = 0;
            _map = _floors.GetOrCreate(0, _tilesetTexture);
            _player.SetMapAndPosition(_map, _map.PlayerSpawnPoint);
            _map.UpdateExploration(_player.Position);
            _enemies.SpawnEnemies(_map, _player, _state);
        }

        private void GainXp(int amount)
        {
            _state.TotalXpGained += amount;
            if (_state.HeroLevel >= GameConstants.MaxLevel || _state.PendingLevelUp) return;
            _state.HeroXp += amount;
            var needed = GameConstants.XpNeeded(_state.HeroLevel);
            if (_state.HeroXp < needed) return;
            _state.HeroXp -= needed;
            // Уровень вырастет только когда игрок выберет улучшение через TAB
            _state.PendingLevelUp           = true;
            _state.LevelUpSelectedIndex     = 0;
            _state.LevelUpEdgeFlashStrength = 1f;
            _log.AddMessage("Новый уровень! Нажмите TAB", new Color(100, 255, 100));
        }

        private void UpdateFps(float dt)
        {
            _state.FpsTimer += dt;
            _state.FpsFrameCounter++;
            if (_state.FpsTimer >= 1f)
            {
                _state.CurrentFps = _state.FpsFrameCounter;
                _state.FpsFrameCounter = 0;
                _state.FpsTimer -= 1f;
            }
        }

        private void UpdateCursor(MouseState mouse, float dt)
        {
            if (mouse.Position != _state.LastMousePosition)
            {
                _state.MouseIdleTime = 0f;
                _state.GameCursorAlpha = 1f;
                _state.LastMousePosition = mouse.Position;
            }
            else
            {
                _state.MouseIdleTime += dt;
                if (_state.MouseIdleTime > GameConstants.MouseCursorIdleBeforeFade)
                    _state.GameCursorAlpha = Math.Max(0f, _state.GameCursorAlpha - dt * GameConstants.MouseCursorFadeSpeed);
            }
            IsMouseVisible = false;
        }

        private void ToggleFullscreen() => SetFullscreen(!_graphics.IsFullScreen);

        private void SetFullscreen(bool isFullscreen)
        {
            if (_isApplyingDisplayChange) return;
            _isApplyingDisplayChange = true;
            try
            {
                if (isFullscreen)
                {
                    _windowedWidth  = Window.ClientBounds.Width  > 0 ? Window.ClientBounds.Width  : _windowedWidth;
                    _windowedHeight = Window.ClientBounds.Height > 0 ? Window.ClientBounds.Height : _windowedHeight;
                    var mode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
                    _graphics.PreferredBackBufferWidth  = mode.Width;
                    _graphics.PreferredBackBufferHeight = mode.Height;
                }
                else
                {
                    _graphics.PreferredBackBufferWidth  = _windowedWidth;
                    _graphics.PreferredBackBufferHeight = _windowedHeight;
                }
                _graphics.IsFullScreen = isFullscreen;
                _graphics.ApplyChanges();
            }
            finally { _isApplyingDisplayChange = false; }
        }

        private void OnClientSizeChanged(object sender, EventArgs e)
        {
            if (_isApplyingDisplayChange || _graphics.IsFullScreen) return;
            var w = Window.ClientBounds.Width;
            var h = Window.ClientBounds.Height;
            if (w <= 0 || h <= 0) return;
            _windowedWidth  = w;
            _windowedHeight = h;
            _graphics.PreferredBackBufferWidth  = w;
            _graphics.PreferredBackBufferHeight = h;
        }

        private void OnGameExiting(object sender, EventArgs args)
        {
            if (_state.VoluntaryExitRequested) return;
            if (args is not ExitingEventArgs exitingArgs) return;
            exitingArgs.Cancel = true;
            OpenPauseMenu();
        }
    }
}
