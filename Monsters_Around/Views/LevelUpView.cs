using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using Monsters_Around.Models;
using System;

namespace Monsters_Around.Views
{
    public class LevelUpView
    {
        private readonly SpriteBatch _spriteBatch;
        private readonly SpriteFont _font;
        private readonly Texture2D _pixel;
        private readonly GraphicsDevice _graphicsDevice;

        private static readonly Color PanelBg     = new(22, 33, 62);
        private static readonly Color PanelBorder  = new(255, 215, 0);
        private static readonly Color BtnIdle      = new(15, 52, 96);
        private static readonly Color BtnHover     = new(26, 76, 130);
        private static readonly Color TitleColor   = new(255, 230, 80);
        private static readonly Color Overlay      = Color.Black * 0.65f;

        private const int BtnW     = 360;
        private const int BtnH     = 52;
        private const int Gap      = 12;
        private const int PanelPad = 32;
        private const int TitleH   = 64;
        private const int SubtitleH = 44;
        private const int HintH    = 32;

        public LevelUpView(SpriteBatch spriteBatch, SpriteFont font, Texture2D pixel, GraphicsDevice graphicsDevice)
        {
            _spriteBatch = spriteBatch;
            _font        = font;
            _pixel       = pixel;
            _graphicsDevice = graphicsDevice;
        }

        public void Update(GameState state, MouseState mouse, MouseState prevMouse)
        {
            if (!state.ShowLevelUpOverlay) return;

            const int btnCount = 3;
            var vp = _graphicsDevice.Viewport;
            ComputeLayout(vp, out _, out var firstBtnY, out var btnX);

            if (InputHandler.IsKeyPressed(Keys.Down) || InputHandler.IsKeyPressed(Keys.S))
                state.LevelUpSelectedIndex = (state.LevelUpSelectedIndex + 1) % btnCount;
            if (InputHandler.IsKeyPressed(Keys.Up) || InputHandler.IsKeyPressed(Keys.W))
                state.LevelUpSelectedIndex = (state.LevelUpSelectedIndex - 1 + btnCount) % btnCount;

            for (var i = 0; i < btnCount; i++)
            {
                var r = new Rectangle(btnX, firstBtnY + i * (BtnH + Gap), BtnW, BtnH);
                if (r.Contains(mouse.X, mouse.Y))
                    state.LevelUpSelectedIndex = i;
            }

            var choice = -1;
            if (InputHandler.IsKeyPressed(Keys.D1) || InputHandler.IsKeyPressed(Keys.NumPad1))      choice = 0;
            else if (InputHandler.IsKeyPressed(Keys.D2) || InputHandler.IsKeyPressed(Keys.NumPad2)) choice = 1;
            else if (InputHandler.IsKeyPressed(Keys.D3) || InputHandler.IsKeyPressed(Keys.NumPad3)) choice = 2;
            else if (InputHandler.IsKeyPressed(Keys.Enter) || InputHandler.IsKeyPressed(Keys.Space))
                choice = state.LevelUpSelectedIndex;
            else
            {
                for (var i = 0; i < btnCount; i++)
                {
                    var r = new Rectangle(btnX, firstBtnY + i * (BtnH + Gap), BtnW, BtnH);
                    if (mouse.LeftButton == ButtonState.Released &&
                        prevMouse.LeftButton == ButtonState.Pressed && r.Contains(mouse.X, mouse.Y))
                    { choice = i; break; }
                }
            }

            if (choice < 0) return;

            ApplyUpgrade(state, choice);
        }

        public void Draw(GameState state, MouseState mouse)
        {
            if (!state.ShowLevelUpOverlay || _font == null || _pixel == null) return;

            var vp = _graphicsDevice.Viewport;
            ComputeLayout(vp, out var panel, out var firstBtnY, out var btnX);

            _spriteBatch.Begin(samplerState: SamplerState.PointClamp);
            try
            {
                _spriteBatch.Draw(_pixel, new Rectangle(0, 0, vp.Width, vp.Height), Overlay);

                DrawPanelFrame(panel);

                var title    = $"↑  УРОВЕНЬ {state.HeroLevel + 1}  ↑"; // уровень ещё не повышен, показываем будущий
                var titlePos = new Vector2(panel.X + PanelPad, panel.Y + PanelPad);
                _spriteBatch.DrawString(_font, title, titlePos + Vector2.One * 2, Color.Black * 0.45f);
                _spriteBatch.DrawString(_font, title, titlePos, TitleColor);
                var titleW = (int)_font.MeasureString(title).X;
                _spriteBatch.Draw(_pixel,
                    new Rectangle((int)titlePos.X, (int)(titlePos.Y + _font.LineSpacing + 4), titleW, 3),
                    new Color(255, 215, 0));

                const string subtitle  = "Выберите улучшение:";
                var subPos = new Vector2(panel.X + PanelPad, panel.Y + PanelPad + TitleH);
                _spriteBatch.DrawString(_font, subtitle, subPos + Vector2.One, Color.Black * 0.35f);
                _spriteBatch.DrawString(_font, subtitle, subPos, new Color(200, 210, 230));

                string[] labels =
                {
                    $"[1]  Урон  +{GameConstants.DamageUpgradeAmount}",
                    $"[2]  ♥ Здоровье  +{GameConstants.HealthUpgradeAmount}",
                    $"[3]  Крит  +{(int)(GameConstants.CritUpgradeAmount * 100)}%"
                };

                for (var i = 0; i < labels.Length; i++)
                {
                    var r       = new Rectangle(btnX, firstBtnY + i * (BtnH + Gap), BtnW, BtnH);
                    var hover   = r.Contains(mouse.X, mouse.Y);
                    var sel     = state.LevelUpSelectedIndex == i;

                    if (sel)
                    {
                        _spriteBatch.Draw(_pixel,
                            new Rectangle(r.X - 3, r.Y - 3, r.Width + 6, r.Height + 6),
                            new Color(255, 215, 0) * 0.45f);
                    }

                    _spriteBatch.Draw(_pixel, r, hover || sel ? BtnHover : BtnIdle);
                    _spriteBatch.Draw(_pixel,
                        new Rectangle(r.X + 2, r.Y + 2, r.Width - 4, r.Height - 4),
                        Color.Black * 0.2f);

                    var labelSize = _font.MeasureString(labels[i]);
                    var labelPos  = new Vector2(
                        r.X + (r.Width  - labelSize.X) * 0.5f,
                        r.Y + (r.Height - labelSize.Y) * 0.5f);
                    _spriteBatch.DrawString(_font, labels[i], labelPos + Vector2.One, Color.Black * 0.5f);
                    _spriteBatch.DrawString(_font, labels[i], labelPos, Color.White);
                }

                const string hint      = "W/S или стрелки - навигация   Enter / 1-2-3 - выбор";
                const float hintScale  = 0.7f;
                var hintSize  = _font.MeasureString(hint) * hintScale;
                var hintPos   = new Vector2(
                    panel.X + (panel.Width - hintSize.X) * 0.5f,
                    panel.Bottom - PanelPad / 2 - hintSize.Y);
                _spriteBatch.DrawString(_font, hint, hintPos,
                    new Color(140, 150, 175) * 0.75f,
                    0f, Vector2.Zero, new Vector2(hintScale), SpriteEffects.None, 0f);
            }
            finally { _spriteBatch.End(); }
        }

        private static void ApplyUpgrade(GameState state, int index)
        {
            state.HeroLevel++;   // уровень растёт только при выборе улучшения

            switch (index)
            {
                case 0:
                    state.HeroDamageBonus += GameConstants.DamageUpgradeAmount;
                    break;
                case 1:
                    state.HeroMaxHealthBonus += GameConstants.HealthUpgradeAmount;
                    state.HeroHealth = Math.Min(
                        state.HeroHealth + GameConstants.HealthUpgradeAmount,
                        state.EffectiveMaxHealth);
                    break;
                case 2:
                    state.HeroCritChanceBonus += GameConstants.CritUpgradeAmount;
                    break;
            }

            state.ShowLevelUpOverlay      = false;
            state.PendingLevelUp          = false;
            state.IsCharacterScreenOpen   = false;
            state.LevelUpSelectedIndex    = 0;
            state.LevelUpBlinkAccumulator = 0f;
        }

        private void ComputeLayout(Viewport vp, out Rectangle panel, out int firstBtnY, out int btnX)
        {
            var btnsTotalH = 3 * BtnH + 2 * Gap;
            var panelH     = PanelPad + TitleH + SubtitleH + btnsTotalH + HintH + PanelPad;
            var panelW     = BtnW + PanelPad * 2;
            panel      = new Rectangle((vp.Width - panelW) / 2, (vp.Height - panelH) / 2, panelW, panelH);
            btnX       = panel.X + PanelPad;
            firstBtnY  = panel.Y + PanelPad + TitleH + SubtitleH;
        }

        private void DrawPanelFrame(Rectangle panel)
        {
            _spriteBatch.Draw(_pixel, panel, PanelBg);
            const int t = 3;
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Y,              panel.Width, t),      PanelBorder);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Bottom - t,     panel.Width, t),      PanelBorder);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.X, panel.Y,              t,           panel.Height), PanelBorder);
            _spriteBatch.Draw(_pixel, new Rectangle(panel.Right - t, panel.Y,      t,           panel.Height), PanelBorder);
        }
    }
}
