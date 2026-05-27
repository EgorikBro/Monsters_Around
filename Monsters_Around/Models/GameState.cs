using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Monsters_Around.Models
{
    public enum PauseScreen { Main, Controls }

    public class GameState
    {
        public int HeroHealth = GameConstants.HeroMaxHealth;
        public int HeroTurnsSinceRegen;

        public int HeroLevel = 1;
        public int HeroXp = 0;

        public int   HeroDamageBonus      = 0;
        public int   HeroMaxHealthBonus   = 0;
        public float HeroCritChanceBonus  = 0f;

        public int EffectiveMaxHealth => GameConstants.HeroMaxHealth + HeroMaxHealthBonus;

        public bool PendingLevelUp        = false;  // XP накоплен, улучшение ещё не выбрано
        public bool IsCharacterScreenOpen = false;
        public bool ShowLevelUpOverlay    = false;
        public int  LevelUpSelectedIndex  = 0;

        public float LevelUpEdgeFlashStrength = 0f;
        public float LevelUpBlinkAccumulator  = 0f;

        public int CurrentFloorIndex;
        public bool StairTransitionLock;

        public bool IsGameOver;
        public bool IsVictory;
        public int  VictorySelectedIndex;
        public bool IsPaused;
        public PauseScreen PauseScreenState = PauseScreen.Main;
        public int PauseKeyboardIndex;
        public int GameOverSelectedIndex;
        public bool VoluntaryExitRequested;

        public bool HeroActionLocked;
        public float PendingEnemyCounterDelayRemaining;
        public Enemy PendingEnemyCounter;
        public bool PendingEnemyActionPhase;
        public bool JustResolvedHeroBump;

        public float EdgeFlashStrength;
        public float HeroOverHeadHpBarTimer;

        public float HeroBumpAnimTimer;
        public Vector2 HeroBumpAnimDirection;
        public Dictionary<Enemy, float> EnemyBumpAnimTimers { get; } = new();
        public Dictionary<Enemy, Vector2> EnemyBumpAnimDirections { get; } = new();

        public bool ShowFps;
        public bool ShowMapOverlay;
        public float FpsTimer;
        public int FpsFrameCounter;
        public int CurrentFps;

        // Статистика за текущую игру (для экрана победы)
        /// <summary>Количество убитых врагов по типу (индекс = (int)EnemyType).</summary>
        public int[] KillsByType { get; } = new int[5];
        /// <summary>Суммарный полученный опыт.</summary>
        public int TotalXpGained;
        public int TotalKills => KillsByType[0] + KillsByType[1] + KillsByType[2] + KillsByType[3] + KillsByType[4];

        public float GameCursorAlpha;
        public float MouseIdleTime;
        public Point LastMousePosition;

        public void StartHeroBump(Vector2 direction)
        {
            if (direction == Vector2.Zero) return;
            direction.Normalize();
            HeroBumpAnimDirection = direction;
            HeroBumpAnimTimer = GameConstants.BumpAnimDurationSeconds;
        }

        public void StartEnemyBump(Enemy enemy, Vector2 direction)
        {
            if (enemy == null || direction == Vector2.Zero) return;
            direction.Normalize();
            EnemyBumpAnimDirections[enemy] = direction;
            EnemyBumpAnimTimers[enemy] = GameConstants.BumpAnimDurationSeconds;
        }
    }
}
