using Microsoft.Xna.Framework;
using System.Collections.Generic;

namespace Monsters_Around.Models
{
    public enum PauseScreen { Main, Controls }

    public class GameState
    {
        // Hero vitals
        public int HeroHealth = GameConstants.HeroMaxHealth;
        public int HeroTurnsSinceRegen;

        // Floor navigation
        public int CurrentFloorIndex;
        public bool StairTransitionLock;

        // Screen state
        public bool IsGameOver;
        public bool IsPaused;
        public PauseScreen PauseScreenState = PauseScreen.Main;
        public int PauseKeyboardIndex;
        public int GameOverSelectedIndex;
        public bool VoluntaryExitRequested;

        // Turn sequencing
        public bool HeroActionLocked;
        public float PendingEnemyCounterDelayRemaining;
        public Enemy PendingEnemyCounter;
        public bool PendingEnemyActionPhase;
        public bool PendingEnemyActionAllowMovement;
        public int HeroStepsSinceEnemyTurn;
        public bool JustResolvedHeroBump;

        // Visual feedback
        public float EdgeFlashStrength;
        public float HeroOverHeadHpBarTimer;

        // Bump animations
        public float HeroBumpAnimTimer;
        public Vector2 HeroBumpAnimDirection;
        public Dictionary<Enemy, float> EnemyBumpAnimTimers { get; } = new();
        public Dictionary<Enemy, Vector2> EnemyBumpAnimDirections { get; } = new();

        // HUD flags
        public bool ShowFps;
        public bool ShowMapOverlay;
        public float FpsTimer;
        public int FpsFrameCounter;
        public int CurrentFps;

        // Custom cursor
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
