using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace Monsters_Around
{
    public class Enemy
    {
        public Point Position { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; }
        public int Defense { get; }

        private readonly int _tileSize;
        private Vector2 _worldPosition;
        private Vector2 _moveStartWorldPosition;
        private Vector2 _moveTargetWorldPosition;
        private bool _isMoving;
        private float _moveProgress;

        private const float MoveDurationSeconds = 0.20f;

        public Enemy(Point startPosition, int maxHealth, int defense, int tileSize)
        {
            Position = startPosition;
            MaxHealth = maxHealth;
            Health = maxHealth;
            Defense = defense;
            _tileSize = tileSize;
            _worldPosition = GridToWorld(startPosition);
            _moveStartWorldPosition = _worldPosition;
            _moveTargetWorldPosition = _worldPosition;
        }

        public Vector2 WorldPosition => _worldPosition;
        public bool IsDead => Health <= 0;

        public void MoveTo(Point newPosition)
        {
            _moveStartWorldPosition = _worldPosition;
            _moveTargetWorldPosition = GridToWorld(newPosition);
            _moveProgress = 0f;
            _isMoving = true;
            Position = newPosition;
        }

        public void Update(GameTime gameTime)
        {
            if (!_isMoving)
            {
                return;
            }

            _moveProgress += (float)(gameTime.ElapsedGameTime.TotalSeconds / MoveDurationSeconds);
            var t = MathHelper.Clamp(_moveProgress, 0f, 1f);
            _worldPosition = Vector2.Lerp(_moveStartWorldPosition, _moveTargetWorldPosition, t);

            if (t >= 1f)
            {
                _worldPosition = _moveTargetWorldPosition;
                _isMoving = false;
            }
        }

        public void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
        }

        private Vector2 GridToWorld(Point gridPosition)
        {
            return new Vector2(gridPosition.X * _tileSize, gridPosition.Y * _tileSize);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Color bodyColor, Color barFillColor, Vector2 drawOffset)
        {
            var rect = new Rectangle(
                (int)(WorldPosition.X + drawOffset.X),
                (int)(WorldPosition.Y + drawOffset.Y),
                _tileSize,
                _tileSize
            );

            spriteBatch.Draw(pixel, rect, bodyColor);
            DrawHealthBar(spriteBatch, pixel, rect, barFillColor);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, Texture2D pixel, Rectangle enemyRect, Color barFillColor)
        {
            const int barH = 4;
            var barW = _tileSize;
            var topY = enemyRect.Y - barH - 2;
            var leftX = enemyRect.X;

            var ratio = MaxHealth <= 0 ? 0f : (float)Health / MaxHealth;
            ratio = MathHelper.Clamp(ratio, 0f, 1f);
            var fillW = (int)(barW * ratio);

            spriteBatch.Draw(pixel, new Rectangle(leftX, topY, barW, barH), Color.Black * 0.5f);
            if (fillW > 0)
            {
                spriteBatch.Draw(pixel, new Rectangle(leftX, topY, fillW, barH), barFillColor);
            }
        }
    }
}

