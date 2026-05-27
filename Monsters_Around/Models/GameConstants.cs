namespace Monsters_Around.Models
{
    public static class GameConstants
    {
        public const int TileSize = 16;
        public const int WindowWidth = 800;
        public const int WindowHeight = 480;
        public const int MapWidth = 70;
        public const int MapHeight = 70;
        public const float CameraZoom = 5f;

        public const int HeroMaxHealth = 100;
        public const int HeroDefense = 5;
        public const int HeroDamageMin = 4;
        public const int HeroDamageMax = 7;
        public const int HeroCritDamage = 10;
        public const float HeroCritChance = 0.16f;
        public const float HeroMissChance = 0.05f;

        public const float EnemyCritChance = 0.14f;
        public const float EnemyMissChance = 0.05f;

        public const float EnemyActionDelaySeconds = 0.5f;
        public const float BumpAnimDurationSeconds = 0.09f;
        public const float BumpAnimAmplitudePx = 4f;
        public const float EdgeCriticalStrength = 0.16f;
        public const float EdgeFlashDurationSeconds = 0.35f;
        public const float HeroOverHeadHpBarDurationSeconds = 1.0f;

        public const int CombatLogVisibleCount = 3;
        public const int CombatLogExpandedVisibleCount = 7;
        public const float CombatLogLineHeight = 28f;
        public const float CombatLogTextScale = 1.2f;
        public const float CombatLogFadeSpeed = 2.8f;
        public const float CombatLogMoveSpeed = 16f;

        public const float LevelUpEdgeFlashDuration = 0.5f;
        public const float LevelUpBlinkSpeed = 2.5f; // пульсаций в секунду

        public const float MouseCursorIdleBeforeFade = 0.5f;
        public const float MouseCursorFadeSpeed = 2.2f;

        public const int MaxLevel = 20;
        public const int MaxFloor = 10; // последний этаж (индекс 9)
        public const int MaxEnemyLevel = 5; // максимальный уровень врага

        // Автоматический прирост характеристик героя за каждый уровень
        public const int AutoHealthPerLevel = 5; // +5 макс. HP (и лечение на 5)
        public const float AutoCritPerLevel = 0.005f; // +0.5% шанс крита
        public const int AutoDamageLevelStep = 3; // +1 урон каждые N уровней (3, 6, 9 …)
        public const float MaxCritChance = 0.90f; // потолок суммарного шанса крита

        public const int DamageUpgradeAmount = 1; // +1 к мин. и макс. урону (выбор)
        public const int HealthUpgradeAmount = 15; // +15 макс. HP и лечение (выбор)
        public const float CritUpgradeAmount = 0.04f; // +4% к шансу крита (выбор)

        /// <summary>Возвращает количество опыта, необходимое для достижения следующего уровня. Принимает текущий уровень (1..14).</summary>
        public static int XpNeeded(int level)
        {
            if (level >= MaxLevel) return int.MaxValue;
            return 3 + (level - 1) * 5 + (level - 1) * (level - 2) / 2;
        }
    }
}