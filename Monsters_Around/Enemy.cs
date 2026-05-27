using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monsters_Around.Models;

namespace Monsters_Around
{
    public enum EnemyType
    {
        Zombie,
        Skeleton,
        Slime,
        Mage,
        Spider
    }

    public class Enemy
    {
        public EnemyType Type { get; }
        public string Name { get; }
        public Color BodyColor { get; }
        public int MoveInterval { get; }

        /// <summary>Дальность атаки в расстоянии Чебышева. 1 — ближний бой, >1 — дальний.</summary>
        public int AttackRange { get; }

        public int DamageMin { get; private set; }
        public int DamageMax { get; private set; }
        public int CritDamage { get; private set; }
        public int XpReward { get; private set; }

        /// <summary>Уровень врага (1–5). Влияет на характеристики и опыт.</summary>
        public int EnemyLevel { get; }

        public Point Position { get; private set; }
        public int Health { get; private set; }
        public int MaxHealth { get; }
        public int Defense { get; }

        /// <summary>Оставшиеся ходы до следующего действия врага.</summary>
        public int ActionCooldown;

        private readonly int _tileSize;
        private Vector2 _worldPosition;
        private Vector2 _moveStartWorldPosition;
        private Vector2 _moveTargetWorldPosition;
        private bool _isMoving;
        private float _moveProgress;

        private const float MoveDurationSeconds = 0.20f;

        public Enemy(Point startPosition, EnemyType type, int enemyLevel, int tileSize)
        {
            Type = type;
            _tileSize = tileSize;

            switch (type)
            {
                case EnemyType.Zombie:
                    Name = "Зомби";
                    BodyColor = new Color(0, 110, 0);
                    MaxHealth = 50;
                    Defense = 8;
                    DamageMin = 3;
                    DamageMax = 6;
                    CritDamage = 9;
                    MoveInterval = 3;
                    AttackRange = 1;
                    XpReward = 3;
                    break;
                case EnemyType.Skeleton:
                    Name = "Скелет";
                    BodyColor = new Color(220, 220, 200);
                    MaxHealth = 30;
                    Defense = 5;
                    DamageMin = 3;
                    DamageMax = 6;
                    CritDamage = 9;
                    MoveInterval = 2;
                    AttackRange = 1;
                    XpReward = 2;
                    break;
                case EnemyType.Slime:
                    Name = "Слизень";
                    BodyColor = new Color(110, 210, 80);
                    MaxHealth = 15;
                    Defense = 2;
                    DamageMin = 1;
                    DamageMax = 3;
                    CritDamage = 5;
                    MoveInterval = 3;
                    AttackRange = 1;
                    XpReward = 1;
                    break;
                case EnemyType.Mage:
                    Name = "Маг";
                    BodyColor = new Color(155, 50, 220);
                    MaxHealth = 22;
                    Defense = 3;
                    DamageMin = 4;
                    DamageMax = 8;
                    CritDamage = 12;
                    MoveInterval = 3;
                    AttackRange = 5;
                    XpReward = 2;
                    break;
                case EnemyType.Spider:
                    Name = "Паук";
                    BodyColor = new Color(25, 25, 25);
                    MaxHealth = 20;
                    Defense = 3;
                    DamageMin = 2;
                    DamageMax = 5;
                    CritDamage = 7;
                    MoveInterval = 1;
                    AttackRange = 1;
                    XpReward = 2;
                    break;
                default:
                    Name = "Монстр";
                    BodyColor = Color.IndianRed;
                    MaxHealth = 30;
                    Defense = 7;
                    DamageMin = 2;
                    DamageMax = 5;
                    CritDamage = 8;
                    MoveInterval = 1;
                    AttackRange = 1;
                    XpReward = 1;
                    break;
            }

            // Масштабирование по уровню: каждый уровень сверх первого увеличивает силу врага
            EnemyLevel = Math.Clamp(enemyLevel, 1, GameConstants.MaxEnemyLevel);
            if (EnemyLevel > 1)
            {
                var hpScale = 1f + (EnemyLevel - 1) * 0.40f; // ×1.4 / 1.8 / 2.2 / 2.6
                var dmgBonus = (EnemyLevel - 1) * 2; // +2 / 4 / 6 / 8 к урону
                MaxHealth = (int)(MaxHealth * hpScale);
                DamageMin += dmgBonus;
                DamageMax += dmgBonus;
                CritDamage += dmgBonus + (EnemyLevel - 1); // чуть больший крит
                Defense = Math.Min(Defense + (EnemyLevel - 1) * 2, 50);
                XpReward *= EnemyLevel; // ×2 / 3 / 4 / 5 опыта
            }

            Position = startPosition;
            Health = MaxHealth;
            ActionCooldown = 0;

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
            if (!_isMoving) return;

            _moveProgress += (float)(gameTime.ElapsedGameTime.TotalSeconds / MoveDurationSeconds);
            var t = MathHelper.Clamp(_moveProgress, 0f, 1f);
            _worldPosition = Vector2.Lerp(_moveStartWorldPosition, _moveTargetWorldPosition, t);

            if (t >= 1f)
            {
                _worldPosition = _moveTargetWorldPosition;
                _isMoving = false;
            }
        }

        public void TakeDamage(int damage) => Health = Math.Max(0, Health - damage);

        private Vector2 GridToWorld(Point p) => new Vector2(p.X * _tileSize, p.Y * _tileSize);

        public void Draw(SpriteBatch spriteBatch, Texture2D pixel, Color bodyColor, Color barFillColor,
            Vector2 drawOffset)
        {
            var rect = new Rectangle(
                (int)(WorldPosition.X + drawOffset.X),
                (int)(WorldPosition.Y + drawOffset.Y),
                _tileSize, _tileSize);

            spriteBatch.Draw(pixel, rect, bodyColor);
            DrawHealthBar(spriteBatch, pixel, rect, barFillColor);
        }

        private void DrawHealthBar(SpriteBatch spriteBatch, Texture2D pixel, Rectangle enemyRect, Color barFillColor)
        {
            const int barH = 4;
            var topY = enemyRect.Y - barH - 2;
            var leftX = enemyRect.X;
            var ratio = MaxHealth <= 0 ? 0f : MathHelper.Clamp((float)Health / MaxHealth, 0f, 1f);
            var fillW = (int)(_tileSize * ratio);

            spriteBatch.Draw(pixel, new Rectangle(leftX, topY, _tileSize, barH), Color.Black * 0.5f);
            if (fillW > 0)
                spriteBatch.Draw(pixel, new Rectangle(leftX, topY, fillW, barH), barFillColor);
        }
    }
}