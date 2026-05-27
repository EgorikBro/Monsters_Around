using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using System;

namespace Monsters_Around
{
    public class Player
    {
        public Point Position { get; private set; }
        public Vector2 WorldPosition => _worldPosition;
        public bool IsMoving => _isMoving;
        public Point LastMoveDirection => _lastMoveDirection;

        public Func<Point, bool> IsEnemyAt { get; set; }
        public Action<Point, Point> OnBumpRequested { get; set; }

        private Texture2D _texture;
        private Map _map;

        private Vector2 _worldPosition;
        private Vector2 _moveStartWorldPosition;
        private Vector2 _moveTargetWorldPosition;
        private bool _isMoving;
        private float _moveProgress;
        private Point _lastMoveDirection;

        private const float MoveDurationSeconds = 0.16f;

        public Player(Point startPosition, Map map)
        {
            Position = startPosition;
            _map = map;
            _worldPosition = GridToWorld(startPosition);
        }

        public void LoadContent(Texture2D texture)
        {
            _texture = texture;
        }

        public void Update(GameTime gameTime)
        {
            if (_isMoving)
            {
                UpdateMovementAnimation(gameTime);
                if (_isMoving)
                {
                    return;
                }
            }

            var dx = 0;
            var dy = 0;

            if (InputHandler.IsKeyDown(Keys.W) || InputHandler.IsKeyDown(Keys.Up)) dy = -1;
            else if (InputHandler.IsKeyDown(Keys.S) || InputHandler.IsKeyDown(Keys.Down)) dy = 1;
            else if (InputHandler.IsKeyDown(Keys.A) || InputHandler.IsKeyDown(Keys.Left)) dx = -1;
            else if (InputHandler.IsKeyDown(Keys.D) || InputHandler.IsKeyDown(Keys.Right)) dx = 1;

            if (dx != 0 || dy != 0)
            {
                TryStartMove(dx, dy);
            }
        }

        private void TryStartMove(int dx, int dy)
        {
            var newX = Position.X + dx;
            var newY = Position.Y + dy;
            _lastMoveDirection = new Point(dx, dy);
            var targetPos = new Point(newX, newY);

            if (IsEnemyAt != null && IsEnemyAt(targetPos))
            {
                OnBumpRequested?.Invoke(Position, targetPos);
                return;
            }

            if (_map.IsWalkable(newX, newY))
            {
                _moveStartWorldPosition = _worldPosition;
                _moveTargetWorldPosition = GridToWorld(new Point(newX, newY));
                _moveProgress = 0f;
                _isMoving = true;
                Position = new Point(newX, newY);
            }
        }

        private void UpdateMovementAnimation(GameTime gameTime)
        {
            _moveProgress += (float)(gameTime.ElapsedGameTime.TotalSeconds / MoveDurationSeconds);
            var t = MathHelper.Clamp(_moveProgress, 0f, 1f);
            _worldPosition = Vector2.Lerp(_moveStartWorldPosition, _moveTargetWorldPosition, t);

            if (t >= 1f)
            {
                _worldPosition = _moveTargetWorldPosition;
                _isMoving = false;
            }
        }

        private Vector2 GridToWorld(Point gridPosition)
        {
            return new Vector2(gridPosition.X * _map.TileSize, gridPosition.Y * _map.TileSize);
        }

        public void Draw(SpriteBatch spriteBatch, Vector2 drawOffset)
        {
            if (_texture != null)
            {
                spriteBatch.Draw(
                    _texture,
                    new Rectangle((int)(_worldPosition.X + drawOffset.X), (int)(_worldPosition.Y + drawOffset.Y),
                        _map.TileSize, _map.TileSize),
                    Color.White
                );
            }
        }

        public void TeleportTo(Point position)
        {
            Position = position;
            _worldPosition = GridToWorld(position);
            _moveStartWorldPosition = _worldPosition;
            _moveTargetWorldPosition = _worldPosition;
            _moveProgress = 0f;
            _isMoving = false;
        }

        public void SetMapAndPosition(Map map, Point position)
        {
            _map = map;
            Position = position;
            _worldPosition = GridToWorld(position);
            _moveStartWorldPosition = _worldPosition;
            _moveTargetWorldPosition = _worldPosition;
            _moveProgress = 0f;
            _isMoving = false;
        }
    }
}