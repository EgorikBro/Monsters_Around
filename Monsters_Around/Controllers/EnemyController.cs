using Microsoft.Xna.Framework;
using Monsters_Around.Models;
using System;
using System.Collections.Generic;

namespace Monsters_Around.Controllers
{
    public class EnemyController
    {
        public List<Enemy> Enemies { get; } = new();
        public HashSet<Point> EnemyPositions { get; } = new();

        private readonly Random _random;

        public EnemyController(Random random)
        {
            _random = random;
        }

        public bool IsEnemyAt(Point p) => EnemyPositions.Contains(p);

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
            state.HeroStepsSinceEnemyTurn = 0;
            state.HeroTurnsSinceRegen = 0;

            if (map == null || player == null) return;

            var rooms = map.Rooms;
            var reserved = new HashSet<Point>
            {
                map.PlayerSpawnPoint,
                map.StairUpPoint,
                map.StairDownPoint
            };

            for (var roomIndex = 0; roomIndex < rooms.Count; roomIndex++)
            {
                if (roomIndex == map.StartingRoomIndex) continue;

                var room = rooms[roomIndex];
                var toSpawn = RollEnemyCount();
                if (toSpawn <= 0) continue;

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

                        if (xMaxExclusive <= xMin || yMaxExclusive <= yMin) break;

                        var x = _random.Next(xMin, xMaxExclusive);
                        var y = _random.Next(yMin, yMaxExclusive);
                        var p = new Point(x, y);

                        if (reserved.Contains(p) || !map.IsWalkable(p.X, p.Y) || EnemyPositions.Contains(p))
                            continue;
                        if (IsEnemyTooClose(p, 1)) continue;

                        Enemies.Add(new Enemy(p, GameConstants.EnemyMaxHealth, GameConstants.EnemyDefense, GameConstants.TileSize));
                        EnemyPositions.Add(p);
                        spawned = true;
                        break;
                    }

                    if (!spawned) break;
                }
            }
        }

        public void ProcessTurn(bool allowMovement, Map map, Player player, GameState state, CombatLog log, CombatController combat)
        {
            if (state.HeroHealth <= 0) return;

            for (var i = 0; i < Enemies.Count; i++)
            {
                var enemy = Enemies[i];
                if (enemy.IsDead) continue;

                var dist = Math.Abs(player.Position.X - enemy.Position.X) +
                           Math.Abs(player.Position.Y - enemy.Position.Y);

                if (dist == 1)
                {
                    combat.ResolveEnemyAttack(enemy, player, state, log);
                    if (state.IsGameOver) return;
                    continue;
                }

                if (!HasLineOfSight(enemy.Position, player.Position, map)) continue;
                if (!allowMovement) continue;

                var next = ChooseMove(enemy.Position, player.Position, map);
                if (next.HasValue && next.Value != enemy.Position)
                {
                    EnemyPositions.Remove(enemy.Position);
                    enemy.MoveTo(next.Value);
                    EnemyPositions.Add(next.Value);
                }

                if (state.IsGameOver) return;
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
            {
                if (!enemy.IsDead) enemy.Update(gameTime);
            }
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
                if (e2 < dx)  { err += dx; y0 += sy; }
                if (x0 == x1 && y0 == y1) return true;
                if (!map.IsWalkable(x0, y0)) return false;
            }
        }

        public Point? ChooseMove(Point enemyPos, Point heroPos, Map map)
        {
            var bestDist = int.MaxValue;
            var candidates = new List<Point>();

            TryDir(1, 0); TryDir(-1, 0); TryDir(0, 1); TryDir(0, -1);

            return candidates.Count == 0 ? null : candidates[_random.Next(candidates.Count)];

            void TryDir(int ddx, int ddy)
            {
                var p = new Point(enemyPos.X + ddx, enemyPos.Y + ddy);
                if (p == heroPos || !map.IsWalkable(p.X, p.Y) || EnemyPositions.Contains(p)) return;
                var d = Math.Abs(p.X - heroPos.X) + Math.Abs(p.Y - heroPos.Y);
                if (d < bestDist) { bestDist = d; candidates.Clear(); candidates.Add(p); }
                else if (d == bestDist) candidates.Add(p);
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
    }
}
