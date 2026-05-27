using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Monsters_Around.Models;
using System;

namespace Monsters_Around.Views
{
    public class HudView
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GraphicsDevice _graphicsDevice;

        public HudView(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch;
            _font = font;
            _pixel = pixel;
            _graphicsDevice = graphicsDevice;
        }

        public void DrawFps(GameState state)
        {
            if (_font == null) return;
            var text = $"FPS: {state.CurrentFps}";
            var size = _font.MeasureString(text);
            var vp = _graphicsDevice.Viewport;
            var pos = new Vector2(vp.Width - size.X - 10f, 10f);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.DrawString(_font, text, pos + new Vector2(1f, 1f), Color.Black);
            _spriteBatch.DrawString(_font, text, pos, Color.White);
            _spriteBatch.End();
        }

        public void DrawHealthBar(GameState state)
        {
            if (_pixel == null || _font == null) return;

            var vp    = _graphicsDevice.Viewport;
            var lineH = _font.LineSpacing;

            const int hpBarH   = 22;
            const int xpBarH   = 14;
            const int rowGap   = 10;
            const int padX     = 16;
            const int padY     = 10;
            const int labelGap = 12;
            const int panelW   = 540;

            var floorLabel = $"Эт.{state.CurrentFloorIndex + 1}/{GameConstants.MaxFloor}";
            var xpLabel = state.HeroLevel >= GameConstants.MaxLevel
                ? $"Ур.{state.HeroLevel}  MAX"
                : state.PendingLevelUp
                    ? $"Ур.{state.HeroLevel}  ↑"
                    : $"Ур.{state.HeroLevel}";
            var hpLabel = $"♥  {state.HeroHealth} / {state.EffectiveMaxHealth}";

            var labelW = (int)Math.Max(
                _font.MeasureString(xpLabel).X,
                _font.MeasureString(hpLabel).X);
            var barW = Math.Max(80, panelW - padX * 2 - labelW - labelGap);

            var xpRowH = Math.Max(lineH, xpBarH);
            var hpRowH = Math.Max(lineH, hpBarH);
            var panelH = padY + xpRowH + rowGap + hpRowH + padY;

            var panelX = (vp.Width  - panelW) / 2;
            var panelY =  vp.Height - panelH  - 8;
            var barX   =  panelX + padX + labelW + labelGap;

            var xpBarY   = panelY + padY + (xpRowH - xpBarH) / 2;
            var xpLabelY = panelY + padY + (xpRowH - lineH)  / 2;
            var hpBarY   = panelY + padY + xpRowH + rowGap + (hpRowH - hpBarH) / 2;
            var hpLabelY = panelY + padY + xpRowH + rowGap + (hpRowH - lineH)  / 2;

            var xpNeeded = GameConstants.XpNeeded(state.HeroLevel);
            var xpRatio  = state.HeroLevel >= GameConstants.MaxLevel ? 1f
                : MathHelper.Clamp((float)state.HeroXp / xpNeeded, 0f, 1f);
            var hpRatio  = MathHelper.Clamp((float)state.HeroHealth / state.EffectiveMaxHealth, 0f, 1f);
            var xpFillW  = (int)(barW * xpRatio);
            var hpFillW  = (int)(barW * hpRatio);

            var xpBarColor = new Color(80, 180, 255);

            // Мигание надписи «Ур.» зелёным при ожидании прокачки
            Color xpLabelColor;
            if (state.PendingLevelUp)
            {
                var pulse = 0.5f + 0.5f * MathF.Sin(
                    state.LevelUpBlinkAccumulator * GameConstants.LevelUpBlinkSpeed * MathF.PI * 2f);
                xpLabelColor = Color.Lerp(xpBarColor, Color.LimeGreen, pulse);
            }
            else
            {
                xpLabelColor = xpBarColor;
            }

            var hpFillColor  = hpRatio > 0.5f ? new Color(210, 45, 45)
                : hpRatio > 0.25f ? new Color(210, 100, 0)
                : new Color(255, 30, 30);
            var hpLabelColor = hpRatio > 0.35f ? Color.White : new Color(255, 150, 150);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);

            _spriteBatch.Draw(_pixel,
                new Rectangle(panelX, panelY, panelW, panelH), Color.Black * 0.52f);
            _spriteBatch.Draw(_pixel,
                new Rectangle(panelX, panelY, panelW, 2), new Color(50, 70, 110) * 0.9f);

            _spriteBatch.Draw(_pixel, new Rectangle(barX, xpBarY, barW, xpBarH), Color.Black * 0.55f);
            if (xpFillW > 0)
                _spriteBatch.Draw(_pixel, new Rectangle(barX, xpBarY, xpFillW, xpBarH), xpBarColor);
            _spriteBatch.Draw(_pixel, new Rectangle(barX, xpBarY, barW, 1), Color.White * 0.10f);

            _spriteBatch.DrawString(_font, xpLabel,
                new Vector2(panelX + padX, xpLabelY) + Vector2.One, Color.Black * 0.45f);
            _spriteBatch.DrawString(_font, xpLabel,
                new Vector2(panelX + padX, xpLabelY), xpLabelColor);

            // Номер этажа — справа в строке опыта
            var floorSz = _font.MeasureString(floorLabel);
            var floorX  = panelX + panelW - padX - (int)floorSz.X;
            var floorY  = xpLabelY;
            _spriteBatch.DrawString(_font, floorLabel,
                new Vector2(floorX, floorY) + Vector2.One, Color.Black * 0.45f);
            _spriteBatch.DrawString(_font, floorLabel,
                new Vector2(floorX, floorY), new Color(160, 200, 255));

            _spriteBatch.Draw(_pixel, new Rectangle(barX, hpBarY, barW, hpBarH), Color.Black * 0.55f);
            if (hpFillW > 0)
                _spriteBatch.Draw(_pixel, new Rectangle(barX, hpBarY, hpFillW, hpBarH), hpFillColor);
            _spriteBatch.Draw(_pixel, new Rectangle(barX, hpBarY, barW, 1), Color.White * 0.10f);

            _spriteBatch.DrawString(_font, hpLabel,
                new Vector2(panelX + padX, hpLabelY) + Vector2.One, Color.Black * 0.45f);
            _spriteBatch.DrawString(_font, hpLabel,
                new Vector2(panelX + padX, hpLabelY), hpLabelColor);

            _spriteBatch.End();
        }

        public void DrawDamageEdges(GameState state)
        {
            if (_pixel == null) return;
            var intensity = Math.Max(state.EdgeFlashStrength, state.HeroHealth < 30 ? GameConstants.EdgeCriticalStrength : 0f);
            if (intensity <= 0.001f) return;

            var vp = _graphicsDevice.Viewport;
            const int thickness = 26;
            var c = Color.Red * (0.12f + intensity * 0.7f);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, thickness), c);
            _spriteBatch.Draw(_pixel, new Rectangle(0, vp.Height - thickness, vp.Width, thickness), c);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, thickness, vp.Height), c);
            _spriteBatch.Draw(_pixel, new Rectangle(vp.Width - thickness, 0, thickness, vp.Height), c);
            _spriteBatch.End();
        }

        public void DrawLevelUpEdge(GameState state)
        {
            if (_pixel == null || state.LevelUpEdgeFlashStrength <= 0.001f) return;

            var vp = _graphicsDevice.Viewport;
            const int thickness = 26;
            var c = Color.LimeGreen * (0.1f + state.LevelUpEdgeFlashStrength * 0.75f);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, thickness), c);
            _spriteBatch.Draw(_pixel, new Rectangle(0, vp.Height - thickness, vp.Width, thickness), c);
            _spriteBatch.Draw(_pixel, new Rectangle(0, 0, thickness, vp.Height), c);
            _spriteBatch.Draw(_pixel, new Rectangle(vp.Width - thickness, 0, thickness, vp.Height), c);
            _spriteBatch.End();
        }

        /// <summary>Рисует полоску HP над головой героя. Должен вызываться внутри активного блока SpriteBatch.Begin() в мировых координатах.</summary>
        public void DrawOverheadHpBar(GameState state, Player player, Map map)
        {
            if (_pixel == null || map == null) return;
            if (state.HeroOverHeadHpBarTimer <= 0f || state.HeroHealth <= 0) return;

            var ratio = MathHelper.Clamp((float)state.HeroHealth / GameConstants.HeroMaxHealth, 0f, 1f);
            var alpha = MathHelper.Clamp(state.HeroOverHeadHpBarTimer / GameConstants.HeroOverHeadHpBarDurationSeconds, 0f, 1f);
            const int barH = 4;
            var tileSize = map.TileSize;
            var fillW = (int)(tileSize * ratio);
            var topY = (int)player.WorldPosition.Y - barH - 2;
            var leftX = (int)player.WorldPosition.X;

            _spriteBatch.Draw(_pixel, new Rectangle(leftX, topY, tileSize, barH), Color.Black * (0.45f * alpha));
            if (fillW > 0)
                _spriteBatch.Draw(_pixel, new Rectangle(leftX, topY, fillW, barH), Color.Red * (0.95f * alpha));
        }

        public void DrawCursor(GameState state)
        {
            if (_pixel == null || state.GameCursorAlpha <= 0.001f || state.IsPaused || state.IsGameOver) return;

            var p = state.LastMousePosition;
            var c = Color.White * state.GameCursorAlpha;
            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.Draw(_pixel, new Rectangle(p.X - 1, p.Y - 6, 2, 12), c);
            _spriteBatch.Draw(_pixel, new Rectangle(p.X - 6, p.Y - 1, 12, 2), c);
            _spriteBatch.End();
        }
    }
}
