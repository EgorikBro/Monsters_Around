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

            var vp = _graphicsDevice.Viewport;
            const int barH = 16;
            const int padding = 12;
            const int barW = 320;

            var hpText = state.HeroHealth.ToString();
            var textSize = _font.MeasureString(hpText);
            var y = vp.Height - barH - padding;
            var barX = (vp.Width - barW) / 2;
            var textX = Math.Max(0, barX - (int)textSize.X - 12);
            var textPos = new Vector2(textX, y - (textSize.Y - barH) * 0.5f);
            var ratio = MathHelper.Clamp((float)state.HeroHealth / GameConstants.HeroMaxHealth, 0f, 1f);
            var fillW = (int)(barW * ratio);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            _spriteBatch.DrawString(_font, hpText, textPos + Vector2.One, Color.Black * 0.35f);
            _spriteBatch.DrawString(_font, hpText, textPos, Color.White);
            _spriteBatch.Draw(_pixel, new Rectangle(barX, y, barW, barH), Color.Black * 0.5f);
            if (fillW > 0)
                _spriteBatch.Draw(_pixel, new Rectangle(barX, y, fillW, barH), Color.Red);
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

        // Must be called inside an active world-space SpriteBatch.Begin() block.
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
