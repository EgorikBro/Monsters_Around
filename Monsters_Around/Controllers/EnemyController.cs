using Microsoft.Xna.Framework;
using Monsters_Around.Models;
using System;
using System.Collections.Generic;

namespace Monsters_Around.Controllers
{
    public class EnemyController
    {
        public List<Enemy>      Enemies         { get; } = new();
        public HashSet<Point>   EnemyPositions  { get; } = new();

        private readonly Random _random;

        public EnemyController(Random random) => _random = random;

        public bool  IsEnemyAt(Point p) => EnemyPositions.Contains(p);

        public Enemy FindEnemyAt(Point p)
        {
            foreach (var e in Enemies)
                if (e.Position == p) return e;
            return null;
        }

        public void RemoveEnemy(Enemy enemy)
        {
            EnemyPositions.Remove(enemy.Position);
            Enemies.Remove(enemy);
        }

        public void SpawnEnemies(Map map, Player player, GameState state)
        {
            Enemies.Clear();
            EnemyPositions.Clear();
            state.EnemyBumpAnimTimers.Clear();
            state.EnemyBumpAnimDirections.Clear();
            state.HeroTurnsSinceRegen = 0;

            if (map == null || player == null) return;

            var rooms    = map.Rooms;
            var reserved = new HashSet<Point>
            {
                map.PlayerSpawnPoint,
                map.StairUpPoint,
                map.StairDownPoint
            };

            for (var roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
            {
                if (roomIndex == map.StartingRoomIndex) continue;

                var room    = rooms[roomIndex];
                var toSpawn = RollEnemyCount();
                if (toSpawn <= 0) continue;

                for (var i = 0; i < toSpawn; i++)
                {
                    const int maxAttempts = 60;
                    var spawned = false;

                    for (var attempt = 0; attempt < maxAttempts; attempt++)
                    {
                        var xMin = room.Left  + 1;
                        var xMax = room.Right - 1;
                        var yMin = room.Top   + 1;
                        var yMax = room.Bottom - 1;

                        if (xMax <= xMin || yMax <= yMin) break;

                        var p = new Point(_random.Next(xMin, xMax), _random.Next(yMin, yMax));

                        if (reserved.Contains(p) || !map.IsWalkable(p.X, p.Y) || EnemyPositions.Contains(p))
                            continue;
                        if (IsEnemyTooClose(p, 1)) continue;

                        var type  = RollEnemyType();
                        var level = RollEnemyLevel(state.CurrentFloorIndex);
                        var enemy = new Enemy(p, type, level, GameConstants.TileSize);
                        enemy.ActionCooldown = _random.Next(0, enemy.MoveInterval);
                        Enemies.Add(enemy);
                        EnemyPositions.Add(p);
                        spawned = true;
                        break;
                    }

                    if (!spawned) break;
                }
            }
        }

        /// <summary>
        /// Разрешает ход всех врагов.
        /// Ближний бой: атакует каждый ход если рядом, движется по своему интервалу.
        /// Маг: и атака, и движение подчинены единому кулдауну.
        /// </summary>
        public void ProcessTurn(Map map, Player player, GameState state, CombatLog log, CombatController combat)
        {
            if (state.HeroHealth <= 0) return;

            for (var i = 0; i < Enemies.Count; i++)
            {
                var enemy = Enemies[i];
                if (enemy.IsDead) continue;

                var heroPos       = player.Position;
                var dx            = Math.Abs(heroPos.X - enemy.Position.X);
                var dy            = Math.Abs(heroPos.Y - enemy.Position.Y);
                var distManhattan = dx + dy;
                var distChebyshev = Math.Max(dx, dy);

                if (enemy.AttackRange > 1)
                {
                    // Маг: оба действия (атака и движение) регулируются одним кулдауном
                    if (enemy.ActionCooldown > 0) { enemy.ActionCooldown--; continue; }

                    if (distChebyshev <= enemy.AttackRange && HasLineOfSight(enemy.Position, heroPos, map))
                        combat.ResolveEnemyAttack(enemy, player, state, log);
                    else if (HasLineOfSight(enemy.Position, heroPos, map))
                        TryMove(enemy, heroPos, map);

                    enemy.ActionCooldown = enemy.MoveInterval - 1;
                    if (state.IsGameOver) return;
                }
                else
                {
                    // Ближний бой: атакует каждый ход когда рядом, движется по интервалу
                    if (distManhattan == 1)
                    {
                        combat.ResolveEnemyAttack(enemy, player, state, log);
                        if (state.IsGameOver) return;
                    }
                    else
                    {
                        if (enemy.ActionCooldown > 0) { enemy.ActionCooldown--; continue; }

                        if (HasLineOfSight(enemy.Position, heroPos, map))
                            TryMove(enemy, heroPos, map);

                        enemy.ActionCooldown = enemy.MoveInterval - 1;
                    }
                }
            }
        }

        public void UpdateAnimations(float dt, GameState state)
        {
            var toRemove = new List<Enemy>();
            foreach (var kv in state.EnemyBumpAnimTimers)
            {
                var newTimer = Math.Max(0f, kv.Value - dt);
                if (newTimer <= 0f || kv.Key == null || kv.Key.IsDead)
                    toRemove.Add(kv.Key);
                else
                    state.EnemyBumpAnimTimers[kv.Key] = newTimer;
            }
            foreach (var k in toRemove)
            {
                state.EnemyBumpAnimTimers.Remove(k);
                state.EnemyBumpAnimDirections.Remove(k);
            }
        }

        public void UpdateMovement(GameTime gameTime)
        {
            foreach (var enemy in Enemies)
                if (!enemy.IsDead) enemy.Update(gameTime);
        }

        public bool HasLineOfSight(Point from, Point to, Map map)
        {
            if (from == to) return true;
            var x0 = from.X; var y0 = from.Y;
            var x1 = to.X;   var y1 = to.Y;
            var dx = Math.Abs(x1 - x0);
            var dy = Math.Abs(y1 - y0);
            var sx = x0 < x1 ? 1 : -1;
            var sy = y0 < y1 ? 1 : -1;
            var err = dx - dy;

            while (true)
            {
                if (x0 == x1 && y0 == y1) return true;
                var e2 = 2 * err;
                if (e2 > -dy) { err -= dy; x0 += sx; }
                if (e2 <  dx) { err += dx; y0 += sy; }
                if (x0 == x1 && y0 == y1) return true;
                if (!map.IsWalkable(x0, y0)) return false;
            }
        }

        public Point? ChooseMove(Point enemyPos, Point heroPos, Map map)
        {
            var bestDist   = int.MaxValue;
            var candidates = new List<Point>();

            TryDir(1, 0); TryDir(-1, 0); TryDir(0, 1); TryDir(0, -1);

            return candidates.Count == 0 ? null : candidates[_random.Next(candidates.Count)];

            void TryDir(int ddx, int ddy)
            {
                var p = new Point(enemyPos.X + ddx, enemyPos.Y + ddy);
                if (p == heroPos || !map.IsWalkable(p.X, p.Y) || EnemyPositions.Contains(p)) return;
                var d = Math.Abs(p.X - heroPos.X) + Math.Abs(p.Y - heroPos.Y);
                if      (d < bestDist) { bestDist = d; candidates.Clear(); candidates.Add(p); }
                else if (d == bestDist) candidates.Add(p);
            }
        }

        private void TryMove(Enemy enemy, Point heroPos, Map map)
        {
            var next = ChooseMove(enemy.Position, heroPos, map);
            if (next.HasValue && next.Value != enemy.Position)
            {
                EnemyPositions.Remove(enemy.Position);
                enemy.MoveTo(next.Value);
                EnemyPositions.Add(next.Value);
            }
        }

        private bool IsEnemyTooClose(Point p, int minSeparation)
        {
            foreach (var e in Enemies)
            {
                if (e.IsDead) continue;
                if (Math.Abs(e.Position.X - p.X) <= minSeparation &&
                    Math.Abs(e.Position.Y - p.Y) <= minSeparation)
                    return true;
            }
            return false;
        }

        private EnemyType RollEnemyType()
        {
            var r = _random.NextDouble();
            if (r < 0.30) return EnemyType.Slime;
            if (r < 0.60) return EnemyType.Skeleton;
            if (r < 0.75) return EnemyType.Spider;
            if (r < 0.92) return EnemyType.Zombie;
            return EnemyType.Mage;
        }

        /// <summary>
        /// Определяет уровень врага (1–5) исходя из номера этажа.
        /// На ранних этажах преобладают слабые мобы, на поздних — сильные.
        /// </summary>
        private int RollEnemyLevel(int floorIndex)
        {
            // Вес каждого уровня [lv1, lv2, lv3, lv4, lv5] для этажей 0–9
            float[] w = floorIndex switch
            {
                0  => new[] { 0.88f, 0.12f, 0f,    0f,    0f    },
                1  => new[] { 0.65f, 0.30f, 0.05f, 0f,    0f    },
                2  => new[] { 0.40f, 0.42f, 0.18f, 0f,    0f    },
                3  => new[] { 0.18f, 0.42f, 0.32f, 0.08f, 0f    },
                4  => new[] { 0.05f, 0.28f, 0.42f, 0.22f, 0.03f },
                5  => new[] { 0f,    0.12f, 0.42f, 0.36f, 0.10f },
                6  => new[] { 0f,    0.04f, 0.28f, 0.46f, 0.22f },
                7  => new[] { 0f,    0f,    0.14f, 0.46f, 0.40f },
                8  => new[] { 0f,    0f,    0.05f, 0.38f, 0.57f },
                _  => new[] { 0f,    0f,    0f,    0.25f, 0.75f },
            };

            var r   = (float)_random.NextDouble();
            var acc = 0f;
            for (var i = 0; i < w.Length; i++)
            {
                acc += w[i];
                if (r < acc) return i + 1;
            }
            return GameConstants.MaxEnemyLevel;
        }

        private int RollEnemyCount()
        {
            var r = _random.NextDouble();
            if (r < 0.35)  return 0;
            if (r < 0.62)  return 1;
            if (r < 0.80)  return 2;
            if (r < 0.92)  return 3;
            if (r < 0.985) return 4;
            return 5;
        }
    }
}
