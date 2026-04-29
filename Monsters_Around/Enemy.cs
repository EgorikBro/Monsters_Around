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

        private readonly int _tileSize;

        public Enemy(Point startPosition, int maxHealth, int tileSize)
        {
            Position = startPosition;
            MaxHealth = maxHealth;
            Health = maxHealth;
            _tileSize = tileSize;
        }

        public Vector2 WorldPosition => new Vector2(Position.X * _tileSize, Position.Y * _tileSize);
        public bool IsDead => Health <= 0;

        public void MoveTo(Point newPosition)
        {
            Position = newPosition;
        }

        public void TakeDamage(int damage)
        {
            Health = Math.Max(0, Health - damage);
        }

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Color bodyColor, Color barFillColor)
        {
            var rect = new Rectangle(
                (int)WorldPosition.X,
                (int)WorldPosition.Y,
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

            // Background.
            spriteBatch.Draw(pixel, new Rectangle(leftX, topY, barW, barH), Color.Black * 0.5f);
            // Fill.
            if (fillW > 0)
            {
                spriteBatch.Draw(pixel, new Rectangle(leftX, topY, fillW, barH), barFillColor);
            }
        }
    }
}

