using Microsoft.Xna.Framework;
using Monsters_Around.Models;
using System;

namespace Monsters_Around.Controllers
{
    public class CombatController
    {
        private readonly Random _random;

        public CombatController(Random random)
        {
            _random = random;
        }

        public static float GetBlockChance(int defense) =>
            Math.Clamp(defense, 0, 100) / 100f;

        public static int ApplyDefenseReduction(int rawDamage, int defense)
        {
            if (rawDamage <= 0) return 0;
            var reduction = (int)Math.Ceiling(rawDamage * (Math.Clamp(defense, 0, 100) / 100f));
            return Math.Max(1, rawDamage - reduction);
        }

        public void AdvanceTurnAndRegen(GameState state)
        {
            state.HeroTurnsSinceRegen++;
            if (state.HeroTurnsSinceRegen < 3) return;
            state.HeroTurnsSinceRegen = 0;
            if (state.HeroHealth > 0 && state.HeroHealth < GameConstants.HeroMaxHealth)
                state.HeroHealth = Math.Min(GameConstants.HeroMaxHealth, state.HeroHealth + 1);
        }

        // Returns true if target died.
        public bool ResolveHeroAttack(Enemy target, Player player, GameState state, CombatLog log)
        {
            if (target == null || target.IsDead || state.HeroHealth <= 0) return false;

            var dist = Math.Abs(player.Position.X - target.Position.X) +
                       Math.Abs(player.Position.Y - target.Position.Y);
            if (dist != 1) return false;

            if (_random.NextDouble() < GameConstants.HeroMissChance)
            {
                log.AddMessage("Вы промахнулись", new Color(200, 200, 200));
                return false;
            }

            if (_random.NextDouble() < GetBlockChance(target.Defense))
            {
                log.AddMessage("Монстр блокирует удар", new Color(170, 220, 255));
                return false;
            }

            var isCrit = _random.NextDouble() < GameConstants.HeroCritChance;
            var damage = isCrit
                ? GameConstants.HeroCritDamage
                : _random.Next(GameConstants.HeroDamageMin, GameConstants.HeroDamageMax + 1);
            damage = ApplyDefenseReduction(damage, target.Defense);

            target.TakeDamage(damage);
            log.AddDamagePopup(target.WorldPosition, damage, isCrit, GameConstants.TileSize);
            log.AddMessage(
                isCrit ? $"Крит! Вы нанесли {damage} ед. урона" : $"Вы нанесли {damage} ед. урона",
                new Color(255, 210, 120));

            return target.IsDead;
        }

        public void ResolveEnemyAttack(Enemy attacker, Player player, GameState state, CombatLog log)
        {
            if (attacker == null || attacker.IsDead || state.HeroHealth <= 0) return;

            var heroPos = player.Position;
            var enemyPos = attacker.Position;
            var dist = Math.Abs(heroPos.X - enemyPos.X) + Math.Abs(heroPos.Y - enemyPos.Y);
            if (dist != 1) return;

            var dir = new Vector2(heroPos.X - enemyPos.X, heroPos.Y - enemyPos.Y);
            if (dir != Vector2.Zero)
            {
                dir.Normalize();
                state.StartEnemyBump(attacker, dir);
                state.StartHeroBump(-dir);
            }

            if (_random.NextDouble() < GameConstants.EnemyMissChance)
            {
                log.AddMessage("Монстр промахивается", new Color(200, 200, 200));
                return;
            }

            if (_random.NextDouble() < GetBlockChance(GameConstants.HeroDefense))
            {
                log.AddMessage("Вы блокируете удар", new Color(170, 220, 255));
                return;
            }

            state.EdgeFlashStrength = 1f;
            state.HeroOverHeadHpBarTimer = GameConstants.HeroOverHeadHpBarDurationSeconds;

            var isCrit = _random.NextDouble() < GameConstants.EnemyCritChance;
            var damage = isCrit
                ? GameConstants.EnemyCritDamage
                : _random.Next(GameConstants.EnemyDamageMin, GameConstants.EnemyDamageMax + 1);
            damage = ApplyDefenseReduction(damage, GameConstants.HeroDefense);

            state.HeroHealth = Math.Max(0, state.HeroHealth - damage);
            log.AddDamagePopup(player.WorldPosition, damage, isCrit, GameConstants.TileSize);
            log.AddMessage(
                isCrit ? $"Крит по вам: {damage} ед. урона" : $"Вы получили {damage} ед. урона",
                new Color(255, 150, 150));

            if (state.HeroHealth <= 0)
            {
                state.IsGameOver = true;
                log.AddMessage("Вы погибли", Color.OrangeRed);
            }
        }
    }
}
