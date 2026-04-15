using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace Monsters_Around
{
    public class Game1 : Game
    {
        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;

        private Map _map;
        private Player _player;
        private Camera2D _camera;

        private const int TileSize = 16;
        private const int WindowWidth = 800;
        private const int WindowHeight = 480;
        private const int MapWidth = 100;
        private const int MapHeight = 60;
        private const float CameraZoom = 2f;

        private Texture2D _dummyWallTex;
        private Texture2D _dummyFloorTex;
        private Texture2D _dummyPlayerTex;

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this);
            Content.RootDirectory = "Content";
            IsMouseVisible = true;

            _graphics.PreferredBackBufferWidth = WindowWidth;
            _graphics.PreferredBackBufferHeight = WindowHeight;
        }

        protected override void Initialize()
        {
            _map = new Map(MapWidth, MapHeight, TileSize);
            _player = new Player(new Point(1, 1), _map);
            _camera = new Camera2D(CameraZoom);

            base.Initialize();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            _dummyWallTex = new Texture2D(GraphicsDevice, 1, 1);
            _dummyWallTex.SetData(new[] { Color.DarkGray });

            _dummyFloorTex = new Texture2D(GraphicsDevice, 1, 1);
            _dummyFloorTex.SetData(new[] { Color.LightGray });

            _dummyPlayerTex = new Texture2D(GraphicsDevice, 1, 1);
            _dummyPlayerTex.SetData(new[] { Color.Green });

            _map.LoadContent(_dummyFloorTex, _dummyWallTex);
            _player.LoadContent(_dummyPlayerTex);
        }

        protected override void Update(GameTime gameTime)
        {
            InputHandler.Update();

            if (GamePad.GetState(PlayerIndex.One).Buttons.Back == ButtonState.Pressed || Keyboard.GetState().IsKeyDown(Keys.Escape))
                Exit();

            _player.Update(gameTime);
            _camera.Follow(
                _player.WorldPosition + new Vector2(TileSize * 0.5f, TileSize * 0.5f),
                GraphicsDevice.Viewport,
                new Point(MapWidth * TileSize, MapHeight * TileSize)
            );

            base.Update(gameTime);
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(Color.Black);

            _spriteBatch.Begin(transformMatrix: _camera.GetViewMatrix());

            _map.Draw(_spriteBatch);
            _player.Draw(_spriteBatch);

            _spriteBatch.End();

            base.Draw(gameTime);
        }
    }
}
