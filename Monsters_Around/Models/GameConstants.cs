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

        // Hero stats
        public const int HeroMaxHealth = 100;
        public const int HeroDefense = 5;
        public const int HeroDamageMin = 4;
        public const int HeroDamageMax = 7;
        public const int HeroCritDamage = 10;
        public const float HeroCritChance = 0.16f;
        public const float HeroMissChance = 0.05f;

        // Enemy stats
        public const int EnemyMaxHealth = 30;
        public const int EnemyDefense = 7;
        public const int EnemyDamageMin = 2;
        public const int EnemyDamageMax = 5;
        public const int EnemyCritDamage = 8;
        public const float EnemyCritChance = 0.14f;
        public const float EnemyMissChance = 0.05f;

        // Timing
        public const float EnemyActionDelaySeconds = 0.5f;
        public const float BumpAnimDurationSeconds = 0.09f;
        public const float BumpAnimAmplitudePx = 4f;
        public const float EdgeCriticalStrength = 0.16f;
        public const float EdgeFlashDurationSeconds = 0.35f;
        public const float HeroOverHeadHpBarDurationSeconds = 1.0f;

        // Combat log
        public const int CombatLogVisibleCount = 3;
        public const int CombatLogExpandedVisibleCount = 7;
        public const float CombatLogLineHeight = 28f;
        public const float CombatLogTextScale = 1.2f;
        public const float CombatLogFadeSpeed = 2.8f;
        public const float CombatLogMoveSpeed = 16f;

        // Cursor
        public const float MouseCursorIdleBeforeFade = 0.5f;
        public const float MouseCursorFadeSpeed = 2.2f;
    }
}
